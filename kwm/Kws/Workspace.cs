using System.Collections.Generic;
using System;
using kwm.KwmAppControls;
using System.Diagnostics;
using System.Runtime.Serialization;
using kwm.Utils;
using kwm.KwmAppControls.AppKfs;
using System.IO;
using System.Xml;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// This class represents a workspace on a KAS.
    /// </summary>
    [Serializable]
    public partial class Workspace : IAppHelper, ISerializable
    {
        /// <summary>
        /// Array of application IDs supported by the workspace.
        /// </summary>
        public static UInt32[] AppIdList = { KAnpType.KANP_NS_CHAT,
                                             KAnpType.KANP_NS_KFS, 
                                             KAnpType.KANP_NS_VNC,
                                             KAnpType.KANP_NS_PB };

        /// <summary>
        /// Reference to the workspace manager owning this workspace.
        /// NonSerialized.
        /// </summary>
        private WorkspaceManager m_wm = null;

        /// <summary>
        /// Reference to the workspace state machine.
        /// NonSerialized.
        /// </summary>
        public KwsStateMachine Sm = null;

        /// <summary>
        /// Number of references to this workspace.
        /// NonSerialized.
        /// </summary>
        public UInt32 RefCount = 0;

        /// <summary>
        /// True if the workspace state has been modified since it has been
        /// serialized. Note: use SetDirty() to set the flag.
        /// NonSerialized.
        /// </summary>
        public bool DirtyFlag = false;

        /// <summary>
        /// Unique identifier assigned to the workspace by the WM. It is
        /// different from the identifier assigned to the workspace by the KAS.
        /// </summary> 
        public UInt64 InternalID = 0;

        /// <summary>
        /// Reference to the KAS associated to this workspace.
        /// </summary>
        public WmKas Kas = null;

        /// <summary>
        /// 'Health' status of the workspace (Good, OnTheWayOut, etc).
        /// </summary>
        public KwsMainStatus MainStatus = KwsMainStatus.NotYetSpawned;

        /// <summary>
        /// KAS ANP state.
        /// </summary>
        public KwsKAnpState KAnpState = new KwsKAnpState();

        /// <summary>
        /// Core workspace data (Credentials, Users and Rebuild info).
        /// </summary>
        public KwsCoreData CoreData = new KwsCoreData();

        /// <summary>
        /// Handle the workspace events received from the KAS that do not
        /// concern the applications.
        /// NonSerialized.
        /// </summary>
        public KwsKasEventHandler KasEventHandler = null;

        /// <summary>
        /// Workspace KAS login handler. 
        /// </summary>
        public KwsKasLoginHandler KasLoginHandler = new KwsKasLoginHandler();

        /// <summary>
        /// Status of the login with the KAS.
        /// NonSerialized.
        /// </summary>
        public KwsLoginStatus LoginStatus = KwsLoginStatus.LoggedOut;

        /// <summary>
        /// Core operation being processed in a workspace, if any.
        /// NonSerialized.
        /// </summary>
        public KwsCoreOp CoreOp = null;

        /// <summary>
        /// Task that the user wants the workspace to be executing. This can
        /// be WorkOnline, WorkOffline or Stop. The state machine will switch
        /// to that task when possible.
        /// </summary>
        public KwsTask UserTask = KwsTask.WorkOnline;

        /// <summary>
        /// Status of the applications of the workspace.
        /// NonSerialized.
        /// </summary>
        public KwsAppStatus AppStatus = KwsAppStatus.Stopped;

        /// <summary>
        /// Last application error that occurred. This field is set to null
        /// when the workspace task is set to WorkOffline or WorkOnline.
        /// </summary>
        public Exception AppException = null;

        /// <summary>
        /// Tree of applications indexed by application ID.
        /// NonSerialized.
        /// </summary>
        public SortedDictionary<UInt32, KwsApp> AppTree = new SortedDictionary<UInt32, KwsApp>();

        public OutlookEventProxy EventProxy = null;

        /// <summary>
        /// Type of handling an event received in a workspace.
        /// </summary>
        /// <param name="msg">The raw anp message</param>
        public delegate void EventReceivedDelegate(Workspace Sender, AnpMsg msg);

        /// <summary>
        /// Event fired whenever an event is received by the workspace.
        /// </summary>
        public event EventReceivedDelegate OnEventReceived;

        public void DoOnEventReceived(AnpMsg msg)
        {
            if (OnEventReceived != null) OnEventReceived(this, msg);
        }

        /// <summary>
        /// Non-deserializing constructor. This constructor can throw.
        /// </summary>
        public Workspace(WorkspaceManager wm, WmKas kas, UInt64 internalID, KwsCredentials creds)
        {
            Kas = kas;
            InternalID = internalID;
            CoreData.Credentials = creds;
            if (creds != null) Debug.Assert(Kas.KasID.CompareTo(creds.KasID) == 0);
            Initialize(wm);
            SetDirty();
        }

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        public Workspace(SerializationInfo info, StreamingContext context)
        {
            Int32 version = info.GetInt32("WorkspaceVersion");
            InternalID = info.GetUInt64("m_internalID");

            // This is an old workspace which needs to be rebuilt.
            if (version < 5)
            {
                FileKwsDeserializer dsr = (FileKwsDeserializer)KwmSpawner.Instance.KwmDsr.CurKwsDsr;
                
                // Get the folder information.
                try
                {
                    dsr.FolderID = UInt64.Parse(info.GetString("m_parentFolderID").Substring(6));
                    dsr.FolderIndex = info.GetInt32("m_indexInFolder");
                }

                catch (Exception)
                {
                }

                // Update the credentials.
                CoreData.Credentials = ((Credentials)info.GetValue("credentials", typeof(Credentials))).newCreds;

                // Set the rebuild information.
                MainStatus = KwsMainStatus.RebuildRequired;
                KwsRebuildInfo rebuildInfo = CoreData.RebuildInfo;
                rebuildInfo.DeleteCachedEventsFlag = true;
                rebuildInfo.DeleteLocalDataFlag = false;
                rebuildInfo.UpgradeFlag = true;

                if (version >= 3)
                {
                    AppKfs appKfs = (AppKfs)info.GetValue("m_appKfs", typeof(AppKfs));
                    appKfs.Share.CompatLastKwsEventID = info.GetUInt64("m_lastEventID");
                    appKfs.Initialize(this);
                    AppTree[KAnpType.KANP_NS_KFS] = appKfs;
                }                
            }

            // Pre-desintermediation and later.
            else
            {
                MainStatus = (KwsMainStatus)info.GetValue("MainStatus", typeof(KwsMainStatus));
                KAnpState = (KwsKAnpState)info.GetValue("KAnpState", typeof(KwsKAnpState));
                CoreData = (KwsCoreData)info.GetValue("CoreData", typeof(KwsCoreData));
                UserTask = (KwsTask)info.GetValue("UserTask", typeof(KwsTask));

                // Post-desintermediation.
                if (version >= 6)
                {
                    KasLoginHandler = (KwsKasLoginHandler)info.GetValue("KasLoginHandler", typeof(KwsKasLoginHandler));
                    AppException = (Exception)info.GetValue("AppException", typeof(Exception));
                }
            }

            // Create a temporary KAS object.
            Kas = new WmKas(CoreData.Credentials.KasID);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("WorkspaceVersion", WorkspaceManager.SerializationVersion); 
            info.AddValue("m_internalID", InternalID);
            info.AddValue("MainStatus", MainStatus);
            info.AddValue("KAnpState", KAnpState);
            info.AddValue("CoreData", CoreData);
            info.AddValue("KasLoginHandler", KasLoginHandler);
            info.AddValue("UserTask", UserTask);
            info.AddValue("AppException", AppException);
        }

        /// <summary>
        /// Initialization code common to both the deserialized and
        /// non-deserialized cases. This method MUST be called explicitly
        /// when the object is deserialized. Note that deserialized
        /// applications are added to the application tree before this method
        /// is called. This method can throw.
        /// </summary>
        public void Initialize(WorkspaceManager wm)
        {
            m_wm = wm;
            Sm = new KwsStateMachine(wm, this);
            KasEventHandler = new KwsKasEventHandler(this);
            KasLoginHandler.SetRef(wm, this);

            // Create the missing applications when the workspace is created
            // or when some applications have not been deserialized properly.
            CreateMissingApp();

            // Create the workspace state directory.
            Directory.CreateDirectory(KwsRoamingStatePath);

            // Create the workspace state directory.
            Directory.CreateDirectory(KwsLocalStatePath);
        }

        /// <summary>
        /// Increment the reference count of the workspace.
        /// </summary>
        public void AddRef()
        {
            RefCount++;
        }

        /// <summary>
        /// Decrement the reference count of the workspace.
        /// </summary>
        public void ReleaseRef()
        {
            Debug.Assert(RefCount > 0);
            RefCount--;
            Sm.HandleReleaseRef();
        }

        /// <summary>
        /// Mark the workspace and the WM dirty.
        /// </summary>
        public void SetDirty()
        {
            DirtyFlag = true;
            m_wm.SetDirty();
        }

        /// <summary>
        /// Return the application having the ID specified, if any.
        /// </summary>
        public KwsApp GetApp(UInt32 id)
        {
            if (AppTree.ContainsKey(id)) return AppTree[id];
            return null;
        }

        /// <summary>
        /// Store the event specified in the database with the status specified.
        /// </summary>
        public void StoreEventInDb(AnpMsg msg, KwsAnpEventStatus status)
        {
            m_wm.LocalDbBroker.StoreKwsEvent(InternalID, msg, status);
        }

        /// <summary>
        /// Get the first unprocessed event from the database, if any.
        /// </summary>
        public AnpMsg GetFirstUnprocessedEventInDb()
        {
            return m_wm.LocalDbBroker.GetFirstUnprocessedKwsEvent(InternalID);
        }

        /// <summary>
        /// Get the last event stored in the database, if any.
        /// </summary>
        public AnpMsg GetLastEventInDb()
        {
            return m_wm.LocalDbBroker.GetLastKwsEvent(InternalID);
        }

        /// <summary>
        /// Update the status of the event specified in the database.
        /// </summary>
        public void UpdateEventStatusInDb(UInt64 msgID, KwsAnpEventStatus status)
        {
            m_wm.LocalDbBroker.UpdateKwsEventStatus(InternalID, msgID, status);
        }

        /// <summary>
        /// Delete all the events associated to the workspace from the database.
        /// </summary>
        public void DeleteEventsFromDb()
        {
            m_wm.LocalDbBroker.RemoveKwsEvents(InternalID);
        }

        public AnpMsg[] GetNewEvents(UInt64 LastEvtIDReceived)
        {
            return m_wm.LocalDbBroker.GetNewKwsEvent(InternalID, LastEvtIDReceived);
        }

        /// <summary>
        /// This method creates the applications that are not present in 
        /// AppTree.
        /// </summary>
        private void CreateMissingApp()
        {
            if (!AppTree.ContainsKey(KAnpType.KANP_NS_CHAT))
                AppTree[KAnpType.KANP_NS_CHAT] = new AppChatBox(this);

            if (!AppTree.ContainsKey(KAnpType.KANP_NS_KFS))
                AppTree[KAnpType.KANP_NS_KFS] = new AppKfs(this);

            if (!AppTree.ContainsKey(KAnpType.KANP_NS_VNC))
                AppTree[KAnpType.KANP_NS_VNC] = new AppScreenSharing(this);

            if (!AppTree.ContainsKey(KAnpType.KANP_NS_PB))
                AppTree[KAnpType.KANP_NS_PB] = new AppPublicBridge(this);
        }


        ///////////////////////////////////////////
        // Interface methods for state machines. //
        ///////////////////////////////////////////

        /// <summary>
        /// Return true if the workspace is in the KAS connect tree.
        /// </summary>
        public bool InKasConnectTree()
        {
            return Kas.KwsConnectTree.ContainsKey(InternalID);
        }

        /// <summary>
        /// Add this workspace to the KAS connect tree if it is not already
        /// there
        /// </summary>
        public void AddToKasConnectTree()
        {
            if (!InKasConnectTree()) Kas.KwsConnectTree[InternalID] = this;
        }

        /// <summary>
        /// Remove this workspace from the KAS connect tree if it is there.
        /// </summary>
        public void RemoveFromKasConnectTree()
        {
            if (InKasConnectTree()) Kas.KwsConnectTree.Remove(InternalID);
        }

        /// <summary>
        /// Return true if the workspace is in the workspace deletion tree.
        /// </summary>
        public bool InKwsDeleteTree()
        {
            return m_wm.KwsDeleteTree.ContainsKey(InternalID);
        }

        /// <summary>
        /// Add this workspace to the workspace deletion tree if it is not
        /// already there.
        /// </summary>
        public void AddToKwsDeleteTree()
        {
            if (!InKwsDeleteTree()) m_wm.KwsDeleteTree[InternalID] = this;
        }

        /// <summary>
        /// Remove this workspace from the workspace deletion tree if it is
        /// there.
        /// </summary>
        public void RemoveFromKwsDeleteTree()
        {
            if (InKwsDeleteTree()) m_wm.KwsDeleteTree.Remove(InternalID);
        }

        /// <summary>
        /// Start the applications if they are stopped.
        /// </summary>
        public void StartApp()
        {
            if (AppStatus != KwsAppStatus.Stopped) return;

            AppStatus = KwsAppStatus.Starting;

            // Applications have an implicit reference to the workspace when
            // they aren't stopped.
            AddRef();

            // Try to start the applications, until all applications are started
            // or a request to stop the applications is received.
            foreach (KwsApp app in AppTree.Values)
            {
                try
                {
                    if (AppStatus != KwsAppStatus.Starting) break;
                    Debug.Assert(app.AppStatus == KwsAppStatus.Stopped);
                    app.AppStatus = KwsAppStatus.Starting;
                    app.RequestStart();
                }

                catch (Exception ex)
                {
                    Sm.HandleAppFailure(app, ex);
                    return;
                }

                // Required if there are no applications.
                Sm.OnAppStarted();
            }
        }

        /// <summary>
        /// Stop the applications if they are starting / started.
        /// </summary>
        public void StopApp()
        {
            if (AppStatus == KwsAppStatus.Stopped || AppStatus == KwsAppStatus.Stopping) return;

            try
            {
                AppStatus = KwsAppStatus.Stopping;
                foreach (KwsApp app in AppTree.Values)
                {
                    if (app.AppStatus == KwsAppStatus.Started || app.AppStatus == KwsAppStatus.Starting)
                        app.AppStatus = KwsAppStatus.Stopping;
                    app.RequestStop();
                }
                Sm.OnAppStopped();
            }

            // We cannot handle applications that won't stop.
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// This method normalizes the state of the applications when the WM
        /// is starting or a workspace is created.
        /// </summary>
        public void NormalizeAppState()
        {
            foreach (KwsApp app in AppTree.Values)
            {
                try
                {
                    app.NormalizeState();
                }

                catch (Exception ex)
                {
                    Sm.HandleAppFailure(app, ex);
                    return;
                }
            }
        }

        /// <summary>
        /// Delete all the data related to the workspace.
        /// </summary>
        public void DeleteAllData()
        {
            foreach (KwsApp app in AppTree.Values) app.DeleteAllData();
            DeleteNonAppDbData();
        }

        /// <summary>
        /// Delete all the workspace data stored in the database.
        /// </summary>
        public void DeleteDbData()
        {
            foreach (KwsApp app in AppTree.Values) app.DeleteDbData();
            DeleteNonAppDbData();
        }

        /// <summary>
        /// Delete non-application-related database data.
        /// </summary>
        private void DeleteNonAppDbData()
        {
            DeleteEventsFromDb();
            m_wm.LocalDbBroker.RemoveKwsFromKwsList(InternalID);
        }

        /// <summary>
        /// Request the UI to update its information about the workspace if the
        /// workspace is selected. If 'kwsListFlag' is true, the browser 
        /// and Outlook information will also be refreshed.
        /// </summary>
        public void StateChangeUpdate(bool kwsListFlag)
        {
            if (kwsListFlag)
            {
                m_wm.UiBroker.RequestBrowserUiUpdate(false);
                m_wm.OutlookBroker.OnKwsListChanged();
            }

            m_wm.UiBroker.RequestKwsUiUpdateIfSelected(this);
        }

        
        /////////////////////////////////////////////
        // Interface methods for external parties. //
        /////////////////////////////////////////////

        // Workspace listeners.
        public event EventHandler<KwsSmNotifEventArgs> OnKwsSmNotif;
        public event EventHandler<EventArgs> OnKwsStatusChanged;
        public event EventHandler<OnKwsUserChangedEventArgs> OnKwsUserChanged;
    
        /// <summary>
        /// Path to the directory containing the workspace state.
        /// </summary>
        public String KwsRoamingStatePath
        {
            get { return Misc.GetKwmRoamingStatePath() + "workspaces\\" + InternalID + "\\"; }
        }

        public String KwsLocalStatePath
        {
            get { return Misc.GetKwmLocalStatePath() + "workspaces\\" + InternalID + "\\"; }
        }

        public void FireKwsSmNotif(KwsSmNotifEventArgs notif)
        {
            try
            {
                if (OnKwsSmNotif != null) OnKwsSmNotif(this, notif);
            }

            catch (Exception ex)
            {
                // We cannot handle failures in immediate notifications.
                Base.HandleException(ex, true);
            }
        }

        public void FireKwsStatusChanged()
        {
            if (OnKwsStatusChanged != null) OnKwsStatusChanged(this, null);
        }

        public void FireKwsUserChanged(UInt32 userID)
        {
            if (OnKwsUserChanged != null)
                OnKwsUserChanged(this, new OnKwsUserChangedEventArgs(userID));
        }

        /// <summary>
        /// Return true if the workspace should be displayed in the workspace browser.
        /// </summary>
        public bool IsDisplayable()
        {
            return (MainStatus == KwsMainStatus.Good || MainStatus == KwsMainStatus.RebuildRequired);
        }

        public AnpMsg NewKAnpMsg(UInt32 type)
        {
            return m_wm.NewKAnpCmd(Kas.MinorVersion, type);
        }
        
        public AnpMsg NewKAnpCmd(UInt32 type)
        {
            AnpMsg msg = NewKAnpMsg(type);
            msg.AddUInt64(CoreData.Credentials.ExternalID);
            return msg;
        }

        /// <summary>
        /// Helper for PostNonAppKasQuery() and PostKasQuery().
        /// </summary>
        private WmKasQuery PostKasQueryInternal(AnpMsg cmd, Object[] metaData, KasQueryDelegate callback, KwsApp app,
                                                bool clearOnLogoutFlag)
        {
            WmKasQuery query = new WmKasQuery(cmd, metaData, callback, Kas, this, app, clearOnLogoutFlag);
            m_wm.Sm.PostKasQuery(query);
            return query;
        }

        /// <summary>
        /// Post a non-application related KAS query.
        /// </summary>
        public WmKasQuery PostNonAppKasQuery(AnpMsg cmd, Object[] metaData, KasQueryDelegate callback,
                                             bool clearOnLogoutFlag)
        {
            return PostKasQueryInternal(cmd, metaData, callback, null, clearOnLogoutFlag);
        }

        public KasQuery PostAppKasQuery(AnpMsg cmd, Object[] metaData, KasQueryDelegate callback, KwsApp app)
        {
            Debug.Assert(app != null);
            return PostKasQueryInternal(cmd, metaData, callback, app, true); 
        }

        public void PostGuiExecRequest(GuiExecRequest req)
        {
            m_wm.Sm.PostGuiExecRequest(req, this);
        }

        public IAnpTunnel CreateTunnel()
        {
            return new AnpTunnel(Kas.KasID.Host, Kas.KasID.Port);
        }

        public void ActivateKws()
        {
            m_wm.UiBroker.RequestSelectKws(this, true);
        }

        public void SelectUser(UInt32 userID)
        {
            m_wm.UiBroker.RequestSelectUser(this, userID, true);
        }

        public void NotifyUser(NotificationItem item)
        {
            if (!HasCaughtUpWithKasEvents()) item.WantShowPopup = false;

            m_wm.UiBroker.NotifyUser(item);
        }

        public void SetUserPwd(UInt32 userID, String newPassword)
        {
            KwsSetUserProperty op = new KwsSetUserProperty(m_wm, this, userID, newPassword);
            op.PerformQuery();
        }

        public void SetAppDirty(KwsApp app, String reason)
        {
            SetDirty();
        }

        public WmLocalDb GetLocalDb()
        {
            return m_wm.LocalDb;
        }

        public UInt64 GetInternalKwsID()
        {
            return InternalID;
        }

        public ulong GetExternalKwsID()
        {
            return CoreData.Credentials.ExternalID;
        }

        public bool IsPublicKws()
        {
            return CoreData.Credentials.PublicFlag;
        }

        public string GetKwsName()
        {
            return CoreData.Credentials.KwsName;
        }

        public string GetKwsUniqueName()
        {
            return GetKwsName() + " (" + InternalID + ")";
        }
       
        public bool IsKwsSelected()
        {
            return (m_wm.UiBroker.Browser.SelectedKws == this);
        }

        public string GetUserDisplayName(UInt32 userID)
        {
            if (userID == 0) return "Administrator";
            KwsUser u = CoreData.UserInfo.GetUserByID(userID);
            if (u == null) return "";
            if (u.AdminName != "") return u.AdminName;
            if (u.UserName != "") return u.UserName;
            return u.EmailAddress;
        }

        public string GetUserEmail(UInt32 userID)
        {
            KwsUser u = CoreData.UserInfo.GetUserByID(userID);
            return (u == null ? "" : u.EmailAddress);
        }

        public uint GetKwmUserID()
        {
            return CoreData.Credentials.UserID;
        }

        public KwsUser GetKwmUser()
        {
            return CoreData.UserInfo.GetUserByID(GetKwmUserID());
        }

        public void OnAppStarted(KwsApp app)
        {
            if (app.AppStatus != KwsAppStatus.Starting)
            {
                Logging.Log(2, "OnAppStarted() called when app not starting");
                return;
            }
            
            app.AppStatus = KwsAppStatus.Started;
            Sm.OnAppStarted();
        }

        public void OnAppStopped(KwsApp app)
        {
            if (app.AppStatus != KwsAppStatus.Stopping && app.AppStatus != KwsAppStatus.Stopped)
            {
                Logging.Log(2, "OnAppStopped() called when app not stopping/stopped");
                return;
            }

            app.AppStatus = KwsAppStatus.Stopped;
            Sm.OnAppStopped();
        }

        public void HandleAppFailure(KwsApp app, Exception ex)
        {
            Sm.HandleAppFailure(app, ex);
        }

        /// <summary>
        /// Return the current run level of the workspace (Stopped, Offline, Online).
        /// </summary>
        public KwsRunLevel GetRunLevel() { return Sm.GetRunLevel(); }

        /// <summary>
        /// Return true if the workspace has a level of functionality greater
        /// or equal to the offline mode.
        /// </summary>
        public bool IsOfflineCapable() { return GetRunLevel() >= KwsRunLevel.Offline; }

        /// <summary>
        /// Return true if the workspace has a level of functionality equal to
        /// the online mode.
        /// </summary>
        public bool IsOnlineCapable() { return GetRunLevel() == KwsRunLevel.Online; }

        /// <summary>
        /// Return true if the KWM has caught up with the events of the 
        /// workspace stored on the KAS. 
        /// </summary>
        public bool HasCaughtUpWithKasEvents() { return KAnpState.CaughtUpFlag; }
    }

    /// <summary>
    /// Represent an exported Workspace.
    /// </summary>
    public class ExportedKws
    {
        /// <summary>
        /// Exported version of the credentials.
        /// </summary>
        public UInt32 ExportedVersion = KwsCredentials.Version;

        /// <summary>
        /// Credentials of the workspace.
        /// </summary>
        public KwsCredentials Creds;

        /// <summary>
        /// Full path of the exported workspace's parent folder.
        /// </summary>
        public String FolderPath;

        public ExportedKws() :
            this (new KwsCredentials(), "")
        {
        }

        public ExportedKws(Workspace kws, String folderPath) : 
            this(new KwsCredentials(kws.CoreData.Credentials), folderPath)
        {
        }

        public ExportedKws(KwsCredentials creds, String folderPath)
        {
            Creds = creds;
            FolderPath = folderPath;
        }

        /// <summary>
        /// Add the ExportedKws object to the given XmlElement.
        /// </summary>
        public void ToXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement kws = doc.CreateElement("Kws");
            kws.SetAttribute("version", ExportedVersion.ToString());
            parent.AppendChild(kws);

            Creds.KasID.ExportToXml(doc, kws);

            Misc.CreateXmlElement(doc, kws, "ExternalID", Creds.ExternalID.ToString());
            Misc.CreateXmlElement(doc, kws, "EmailID", Creds.EmailID);
            Misc.CreateXmlElement(doc, kws, "KwsName", Creds.KwsName);
            Misc.CreateXmlElement(doc, kws, "UserName", Creds.UserName);
            Misc.CreateXmlElement(doc, kws, "UserEmailAddress", Creds.UserEmailAddress);
            Misc.CreateXmlElement(doc, kws, "InviterName", Creds.InviterName);
            Misc.CreateXmlElement(doc, kws, "InviterEmailAddress", Creds.InviterEmailAddress);
            Misc.CreateXmlElement(doc, kws, "AdminFlag", Creds.AdminFlag.ToString());
            Misc.CreateXmlElement(doc, kws, "PublicFlag", Creds.PublicFlag.ToString());
            Misc.CreateXmlElement(doc, kws, "SecureFlag", Creds.SecureFlag.ToString());
            Misc.CreateXmlElement(doc, kws, "UserID", Creds.UserID.ToString());
            Misc.CreateXmlElement(doc, kws, "Pwd", Creds.Pwd);
            Misc.CreateXmlElement(doc, kws, "Ticket", Convert.ToBase64String((Creds.Ticket == null) ? new byte[0] : Creds.Ticket));
            Misc.CreateXmlElement(doc, kws, "KwmoAddress", Creds.KwmoAddress);
            Misc.CreateXmlElement(doc, kws, "FolderPath", FolderPath);
        }

        /// <summary>
        /// Convert the Xml representation of a ExportedKws object to its
        /// ExportedKws object equivalent.
        /// </summary>
        public static ExportedKws FromXml(XmlElement parent)
        {
            ExportedKws kws = new ExportedKws();
            kws.ExportedVersion = UInt32.Parse(parent.GetAttribute("version"));
            if (kws.ExportedVersion > 3)
                throw new Exception("Unsupported Kws version ('" + kws.ExportedVersion + "').");

            XmlElement kasIDElem = Misc.GetXmlChildElement(parent, "KasID");
            if (kasIDElem == null) throw new Exception("KasID element not present");
            kws.Creds.KasID = KasIdentifier.FromXml(kasIDElem);

            kws.Creds.ExternalID = UInt64.Parse(Misc.GetXmlChildValue(parent, "ExternalID", "0"));
            kws.Creds.EmailID = Misc.GetXmlChildValue(parent, "EmailID", "");
            kws.Creds.KwsName = Misc.GetXmlChildValue(parent, "KwsName", "");
            kws.Creds.UserName = Misc.GetXmlChildValue(parent, "UserName", "");
            kws.Creds.UserEmailAddress = Misc.GetXmlChildValue(parent, "UserEmailAddress", "");
            kws.Creds.InviterName = Misc.GetXmlChildValue(parent, "InviterName", "");
            kws.Creds.InviterEmailAddress = Misc.GetXmlChildValue(parent, "InviterEmailAddress", "");
            kws.Creds.AdminFlag = bool.Parse(Misc.GetXmlChildValue(parent, "AdminFlag", "False"));
            kws.Creds.PublicFlag = bool.Parse(Misc.GetXmlChildValue(parent, "PublicFlag", "False"));
            kws.Creds.SecureFlag = bool.Parse(Misc.GetXmlChildValue(parent, "SecureFlag", "True"));
            kws.Creds.UserID = UInt32.Parse(Misc.GetXmlChildValue(parent, "UserID", "0"));
            kws.Creds.Pwd = Misc.GetXmlChildValue(parent, "Pwd", "");
            kws.Creds.Ticket = Convert.FromBase64String(Misc.GetXmlChildValue(parent, "Ticket", ""));
            kws.Creds.KwmoAddress = Misc.GetXmlChildValue(parent, "KwmoAddress", "");
            kws.FolderPath = Misc.GetXmlChildValue(parent, "FolderPath", "");

            // Normalize the ticket value.
            if (kws.Creds.Ticket != null && kws.Creds.Ticket.Length == 0) kws.Creds.Ticket = null;

            return kws;
        }
    }
}