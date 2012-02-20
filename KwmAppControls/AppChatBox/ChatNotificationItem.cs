using System;
using System.Collections.Generic;
using System.Text;

using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public class ChatNotificationItem : NotificationItem
    {
        private bool m_forcedHidePopup;

        /// <summary>
        /// Anp message of the event received.
        /// </summary>
        private AnpMsg m_msg;

        public override bool WantShowPopup
        {
            get
            {
                return base.WantShowPopup && !m_forcedHidePopup;
            }
        }

        public override bool HasRtfText
        {
            get
            {
                return true;
            }
        }

        public override String EventRtf
        {
            get
            {
                return @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fswiss\fcharset0 Arial;}}" +
                       @"\viewkind4\uc1\pard\b\f0\fs22 " + Who() + @" says:" + @"\par\b0 " +
                       What() + @"\par}";
            }
        }

        public override String EventText
        {
            get 
            {
                switch (m_eventType)
                {
                    case KAnpType.KANP_EVT_CHAT_MSG: 
                         return Who() + " says: " + Environment.NewLine + What();

                    default: return "Unknown event";
                }
            }
        }

        public ChatNotificationItem(AnpMsg _msg, IAppHelper _helper, bool _forceHidePopup)
            : base(_msg, KAnpType.KANP_NS_CHAT, _helper)
        {
            m_msg = _msg;
            m_forcedHidePopup = _forceHidePopup;
        }

        private String Who()
        {
            return Base.TroncateString(m_helper.GetUserDisplayName(m_msg.Elements[3].UInt32), 29);
        }

        private String What()
        {
            return Base.TroncateString(m_msg.Elements[4].String, 99);
        }

        public override String GetSimplifiedFormattedDetail()
        {
            return EventText;
        }

        public override String GetFullFormattedDetail()
        {
            return GetLongFormattedDate + GetSimplifiedFormattedDetail() + System.Environment.NewLine + m_msg.Elements[4];
        }
        public override string GetSimpleDetail()
        {
            return EventText;
        }

        public override string GetFullDetail()
        {
            return GetLongFormattedDate + "==>"+ GetSimpleDetail() + ": \"" + m_msg.Elements[4] + "\"";
        }
    }
}
