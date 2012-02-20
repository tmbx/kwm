using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using System.Diagnostics;
using System.IO;
using kwm.Utils;
using System.Runtime.Serialization;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// Common base class for KfsServerDirectory and KfsServerFile.
    /// </summary>
    [Serializable]
    public class KfsServerObject
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Inode of the object. This is 0 for the root.
        /// </summary>
        public UInt64 Inode;

        /// <summary>
        /// Inode of the parent of this object. This is 0 for the root.
        /// </summary>
        public UInt64 ParentInode;

        /// <summary>
        /// Last commit ID received from the server about this object.
        /// </summary>
        public UInt64 CommitID;

        /// <summary>
        /// ID of the user who originally created this object. NOTE:
        /// this field should really be a 32 bits field. Leave it to 64
        /// bits for serialization backward-compatibility.
        /// </summary>
        public UInt64 CreationUserID;

        /// <summary>
        /// Date at which the object was created.
        /// </summary>
        public UInt64 CreationDate;

        /// <summary>
        /// Current name of the object.
        /// </summary>
        public String Name;

        /// <summary>
        /// Parent of this object. This is null in the case of the root directory.
        /// </summary>
        public KfsServerDirectory Parent
        {
            get
            {
                if (IsRoot()) return null;
                KfsServerDirectory d = (KfsServerDirectory)Share.ServerView.GetObjectByInode(ParentInode);
                Debug.Assert(d != null);
                return d;
            }
        }

        /// <summary>
        /// Relative path to this object from the share root directory. The
        /// delimiters are slashes.
        /// </summary>
        public String RelativePath
        {
            get
            {
                if (Parent == null) return "";
                String path = Name;
                KfsServerDirectory cur = Parent;
                while (!cur.IsRoot())
                {
                    path = cur.Name + "/" + path;
                    cur = cur.Parent;
                }
                return path;
            }
        }

        /// <summary>
        /// Full path to this object from and including the Windows drive. The
        /// delimiters are slashes.
        /// </summary>
        public String FullPath
        {
            get
            {
                return Share.MakeAbsolute(RelativePath);
            }
        }

        /// <summary>
        /// This constructor creates the object and inserts it in the view.
        /// </summary>
        /// <param name="S">Share</param>
        /// <param name="I">Inode</param>
        /// <param name="PI">Parent Inode</param>
        /// <param name="CID">Commit ID</param>
        /// <param name="CUID">Creation user ID</param>
        /// <param name="CD">Creation date</param>
        /// <param name="N">Name</param>
        public KfsServerObject(KfsShare S, UInt64 I, UInt64 PI, UInt64 CID, UInt32 CUID, UInt64 CD, String N)
        {
            Share = S;
            Inode = I;
            ParentInode = PI;
            CommitID = CID;
            CreationUserID = CUID;
            CreationDate = CD;
            Name = N;
            AddToView();
        }

        /// <summary>
        /// Add the object at the appropriate location in the server view.
        /// </summary>
        public void AddToView()
        {
            Debug.Assert(Share.ServerView.GetObjectByInode(Inode) == null);

            if (IsRoot())
            {
                Debug.Assert(ParentInode == 0);
                Debug.Assert(CommitID == 0);
                Debug.Assert(Name == "");
                Debug.Assert(this is KfsServerDirectory);
                Debug.Assert(Share.ServerView.Root == null);
                Share.ServerView.Root = this as KfsServerDirectory;
            }

            else
            {
                Debug.Assert(Name != "");
                KfsServerDirectory d = Parent;
                Debug.Assert(d != null);
                Debug.Assert(!d.ChildTree.ContainsKey(Name));
                d.ChildTree[Name] = this;
            }

            Share.ServerView.InodeTree[Inode] = this;
        }

        /// <summary>
        /// Remove the object from the server view.
        /// </summary>
        public void RemoveFromView()
        {
            Debug.Assert(Share.ServerView.GetObjectByInode(Inode) != null);

            if (IsRoot())
            {
                Debug.Assert(this is KfsServerDirectory);
                Share.ServerView.Root = null;
            }

            else
            {
                KfsServerDirectory d = Parent;
                Debug.Assert(d != null);
                Debug.Assert(d.ChildTree[Name] == this);
                d.ChildTree.Remove(Name);
            }

            Share.ServerView.InodeTree.Remove(Inode);
        }

        /// <summary>
        /// Return true if this object is the root directory.
        /// </summary>
        public bool IsRoot()
        {
            return (Inode == 0);
        }

        /// <summary>
        /// Return true if the object is a ghost. A server file is a ghost if it
        /// was never successfully uploaded and there is at least one uploader
        /// remaining.
        /// </summary>
        public bool IsGhost()
        {
            KfsServerFile f = this as KfsServerFile;
            if (f == null) return false;
            return (!HasCurrentVersion() && f.UploaderSet.IsEmpty);
        }

        /// <summary>
        /// Return true if this object is a file and if there is a downloadable
        /// version of the file.
        /// </summary>
        public bool HasCurrentVersion()
        {
            KfsServerFile f = this as KfsServerFile;
            if (f == null) return false;
            return (f.CurrentVersion != null);
        }
    }

    /// <summary>
    /// Represent a directory in the share.
    /// </summary>
    [Serializable]
    public class KfsServerDirectory : KfsServerObject
    {
        /// <summary>
        /// Tree that maps directory entry names to KfsServerObject.
        /// </summary>
        public SortedDictionary<String, KfsServerObject> ChildTree = new SortedDictionary<string, KfsServerObject>();

        /// <summary>
        /// True if the directory is expanded.
        /// </summary>
        public bool ExpandedFlag = false;

        public KfsServerDirectory(KfsShare S, UInt64 I, UInt64 PI, UInt64 CID, UInt32 CUID, UInt64 CD, String N)
            : base(S, I, PI, CID, CUID, CD, N)
        {
        }

        /// <summary>
        /// Return true if the directory has a child having the name specified.
        /// </summary>
        public bool HasChild(String name)
        {
            return ChildTree.ContainsKey(name);
        }

        /// <summary>
        /// Return the child specified if it is a directory.
        /// </summary>
        public KfsServerDirectory GetChildDir(String name)
        {
            if (!ChildTree.ContainsKey(name)) return null;
            return ChildTree[name] as KfsServerDirectory;
        }

        /// <summary>
        /// Return the child specified if it is a file.
        /// </summary>
        public KfsServerFile GetChildFile(String name)
        {
            if (!ChildTree.ContainsKey(name)) return null;
            return ChildTree[name] as KfsServerFile;
        }
    }

    /// <summary>
    /// Status of a local file.
    /// </summary>
    public enum LocalStatus
    {
        None,
        Modified,
        Unmodified,
        Absent,
        Error
    }

    /// <summary>
    /// Represent a file in the share.
    /// </summary>
    [Serializable]
    public class KfsServerFile : KfsServerObject
    {
        /// <summary>
        /// Set of uploaders uploading a version of this file.
        /// </summary>
        public HashedSet<KfsServerFileUploader> UploaderSet = new HashedSet<KfsServerFileUploader>();

        /// <summary>
        /// Last uploaded version of this file, if any.
        /// </summary>
        public KfsServerFileVersion CurrentVersion;

        /// <summary>
        /// Downloaded version of this file, if any.
        /// </summary>
        public KfsServerFileVersion DownloadVersion;

        /// <summary>
        /// There are three possible values for the persistent local status: "none",
        /// "modified" or "unmodified".
        /// 
        /// The status "none" is set when the file has just been created on the
        /// server. It indicates that the value of the "local" fields is meaningless.
        /// 
        /// The status "modified" is set when the local file has been deemed to be
        /// different than the downloaded version (if any) and the current version. If
        /// DownloadVersion is null, then "modified" indicates that the hash of the
        /// local file does not match the current version on the server. If
        /// DownloadVersion is non-null, then "modified" indicates that the hash of
        /// the local file is different than both the downloaded version and the
        /// current version.
        /// 
        /// The status "unmodified" is set when the local file has been deemed to
        /// match the downloaded version or the current version. If the local file
        /// matches the current version, then the current version de-facto becomes the
        /// downloaded version.
        /// 
        /// Note that the persistent local status does not change even if the local
        /// file disappears. The user may be shuffling files around or a program may
        /// be updating the file. The persistent status should only change after the
        /// corresponding local file has been proven to exist and be different or
        /// equal to the current version or the downloaded version.
        /// </summary>
        public LocalStatus PersistentLocalStatus = LocalStatus.None;

        /// <summary>
        /// There are five possible statuses for the current local status: "none",
        /// "modified", "unmodified", "absent", "error". The semantics of "none",
        /// "modified" and "unmodified" are the same as in the persistent local
        /// status. The status "absent" indicates that the corresponding local file no
        /// longer exist. The status "error" indicates that the current status could
        /// not be determined because an error occurred.
        /// </summary>
        [NonSerialized]
        public LocalStatus CurrentLocalStatus;

        /// <summary>
        /// If UpdateLocalStatus() is called and the status cannot be determined
        /// because an error occurred, then LocalUpdateFlag is set and the reason of
        /// the failure is written in LocalError. It is important not to modify the
        /// persistent status and its associated fields if the status could not be
        /// determined properly.
        /// </summary>
        [NonSerialized]
        public String LocalError;

        /// <summary>
        /// True if the local status of the file needs to be updated. This flag needs
        /// not be set to true if CurrentVersion is null. Otherwise, this flag must
        /// be set to true on the following occasions: when the KWM is started,
        /// because the corresponding local file may have changed; when the
        /// FileSystemWatcher informs us that an event has occurred for the
        /// corresponding local file; when the first version is uploaded (status was
        /// "none"), because it is possible that the local file matches the first
        /// version uploaded; when the file is downloaded.
        /// </summary>
        [NonSerialized]
        public bool LocalUpdateFlag;

        /// <summary>
        /// Last observed modification date of the file on the filesystem.
        /// </summary>
        public DateTime LocalDate = DateTime.MinValue;

        /// <summary>
        /// Timespamp of this file's latest commit.
        /// </summary>
        public UInt64 LastCommitDate;

        /// <summary>
        /// Last observed identifier of the file on the filesystem.
        /// </summary>
        public UInt64 LocalID = 0;

        /// <summary>
        /// Last observed size of the file on the filesystem.
        /// </summary>
        public UInt64 LocalSize = 0;

        /// <summary>
        /// Last computed hash of the file on the filesystem.
        /// </summary>
        public byte[] LocalHash = null;

        public KfsServerFile(KfsShare S, UInt64 I, UInt64 PI, UInt64 CID, UInt32 CUID, UInt64 CD, String N)
            : base(S, I, PI, CID, CUID, CD, N)
        {
            LastCommitDate = CD;
            Initialize();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Initialization code common to both the deserialized and
        /// non-deserialized cases.
        /// </summary>
        public void Initialize()
        {
            CurrentLocalStatus = LocalStatus.None;
            LocalUpdateFlag = true;
        }

        /// <summary>
        /// Request an update of the local status.
        /// </summary>
        public void RequestUpdate()
        {
            Logging.Log("Requesting update of " + RelativePath + ".");
            LocalUpdateFlag = true;
            Share.LocalUpdateFlag = true;
        }

        /// <summary>
        /// Update the local status of the file.
        /// </summary>
        public void UpdateLocalStatus()
        {
            // Note about problematic Office files:
            //
            // Some Office programs change the modification date of a file when 
            // they open the file, even though the user did not modify the file yet. 
            // On exit, they restore the previous modification date if the user did not modify
            // the file. The hash is modified in all cases. Therefore, for the
            // algorithm to detect these file statuses reliably, we must be careful
            // to never flush the unmodified date unless the file is probably
            // modified.
            Logging.Log("Updating status of " + RelativePath + ".");

            FileStream stream = null;
            LocalUpdateFlag = false;
            LocalError = null;

            if (CurrentVersion == null)
            {
                Debug.Assert(PersistentLocalStatus == LocalStatus.None);
                Debug.Assert(CurrentLocalStatus == LocalStatus.None);
                return;
            }

            if (PersistentLocalStatus != LocalStatus.None) Debug.Assert(LocalHash != null);

            try
            {
                // If this is a problematic Office file, open the file in write-exclusive mode
                // to detect the case where the problematic Office application has opened the file. 
                // We can't compute the status in that case.

                // This operation can be slow because of antivirus scanning.
                if (IsOfficeProblemFile())
                    stream = new FileStream(FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                // Open the file in non-exclusive mode in the general case.
                else
                    stream = new FileStream(FullPath, FileMode.Open, FileAccess.Read);
            }

            catch (Exception ex)
            {
                Debug.Assert(stream == null);

                // The file no longer exist. 
                if (!File.Exists(FullPath)) CurrentLocalStatus = LocalStatus.Absent;

                // We could not open the file. 
                else
                {
                    CurrentLocalStatus = LocalStatus.Error;
                    LocalError = ex.Message;
                    Logging.Log("UpdateLocalStatus(): cannot open file " + RelativePath + ": " + LocalError + ".");
                    RequestUpdate();
                }

                return;
            }

            try
            {
                // Get the file information.
                Syscalls.BY_HANDLE_FILE_INFORMATION bhfi; 
                Syscalls.GetFileInformationByHandle(stream.SafeFileHandle.DangerousGetHandle(),
                                                          out bhfi);
                UInt64 observedID = (bhfi.FileIndexHigh << 32) + bhfi.FileIndexLow;
                UInt64 observedSize = (bhfi.FileSizeHigh << 32) + bhfi.FileSizeLow;
                DateTime observedDate =
                    DateTime.FromFileTime((Int64)(((UInt64)bhfi.LastWriteTime.dwHighDateTime << 32) +
                                                  (UInt64)bhfi.LastWriteTime.dwLowDateTime));

                // If the observed information match the cached information, the
                // persistent status is current. In the case of problematic Office 
                // applications, always recompute the hash.
                if (!IsOfficeProblemFile() &&
                    PersistentLocalStatus != LocalStatus.None &&
                    observedID == LocalID  &&
                    observedSize == LocalSize &&
                    observedDate == LocalDate)
                {
                    CurrentLocalStatus = PersistentLocalStatus;
                }

                // We have to get information about the file using the hash. 
                else
                {
                    // Compute the hash. If we opened the file in non-exclusive mode,
                    // it is possible, though unlikely, that we compute the hash
                    // incorrectly. Normally, the filesystem watcher should send us an
                    // update event for the file and the modification date should
                    // change, so this is not a problem normally.

                    byte[] observedHash;
                    UInt64 ignored;
                    KfsHash.GetHashAndSize(stream, out observedHash, out ignored);

                    // The file matches the version we downloaded. The file is
                    // unmodified.
                    if (DownloadVersion != null && Base.ByteArrayEqual(DownloadVersion.Hash, observedHash))
                    {
                        UpdatePersistentStatus(LocalStatus.Unmodified, observedDate, observedID,
                                               observedSize, observedHash);
                    }

                    // The file matches the current version. Make this version the
                    // downloaded version. The file is unmodified.
                    else if (Base.ByteArrayEqual(CurrentVersion.Hash, observedHash))
                    {
                        UpdateDownloadVersion(CurrentVersion, false);
                        UpdatePersistentStatus(LocalStatus.Unmodified, observedDate, observedID,
                                               observedSize, observedHash);
                    }

                    // The file does not match either version.
                    else
                    {
                        // The file is modified.
                        if (!IsOfficeProblemFile())
                        {
                            UpdatePersistentStatus(LocalStatus.Modified, observedDate, observedID,
                                                   observedSize, observedHash);
                        }

                        // Apparently, the content of the file has not changed
                        // since we evaluated it.
                        else if (PersistentLocalStatus != LocalStatus.None &&
                            observedDate == LocalDate)
                        {
                            UpdatePersistentStatus(PersistentLocalStatus, observedDate, observedID,
                                                   observedSize, observedHash);
                        }

                        // The file is modified.
                        else
                        {
                            UpdatePersistentStatus(LocalStatus.Modified, observedDate, observedID,
                                                   observedSize, observedHash);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                CurrentLocalStatus = LocalStatus.Error;
                LocalError = ex.Message;
                Logging.Log("UpdateLocalStatus(): cannot compute status of " + RelativePath + ": " + LocalError + ".");
                RequestUpdate();
            }

            finally
            {
                if (stream != null) stream.Close();
                Logging.Log("Finished updating");
            }
        }

        /// <summary>
        /// Return true if the file is an Office file that gets
        /// modified by just opening it.
        /// </summary>
        public bool IsOfficeProblemFile()
        {
            return (FullPath.EndsWith(".xls") ||
                    FullPath.EndsWith(".ppt"));
        }

        /// <summary>
        /// Update the persistent and current local status. This cannot be used
        /// to set the status to "none".
        /// </summary>
        private void UpdatePersistentStatus(LocalStatus newStatus, DateTime observedDate,
                                            UInt64 observedID, UInt64 observedSize, byte[] observedHash)
        {
            Debug.Assert(observedHash != null);
            PersistentLocalStatus = CurrentLocalStatus = newStatus;
            
            LocalDate = observedDate;
            LocalID = observedID;
            LocalSize = observedSize;
            LocalHash = observedHash;

            Share.App.SetDirty("UpdatePersistentStatus");
        }

        /// <summary>
        /// Update the downloaded version of the file and force the refresh of
        /// the status of the file, if requested.
        /// </summary>
        public void UpdateDownloadVersion(KfsServerFileVersion NewDownloadVersion, bool refreshFlag)
        {
            DownloadVersion = NewDownloadVersion;
            
            if (refreshFlag)
            {
                PersistentLocalStatus = LocalStatus.None;
                CurrentLocalStatus = LocalStatus.None;
                RequestUpdate();
            }

            Share.App.SetDirty("UpdateDownloadVersion");
        }
    }

    /// <summary>
    /// Represents a user uploading a set of files.
    /// </summary>
    [Serializable]
    public class KfsServerFileUploader
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// ID of the user uploading the file.
        /// </summary>
        public UInt64 UserID;

        /// <summary>
        /// Commit ID associated to the upload.
        /// </summary>
        public UInt64 CommitID;

        /// <summary>
        /// Tree of files being uploaded indexed by inode ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsServerFile> FileTree = new SortedDictionary<ulong, KfsServerFile>();

        public KfsServerFileUploader(KfsShare S, UInt64 UID, UInt64 CID)
        {
            Share = S;
            UserID = UID;
            CommitID = CID;
        }
    }

    /// <summary>
    /// Represents an uploaded version of a file.
    /// </summary>
    [Serializable]
    public class KfsServerFileVersion
    {
        public UInt64 Inode;
        public UInt64 CommitID;
        public UInt32 UserID;
        public UInt64 Date;
        public UInt64 Size;
        public byte[] Hash;
    }

    /// <summary>
    /// The server view describes the current view of the share on the server.
    /// </summary>
    [Serializable]
    public class KfsServerView
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Reference to the server root directory.
        /// </summary>
        public KfsServerDirectory Root;

        /// <summary>
        /// Tree mapping inode IDs to KfsServerObject.
        /// </summary>
        public SortedDictionary<UInt64, KfsServerObject> InodeTree = new SortedDictionary<ulong, KfsServerObject>();

        /// <summary>
        /// Tree mapping commit IDs to KfsServerFileUploader.
        /// </summary>
        public SortedDictionary<UInt64, KfsServerFileUploader> UploaderTree = new SortedDictionary<ulong, KfsServerFileUploader>();

        public KfsServerView(KfsShare S)
        {
            Share = S;
            Share.ServerView = this;

            // Create the root directory.
            new KfsServerDirectory(Share, 0, 0, 0, 0, 0, "");
        }

        /// <summary>
        /// Return the object having the inode specified, if any.
        /// </summary>
        public KfsServerObject GetObjectByInode(UInt64 inode)
        {
            if (InodeTree.ContainsKey(inode)) return InodeTree[inode];
            return null;
        }

        /// <summary>
        /// Return the object having the path specified, if any.
        /// </summary>
        public KfsServerObject GetObjectByPath(String path)
        {
            String[] components = KfsPath.SplitRelativePath(path);
            KfsServerDirectory cur = Root;

            for (int i = 0; i < components.Length; i++)
            {
                String c = components[i];
                if (!cur.ChildTree.ContainsKey(c)) return null;
                KfsServerObject o = cur.ChildTree[c];
                if (i == components.Length - 1) return o;
                cur = o as KfsServerDirectory;
                if (cur == null) return null;
            }

            return cur;
        }

        /// <summary>
        /// Return an array containing all the paths in the view. 
        /// </summary>
        /// <param name="leafFirst">True if the leafs are added before their parent.</param>
        public List<String> GetPathArray(bool leafFirst)
        {
            List<String> a = new List<String>();
            GetPathArrayRecursive(a, Root, leafFirst);
            return a;
        }

        private void GetPathArrayRecursive(List<String> a, KfsServerDirectory c, bool lf)
        {
            if (!lf) a.Add(c.RelativePath);
            foreach (KfsServerObject o in c.ChildTree.Values)
            {
                if (o is KfsServerDirectory)
                {
                    GetPathArrayRecursive(a, o as KfsServerDirectory, lf);
                }
                else
                {
                    a.Add(o.RelativePath);
                }
            }
            if (lf) a.Add(c.RelativePath);
        }

        /// <summary>
        /// Fill the array specified with the operations contained in the phase 1
        /// event message specified.
        /// </summary>
        public void DecomposePhase1Event(AnpMsg m, out List<KfsServerOp> opList)
        {
            opList = new List<KfsServerOp>();
            UInt64 eid = m.ID;
            UInt64 date = m.Elements[1].UInt64;
            UInt32 uid = m.Elements[2].UInt32;
            UInt64 cid = m.Elements[4].UInt64;
            UInt32 nbChange = m.Elements[5].UInt32;
            int pos = 6;

            for (int i = 0; i < nbChange; i++)
            {
                UInt32 type = m.Elements[pos + 1].UInt32;
                KfsServerOp o;

                if (type == KAnpType.KANP_KFS_OP_CREATE_FILE || type == KAnpType.KANP_KFS_OP_CREATE_DIR)
                    o = new KfsCreateServerOp(Share, eid, date, uid, cid,
                                              type == KAnpType.KANP_KFS_OP_CREATE_FILE,
                                              m.Elements[pos + 2].UInt64,
                                              m.Elements[pos + 3].UInt64,
                                              m.Elements[pos + 4].String);

                else if (type == KAnpType.KANP_KFS_OP_UPDATE_FILE)
                    o = new KfsUpdateServerOp(Share, eid, date, uid, cid, m.Elements[pos + 2].UInt64);

                else if (type == KAnpType.KANP_KFS_OP_DELETE_FILE || type == KAnpType.KANP_KFS_OP_DELETE_DIR)
                    o = new KfsDeleteServerOp(Share, eid, date, uid, cid,
                                              type == KAnpType.KANP_KFS_OP_DELETE_FILE,
                                              m.Elements[pos + 2].UInt64);

                else if (type == KAnpType.KANP_KFS_OP_MOVE_FILE || type == KAnpType.KANP_KFS_OP_MOVE_DIR)
                    o = new KfsMoveServerOp(Share, eid, date, uid, cid,
                                            type == KAnpType.KANP_KFS_OP_MOVE_FILE,
                                            m.Elements[pos + 2].UInt64,
                                            m.Elements[pos + 3].UInt64,
                                            m.Elements[pos + 4].String);

                else throw new Exception("invalid server phase 1 operation type");

                opList.Add(o);

                int nbElem = (int)m.Elements[pos].UInt32;
                if (nbElem < 3) throw new Exception("invalid number of elements in server phase 1 operation");
                pos += nbElem;
            }
        }

        /// <summary>
        /// Extract the operation contained in the phase 2 event message specified.
        /// </summary>
        public void DecomposePhase2Event(AnpMsg m, out KfsPhase2ServerOp op)
        {
            UInt64 eid = m.ID;
            UInt64 date = m.Elements[1].UInt64;
            UInt32 uid = m.Elements[2].UInt32;
            UInt64 cid = m.Elements[4].UInt64;
            UInt32 nbFile = m.Elements[5].UInt32;
            int pos = 6;
            op = new KfsPhase2ServerOp(Share, eid, date, uid, cid);

            for (int i = 0; i < nbFile; i++)
            {
                KfsServerPhase2File f = new KfsServerPhase2File();
                f.Inode = m.Elements[pos + 0].UInt64;
                f.Size = m.Elements[pos + 1].UInt64;
                f.Hash = m.Elements[pos + 2].Bin;
                op.UploadArray.Add(f);
                pos += 3;
            }
        }
    }
}