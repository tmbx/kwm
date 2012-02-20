using System;
using System.Diagnostics;
using kwm.KwmAppControls;
using kwm.Utils;
using Tbx.Utils;

/* Notes about the workspace state machine:
 *
 * The primary role of the state machine is to transition between the various
 * workspace tasks gracefully and to coordinate the delivery of notifications
 * about state changes to the interested parties.
 * 
 * The main levels of functionality managed by the state machine are:
 * 1) The connection to the KAS.
 * 2) The login on the KAS.
 * 3) The execution of the workspace applications.
 * 
 * The complexity of the state machine lies in the delivery of notifications
 * about state changes. Every time such a notification is delivered, the state
 * of the workspace may change again. This situation can potentially lead to a 
 * cascade of notifications, some of them stale. To handle this complexity, the
 * state machine uses a fixed and simple processing pipeline, as follow.
 * 
 * The current workspace task determines the levels of functionality that the
 * state machine needs to attain. When a task switch occurs, the state machine
 * lowers the levels of functionality immediately if required. All interested
 * parties are notified of the state change. Then, the state machine gradually
 * increases the level of functionality, notifying the interested parties along
 * the way. The same processing occurs if an external event causes the levels
 * of functionality to change.
 * 
 * The state machine guarantees that a stable state will be reached eventually.
 * For that purpose, the state machine assumes no knowledge of the behavior of 
 * the external parties. It is built to be resilient to any state change
 * requested by these parties. All it cares about is to manage the workspace
 * tasks and the levels of functionality properly. Consequently, all external
 * parties may safely request state changes at any time. These state change
 * requests will be ignored if they are illegal.
 * 
 * The state machine uses two types of notifications: immediate and deferred.
 * Immediate notifications are delivered immediately after an event has been
 * received and the state machine has updated its state. This allows the
 * listeners to react to the event in real time (for example, when a KAS has 
 * just been connected). Deferred notifications are delivered when the state 
 * has stabilized. This allows the listeners to stabilize their own state.
 */

namespace kwm
{
    /// <summary>
    /// This class represents the state machine of a workspace. The state
    /// machine coordinates the state transitions of the workspace.
    /// </summary>
    public class KwsStateMachine
    {
        /// <summary>
        /// Reference to the workspace manager owning this workspace.
        /// </summary>
        private WorkspaceManager m_wm;

        /// <summary>
        /// Reference to the workspace.
        /// </summary>
        private Workspace m_kws;

        /// <summary>
        /// Date at which the state machine needs to run again.
        /// MinValue: the state machine needs to run at the first
        ///           opportunity.
        /// MaxValue: the state machine does not need to run.
        /// Only access this from the context of the state machines.
        /// </summary>
        public DateTime NextRunDate = DateTime.MaxValue;

        /// <summary>
        /// True if the listeners must be sent a deferred notification to
        /// the effect that the status of the workspace has changed.
        /// </summary>
        private bool m_notifyFlag = false;

        /// <summary>
        /// True if a task switch is in progress.
        /// </summary>
        private bool m_taskSwitchFlag = false;

        /// <summary>
        /// Current rebuilding step, if any.
        /// </summary>
        private KwsRebuildTaskStep m_rebuildStep = KwsRebuildTaskStep.None;

        /// <summary>
        /// Current spawn step, if any.
        /// </summary>
        private KwsSpawnTaskStep m_spawnStep = KwsSpawnTaskStep.Wait;

        /// <summary>
        /// Task currently in progress. This field must be Stop when the
        /// workspace manager is stopped.
        /// </summary>
        private KwsTask m_currentTask = KwsTask.Stop;

        public KwsStateMachine(WorkspaceManager wm, Workspace kws)
        {
            m_wm = wm;
            m_kws = kws;
        }

        /// <summary>
        /// Validate our internal state invariants.
        /// </summary>
        private void CheckInvariants()
        {
            Debug.Assert(m_kws.UserTask == KwsTask.Stop ||
                         m_kws.UserTask == KwsTask.WorkOffline ||
                         m_kws.UserTask == KwsTask.WorkOnline);

            // Conditional xor check for the deletion tree.
            Debug.Assert((m_currentTask != KwsTask.Delete) ^ (m_kws.InKwsDeleteTree()));

            // The applications shouldn't be running if we don't want them to run.
            Debug.Assert(WantAppRunning() ||
                         m_kws.AppStatus == KwsAppStatus.Stopping ||
                         m_kws.AppStatus == KwsAppStatus.Stopped);

            // We shouldn't be in the KAS connect tree if we don't want to be connected.
            Debug.Assert(WantKasConnected() || !m_kws.InKasConnectTree());

            // A logout request should have been sent if we don't want to be logged in.
            Debug.Assert(WantLogin() ||
                         m_kws.LoginStatus == KwsLoginStatus.LoggingOut ||
                         m_kws.LoginStatus == KwsLoginStatus.LoggedOut);

            // We should be logged out if we are not connected.
            Debug.Assert(m_kws.Kas.ConnStatus == KasConnStatus.Connected ||
                         m_kws.LoginStatus == KwsLoginStatus.LoggedOut);
        }

        /// <summary>
        /// Return true if the state machine wants to run now.
        /// </summary>
        private bool WantToRunNow()
        {
            // Fast track.
            if (NextRunDate == DateTime.MinValue) return true;

            // System call path.
            return (NextRunDate <= DateTime.Now);
        }

        /// <summary>
        /// Run once through the bowels of the state machine.
        /// </summary>
        private void RunPass()
        {
            // Reset the next run date to the maximum. During the processing of this
            // method, the next run date will lower itself as needed.
            NextRunDate = DateTime.MaxValue;

            CheckInvariants();

            // Process the deferred notifications.
            ProcessDeferredNotif();

            // Handle task-specific actions.
            ProcessKwsRebuildIfNeeded();

            // Start the applications if required.
            StartAppIfNeeded();

            // Connect to the KAS if required.
            ConnectToKasIfNeeded();

            // Send a login request if required.
            LoginIfNeeded();

            // Dispatch the unprocessed ANP events if required.
            DispatchUnprocessedKAnpEvents();

            // Check if we have caught up with the KAS events.
            UpdateKAnpCatchUpState();

            CheckInvariants();
        }

        /// <summary>
        /// Return true if the applications should be running.
        /// </summary>
        private bool WantAppRunning()
        {
            return (m_currentTask == KwsTask.WorkOffline || m_currentTask == KwsTask.WorkOnline);
        }

        /// <summary>
        /// Return true if the KAS of the workspace should be connected.
        /// </summary>
        private bool WantKasConnected()
        {
            return (m_currentTask == KwsTask.WorkOnline ||
                    m_currentTask == KwsTask.Spawn && m_spawnStep >= KwsSpawnTaskStep.Connect);
        }

        /// <summary>
        /// Return true if the workspace should be logged in.
        /// </summary>
        private bool WantLogin()
        {
            return (m_currentTask == KwsTask.WorkOnline ||
                    m_currentTask == KwsTask.Spawn && m_spawnStep >= KwsSpawnTaskStep.Login);
        }

        /// <summary>
        /// Start the applications if required.
        /// </summary>
        private void StartAppIfNeeded()
        {
            if (WantAppRunning()) m_kws.StartApp();
        }

        /// <summary>
        /// Stop the applications if required.
        /// </summary>
        private void StopAppIfNeeded()
        {
            if (!WantAppRunning()) m_kws.StopApp();
        }

        /// <summary>
        /// Connect to the KAS if required.
        /// </summary>
        private void ConnectToKasIfNeeded()
        {
            if (WantKasConnected() && !m_kws.InKasConnectTree()) m_wm.Sm.HandleKwsToConnect(m_kws);
        }

        /// <summary>
        /// Disconnect from the KAS if required.
        /// </summary>
        private void DisconnectFromKasIfNeeded()
        {
            if (!WantKasConnected() && m_kws.InKasConnectTree()) m_wm.Sm.HandleKwsToDisconnect(m_kws);
        }

        /// <summary>
        /// Login if required.
        /// </summary>
        private void LoginIfNeeded()
        {
            if (WantLogin() &&
                m_kws.Kas.ConnStatus == KasConnStatus.Connected &&
                m_kws.LoginStatus == KwsLoginStatus.LoggedOut)
            {
                m_kws.LoginStatus = KwsLoginStatus.LoggingIn;
                m_kws.KasLoginHandler.PerformLogin();
            }
        }

        /// <summary>
        /// Logout if required.
        /// </summary>
        private void LogoutIfNeeded()
        {
            if (!WantLogin() &&
                m_kws.LoginStatus != KwsLoginStatus.LoggedOut &&
                m_kws.LoginStatus != KwsLoginStatus.LoggingOut)
            {
                Debug.Assert(m_kws.Kas.ConnStatus == KasConnStatus.Connected);
                m_kws.LoginStatus = KwsLoginStatus.LoggingOut;
                m_kws.KasLoginHandler.PerformLogout();
            }
        }

        /// <summary>
        /// Interrupt any rebuild in progress if needed.
        /// </summary>
        private void StopRebuildIfNeeded()
        {
            m_rebuildStep = KwsRebuildTaskStep.None;
        }

        /// <summary>
        /// Switch the current task to the task specified. Don't call this 
        /// outside RequestSwitchTask() unless you know what you are doing.
        /// The state machine will run ASAP to handle the new state.
        /// </summary>
        private void SwitchTask(KwsTask task)
        {
            // The order of the calls is important here.
            Debug.Assert(m_taskSwitchFlag == false);
            m_taskSwitchFlag = true;
            m_currentTask = task;
            StopAppIfNeeded();
            DisconnectFromKasIfNeeded();
            LogoutIfNeeded();
            StopRebuildIfNeeded();
            m_kws.StateChangeUpdate(true);
            SetDeferredNotif("task switch to " + task);
            m_taskSwitchFlag = false;
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.TaskSwitch, task));
        }

        /// <summary>
        /// Switch to the user task if possible.
        /// </summary>
        private void SwitchToUserTask()
        {
            RequestTaskSwitch(m_kws.UserTask);
        }

        /// <summary>
        /// Ask the state machine to send a deferred notification to the
        /// listeners ASAP.
        /// </summary>
        private void SetDeferredNotif(String reason)
        {
            if (!m_notifyFlag)
            {
                m_notifyFlag = true;
                RequestRun("deferred notification: " + reason);
            }
        }

        /// <summary>
        /// Process the deferred notifications.
        /// </summary>
        private void ProcessDeferredNotif()
        {
            while (m_notifyFlag)
            {
                m_notifyFlag = false;

                // Exceptions are fatal here. Let them trickle up outside the
                // state machine.
                m_kws.FireKwsStatusChanged();
            }
        }

        /// <summary>
        /// Process workspace rebuild if required.
        /// </summary>
        private void ProcessKwsRebuildIfNeeded()
        {
            Debug.Assert(m_rebuildStep == KwsRebuildTaskStep.None);

            if (m_currentTask != KwsTask.Rebuild) return;

            // Sanity check.
            if (m_kws.MainStatus != KwsMainStatus.RebuildRequired)
            {
                Logging.Log("cannot execute rebuild task, " + Base.GetKwsString() + " status is not RebuildRequired");
                RequestTaskSwitch(KwsTask.Stop);
                return;
            }

            // We cannot rebuild until the applications are stopped and we're logged out.
            if (m_kws.AppStatus != KwsAppStatus.Stopped || m_kws.LoginStatus != KwsLoginStatus.LoggedOut)
                return;

            // Protect against spurious state changes.
            m_rebuildStep = KwsRebuildTaskStep.InProgress;

            // Clear the selected user in the UI.
            m_wm.UiBroker.ClearSelectedUserOnRebuild(m_kws);

            // Delete the events and update the KANP state.
            if (m_kws.CoreData.RebuildInfo.DeleteCachedEventsFlag)
            {
                m_kws.DeleteEventsFromDb();
                m_kws.KAnpState.CaughtUpFlag = false;
                m_kws.KAnpState.RebuildFlag = true;
                m_kws.KAnpState.LastReceivedEventId = 0;
                m_kws.KAnpState.LoginLatestEventId = 0;
                m_kws.KAnpState.NbUnprocessedEvent = 0;
            }

            // Tell the applications to prepare for rebuild.
            foreach (KwsApp app in m_kws.AppTree.Values)
            {
                if (m_rebuildStep != KwsRebuildTaskStep.InProgress) return;

                try
                {
                    app.PrepareForRebuild(m_kws.CoreData.RebuildInfo);
                }

                catch (Exception ex)
                {
                    HandleAppFailure(app, ex);
                    return;
                }
            }

            if (m_rebuildStep != KwsRebuildTaskStep.InProgress) return;

            // Clear the rebuild info flags.
            m_kws.CoreData.RebuildInfo.DeleteCachedEventsFlag = false;
            m_kws.CoreData.RebuildInfo.DeleteLocalDataFlag = false;
            m_kws.CoreData.RebuildInfo.UpgradeFlag = false;

            // We have "rebuilt" the workspace. Switch to the user task.
            m_rebuildStep = KwsRebuildTaskStep.None;
            m_kws.MainStatus = KwsMainStatus.Good;
            m_kws.SetDirty();
            SetLoginType(KwsLoginType.NoPwdPrompt);
            SwitchToUserTask();
        }

        /// <summary>
        /// Request the WM to clear the errors of our KAS, if any.
        /// </summary>
        private void ResetKasFailureState()
        {
            m_wm.Sm.ResetKasFailureState(m_kws.Kas);
        }

        /// <summary>
        /// Blame the KAS of this workspace for the failure specified.
        /// </summary>
        private void BlameKas(Exception ex)
        {
            m_wm.Sm.HandleTroublesomeKas(m_kws.Kas, ex);
        }

        /// <summary>
        /// Dispatch an ANP event and update the state as needed.
        /// </summary>
        private void DispatchAnpEvent(AnpMsg msg)
        {
            // Dispatch the event to the appropriate handler.
            KwsAnpEventStatus newStatus = DispatchAnpEventToHandler(msg);

            // For quenching purposes we assume the event was processed.
            m_wm.Sm.HandleKAnpEventProcessed();

            // If the ANP event has been processed, update its
            // entry in the database and the catch up state.
            if (newStatus == KwsAnpEventStatus.Processed)
            {
                Debug.Assert(m_kws.KAnpState.NbUnprocessedEvent > 0);
                m_kws.UpdateEventStatusInDb(msg.ID, KwsAnpEventStatus.Processed);
                m_kws.KAnpState.NbUnprocessedEvent--;
                UpdateKAnpCatchUpState();
                m_kws.SetDirty();

                //Fire an event to tell others about it (outlook pane)
                m_kws.DoOnEventReceived(msg);
            }
        }

        /// <summary>
        /// Dispatch an ANP event to the appropriate handler.
        /// </summary>
        private KwsAnpEventStatus DispatchAnpEventToHandler(AnpMsg msg)
        {
            // We cannot process the event yet.
            if (!CanProcessKanpEvent()) return KwsAnpEventStatus.Unprocessed;

            // If this event version is not supported, disable the workspace.
            if (msg.Minor > KAnp.Minor)
            {
                m_kws.AppException = new Exception("Your " + Base.GetKwmString() + " is too old for this server. Please update your software.");
                RequestTaskSwitch(KwsTask.Stop);
                return KwsAnpEventStatus.Unprocessed;
            }

            // Dispatch to the appropriate handler.
            try
            {
                UInt32 ns = KAnp.GetNsFromType(msg.Type);
                KwsAnpEventStatus status = KwsAnpEventStatus.Unprocessed;

                // Non-application-specific event.
                if (ns == KAnpType.KANP_NS_KWS)
                {
                    // Set the workspace dirty preemptively and dispatch.
                    m_kws.SetDirty();
                    status = m_kws.KasEventHandler.HandleAnpEvent(msg);
                }

                // Application-specific event.
                else
                {
                    // Trivially process whiteboard events.
                    if (ns == KAnpType.KANP_NS_WB) return KwsAnpEventStatus.Processed;

                    // Locate the application.
                    KwsApp app = m_kws.GetApp(ns);
                    if (app == null) throw new Exception("unknown application of type " + ns);

                    // Set the application dirty preemptively and dispatch.
                    app.SetDirty("event received");
                    status = app.HandleAnpEvent(msg);
                }

                // Throw an exception if we cannot process an event that we
                // should have been able to process.
                if (status == KwsAnpEventStatus.Unprocessed && CanProcessKanpEvent())
                    throw new Exception("cannot process KAnp event");

                return status;
            }

            // Leave the message unprocessed, blame the KAS, request the 
            // workspace to stop.
            catch (Exception ex)
            {
                BlameKas(ex);
                RequestTaskSwitch(KwsTask.Stop);
                return KwsAnpEventStatus.Unprocessed;
            }
        }

        /// <summary>
        /// Dispatch the unprocessed KAS ANP events.
        /// </summary>
        private void DispatchUnprocessedKAnpEvents()
        {
            while (m_kws.KAnpState.NbUnprocessedEvent > 0 && CanProcessKanpEvent())
            {
                AnpMsg msg = m_kws.GetFirstUnprocessedEventInDb();

                // Ouch! Trust the DB...
                if (msg == null)
                {
                    Logging.Log(2, Base.GetKwsString() + " thinks it has unprocessed events, but DB says there aren't.");
                    m_kws.KAnpState.NbUnprocessedEvent = 0;
                    m_kws.SetDirty();
                    break;
                }

                DispatchAnpEvent(msg);
            }
        }

        /// <summary>
        /// Update the KAS ANP catch up state.
        /// </summary>
        private void UpdateKAnpCatchUpState()
        {
            if (m_kws.KAnpState.CaughtUpFlag ||
                m_kws.KAnpState.NbUnprocessedEvent > 0 ||
                m_kws.LoginStatus != KwsLoginStatus.LoggedIn) return;

            bool caughtUpFlag = (m_kws.KAnpState.LastReceivedEventId >= (UInt64)m_kws.KAnpState.LoginLatestEventId);

            if (caughtUpFlag)
            {
                m_kws.KAnpState.CaughtUpFlag = true;

                if (m_kws.KAnpState.RebuildFlag)
                {
                    m_kws.KAnpState.RebuildFlag = false;
                    m_kws.SetDirty();
                }

                SetDeferredNotif("caught up with KAS events");
            }
        }


        ///////////////////////////////////////////
        // Interface methods for state machines. //
        ///////////////////////////////////////////

        /// <summary>
        /// Called by the workspace when the reference count has been decremented.
        /// </summary>
        public void HandleReleaseRef()
        {
            // Notify the WM that the workspace can be deleted.
            if (m_kws.RefCount == 0 && m_currentTask == KwsTask.Delete)
                RequestRun("last " + Base.GetKwsString() + " reference is gone");
        }

        /// <summary>
        /// This method is called by the WM to stop the workspace. This method
        /// must return true when the workspace has stopped.
        /// </summary>
        public bool TryStop()
        {
            if (m_currentTask != KwsTask.Stop && m_currentTask != KwsTask.Delete) RequestTaskSwitch(KwsTask.Stop);
            if (m_currentTask != KwsTask.Stop) return false;
            if (m_kws.AppStatus != KwsAppStatus.Stopped) return false;
            if (m_kws.LoginStatus != KwsLoginStatus.LoggedOut) return false;
            return true;
        }

        /// <summary>
        /// Update the state of the workspace when the workspace manager is
        /// starting.
        /// </summary>
        public void UpdateStateOnStartup()
        {
            // We should be stopped at this point.
            Debug.Assert(m_currentTask == KwsTask.Stop);
            CheckInvariants();

            // The workspace shouldn't exist anymore. Request its deletion.
            if (m_kws.MainStatus == KwsMainStatus.NotYetSpawned ||
                m_kws.MainStatus == KwsMainStatus.OnTheWayOut)
            {
                m_kws.Sm.RequestTaskSwitch(KwsTask.Delete);
            }

            // We want the workspace to exist.
            else
            {
                // Warn the applications that the WM is starting.
                m_kws.NormalizeAppState();

                // A rebuild is required. Keep the workspace stopped and let
                // the WM handles it as it sees fit.
                if (m_kws.MainStatus == KwsMainStatus.RebuildRequired)
                {
                    // Void.
                }

                // The workspace is ready to work. Switch to the user task, if needed.
                else
                {
                    SetLoginType(KwsLoginType.NoPwdPrompt);
                    SwitchToUserTask();
                }
            }

            CheckInvariants();
        }

        /// <summary>
        /// This method can be called by the state machine methods and its helpers 
        /// to request the state machine to run ASAP.
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
            if (deadline < NextRunDate)
            {
                NextRunDate = deadline;
                m_wm.Sm.ScheduleRun(Base.GetKwsString() + m_kws.InternalID + ": " + reason, deadline);
            }
        }

        /// <summary>
        /// Run the workspace state machine.
        /// 
        /// Important: this method should only be called by the workspace manager
        ///            state machine.
        /// </summary>
        public void Run()
        {
            // Loop until our state stabilize.
            while (WantToRunNow()) RunPass();
        }

        /// <summary>
        /// This method returns true if the workspace can be deleted safely.
        /// </summary>
        public bool ReadyToDelete()
        {
            return (m_currentTask == KwsTask.Delete &&
                    m_wm.UiBroker.UiEntryCount == 0 &&
                    m_kws.RefCount == 0 &&
                    m_kws.AppStatus == KwsAppStatus.Stopped &&
                    m_kws.LoginStatus == KwsLoginStatus.LoggedOut);
        }

        /// <summary>
        /// Called when the KAS has connected.
        /// </summary>
        public void HandleKasConnected()
        {
            // Let the state machine sort it out.
            RequestRun("KAS connected");

            // Notify the listeners.
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.Connected));
        }

        /// <summary>
        /// Called when the KAS is disconnecting or has disconnected.
        /// </summary>
        public void HandleKasDisconnecting()
        {
            // Request an update of the UI (no longer connecting).
            m_kws.StateChangeUpdate(true);

            // Let the state machine sort it out.
            RequestRun("KAS disconnected");

            // Update our state.
            UpdateStateOnLogout();

            // Notify the listeners.
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.Disconnecting, m_kws.Kas.ErrorEx));
        }

        /// <summary>
        /// Called when the workspace becomes logged in.
        /// </summary>
        public void HandleLoginSuccess()
        {
            Debug.Assert(m_kws.LoginStatus == KwsLoginStatus.LoggingIn);

            // We're now logged in.
            m_kws.LoginStatus = KwsLoginStatus.LoggedIn;

            // Update the catch up state.
            m_wm.KAnpState.LastProcessedEventDate = DateTime.Now;
            UpdateKAnpCatchUpState();

            // Request an update of the UI (WorkOnline mode now OK).
            m_kws.StateChangeUpdate(true);
            SetDeferredNotif(Base.GetKwsString() + " login");

            // Notify the listeners.
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.Login));
        }

        /// <summary>
        /// Called when the login fails for some reason.
        /// </summary>
        public void HandleLoginFailure()
        {
            Debug.Assert(m_kws.LoginStatus == KwsLoginStatus.LoggingIn);

            // Update our state.
            UpdateStateOnLogout();

            // The events are out of sync.
            if (m_kws.KasLoginHandler.LoginResult == KwsLoginResult.OOS)
            {
                // Request a rebuild. We have to delete both the cached events and
                // the user data. This is nasty.
                if (m_kws.MainStatus == KwsMainStatus.Good || m_kws.MainStatus == KwsMainStatus.RebuildRequired)
                {
                    m_kws.MainStatus = KwsMainStatus.RebuildRequired;
                    KwsRebuildInfo rebuildInfo = new KwsRebuildInfo();
                    rebuildInfo.DeleteCachedEventsFlag = true;
                    rebuildInfo.DeleteLocalDataFlag = true;
                    WorsenRebuild(rebuildInfo);
                }
            }

            // Notify the listeners.
            Exception ex = new Exception(m_kws.KasLoginHandler.LoginResultString);
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.Logout, ex));

            // Stop the workspace if required.
            RequestTaskSwitch(KwsTask.Stop);
        }

        /// <summary>
        /// Called when the workspace logs out normally.
        /// </summary>
        public void HandleNormalLogout()
        {
            Debug.Assert(m_kws.LoginStatus == KwsLoginStatus.LoggingOut);

            // Update our state.
            UpdateStateOnLogout();

            // Notify the listeners.
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.Logout, null));
        }

        /// <summary>
        /// Called when the workspace logs out normally, the login fails or
        /// the connection to the KAS is lost.
        /// </summary>
        private void UpdateStateOnLogout()
        {
            if (m_kws.LoginStatus == KwsLoginStatus.LoggedOut) return;

            // Update the login status and cancel all the pending queries.
            m_kws.LoginStatus = KwsLoginStatus.LoggedOut;
            m_kws.KasLoginHandler.ClearAllQueries();

            // We have not caught up with the KAS anymore.
            m_kws.KAnpState.CaughtUpFlag = false;

            // Cancel all the pending KAS queries that depend on the login 
            // state.
            m_kws.Kas.CancelKwsKasQuery(m_kws, true);

            // Request an update of the UI (now logged out).
            m_kws.StateChangeUpdate(true);
            SetDeferredNotif("logout");
        }

        /// <summary>
        /// Handle an ANP reply received from the KAS.
        /// </summary>
        public void HandleAnpReply(WmKasQuery query)
        {
            try
            {
                // Call the callback.
                query.Callback(query);
            }

            catch (Exception ex)
            {
                // The network handler throws if the KAS sends us garbage.
                if (query.App == null) BlameKas(ex);

                // Blame the application.
                else HandleAppFailure(query.App, ex);
            }
        }

        /// <summary>
        /// Handle an ANP event received from the KAS.
        /// </summary>
        public void HandleAnpEvent(AnpMsg msg)
        {
            Logging.Log("HandleAnpEvent() in kws " + m_kws.InternalID + ", status " + m_kws.MainStatus);

            // Logic problem detected.
            if (msg.ID < m_kws.KAnpState.LastReceivedEventId)
            {
                BlameKas(new Exception("received ANP event with bogus ID"));
                return;
            }

            // Store the event in the database. Mark it as unprocessed.
            m_kws.StoreEventInDb(msg, KwsAnpEventStatus.Unprocessed);

            // Update the information about the events.
            m_kws.KAnpState.NbUnprocessedEvent++;
            m_kws.KAnpState.LastReceivedEventId = msg.ID;
            m_kws.SetDirty();

            // If this is the only unprocessed event, dispatch it right away
            // if possible. This is done so that single incoming events are processed
            // very quickly instead of waiting for a future KWS state machine run.
            if (m_kws.KAnpState.NbUnprocessedEvent == 1) DispatchAnpEvent(msg);
        }


        /////////////////////////////////////////////
        // Interface methods for external parties. //
        /////////////////////////////////////////////

        /// <summary>
        /// Return the current task of the workspace.
        /// </summary>
        public KwsTask GetCurrentTask()
        {
            return m_currentTask;
        }

        /// <summary>
        /// Return the current run level of the workspace.
        /// </summary>
        public KwsRunLevel GetRunLevel()
        {
            if (m_kws.AppStatus != KwsAppStatus.Started ||
                (m_currentTask != KwsTask.WorkOffline && m_currentTask != KwsTask.WorkOnline))
                return KwsRunLevel.Stopped;

            if (m_currentTask == KwsTask.WorkOnline && m_kws.LoginStatus == KwsLoginStatus.LoggedIn)
                return KwsRunLevel.Online;

            return KwsRunLevel.Offline;
        }

        /// <summary>
        /// Return true if the workspace can be requested to stop.
        /// </summary>
        public bool CanStop()
        {
            return (m_currentTask != KwsTask.Delete && m_currentTask != KwsTask.Stop);
        }

        /// <summary>
        /// Return true if the workspace can be requested to be spawn.
        /// </summary>
        public bool CanSpawn()
        {
            return (m_kws.MainStatus == KwsMainStatus.NotYetSpawned && m_currentTask == KwsTask.Stop);
        }

        /// <summary>
        /// Return true if the workspace can be requested to be rebuilt.
        /// </summary>
        public bool CanRebuild()
        {
            return (m_kws.MainStatus != KwsMainStatus.NotYetSpawned &&
                    m_kws.MainStatus != KwsMainStatus.OnTheWayOut &&
                    m_currentTask != KwsTask.Rebuild);
        }

        /// <summary>
        /// Return true if the workspace can be requested to work offline.
        /// </summary>
        public bool CanWorkOffline()
        {
            return (m_kws.MainStatus == KwsMainStatus.Good && m_currentTask != KwsTask.WorkOffline);
        }

        /// <summary>
        /// Return true if the workspace can be requested to work online.
        /// </summary>
        public bool CanWorkOnline()
        {
            return (m_kws.MainStatus == KwsMainStatus.Good &&
                    (m_currentTask != KwsTask.WorkOnline ||
                     m_kws.Kas.ConnStatus == KasConnStatus.Disconnecting ||
                     m_kws.Kas.ConnStatus == KasConnStatus.Disconnected));
        }

        /// <summary>
        /// Return true if the workspace can be requested to be deleted.
        /// </summary>
        public bool CanDelete()
        {
            return (m_currentTask != KwsTask.Delete);
        }

        /// <summary>
        /// Return true if the workspace credentials can be exported.
        /// </summary>
        public bool CanExport()
        {
            return (m_kws.MainStatus == KwsMainStatus.Good || m_kws.MainStatus == KwsMainStatus.RebuildRequired);
        }

        /// <summary>
        /// Return true if KAnp events can be processed.
        /// </summary>
        public bool CanProcessKanpEvent()
        {
            return (m_kws.IsOnlineCapable() && !m_wm.KAnpState.QuenchFlag);
        }

        /// <summary>
        /// This method must be called by each application when it has started.
        /// </summary>
        public void OnAppStarted()
        {
            // The applications are no longer starting.
            if (m_kws.AppStatus != KwsAppStatus.Starting) return;

            // Not all applications have started.
            foreach (KwsApp app in m_kws.AppTree.Values)
                if (app.AppStatus != KwsAppStatus.Started) return;

            // All applications are started.
            m_kws.AppStatus = KwsAppStatus.Started;

            // Request an update of the UI (WorkOffline mode now OK).
            m_kws.StateChangeUpdate(true);
            SetDeferredNotif("applications started");
        }

        /// <summary>
        /// This method must be called by each application when it has stopped.
        /// </summary>
        public void OnAppStopped()
        {
            // The applications are no longer stopping.
            if (m_kws.AppStatus != KwsAppStatus.Stopping) return;

            // Not all applications have stopped.
            foreach (KwsApp app in m_kws.AppTree.Values)
                if (app.AppStatus != KwsAppStatus.Stopped) return;

            // All applications are stopped.
            m_kws.AppStatus = KwsAppStatus.Stopped;
            m_kws.ReleaseRef();
            
            SetDeferredNotif("applications stopped");
        }

        /// <summary>
        /// This method should be called when an application fails.
        /// </summary>
        public void HandleAppFailure(KwsApp app, Exception ex)
        {
            Logging.Log(2, "Application " + app + " failed: " + ex.Message);
            Logging.LogException(ex);

            // We cannot handle application failures during task switches:
            // we are stopping the applications.
            if (m_taskSwitchFlag) Base.HandleException(ex, true);

            // Set the application exception.
            m_kws.AppException = ex;

            // Increase the severity of the rebuild required if possible.
            if (m_currentTask == KwsTask.Rebuild)
            {
                KwsRebuildInfo rebuildInfo = new KwsRebuildInfo();
                rebuildInfo.DeleteCachedEventsFlag = true;
                rebuildInfo.DeleteLocalDataFlag = true;
                WorsenRebuild(rebuildInfo);
            }

            // Run the state machine to adjust the state.
            RequestRun("application failure");
            m_kws.StateChangeUpdate(false);

            // Notify the listeners.
            m_kws.FireKwsSmNotif(new KwsSmNotifEventArgs(KwsSmNotif.AppFailure, ex));

            // Stop the workspace if required.
            SetUserTask(KwsTask.Stop);
            RequestTaskSwitch(KwsTask.Stop);
        }

        /// <summary>
        /// Set the task the user would like to run when the WM starts. The
        /// current task is unaffected.
        /// </summary>
        public void SetUserTask(KwsTask userTask)
        {
            if ((userTask == KwsTask.Stop || userTask == KwsTask.WorkOffline || userTask == KwsTask.WorkOnline) &&
                m_kws.UserTask != userTask)
            {
                m_kws.UserTask = userTask;
                m_kws.SetDirty();
            }
        }

        /// <summary>
        /// Set the next spawn step, if possible.
        /// </summary>
        public void SetSpawnStep(KwsSpawnTaskStep step)
        {
            if (m_spawnStep >= step) return;
            m_spawnStep = step;
            ResetKasFailureState();
            RequestRun("set spawn step to " + step);
        }

        /// <summary>
        /// Set the login type. This method has no effect is the login process
        /// is under way.
        /// </summary>
        public void SetLoginType(KwsLoginType type)
        {
            if (m_kws.LoginStatus == KwsLoginStatus.LoggingIn) return;
            m_kws.KasLoginHandler.SetLoginType(type);
        }

        /// <summary>
        /// Increase the severity of the rebuild with the parameters provided. 
        /// </summary>
        public void WorsenRebuild(KwsRebuildInfo rebuildInfo)
        {
            if (rebuildInfo.DeleteCachedEventsFlag) m_kws.CoreData.RebuildInfo.DeleteCachedEventsFlag = true;
            if (rebuildInfo.DeleteLocalDataFlag) m_kws.CoreData.RebuildInfo.DeleteLocalDataFlag = true;
            m_kws.CoreData.RebuildInfo.UpgradeFlag = rebuildInfo.UpgradeFlag;
            m_kws.SetDirty();
        }

        /// <summary>
        /// Request a switch to the task specified, if possible.
        /// </summary>
        public void RequestTaskSwitch(KwsTask task)
        {
            try
            {
                // Validate.
                if (m_taskSwitchFlag ||
                    task == KwsTask.Stop && !CanStop() ||
                    task == KwsTask.Spawn && !CanSpawn() ||
                    task == KwsTask.Rebuild && !CanRebuild() ||
                    task == KwsTask.WorkOffline && !CanWorkOffline() ||
                    task == KwsTask.WorkOnline && !CanWorkOnline() ||
                    task == KwsTask.Delete && !CanDelete())
                {
                    Logging.Log("Request to switch to task " + task + " ignored.");
                    return;
                }

                Logging.Log("Switching to task " + task + ".");

                // Update some state prior to the task switch.
                if (task == KwsTask.Rebuild)
                {
                    m_kws.MainStatus = KwsMainStatus.RebuildRequired;
                    m_rebuildStep = KwsRebuildTaskStep.None;
                }

                else if (task == KwsTask.WorkOnline)
                {
                    ResetKasFailureState();
                }

                else if (task == KwsTask.Delete)
                {
                    m_kws.MainStatus = KwsMainStatus.OnTheWayOut;
                    m_kws.AddToKwsDeleteTree();
                    m_kws.SetDirty();
                }

                if (task == KwsTask.Spawn ||
                    task == KwsTask.Rebuild ||
                    task == KwsTask.WorkOffline ||
                    task == KwsTask.WorkOnline)
                {
                    m_kws.AppException = null;
                }

                // Perform the task switch, if required.
                if (task != m_currentTask) SwitchTask(task);

                // Update some state after the task switch.
                if (task == KwsTask.Spawn)
                {
                    m_kws.NormalizeAppState();
                }
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }
    }
}
