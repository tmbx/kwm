using System;
using System.Collections.Generic;
using System.Text;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    public class KwsInvitationNotificationItem : Utils.NotificationItem
    {
        private List<KwmAppControls.KwsUser> m_users;

        public KwsInvitationNotificationItem(kwm.KwmAppControls.IAppHelper helper, List<KwmAppControls.KwsUser> users)
            : base(null, 0, helper)
        {
            m_users = users;
        }

        private String GetUsersFlatList()
        {
            String msg = "";
            foreach (KwmAppControls.KwsUser u in m_users) msg += u.UiFullName;
            return msg;
        }

        public override string EventText
        {
            get 
            {
                String msg = m_users[0].UiSimpleName;
                if (m_users.Count > 1) msg += " and " + (m_users.Count - 1) + " other(s) have";
                else msg += " has";

                msg += " been invited to the " + Base.GetKwsString() + " '" + m_helper.GetKwsName() + "'.";

                return msg;
            }
        }

        public override String GetSimplifiedFormattedDetail()
        {
            return EventText;
        }

        public override String GetFullFormattedDetail()
        {
            return GetLongFormattedDate + "New invitees : " + System.Environment.NewLine + GetUsersFlatList();
        }
        public override string GetSimpleDetail()
        {
            return EventText;
        }

        public override string GetFullDetail()
        {
            return GetLongFormattedDate + "New invitees : " + GetUsersFlatList();
        }
    }
}
