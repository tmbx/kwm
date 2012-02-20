using System.Collections;
using System;
using System.Runtime.Serialization;

// Don't try to move that file in another project. That'll cause runtime
// deserialization failures.
namespace kwm
{
    // ************************
    // Classes removed in v5
    // ************************

    [Serializable]
    public class WorkspaceFolderList : ArrayList
    {
        public UInt64 m_folderID;
    }

    [Serializable]
    public class WorkspaceFolder
    {
        public UInt64 m_id;
        public String m_text;
        public bool m_isExpanded;
    }

    public partial class Workspace
    {
        [Serializable]
        public class Credentials : ISerializable
        {
            public enum CredType
            {
                Creator,
                Member,
                NonMember
            }

            public string Host = "";
            public int Port = 0;
            public UInt64 WorkspaceID = 0;
            public string WorkspaceName = "";
            public string UserName = "";
            public string UserSmtp = "";
            public byte[] Nonce = null;
            public CredType Type = CredType.Creator;
            public String Password = "";
            public byte[] Ticket = null;
            public bool IsAdmin = false;
            public UInt32 UserID = 0;
            public bool IsPublic = false;

            public KwsCredentials newCreds = new KwsCredentials();

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
            }

            public Credentials(SerializationInfo info, StreamingContext context)
            {
                int version = 0;
                
                try
                {
                    version = info.GetInt32("version");
                }

                catch { }

                if (version == 0)
                {
                    Host = info.GetString("m_Host");
                    Port = info.GetInt32("m_Port");
                    WorkspaceID = info.GetUInt64("m_ID");
                    WorkspaceName = info.GetString("m_Name");
                    UserName = info.GetString("m_UserName");
                    UserSmtp = info.GetString("m_UserSmtp");
                    Nonce = (byte[])info.GetValue("m_Nonce", (new byte[] { }).GetType());
                    Type = (Credentials.CredType)Enum.Parse(typeof(CredType), info.GetString("m_type"));
                    Password = info.GetString("m_pwd");
                    Ticket = (byte[])info.GetValue("m_ticket", (new byte[] { }).GetType());
                    IsAdmin = info.GetBoolean("m_IsAdmin");
                    UserID = info.GetUInt32("m_UserId");
                }

                else
                {
                    Host = info.GetString("Host");
                    Port = info.GetInt32("Port");
                    WorkspaceID = info.GetUInt64("ID");
                    WorkspaceName = info.GetString("Name");
                    UserName = info.GetString("UserName");
                    UserSmtp = info.GetString("UserSmtp");
                    Nonce = (byte[])info.GetValue("Nonce", (new byte[] { }).GetType());
                    Type = (Credentials.CredType)Enum.Parse(typeof(CredType), info.GetString("Type"));
                    Password = info.GetString("Password");
                    Ticket = (byte[])info.GetValue("Ticket", (new byte[] { }).GetType());
                    IsAdmin = info.GetBoolean("IsAdmin");
                    UserID = info.GetUInt32("UserID");
                    if (version == 2) IsPublic = info.GetBoolean("IsPublic");
                }

                newCreds.KasID = new KasIdentifier(Host, (UInt16)Port);
                newCreds.ExternalID = WorkspaceID;
                newCreds.KwsName = WorkspaceName;
                newCreds.UserName = UserName;
                newCreds.UserEmailAddress = UserSmtp;
                newCreds.AdminFlag = IsAdmin;
                newCreds.PublicFlag = IsPublic;
                newCreds.UserID = UserID;
                newCreds.Ticket = Ticket;
                newCreds.Pwd = Password;
            }
        }
    }
}