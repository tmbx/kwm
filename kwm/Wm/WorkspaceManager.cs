using System.Collections.Generic;
using System;
using kwm.Utils;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using kwm.KwmAppControls;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections;
using System.Runtime.Serialization.Formatters.Soap;
using System.Windows.Forms;
using Microsoft.Win32;
using Tbx.Utils;


/**************** KWM DESIGN INFORMATION NOTES ***************
 * 
 * The KWM is a relatively complex application that has to deal with
 * some non-trivial scalability and synchronization issues. In order to
 * the keep the complexity manageable, the following design has been used.
 * 
 * 
 * The WorkspaceManager class contains the top-level logic of the
 * application. It contains the workspace list and knows how to initialize &
 * terminate the application and how to deal with errors.
 * 
 * The Workspace class contains the information related to a particular 
 * workspace.
 * 
 * The WorkspaceManager and the Workspace classes are tightly coupled. They
 * know the internals of each other and cooperate closely to get the work done.
 * 
 * Both the WorkspaceManager and the Workspace classes use a state machine
 * to manage their state. The state machines contain the logic that dictates
 * the actions to take when some event occurs, such as the failure of a KAS
 * server. The main benefits of using state machines is that the logic is
 * regrouped in one place, making it easier to analyze the behavior of the
 * program.
 *
 * The WorkspaceManager and the Workspace classes expose their state
 * publically. For instance, the WorkspaceManager contains a public tree of
 * workspaces. This allows the state machines to access and modify this data
 * directly.
 * 
 * To keep things simple, there are two conventions that must be respected:
 * - Keep the core logic buried in the state machines and their helpers.
 * - Don't modify the core public data members of the WorkspaceManager and the
 *   Workspace classes outside these two classes, their state machines and their
 *   helpers.
 * 
 * 
 * Windows guarantees that only one thread is executing in the context of the UI
 * at any given moment. This means that there is no need for explicit
 * synchronization when a thread is executing in the context of the UI; it can
 * manipulate UI objects and access the state of most of the objects of the KWM.
 * 
 * The messages posted to the UI by the worker threads are delivered to a thread
 * executing in UI context when no other thread is executing in the UI context.
 * These messages are guaranteed to be received in the order they are posted.
 * 
 * One thing to keep in mind is that the UI context is reentrant. When a thread
 * executing in UI context calls something like MessageBox.Show(), it
 * relinquishes the control of the UI until the user causes the method to
 * return. In the mean time, the thread can re-enter the UI context to handle
 * other events like timer events.
 * 
 * Writing reentrant code is tricky and the state machines are NOT reentrant.
 * Therefore, it is critically important to never call things like
 * MessageBox.Show() from any method that is called from a state machine.
 * 
 * The state machines invoke the methods of the applications to start/stop
 * the applications, notify them that their state has changed and to deliver
 * events to them.
 * 
 * If an application needs to reenter the UI while it is executing in the
 * context of a state machine, it must post a GUI execution request to the
 * workspace manager. The request includes a method to invoke. The workspace
 * manager will call this method outside the state machine. It is the
 * responsability of the application to revalidate the context that led to the
 * generation of the request; the state of the application may have changed by
 * the time the request is executed.
 */

namespace kwm
{
    /// <summary>
    /// This class implements the KWM application logic.
    /// </summary>
    [Serializable]
    public class WorkspaceManager : ISerializable
    {
        /// <summary>
        /// Current serialization version of the KWM.
        /// </summary>
        public const Int32 SerializationVersion = 6;

        /// <summary>
        /// Reference to the local SQLite database.
        /// NonSerialized.
        /// </summary>
        public WmLocalDb LocalDb = null;

        /// <summary>
        /// Reference to the local SQLite database broker.
        /// NonSerialized.
        /// </summary>
        public WmLocalDbBroker LocalDbBroker = null;

        /// <summary>
        /// Reference to the workspace manager state machine.
        /// NonSerialized.
        /// </summary>
        public WmStateMachine Sm = null;

        /// <summary>
        /// KAS ANP processing state.
        /// NonSerialized.
        /// </summary>
        public WmKAnpState KAnpState = new WmKAnpState();

        /// <summary>
        /// This broker manages the interaction with Outlook.
        /// NonSerialized.
        /// </summary>
        public WmOutlookBroker OutlookBroker = null;

        /// This broker manages the interaction with KMOD.
        /// NonSerialized.
        public WmKmodBroker KmodBroker = null;

        /// <summary>
        /// Broker handling interactions between the WM and the UI.
        /// NonSerialized.
        /// </summary>
        public WmUiBroker UiBroker = null;

        /// <summary>
        /// Tree of KASes indexed by KAS identifier.
        /// </summary>
        public SortedDictionary<KasIdentifier, WmKas> KasTree = new SortedDictionary<KasIdentifier, WmKas>();

        /// <summary>
        /// Tree of workspaces indexed by internal ID.
        /// NonSerialized.
        /// </summary>
        public SortedDictionary<UInt64, Workspace> KwsTree = new SortedDictionary<UInt64, Workspace>();

        /// <summary>
        /// Tree of workspaces that are being deleted indexed by internal ID.
        /// NonSerialized.
        /// </summary>
        public SortedDictionary<UInt64, Workspace> KwsDeleteTree = new SortedDictionary<UInt64, Workspace>();

        /// <summary>
        /// Main status of the workspace manager.
        /// NonSerialized.
        /// </summary>
        public WmMainStatus MainStatus = WmMainStatus.Stopped;

        /// <summary>
        /// True if the WM state has been modified since it has been
        /// serialized. Note: use SetDirty() to set the flag.
        /// NonSerialized.
        /// </summary>
        public bool DirtyFlag = false;

        /// <summary>
        /// Internal ID of the public workspace, if any. This is 0 if no public
        /// workspace exists.
        /// </summary>
        public UInt64 PublicKwsID = 0;

        /// <summary>
        /// Internal ID that should be given to the next workspace.
        /// </summary>
        public UInt64 NextKwsInternalId = 1;

        /// <summary>
        /// Workspaces and folders to import in the WM when initialized.
        /// NonSerialized.
        /// </summary>
        public WmImportData ImportData = new WmImportData();

        /// <summary>
        /// Non-deserializing constructor.
        /// </summary>
        public WorkspaceManager()
        {
        }

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        public WorkspaceManager(SerializationInfo info, StreamingContext context)
        {
            Int32 version = info.GetInt32("version");
            Logging.Log("Deserializing WorkspaceManager from version " + version + ".");

            // Handle old folder list.
            if (version == 3 || version == 4)
            {
                FileKwmDeserializer dsr = (FileKwmDeserializer)KwmSpawner.Instance.KwmDsr;
                dsr.FolderList = (kwm.WorkspaceFolderList)info.GetValue("m_wsFolders", typeof(kwm.WorkspaceFolderList));;
            }

            // If we're deserializing version >= 5, trust the serialized
            // workspace IDs, otherwise reset from 0.
            if (version >= 5)
            {
                PublicKwsID = info.GetUInt64("PublicKwsID");
                NextKwsInternalId = info.GetUInt64("NextKwsInternalId");

                // Deserialize the workspace browser.
                KwmSpawner.Instance.KwmDsr.Browser =
                    (SerializedKwsBrowser)info.GetValue("KwsBrowser", typeof(SerializedKwsBrowser));
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", SerializationVersion);
            info.AddValue("KasTree", KasTree);
            info.AddValue("PublicKwsID", PublicKwsID);
            info.AddValue("NextKwsInternalId", NextKwsInternalId);
            info.AddValue("KwsBrowser", UiBroker.Browser.Serialize());
        }

        /// <summary>
        /// Initialization code common to both the deserialized and
        /// non-deserialized cases. This must be called in the WM spawner.
        /// </summary>
        public void Initialize(WmLocalDb localDb)
        {
            LocalDb = localDb;
            LocalDbBroker = new WmLocalDbBroker(LocalDb);
            Sm = new WmStateMachine(this);
            OutlookBroker = new WmOutlookBroker(this);
            OutlookBroker.OnThreadCollected += OnThreadCollected;
            KmodBroker = new WmKmodBroker();
            KmodBroker.OnThreadCollected += OnThreadCollected;
            UiBroker = new WmUiBroker(this);

            // Create or upgrade the database.
            LocalDbBroker.InitDb();

            // Open the lingering database transaction.
            LocalDbBroker.BeginTransaction();
        }

        /// <summary>
        /// Called when the main form is loaded, meaning the Windows message loop
        /// is properly started by the Application.Run() call. This is required since
        /// a couple of components require a valid MainForm for marshalling calls to the
        /// UI thread.
        /// </summary>
        public void OnMainFormLoaded(object sender, EventArgs args)
        {
            // Start the workspace manager. This is the real entry point of 
            // our software.
            Sm.RequestStart();

            // Once the state machines are running, check if we need to show the 
            // configuration wizard. This will show the modal wizard if needed.
            WmWinRegistry reg = WmWinRegistry.Spawn();
            if (MainStatus == WmMainStatus.Started && reg.MustPromptWizardOnStartup())
            {
                UiBroker.RequestShowMainForm();
                UiBroker.ShowConfigWizard();
            }
        }

        /// <summary>
        /// Called when a thread of the KWM has been collected. This is used to
        /// detect when it is safe to quit.
        /// </summary>
        private void OnThreadCollected(Object sender, EventArgs args)
        {
            if (Sm.StopFlag) Sm.RequestRun("KWM thread collected");
        }

        /// <summary>
        /// Mark the WM dirty.
        /// </summary>
        public void SetDirty()
        {
            DirtyFlag = true;
        }

        /// <summary>
        /// Mark the WM, the workspaces and the applications dirty.
        /// </summary>
        public void SetAllDirty()
        {
            SetDirty();
            foreach (Workspace kws in KwsTree.Values)
            {
                kws.SetDirty();
                foreach (KwsApp app in kws.AppTree.Values) app.SetDirty("SetAllDirty() called");
            }
        }

        /// <summary>
        /// Serialize the state of the WM and the workspaces. If fullFlag is
        /// true, everything must be serialized regardless of the value of the
        /// dirty flag.
        /// </summary>
        public void Serialize(bool fullFlag)
        {
            // We should have a transaction open.
            Debug.Assert(LocalDbBroker.HasTransaction());

            if (fullFlag) SetAllDirty();
            
            // Serialize the objects in the database.
            if (DirtyFlag)
            {
                DirtyFlag = false;
                SerializeObject("wm", this);

                foreach (Workspace kws in KwsTree.Values)
                {
                    if (kws.DirtyFlag)
                    {
                        kws.DirtyFlag = false;
                        String kwsName = "kws_" + kws.InternalID;
                        SerializeObject(kwsName, kws);

                        foreach (KwsApp app in kws.AppTree.Values)
                        {
                            if (app.DirtyFlag)
                            {
                                app.DirtyFlag = false;
                                SerializeObject(kwsName + "_" + app.AppID, app);
                            }
                        }
                    }
                }
            }

            // Commit the lingering transaction.
            LocalDbBroker.CommitTransaction();

            // Open a new lingering transaction.
            LocalDbBroker.BeginTransaction();
        }

        /// <summary>
        /// Serialize the object specified and store it under the name
        /// specified in the database.
        /// </summary>
        private void SerializeObject(String name, Object obj)
        {
            Logging.Log("Serializing " + name + ".");
            MemoryStream s = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(s, obj);
            LocalDbBroker.AddSerializedObject(name, s.ToArray());
            s.Close();
        }

        /// <summary>
        /// Link the workspaces and folders that have been deserialized. The 
        /// caller guarantees that all workspaces have a unique ID.
        /// 
        /// NOTE: this method is not the place where the state of the workspaces
        /// should be normalized. The workspace manager is not fully initialized
        /// so perform as few operations as possible. 'notInDbFlag' is true if 
        /// the workspaces aren't present in the local database because the KWM
        /// has been upgraded.
        /// </summary>
        public void LinkDeserializedKws(SerializedKwsBrowser skb, List<Workspace> kwsList, bool notInDbFlag)
        {
            // Deserialize the browser folders, reorder kwsList and get the 
            // workspace folders.
            List<KwsBrowserFolderNode> kwsFolderList;
            List<bool> notifyFlagsList;

            UiBroker.Browser.Deserialize(skb, kwsList, out kwsFolderList, out notifyFlagsList);

            // Relink the workspaces.
            for (int i = 0; i < kwsList.Count; i++)
                RelinkWorkspaceObject(kwsList[i], kwsFolderList[i], notifyFlagsList[i], notInDbFlag);

            // Reselect the selected workspace, if any.
            UiBroker.Browser.SelectedKws = GetKwsByInternalID(skb.SelectedKwsID);
            
            // Adjust the public workspace ID.
            AdjustPublicKwsID();
        }

        /// <summary>
        /// Ensure that the value of PublicKwsID is consistent. This method
        /// should be called when the WM is deserialized and when a workspace is
        /// added / removed.
        /// </summary>
        public void AdjustPublicKwsID()
        {
            UInt64 id = PublicKwsID;
            
            // If we have a public workspace ID, check whether the workspace exists
            // and is public.
            if (id != 0 && (!KwsTree.ContainsKey(id) || !KwsTree[id].IsPublicKws()))
                id = 0;

            // Find a public workspace if there is one.
            if (id == 0)
            {
                foreach (Workspace kws in KwsTree.Values)
                {
                    if (kws.IsPublicKws())
                    {
                        id = kws.InternalID;
                        break;
                    }
                }
            }

            if (PublicKwsID != id)
            {
                PublicKwsID = id;
                SetDirty();
            }
        }

        /// <summary>
        /// Return the KAS having the identifier specified, if any.
        /// </summary>
        public WmKas GetKasById(KasIdentifier kasID)
        {
            if (!KasTree.ContainsKey(kasID)) return null;
            return KasTree[kasID];
        }

        /// <summary>
        /// Return the public workspace if there is one.
        /// </summary>
        public Workspace GetPublicKws()
        {
            if (PublicKwsID != 0) return KwsTree[PublicKwsID];
            return null;
        }

        /// <summary>
        /// Return the workspace having the internal ID specified, if any.
        /// </summary>
        public Workspace GetKwsByInternalID(UInt64 internalID)
        {
            if (KwsTree.ContainsKey(internalID)) return KwsTree[internalID];
            return null;
        }

        /// <summary>
        /// Return the workspace having the Kas ID and external ID specified, 
        /// if any.
        /// </summary>
        public Workspace GetKwsByExternalID(KasIdentifier kasID, UInt64 externalID)
        {
            if (externalID == 0) return null;
            foreach (Workspace kws in KwsTree.Values)
                if (kws.Kas.KasID.CompareTo(kasID) == 0 && kws.CoreData.Credentials.ExternalID == externalID)
                    return kws;
            return null;
        }

        /// <summary>
        /// Return the workspace having the KWMO hostname and external ID
        /// specified, if any.
        /// </summary>
        public Workspace GetKwsByKwmoHostname(String host, UInt64 externalID)
        {
            if (host == "" || externalID == 0) return null;
            foreach (Workspace kws in KwsTree.Values)
                if (kws.CoreData.Credentials.KwmoAddress == host && kws.CoreData.Credentials.ExternalID == externalID)
                    return kws;
            return null;
        }

        /// <summary>
        /// Parse the specified workspace credential file and return the import
        /// data generated. The method throws an exception on error.
        /// </summary>
        public static WmImportData GetImportDataFromFile(String path)
        {
            WmImportData data = null;

            // Import the data using the legacy format first, then the
            // new format last, to get the most informative exception.
            try
            {
                data = ImportKwsFromFileLegacy(path);
            }

            catch (Exception)
            {
                data = ImportKwsFromFile(path);
            }

            return data;
        }

        /// <summary>
        /// Import the content of an Xml file created in the new Export format.
        /// </summary>
        private static WmImportData ImportKwsFromFile(String path)
        {
            WmImportData data = new WmImportData();
            XmlDocument XmlFile = new XmlDocument();
            XmlFile.Load(path);

            UInt32 version = UInt32.Parse(XmlFile.DocumentElement.GetAttribute("version"));
            if (version == 1)
            {
                foreach (XmlNode el in XmlFile.DocumentElement.ChildNodes)
                {
                    XmlElement elem = el as XmlElement;
                    if (elem == null)
                        throw new Exception("Corrupted document (Node is not an XmlElement).");

                    if (elem.Name == "Kws")
                        data.KwsList.Add(ExportedKws.FromXml(elem));
                    else if (elem.Name == "KwsBrowser")
                        data.FolderList.AddRange(KwsBrowser.FromXml(elem));
                    else
                        throw new Exception("Corrupted document (unknown node '" + elem.Name + "'.");
                }
            }

            else
            {
                throw new Exception("Unsupported KwsExport version ('" + version + "').");
            }

            return data;
        }

        /// <summary>
        /// Import workspaces from the legacy file format.
        /// </summary>
        private static WmImportData ImportKwsFromFileLegacy(String path)
        {
            WmImportData data = new WmImportData();
            Stream stream = null;

            try
            {
                stream = File.Open(path, FileMode.Open);
                SoapFormatter formatter = new SoapFormatter();
                ArrayList list = (ArrayList)formatter.Deserialize(stream);

                // Folder list in the legacy format is ignored. No customer asked for this feature.
                foreach (Workspace.Credentials cred in list)
                    data.KwsList.Add(new ExportedKws(cred.newCreds, ""));
            }

            finally
            {
                if (stream != null) stream.Close();
            }

            return data;
        }

        /// <summary>
        /// Process the workspaces and folders that are waiting to be imported,
        /// if required. This method does not throw.
        /// </summary>
        public void ProcessPendingKwsToImport()
        {
            // Set a new import data in case somebody adds data to it while
            // we're working with it.
            WmImportData data = ImportData;
            ImportData = new WmImportData();

            // Create the folders specified.
            foreach (String folderPath in data.FolderList)
                UiBroker.Browser.CreateFolderFromPath(folderPath);

            // Remember how many workspaces we have to import. In particular,
            // if there are multiple workspaces to import, we do not prompt for
            // passwords.
            bool singleFlag = (data.KwsList.Count <= 1);

            // Import the workspaces specified using the appropriate handler.
            foreach (ExportedKws eKws in data.KwsList)
            {
                Workspace kws = GetKwsByExternalID(eKws.Creds.KasID, eKws.Creds.ExternalID);
                if (kws != null) ImportExistingKws(kws, eKws.Creds, singleFlag);
                else ImportNewKws(eKws.Creds, eKws.FolderPath, singleFlag);
            }
        }

        /// <summary>
        /// "Import" a workspace that already exists in the KWM.
        /// </summary>
        public void ImportExistingKws(Workspace kws, KwsCredentials creds, bool singleFlag)
        {
            KwsTask task = kws.Sm.GetCurrentTask();
            Logging.Log("Import of existing workspace " + kws.InternalID + " with task " + task + " requested.");

            // Do not do anything if the workspace is being spawned, deleted or
            // is already working online.
            if (task == KwsTask.Spawn || task == KwsTask.Delete || task == KwsTask.WorkOnline)
            {
                Logging.Log("Skipping import.");
                return;
            }

            // Update the credentials unless they were already accepted.
            if (kws.KasLoginHandler.LoginResult != KwsLoginResult.Accepted)
            {
                Logging.Log("Updating workspace credentials.");
                creds.PublicFlag = kws.CoreData.Credentials.PublicFlag;
                kws.CoreData.Credentials = creds;
            }

            // Make the workspace work online.
            if (task == KwsTask.WorkOffline || task == KwsTask.Stop)
            {
                KwsLoginType loginType = singleFlag ? KwsLoginType.All : KwsLoginType.NoPwdPrompt;
                Logging.Log("Switching to WorkOnline task with login type " + loginType + ".");
                kws.Sm.SetLoginType(loginType);
                kws.Sm.RequestTaskSwitch(KwsTask.WorkOnline);
            }
        }

        /// <summary>
        /// Import or join a workspace that does not exist in the KWM.
        /// </summary>
        private void ImportNewKws(KwsCredentials creds, String folderPath, bool singleFlag)
        {
            Logging.Log("Importing new workspace " + creds.KwsName + ".");

            // Validate the destination folder.
            KwsBrowserFolderNode folder = UiBroker.Browser.CreateFolderFromPath(folderPath);
            if (folder.IsRoot()) folderPath = UiBroker.Browser.PrimaryFolder.FullPath;

            // Dispatch to the import operation.
            KwmImportKwsOp op = new KwmImportKwsOp(this, creds, folderPath, singleFlag);
            op.StartOp();
        }

        /// <summary>
        /// Export the given KWS to the specified path. Set kwsID to 0
        /// to export all the workspaces.
        /// </summary>
        public void ExportKws(String path, UInt64 kwsID)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode node = doc.CreateNode(XmlNodeType.XmlDeclaration, "", "");
            doc.AppendChild(node);
            
            XmlElement elem = doc.CreateElement("TeamboxExport");
            elem.SetAttribute("version", "1");

            doc.AppendChild(elem);

            if (kwsID == 0)
            {
                // Export the entire list of workspaces and the KwsBrowser as well.
                foreach (Workspace kws in KwsTree.Values) ExportKws(kws, doc, elem);

                // Export the entire browser to make sure empty folders are restored.
                UiBroker.Browser.ExportToXml(doc, elem);
            }
            else
            {
                // Export only the given workspace.
                ExportKws(KwsTree[kwsID], doc, elem);
            }

            using (Stream s = File.Open(path, FileMode.Create))
            {
                doc.Save(s);
            }
        }

        /// <summary>
        /// Helper method that wraps a Kws export and its parent folder path.
        /// </summary>
        private void ExportKws(Workspace kws, XmlDocument doc, XmlElement parent)
        {
            // Bail out if we cannot export the workspace.
            if (!kws.Sm.CanExport()) return;

            // Get the parent folder of this workspace.
            KwsBrowserNode node = UiBroker.Browser.GetKwsNodeByKws(kws);
            Debug.Assert(node != null);

            ExportedKws expKws = new ExportedKws(kws, node.Parent.FullPath);

            expKws.ToXml(doc, parent);
        }

        /// <summary>
        /// Create a new workspace object having the credentials specified and 
        /// insert it in the workspace manager, with the current task Stop and
        /// under the folder specified. Set selectFlag to true if you want the
        /// workspace to be selected once created.
        /// </summary>
        public Workspace CreateWorkspaceObject(KwsCredentials creds, KwsBrowserFolderNode folder, bool firstFlag)
        {
            try
            {
                // Clear the public flag if we already have a public workspace.
                if (PublicKwsID != 0) creds.PublicFlag = false;

                // Get the KAS object.
                WmKas kas = GetOrCreateKas(creds.KasID);

                // Create the workspace object, if possible.
                Workspace kws = new Workspace(this, kas, NextKwsInternalId++, creds);

                // Register the workspace in the WM and the KAS.
                KwsTree[kws.InternalID] = kws;
                kas.KwsTree[kws.InternalID] = kws;
                AdjustPublicKwsID();

                // Insert the workspace in the folder specified.
                UiBroker.Browser.AddKws(kws, folder, firstFlag);
                UiBroker.RequestBrowserUiUpdate(true);

                // Insert the workspace in the database.
                AddKwsToDb(kws);
                SetDirty();

                return kws;
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return null;
            }
        }

        /// <summary>
        /// Relink a workspace to the workspace manager after it has been
        /// deserialized.
        /// </summary>
        private void RelinkWorkspaceObject(Workspace kws, KwsBrowserFolderNode folder, bool notifyFlag, bool notInDbFlag)
        {
            Debug.Assert(kws != null);

            // Call Initialize() to restore the link to us.
            kws.Initialize(this);

            // Insert the workspace in the database.
            if (notInDbFlag) AddKwsToDb(kws);

            // Add the workspace to the workspace tree. 
            KwsTree[kws.InternalID] = kws;

            // The workspace has a serialized KAS, but it is not the same
            // object as the one serialized in the WM. We need to correct
            // that situation.
            WmKas kas = GetOrCreateKas(kws.Kas.KasID);
            kws.Kas = kas;
            kas.KwsTree.Add(kws.InternalID, kws);

            // Make sure our workspace ID generator is in a sane state.
            NextKwsInternalId = Math.Max(NextKwsInternalId, kws.InternalID + 1);

            // Add the workspace to the browser.
            UiBroker.Browser.AddKws(kws, folder, false);

            // Set the node's Notify flag. Checking the return value is unnecessary.
            if (notifyFlag)
                UiBroker.MarkNodeNotifyFlag(kws);
        }

        /// <summary>
        /// Remove the workspace object from the workspace manager.
        /// </summary>
        public void RemoveWorkspaceObject(Workspace kws)
        {
            try
            {
                WmKas kas = kws.Kas;
                Debug.Assert(kws.RefCount == 0);
                Debug.Assert(KwsTree.ContainsKey(kws.InternalID));
                Debug.Assert(kas.KwsTree.ContainsKey(kws.InternalID));
                Debug.Assert(!kas.KwsConnectTree.ContainsKey(kws.InternalID));

                // Cancel the KAS queries related to the workspace.
                kas.CancelKwsKasQuery(kws, false);

                // Unregister the workspace from the KAS object. Get rid of the
                // KAS object if required.
                kas.KwsTree.Remove(kws.InternalID);
                RemoveKasIfNoRef(kas);

                // Unregister the workspace from the manager.
                KwsTree.Remove(kws.InternalID);
                if (KwsDeleteTree.ContainsKey(kws.InternalID)) KwsDeleteTree.Remove(kws.InternalID);
                AdjustPublicKwsID();

                // Remove the workspace from its containing folder.
                UiBroker.RequestBrowserUiUpdate(true);
                if (UiBroker.Browser.SelectedKws == kws)
                    UiBroker.RequestSelectOtherKws(kws);

                UiBroker.Browser.RemoveKws(kws);
                SetDirty();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }
            
        /// <summary>
        /// Insert the workspace in the database.
        /// </summary>
        private void AddKwsToDb(Workspace kws)
        {
            LocalDbBroker.AddKwsToKwsList(kws.InternalID, kws.CoreData.Credentials.KwsName);
        }

        /// <summary>
        /// Create the WmKas object specified if it does not exist,
        /// and return a reference to the WmKas object specified.
        /// </summary>
        private WmKas GetOrCreateKas(KasIdentifier kasID)
        {
            if (!KasTree.ContainsKey(kasID))
            {
                KasTree[kasID] = new WmKas(kasID);
            }

            return KasTree[kasID];
        }

        /// <summary>
        /// Unregister the WmKas object specified if there are no more references
        /// to it and it is disconnected.
        /// </summary>
        public void RemoveKasIfNoRef(WmKas kas)
        {
            if (kas.KwsTree.Count == 0 && kas.ConnStatus == KasConnStatus.Disconnected)
            {
                KasTree.Remove(kas.KasID);
                SetDirty();
            }
        }

        /// <summary>
        /// Remove KASes having no references.
        /// </summary>
        public void RemoveStaleKases()
        {
            SortedDictionary<KasIdentifier, WmKas> kasTree = new SortedDictionary<KasIdentifier, WmKas>(KasTree);
            foreach (WmKas kas in kasTree.Values) RemoveKasIfNoRef(kas); 
        }

        /// <summary>
        /// Create a new KAS ANP command message having the minor version and 
        /// type specified and a unique ID.
        /// </summary>
        public AnpMsg NewKAnpCmd(UInt32 minorVersion, UInt32 type)
        {
            AnpMsg msg = new AnpMsg();
            msg.Major = KAnp.Major;
            msg.Minor = minorVersion;
            msg.Type = type;
            msg.ID = KAnpState.NextCmdMsgID++;
            return msg;
        }

        /// <summary>
        /// Return true if the configuration wizard should be shown to the user, 
        /// false otherwise.
        /// Config_Status meaning:
        /// 0 => KWM is unconfigured.
        /// 1 => KWM has just been configured, MSO reload required.
        /// 2 => No change.
        /// </summary>
        private bool NeedWizard()
        {
            RegistryKey regKey = null;

            try
            {
                regKey = Registry.CurrentUser.CreateSubKey(Base.GetOtcRegKey());
                if (regKey != null)
                {
                    // If the key does not exist, use 0 as the default value.
                    Int32 configStatus = (Int32)regKey.GetValue("Config_Status", 0);
                    return (configStatus == 0);
                }

                return true;
            }
            finally
            {
                if (regKey != null) 
                    regKey.Close();
            }
        }
    }
}
