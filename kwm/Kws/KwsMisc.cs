using System;
using System.Diagnostics;
using kwm.KwmAppControls;
using System.Collections.Generic;
using System.Runtime.Serialization;
using kwm.Utils;
using System.IO;
using Tbx.Utils;

/* This file contains miscellaneous stuff related to the workspace internals.
 * Create separate files as needed.
 */

namespace kwm
{
    /// <summary>
    /// Compare two workspaces by name. On equality, the workspaces are compared
    /// by internal ID.
    /// </summary>
    class KwsNameComparer : Comparer<Workspace>
    {
        public override int Compare(Workspace x, Workspace y)
        {
            int res = x.GetKwsName().CompareTo(y.GetKwsName());
            if (res != 0) return res;
            return x.InternalID.CompareTo(y.InternalID);
        }
    }

    /// <summary>
    /// This class is used to report a major error to the user.
    /// </summary>
    public class WmMajorErrorGer : GuiExecRequest
    {
        private String m_context;
        private Exception m_ex;

        public WmMajorErrorGer(String context, Exception ex)
        {
            m_context = context;
            m_ex = ex;
            Logging.LogException(ex);
        }

        public override void Run()
        {
            Misc.KwmTellUser(m_context + ": " + m_ex.Message + ".", System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// This class is a pure data store that contains information about the
    /// ANP data received from the KAS.
    /// </summary>
    [Serializable]
    public class KwsKAnpState
    {
        /// <summary>
        /// True if the local workspace has caught up with the KAS as far
        /// as ANP events are concerned. This flag is set when the KAS has no
        /// more events to send us and we have no more unprocessed events in
        /// the DB.
        /// </summary>
        [NonSerialized]
        public bool CaughtUpFlag;

        /// <summary>
        /// If CaughtUpFlag is false, this value indicates whether the
        /// workspace is catching up because it is has reconnected or because
        /// it is rebuilding from scratch.
        /// </summary>
        public bool RebuildFlag;

        /// <summary>
        /// ID of the last ANP event received. This is 0 if no event has been
        /// received yet.
        /// </summary>
        public UInt64 LastReceivedEventId = 0;

        /// <summary>
        /// ID of the latest event available on the KAS when the workspace
        /// logs in. This is 0 if no event is available on the KAS.
        /// </summary>
        [NonSerialized]
        public UInt64 LoginLatestEventId;

        /// <summary>
        /// Number of unprocessed ANP events lingering in the DB.
        /// </summary>
        public UInt64 NbUnprocessedEvent = 0;

        /// <summary>
        /// Non-deserializing constructor.
        /// </summary>
        public KwsKAnpState()
        {
            Initialize(new StreamingContext());
        }

        /// <summary>
        /// Initialization code common to both the deserialized and
        /// non-deserialized cases.
        /// </summary>
        [OnDeserialized]
        public void Initialize(StreamingContext context)
        {
            CaughtUpFlag = false;
            LoginLatestEventId = 0;
        }
    }

    /// <summary>
    /// Current step of the spawn task of the workspace.
    /// </summary>
    public enum KwsSpawnTaskStep
    {
        /// <summary>
        /// Wait for the authorization to connect.
        /// </summary>
        Wait,

        /// <summary>
        /// Connect to the KAS.
        /// </summary>
        Connect,

        /// <summary>
        /// Login to the KAS.
        /// </summary>
        Login
    }

    /// <summary>
    /// Current step of the rebuilding task of the workspace.
    /// </summary>
    public enum KwsRebuildTaskStep
    {
        /// <summary>
        /// Not yet started.
        /// </summary>
        None,

        /// <summary>
        /// In progress.
        /// </summary>
        InProgress
    }

    /// <summary>
    /// Workspace credentials.
    /// </summary>
    [Serializable]
    public class KwsCredentials
    {
        /// <summary>
        /// Current serialization version.
        /// </summary>
        public const Int32 Version = 3;

        /// <summary>
        /// KAS identifier.
        /// </summary>
        public KasIdentifier KasID;

        /// <summary>
        /// External workspace ID.
        /// </summary>
        public UInt64 ExternalID;

        /// <summary>
        /// Email ID associated to this workspace.
        /// </summary>
        public String EmailID = "";

        /// <summary>
        /// Name of the workspace.
        /// </summary>
        public String KwsName = "";

        /// <summary>
        /// Name of the user using this workspace.
        /// </summary>
        public String UserName = "";

        /// <summary>
        /// Email address of the user using this workspace.
        /// </summary>
        public String UserEmailAddress = "";

        /// <summary>
        /// Name of the person who has invited the user, if any.
        /// </summary>
        public String InviterName = "";

        /// <summary>
        /// Email address of the person who has invited the user, if any.
        /// </summary>
        public String InviterEmailAddress = "";

        /// <summary>
        /// True if the user is an administrator.
        /// </summary>
        public bool AdminFlag;

        /// <summary>
        /// True if the workspace is public.
        /// </summary>
        public bool PublicFlag;

        /// <summary>
        /// True if the workspace is secure.
        /// </summary>
        public bool SecureFlag;

        /// <summary>
        /// ID of the user.
        /// </summary>
        public UInt32 UserID;

        /// <summary>
        /// Login ticket.
        /// </summary>
        public byte[] Ticket = null;

        /// <summary>
        /// Login password.
        /// </summary>
        public String Pwd = "";

        /// <summary>
        /// Address of the KWMO server.
        /// </summary>
        public String KwmoAddress = "";

        public KwsCredentials()
        {
        }

        public KwsCredentials(KwsCredentials other)
        {
            KasID = other.KasID;
            ExternalID = other.ExternalID;
            EmailID = other.EmailID;
            KwsName = other.KwsName;
            UserName = other.UserName;
            UserEmailAddress = other.UserEmailAddress;
            InviterName = other.InviterName;
            InviterEmailAddress = other.InviterEmailAddress;
            AdminFlag = other.AdminFlag;
            PublicFlag = other.PublicFlag;
            SecureFlag = other.SecureFlag;
            UserID = other.UserID;
            if (other.Ticket != null) Ticket = (byte[])other.Ticket.Clone();
            Pwd = other.Pwd;
            KwmoAddress = other.KwmoAddress;
        }
    }

    /// <summary>
    /// This class contains information about the users of a workspace.
    /// </summary>
    [Serializable]
    public class KwsUserInfo
    {
        /// <summary>
        /// Tree of users indexed by user ID.
        /// </summary>
        public SortedDictionary<UInt32, KwsUser> UserTree = new SortedDictionary<UInt32, KwsUser>();

        /// <summary>
        /// Reference to the workspace creator, if any.
        /// </summary>
        public KwsUser Creator = null;

        /// <summary>
        /// Return true if the user specified exists.
        /// </summary>
        public bool IsUser(UInt32 ID)
        {
            return UserTree.ContainsKey(ID);
        }

        /// <summary>
        /// Return the user having the ID specified, if any.
        /// </summary>
        public KwsUser GetUserByID(UInt32 ID)
        {
            return (IsUser(ID) ? UserTree[ID] : null);
        }

        /// <summary>
        /// Return the user having the email address specified, if any.
        /// </summary>
        public KwsUser GetUserByEmailAddress(String emailAddress)
        {
            foreach (KwsUser u in UserTree.Values) if (emailAddress.ToLower() == u.EmailAddress.ToLower()) return u;
            return null;
        }
    }

    /// <summary>
    /// This class contains the core data of the workspace (credentials, user
    /// list, etc).
    /// </summary>
    [Serializable]
    public class KwsCoreData
    {
        /// <summary>
        /// Workspace credentials.
        /// </summary>
        public KwsCredentials Credentials = null;

        /// <summary>
        /// Users of the workspace.
        /// </summary>
        public KwsUserInfo UserInfo = new KwsUserInfo();

        /// <summary>
        /// Information required to rebuild the workspace.
        /// </summary>
        public KwsRebuildInfo RebuildInfo = new KwsRebuildInfo();
    }

    /// <summary>
    /// Contains the entire input of the user when inviting users to a 
    /// workspace.
    /// </summary>
    public class KwsInviteOpParams
    {
        /// <summary>
        /// List of users being invited.
        /// </summary>
        public List<KwsInviteOpUser> UserArray = new List<KwsInviteOpUser>();

        /// <summary>
        /// Personalized message in plain text to send along the invitation 
        /// email. Empty if none.
        /// </summary>
        public String Message = "";

        /// <summary>
        /// WLEU returned by the KCD.
        /// </summary>
        public String WLEU = "";

        /// <summary>
        /// Set to true to tell the KCD to send the invitation email
        /// by itself. Used when the invitation is done via the KWM.
        /// </summary>
        public bool KcdSendInvitationEmailFlag;

        /// <summary>
        /// Set the AlreadyInvitedFlag for the users which have already been 
        /// invited to the workspace.
        /// </summary>
        public void FlagInvitedUsers(KwsUserInfo kui)
        {
            foreach (KwsInviteOpUser iu in UserArray)
                iu.AlreadyInvitedFlag = (kui.GetUserByEmailAddress(iu.EmailAddress) != null);
        }

        /// <summary>
        /// Return the list of users that are not already invited to the workspace.
        /// </summary>
        public List<KwsInviteOpUser> NotAlreadyInvitedUserArray
        {
            get
            {
                List<KwsInviteOpUser> list = new List<KwsInviteOpUser>();
                foreach (KwsInviteOpUser u in UserArray)
                    if (!u.AlreadyInvitedFlag) list.Add(u);
                return list;
            }
        }
        /// <summary>
        /// Get the invitees formatted as a single string, each email address 
        /// separated by a space. 
        /// </summary>
        public String GetInviteesLine()
        {
            String line = "";

            foreach (KwsInviteOpUser i in UserArray)
            {
                if (line != "") line += " ";
                line += i.EmailAddress;
            }

            return line;            
        }

        /// <summary>
        /// Return true if at least one user has not been successfully invited.
        /// </summary>
        public bool HasInvitationErrors()
        {
            foreach (KwsInviteOpUser u in UserArray)
            {
                if (u.Error != "")
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return the list of email addresses that could not be invited, followed by
        /// the error string.
        /// </summary>
        public String GetErroneousInviteesText()
        {
            String msg = "";
            foreach (KwsInviteOpUser o in UserArray)
            {
                if (o.Error != "")
                {
                    if (msg != "") msg += Environment.NewLine;
                    msg += o.EmailAddress + " (" + o.Error + ")";
                }
            }
            return msg;
        }
    }

    /// <summary>
    /// Represent a user invited to a workspace.
    /// </summary>
    public class KwsInviteOpUser
    {
        /// <summary>
        /// Name of the user, if any.
        /// </summary>
        public String UserName = "";

        /// <summary>
        /// User email address.
        /// </summary>
        public String EmailAddress = "";

        /// <summary>
        /// Inviter-specified password, if any.
        /// </summary>
        public String Pwd = "";

        /// <summary>
        /// User key ID. If none, set to 0.
        /// </summary>
        public UInt64 KeyID = 0;

        /// <summary>
        /// User organization's name, if any.
        /// </summary>
        public String OrgName = "";

        /// <summary>
        /// Email ID used to invite the user, if any.
        /// </summary>
        public String EmailID;

        /// <summary>
        /// URL that should appear in the invitation mail.
        /// </summary>
        public String Url;

        /// <summary>
        /// Invitation error for the user, if any.
        /// </summary>
        public String Error;

        /// <summary>
        /// Flag telling if the user has already been invited to the workspace.
        /// </summary>
        public bool AlreadyInvitedFlag = false;

        public KwsInviteOpUser()
        {
        }

        public KwsInviteOpUser(String emailAddress)
        {
            EmailAddress = emailAddress;
        }
    }
}