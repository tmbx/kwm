using System.Collections.Generic;
using System;
using kwm.KwmAppControls;
using System.Diagnostics;
using System.Runtime.Serialization;
using kwm.Utils;

namespace kwm.KwmAppControls
{
    /// <summary>
    /// Main status of the workspace. 
    /// </summary>
    public enum KwsMainStatus
    {
        /// <summary>
        /// The workspace has not been spawned successfully (yet).
        /// </summary>
        NotYetSpawned,

        /// <summary>
        /// The workspace was created successfully and it is working as 
        /// advertised.
        /// </summary>
        Good,

        /// <summary>
        /// The workspace needs to be rebuilt to be functional. The type
        /// of rebuild required is given by the RebuildInfo field.
        /// </summary>
        RebuildRequired,

        /// <summary>
        /// The workspace has been scheduled for deletion. Kiss it goodbye.
        /// </summary>
        OnTheWayOut
    }

    /// <summary>
    /// Task to perform in the workspace.
    /// </summary>
    public enum KwsTask
    {
        /// <summary>
        /// Stop the workspace. Don't run applications, don't connect to
        /// anything, don't delete anything.
        /// </summary>
        Stop,

        /// <summary>
        /// Create a new workspace or join or import an existing workspace.
        /// </summary>
        Spawn,

        /// <summary>
        /// Work offline.
        /// </summary>
        WorkOffline,

        /// <summary>
        /// Work online.
        /// </summary>
        WorkOnline,

        /// <summary>
        /// Rebuild the workspace.
        /// </summary>
        Rebuild,

        /// <summary>
        /// Delete the workspace.
        /// </summary>
        Delete
    }

    /// <summary>
    /// Represent the run level of a workspace.
    /// </summary>
    public enum KwsRunLevel
    {
        /// <summary>
        /// The workspace isn't ready to work offline.
        /// </summary>
        Stopped,

        /// <summary>
        /// The workspace is ready to work offline.
        /// </summary>
        Offline,

        /// <summary>
        /// The workspace is ready to work online.
        /// </summary>
        Online
    }

    /// <summary>
    /// Status of an application of the workspace.
    /// </summary>
    public enum KwsAppStatus
    {
        /// <summary>
        /// The application is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The application is stopping.
        /// </summary>
        Stopping,

        /// <summary>
        /// The application is started.
        /// </summary>
        Started,

        /// <summary>
        /// The application is starting.
        /// </summary>
        Starting
    }

    /// <summary>
    /// Status of the login with the KAS.
    /// </summary>
    public enum KwsLoginStatus
    {
        /// <summary>
        /// The user of the workspace is logged out.
        /// </summary>
        LoggedOut,

        /// <summary>
        /// Waiting for logout command reply.
        /// </summary>
        LoggingOut,

        /// <summary>
        /// Waiting for login command reply.
        /// </summary>
        LoggingIn,

        /// <summary>
        /// The user of the workspace is logged in.
        /// </summary>
        LoggedIn
    }

    /// <summary>
    /// Processing status of an ANP event. These statuses are used in the 
    /// database.
    /// </summary>
    public enum KwsAnpEventStatus
    {
        /// <summary>
        /// The event has not been processed yet.
        /// </summary>
        Unprocessed = 0,

        /// <summary>
        /// The event was processed successfully.
        /// </summary>
        Processed
    }

    /// <summary>
    /// Type of an immediate notification sent by a workspace state machine.
    /// </summary>
    public enum KwsSmNotif
    {
        /// <summary>
        /// The KAS is connected.
        /// </summary>
        Connected,

        /// <summary>
        /// The KAS is disconnecting or has disconnected. 'Ex' is non-null if
        /// the connection was lost because an error occurred.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// The workspace has logged in.
        /// </summary>
        Login,
        
        /// <summary>
        /// The workspace has logged out normally or the login has failed. 'Ex'
        /// is non-null if the login has failed.
        Logout,

        /// <summary>
        /// A task switch has occurred. 'Task' is set to the task that was
        /// switched to.
        /// </summary>
        TaskSwitch,

        /// <summary>
        /// An application has failed. 'Ex' is set to the exception that
        /// occurred.
        /// </summary>
        AppFailure
    }

    /// <summary>
    /// Notification sent by a workspace state machine.
    /// </summary>
    public class KwsSmNotifEventArgs : EventArgs
    {
        /// <summary>
        /// Type of the notification.
        /// </summary>
        public KwsSmNotif Type;

        /// <summary>
        /// Exception associated to the notification, if any.
        /// </summary>
        public Exception Ex;

        /// <summary>
        /// Task associated to the notification, if any.
        /// </summary>
        public KwsTask Task;

        public KwsSmNotifEventArgs(KwsSmNotif type)
        {
            Type = type;
        }
        public KwsSmNotifEventArgs(KwsSmNotif type, Exception ex)
        {
            Type = type;
            Ex = ex;
        }

        public KwsSmNotifEventArgs(KwsSmNotif type, KwsTask task)
        {
            Type = type;
            Task = task;
        }
    }

    /// <summary>
    /// Information required to rebuild a workspace.
    /// </summary>
    [Serializable]
    public class KwsRebuildInfo
    {
        /// <summary>
        /// True if the events cached in the DB should be deleted.
        /// </summary>
        public bool DeleteCachedEventsFlag = false;

        /// <summary>
        /// True if the local application data should be deleted. 
        /// </summary>
        public bool DeleteLocalDataFlag = false;

        /// <summary>
        /// True if we are rebuilding for a KWM upgrade.
        /// </summary>
        public bool UpgradeFlag = false;
    }

    /// <summary>
    /// Represent a user in a workspace.
    /// </summary>
    [Serializable]
    public class KwsUser
    {
        /// <summary>
        /// ID of the user.
        /// </summary>
        public UInt32 UserID = 0;

        /// <summary>
        /// Date at which the user was added.
        /// </summary>
        public UInt64 InvitationDate = 0;

        /// <summary>
        /// ID of the inviting user. 0 if none.
        /// </summary>
        public UInt32 InvitedBy = 0;

        /// <summary>
        /// Name given by the workspace administrator.
        /// </summary>
        public String AdminName = "";

        /// <summary>
        /// Name given by the user himself.
        /// </summary>
        public String UserName = "";

        /// <summary>
        /// Email address of the user.
        /// </summary>
        public String EmailAddress = "";

        /// <summary>
        /// Powers of the user. 
        /// 1 -> AdminFlag.
        /// </summary>
        public UInt32 Power = 0;

        /// <summary>
        /// Organization name, if the user is a member.
        /// </summary>
        public String OrgName = "";

        /// <summary>
        /// Get the username to display in the UI., without the user's email address (unless no
        /// username exists).
        /// </summary>
        public String UiSimpleName
        {
            get 
            {
                if (AdminName != "") return AdminName;
                if (UserName != "") return UserName;
                return EmailAddress;
            }
        }

        /// <summary>
        /// Get the given name of the user (prénom). If no AdminName or UserName is
        /// set, use the left part of the email address.
        /// </summary>
        public String UiSimpleGivenName
        {
            get
            {
                // Give priority to AdminName.
                String name = AdminName != "" ? AdminName : UserName;

                // Try to get a valid given name.
                name = GetGivenName(name);

                if (name == "")
                    return GetEmailAddrLeftPart(EmailAddress);
                else
                    return name;
            }
        }

        /// <summary>
        /// Return the first characters of the given name until a space
        /// is found.
        /// </summary>
        private String GetGivenName(String name)
        {
            String[] splitted = name.Split(new char[] {' '});
            if (splitted.Length > 0) return splitted[0];
            else return "";
        }

        /// <summary>
        /// Return the left part of an email address, the entire address
        /// if any problem occurs.
        /// </summary>
        private String GetEmailAddrLeftPart(String addr)
        {
            String[] splitted = addr.Split(new char[] { '@' });
            if (splitted.Length > 0) return splitted[0];
            else return addr;
        }

        /// <summary>
        /// Get the username to display in the UI, with its email address appended. If no
        /// username is present, return the email address only.
        /// </summary>
        public String UiFullName
        {
            get 
            {
                if (AdminName == "" && UserName == "") return EmailAddress;

                return UiSimpleName + " (" + EmailAddress + ")";
            }
        }

        /// <summary>
        /// Get the KwsUser description text.
        /// </summary>
        public String UiTooltipText
        {
            get
            {
                if (UiSimpleName == EmailAddress) return EmailAddress;
                
                return UiSimpleName + Environment.NewLine + EmailAddress;
            }
        }

        /// <summary>
        /// Return true if the user has an administrative name set.
        /// </summary>
        public bool HasAdminName()
        {
            return AdminName != "";
        }
    }
}