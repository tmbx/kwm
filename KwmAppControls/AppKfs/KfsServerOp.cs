using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum ApplyOpRequestType
    {
        /// <summary>
        /// Rename the specified path.
        /// </summary>
        Rename,

        /// <summary>
        /// Close or delete the specified file.
        /// </summary>
        Close,

        /// <summary>
        /// An unexpected error occurred while applying the operation. The user
        /// must fix the problem specified.
        /// </summary>
        Error
    }

    /// <summary>
    /// This class represents a request presented to the user that will allow
    /// the current server operation to be applied on the filesystem.
    /// </summary>
    public class KfsApplyOpRequest
    {
        /// <summary>
        /// Type of the request.
        /// </summary>
        public ApplyOpRequestType Type;

        /// <summary>
        /// Relative path to the associated file or directory. This is null if
        /// there is no associated path.
        /// </summary>
        public String RelPath;

        /// <summary>
        /// String describing the error that occurred, if any. This is null if
        /// no error occurred.
        /// </summary>
        public String Error;

        public KfsApplyOpRequest(ApplyOpRequestType type, String relPath, String error)
        {
            Type = type;
            RelPath = relPath;
            Error = error;
        }
    }

    /// <summary>
    /// Represent an operation in a phase 1 or 2 event.
    /// </summary>
    [Serializable]
    abstract public class KfsServerOp
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Unique operation ID.
        /// </summary>
        public UInt64 OpID;

        /// <summary>
        /// KANP ID of the event associated to the operation.
        /// </summary>
        public UInt64 EventID;

        /// <summary>
        /// Date of the operation.
        /// </summary>
        public UInt64 Date;

        /// <summary>
        /// ID of the user who generated the operation.
        /// </summary>
        /// 
        public UInt32 UserID;

        /// <summary>
        /// Commit ID of the event associated to the operation.
        /// </summary>
        public UInt64 CommitID;

        public KfsServerOp(KfsShare s, UInt64 eid, UInt64 date, UInt32 uid, UInt64 cid)
        {
            Share = s;
            EventID = eid;
            OpID = Share.ServerOpID++;
            Date = date;
            UserID = uid;
            CommitID = cid;
        }

        /// <summary>
        /// This method should be called to validate the operation before applying
        /// it. It throws an exception on error.
        /// </summary>
        public abstract void Validate();

        /// <summary>
        /// Apply the operation on the server view. This method should not throw an
        /// exception.
        /// </summary>
        public abstract void ApplyOnServerView();

        /// <summary>
        /// Apply the operation on the filesystem. The method returns a request to
        /// to present to the user if the operation cannot be applied. The
        /// 'fsModifiedFlag' is set to true if the filesystem has been modified. This 
        /// method should not throw an exception. Note that it is safe to apply the
        /// same operation twice on the filesystem if no subsequent operations were
        /// applied.
        /// </summary>
        public KfsApplyOpRequest ApplyOnFileSystem(out bool fsModifiedFlag)
        {
            // We did not modify the filesystem yet.
            fsModifiedFlag = false;

            try
            {
                // Abort if the operation is already applied.
                if (IsAlreadyApplied()) return null;

                // Apply the operation.
                KfsApplyOpRequest req = ApplyOnFileSystemInternal(ref fsModifiedFlag);
                
                // We could not apply the operation.
                if (req != null) return req;

                // Remember that we applied the operation.
                MarkAsApplied();
                return null;
            }

            catch (Exception ex)
            {
                return new KfsApplyOpRequest(ApplyOpRequestType.Error, null, ex.Message);
            }
        }

        /// <summary>
        /// Overriden to actually apply the operation on the filesystem.
        /// </summary>
        protected virtual KfsApplyOpRequest ApplyOnFileSystemInternal(ref bool fsModifiedFlag)
        {
            return null;
        }

        /// <summary>
        /// Helper method for Validate().
        /// </summary>
        protected void Assert(bool cond, String reason)
        {
            if (!cond) throw new Exception("received invalid server operation: " + reason);
        }

        /// <summary>
        /// Helper method for Validate().
        /// </summary>
        protected void ValidateName(String Name)
        {
            Assert(KfsPath.IsValidFileName(Name), "invalid file name : " + Name);
        }

        /// <summary>
        /// Return a reference to the uploader of this file if it exists. The user
        /// ID is validated.
        /// </summary>
        protected KfsServerFileUploader GetUploader()
        {
            if (Share.ServerView.UploaderTree.ContainsKey(CommitID))
            {
                KfsServerFileUploader u = Share.ServerView.UploaderTree[CommitID];
                Assert(u.UserID == UserID, "uploader has the wrong user ID");
                return u;
            }

            return null;
        }

        /// <summary>
        /// Add the file specified in the appropriate uploader set.
        /// </summary>
        protected void AddUpload(KfsServerFile f)
        {
            KfsServerFileUploader u = GetUploader();

            if (u == null)
            {
                u = new KfsServerFileUploader(Share, UserID, CommitID);
                Share.ServerView.UploaderTree[CommitID] = u;
            }

            u.FileTree[f.Inode] = f;
            f.UploaderSet.Add(u);
        }

        /// <summary>
        /// Return the relative path obtained by combining the path to the inode
        /// specified with the name specified. 'name' can be null if only the path
        /// to the inode is needed.
        /// </summary>
        protected String GetRelPath(UInt64 inode, String name)
        {
            KfsServerObject o = Share.ServerView.GetObjectByInode(inode);
            Debug.Assert(o != null);
            String path = o.RelativePath;

            if (name != null)
            {
                if (path != "") path += "/";
                path += name;
            }

            return path;
        }

        /// <summary>
        /// Return a request to rename the path specified.
        /// </summary>
        protected KfsApplyOpRequest AskRenamePath(String relPath)
        {
            return new KfsApplyOpRequest(ApplyOpRequestType.Rename, relPath, "The file or directory " + Share.MakeAbsolute(relPath) + " is in the way of an operation.");
        }

        /// <summary>
        /// Try to remove the specified directory recursively. If the directory
        /// contains a file, a request to rename the directory is returned,
        /// otherwise null is returned.
        /// </summary>
        protected KfsApplyOpRequest RemoveFsDir(String relPath, ref bool fsModifiedFlag)
        {
            if (RemoveFsDirRecursive(Share.MakeAbsolute(relPath), ref fsModifiedFlag)) return null;
            return new KfsApplyOpRequest(ApplyOpRequestType.Rename, relPath, "Unable to delete " + Share.MakeAbsolute(relPath) + ": the directory is not empty.");
        }

        /// <summary>
        /// Helper method for RemoveFsDir(). It returns true if the directory
        /// specified was removed.
        /// </summary>
        private bool RemoveFsDirRecursive(String fullPath, ref bool fsModifiedFlag)
        {
            DirectoryInfo di = new DirectoryInfo(fullPath);
            if (di.GetFiles().Length > 0) return false;

            foreach (DirectoryInfo d in di.GetDirectories())
                if (!RemoveFsDirRecursive(fullPath + "\\" + d.Name, ref fsModifiedFlag)) return false;

            File.SetAttributes(fullPath, FileAttributes.Normal);
            Directory.Delete(fullPath);
            fsModifiedFlag = true;
            return true;
        }

        /// <summary>
        /// Return true if the operation was already applied on the filesystem.
        /// </summary>
        private bool IsAlreadyApplied()
        {
            String path = Share.AppliedOpFilePath;
            if (!File.Exists(path)) return false;
            StreamReader s = null;

            try
            {
                s = new StreamReader(path);
                String data = s.ReadLine();
                s.Close();
                s = null;
                UInt64 lastId;
                return (UInt64.TryParse(data, out lastId) && lastId >= OpID);
            }

            catch (Exception)
            {
                if (s != null) s.Close();
                return false;
            }
        }

        /// <summary>
        /// Mark the operation as having been applied.
        /// </summary>
        private void MarkAsApplied()
        {
            StreamWriter s = null;
            String permPath = Share.AppliedOpFilePath;
            String tmpPath = permPath + ".tmp";

            try
            {
                s = new StreamWriter(tmpPath);
                s.WriteLine(OpID);
                s.Close();
                s = null;

                // Ideally we'd have atomic semantics, but .Net sucks.
                File.Copy(tmpPath, permPath, true);
            }

            catch (Exception)
            {
                if (s != null) s.Close();
                throw;
            }
        }

        public override String ToString()
        {
            return "Event ID " + EventID + ", op ID " + OpID + ", date " + Date + ", user ID " + UserID +
                   ", commit ID " + CommitID + ".\n";
        }
    }

    /// <summary>
    /// Represent a create file/directory operation.
    /// </summary>
    [Serializable]
    public class KfsCreateServerOp : KfsServerOp
    {
        /// <summary>
        /// True if this is a create file operation.
        /// </summary>
        public bool IsFile;

        public UInt64 CreatedInode;
        public UInt64 ParentInode;
        public String Name;

        public KfsCreateServerOp(KfsShare s, UInt64 eid, UInt64 date, UInt32 uid, UInt64 cid, bool f, UInt64 ci, UInt64 pi, String n)
            : base(s, eid, date, uid, cid)
        {
            IsFile = f;
            CreatedInode = ci;
            ParentInode = pi;
            Name = n;
        }

        public override void Validate()
        {
            ValidateName(Name);
            Assert(CreatedInode != 0, "operation on root");
            KfsServerDirectory p = Share.ServerView.GetObjectByInode(ParentInode) as KfsServerDirectory;
            Assert(p != null, "parent does not exist");
            Assert(!p.ChildTree.ContainsKey(Name), "object name already exists");
            Assert(Share.ServerView.GetObjectByInode(CreatedInode) == null, "inode already exists");
        }

        public override void ApplyOnServerView()
        {
            if (IsFile)
            {
                AddUpload(new KfsServerFile(Share, CreatedInode, ParentInode, CommitID, UserID, Date, Name));
            }

            else
            {
                new KfsServerDirectory(Share, CreatedInode, ParentInode, CommitID, UserID, Date, Name);
            }
        }

        protected override KfsApplyOpRequest ApplyOnFileSystemInternal(ref bool fsModifiedFlag)
        {
            String destRelPath = GetRelPath(ParentInode, Name);
            String destFullPath = Share.MakeAbsolute(destRelPath);

            if (IsFile)
            {
                if (Directory.Exists(destFullPath)) return RemoveFsDir(destRelPath, ref fsModifiedFlag);
            }

            else
            {
                if (File.Exists(destFullPath)) return AskRenamePath(destRelPath);
                if (!Directory.Exists(destFullPath))
                {
                    fsModifiedFlag = true;
                    Share.AddMissingLocalDirectories(destRelPath);
                }
            }

            return null;
        }

        public override String ToString()
        {
            return base.ToString() + "Create " + (IsFile ? "file" : "directory") + ", created inode " +
                   CreatedInode + ", parent inode " + ParentInode + ", name " + Name + ".";
        }
    }

    /// <summary>
    /// Represent a file update operation.
    /// </summary>
    [Serializable]
    public class KfsUpdateServerOp : KfsServerOp
    {
        public UInt64 Inode;

        /// <summary>
        /// Updated file name.
        /// </summary>
        public String Name;

        public KfsUpdateServerOp(KfsShare s, UInt64 eid, UInt64 date, UInt32 uid, UInt64 cid, UInt64 i)
            : base(s, eid, date, uid, cid)
        {
            Inode = i;
        }

        public override void Validate()
        {
            KfsServerFile f = Share.ServerView.GetObjectByInode(Inode) as KfsServerFile;
            Assert(f != null, "file to update does not exist");
            KfsServerFileUploader u = GetUploader();
            if (u != null) Assert(!u.FileTree.ContainsKey(f.Inode), "file already being uploaded");
        }

        public override void ApplyOnServerView()
        {
            Name = this.Share.ServerView.GetObjectByInode(Inode).Name;

            KfsServerFile f = Share.ServerView.GetObjectByInode(Inode) as KfsServerFile;
            f.CommitID = CommitID;
            f.LastCommitDate = Date;
            AddUpload(f);
        }

        public override String ToString()
        {
            return base.ToString() + "Update file, inode " + Inode + ".";
        }
    }

    /// <summary>
    /// Represent a file or directory removal operation.
    /// </summary>
    [Serializable]
    public class KfsDeleteServerOp : KfsServerOp
    {
        /// <summary>
        /// True if this is a delete file operation.
        /// </summary>
        public bool IsFile;

        public UInt64 Inode;

        /// <summary>
        /// Name of the deleted object.
        /// </summary>
        public String Name;

        public KfsDeleteServerOp(KfsShare s, UInt64 eid, UInt64 date, UInt32 uid, UInt64 cid, bool f, UInt64 i)
            : base(s, eid, date, uid, cid)
        {
            IsFile = f;
            Inode = i;
        }

        public override void Validate()
        {
            Assert(Inode != 0, "operation on root");
            KfsServerObject o = Share.ServerView.GetObjectByInode(Inode);
            Assert(o != null, "object to delete does not exist");

            if (IsFile)
            {
                KfsServerFile f = o as KfsServerFile;
                Assert(f != null, "object to delete is a directory");
            }

            else
            {
                KfsServerDirectory d = o as KfsServerDirectory;
                Assert(d != null, "object to delete is a file");
                Assert(d.ChildTree.Count == 0, "directory is not empty");
            }
        }

        public override void ApplyOnServerView()
        {
            // Remember item name we just removed.
            Name = this.Share.ServerView.GetObjectByInode(Inode).Name;
            Share.ServerView.GetObjectByInode(Inode).RemoveFromView();
        }

        protected override KfsApplyOpRequest ApplyOnFileSystemInternal(ref bool fsModifiedFlag)
        {
            String destRelPath = GetRelPath(Inode, null);
            String destFullPath = Share.MakeAbsolute(destRelPath);

            if (IsFile)
            {
                if (Directory.Exists(destFullPath)) return RemoveFsDir(destRelPath, ref fsModifiedFlag);

                if (File.Exists(destFullPath))
                {
                    // Get the server file corresponding to the file.
                    KfsServerFile f = Share.ServerView.GetObjectByPath(destRelPath) as KfsServerFile;
                    Debug.Assert(f != null);

                    // The server file does not have a current version, so 
                    // don't delete the file -- it never really existed.
                    if (!f.HasCurrentVersion()) return null;

                    // Update the local status of the file.
                    f.UpdateLocalStatus();

                    // If the file is modified, don't delete it.
                    if (f.CurrentLocalStatus == LocalStatus.Modified) return null;

                    // If the file is not unmodified, complain to the user. If 
                    // there was an error determining the local status, the 
                    // error is made available to the user. Otherwise, it seems
                    // there was a race condition. It is safer to let the user 
                    // deal with the resulting mess (try again later).
                    if (f.CurrentLocalStatus != LocalStatus.Unmodified)
                        return new KfsApplyOpRequest(ApplyOpRequestType.Close, destRelPath, f.LocalError);

                    // Delete the file.
                    try
                    {
                        File.SetAttributes(destFullPath, FileAttributes.Normal);
                        File.Delete(destFullPath);
                        fsModifiedFlag = true;
                    }

                    catch (Exception ex)
                    {
                        return new KfsApplyOpRequest(ApplyOpRequestType.Close, destRelPath, ex.Message);
                    }
                }
            }

            else
            {
                if (File.Exists(destFullPath)) return AskRenamePath(destRelPath);
                if (Directory.Exists(destFullPath)) return RemoveFsDir(destRelPath, ref fsModifiedFlag);
            }

            return null;
        }

        public override String ToString()
        {
            return base.ToString() + "Delete " + (IsFile ? "file" : "directory") + ", inode " + Inode + ".";
        }
    }

    /// <summary>
    /// Represent a file or directory move operation.
    /// </summary>
    [Serializable]
    public class KfsMoveServerOp : KfsServerOp
    {
        /// <summary>
        /// True if this is a move file operation.
        /// </summary>
        public bool IsFile;

        /// <summary>
        /// Inode to move.
        /// </summary>
        public UInt64 MovedInode;

        /// <summary>
        /// Destination directory.
        /// </summary>
        public UInt64 DestinationDirInode;

        /// <summary>
        /// Name of the item in its original location.
        /// </summary>
        public String SrcName;

        /// <summary>
        /// Name of the item in its new location.
        /// </summary>
        public String DestName;

        /// <summary>
        /// Original location of the item, slash-terminated.
        /// </summary>
        public String SrcDirPath;

        /// <summary>
        /// New item location, slash-terminated.
        /// </summary>
        public String DestDirPath;

        public KfsMoveServerOp(KfsShare s, UInt64 eid, UInt64 date, UInt32 uid, UInt64 cid, bool f, UInt64 mi, UInt64 di, String n)
            : base(s, eid, date, uid, cid)
        {
            IsFile = f;
            MovedInode = mi;
            DestinationDirInode = di;
            DestName = n;
        }

        public override void Validate()
        {
            ValidateName(DestName);
            Assert(MovedInode != 0, "operation on root");

            KfsServerObject o = Share.ServerView.GetObjectByInode(MovedInode);
            Assert(o != null, "object to move does not exist");
            if (IsFile) Assert(o is KfsServerFile, "object to move is a directory");
            else Assert(o is KfsServerDirectory, "object to move is a file");

            KfsServerDirectory d = Share.ServerView.GetObjectByInode(DestinationDirInode) as KfsServerDirectory;
            Assert(d != null, "destination does not exist");
            Assert(!d.ChildTree.ContainsKey(DestName), "object name already exists");

            KfsServerDirectory p = o.Parent;
            while (p != null)
            {
                Assert(p.Inode != MovedInode, "making directory child of itself");
                p = p.Parent;
            }
        }

        public override void ApplyOnServerView()
        {
            // Save data right before we apply the server operation.
            DestDirPath = KfsPath.AddTrailingSlash(Share.ServerView.GetObjectByInode(DestinationDirInode).RelativePath, true);

            // Get the moved ServerObject.
            KfsServerObject o = Share.ServerView.GetObjectByInode(MovedInode);
            SrcDirPath = KfsPath.AddTrailingSlash(o.Parent.RelativePath, true);
            SrcName = o.Name;

            // Apply the operation itself.
            o.RemoveFromView();
            o.ParentInode = DestinationDirInode;
            o.Name = DestName;
            o.AddToView();
        }

        protected override KfsApplyOpRequest ApplyOnFileSystemInternal(ref bool fsModifiedFlag)
        {
            String srcRelPath = GetRelPath(MovedInode, null);
            String srcFullPath = Share.MakeAbsolute(srcRelPath);
            String destRelPath = GetRelPath(DestinationDirInode, DestName);
            String destFullPath = Share.MakeAbsolute(destRelPath);

            if ((IsFile && File.Exists(srcFullPath)) || (!IsFile && Directory.Exists(srcFullPath)))
            {
                if (File.Exists(destFullPath) || Directory.Exists(destFullPath))
                    return AskRenamePath(destRelPath);

                // Create the destination directory, if required.
                fsModifiedFlag = true;
                Share.AddMissingLocalDirectories(KfsPath.DirName(destRelPath));

                // Perform the move.
                if (IsFile) File.Move(srcFullPath, destFullPath);
                else Directory.Move(srcFullPath, destFullPath);
            }

            return null;
        }

        public override String ToString()
        {
            return base.ToString() + "Move " + (IsFile ? "file" : "directory") + ", moved inode " +
                   MovedInode + ", destination inode " + DestinationDirInode + ", name " + DestName + ".";
        }
    }

    /// <summary>
    /// Represent a phase 2 operation.
    /// </summary>
    [Serializable]
    public class KfsPhase2ServerOp : KfsServerOp
    {
        /// <summary>
        /// Array of files which have been successfully uploaded.
        /// </summary>
        public List<KfsServerPhase2File> UploadArray = new List<KfsServerPhase2File>();

        public KfsPhase2ServerOp(KfsShare s, UInt64 eid, UInt64 date, UInt32 uid, UInt64 cid)
            : base(s, eid, date, uid, cid)
        {
        }

        public override void Validate()
        {
            KfsServerFileUploader u = GetUploader();
            Assert(u != null, "no uploader associated to phase 2 operation");

            HashedSet<UInt64> InodeSet = new HashedSet<UInt64>();
            foreach (KfsServerPhase2File f in UploadArray)
            {
                // It is possible that the file referred to by f.Inode was deleted.
                // Do not assume that it is present in the inode tree of the
                // server view.
                Assert(!InodeSet.Contains(f.Inode), "same inode specified twice");
                Assert(u.FileTree.ContainsKey(f.Inode), "inode not in uploader set");
                InodeSet.Add(f.Inode);
            }
        }

        public override void ApplyOnServerView()
        {
            KfsServerFileUploader u = GetUploader();

            // Process the uploaded files.
            foreach (KfsServerPhase2File f in UploadArray)
            {
                KfsServerFile g = u.FileTree[f.Inode];
                Debug.Assert(g.Inode == f.Inode);
                Debug.Assert(g.UploaderSet.Contains(u));

                // Create a new file version.
                KfsServerFileVersion v = new KfsServerFileVersion();
                v.Inode = f.Inode;
                v.CommitID = CommitID;
                v.UserID = UserID;
                v.Date = Date;
                v.Size = f.Size;
                v.Hash = f.Hash;

                // This version is more recent than the last uploaded version.
                if (g.CurrentVersion == null || g.CurrentVersion.CommitID < CommitID)
                    g.CurrentVersion = v;

                // If this is the first upload, force an update to check the hash
                // of the local file against this version.
                if (g.PersistentLocalStatus == LocalStatus.None)
                    g.RequestUpdate();

                // If the local file appears to be modified, check if the hash of
                // the new version matches the hash of the local file. If so,
                // update the downloaded version and force the update of the
                // current local status.
                else if (g.PersistentLocalStatus == LocalStatus.Modified)
                {
                    if (Base.ByteArrayEqual(g.LocalHash, v.Hash))
                    {
                        g.DownloadVersion = v;
                        g.PersistentLocalStatus = LocalStatus.Unmodified;
                        g.RequestUpdate();
                    }
                }

                // Remove the file from the uploader set.
                g.UploaderSet.Remove(u);
                u.FileTree.Remove(g.Inode);
            }

            // Process the cancelled uploads.
            foreach (KfsServerFile g in u.FileTree.Values)
            {
                Debug.Assert(g.UploaderSet.Contains(u));
                g.UploaderSet.Remove(u);
            }

            u.FileTree.Clear();

            // Remove the uploader from the uploader tree.
            Share.ServerView.UploaderTree.Remove(u.CommitID);
        }

        public override String ToString()
        {
            String s = base.ToString() + "Phase 2:\n";
            foreach (KfsServerPhase2File f in UploadArray)
                s += "Inode " + f.Inode + ", size " + f.Size + ", hash " + Base.HexStr(f.Hash) + ".\n";
            s += "\n";
            return s;
        }
    }

    /// <summary>
    /// Represent a phase 2 server file.
    /// </summary>
    [Serializable]
    public class KfsServerPhase2File
    {
        public UInt64 Inode;
        public UInt64 Size;
        public byte[] Hash;
    }
}