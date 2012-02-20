using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using System.Diagnostics;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// Status of the path.
    /// </summary>
    public enum PathStatus
    {
        /// <summary>
        /// Undetermined status for lack of information.
        /// </summary>
        [Base.Description("File opened")]
        Undetermined,

        /// <summary>
        /// This path corresponds to a ghost server file.
        /// </summary>
        [Base.Description("Server Ghost")]
        ServerGhost,

        /// <summary>
        /// A file exists remotely but it has not been downloaded yet. Note that this
        /// status does not imply that the file can necessarily be downloaded yet. The
        /// first version might still be uploading.
        /// </summary>
        [Base.Description("Not Downloaded")]
        NotDownloaded,

        /// <summary>
        /// A file has been added by the user locally and it does not exist on the
        /// server.
        /// </summary>
        [Base.Description("Not yet shared")]
        NotAdded,

        /// <summary>
        /// The current version of a file has been modified locally.
        /// </summary>
        [Base.Description("Modified by you")]
        ModifiedCurrent,

        /// <summary>
        /// A stale version of a file has been modified locally, or the user added a
        /// new file locally which was already present on the server.
        /// </summary>
        [Base.Description("Conflict")]
        ModifiedStale,

        /// <summary>
        /// The current version of a file exists remotely and locally, and it has not
        /// been modified locally.
        /// </summary>
        [Base.Description("Up to date")]
        UnmodifiedCurrent,

        /// <summary>
        /// A stale but unmodified version of a file exists locally.
        /// </summary>
        [Base.Description("New version available")]
        UnmodifiedStale,

        /// <summary>
        /// A directory exists locally and/or remotely.
        /// </summary>
        Directory,

        /// <summary>
        /// A directory that exists locally conflicts with a file that exists
        /// remotely.
        /// </summary>
        [Base.Description("Conflict")]
        DirFileConflict,

        /// <summary>
        /// A file that exists locally conflicts with a directory that exists
        /// remotely.
        /// </summary>
        [Base.Description("Conflict")]
        FileDirConflict
    }

    /// <summary>
    /// Represent the status of a given unified path.
    /// </summary>
    public class KfsStatusPath
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Name in the parent directory. This is "" for the root.
        /// </summary>
        public String Name;

        /// <summary>
        /// Flag telling whether this directory should appear expanded in the 
        /// UI. Relevant only if Status == PathStatus.Directory. 
        /// </summary>
        public bool ExpandedFlag;

        /// <summary>
        /// Path to the object. This is "" for the root.
        /// </summary>
        public String Path;

        /// <summary>
        /// Status of the path.
        /// </summary>
        public PathStatus Status = PathStatus.Undetermined;

        /// <summary>
        /// The associated local object, if any.
        /// </summary>
        public KfsLocalObject LocalObject;

        /// <summary>
        /// The associated server object, if any.
        /// </summary>
        public KfsServerObject ServerObject;

        /// <summary>
        /// Tree that maps child names to KfsStatusPath.
        /// </summary>
        public SortedDictionary<String, KfsStatusPath> ChildTree = new SortedDictionary<string, KfsStatusPath>();

        /// <summary>
        /// Local file size if the file exists locally, file 
        /// size on the server if the file does not exist locally.
        /// </summary>
        public UInt64 Size
        {
            get
            {
                if ((Status == PathStatus.NotDownloaded && ((KfsServerFile)ServerObject).HasCurrentVersion()) ||
                    Status == PathStatus.UnmodifiedCurrent ||
                    Status == PathStatus.UnmodifiedStale)
                {
                    return ((KfsServerFile)ServerObject).CurrentVersion.Size;
                }
                else if (Status == PathStatus.ModifiedCurrent ||
                         Status == PathStatus.ModifiedStale)
                {
                    return ((KfsServerFile)ServerObject).LocalSize;
                }
                else if (Status == PathStatus.NotAdded ||
                         Status == PathStatus.Undetermined)
                {
                    return Base.GetFileSize(Share.MakeAbsolute(Path));
                }
                else
                {
                    return UInt64.MaxValue;
                }
            }
        }

        /// <summary>
        /// Return the last modified date of a given StatusPath.
        /// If the SP exists remotely and no locally modified version exists:
        ///     Get the last commit date.
        /// Else if the SP only exists locally, or if it is modified locally:
        ///     Get the file system information.
        /// </summary>
        public DateTime LastModifiedDate
        {
            get
            {
                if ((Status == PathStatus.NotDownloaded && ((KfsServerFile)ServerObject).HasCurrentVersion()) ||
                    Status == PathStatus.UnmodifiedCurrent ||
                    Status == PathStatus.UnmodifiedStale)
                {
                    return Base.KDateToDateTime(((KfsServerFile)ServerObject).CurrentVersion.Date);
                }
                else if (Status == PathStatus.NotDownloaded && !((KfsServerFile)ServerObject).HasCurrentVersion())
                {
                    // Corresponds to the first phase1 event of this file. The file
                    // does not have a KfsServerFileVersion, and it does not exist on
                    // the local disk.
                    return Base.KDateToDateTime(((KfsServerFile)ServerObject).CreationDate);
                }
                else if (Status == PathStatus.Directory && HasServerDir())
                {
                    return Base.KDateToDateTime(((KfsServerDirectory)ServerObject).CreationDate);
                }
                else if (Status == PathStatus.ModifiedCurrent ||
                         Status == PathStatus.ModifiedStale)
                {
                    return ((KfsServerFile)ServerObject).LocalDate;
                }
                else if (Status == PathStatus.NotAdded || 
                         Status == PathStatus.Directory || 
                         Status == PathStatus.Undetermined)
                {
                    return Base.GetLastModificationDate(Share.MakeAbsolute(Path));
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Parent of this path, if any.
        /// </summary>
        public KfsStatusPath Parent
        {
            get
            {
                if (IsRoot()) return null;
                KfsStatusPath sp = Share.StatusView.GetPath(KfsPath.DirName(Path));
                Debug.Assert(sp != null);
                return sp;
            }
        }

        /// <summary>
        /// This constructor creates the path but does NOT insert it in the view.
        /// </summary>
        /// <param name="S">Share</param>
        /// <param name="P">Local object, if any</param>
        /// <param name="N">Server object, if any</param>
        public KfsStatusPath(KfsShare s, String path, KfsLocalObject lo, KfsServerObject so)
        {
            Share = s;
            Path = path;
            Name = KfsPath.BaseName(path);
            LocalObject = lo;
            ServerObject = so;
        }

        /// <summary>
        /// Add the object at the appropriate location in the local view.
        /// </summary>
        public void AddToView()
        {
            Debug.Assert(!Share.StatusView.PathTree.ContainsKey(Path));

            if (IsRoot())
            {
                Debug.Assert(Name == "");
                Debug.Assert(Share.StatusView.Root == null);
                Share.StatusView.Root = this;
            }

            else
            {
                Debug.Assert(Name != "");
                KfsStatusPath p = Parent;
                Debug.Assert(p != null);
                Debug.Assert(!p.ChildTree.ContainsKey(Name));
                p.ChildTree[Name] = this;
            }

            Share.StatusView.PathTree[Path] = this;
        }

        /// <summary>
        /// Return true if this object is the root directory.
        /// </summary>
        public bool IsRoot()
        {
            return (Path == "");
        }

        /// <summary>
        /// Return true if the status is undetermined.
        /// </summary>
        public bool IsUndetermined()
        {
            return (Status == PathStatus.Undetermined);
        }

        /// <summary>
        /// Return true if the status represents a type conflict.
        /// </summary>
        public bool IsTypeConflict()
        {
            return (Status == PathStatus.DirFileConflict || Status == PathStatus.FileDirConflict);
        }

        /// <summary>
        /// Return true if the status represents a file. A ghost is
        /// considered to be a file.
        /// </summary>
        public bool IsFile()
        {
            switch (Status)
            {
                case PathStatus.ModifiedCurrent:
                case PathStatus.ModifiedStale:
                case PathStatus.NotAdded:
                case PathStatus.NotDownloaded:
                case PathStatus.ServerGhost:
                case PathStatus.UnmodifiedCurrent:
                case PathStatus.UnmodifiedStale:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Return true if the status represents a directory. 
        /// </summary>
        public bool IsDir()
        {
            return (Status == PathStatus.Directory);
        }

        /// <summary>
        /// Return true if the server object is non-null.
        /// </summary>
        public bool OnServer()
        {
            return (ServerObject != null);
        }

        /// <summary>
        /// Return true if the server object is non-null and is not a ghost.
        /// </summary>
        public bool OnServerAndNotGhost()
        {
            return (ServerObject != null && !ServerObject.IsGhost());
        }

        /// <summary>
        /// Return true if the server object is a ghost.
        /// </summary>
        public bool OnServerAndGhost()
        {
            return (ServerObject != null && ServerObject.IsGhost());
        }

        /// <summary>
        /// Return true if the local object is a file.
        /// </summary>
        public bool HasLocalFile()
        {
            return (LocalObject != null && LocalObject is KfsLocalFile);
        }

        /// <summary>
        /// Return true if the server object is a file.
        /// </summary>
        public bool HasServerFile()
        {
            return (ServerObject != null && ServerObject is KfsServerFile);
        }

        /// <summary>
        /// Return true if the local object is a directory.
        /// </summary>
        public bool HasLocalDir()
        {
            return (LocalObject != null && LocalObject is KfsLocalDirectory);
        }

        /// <summary>
        /// Return true if the server object is a directory.
        /// </summary>
        public bool HasServerDir()
        {
            return (ServerObject != null && ServerObject is KfsServerDirectory);
        }

        /// <summary>
        /// Update the status of path.
        /// </summary>
        public void UpdateStatus()
        {
            Debug.Assert(LocalObject != null || ServerObject != null);

            if (LocalObject == null)
            {
                // This is a server ghost.
                if (ServerObject.IsGhost()) Status = PathStatus.ServerGhost;

                // This is a remote directory.
                else if (ServerObject is KfsServerDirectory) Status = PathStatus.Directory;

                // This is a not-downloaded file.
                else Status = PathStatus.NotDownloaded;
            }

            else if (LocalObject is KfsLocalDirectory)
            {
                // There is no directory conflict.
                if (ServerObject == null || ServerObject is KfsServerDirectory || ServerObject.IsGhost())
                    Status = PathStatus.Directory;

                // There is a directory/file conflict.
                else Status = PathStatus.DirFileConflict;
            }

            else
            {
                Debug.Assert(LocalObject is KfsLocalFile);

                // The file has no version on the server.
                if (ServerObject == null ||
                    (ServerObject is KfsServerFile && !ServerObject.HasCurrentVersion()))
                {
                    Status = PathStatus.NotAdded;
                }

                // There is a file/directory conflict.
                else if (ServerObject is KfsServerDirectory)
                {
                    Status = PathStatus.FileDirConflict;
                }

                else
                {
                    Debug.Assert(ServerObject is KfsServerFile);
                    KfsLocalFile localFile = LocalObject as KfsLocalFile;
                    KfsServerFile serverFile = ServerObject as KfsServerFile;

                    // The current local status is unknown or the server view does not
                    // see the local file. In either case, the status is undetermined
                    // and we must ask for an update.
                    if (serverFile.CurrentLocalStatus == LocalStatus.Error ||
                        serverFile.CurrentLocalStatus == LocalStatus.Absent ||
                        serverFile.CurrentLocalStatus == LocalStatus.None)
                    {
                        Status = PathStatus.Undetermined;
                        Logging.Log(2, "Requesting update of file " + serverFile.RelativePath +
                                       " in status view because current local status is " +
                                       serverFile.CurrentLocalStatus + ".");
                        serverFile.RequestUpdate();
                    }

                    // The file is unmodified.
                    else if (serverFile.CurrentLocalStatus == LocalStatus.Unmodified)
                    {
                        // The current server version is the downloaded version, so the
                        // file is current.
                        if (serverFile.CurrentVersion == serverFile.DownloadVersion)
                            Status = PathStatus.UnmodifiedCurrent;

                        // The current server version is not downloaded version, so the
                        // file is stale.
                        else Status = PathStatus.UnmodifiedStale;
                    }

                    // The file is modified.
                    else
                    {
                        Debug.Assert(serverFile.CurrentLocalStatus == LocalStatus.Modified);

                        // The current server version is the downloaded version, so
                        // there is no conflict, as would say Darth Vador.
                        if (serverFile.CurrentVersion == serverFile.DownloadVersion)
                            Status = PathStatus.ModifiedCurrent;

                        // The downloaded version exists but it is not the current
                        // version, so there is a conflict, or the user created his own
                        // file at the location of a non-downloaded server file. Since
                        // the user is possibly unaware of this fact, we must also
                        // report it as modified stale (conflict).
                        else Status = PathStatus.ModifiedStale;
                    }
                }
            }
        }
    }

    /// <summary>
    /// The status view merges the local view with the server view and describes
    /// the status of each path that exists in the local view and/or the server
    /// view. It should be noted that the status view becomes inconsistent as
    /// soon as the local or server view is modified.
    /// </summary>
    public class KfsStatusView
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Root directory.
        /// </summary>
        public KfsStatusPath Root;

        /// <summary>
        /// Tree that maps paths to KfsStatusPath. The paths must not end with
        /// a delimiter.
        /// </summary>
        public SortedDictionary<String, KfsStatusPath> PathTree = new SortedDictionary<string, KfsStatusPath>();

        public KfsStatusView(KfsShare S)
        {
            Share = S;
            Share.StatusView = this;
        }

        /// <summary>
        /// Return the status corresponding to the path specified, if it exists.
        /// </summary>
        public KfsStatusPath GetPath(String path)
        {
            path = KfsPath.StripTrailingDelim(path);
            if (!PathTree.ContainsKey(path)) return null;
            return PathTree[path];
        }

        /// <summary>
        /// Return an array containing the status paths having the prefix
        /// specified.
        /// </summary>
        /// <param name="leafFirst">True if the leafs are added before their parent.</param>
        public List<KfsStatusPath> GetPathArray(String prefix, bool leafFirst)
        {
            List<KfsStatusPath> a = new List<KfsStatusPath>();
            KfsStatusPath sp = GetPath(prefix);
            if (sp != null) GetPathArrayRecursive(a, sp, leafFirst);
            return a;
        }

        /// <summary>
        /// Helper method for GetPathArray().
        /// </summary>
        private void GetPathArrayRecursive(List<KfsStatusPath> a, KfsStatusPath c, bool lf)
        {
            if (!lf) a.Add(c);
            foreach (KfsStatusPath s in c.ChildTree.Values) GetPathArrayRecursive(a, s, lf);
            if (lf) a.Add(c);
        }

        /// <summary>
        /// Clear the content of the status view.
        /// </summary>
        public void Clear()
        {
            Root = null;
            PathTree.Clear();
        }

        /// <summary>
        /// Update the status view from the current server and local views.
        /// </summary>
        public void UpdateView()
        {
            // There are no type conflicts at this point.
            Share.TypeConflictFlag = false;
            Clear();

            List<String> localPathArray = Share.LocalView.GetPathArray(false);
            List<String> serverPathArray = Share.ServerView.GetPathArray(false);

            // Find the entries that are present in both the local view and in the
            // server view.
            foreach (String path in localPathArray)
            {
                KfsLocalObject lo = Share.LocalView.GetObjectByPath(path);
                KfsServerObject so = Share.ServerView.GetObjectByPath(path);
                Debug.Assert(lo != null);
                KfsStatusPath sp = new KfsStatusPath(Share, path, lo, so);
                sp.AddToView();
            }

            // Find the entries that are present only in the server view.
            foreach (String path in serverPathArray)
            {
                if (GetPath(path) != null) continue;
                KfsServerObject so = Share.ServerView.GetObjectByPath(path);
                Debug.Assert(so != null);
                KfsStatusPath sp = new KfsStatusPath(Share, path, null, so);
                sp.AddToView();
            }

            // Link the entries properly, compute their status and remember conflicts.
            Debug.Assert(Root != null);

            foreach (KfsStatusPath sp in PathTree.Values)
            {
                sp.UpdateStatus();
                if (sp.Status == PathStatus.DirFileConflict || sp.Status == PathStatus.FileDirConflict)
                    Share.TypeConflictFlag = true;
            }

            // If there are type conflicts, disallow all user operations but downloads.
            if (Share.TypeConflictFlag)
                Share.DisallowUserOp("type conflicts detected", AllowedOpStatus.Download);
        }

        /// <summary>
        /// Display the content of the status view. Useful for debugging.
        /// </summary>
        public override string ToString()
        {
            String s = "Status view:\n";
            foreach (KfsStatusPath sp in GetPathArray("", false))
                s += sp.Path + ": " + sp.Status + ".\n";
            return s;
        }
    }
}