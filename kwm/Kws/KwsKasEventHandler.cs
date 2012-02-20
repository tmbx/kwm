using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using kwm.Utils;
using kwm.KwmAppControls;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// Handle the workspace events received from the KAS that do not concern 
    /// the applications.
    /// </summary>
    public class KwsKasEventHandler
    {
        /// <summary>
        /// Reference to the workspace.
        /// </summary>
        private Workspace m_kws;

        public KwsKasEventHandler(Workspace kws)
        {
            m_kws = kws;
        }

        /// <summary>
        /// Handle an ANP event.
        /// </summary>
        public KwsAnpEventStatus HandleAnpEvent(AnpMsg msg)
        {
            UInt32 type = msg.Type;

            // Dispatch.
            if (type == KAnpType.KANP_EVT_KWS_CREATED) return HandleKwsCreatedEvent(msg);
            else if (type == KAnpType.KANP_EVT_KWS_INVITED) return HandleKwsInvitationEvent(msg);
            else if (type == KAnpType.KANP_EVT_KWS_USER_REGISTERED) return HandleUserRegisteredEvent(msg);
            else if (type == KAnpType.KANP_EVT_KWS_DELETED) return HandleKwsDeletedEvent();
            else return KwsAnpEventStatus.Unprocessed;
        }

        private KwsAnpEventStatus HandleKwsCreatedEvent(AnpMsg msg)
        {
            // FIXME store the workspace creation date.
            KwsUser user = new KwsUser();
            user.UserID = msg.Elements[2].UInt32;
            user.InvitationDate = msg.Elements[1].UInt64;
            user.AdminName = msg.Elements[3].String;
            user.EmailAddress = msg.Elements[4].String;
            user.Power = 1;
            user.OrgName = msg.Elements[msg.Minor <= 2 ? 7 : 5].String;

            // Add the creator to the user list.
            m_kws.CoreData.UserInfo.UserTree[user.UserID] = user;
            m_kws.CoreData.UserInfo.Creator = user;

            // FIXME do something better when we look into the user powers.

            // If we are the creator of this workspace, set our Admin flag.
            if (m_kws.CoreData.Credentials.UserID == user.UserID)
                m_kws.CoreData.Credentials.AdminFlag = true;

            m_kws.StateChangeUpdate(false);

            return KwsAnpEventStatus.Processed;
        }

        private KwsAnpEventStatus HandleKwsInvitationEvent(AnpMsg msg)
        {
            UInt32 nbUser = msg.Elements[msg.Minor <= 2 ? 2 : 3].UInt32;

            // This is not supposed to happen, unless in the case of a broken
            // KWM. Indeed, the server does not enforce any kind of restriction
            // regarding the number of invitees in an INVITE command. If a KWM
            // sends such a command with no invitees, the server will fire an
            // empty INVITE event.
            if (nbUser < 1) return KwsAnpEventStatus.Processed;

            List<KwsUser> users = new List<KwsUser>();

            // Add the users in the user list.
            int j = (msg.Minor <= 2) ? 3 : 4;

            for (int i = 0; i < nbUser; i++)
            {                
                KwsUser user = new KwsUser();
                user.UserID = msg.Elements[j++].UInt32;
                user.InvitationDate = msg.Elements[1].UInt64;
                if (msg.Minor >= 3) user.InvitedBy = msg.Elements[2].UInt32;
                user.AdminName = msg.Elements[j++].String;
                user.EmailAddress = msg.Elements[j++].String;
                if (msg.Minor <= 2) j += 2;
                user.OrgName = msg.Elements[j++].String;
                users.Add(user);
                m_kws.CoreData.UserInfo.UserTree[user.UserID] = user;
            }

            m_kws.StateChangeUpdate(false);

            // Never notify new public workspace invitations. They are automatically
            // generated when a recipient takes an action on the Web page.
            if (!m_kws.IsPublicKws())
            {
                // Notify the new invitees to the user if it was not him that invited them.
                // Note: we only have this information from v3 and later. In case of an older
                // version, notify in all cases.
                if (msg.Minor >= 3)
                {
                    if (msg.Elements[2].UInt32 != m_kws.CoreData.Credentials.UserID)
                        m_kws.NotifyUser(new KwsInvitationNotificationItem(m_kws, users));
                }

                else
                {
                    m_kws.NotifyUser(new KwsInvitationNotificationItem(m_kws, users));
                }
            }
            
            return KwsAnpEventStatus.Processed;
        }

        private KwsAnpEventStatus HandleUserRegisteredEvent(AnpMsg msg)
        {
            UInt32 userID = msg.Elements[2].UInt32;
            String userName = msg.Elements[3].String;

            KwsUser user = m_kws.CoreData.UserInfo.GetUserByID(userID);
            if (user == null)
                throw new Exception("no such user");

            user.UserName = userName;

            // Refresh the user list.
            m_kws.StateChangeUpdate(false);

            return KwsAnpEventStatus.Processed;
        }

        private KwsAnpEventStatus HandleKwsDeletedEvent()
        {
            m_kws.KasLoginHandler.LoginResult = KwsLoginResult.DeletedKws;
            m_kws.KasLoginHandler.LoginResultString = "the " + Base.GetKwsString() + " has been deleted";
            m_kws.Sm.RequestTaskSwitch(KwsTask.WorkOffline);
            return KwsAnpEventStatus.Processed;
        }
    }
}