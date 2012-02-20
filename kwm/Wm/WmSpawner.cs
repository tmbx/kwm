using System.Collections.Generic;
using System;
using kwm.Utils;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using kwm.KwmAppControls;
using System.Windows.Forms;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// Deserialization status of the KWM.
    /// </summary>
    public enum KwmDeserializationStatus
    {
        /// <summary>
        /// The WM was not deserialized.
        /// </summary>
        None,

        /// <summary>
        /// The WM was deserialized, but some workspaces were not
        /// deserialized.
        /// </summary>
        Partial,

        /// <summary>
        /// The WM and all its workspaces have been deserialized.
        /// </summary>
        Full
    }

    /// <summary>
    /// Represent a KWM to deserialize.
    /// </summary>
    public abstract class KwmDeserializer
    {
        /// <summary>
        /// Current deserialization status.
        /// </summary>
        public KwmDeserializationStatus Status = KwmDeserializationStatus.None;

        /// <summary>
        /// Workspace manager to deserialize.
        /// </summary>
        public WmDeserializer WmDsr = null;

        /// <summary>
        /// List of workspaces to deserialize.
        /// </summary>
        public List<KwsDeserializer> KwsDsrList = new List<KwsDeserializer>();

        /// <summary>
        /// Deserializer of the workspace currently being deserialized, if any.
        /// </summary>
        public KwsDeserializer CurKwsDsr = null;

        /// <summary>
        /// Deserialized workspace browser, if any.
        /// </summary>
        public SerializedKwsBrowser Browser = new SerializedKwsBrowser();

        /// <summary>
        /// Serialization version number.
        /// </summary>
        public UInt32 Version = 0;

        /// <summary>
        /// Reference to the KWM spawner.
        /// </summary>
        public KwmSpawner Spawner { get { return KwmSpawner.Instance; } }

        /// <summary>
        /// Setup the environment required to deserialize the workspace manager
        /// and the workspaces.
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Return true if the KWM can be deserialized.
        /// </summary>
        public abstract bool CanDeserialize();

        /// <summary>
        /// Backup the state of the KWM, with the exception of the user 
        /// data files such as the KFS files, in the directory specified. This
        /// is called on deserialization failure for debugging purposes; do a
        /// best effort.
        /// </summary>
        public abstract void BackupState(String destDirPath);

        /// <summary>
        /// Remove the stale data related to obsolete serializers. Do a best
        /// effort.
        /// </summary>
        public virtual void CleanUp() { }

        /// <summary>
        /// Deserialize as much stuff as possible and set the deserialization
        /// status.
        /// </summary>
        public virtual void Deserialize()
        {
            if (!DeserializeWm()) return;
            DeserializeAllKws();
            LinkKwsToWm();
        }

        /// <summary>
        /// Deserialize the stream specified with the formatter specified and
        /// return the deserialized object. The stream is closed in all cases.
        /// </summary>
        public Object DeserializeFromStream(Stream stream, IFormatter formatter)
        {
            try
            {
                Object o = formatter.Deserialize(stream);
                if (o == null) throw new Exception("failed to deserialize object from stream");
                return o;
            }

            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Deserialize the WM in WmDsr. Downgrade Status to None as needed.
        /// Return true if it worked.
        /// </summary>
        public virtual bool DeserializeWm()
        {
            try
            {
                Status = KwmDeserializationStatus.Full;
                WmDsr.Deserialize();
                WmDsr.Wm.Initialize(Spawner.LocalDb);
                return true;
            }

            catch (Exception ex)
            {
                Logging.LogException(ex);
                WmDsr.Ex = ex;
                Status = KwmDeserializationStatus.None;
                return false;
            }
        }

        /// <summary>
        /// Deserialize all the workspaces in KwsDsrList. Downgrade Status to
        /// partial as needed.
        /// </summary>
        public virtual void DeserializeAllKws()
        {
            // This tree is used to detect duplicate IDs.
            SortedDictionary<UInt64, Workspace> kwsTree = new SortedDictionary<UInt64, Workspace>();

            foreach (KwsDeserializer KwsDsr in KwsDsrList)
            {
                CurKwsDsr = KwsDsr;

                try
                {
                    KwsDsr.Deserialize();
                    Workspace kws = KwsDsr.Kws;
                    if (kws.InternalID == 0)
                        throw new Exception(Base.GetKwsString() + " has no internal ID");
                    if (kwsTree.ContainsKey(kws.InternalID))
                        throw new Exception(Base.GetKwsString() + " has duplicate internal ID " + kws.InternalID);
                    kwsTree.Add(kws.InternalID, kws);
                }

                catch (Exception ex)
                {
                    Logging.LogException(ex);
                    KwsDsr.Ex = ex;
                    if (Status != KwmDeserializationStatus.None) Status = KwmDeserializationStatus.Partial;
                }

                CurKwsDsr = null;
            }
        }

        /// <summary>
        /// Link the workspaces to the WM.
        /// </summary>
        public virtual void LinkKwsToWm()
        {
            // Get the list of workspaces that deserialized correctly.
            List<Workspace> kwsList = new List<Workspace>();
            List<KwsDeserializer> dsrList = new List<KwsDeserializer>();
            foreach (KwsDeserializer kwsDsr in KwsDsrList)
            {
                if (kwsDsr.Ex == null)
                {
                    kwsList.Add(kwsDsr.Kws);
                    dsrList.Add(kwsDsr);
                }

                else Logging.Log(2, "A Teambox could not be restored properly: " + kwsDsr.Ex);
            }

            // Convert the old folder list, if required.
            ConvertOldFolderList(dsrList);

            // Tell the WM to digest the workspaces.
            WmDsr.Wm.LinkDeserializedKws(Browser, kwsList, this is FileKwmDeserializer);
        }

        /// <summary>
        /// Convert the old folder list, if required.
        /// </summary>
        public virtual void ConvertOldFolderList(List<KwsDeserializer> dsrList) { }

        /// <summary>
        /// Add the IDs of the workspace to the local database, if required.
        /// </summary>
        public virtual void RepopulateLocalDb(List<Workspace> kwsList) { }
    }

    /// <summary>
    /// Represent a workspace manager to deserialize.
    /// </summary>
    public abstract class WmDeserializer
    {
        /// <summary>
        /// Deserialized workspace manager, if any.
        /// </summary>
        public WorkspaceManager Wm = null;

        /// <summary>
        /// Exception that occurred while deserializing the workspace manager,
        /// if any.
        /// </summary>
        public Exception Ex = null;

        /// <summary>
        /// Reference to the KWM deserializer.
        /// </summary>
        public KwmDeserializer KwmDsr { get { return KwmSpawner.Instance.KwmDsr; } }

        /// <summary>
        /// Attempt to deserialize the workspace manager. Throw an exception
        /// on error.
        /// </summary>
        public abstract void Deserialize();
    }

    /// <summary>
    /// Represent a workspace to deserialize.
    /// </summary>
    public abstract class KwsDeserializer
    {
        /// <summary>
        /// Deserialized workspace, if any.
        /// </summary>
        public Workspace Kws = null;

        /// <summary>
        /// Exception that occurred while deserializing the workspace, if any.
        /// </summary>
        public Exception Ex = null;

        /// <summary>
        /// Reference to the KWM deserializer.
        /// </summary>
        public KwmDeserializer KwmDsr { get { return KwmSpawner.Instance.KwmDsr; } }

        /// <summary>
        /// Attempt to deserialize the workspace. Throw an exception on error.
        /// </summary>
        public abstract void Deserialize();
    }


    /////////////////////////////////////
    // Deserializers for legacy files. //
    /////////////////////////////////////

    public class FileKwmDeserializer : KwmDeserializer
    {
        public FileWmDeserializer FileWmDsr { get { return WmDsr as FileWmDeserializer; } }

        /// <summary>
        /// Path to the directory containing the serialization file of the
        /// workspace manager. Beware of touching this; this is a legacy value.
        /// </summary>
        public String WmSerializationDirPath = null;

        /// <summary>
        /// Path to the directory containing the directories containing
        /// the serialization file of the workspaces. Beware of touching this;
        /// this is a legacy value.
        /// </summary>
        public String RootKwsSerializationDirPath = null;

        /// <summary>
        /// Folder list.
        /// </summary>
        public WorkspaceFolderList FolderList = new WorkspaceFolderList();

        public override void Setup()
        {
            String appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            WmSerializationDirPath = appData + "\\teambox\\kcs\\kwm\\state\\";
            RootKwsSerializationDirPath = appData + "\\teambox\\kcs\\kwm\\state\\workspaces\\";
            WmDsr = new FileWmDeserializer();
            FileWmDsr.FindWmFile(WmSerializationDirPath);
            FindAllKws();
        }

        public override bool CanDeserialize()
        {
            return (FileWmDsr.WmFilePath != null);
        }

        public override void BackupState(string destDirPath)
        {
            if (FileWmDsr.WmFilePath != null)
                File.Copy(FileWmDsr.WmFilePath,
                          Path.Combine(destDirPath, Path.GetFileName(FileWmDsr.WmFilePath)));

            if (Directory.Exists(RootKwsSerializationDirPath))
                Misc.CopyDirContent(RootKwsSerializationDirPath, Path.Combine(destDirPath, "workspaces"));
        }

        /// <summary>
        /// This is WAY too dangerous. Disabled for now.
        /// </summary>
        public override void CleanUp()
        {
            //FileWmDsr.CleanUp(WmSerializationDirPath);
            //Directory.Delete(RootKwsSerializationDirPath, true);
        }

        /// <summary>
        /// Find all the workspaces that have been serialized and add them to
        /// KwsDsrList.
        /// </summary>
        private void FindAllKws()
        {
            if (!Directory.Exists(RootKwsSerializationDirPath)) return;

            DirectoryInfo[] dirList = (new DirectoryInfo(RootKwsSerializationDirPath)).GetDirectories();

            foreach (DirectoryInfo wsDir in dirList)
            {
                FileKwsDeserializer kwsDsr = new FileKwsDeserializer();
                kwsDsr.FindKwsFile(wsDir.FullName);

                // No serialization file found, ignore.
                if (kwsDsr.KwsFilePath == null) continue;

                // Add the workspace to KwsDsrList.
                KwsDsrList.Add(kwsDsr);
            }
        }

        public override void ConvertOldFolderList(List<KwsDeserializer> dsrList)
        {
            foreach (WorkspaceFolder oldFolder in FolderList)
            {
                SerializedKwsBrowserFolder skbf = new SerializedKwsBrowserFolder();
                String oldName = oldFolder.m_text;
                if (oldName == "My Workspaces") oldName = "My Teamboxes";
                skbf.Path = "/f_" + oldName;
                skbf.ExpandedFlag = oldFolder.m_isExpanded;
                Browser.FolderList.Add(skbf);

                // Map index IDs to deserializer to order them.
                SortedDictionary<int, FileKwsDeserializer> dsrTree = new SortedDictionary<int,FileKwsDeserializer>(); 
                foreach (FileKwsDeserializer dsr in dsrList)
                    if (dsr.FolderID == oldFolder.m_id) dsrTree[dsr.FolderIndex] = dsr;

                foreach (FileKwsDeserializer dsr in dsrTree.Values)
                {
                    SerializedKwsBrowserKws skbk = new SerializedKwsBrowserKws();
                    skbk.ID = dsr.Kws.InternalID;
                    skbk.ParentPath = skbf.Path;
                    Browser.KwsList.Add(skbk);
                }
            }
        }
    }

    public class FileWmDeserializer : WmDeserializer
    {
        /// <summary>
        /// Path to the serialization file of the WM.
        /// </summary>
        public String WmFilePath = null;

        /// <summary>
        /// Stream formatter for the serialization file.
        /// </summary>
        public IFormatter Formatter;

        /// <summary>
        /// Set the path and type of the serialization file of the WM. The path
        /// is null if no file has been found.
        /// </summary>
        public void FindWmFile(String wmDirPath)
        {
            if (!(FindWmFileHelper(wmDirPath, "WorkspaceManager.bin", false) ||
                  FindWmFileHelper(wmDirPath, "WorkspaceManager.bin.tmp", false) ||
                  FindWmFileHelper(wmDirPath, "WorkspaceManager.xml", true) ||
                  FindWmFileHelper(wmDirPath, "WorkspaceManager.xml.tmp", true)))
            {
                WmFilePath = null;
            }
        }
        /// <summary>
        /// Helper method for FindWmFilePath().
        /// </summary>
        private bool FindWmFileHelper(String dir, String name, bool xmlFlag)
        {
            String filePath = Path.Combine(dir, name);
            if (File.Exists(filePath))
            {
                WmFilePath = filePath;
                if (xmlFlag) Formatter = new SoapFormatter();
                else Formatter = new BinaryFormatter();
                return true;
            }

            return false;
        }

        public void CleanUp(String wmDirPath)
        {
            String[] l = { "WorkspaceManager.bin", "WorkspaceManager.bin.tmp",
                           "WorkspaceManager.xml", "WorkspaceManager.xml.tmp" };
            foreach (String s in l) File.Delete(Path.Combine(wmDirPath, s));
        }

        public override void Deserialize()
        {
            Wm = (WorkspaceManager)KwmDsr.DeserializeFromStream(File.Open(WmFilePath, FileMode.Open), Formatter);
        }
    }

    public class FileKwsDeserializer : KwsDeserializer
    {
        /// <summary>
        /// Path to the serialization file of the workspace.
        /// </summary>
        public String KwsFilePath = null;

        /// <summary>
        /// Stream formatter for the serialization file.
        /// </summary>
        public IFormatter Formatter;

        /// <summary>
        /// Parent folder ID.
        /// </summary>
        public UInt64 FolderID;

        /// <summary>
        /// Index in the parent folder.
        /// </summary>
        public int FolderIndex;

        /// <summary>
        /// Set the path and type of the serialization file of the workspace. 
        /// The path is null if no file has been found.
        /// </summary>
        public void FindKwsFile(String kwsDirPath)
        {
            if (!(FindKwsFileHelper(kwsDirPath, "Workspace.bin", false) ||
                  FindKwsFileHelper(kwsDirPath, "Workspace.bin.tmp", false) ||
                  FindKwsFileHelper(kwsDirPath, "Workspace.xml", true) ||
                  FindKwsFileHelper(kwsDirPath, "Workspace.xml.tmp", true)))
            {
                KwsFilePath = null;
            }
        }

        /// <summary>
        /// Helper method for FindKwsFile().
        /// </summary>
        private bool FindKwsFileHelper(String dir, String name, bool xmlFlag)
        {
            String filePath = Path.Combine(dir, name);

            if (File.Exists(filePath))
            {
                KwsFilePath = filePath;
                if (xmlFlag) Formatter = new SoapFormatter();
                else Formatter = new BinaryFormatter();
                return true;
            }

            return false;
        }

        public override void Deserialize()
        {
            Kws = (Workspace)KwmDsr.DeserializeFromStream(File.Open(KwsFilePath, FileMode.Open), Formatter);
        }
    }


    ///////////////////////////////////////
    // Deserializers for local database. //
    ///////////////////////////////////////

    public class DbKwmDeserializer : KwmDeserializer
    {
        /// <summary>
        /// Reference to the local database.
        /// </summary>
        public WmLocalDb LocalDb;

        /// <summary>
        /// Reference to the local database broker.
        /// </summary>
        public WmLocalDbBroker Broker;

        /// <summary>
        /// Stream formatter for the serialization objects.
        /// </summary>
        public IFormatter Formatter = new BinaryFormatter();

        public DbKwmDeserializer(WmLocalDb localDB)
        {
            LocalDb = localDB;
            Broker = new WmLocalDbBroker(LocalDb);
        }

        public override void Setup()
        {
            WmDsr = new DbWmDeserializer();
        }

        public override bool CanDeserialize()
        {
            return (Broker.GetDbVersion() != 0 && HasSerializedObject("wm"));
        }

        public override void BackupState(string destDirPath)
        {
            File.Copy(LocalDb.DbPath, Path.Combine(destDirPath, Path.GetFileName(LocalDb.DbPath)));
        }

        /// <summary>
        /// Deserialize the serialized object specified.
        /// </summary>
        public Object DeserializeObjectFromStream(String name)
        {
            Debug.Assert(HasSerializedObject(name));
            return DeserializeFromStream(new MemoryStream(Broker.GetSerializedObject(name)), Formatter);
        }

        /// <summary>
        /// Return true if the serialized object specified exists.
        /// </summary>
        public bool HasSerializedObject(String name)
        {
            return Broker.HasSerializedObject(name);
        }

        /// <summary>
        /// Find all the workspaces that have been serialized and add them to
        /// KwsDsrList.
        /// </summary>
        public void FindAllKws()
        {
            foreach (UInt32 id in Broker.GetKwsList().Keys)
                KwsDsrList.Add(new DbKwsDeserializer(id));
        }
    }

    public class DbWmDeserializer : WmDeserializer
    {
        public DbKwmDeserializer DbKwmDsr { get { return KwmDsr as DbKwmDeserializer; } }

        public override void Deserialize()
        {
            Wm = (WorkspaceManager)DbKwmDsr.DeserializeObjectFromStream("wm");
            
            // Cannot be done in Setup() until we have confirmed that
            // the schema has been initialized.
            DbKwmDsr.FindAllKws();
        }
    }

    public class DbKwsDeserializer : KwsDeserializer
    {
        public DbKwmDeserializer DbKwmDsr { get { return KwmDsr as DbKwmDeserializer; } }
        public UInt32 InternalID;

        public DbKwsDeserializer(UInt32 internalID)
        {
            InternalID = internalID;
        }

        public override void Deserialize()
        {
            // Deserialize the workspace object.
            String kwsName = "kws_" + InternalID;

            if (!DbKwmDsr.HasSerializedObject(kwsName))
                throw new Exception(Base.GetKwsString() + " " + InternalID + " does not exist in DB");

            Kws = (Workspace)DbKwmDsr.DeserializeObjectFromStream(kwsName);

            // Deserialize the application objects. Skip applications we
            // didn't serialize.
            foreach (UInt32 appID in Workspace.AppIdList)
            {
                String appName = kwsName + "_" + appID;
                if (!DbKwmDsr.HasSerializedObject(appName)) continue;
                KwsApp app = (KwsApp)DbKwmDsr.DeserializeObjectFromStream(appName);
                app.Initialize(Kws);
                Kws.AppTree[appID] = app;
            }
        }
    }


    ////////////////////
    // Spawner class. //
    ////////////////////

    /// <summary>
    /// This class is used to spawn a workspace manager instance, which is a
    /// complex undertaking due to backward compatibility and deserialization
    /// issues.
    /// </summary>
    public class KwmSpawner
    {
        /// <summary>
        /// Reference to the KWM spawner. 
        /// </summary>
        public static KwmSpawner Instance = null;

        /// <summary>
        /// Reference to the database used by the workspace manager.
        /// </summary>
        public WmLocalDb LocalDb = new WmLocalDb();
    
        /// <summary>
        /// List of available deserializers, in order of preference.
        /// </summary>
        public List<KwmDeserializer> KwmDsrList = new List<KwmDeserializer>();

        /// <summary>
        /// Reference to the current KWM deserializer, if any.
        /// </summary>
        public KwmDeserializer KwmDsr = null;

        /// <summary>
        /// True if the user has asked us to recover from a deserialization
        /// failure.
        /// </summary>
        public bool RecoverFlag = false;

        /// <summary>
        /// Path to the directory where the corrupted stuff should be backed up.
        /// </summary>
        public String BackupPath { get { return Misc.GetCorruptedWmPath(); } }

        /// <summary>
        /// This method is called by the program initialization code to create
        /// an empty workspace manager or deserialize an existing one from disk.
        /// It returns null if the program should exit.
        /// </summary>
        public WorkspaceManager Spawn()
        {
            WorkspaceManager wm = null;

            // Open or create the database.
            OpenLocalDbIfRequired();

            // Fill the list of deserializers. 
            FillDsrList();

            // Try to find a deserializer for the KWM.
            FindDeserializer();

            // We found a deserializer.
            if (KwmDsr != null)
            {
                // Try to deserialize.
                KwmDsr.Deserialize();

                // The deserialization failed completely or partially.
                if (KwmDsr.Status != KwmDeserializationStatus.Full)
                {
                    // Tell the user.
                    TellUserAboutDsrFailure();

                    // If the user doesn't want to recover, bail out.
                    if (!RecoverFlag) return null;

                    // Create the backup directory.
                    if (Directory.Exists(BackupPath)) Directory.Delete(BackupPath, true);
                    Directory.CreateDirectory(BackupPath);

                    // Backup the files.
                    KwmDsr.BackupState(BackupPath);
                }

                // The deserialization succeeded completely or partially.
                // We now have a deserialized WM instance.
                if (KwmDsr.Status != KwmDeserializationStatus.None)
                    wm = KwmDsr.WmDsr.Wm;
            }

            // We don't have a workspace manager. Create a new one.
            if (wm == null)
            {
                wm = new WorkspaceManager();
                wm.Initialize(LocalDb);
            }

            // Ask the workspace manager to clean up its state before
            // start up.
            wm.Sm.HandlePreStart();

            // If we didn't deserialize the KWM fully, or if we used an
            // obsolete deserializer, set everything dirty and serialize 
            // everything to stabilize our state.
            if (KwmDsr == null ||
                KwmDsr.Status != KwmDeserializationStatus.Full ||
                !(KwmDsr is DbKwmDeserializer))
            {
                wm.Serialize(true);
            }

            // Clean up any cruft left by obsolete serializers.
            if (KwmDsr != null) KwmDsr.CleanUp();

            return wm;
        }

        /// <summary>
        /// Fill the list of deserializers.
        /// </summary>
        private void FillDsrList()
        {
            KwmDsrList.Add(new DbKwmDeserializer(LocalDb));
            KwmDsrList.Add(new FileKwmDeserializer());
        }

        /// <summary>
        /// Set KwmDsr to a suitable deserializer, or null if none is found.
        /// </summary>
        private void FindDeserializer()
        {
            KwmDsr = null;
            foreach (KwmDeserializer dsr in KwmDsrList)
            {
                dsr.Setup();

                if (dsr.CanDeserialize())
                {
                    KwmDsr = dsr;
                    break;
                }
            }
        }

        /// <summary>
        /// Interact with the user to determine what should be done now that
        /// the deserialization has failed. Set RecoverFlag.
        /// </summary>
        private void TellUserAboutDsrFailure()
        {
            // Get the message to display.
            String msg;

            if (KwmDsr.Status == KwmDeserializationStatus.None)
                msg = "The Teambox Manager could not be opened.";
            else
                msg = "Some Teamboxes could not be opened. More information is available in the logs (View / Debugging Console).";

            Misc.KwmTellUser(msg, MessageBoxIcon.Error);

            // For now we always recover.
            RecoverFlag = true;
        }

        /// <summary>
        /// Open the database if required and returns a reference to its instance.
        /// </summary>
        private void OpenLocalDbIfRequired()
        {
            if (!LocalDb.IsOpen()) LocalDb.OpenOrCreateDb(Misc.GetKwmDbPath());
        }
    }
}