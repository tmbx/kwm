using System;
using System.Diagnostics;
using kwm.KwmAppControls;
using System.Collections.Generic;
using kwm.Utils;
using Tbx.Utils;
using kwm.KwmAppControls.AppKfs;
using System.IO;

namespace kwm
{
    /// <summary>
    /// Status of a request received from Outlook.
    /// </summary>
    public enum WmOutlookRequestStatus
    {
        /// <summary>
        /// The request is executing.
        /// </summary>
        Executing,

        /// <summary>
        /// The request is finished.
        /// </summary>
        Finished,

        /// <summary>
        /// The request is aborted because an error has occurred with Outlook. 
        /// A call to SendReply() will set the status to 'Finished'.
        /// </summary>
        Aborted
    }

    /// <summary>
    /// This class represents a request received from Outlook.
    /// </summary>
    public class WmOutlookRequest
    {
        /// <summary>
        /// Broker associated to this request.
        /// </summary>
        private WmOutlookBroker m_broker;

        /// <summary>
        /// Status of the request.
        /// </summary>
        public WmOutlookRequestStatus Status = WmOutlookRequestStatus.Executing;

        /// <summary>
        /// ANP command of this request.
        /// </summary>
        public AnpMsg Cmd = null;

        /// <summary>
        /// Delegate called when HandleOutlookFailure() is called.
        /// </summary>
        public Base.ExceptionDelegate OnOutlookFailure;

        /// <summary>
        /// Message ID associated to this request.
        /// </summary>
        public UInt64 MsgID { get { return Cmd.ID; } }

        public WmOutlookRequest(WmOutlookBroker broker, AnpMsg cmd)
        {
            m_broker = broker;
            Cmd = cmd;
        }

        /// <summary>
        /// Create a reply of the type specified. 
        /// </summary>
        public AnpMsg MakeReply(UInt32 type)
        {
            return WmOutlookBroker.MakeOanpMsg(Cmd.ID, type);
        }

        /// <summary>
        /// Return the result specified to Outlook and mark the request as
        /// finished. Nothing is done if the request is already finished.
        /// </summary>
        public void SendReply(AnpMsg res)
        {
            if (res.Type == 671154176)
            {
                Debug.Assert(false, "Unexpected reply being sent to Outlook.");
                Logging.LogException(new Exception("Unexpected reply being sent to Outlook."));
            }
            m_broker.SendReply(this, res);
        }

        /// <summary>
        /// Send back a generic failure reply to Outlook having the reason 
        /// specified.
        /// </summary> 
        public void SendFailure(String error)
        {
            Logging.Log(2, "Sending failure to Outlook: " + error);
            SendReply(WmOutlookBroker.MakeOanpFailureMsg(Cmd.ID, error));
        }

        /// <summary>
        /// Report a failure related to Outlook.
        /// </summary>
        public void HandleOutlookFailure(Exception ex)
        {
            try
            {
                if (OnOutlookFailure != null) OnOutlookFailure(ex);
            }

            catch (Exception ex2)
            {
                Base.HandleException(ex2, true);
            }
        }
    }

    /// <summary>
    /// Workspace information used by Outlook.
    /// </summary>
    public class OutlookKws
    {
        public UInt64 InternalID;
        public UInt64 ExternalID;
        public String KcdAddress;
        public String KwmoAddress;
        public String KwsName;
        public String FolderPath;
        public bool SecureFlag;
        public bool InviteFlag;
        public bool ConnectedFlag;
        public bool PublicFlag;
        public UInt64 CreationDate;
    }

    /// <summary>
    /// The Outlook broker manages the interactions with Outlook.
    /// </summary>
    public class WmOutlookBroker
    {
        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        private WorkspaceManager m_wm = null;

        /// <summary>
        /// Fired when the Outlook thread has been collected.
        /// </summary>
        public event EventHandler<EventArgs> OnThreadCollected;

        /// <summary>
        /// Tree mapping message IDs to Outlook requests.
        /// </summary>
        public SortedDictionary<UInt64, WmOutlookRequest> RequestTree = new SortedDictionary<UInt64, WmOutlookRequest>();

        /// <summary>
        /// List of workspaces that was last sent to Outlook.
        /// </summary>
        public List<OutlookKws> LastKwsList = new List<OutlookKws>();

        /// <summary>
        /// Set to true when the KWM settings have been written to the registry,
        /// This will eventually trigger the method SendNewKemStateInfos so that 
        /// Outlook gets notified of interesting changed, such as the CreationPower 
        /// flag.
        /// </summary>
        private bool m_kwmRegistryChanged = false;

        /// <summary>
        /// Time at which the KWM state must be resent to Outlook. This is
        /// MaxValue if the state needs not be resent. 
        /// </summary>
        public DateTime KwmStateDate = DateTime.MaxValue;

        /// <summary>
        /// True if the Outlook broker is enabled.
        /// </summary>
        private bool m_enabledFlag = false;

        /// <summary>
        /// A unique ID is associated to the Outlook session when it is 
        /// established. This ensures that the ANP messages sent to Outlook by
        /// the broker are dispatched correctly even if Outlook connects and
        /// disconnects quickly.
        /// </summary>
        private UInt64 m_sessionID = 0;

        /// <summary>
        /// ID of the next event to send to Outlook.
        /// </summary>
        private UInt32 m_nextEventID = 1;

        /// <summary>
        /// Status of the Outlook session.
        /// </summary>
        private OutlookSessionStatus m_sessionStatus = OutlookSessionStatus.Stopped;

        /// <summary>
        /// Reference to the Outlook thread.
        /// </summary>
        private WmOutlookThread m_thread = null;

        /// <summary>
        /// Create an OANP message having the ID and type specified.
        /// </summary>
        public static AnpMsg MakeOanpMsg(UInt64 ID, UInt32 type)
        {
            AnpMsg msg = new AnpMsg();
            msg.ID = ID;
            msg.Major = OAnp.Major;
            msg.Minor = OAnp.Minor;
            msg.Type = type;
            return msg;
        }

        /// <summary>
        /// Create an OANP failure message having the ID and error string specified.
        /// </summary>
        public static AnpMsg MakeOanpFailureMsg(UInt64 ID, String error)
        {
#if DEBUG
            Logging.Log("MakeOanpFailureMsg: " + error);
#endif
            AnpMsg res = MakeOanpMsg(ID, OAnpType.OANP_RES_FAIL);
            res.AddUInt32(OAnpType.OANP_RES_FAIL_GEN_ERROR);
            res.AddString(error);
            return res;
        }

        public WmOutlookBroker(WorkspaceManager wm)
        {
            m_wm = wm;
            WmWinRegistry.OnRegistryWritten += new EventHandler(HandleOnRegistryWritten);
        }

        /// <summary>
        /// Return true if the broker is enabled.
        /// </summary>
        public bool IsEnabled()
        {
            return m_enabledFlag;
        }

        /// <summary>
        /// Return true if the session is open.
        /// </summary>
        public bool IsSessionOpen()
        {
            return (m_sessionStatus == OutlookSessionStatus.Open);
        }

        /// <summary>
        /// Return true if the worker thread is ready to receive messages.
        /// </summary>
        public bool IsThreadReady()
        {
            return (m_sessionStatus != OutlookSessionStatus.Stopped && m_sessionStatus != OutlookSessionStatus.Stopping);
        }

        /// <summary>
        /// Enable the broker.
        /// </summary>
        public void Enable()
        {
            if (m_enabledFlag) return;
            m_enabledFlag = true;
            StartThreadIfNeeded();
        }

        /// <summary>
        /// Disable the broker.
        /// </summary>
        public void Disable()
        {
            if (!m_enabledFlag) return;
            m_enabledFlag = false;
            StopThreadIfNeeded();
            AbortPendingRequests(new Exception("closing Outlook connection"));
        }

        /// <summary>
        /// This method is called to stop the broker. It returns true when the
        /// broker is stopped.
        /// </summary>
        public bool TryStop()
        {
            Disable();
            return (m_sessionStatus == OutlookSessionStatus.Stopped);
        }

        /// <summary>
        /// Called by WmOutlookRequest.SendReply().
        /// </summary>
        public void SendReply(WmOutlookRequest request, AnpMsg msg)
        {
            if (request.Status == WmOutlookRequestStatus.Finished) return;

            if (request.Status == WmOutlookRequestStatus.Executing)
            {
                Debug.Assert(RequestTree.ContainsKey(request.MsgID));
                Debug.Assert(m_sessionStatus == OutlookSessionStatus.Open);
                RequestTree.Remove(request.MsgID);
                SendAnpMsgToThread(msg);
            }

            request.Status = WmOutlookRequestStatus.Finished;
        }

        /// <summary>
        /// Create a OANP event having the type specified.
        /// </summary>
        public AnpMsg MakeEvent(UInt32 type)
        {
            return MakeOanpMsg(m_nextEventID++, type);
        }

        /// <summary>
        /// Send an OANP event to Outlook, if possible.
        /// </summary>
        public void SendEvent(AnpMsg evt)
        {
            if (!IsSessionOpen()) return;
            SendAnpMsgToThread(evt);
        }

        /// <summary>
        /// Return the current list of Outlook workspaces.
        /// </summary>
        public List<OutlookKws> GetKwsList()
        {
            List<OutlookKws> outlookList = new List<OutlookKws>();

            List<KwsBrowserFolderNode> folderList = new List<KwsBrowserFolderNode>();
            List<KwsBrowserKwsNode> kwsList = new List<KwsBrowserKwsNode>();
            m_wm.UiBroker.Browser.RecursiveList(true, m_wm.UiBroker.Browser.RootNode, folderList, kwsList);

            foreach (KwsBrowserKwsNode node in kwsList)
            {
                Workspace kws = node.Kws;
                OutlookKws o = new OutlookKws();
                outlookList.Add(o);
                o.InternalID = kws.InternalID;
                o.ExternalID = kws.CoreData.Credentials.ExternalID;
                o.KcdAddress = kws.CoreData.Credentials.KasID.Host;
                o.KwmoAddress = kws.CoreData.Credentials.KwmoAddress;
                o.KwsName = kws.CoreData.Credentials.KwsName;
                o.FolderPath = node.Parent.FullPath;
                o.SecureFlag = kws.CoreData.Credentials.SecureFlag;
                o.InviteFlag = kws.CoreData.Credentials.AdminFlag;
                o.ConnectedFlag = kws.GetRunLevel() == KwsRunLevel.Online;
                o.PublicFlag = kws.IsPublicKws();
                if (kws.CoreData.UserInfo.Creator == null) o.CreationDate = UInt64.MaxValue;
                else o.CreationDate = kws.CoreData.UserInfo.Creator.InvitationDate;
            }

            return outlookList;
        }

        /// <summary>
        /// Return true if the workspace lists specified are equal.
        /// </summary>
        public bool AreKwsListEqual(List<OutlookKws> l1, List<OutlookKws> l2)
        {
            if (l1.Count != l2.Count) return false;
            
            for (int i = 0; i < l1.Count; i++)
            {
                OutlookKws k1 = l1[i];
                OutlookKws k2 = l2[i];
                if (k1.InternalID != k2.InternalID ||
                    k1.ExternalID != k2.ExternalID ||
                    k1.KcdAddress != k2.KcdAddress ||
                    k1.KwmoAddress != k2.KwmoAddress ||
                    k1.KwsName != k2.KwsName ||
                    k1.FolderPath != k2.FolderPath ||
                    k1.InviteFlag != k2.InviteFlag ||
                    k1.SecureFlag != k2.SecureFlag ||
                    k1.ConnectedFlag != k2.ConnectedFlag ||
                    k1.PublicFlag != k2.PublicFlag ||
                    k1.CreationDate != k2.CreationDate)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called when the workspace list has been modified.
        /// </summary>
        public void OnKwsListChanged()
        {
            TriggerWmSmRun("Outlook workspace list changed");
        }

        /// <summary>
        /// Called when the KWM registry settings have been modified.
        /// </summary>
        public void HandleOnRegistryWritten(object sender, EventArgs e)
        {
            m_kwmRegistryChanged = true;

            TriggerWmSmRun("KWM registry state changed, must notify Outlook");
        }

        /// <summary>
        /// Triggers the workspace manager state machine to run.
        /// </summary>
        private void TriggerWmSmRun(String reason)
        {
            // No action required.
            if (!IsSessionOpen() || KwmStateDate != DateTime.MaxValue) return;

            // Set the date at which the workspace list will be resent.
            // Quench for a while so we do not flood Outlook if multiple 
            // modifications were quickly done.
            KwmStateDate = DateTime.Now.AddSeconds(1);

            // Notify the WM state machine.
            m_wm.Sm.ScheduleRun(reason, KwmStateDate);
        }

        /// <summary>
        /// Send the workspace list to Outlook if required. This is called by
        /// the workspace manager state machine. When the KWM state changes and
        /// Outlook must be notified, you must call TriggerWmSmRun() to make sure
        /// the SM runs and calls this method.
        /// </summary>
        public void SendKwsListIfNeeded()
        {
            // No action required.
            if (!IsSessionOpen() || KwmStateDate == DateTime.MaxValue) return;
            
            // It's not yet time to send it. Schedule a run.
            if (KwmStateDate > DateTime.Now)
            {
                m_wm.Sm.ScheduleRun("Outlook workspace list changed", KwmStateDate);
                return;
            }

            // Reset the workspace date.
            KwmStateDate = DateTime.MaxValue;

            // Get the new workspace list.
            List<OutlookKws> newList = GetKwsList();

            // Do not send new state if the list is the same as the last one and
            // the registry settings did not change.
            if (AreKwsListEqual(newList, LastKwsList) && !m_kwmRegistryChanged) return;

            // Send the new workspace list. The OTC registry settings have not changed.
            SendNewKwmStateInfos(newList, false);
        }


        /// <summary>
        /// Send the entire KWM current state to Outlook and notify that 
        /// the settings changed.
        /// </summary>
        public void SendNewKwmStateInfos()
        {
            SendNewKwmStateInfos(GetKwsList(), true);
        }

        /// <summary>
        ///  Send the entire KWM state to Outlook, with the specified
        ///  workspace list.
        /// </summary>
        public void SendNewKwmStateInfos(List<OutlookKws> newList, bool settingsChanged)
        {
            Logging.Log("SendNewKwmStateInfos() called.");
            Debug.Assert(IsSessionOpen());

            // Remember the last workspace list we sent.
            LastKwsList = newList;

            // Remember that we sent the latest registry changes.
            m_kwmRegistryChanged = false;

            // Send the event.
            AnpMsg evt = MakeEvent(OAnpType.OANP_EVT_NEW_KWM_STATE);
            evt.AddUInt32(Convert.ToUInt32(WmWinRegistry.Spawn().CanCreateKws()));
            
            evt.AddUInt32(settingsChanged ? (UInt32)1 : 0);
            evt.AddUInt32((UInt32)newList.Count);
            foreach (OutlookKws kws in newList)
            {
                evt.AddUInt64(kws.InternalID);
                evt.AddUInt64(kws.ExternalID);
                evt.AddString(kws.KcdAddress);
                evt.AddString(kws.KwmoAddress);
                evt.AddString(kws.KwsName);
                evt.AddString(kws.FolderPath);
                evt.AddUInt32(Convert.ToUInt32(kws.SecureFlag));
                evt.AddUInt32(Convert.ToUInt32(kws.InviteFlag));
                evt.AddUInt32(Convert.ToUInt32(kws.ConnectedFlag));
                evt.AddUInt32(Convert.ToUInt32(kws.PublicFlag));
                evt.AddUInt64(kws.CreationDate);
            }
            SendEvent(evt);
        }

        /// <summary>
        /// Handle a message received from the Outlook thread.
        /// </summary>
        public void HandleThreadMsg(OutlookUiThreadMsg msg)
        {
            Logging.Log("HandleThreadMsg() called.");
            // The thread is stopped or stopping, ignore the message.
            if (!IsThreadReady()) return;

            // Dispatch.
            try
            {
                if (msg.Type == OutlookBrokerMsgType.Connected) HandleConnectedToOutlook(msg.SessionID);
                else if (msg.Type == OutlookBrokerMsgType.Disconnected) HandleDisconnectedFromOutlook(msg.Ex);
                else if (msg.Type == OutlookBrokerMsgType.ReceivedMsg) HandleOutlookMessage(msg.Msg);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Send an ANP message to the Outlook thread.
        /// </summary>
        private void SendAnpMsgToThread(AnpMsg msg)
        {
            Debug.Assert(IsSessionOpen());
            m_thread.PostToWorker(new WmOutlookThreadMsg(m_thread, m_sessionID, msg));
        }

        /// <summary>
        /// Start the Outlook thread if required.
        /// </summary>
        private void StartThreadIfNeeded()
        {
            if (!m_enabledFlag || m_sessionStatus != OutlookSessionStatus.Stopped) return;
            m_sessionStatus = OutlookSessionStatus.Listening;
            Debug.Assert(m_thread == null);
            m_thread = new WmOutlookThread(this);
            m_thread.Start();
        }

        /// <summary>
        /// Stop the Outlook thread if required.
        /// </summary>
        private void StopThreadIfNeeded()
        {
            if (!IsThreadReady()) return;
            m_sessionStatus = OutlookSessionStatus.Stopping;
            Debug.Assert(m_thread != null);
            m_thread.RequestCancellation();
        }

        /// <summary>
        /// Called when the Outlook thread has completed. 'ex' is null if the 
        /// thread has been cancelled, otherwise it contains the reason why the
        /// thread has stopped.
        /// </summary>
        public void OnThreadCompletion(Exception ex)
        {
            Debug.Assert(m_thread != null);

            // The Outlook thread is not allowed to fail.
            if (ex != null) Base.HandleException(ex, true);

            // The thread has stopped.
            Debug.Assert(m_sessionStatus == OutlookSessionStatus.Stopping);
            m_sessionStatus = OutlookSessionStatus.Stopped;
            m_thread = null;

            // Notify the listeners.
            if (OnThreadCollected != null) OnThreadCollected(this, null);

            // Restart the thread if the broker is enabled.
            StartThreadIfNeeded();
        }

        /// <summary>
        /// Called when Outlook is connected.
        /// </summary>
        private void HandleConnectedToOutlook(UInt64 sessionID)
        {
            Logging.Log("HandleConnectedToOutlook(" + sessionID + ") called.");
            Debug.Assert(m_sessionStatus == OutlookSessionStatus.Listening);
            m_sessionStatus = OutlookSessionStatus.Open;
            m_sessionID = sessionID;
            KwmStateDate = DateTime.MaxValue;
            SendNewKwmStateInfos();
        }

        /// <summary>
        /// Called when Outlook is disconnected.
        /// </summary>
        private void HandleDisconnectedFromOutlook(Exception ex)
        {
            m_sessionStatus = OutlookSessionStatus.Listening;
            AbortPendingRequests(ex);
        }

        /// <summary>
        /// Abort all pending requests.
        /// </summary>
        private void AbortPendingRequests(Exception ex)
        {
            SortedDictionary<UInt64, WmOutlookRequest> tree = new SortedDictionary<UInt64, WmOutlookRequest>(RequestTree);
            RequestTree.Clear();

            foreach (WmOutlookRequest request in tree.Values)
            {
                Debug.Assert(request.Status == WmOutlookRequestStatus.Executing);
                request.Status = WmOutlookRequestStatus.Aborted;
            }

            foreach (WmOutlookRequest request in tree.Values)
            {
                if (request.Status == WmOutlookRequestStatus.Aborted)
                    request.HandleOutlookFailure(ex);
            }
        }

        /// <summary>
        /// Handle an ANP message received from Outlook.
        /// </summary>
        private void HandleOutlookMessage(AnpMsg msg)
        {
            if (msg.Type == OAnpType.OANP_CMD_CANCEL_CMD) HandleOutlookCancellationCommand(msg);
            else HandleOutlookRequest(msg);
        }

        /// <summary>
        /// Handle a cancellation command received from Outlook.
        /// </summary>
        private void HandleOutlookCancellationCommand(AnpMsg msg)
        {
            try
            {
                UInt64 ID = msg.Elements[0].UInt64;
                if (!RequestTree.ContainsKey(ID)) return;
                RequestTree[ID].HandleOutlookFailure(new Exception("Outlook request cancelled"));
            }

            catch (Exception ex)
            {
                Logging.Log(2, "Cannot parse Outlook cancellation message: " + ex.Message);
            }
        }

        /// <summary>
        /// Handle a request received from Outlook.
        /// </summary>
        private void HandleOutlookRequest(AnpMsg msg)
        {
            Logging.Log(1, "HandleOutlookRequest() called.");

            if (RequestTree.ContainsKey(msg.ID))
            {
                Logging.Log(2, "Received duplicate Outlook request ID.");
                return;
            }

            // Register the request.
            WmOutlookRequest request = new WmOutlookRequest(this, msg);
            RequestTree[request.MsgID] = request;

            try
            {
                // Validate version.
                if (msg.Major != OAnp.Major || msg.Minor != OAnp.Minor)
                    throw new Exception("Unsupported OANP version " + msg.Major + "." + msg.Minor + ".");

                // Dispatch.
                UInt32 type = msg.Type;
                if (type == OAnpType.OANP_CMD_IS_KWS_USER) HandleIsKwsUser(request);
                else if (type == OAnpType.OANP_CMD_OPEN_KWS) HandleOpenKws(request);
                else if (type == OAnpType.OANP_CMD_JOIN_KWS) HandleJoinKws(request);
                else if (type == OAnpType.OANP_CMD_CREATE_KWS) HandleCreateKws(request);
                else if (type == OAnpType.OANP_CMD_INVITE_TO_KWS) HandleInviteToKws(request);
                else if (type == OAnpType.OANP_CMD_GET_SKURL) HandleGetSkurl(request);
                else if (type == OAnpType.OANP_CMD_LOOKUP_REC_ADDR) HandleLookupRecAddr(request);
                else if (type == OAnpType.OANP_CMD_WORKSPACE_SUBSCRIBE) HandleWorkspaceSubscribe(request);
                else if (type == OAnpType.OANP_CMD_START_SCREEN_SHARE) HandleStartScreenShare(request);
                else throw new Exception("unknown command received");
            }

            catch (Exception ex)
            {
                request.SendFailure(ex.Message);
            }
        }

        private void HandleIsKwsUser(WmOutlookRequest request)
        {
            UInt64 kwsID = request.Cmd.Elements[0].UInt64;
            UInt32 nbUser = request.Cmd.Elements[1].UInt32;
            List<String> addrList = new List<String>();
            for (UInt32 i = 0; i < nbUser; i++) addrList.Add(request.Cmd.Elements[(int)i + 2].String);
            
            AnpMsg res = request.MakeReply(OAnpType.OANP_RES_IS_KWS_USER);
            res.AddUInt32(nbUser);
            Workspace kws = m_wm.GetKwsByInternalID(kwsID);

            foreach (String addr in addrList)
            {
                if (kws == null) res.AddUInt32(0);
                else res.AddUInt32(Convert.ToUInt32(kws.CoreData.UserInfo.GetUserByEmailAddress(addr) != null));
            }

            request.SendReply(res);
        }

        private void HandleOpenKws(WmOutlookRequest request)
        {
            UInt64 kwsID = request.Cmd.Elements[0].UInt64;
            Workspace kws = m_wm.GetKwsByInternalID(kwsID);
            if (kws == null || kws.GetRunLevel() < KwsRunLevel.Offline)
                throw new Exception("the " + Base.GetKwsString() + " cannot be displayed at this time");
            request.SendReply(request.MakeReply(OAnpType.OANP_RES_OK));

            m_wm.UiBroker.RequestShowMainForm();
            m_wm.UiBroker.RequestSelectKws(kws, true);
        }

        private void HandleJoinKws(WmOutlookRequest request)
        {
            (new OutlookJoinKwsOp(m_wm, request)).StartOp();
        }

        private void HandleCreateKws(WmOutlookRequest request)
        {
            (new OutlookCreateKwsOp(m_wm, request)).StartOp();
        }

        private void HandleInviteToKws(WmOutlookRequest request)
        {
            UInt64 kwsID = request.Cmd.Elements[0].UInt64;
            Workspace kws = m_wm.GetKwsByInternalID(kwsID);
            if (kws == null || kws.GetRunLevel() != KwsRunLevel.Online)
                throw new Exception("the " + Base.GetKwsString() + " is not available at this time");
            (new OutlookInviteOp(m_wm, kws, request)).StartOp();
        }

        private void HandleGetSkurl(WmOutlookRequest request)
        {
            (new OutlookSkurlKwsOp(m_wm, request)).StartOp();
        }

        private void HandleLookupRecAddr(WmOutlookRequest request)
        {
            (new OutlookLookupRecAddrOp(m_wm, request)).StartOp();
        }

        private void HandleWorkspaceSubscribe(WmOutlookRequest request)
        {
            (new OutlookWorkspaceSubscribeOp(m_wm, request)).StartOp();
        }

        private void HandleStartScreenShare(WmOutlookRequest request)
        {
            (new OutlookStartScreenShareOp(m_wm, request)).StartOp();
        }
    }
}
