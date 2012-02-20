using System.Collections.Generic;
using System;
using kwm.Utils;
using System.Diagnostics;
using kwm.KwmAppControls;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// This message is posted to the UI thread by the WM state machine to
    /// process GUI execution requests in a clean context.
    /// </summary>
    public class WmGuiExecWakeUpMsg : UiThreadMsg
    {
        private WmStateMachine m_wm;

        public WmGuiExecWakeUpMsg(WmStateMachine wm)
        {
            m_wm = wm;
        }

        public override void Run()
        {
            m_wm.HandleGuiExecWakeUp(this);
        }
    }

    /// <summary>
    /// This class represents the state machine of the workspace manager. The
    /// state machine coordinates the state transitions of the workspace
    /// manager.
    public class WmStateMachine
    {
        /// <summary>
        /// Interval in seconds between WM serializations.
        /// </summary>
        private const UInt32 WmSerializationDelay = 5*60;

        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        private WorkspaceManager m_wm = null;

        /// <summary>
        /// Reference to the KAS communication manager. Access is restricted to
        /// starting and stopping the thread.
        /// </summary>
        private KasCommunicationManager m_kcm;
        
        /// <summary>
        /// Reference to the WmKcm broker.
        /// </summary>
        private WmKcmBroker m_wkb;

        /// <summary>
        /// True if the KCM thread is running.
        /// </summary>
        private bool m_kcmRunningFlag = false;

        /// <summary>
        /// True if the WmKcm broker has notified us that an event occurred.
        /// </summary>
        private bool m_wmKcmNotifFlag = false;

        /// <summary>
        /// This timer is used to wake-up the state machine when it needs to
        /// run.
        /// </summary>
        private KWakeupTimer m_wakeupTimer = new KWakeupTimer();

        /// <summary>
        /// Message posted to execute GUI execution requests.
        /// </summary>
        private WmGuiExecWakeUpMsg m_guiExecWakeUpMsg = null;

        /// <summary>
        /// Date at which the WM was last serialized.
        /// </summary>
        private DateTime m_lastSerializationDate;

        /// <summary>
        /// True if the state machine is running.
        /// </summary>
        private bool m_runningFlag = false;

        /// <summary>
        /// Date at which the state machine needs to run again.
        /// MinValue: the state machine needs to run at the first
        ///           opportunity.
        /// MaxValue: the state machine does not need to run.
        /// </summary>
        private DateTime m_nextRunDate = DateTime.MaxValue;

        /// <summary>
        /// True if the workspace manager is stopped or stopping.
        /// </summary>
        public bool StopFlag
        {
            get { return m_wm.MainStatus == WmMainStatus.Stopped || m_wm.MainStatus == WmMainStatus.Stopping; }
        }

        public WmStateMachine(WorkspaceManager wm)
        {
            m_wm = wm;
            m_wkb = new WmKcmBroker();
            m_kcm = new KasCommunicationManager(m_wkb, HandleKcmCompletion);
            m_wkb.Initialize(m_wm, m_kcm);
            m_wakeupTimer.TimerWakeUpCallback = HandleTimerWakeUp;
            m_lastSerializationDate = DateTime.Now;
        }

        /// <summary>
        /// This method is called to stop the WM. It returns true when the WM
        /// is ready to stop.
        /// </summary>
        private bool TryStop()
        {
            // Ask the workspaces to stop.
            bool allStopFlag = true;
            foreach (Workspace kws in m_wm.KwsTree.Values)
                if (!kws.Sm.TryStop()) allStopFlag = false;

            // Ask the KMOD broker to stop.
            if (!m_wm.KmodBroker.TryStop()) allStopFlag = false;

            // Ask the Outlook broker to stop.
            if (!m_wm.OutlookBroker.TryStop()) allStopFlag = false;

            // All workspaces and brokers must be stopped.
            if (!allStopFlag) return false;

            // All KASes must be disconnected.
            foreach (WmKas kas in m_wm.KasTree.Values)
                if (kas.ConnStatus != KasConnStatus.Disconnected) return false;

            // The GUI execution request event must be null.
            if (m_guiExecWakeUpMsg != null) return false;

            // The UI must not have been reentered.
            if (m_wm.UiBroker.UiEntryCount > 0) return false;

            // Request the KCM thread to exit now that it has disconnected all
            // our workspaces. By design there shall be no more connection 
            // requests sent to the KCM.
            if (m_kcmRunningFlag)
            {
                m_kcm.RequestCancellation();
                return false;
            }

            return true;
        }

        /// <summary>
        /// This method instructs the timer thread to send us an event at next
        /// run date so that the state machine can run at the proper time.
        /// </summary>
        private void ScheduleTimerEvent()
        {
            Int64 ms = -1;
            
            if (m_nextRunDate != DateTime.MaxValue)
            {
                DateTime now = DateTime.Now;
                if (m_nextRunDate < now) ms = 0;
                else ms = (Int64)(m_nextRunDate - now).TotalMilliseconds;
            }
            
            m_wakeupTimer.WakeMeUp(ms);
        }

        /// <summary>
        /// Return true if the state machine wants to run now.
        /// </summary>
        private bool WantToRunNow()
        {
            // Fast track.
            if (m_nextRunDate == DateTime.MinValue) return true;

            // System call path.
            return (m_nextRunDate <= DateTime.Now);
        }

        /// <summary>
        /// Run the workspace manager state machine.
        /// </summary>
        private void Run(String who)
        {
            Logging.Log("WmSm: Run() called by " + who);

            try
            {
                // Avoid reentrance.
                if (m_runningFlag)
                {
                    Logging.Log("WmSM: already running, bailing out.");
                    return;
                }

                m_runningFlag = true;

                // Loop until our state stabilize.
                while (WantToRunNow()) RunPass();

                // Schedule the next timer event appropriately.
                ScheduleTimerEvent();

                // We're no longer running the WM.
                m_runningFlag = false;
            }

            // We cannot recover from these errors.
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }
        
        /// <summary>
        /// Run once through the bowels of the state machine.
        /// </summary>
        private void RunPass()
        {
            // Reset the next run date to the maximum. During the processing of this
            // method, the next run date will lower itself as needed.
            m_nextRunDate = DateTime.MaxValue;

            // We're stopped, return.
            if (m_wm.MainStatus == WmMainStatus.Stopped)
            {
                Logging.Log("WmSm: " + Base.GetKwmString() + " is stopped, nothing to do.");
                return;
            }

            // Check if we can stop.
            else if (m_wm.MainStatus == WmMainStatus.Stopping && TryStop())
            {
                Logging.Log("WmSm: " + Base.GetKwmString() + " has stopped.");
                m_wm.MainStatus = WmMainStatus.Stopped;
                HandlePostStop();
                return;
            }

            // Serialize the WM if needed.
            SerializeWmIfNeeded();

            // Process the workspace state machines.
            ProcessKwsStateMachines();

            // Process the workspaces to delete.
            ProcessKwsToDelete();

            // Process the KASes state changes.
            ProcessKasState();

            // Process the required updates to the UI.
            ProcessUpdateUI();

            // Recompute the KAnp event processing quench.
            RecomputeKAnpQuenching();

            // Process the KCM messages.
            ProcessKcmMessages();

            // Send the workspace list to Outlook.
            SendOutlookWorkspaceList();
        }

        /// <summary>
        /// Process the workspaces to delete, once.
        /// </summary>
        private void ProcessKwsToDelete()
        {
            // Fast track.
            if (m_wm.KwsDeleteTree.Count == 0) return;

            SortedDictionary<UInt64, Workspace> tree = new SortedDictionary<UInt64, Workspace>(m_wm.KwsDeleteTree);
            foreach (Workspace kws in tree.Values)
                if (m_wm.KwsDeleteTree.ContainsKey(kws.InternalID)) ProcessOneKwsToDelete(kws);
        }

        /// <summary>
        /// Process one workspace to delete.
        /// </summary>
        private void ProcessOneKwsToDelete(Workspace kws)
        {
            // We can delete the workspace now.
            if (kws.Sm.ReadyToDelete())
            {
                // Delete the workspace data.
                kws.DeleteAllData();

                // Remove the workspace from the manager.
                m_wm.RemoveWorkspaceObject(kws);

                // Request a run of the state machine in case we want to stop.
                RequestRun("deleted " + Base.GetKwsString());
            }
        }

        /// <summary>
        /// Process the KASes state changes. Connects KASes that need
        /// to be reconnected.
        /// The loop could be optimized with a priority queue.
        /// </summary>
        private void ProcessKasState()
        {
            foreach (WmKas kas in m_wm.KasTree.Values)
            {
                // The KAS should be disconnecting if nobody is using it.
                Debug.Assert(kas.KwsConnectTree.Count > 0 ||
                             kas.ConnStatus == KasConnStatus.Disconnected ||
                             kas.ConnStatus == KasConnStatus.Disconnecting);
                
                // We want the KAS to be connected.
                if (kas.ConnStatus == KasConnStatus.Disconnected &&
                    kas.KwsConnectTree.Count > 0)
                {
                    DateTime deadline = kas.GetReconnectDeadline();

                    // Time to try again.
                    if (deadline <= DateTime.Now) ConnectKas(kas);
                    
                    // Try again later.
                    else ScheduleRun("KAS automatic reconnection", deadline);
                }
            }
        }

        /// <summary>
        /// Process any pending UI update.
        /// </summary>
        private void ProcessUpdateUI()
        {
            m_wm.UiBroker.UpdateUI();
        }

        /// <summary>
        /// Send the workspace list to Outlook.
        /// </summary>
        private void SendOutlookWorkspaceList()
        {
            m_wm.OutlookBroker.SendKwsListIfNeeded();
        }

        /// <summary>
        /// Serialize the WM if it is time to do so.
        /// </summary>
        private void SerializeWmIfNeeded()
        {
            DateTime now = DateTime.Now;

            if (m_lastSerializationDate.AddSeconds(WmSerializationDelay) < now)
            {
                m_lastSerializationDate = now;
                m_wm.Serialize(false);
            }

            ScheduleRun("WM serialization", m_lastSerializationDate.AddSeconds(WmSerializationDelay));
        }

        /// <summary>
        /// Run the workspace state machines that are ready to run, once.
        /// Also update the WM state machine next run date appropriately.
        /// The loop could be optimized with a priority queue, if needed.
        /// </summary>
        private void ProcessKwsStateMachines()
        {
            DateTime now = DateTime.Now;
            foreach (Workspace kws in m_wm.KwsTree.Values)
            {
                if (kws.Sm.NextRunDate <= now) kws.Sm.Run();
                if (kws.Sm.NextRunDate < m_nextRunDate) m_nextRunDate = kws.Sm.NextRunDate;
            }
        }

        /// <summary>
        /// Recompute the value of the KAnp event processing quench.
        /// </summary>
        private void RecomputeKAnpQuenching()
        {
            // Batch check count not yet reached.
            if (m_wm.KAnpState.CurrentBatchCount < WmKAnpState.QuenchBatchCount)
            {
                Debug.Assert(m_wm.KAnpState.QuenchFlag == false);
                return;
            }

            // Compute deadline.
            DateTime deadline = m_wm.KAnpState.CurrentBatchStartDate.AddMilliseconds(m_wm.KAnpState.CurrentBatchCount * WmKAnpState.QuenchProcessRate);
            DateTime now = DateTime.Now;

            // Enough time has passed during the processing of the batch.
            // Reset the batch statistics.
            if (deadline < now)
            {
                m_wm.KAnpState.CurrentBatchCount = 0;
                m_wm.KAnpState.CurrentBatchStartDate = now;

                // We were previously quenched.
                if (m_wm.KAnpState.QuenchFlag)
                {
                    // We're no longer quenched.
                    m_wm.KAnpState.QuenchFlag = false;

                    // Run every workspace state machine.
                    RequestAllKwsRun("KAnp event processing unquenched");
                }
            }

            // Quench for a bit.
            else
            {
                m_wm.KAnpState.QuenchFlag = true;
                ScheduleRun("KAnp event processing quenched", deadline);
            }
        }

        /// <summary>
        /// Process the messages received from the KCM, if any.
        /// </summary>
        private void ProcessKcmMessages()
        {
            // If we were not notified, bail out quickly.
            if (!m_wmKcmNotifFlag) return;

            // Clear the notification flag.
            m_wmKcmNotifFlag = false;

            // Get the messages.
            List<KcmControlMsg> controlList = new List<KcmControlMsg>();
            List<KcmAnpMsg> anpList = new List<KcmAnpMsg>();
            m_wkb.GetMessagesForWm(out controlList, out anpList);

            Logging.Log("ProcessKcmMessages(), anpList.Count = " + anpList.Count);
            // Process the messages.
            foreach (KcmControlMsg m in controlList) ProcessKcmControlMsg(m);
            foreach (KcmAnpMsg m in anpList) ProcessKcmAnpMsg(m);
            
        }

        /// <summary>
        /// Process an control messages received from the KCM.
        /// </summary>
        private void ProcessKcmControlMsg(KcmControlMsg m)
        {
            // Dispatch the message.
            if (m is KcmConnectionNotice) ProcessKcmConnectionNotice((KcmConnectionNotice)m);
            else if (m is KcmDisconnectionNotice) ProcessKcmDisconnectionNotice((KcmDisconnectionNotice)m);
            else throw new Exception("unexpected KCM control message received");
        }

        /// <summary>
        /// Process a KAS connection notice.
        /// </summary>
        private void ProcessKcmConnectionNotice(KcmConnectionNotice notice)
        {
            Debug.Assert(m_wm.KasTree.ContainsKey(notice.KasID));
            WmKas kas = m_wm.KasTree[notice.KasID];
            Debug.Assert(kas.ConnStatus == KasConnStatus.Connecting ||
                         kas.ConnStatus == KasConnStatus.Disconnecting);

            // We do not want the KAS to be connected anymore. Ignore the
            // message.
            if (kas.ConnStatus == KasConnStatus.Disconnecting) return;

            // The KAS is now connected.
            kas.ConnStatus = KasConnStatus.Connected;
            kas.MinorVersion = notice.MinorVersion;
            kas.ClearError(true);

            // Notify every workspace that the KAS is connected. Stop if the KAS
            // state changes while notifications are being sent.
            foreach (Workspace kws in kas.KwsTree.Values)
            {
                if (kas.ConnStatus != KasConnStatus.Connected) break;
                kws.Sm.HandleKasConnected();
            }
        }

        /// <summary>
        /// Process a KAS disconnection notice.
        /// </summary>
        private void ProcessKcmDisconnectionNotice(KcmDisconnectionNotice notice)
        {
            Debug.Assert(m_wm.KasTree.ContainsKey(notice.KasID));
            WmKas kas = m_wm.KasTree[notice.KasID];
            Debug.Assert(kas.ConnStatus != KasConnStatus.Disconnected);

            // Remember if we want to notify the workspaces.
            bool notifyKwsFlag = false;

            // The KAS died unexpectedly.
            if (kas.ConnStatus != KasConnStatus.Disconnecting)
            {
                // Handle the offense.
                if (notice.Ex != null)
                {
                    // Increase the failed connection attempt count if were
                    // connecting.
                    AssignErrorToKas(kas, notice.Ex, (kas.ConnStatus == KasConnStatus.Connecting));
                }
                
                // Notify the workspaces.
                notifyKwsFlag = true;
            }

            // The KAS is now disconnected.
            kas.ConnStatus = KasConnStatus.Disconnected;

            // Clear the command-reply mappings associated to the KAS.
            kas.QueryMap.Clear();

            // Notify every workspace that the KAS is disconnected.
            if (notifyKwsFlag) foreach (Workspace kws in kas.KwsTree.Values) kws.Sm.HandleKasDisconnecting();

            // Remove the KAS if we no longer need it.
            m_wm.RemoveKasIfNoRef(kas);

            // Re-run the state machine, in case we want to reconnect to the KAS
            // or stop.
            RequestRun("KAS disconnected");
        }

        /// <summary>
        /// Process an ANP message received from the KCM.
        /// </summary>
        private void ProcessKcmAnpMsg(KcmAnpMsg m)
        {
            Logging.Log("ProcessKcmAnpMsg() called");

            // We're stopping. Bail out.
            if (StopFlag) return;

            // The KAS specified does not exist. Bail out.
            if (!m_wm.KasTree.ContainsKey(m.KasID)) return;

            // The KAS is not connected. Bail out.
            WmKas kas = m_wm.KasTree[m.KasID];
            if (kas.ConnStatus != KasConnStatus.Connected) return;

            // Process the message according to its type.
            try
            {
                if (m.Msg.ID == 0) throw new Exception("backend error: " + m.Msg.Elements[1].String);
                else if (m.IsReply()) ProcessKcmAnpReply(kas, m.Msg);
                else if (m.IsEvent()) ProcessKcmAnpEvent(kas, m.Msg);
                else throw new Exception("received unexpected ANP message type (" + m.Msg.Type + ")");
            }

            catch (Exception ex)
            {
                HandleTroublesomeKas(kas, ex);
            }
        }

        /// <summary>
        /// Process an ANP reply received from the KCM.
        /// </summary>
        private void ProcessKcmAnpReply(WmKas kas, AnpMsg msg)
        {
            m_wm.UiBroker.OnAnpMsgTransfer(kas, msg);

            // We have no knowledge of the query. Ignore the reply.
            if (!kas.QueryMap.ContainsKey(msg.ID)) return;

            // Retrieve and remove the query from the query map.
            WmKasQuery query = kas.QueryMap[msg.ID];
            query.RemoveFromQueryMap();

            // Set the reply in the query.
            query.Res = msg;

            // We don't have non-workspace-related replies yet.
            Debug.Assert(query.Kws != null);
            
            // Dispatch the message to the workspace.
            query.Kws.Sm.HandleAnpReply(query);
        }

        /// <summary>
        /// Process an ANP event received from the KCM.
        /// </summary>
        private void ProcessKcmAnpEvent(WmKas kas, AnpMsg msg)
        {
            m_wm.UiBroker.OnAnpMsgTransfer(kas, msg);

            // Get the external workspace ID referred to by the event.
            UInt64 externalID = msg.Elements[0].UInt64;

            // Locate the workspace.
            Workspace kws = kas.GetWorkspaceByExternalID(externalID);

            // No such workspace, bail out.
            if (kws == null) return;

            // Dispatch the event to its workspace.
            kws.Sm.HandleAnpEvent(msg);
        }

        /// <summary>
        /// Process the GUI execution requests.
        /// </summary>
        private void ProcessGuiExecRequest()
        {
            // Execute the requests sequentially as long as the UI entry count
            // is zero.
            while (m_wm.MainStatus == WmMainStatus.Started &&
                   m_wm.UiBroker.UiEntryCount == 0 && 
                   m_wm.UiBroker.WmGuiExecRequestQueue.Count > 0)
            {
                WmGuiExecRequest wreq = m_wm.UiBroker.WmGuiExecRequestQueue.Dequeue();
                if (!wreq.Request.CancelFlag) wreq.Request.Run();
                if (wreq.Kws != null) wreq.Kws.ReleaseRef();
            }
        }

        /// <summary>
        /// Connect a KAS if it is disconnected.
        /// </summary>
        private void ConnectKas(WmKas kas)
        {
            if (kas.ConnStatus != KasConnStatus.Disconnected) return;

            // Clear the current error, but not the failed connection count.
            kas.ClearError(false);

            // Send a connection request.
            kas.ConnStatus = KasConnStatus.Connecting;
            m_wkb.RequestKasConnect(kas.KasID);

            // Update the UI (workspaces are now connecting).
            m_wm.UiBroker.RequestBrowserUiUpdate(false);
            m_wm.UiBroker.RequestSelectedKwsUiUpdate();
        }

        /// <summary>
        /// Disconnect a KAS if it is connecting/connected.
        /// </summary>
        private void DisconnectKas(WmKas kas)
        {
            if (kas.ConnStatus == KasConnStatus.Disconnecting ||
                kas.ConnStatus == KasConnStatus.Disconnected) return;

            // Send a disconnection request to the KCM.
            kas.ConnStatus = KasConnStatus.Disconnecting;
            m_wkb.RequestKasDisconnect(kas.KasID);

            // Notify every workspace that the KAS is disconnecting.
            // The workspaces will request a UI update themselves.
            foreach (Workspace kws in kas.KwsTree.Values) kws.Sm.HandleKasDisconnecting();
        }

        /// <summary>
        /// Assign an error to a KAS.
        /// </summary>
        private void AssignErrorToKas(WmKas kas, Exception ex, bool connectFailureFlag)
        {
            kas.SetError(ex, DateTime.Now, connectFailureFlag);
        }

        /// <summary>
        /// Post the event that will process the GUI execution requests if
        /// required.
        /// </summary>
        private void PostGuiExecWakeUpEventIfneeded()
        {
            if (m_wm.MainStatus == WmMainStatus.Started &&
                m_wm.UiBroker.WmGuiExecRequestQueue.Count > 0 && 
                m_guiExecWakeUpMsg == null && 
                m_wm.UiBroker.UiEntryCount == 0)
            {
                m_guiExecWakeUpMsg = new WmGuiExecWakeUpMsg(this);
                m_wm.UiBroker.PostToUi(m_guiExecWakeUpMsg);
            }
        }


        /////////////////////////////////////////////
        // Interface methods for internal events. ///
        /////////////////////////////////////////////

        /// <summary>
        /// This method is called by the timer thread to execute the state
        /// machine in a clean context.
        /// </summary>
        private void HandleTimerWakeUp(Object[] args)
        {
            Run("timer thread");
        }

        /// <summary>
        /// This method is called by the GUI wake up event to process GUI
        /// execution requests in a clean context.
        /// </summary>
        public void HandleGuiExecWakeUp(WmGuiExecWakeUpMsg msg)
        {
            // Only our posted message should call this method.
            Debug.Assert(m_guiExecWakeUpMsg == msg);

            // Process the GUI execution requests.
            ProcessGuiExecRequest();

            // Clear the reference to the message to indicate that we're done
            // executing the requests.
            m_guiExecWakeUpMsg = null;

            // Execute the state machine to stop the WM if required.
            if (m_wm.Sm.StopFlag) RequestRun("GUI wake up event when stopping");
        }

        /// <summary>
        /// This method is called by the WmKcm broker when an event interesting
        /// for us has occured. 
        /// </summary>
        public void HandleWmKcmNotification()
        {
            // Set the notification flag, set the next run date to now and run
            // the state machine. By design we're running in a clean context.
            m_wmKcmNotifFlag = true;
            m_nextRunDate = DateTime.Now;
            Run("WmKcm broker");
        }

        /// <summary>
        /// Called by the KCM thread when it has completed.
        /// </summary>
        private void HandleKcmCompletion(bool successFlag, Exception ex)
        {
            Debug.Assert(m_kcmRunningFlag);
            m_kcmRunningFlag = false;

            // We cannot handle KCM failures.
            if (!successFlag) Base.HandleException(ex, true);

            RequestRun("KCM thread stopped");
        }


        /////////////////////////////////////////////////////
        // Interface methods for workspace state machines. //
        /////////////////////////////////////////////////////

        /// <summary>
        /// Called by the workspace manager when the reference count has been
        /// decremented.
        /// </summary>
        public void HandleUiExit()
        {
            // Post a GUI execution wake up event, if required.
            PostGuiExecWakeUpEventIfneeded();

            // Run the state machine in case we have to delete some workspaces.
            RequestRun("UI exit");
        }

        /// <summary>
        /// Handle a workspace that want to be connected.
        /// </summary>
        public void HandleKwsToConnect(Workspace kws)
        {
            if (kws.InKasConnectTree()) return;

            // Add the workspace to the KAS connect tree.
            kws.AddToKasConnectTree();

            // Request an update of the UI since the connection status may
            // have changed.
            kws.StateChangeUpdate(true);

            // Run our state machine if this is the first connection request.
            if (kws.Kas.KwsConnectTree.Count == 1) RequestRun("KAS connection request");
        }

        /// <summary>
        /// Handle a workspace that want to be disconnected.
        /// </summary>
        public void HandleKwsToDisconnect(Workspace kws)
        {
            if (!kws.InKasConnectTree()) return;

            // Remove the workspace from the KAS connection tree.
            kws.RemoveFromKasConnectTree();

            // Request an update of the UI since the connection status may
            // have changed.
            kws.StateChangeUpdate(true);

            // Disconnect the KAS if no workspace want to be connected.
            if (kws.Kas.KwsConnectTree.Count == 0) DisconnectKas(kws.Kas);
        }

        /// <summary>
        /// Handle non-recoverable KAS errors (aside of disconnection notices).
        /// This method should be called when a KAS behaves badly.
        /// </summary>
        public void HandleTroublesomeKas(WmKas kas, Exception ex)
        {
            // Assign the error to the KAS.
            AssignErrorToKas(kas, ex, false);

            // Disconnect the KAS.
            DisconnectKas(kas);
        }

        /// <summary>
        /// Clear the errors associated to the KAS specified, reset the number
        /// of failed connection attempts to 0 and request a run of the state 
        /// machine, if required. This can be used to force a KAS to reconnect
        /// sooner than usual.
        /// </summary>
        public void ResetKasFailureState(WmKas kas)
        {
            if (kas.FailedConnectCount != 0 || kas.ErrorEx != null)
            {
                kas.ClearError(true);
                RequestRun("cleared KAS error");
            }
        }

        /// <summary>
        /// This method should be called when an ANP event has been processed
        /// to recompute quenching.
        /// </summary>
        public void HandleKAnpEventProcessed()
        {
            // One more event processed.
            m_wm.KAnpState.CurrentBatchCount++;

            // Update the last processed event date.
            m_wm.KAnpState.LastProcessedEventDate = DateTime.Now;

            // Recompute quenching if needed.
            if (!m_wm.KAnpState.QuenchFlag) RecomputeKAnpQuenching();
        }
        
        /// <summary>
        /// Post a GUI execution request to the workspace manager.
        /// If kws is non-null, the workspace will be bound to the request.
        /// </summary>
        public void PostGuiExecRequest(GuiExecRequest req, Workspace kws)
        {
            // Create the request and add it to the workspace manager.
            WmGuiExecRequest wreq = new WmGuiExecRequest(req, kws);
            m_wm.UiBroker.WmGuiExecRequestQueue.Enqueue(wreq);

            // Increment the reference count of the workspace. We don't want
            // it to be deleted while we have pending requests.
            if (kws != null) kws.AddRef();

            // Post a GUI execution wake up event, if required.
            PostGuiExecWakeUpEventIfneeded();
        }

        /// <summary>
        /// This method can be called by the state machine methods to request
        /// the state machine to run ASAP.
        /// </summary>
        public void RequestRun(String reason)
        {
            ScheduleRun(reason, DateTime.MinValue);
        }

        /// <summary>
        /// This method can be called by the state machine methods to request
        /// the state machine to run at Deadline. If Deadline is MinValue, the 
        /// state machine will be run again immediately.
        /// </summary>
        public void ScheduleRun(String reason, DateTime deadline)
        {
            // Remember the previous next run date.
            DateTime oldNextRunDate = m_nextRunDate;
            
            // Update the next run date.
            String ls = "WM run scheduled: " + reason + ".";

            if (deadline != DateTime.MinValue)
            {
                ls += " When: " + deadline + ".";
                if (deadline < m_nextRunDate) m_nextRunDate = deadline;
            }

            else
            {
                ls += " When: now.";
                m_nextRunDate = DateTime.MinValue;
            }

            Logging.Log(ls);

            // If we modified the next run date, notify the timer thread,
            // unless we're running inside the SM, in which case we'll call
            // ScheduleTimerEvent() after the state machine has stabilized.
            if (!m_runningFlag && oldNextRunDate != m_nextRunDate) ScheduleTimerEvent();
        }

        /// <summary>
        /// Request the state machine of every workspace to run ASAP. Also 
        /// request a run of the WM state machine.
        /// </summary>
        public void RequestAllKwsRun(String reason)
        {
            foreach (Workspace kws in m_wm.KwsTree.Values) kws.Sm.NextRunDate = DateTime.MinValue;
            RequestRun("all workspace run: " + reason);
        }


        //////////////////////////////////////
        // Miscellaneous interface methods. //
        //////////////////////////////////////

        /// <summary>
        /// This method is called by the WM spawner when it believes it has
        /// successfully created a WM instance. The WM spawner might be about
        /// to serialize the WM. This is the chance to clean up. 
        /// </summary>
        public void HandlePreStart()
        {
            Debug.Assert(m_wm.MainStatus == WmMainStatus.Stopped);

            // There might be stale KASes if some workspaces did not deserialize
            // cleanly. Remove them.
            m_wm.RemoveStaleKases();

            // There might be stale information in the DB if some workspaces did
            // not deserialize. Ask the applications to flush it, using static 
            // methods.
            RemoveStaleKwsFromDb();
        }

        /// <summary>
        /// This method is called when the workspace manager has stopped.
        /// The WM is serialized and the application is told to quit.
        /// </summary>
        private void HandlePostStop()
        {
            Debug.Assert(m_wm.MainStatus == WmMainStatus.Stopped);
            m_wm.Serialize(false);
            Misc.MainForm.Close();
        }

        /// <summary>
        /// Request the workspace manager to start. This should be
        /// called only when the KWM application is running.
        /// </summary>
        public void RequestStart()
        {
            if (m_wm.MainStatus != WmMainStatus.Stopped) return;
            Debug.Assert(m_kcmRunningFlag == false);

            // We're now starting.
            m_wm.MainStatus = WmMainStatus.Starting;

            // Update the broker on startup.
            m_wm.UiBroker.UpdateOnStartup();

            // Start the KCM.
            m_kcmRunningFlag = true;
            m_kcm.Start();

            // Enable the Outlook broker.
            m_wm.OutlookBroker.Enable();

            // Update the state of the workspaces.
            UpdateKwsStateOnStartup();

            // Handle workspaces that need to be upgraded.
            CheckForKwsUpgradeOnStartup();

            // Ask a run of all the KWS state machines and of the WM state machine as well.
            m_nextRunDate = DateTime.MaxValue;
            RequestAllKwsRun("WM started");

            // We're now started if we were not requested to stop in
            // the mean time.
            if (m_wm.MainStatus == WmMainStatus.Starting)
            {
                m_wm.MainStatus = WmMainStatus.Started;

                // Perform the imports, if required.
                m_wm.ProcessPendingKwsToImport();

                // Post a GUI execution wake up event, if required.
                PostGuiExecWakeUpEventIfneeded();
            }
        }

        /// <summary>
        /// Request the workspace manager to stop.
        /// </summary>
        public void RequestStop()
        {
            if (StopFlag) return;
            
            // We're now stopping.
            m_wm.MainStatus = WmMainStatus.Stopping;

            // Call TryStop() to stop as much stuff as possible now.
            TryStop();

            // Run our state machine to check if we can stop now.
            RequestRun("WM stopping");
        }

        /// <summary>
        /// Update the state of the workspaces when the workspace manager is
        /// starting.
        /// </summary>
        private void UpdateKwsStateOnStartup()
        {
            foreach (Workspace kws in m_wm.KwsTree.Values) kws.Sm.UpdateStateOnStartup();
        }

        /// <summary>
        /// Remove the information associated to stale workspaces from the
        /// database.
        /// </summary>
        private void RemoveStaleKwsFromDb()
        {
            foreach (UInt64 id in m_wm.LocalDbBroker.GetKwsList().Keys)
                if (!m_wm.KwsTree.ContainsKey(id)) RemoveOneStaleKwsFromDb(id);
        }

        /// <summary>
        /// Remove the information associated to a stale workspace from the
        /// database.
        /// </summary>
        private void RemoveOneStaleKwsFromDb(UInt64 id)
        {
            // Create a temporary workspace object and ask it to delete the
            // database data.
            Workspace kws = new Workspace(m_wm, null, id, null);
            kws.DeleteDbData();
            m_wm.SetDirty();
        }

        /// <summary>
        /// Check if there are workspaces that need to be rebuilt in order
        /// to complete an upgrade. This method does not consider workspaces
        /// that will not preserve their user data, since there is a good
        /// chance that the user will blindly click on "OK" and loses all his
        /// data in this case.
        /// </summary>
        private void CheckForKwsUpgradeOnStartup()
        {
            List<Workspace> upgradeList = new List<Workspace>();
            foreach (Workspace kws in m_wm.KwsTree.Values)
            {
                if (kws.MainStatus == KwsMainStatus.RebuildRequired &&
                    kws.CoreData.RebuildInfo.UpgradeFlag &&
                    !kws.CoreData.RebuildInfo.DeleteLocalDataFlag)
                {
                    upgradeList.Add(kws);
                }
            }

            foreach (Workspace kws in upgradeList)
                kws.Sm.RequestTaskSwitch(KwsTask.Rebuild);
        }

        /// <summary>
        /// Post the KAS query specified.
        /// </summary>
        public void PostKasQuery(WmKasQuery query)
        {
            Debug.Assert(!query.Kas.QueryMap.ContainsKey(query.MsgID));
            query.Kas.QueryMap[query.MsgID] = query;
            m_wm.UiBroker.OnAnpMsgTransfer(query.Kas, query.Cmd);
            m_wkb.SendAnpMsgToKcm(new KcmAnpMsg(query.Cmd, query.Kas.KasID));
        }
    }
}