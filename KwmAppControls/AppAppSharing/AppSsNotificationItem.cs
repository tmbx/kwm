using System;
using System.Collections.Generic;
using System.Text;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public class ScreenSharingNotificationItem : NotificationItem
    {
        private string m_sessionSubject = "";

        public override String EventText
        {
            get
            {
                switch (m_eventType)
                {
                    case KAnpType.KANP_EVT_VNC_START:
                        return m_eventSourceUserName + " has just started the screen sharing session \"" + m_sessionSubject + "\".";
                    case KAnpType.KANP_EVT_VNC_END:
                        return m_eventSourceUserName + " has just ended a screen sharing session.";
                    default: return "Unknown event";
                }
            }
        }

        public ScreenSharingNotificationItem(AnpMsg _msg, IAppHelper _helper)
            : base(_msg, KAnpType.KANP_NS_VNC, _helper)
        {
            if (m_eventType == KAnpType.KANP_EVT_VNC_START)
                m_sessionSubject = _msg.Elements[4].String;
        }

        public override String GetSimplifiedFormattedDetail()
        {
            return EventText;
        }

        public override String GetFullFormattedDetail()
        {
            return GetLongFormattedDate + GetSimplifiedFormattedDetail();
        }
        public override string GetSimpleDetail()
        {
            return EventText;
        }

        public override string GetFullDetail()
        {
            return GetLongFormattedDate + "==>" + GetSimpleDetail();
        }
    }
}