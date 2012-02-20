using System;
using Tbx.Utils;
using kwm.KwmAppControls;
using kwm.Utils;

namespace kwm
{
    /// <summary>
    /// Notification item used to notify when some user downloaded a file 
    /// in a workspace.
    /// </summary>
    public class AttachManagementNotificationItem : NotificationItem
    {
        private String m_emailSubject;

        public AttachManagementNotificationItem(IAppHelper helper, AnpMsg skurlCmd)
            : base(null, 0, helper)
        {
            m_emailSubject = skurlCmd.Elements[0].String;
            m_notificationToTake = NotificationEffect.ShowPopup;
        }

        public override string EventText
        {
            get
            {
                Logging.Log("Asking EventText for AttachManagementNotificationItem");
                return "The files you attached with your email <" + (m_emailSubject == "" ? "No subject" : Base.TroncateString(m_emailSubject, 40))+ "> are being uploaded.";
            }
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
