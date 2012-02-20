using System;
using System.Diagnostics;
using kwm.KwmAppControls;
using System.Data.Common;
using System.Collections.Generic;
using kwm.Utils;
using Microsoft.Win32;
using Tbx.Utils;
using System.Windows.Forms;

/* This file contains miscellaneous stuff related to the workspace manager
 * internals. Create separate files as needed.
 */

namespace kwm
{
    /// <summary>
    /// Main status of the workspace manager.
    /// </summary>
    public enum WmMainStatus
    {
        /// <summary>
        /// The workspace manager is stopped. The KWM can either exit or
        /// start the workspace manager.
        /// </summary>
        Stopped,

        /// <summary>
        /// The workspace manager will switch to the 'stopped' state when
        /// everything has been cleaned up properly.
        /// </summary>
        Stopping,

        /// <summary>
        /// The workspace manager is initialized and operating normally.
        /// </summary>
        Started,

        /// <summary>
        /// The workspace manager is initializing its state.
        /// </summary>
        Starting
    }

    /// <summary>
    /// This class is used by the workspace manager to store information
    /// about a GuiExecRequest that was posted to it.
    /// </summary>
    public class WmGuiExecRequest
    {
        /// <summary>
        /// Reference to the wrapped request.
        /// </summary>
        public GuiExecRequest Request;

        /// <summary>
        /// Reference to the workspace bound to the request, if any.
        /// </summary>
        public Workspace Kws;

        public WmGuiExecRequest(GuiExecRequest request, Workspace kws)
        {
            Request = request;
            Kws = kws;
        }
    }

    /// <summary>
    /// This class contains methods used to manipulate the local database.
    /// </summary>
    public class WmLocalDbBroker
    {
        /// <summary>
        /// Latest version number of the serialized data in the database.
        /// </summary>
        public const UInt32 LatestDbVersion = 2;

        /// <summary>
        /// Reference to the local database.
        /// </summary>
        private WmLocalDb m_db;

        /// <summary>
        /// Transaction currently in progress, if any.
        /// </summary>
        private DbTransaction m_transaction = null;

        public WmLocalDbBroker(WmLocalDb db)
        {
            m_db = db;
        }

        /// <summary>
        /// Return true if a transaction is open.
        /// </summary>
        public bool HasTransaction()
        {
            return (m_transaction != null);
        }

        /// <summary>
        /// Begin a database transaction. It is an error to open a transaction
        /// while another one is already open.
        /// </summary>
        public void BeginTransaction()
        {
            Debug.Assert(!HasTransaction());
            m_transaction = m_db.DbConn.BeginTransaction();
        }

        /// <summary>
        /// Commit the current database transaction.
        /// </summary>
        public void CommitTransaction()
        {
            Debug.Assert(HasTransaction());
            m_transaction.Commit();
            m_transaction = null;
        }

        /// <summary>
        /// Rollback the current database transaction, if any.
        /// </summary>
        public void RollbackTransaction()
        {
            if (!HasTransaction()) return;
            m_transaction.Rollback();
            m_transaction = null;
        }

        /// <summary>
        /// Create or upgrade the database. Throw an exception if the database
        /// version is newer than the version we can handle.
        /// </summary>
        public void InitDb()
        {
            UInt32 version = GetDbVersion();

            if (version == 0)
                CreateSchema();

            else if (version < LatestDbVersion)
                UpgradeDb(version);

            else if (version != LatestDbVersion)
                throw new Exception("this version of the database (" + version + ") cannot be handled by the KWM");
        }

        /// <summary>
        /// Return the version of the serialized data stored in the database.
        /// 0 is returned if the database does not contain serialized data.
        /// </summary>
        public UInt32 GetDbVersion()
        {
            String s = "SELECT name FROM sqlite_master WHERE type='table' AND name='db_version'";
            Object res = m_db.GetCmd(s).ExecuteScalar();
            if (res == null) return 0;
            res = m_db.GetCmd("SELECT version FROM db_version").ExecuteScalar();
            if (res == null) return 0;
            return (UInt32)(Int32)res; // The double cast is required.
        }

        /// <summary>
        /// Update the version number stored in the database.
        /// </summary>
        private void UpdateDbVersion(UInt32 version)
        {
            m_db.ExecNQ("UPDATE db_version SET version = " + version);
        }

        /// <summary>
        /// Create the initial database schema.
        /// </summary>
        private void CreateSchema()
        {
            Logging.Log("Creating database schema.");

            BeginTransaction();

            String s =
                "CREATE TABLE 'db_version' ('version' INT PRIMARY KEY); " +
                "INSERT INTO db_version (version) VALUES (" + LatestDbVersion + "); " +
                "CREATE TABLE 'serialization' ('name' VARCHAR PRIMARY KEY, 'data' BLOB); " +
                "CREATE TABLE 'kws_list' ('kws_id' INT PRIMARY KEY, 'name' VARCHAR); " +
                "CREATE TABLE 'kws_events' ('kws_id' INT, 'evt_id' INT, 'evt_data' BLOB, 'status' INT); " +
                "CREATE UNIQUE INDEX 'events_index_1' ON 'kws_events' ('kws_id', 'evt_id'); " +
                "CREATE UNIQUE INDEX 'events_index_2' ON 'kws_events' ('kws_id', 'status', 'evt_id'); ";
            m_db.ExecNQ(s);

            CommitTransaction();
        }

        /// <summary>
        /// Upgrade the database schema if required.
        /// </summary>
        private void UpgradeDb(UInt32 version)
        {
            Logging.Log("Upgrading database schema from version " + version);

            BeginTransaction();

            if (version == 1)
            {
                m_db.ExecNQ("UPDATE kws_events SET status = " + (UInt32)KwsAnpEventStatus.Unprocessed +
                            " WHERE status = 2");
            }

            UpdateDbVersion(LatestDbVersion);

            CommitTransaction();
        }

        /// <summary>
        /// Return a tree of workspace names indexed by workspace ID.
        /// </summary>
        public SortedDictionary<UInt64, String> GetKwsList()
        {
            try
            {
                DbCommand cmd = m_db.GetCmd("SELECT kws_id, name FROM kws_list");
                DbDataReader reader = cmd.ExecuteReader();
                SortedDictionary<UInt64, String> d = new SortedDictionary<UInt64, String>();
                while (reader.Read()) d[(UInt64)reader.GetInt64(0)] = reader.GetString(1);
                reader.Close();
                return d;
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return null;
            }
        }

        /// <summary>
        /// Remove the specified workspace from the list of workspaces.
        /// </summary>
        public void RemoveKwsFromKwsList(UInt64 id)
        {
            try
            {
                m_db.ExecNQ("DELETE FROM kws_list WHERE kws_id = " + id);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Add the workspace specified to the list of workspaces. 
        /// </summary>
        public void AddKwsToKwsList(UInt64 id, String name)
        {
            try
            {
                DbCommand cmd = m_db.GetCmd("INSERT INTO kws_list (kws_id, name) VALUES (?, ?);");
                m_db.AddParamToCmd(cmd, id);
                m_db.AddParamToCmd(cmd, name);
                cmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Remove the events associated to the workspace specified.
        /// </summary>
        public void RemoveKwsEvents(UInt64 id)
        {
            try
            {
                m_db.ExecNQ("DELETE FROM kws_events WHERE kws_id = " + id);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Store the workspace event specified with the status specified.
        /// </summary>
        public void StoreKwsEvent(UInt64 kwsID, AnpMsg msg, KwsAnpEventStatus status)
        {
            try
            {
                DbCommand cmd = m_db.GetCmd("INSERT INTO kws_events (kws_id, evt_id, evt_data, status) VALUES (?, ?, ?, ?);");
                m_db.AddParamToCmd(cmd, kwsID);
                m_db.AddParamToCmd(cmd, msg.ID);
                m_db.AddParamToCmd(cmd, msg.ToByteArray(true));
                m_db.AddParamToCmd(cmd, status);
                cmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Update the status of the workspace event specified.
        /// </summary>
        public void UpdateKwsEventStatus(UInt64 kwsID, UInt64 msgID, KwsAnpEventStatus status)
        {
            try
            {
                String s = "UPDATE kws_events SET status = " + (UInt32)status +
                           " WHERE kws_id = " + kwsID + " AND evt_id = " + msgID;
                m_db.ExecNQ(s);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Helper method for GetFirstUnprocessedKwsEvent() and GetLastKwsEvent().
        /// </summary>
        private AnpMsg GetKwsEventFromQuery(String s)
        {
            Object res = m_db.GetCmd(s).ExecuteScalar();
            if (res == null) return null;
            AnpMsg m = new AnpMsg();
            m.FromByteArray((byte[])res);
            return m;
        }

        /// <summary>
        /// Get the first unprocessed event of the workspace specified, if any.
        /// </summary>
        public AnpMsg GetFirstUnprocessedKwsEvent(UInt64 kwsID)
        {
            try
            {
                String s = "SELECT evt_data FROM kws_events WHERE kws_id = " + kwsID +
                           " AND status = " + (UInt32)KwsAnpEventStatus.Unprocessed + " ORDER BY evt_id LIMIT 1";
                return GetKwsEventFromQuery(s);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return null;
            }
        }

        /// <summary>
        /// Get the last event of the workspace specified, if any.
        /// </summary>
        public AnpMsg GetLastKwsEvent(UInt64 kwsID)
        {
            try
            {
                String s = "SELECT evt_data FROM kws_events WHERE kws_id = " + kwsID +
                           " AND evt_id = (SELECT max(evt_id) FROM kws_events WHERE kws_id = " + kwsID + ");";
                return GetKwsEventFromQuery(s);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return null;
            }
        }

        public AnpMsg[] GetNewKwsEvent(UInt64 kwsID, UInt64 evtID)
        {
            String s = "SELECT evt_data FROM kws_events WHERE kws_id = " + kwsID +
                       " AND evt_id > " + evtID + " ORDER BY evt_id;";
            List<AnpMsg> res = new List<AnpMsg>();
            DbDataReader reader = m_db.GetCmd(s).ExecuteReader();
            if (reader == null) return null;
            long buflength = 1024;
            byte[] buf = new byte[buflength];
            do
            {
                AnpMsg m = new AnpMsg();
                m.FromByteArray((byte[])reader.GetValue(0));
                res.Add(m);
            } while (reader.Read());
            return res.ToArray();
        }

        /// <summary>
        /// Add/replace the specified object in the serialization table.
        /// </summary>
        public void AddSerializedObject(String name, byte[] data)
        {
            try
            {
                m_db.ExecNQ("DELETE FROM serialization WHERE name = '" + name + "'");
                DbCommand cmd = m_db.GetCmd("INSERT INTO serialization (name, data) VALUES(?, ?)");
                m_db.AddParamToCmd(cmd, name);
                m_db.AddParamToCmd(cmd, data);
                cmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Return the serialized object having the name specified, if any.
        /// </summary>
        public byte[] GetSerializedObject(String name)
        {
            try
            {
                DbCommand cmd = m_db.GetCmd("SELECT data FROM serialization WHERE name = '" + name + "'");
                return (byte[])cmd.ExecuteScalar();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return null;
            }
        }

        /// <summary>
        /// Return true if the serialized object specified exists.
        /// </summary>
        public bool HasSerializedObject(String name)
        {
            try
            {
                return (m_db.GetCmd("SELECT name FROM serialization WHERE name = '" + name + "'").ExecuteScalar() != null);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return false;
            }
        }
    }

    /// <summary>
    /// This class is a pure data store that contains information about the
    /// KAS ANP messages processed by the workspaces.
    /// </summary>
    public class WmKAnpState
    {
        /// <summary>
        /// Number of events to process between each quench check.
        /// </summary>
        public const UInt32 QuenchBatchCount = 100;

        /// <summary>
        /// Rate at which events will be processed, e.g. 1 message per 2
        /// milliseconds.
        /// </summary>
        public const UInt32 QuenchProcessRate = 5;

        /// <summary>
        /// True if event processing is currently quenched.
        /// </summary>
        public bool QuenchFlag = false;

        /// <summary>
        /// Number of events that have been processed in the current batch.
        /// </summary>
        public UInt32 CurrentBatchCount = 0;

        /// <summary>
        /// Date at which the current batch has been started.
        /// </summary>
        public DateTime CurrentBatchStartDate = DateTime.MinValue;

        /// <summary>
        /// ID of the next ANP command message.
        /// </summary>
        public UInt64 NextCmdMsgID = 1;

        /// <summary>
        /// Date at which we last processed an ANP event. This is set to the
        /// current date when a workspace logs in. This is used for legacy
        /// catch-up handling.
        /// </summary>
        public DateTime LastProcessedEventDate = DateTime.MinValue;
    }

    /// <summary>
    /// This class is used to read, update and write the registry values used
    /// by the KWM and KPP-MSO for using the KPS and the KCD.
    /// </summary>
    public class WmWinRegistry
    {
        /// <summary>
        /// Address of the KPS.
        /// </summary>
        public String KpsAddr;

        /// <summary>
        /// Port of the KPS.
        /// </summary>
        public UInt16 KpsPort;

        /// <summary>
        /// User name on the KPS.
        /// </summary>
        public String KpsUserName;

        /// <summary>
        /// Login token returned by the KPS.
        /// </summary>
        public String KpsLoginToken;

        /// <summary>
        /// Powers of the user listed in the last KPS login ticket obtained.
        /// </summary>
        public UInt32 KpsUserPower;

        /// <summary>
        /// Address of the KCD listed in the last KPS login ticket obtained.
        /// </summary>
        public String KcdAddr;

        /// <summary>
        /// Port of the KCD listed in the last KPS login ticket obtained.
        /// </summary>
        public UInt16 KcdPort;

        /// <summary>
        /// True if the wizard must not be shown to the user when the KWM starts.
        /// The wizard should be shown until the user configures his KWM.
        /// </summary>
        public bool NoAutomaticWizardFlag;

        /// <summary>
        /// Fired whenever the registry is written. Used to notify any
        /// interested party that some settings may have changed, such as
        /// Outlook. Event delegates will always be invoked in the UI thread.
        /// </summary>
        public static event EventHandler OnRegistryWritten;

        /// <summary>
        /// Return an instance of this class having the values read from the
        /// registry.
        /// </summary>
        public static WmWinRegistry Spawn()
        {
            WmWinRegistry self = new WmWinRegistry();
            self.ReadRegistry();
            return self;
        }

        /// <summary>
        /// Clear all the fields of this instance (including the WizardFlag). 
        /// Used when the user selects  "No account" in the configuration wizard.
        /// </summary>
        public void Clear()
        {
            KpsAddr = "";
            KpsPort = 443;
            KpsUserName = "";
            KpsLoginToken = "";
            KpsUserPower = 0;
            KcdAddr = "";
            KcdPort = 0;
            NoAutomaticWizardFlag = false;
        }

        /// <summary>
        /// Read the values of this instance from the registry.
        /// </summary>
        public void ReadRegistry()
        {
            RegistryKey regKey = null;
            try
            {
                regKey = GetRegKey();
                KpsAddr = (String)regKey.GetValue("KPS_Address", "");
                KpsPort = (UInt16)(Int32)regKey.GetValue("KPS_Port", 443);
                KpsUserName = (String)regKey.GetValue("Login", "");
                KpsLoginToken = (String)regKey.GetValue("Ticket", "");
                KpsUserPower = (UInt32)(Int32)regKey.GetValue("Kps_User_Power", 0);
                KcdAddr = (String)regKey.GetValue("KCD_Address", "");
                KcdPort = (UInt16)(Int32)regKey.GetValue("KCD_Port", 0);
                NoAutomaticWizardFlag = ((Int32)regKey.GetValue("Config_Status", 0) > 0);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
            finally
            {
                if (regKey != null) regKey.Close();
            }
        }

        /// <summary>
        /// Write the values of this instance to the registry.
        /// </summary>
        public void WriteRegistry()
        {
            RegistryKey regKey = null;
            try
            {
                regKey = GetRegKey();
                regKey.SetValue("KPS_Address", KpsAddr);
                regKey.SetValue("KPS_Port", (Int32)KpsPort);
                regKey.SetValue("Login", KpsUserName);
                regKey.SetValue("Ticket", KpsLoginToken);
                regKey.SetValue("Kps_User_Power", (Int32)KpsUserPower);
                regKey.SetValue("KCD_Address", KcdAddr);
                regKey.SetValue("KCD_Port", (Int32)KcdPort);
                regKey.SetValue("Config_Status", Convert.ToInt32(NoAutomaticWizardFlag));
                DoOnRegistryWritten();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
            finally
            {
                if (regKey != null) regKey.Close();
            }
        }

        private void DoOnRegistryWritten()
        {
            if (OnRegistryWritten == null) return;

            if (Base.InvokeUiControl.InvokeRequired)
            {
                Base.ExecInUI(new MethodInvoker(DoOnRegistryWritten));
                return;
            }

            OnRegistryWritten(this, new EventArgs());            
        }

        /// <summary>
        /// Return true if the registry contains the necessary informations to 
        /// login on a KPS.
        /// </summary>
        public bool CanLoginOnKps()
        {
            return (KpsAddr != "" && KpsPort != 0 && KpsUserName != "" && KpsLoginToken != "");
        }

        /// <summary>
        /// Return true if the user can create a workspace.
        /// </summary>
        public bool CanCreateKws()
        {
            return (CanLoginOnKps() && KcdAddr != "" && KcdPort != 0);
        }

        /// <summary>
        /// Return true if the Wizard should be shown on the KWM startup, false
        /// otherwise.
        /// </summary>
        public bool MustPromptWizardOnStartup()
        {
            RegistryKey regKey = null;
            try
            {
                // Override the absence of the WizardFlag if a login token is present.
                // Legacy plugins that were properly configured before the invention
                // the configuration wizard should not see the wizard.
                regKey = GetRegKey();
                String loginToken = (String)regKey.GetValue("Ticket", "DEFAULT");
                return (loginToken == "DEFAULT" && !NoAutomaticWizardFlag);
            }

            finally
            {
                if (regKey != null) regKey.Close();
            }
        }

        /// <summary>
        /// Open and return the user's KPP configuration registry key, 
        /// creating it if it does not already exist.
        /// </summary>
        private RegistryKey GetRegKey()
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey(Base.GetOtcRegKey());
            if (regKey == null)
                throw new Exception("Unable to read or create the registry key " + Base.GetOtcRegKey() + ".");
            return regKey;
        }
    }

    /// <summary>
    /// Miscellaneous K3P methods.
    /// </summary>
    public static class WmK3pServerInfo
    {
        /// <summary>
        /// Fill the values of the server info specified based on the values of
        /// the registry specified.
        /// </summary>
        public static void RegToServerInfo(WmWinRegistry reg, K3p.kpp_server_info info)
        {
            if (reg.CanLoginOnKps())
            {
                info.kps_login = reg.KpsUserName;
                info.kps_secret = reg.KpsLoginToken;
                info.kps_net_addr = reg.KpsAddr;
                info.kps_port_num = reg.KpsPort;
            }

            else
            {
                info.kps_login = "";
                info.kps_secret = "";
                info.kps_net_addr = "";
                info.kps_port_num = 0;
            }
        }
    }

    /// <summary>
    /// Represent a workspace login ticket.
    /// </summary>
    public class WmLoginTicket
    {
        /// <summary>
        /// Base64 ticket.
        /// </summary>
        public String B64Ticket = null;

        /// <summary>
        /// Binary ticket.
        /// </summary>
        public byte[] BinaryTicket = null;

        /// <summary>
        /// ANP payload contained in the ticket.
        /// </summary>
        public AnpMsg AnpTicket = null;

        public String UserName = "";
        public String EmailAddr = "";
        public String KcdAddr = "";
        public UInt16 KcdPort = 0;
        public UInt64 KpsKeyID = 0;

        /// <summary>
        /// Parse the base64 ticket specified.
        /// </summary>
        public void FromB64Ticket(String b64Ticket)
        {
            B64Ticket = b64Ticket;
            FromBinaryTicket(Convert.FromBase64String(B64Ticket));
        }

        /// <summary>
        /// Parse the binary ticket specified. The base 64 ticket field is not
        /// modified.
        /// </summary>
        public void FromBinaryTicket(byte[] binaryTicket)
        {
            BinaryTicket = binaryTicket;
            if (BinaryTicket.Length < 38) throw new Exception("invalid ticket length");
            int payloadLength = BinaryTicket[37];
            payloadLength |= (BinaryTicket[36] << 8);
            payloadLength |= (BinaryTicket[35] << 16);
            payloadLength |= (BinaryTicket[34] << 24);
            byte[] strippedTicket = new byte[payloadLength];
            for (int i = 0; i < payloadLength; i++) strippedTicket[i] = BinaryTicket[i + 38];

            AnpTicket = new AnpMsg();
            AnpTicket.Elements = AnpMsg.ParsePayload(strippedTicket);
            UserName = AnpTicket.Elements[0].String;
            EmailAddr = AnpTicket.Elements[1].String;
            KcdAddr = AnpTicket.Elements[2].String;
            KcdPort = (UInt16)AnpTicket.Elements[3].UInt32;
            KpsKeyID = AnpTicket.Elements[4].UInt64;
        }
    }

    /// <summary>
    /// Result of a WmLoginTicketQuery.
    /// </summary>
    public enum WmLoginTicketQueryRes
    {
        /// <summary>
        /// Ticket obtained.
        /// </summary>
        OK,

        /// <summary>
        /// The configuration is invalid.
        /// </summary>
        InvalidCfg,

        /// <summary>
        /// A miscellaneous error occurred.
        /// </summary>
        MiscError
    }

    /// <summary>
    /// Method called when a login ticket query has completed.
    /// </summary>
    public delegate void WmLoginTicketQueryDelegate(WmLoginTicketQuery query);

    /// <summary>
    /// Query used to obtain a ticket using the credentials stored in the
    /// registry.
    /// </summary>
    public class WmLoginTicketQuery : KmodQuery
    {
        /// <summary>
        /// Method called when the login ticket query has completed.
        /// </summary>
        public WmLoginTicketQueryDelegate Callback2;

        /// <summary>
        /// Result of the query.
        /// </summary>
        public WmLoginTicketQueryRes Res;

        /// <summary>
        /// Login ticket obtained.
        /// </summary>
        public WmLoginTicket Ticket;

        /// <summary>
        /// Submit the query using the credentials obtained from the registry
        /// specified.
        /// </summary>
        public void Submit(WmKmodBroker broker, WmWinRegistry registry, WmLoginTicketQueryDelegate callback)
        {
            // Set the proper callback.
            Callback2 = callback;
            
            // Fill out the server information.
            K3p.K3pSetServerInfo ssi = new K3p.K3pSetServerInfo();
            WmK3pServerInfo.RegToServerInfo(registry, ssi.Info);

            // Submit the query.
            base.Submit(broker, new K3pCmd[] { ssi, new K3p.kpp_get_kws_ticket() }, AnalyseResults);
        }

        /// <summary>
        /// Update the values of the registry based on the results of the
        /// query, if required. Note: this method does not read the values
        /// from the registry.
        /// </summary>
        public void UpdateRegistry(WmWinRegistry registry)
        {
            if (Res == WmLoginTicketQueryRes.MiscError) return;

            if (Res == WmLoginTicketQueryRes.InvalidCfg)
            {
                registry.KpsLoginToken = "";
                registry.KpsUserPower = 0;
                registry.KcdAddr = "";
                registry.KcdPort = 0;
            }

            else if (Res == WmLoginTicketQueryRes.OK)
            {
                registry.KcdAddr = Ticket.KcdAddr;
                registry.KcdPort = Ticket.KcdPort;
                registry.KpsUserPower = 0;
            }

            registry.WriteRegistry();
        }

        /// <summary>
        /// Analyze the results of the login query and call the callback.
        /// </summary>
        private void AnalyseResults(KmodQuery ignored)
        {
            if (OutMsg is K3p.kmo_invalid_config)
            {
                Res = WmLoginTicketQueryRes.InvalidCfg;
            }

            else if (OutMsg is K3p.kmo_get_kws_ticket)
            {
                try
                {
                    Res = WmLoginTicketQueryRes.OK;
                    Ticket = new WmLoginTicket();
                    Ticket.FromB64Ticket(((K3p.kmo_get_kws_ticket)OutMsg).Ticket);
                }

                catch (Exception ex)
                {
                    Res = WmLoginTicketQueryRes.MiscError;
                    OutDesc = "cannot parse ticket: " + ex.Message;
                }
            }

            else
            {
                Res = WmLoginTicketQueryRes.MiscError;
            }

            Callback2(this);
        }
    }

    /// <summary>
    /// Workspaces and folders to import in the WM when started.
    /// </summary>
    public class WmImportData
    {
        /// <summary>
        /// List of exported workspaces.
        /// </summary>
        public List<ExportedKws> KwsList = new List<ExportedKws>();
        
        /// <summary>
        /// List of folders.
        /// </summary>
        public List<String> FolderList = new List<String>();

        /// <summary>
        /// Clear both the workspace and the folder list.
        /// </summary>
        public void Clear()
        {
            KwsList.Clear();
            FolderList.Clear();
        }

        /// <summary>
        /// Add the content of the other object specified to this object.
        /// </summary>
        public void Add(WmImportData other)
        {
            KwsList.AddRange(other.KwsList);
            FolderList.AddRange(other.FolderList);
        }
    }
}