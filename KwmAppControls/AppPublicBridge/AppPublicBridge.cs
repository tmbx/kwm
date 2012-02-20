using System;
using kwm.Utils;
using System.Windows.Forms;
using System.Diagnostics;
using Tbx.Utils;

// Public workspace functionalities.

namespace kwm.KwmAppControls
{
    /// <summary>
    /// This requests asks the user if he wants to accept a public chat request.
    /// </summary>
    public class PublicChatGer : GuiExecRequest
    {
        private AppPublicBridge m_app;
        private IAppHelper m_helper;
        private UInt64 m_reqID;
        private UInt32 m_userID;
        private UInt32 m_timeout;
        private String m_userName;
        private String m_subject;

        public PublicChatGer(AppPublicBridge app, UInt64 reqID, UInt32 userID,
                             UInt32 timeout, String userName, String subject)
        {
            m_app = app;
            m_helper = app.Helper;
            m_reqID = reqID;
            m_userID = userID;
            m_timeout = timeout;
            m_userName = userName;
            m_subject = subject;
        }

        public override void Run()
        {
            KMsgBox msgbox = new KMsgBox(m_userName + " would like to chat with you concerning " + m_subject + ".\n",
                                         "My Public Teambox",
                                         KMsgBoxButton.OKCancel,
                                         MessageBoxIcon.Question,
                                         (int)m_timeout);
            if (msgbox.Show() == KMsgBoxResult.OK)
            {
                AnpMsg cmd = m_helper.NewKAnpCmd(KAnpType.KANP_CMD_PB_ACCEPT_CHAT);
                cmd.AddUInt64(m_reqID);
                cmd.AddUInt32(m_userID);
                cmd.AddUInt32(m_userID);
                m_app.PostKasQuery(cmd, null, HandleAcceptChatReply);
            }
        }

        private void HandleAcceptChatReply(KasQuery ctx)
        {
            UInt32 type = ctx.Res.Type;

            if (type == KAnpType.KANP_RES_OK)
            {
                UInt32 userID = ctx.Cmd.Elements[3].UInt32;
                Logging.Log("Chat with " + m_helper.GetUserDisplayName(userID) + " accepted.");
                m_helper.ActivateKws();
                m_helper.SelectUser(userID);
            }

            else Logging.Log(2, Misc.HandleUnexpectedKAnpReply("accept public chat", ctx.Res).Message);
        }
    }

    /// <summary>
    /// This requests asks the user if he wants to create a workspace with his
    /// contact.
    /// </summary>
    public class PublicCreateKwsGer : GuiExecRequest
    {
        private String m_userName;
        private String m_userEmail;
        private String m_subject;

        public PublicCreateKwsGer(String userName, String userEmail, String subject)
        {
            m_userName = userName;
            m_userEmail = userEmail;
            m_subject = subject;
        }

        public override void Run()
        {
            KMsgBox msgbox = new KMsgBox(m_userName + " would like you to create a new " + Base.GetKwsString() + " concerning " +
                                           m_subject + ".\nTo create it, go to your " + Base.GetKwmString() + " and click on the New Teambox button.",
                                         "My Public Teambox",
                                         KMsgBoxButton.OK,
                                         MessageBoxIcon.Information);
            msgbox.Show();
        }
    }

    [Serializable]
    public sealed class AppPublicBridge : KwsApp
    {
        public override UInt32 AppID { get { return KAnpType.KANP_NS_PB; } }

        public AppPublicBridge(IAppHelper _helper) : base(_helper) { }

        public override KwsAnpEventStatus HandleAnpEvent(AnpMsg msg)
        {
            if (msg.Type == KAnpType.KANP_EVT_PB_TRIGGER_CHAT) return HandleTriggerChatEvent(msg);
            else if (msg.Type == KAnpType.KANP_EVT_PB_TRIGGER_KWS) return HandleTriggerKwsEvent(msg);
            // We don't care about this event.
            else if (msg.Type == KAnpType.KANP_EVT_PB_CHAT_ACCEPTED) return KwsAnpEventStatus.Processed;
            else return KwsAnpEventStatus.Unprocessed;
        }

        private KwsAnpEventStatus HandleTriggerChatEvent(AnpMsg msg)
        {
            if (NotifiedCaughtUpFlag)
            {
                UInt64 reqID = msg.Minor <= 2 ? msg.Elements[2].UInt32 : msg.Elements[2].UInt64;
                UInt32 userID = msg.Elements[3].UInt32;
                String subject = msg.Elements[4].String;
                UInt32 timeout = msg.Elements[5].UInt32;
                String userName = Helper.GetUserDisplayName(userID);
                PublicChatGer req = new PublicChatGer(this, reqID, userID, timeout, userName, subject);
                Helper.PostGuiExecRequest(req);
            }

            return KwsAnpEventStatus.Processed;
        }

        private KwsAnpEventStatus HandleTriggerKwsEvent(AnpMsg msg)
        {
            if (NotifiedCaughtUpFlag)
            {
                UInt32 userID = msg.Elements[3].UInt32;
                String subject = msg.Elements[4].String;
                String userName = Helper.GetUserDisplayName(userID);
                String userEmail = Helper.GetUserEmail(userID);
                PublicCreateKwsGer req = new PublicCreateKwsGer(userName, userEmail, subject);
                Helper.PostGuiExecRequest(req);
            }

            return KwsAnpEventStatus.Processed;
        }
    }
}