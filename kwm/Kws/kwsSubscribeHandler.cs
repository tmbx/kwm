using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Tbx.Utils;
using kwm.KwmAppControls;

namespace kwm
{
    /// <summary>
    /// Outlook subcribe to workspace operation.
    /// </summary>
    public class OutlookWorkspaceSubscribeOp : KwsCoreOp
    {
        /// <summary>
        /// Is this a request to subscribe or a request to unsubscribe ?
        /// </summary>
        private bool m_isSubscription = false;

        /// <summary>
        /// The last event ID that the requester received.
        /// </summary>
        private UInt64 m_lastEventID;
        
        public OutlookWorkspaceSubscribeOp(WorkspaceManager wm, WmOutlookRequest request)
            : base(wm)
        {
            RegisterOutlookRequest(request);
        }

        bool ParseRequest(WmOutlookRequest request)
        {
            bool success = false;
            try
            {
                m_kws = Wm.GetKwsByInternalID(request.Cmd.Elements[0].UInt64);
                m_isSubscription = (request.Cmd.Elements[1].UInt32 != 0);
                m_lastEventID = request.Cmd.Elements[2].UInt64;
                success = true;
            }
            catch (Exception ex)
            {
                HandleMiscFailure(ex);
            }
            return success;
        }

        public override void HandleMiscFailure(Exception ex)
        {
            //UnregisterFromKws(true);
            m_outlookRequest.SendFailure(ex.Message);
        }

        /// <summary>
        /// Start the operation.
        /// </summary>
        public void StartOp()
        {
            if (!ParseRequest(m_outlookRequest))
                return;
            if (m_isSubscription)
            {
                if (m_kws.EventProxy == null)
                {
                    OutlookEventProxy proxy = new OutlookEventProxy(Wm, m_kws);
                    //Events should be fetched in another thread. Then OK should be sent.
                    AnpMsg[] MissedEvents = m_kws.GetNewEvents(m_lastEventID);
                    proxy.BeginEventPush();
                    try
                    {
                        foreach (AnpMsg msg in MissedEvents)
                        {
                            proxy.EventReceivedHandler(this, msg);
                        }
                    }
                    finally
                    {
                        proxy.EndEventPush();
                    }

                }
            }
            else
            {
                m_kws.OnEventReceived -= m_kws.EventProxy.EventReceivedHandler;
            }

            AnpMsg res = m_outlookRequest.MakeReply(OAnpType.OANP_RES_OK);
            m_outlookRequest.SendReply(res);

            // We're done.
            //UnregisterFromKws(true);
            m_doneFlag = true;
        }
    }

    public class OutlookEventProxy
    {
        private WorkspaceManager m_Wm;
        private Workspace m_Kws;
        private bool BatchPush = false;
        private bool UserStatusSent = false;

        public OutlookEventProxy(WorkspaceManager wm, Workspace kws)
        {
            m_Wm = wm;
            m_Kws = kws;
            Debug.Assert(m_Kws.EventProxy == null);
            m_Kws.EventProxy = this;
            m_Kws.OnEventReceived += EventReceivedHandler;
        }

        public void Dispose()
        {
            m_Kws.OnEventReceived -= EventReceivedHandler;
            m_Kws.EventProxy = null;
        }

        public void BeginEventPush()
        {
            BatchPush = true;
            UserStatusSent = false;
        }

        public void EndEventPush()
        {
            BatchPush = false;
            UserStatusSent = true;
        }

        /// <summary>
        /// Handle a received event, send it to Outlook
        /// </summary>
        /// <param name="msg"></param>
        public void EventReceivedHandler(object sender, AnpMsg msg)
        {
            switch (msg.Type)
            {
                case KAnpType.KANP_EVT_KWS_INVITED:
                case KAnpType.KANP_EVT_KWS_USER_REGISTERED:
                    if (!BatchPush || !UserStatusSent)
                        SendUserUpdateEvent(m_Kws, msg);
                    break;
            }
        }

        private void SendUserUpdateEvent(Workspace kws, AnpMsg msg)
        {
            AnpMsg evt = m_Wm.OutlookBroker.MakeEvent(OAnpType.OANP_EVT_USER_UPDATE);
            evt.AddUInt64(kws.InternalID);
            evt.AddUInt64(msg.Elements[1].UInt64);
            evt.AddUInt32((UInt32)kws.CoreData.UserInfo.UserTree.Count);
            foreach (KwsUser user in kws.CoreData.UserInfo.UserTree.Values)
            {
                evt.AddUInt32(user.UserID);
                evt.AddString(user.AdminName);
                evt.AddString(user.UserName);
                evt.AddString(user.EmailAddress);
                evt.AddUInt32(user.Power);
                evt.AddString(user.OrgName);
            }
            m_Wm.OutlookBroker.SendEvent(evt);
        }
    }
}
