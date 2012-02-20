using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    /* User Control for the Chat box */
    public sealed partial class AppChatboxControl : BaseAppControl
    {
        public AppChatboxControl()
        {
            InitializeComponent();
        }

        public AppChatboxControl(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        public AppChatBox SrcApp
        {
            get
            {
                return m_srcApp as AppChatBox;
            }
        }

        public override UInt32 ID
        {
            get { return KAnpType.KANP_NS_CHAT; }
        }

        private AppChatBoxSettings Settings
        {
            get { return (AppChatBoxSettings)m_srcApp.Settings; }
        }

        protected override void UnregisterAppEventHandlers()
        {
            base.UnregisterAppEventHandlers();
            SrcApp.OnIncomingChatMsg -= HandleOnIncomingChatMsg;
            SrcApp.OnSentChatMsgFailed -= HandleOnChatMsgFailed;
            SrcApp.OnSentChatMsgOK -= HandleOnChatMsgOK;
            SrcApp.OnAppChatUserChanged -= HandleOnAppChatUserChanged;
        }

        protected override void RegisterAppEventHandlers()
        {
            base.RegisterAppEventHandlers();
            SrcApp.OnIncomingChatMsg += HandleOnIncomingChatMsg;
            SrcApp.OnSentChatMsgFailed += HandleOnChatMsgFailed;
            SrcApp.OnSentChatMsgOK += HandleOnChatMsgOK;
            SrcApp.OnAppChatUserChanged += HandleOnAppChatUserChanged;
        }

        protected override void UpdateControls()
        {
            rtbChatWindow.Clear();
            txtChatMessage.Clear();

            switch (GetRunLevel())
            {
                case KwsRunLevel.Stopped:
                    rtbChatWindow.Enabled = false;
                    txtChatMessage.Enabled = false;
                    btnSend.Enabled = false;
                    break;

                case KwsRunLevel.Offline:
                    // Disable posting new messages
                    rtbChatWindow.Enabled = true; 
                    txtChatMessage.Enabled = false;
                    btnSend.Enabled = false;
                    rtbChatWindow.Text = SrcApp.ChatWindowContent(Settings.ChatID);
                    break;

                case KwsRunLevel.Online:
                    rtbChatWindow.Enabled = true;
                    
                    if (SrcApp != null)
                        txtChatMessage.Text = SrcApp.AppSettings.ChatLine;

                    // Enable all, except if chatting with ourself in the public workspace.
                    if (SrcApp != null && SrcApp.Helper.IsPublicKws() && Settings.ChatID == 0)
                    {
                        txtChatMessage.Enabled = false;
                        btnSend.Enabled = false;
                    }
                    else
                    {
                        txtChatMessage.Enabled = true;
                        btnSend.Enabled = txtChatMessage.Text != "";
                    }

                    rtbChatWindow.Text = SrcApp.ChatWindowContent(Settings.ChatID);
                    MsgBoardTitle.Text = Settings.Title;
                    break;
            }
        }

        private void SendMessage()
        {
            Debug.Assert(m_srcApp != null, "srcApp not set in AppChatBox control");
            SrcApp.SendChatMessage(Settings.ChatID, txtChatMessage.Text); ;
            txtChatMessage.Clear();
            txtChatMessage.Focus();
            SrcApp.AppSettings.ChatLine = "";
            btnSend.Enabled = false;
        }

        private void AddText(String _text)
        {
            try
            {
                if (rtbChatWindow.Text.Length != 0)
                    _text = Environment.NewLine + _text;
                rtbChatWindow.AppendText(_text);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /* Event handlers */

        private void HandleOnIncomingChatMsg(object _sender, EventArgs _args)
        {
            try
            {
                if (SrcApp.NotifiedCaughtUpFlag)
                {
                    OnIncomingChatMsgEventArgs args = (OnIncomingChatMsgEventArgs)_args;
                    rtbChatWindow.Clear();
                    AddText(SrcApp.ChatWindowContent(Settings.ChatID));
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void HandleOnChatMsgOK(object _sender, EventArgs _args)
        {
            // No op.
            return;
        }

        private void HandleOnChatMsgFailed(object _sender, EventArgs _args)
        {
            try
            {
                OnSentChatMsgFailedEventArgs args = (OnSentChatMsgFailedEventArgs)_args;

                AddText("The following message could not be delivered : " +
                    Environment.NewLine +
                    args.SentMessage + Environment.NewLine +
                    "(" + args.ErrorMessage + ")");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void HandleOnAppChatUserChanged(object _sender, EventArgs _args)
        {
            try
            {
                OnAppChatUserChangedEventArgs args = (OnAppChatUserChangedEventArgs)_args;

                MsgBoardTitle.Text = args.Title;
                Settings.Title = args.Title;
                Settings.ChatID = args.ChatID;

                UpdateControls();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                SendMessage();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtChatWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (txtChatMessage.Enabled &&
                    (Control.ModifierKeys != Keys.Control &&
                     Control.ModifierKeys != Keys.Alt))
                {
                    if(!Char.IsControl(e.KeyChar))
                        txtChatMessage.Text += e.KeyChar;

                    txtChatMessage.Focus();
                    txtChatMessage.SelectionStart = txtChatMessage.Text.Length;
                    txtChatMessage.SelectionLength = 0;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtChatMessage_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;

                    if (txtChatMessage.Text != "")
                        SendMessage();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void CutCopyPasteContextMenu_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                ChatWindowContextMenu.Items[0].Enabled = (rtbChatWindow.SelectionLength > 0);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            try
            {
                rtbChatWindow.Copy();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            try
            {
                rtbChatWindow.SelectAll();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtChatWindow_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                rtbChatWindow.SelectionStart = rtbChatWindow.Text.Length;
                rtbChatWindow.ScrollToCaret();
                Base.ScrollToBottom(rtbChatWindow.Handle);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtChatMessage_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (m_srcApp != null)
                    SrcApp.AppSettings.ChatLine = txtChatMessage.Text;

                btnSend.Enabled = (txtChatMessage.Text != "");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtChatWindow_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Misc.OpenFileInWorkerThread(e.LinkText);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        [Serializable]
        public class AppChatBoxSettings : ControlSettings
        {
            [NonSerialized]
            public string ChatLine;

            [NonSerialized]
            public string Title;

            [NonSerialized]
            public UInt32 ChatID;

            public AppChatBoxSettings()
            {
                Title = "Message board";
            }
        }
    }

    public class OnIncomingChatMsgEventArgs : EventArgs
    {
        private UInt32 m_chatID;
        private UInt32 m_userID;
        private UInt64 m_date;
        private String m_strMessage;

        public UInt32 ChatID
        {
            get
            {
                return m_chatID;
            }
        }

        public UInt32 UserID
        {
            get
            {
                return m_userID;
            }
        }

        public UInt64 Date
        {
            get
            {
                return m_date;
            }
        }

        public String Message
        {
            get
            {
                return m_strMessage;
            }
        }


        public OnIncomingChatMsgEventArgs(UInt32 _chatId, UInt32 _userID, UInt64 _date, String _msg)
        {
            m_chatID = _chatId;
            m_userID = _userID;
            m_date = _date;
            m_strMessage = _msg;
        }
    }

    public class OnSentChatMsgOKEventArgs : EventArgs
    {
        private UInt32 m_chatID;
        private String m_strMessage;

        public UInt32 ChatID
        {
            get
            {
                return m_chatID;
            }
        }

        public String SentMessage
        {
            get
            {
                return m_strMessage;
            }
        }

        public OnSentChatMsgOKEventArgs(UInt32 _chatId, String _msg)
        {
            m_chatID = _chatId;
            m_strMessage = _msg;
        }
    }

    public class OnSentChatMsgFailedEventArgs : EventArgs
    {
        private UInt32 m_chatID;
        private String m_strSentMessage;
        private String m_strErrorMessage;

        public UInt32 ChatID
        {
            get
            {
                return m_chatID;
            }
        }

        public String SentMessage
        {
            get
            {
                return m_strSentMessage;
            }
        }

        public String ErrorMessage
        {
            get
            {
                return m_strErrorMessage;
            }
        }

        public OnSentChatMsgFailedEventArgs(UInt32 _chatId, String _sentMsg, String _errorMsg)
        {
            m_chatID = _chatId;
            m_strSentMessage = _sentMsg;
            m_strErrorMessage = _errorMsg;
        }
    }

    public class OnAppChatUserChangedEventArgs : EventArgs
    {
        private string m_title;
        private UInt32 m_chatID;

        /// <summary>
        /// Chatbox control title.
        /// </summary>
        public string Title
        {
            get { return m_title; }
        }

        /// <summary>
        /// Current chat ID.
        /// </summary>
        public UInt32 ChatID
        {
            get { return m_chatID; }
        }

        public OnAppChatUserChangedEventArgs(string title, UInt32 chatID)
        {
            m_title = title;
            m_chatID = chatID;
        }
    }
}
