using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.IO;
using System.Timers;
using kwm.Utils;
using System.Collections;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    /// <summary>
    /// Backend class for the chat box application.
    /// </summary>
    [Serializable]
    public sealed class AppChatBox : KwsApp
    {
        /// <summary>
        /// Time to wait before sending a new popup notification after
        /// the last one was sent, in milliseconds.
        /// </summary>
        public const int DeltaBetweenPopup = 0 * 60 * 1000;

        /// <summary>
        /// Fired when a new chat message is received.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnIncomingChatMsg;

        /// <summary>
        /// Fired when an outgoing chat message is acknowledged by the server.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnSentChatMsgOK;

        /// <summary>
        /// Fired when an outgoing chat message is refused by the server.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnSentChatMsgFailed;

        /// <summary>
        /// Fired when the selected workspace user is changed.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnAppChatUserChanged;

        /// <summary>
        /// Tree mapping chat IDs to chat content.
        /// </summary>
        private Dictionary<UInt32, String> m_chatWindowsContent = new Dictionary<UInt32, String>();

        /// <summary>
        /// Last time an event was notified without hiding the popup notification.
        /// </summary>
        [NonSerialized]
        private DateTime m_lastEventPopup;

        public override UInt32 AppID { get { return KAnpType.KANP_NS_CHAT; } }

        public AppChatboxControl.AppChatBoxSettings AppSettings
        {
            get
            {
                return (AppChatboxControl.AppChatBoxSettings)Settings;
            }
        }

        /// <summary>
        /// Return the entire content of the given chat ID.
        /// </summary>
        public string ChatWindowContent(UInt32 chatID)
        {
            if (m_chatWindowsContent.ContainsKey(chatID)) return m_chatWindowsContent[chatID];
            return "";
        }

        public AppChatBox(IAppHelper appHelper)
            : base(appHelper)
        {
            m_chatWindowsContent[0] = "";
        }

        public override void Initialize(IAppHelper appHelper)
        {
            base.Initialize(appHelper);
            Settings = new AppChatboxControl.AppChatBoxSettings();
            Helper.OnKwsUserChanged += OnUserChanged;
            m_lastEventPopup = DateTime.MinValue;
        }

        /// <summary>
        /// Handle a selection change in the Workspace member list.
        /// Used for the SKURL.
        /// </summary>
        public void OnUserChanged(object caller, EventArgs _args)
        {
            // The chatbox is interested in this event only when in the public workspace.
            if (!Helper.IsPublicKws()) return;

            OnKwsUserChangedEventArgs args = (OnKwsUserChangedEventArgs)_args;

            if (args.ID == Helper.GetKwmUserID())
            {
                // Do not allow chatting with ourself in the public workspace.
                DoOnAppChatUserChanged(new OnAppChatUserChangedEventArgs("Message board", 0));
            }
            else
            {
                // Chatting with someone in the public workspace.
                DoOnAppChatUserChanged(new OnAppChatUserChangedEventArgs("Message board with " + Helper.GetUserDisplayName(args.ID), args.ID));
            }
        }

        /// <summary>
        /// Send the given message to the appropriate chat ID on the server.
        /// </summary>
        public void SendChatMessage(UInt32 chatID, String message)
        {
            AnpMsg msg = Helper.NewKAnpCmd(KAnpType.KANP_CMD_CHAT_MSG);
            msg.AddUInt32(chatID);
            msg.AddString(message);
            PostKasQuery(msg, null, OnChatMsgReply);
        }

        /// <summary>
        /// Format a chat message from its basic information to a human readable form.
        /// </summary>
        public String FormatChatMessage(UInt32 _userID, UInt64 _date, String _message)
        {
            return Helper.GetUserDisplayName(_userID) + " (" +
                   Base.KDateToDateTime(_date).ToString("dd/MM/yyyy HH:mm") + ")" +
                   Environment.NewLine + "    " +
                   _message;
        }

        public void OnChatMsgReply(KasQuery ctx)
        {
            AnpMsg cmd = ctx.Cmd;
            AnpMsg res = ctx.Res;

            if (res.Type == KAnpType.KANP_RES_OK)
                DoOnSentChatMsgOK(new OnSentChatMsgOKEventArgs(cmd.Elements[1].UInt32,
                                                               cmd.Elements[2].String));

            else if (res.Type == KAnpType.KANP_RES_FAIL)
                DoOnSentChatMsgFailed(new OnSentChatMsgFailedEventArgs(cmd.Elements[1].UInt32,
                                                                       cmd.Elements[2].String,
                                                                       res.Elements[1].String));
            else
                Logging.Log(2, "unexpected chat command reply");
        }

        public override KwsAnpEventStatus HandleAnpEvent(AnpMsg msg)
        {
            // Incoming chat message.
            if (msg.Type == KAnpType.KANP_EVT_CHAT_MSG)
            {
                UInt64 date = msg.Elements[1].UInt64;
                UInt32 chatID = msg.Elements[2].UInt32;
                UInt32 userID = msg.Elements[3].UInt32;
                String payload = msg.Elements[4].String;

                String formattedMsg = FormatChatMessage(userID, date, payload);

                // Don't prepend a newline if this is the first message ever.
                if (!m_chatWindowsContent.ContainsKey(chatID))
                    m_chatWindowsContent[chatID] = formattedMsg;
                else
                    m_chatWindowsContent[chatID] += System.Environment.NewLine + formattedMsg;

                
                // Notify the user of a new incoming message if it comes from
                // someone else.
                if (Helper.GetKwmUserID() != userID)
                {
                    TimeSpan delta = DateTime.Now.Subtract(m_lastEventPopup);
                    bool hidePopup = delta.TotalMilliseconds < DeltaBetweenPopup;

                    Helper.NotifyUser(new ChatNotificationItem(msg, Helper, hidePopup));
                    if (!hidePopup) m_lastEventPopup = DateTime.Now;
                }

                DoOnIncomingChatMsg(new OnIncomingChatMsgEventArgs(chatID, userID, date, payload));

                return KwsAnpEventStatus.Processed;
            }

            return KwsAnpEventStatus.Unprocessed;
        }

        public override void PrepareForRebuild(KwsRebuildInfo rebuildInfo)
        {
            m_chatWindowsContent = new Dictionary<UInt32, String>();
            base.PrepareForRebuild(rebuildInfo);
        }

        private void DoOnIncomingChatMsg(OnIncomingChatMsgEventArgs _args)
        {
            if (OnIncomingChatMsg != null)
                OnIncomingChatMsg(this, _args);
        }

        private void DoOnSentChatMsgOK(OnSentChatMsgOKEventArgs _args)
        {
            if (OnSentChatMsgOK != null)
                OnSentChatMsgOK(this, _args);
        }

        private void DoOnSentChatMsgFailed(OnSentChatMsgFailedEventArgs _args)
        {
            if (OnSentChatMsgFailed != null)
                OnSentChatMsgFailed(this, _args);
        }

        private void DoOnAppChatUserChanged(OnAppChatUserChangedEventArgs _args)
        {
            if (OnAppChatUserChanged != null)
                OnAppChatUserChanged(this, _args);
        }
    }
}