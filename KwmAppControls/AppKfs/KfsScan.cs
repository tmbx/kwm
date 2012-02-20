using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;
using kwm.Utils;
using System.Windows.Forms;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// This thread performs the initial scan.
    /// </summary>
    public class KfsInitialScanThread : KwmWorkerThread
    {
        private KfsShare m_share;
        private KfsScan m_scan;

        public KfsInitialScanThread(KfsShare _share)
        {
            m_share = _share;
        }

        protected override void OnCompletion()
        {
            try
            {
                if (Status == WorkerStatus.Cancelled ||
                    Status == WorkerStatus.Failed)
                    m_share.OnInitialScanCompleted(null, false, FailException);
                else if (Status == WorkerStatus.Success)
                    m_share.OnInitialScanCompleted(m_scan, true, null);
                else Debug.Assert(false);
            }
            catch (Exception e)
            {
                Base.HandleException(e, true);
            }
        }

        protected override void Run()
        {
            m_scan = new KfsScan(m_share, "");
            m_scan.Scan();
        }
    }

    /// <summary>
    /// This thread performs a partial scan.
    /// </summary>
    public class KfsPartialScanThread : KwmWorkerThread
    {
        private KfsShare m_share;
        private List<KfsScan> m_results = new List<KfsScan>();

        public KfsPartialScanThread(KfsShare _share, List<string> _toScan)
        {
            m_share = _share;

            foreach (string path in _toScan)
            {
                m_results.Add(new KfsScan(m_share, path));
            }
        }

        protected override void OnCompletion()
        {
            try
            {
                if (Status == WorkerStatus.Cancelled ||
                    Status == WorkerStatus.Failed)
                    m_share.OnPartialScanCompleted(false, null, FailException);
                else if (Status == WorkerStatus.Success)
                    m_share.OnPartialScanCompleted(true, m_results, null);
                else Debug.Assert(false);
            }
            catch (Exception e)
            {
                Base.HandleException(e, true);
            }
        }

        protected override void Run()
        {
            foreach (KfsScan scan in m_results)
            {
                scan.Scan();
            }
        }
    }

    /// <summary>
    /// The class contains the list of paths that need to be scanned in the
    /// share.
    /// </summary>
    public class KfsStaleTree
    {
        private KfsStaleTreeEntry m_root;

        /// <summary>
        /// This entry corresponds to the root directory of the share. This
        /// field is null if no entry in the tree is stale.
        /// </summary>
        public KfsStaleTreeEntry Root
        {
            get
            {
                return m_root;
            }
        }

        /// <summary>
        /// Do we have something stale in here?
        /// </summary>
        public bool IsStale
        {
            get
            {
                return (m_root != null);
            }
        }

        public KfsStaleTree(KfsStaleTreeEntry _entry)
        {
            m_root = _entry;
        }

        /// <summary>
        /// Mark the path specified as stale.
        /// </summary>
        public void Add(String _path)
        {
            // If the root is already stale, stop.
            if (m_root != null && m_root.IsStale())
                return;

            // Create the root if needed.
            if (m_root == null)
            {
                m_root = new KfsStaleTreeEntry("");
            }

            // Split the path into individual components.
            String[] pathComponents = KfsPath.SplitRelativePath(_path);

            // Walk the tree until we find a stale entry or 
            // we create the full path.
            KfsStaleTreeEntry component = m_root;

            foreach (String c in pathComponents)
            {
                // There is already an entry with that name.
                if (component.ChildTree.ContainsKey(c))
                {
                    component = component.ChildTree[c];

                    // The entry is stale, so stop here.
                    if (component.IsStale())
                        return;
                }
                else
                {
                    // No entry exist with that name, so create it.
                    KfsStaleTreeEntry newEntry = new KfsStaleTreeEntry(c);
                    component.ChildTree.Add(c, newEntry);
                    component = newEntry;
                }
            }
            component.MarkStale();
        }

        /// <summary>
        /// Return an array containing the stale paths that must be scanned.
        /// </summary>
        public List<string> GetStalePathArray()
        {
            List<string> staleArray = new List<string>();
            if (m_root != null) GetStalePathArrayRecursive(staleArray, m_root, "");
            return staleArray;
        }

        /// <summary>
        /// Helper method for GetStalePathArray(). 
        /// </summary>
        private void GetStalePathArrayRecursive(List<string> _staleArray, KfsStaleTreeEntry _entry, String _path)
        {
            if (_entry.IsStale())
            {
                _staleArray.Add(_path);
                return;
            }

            foreach (KfsStaleTreeEntry child in _entry.ChildTree.Values)
            {
                String cp = _path;
                if (cp != "") cp += "/";
                cp += child.Name;
                GetStalePathArrayRecursive(_staleArray, child, cp);
            }
        }

        /// <summary>
        /// Clear all entries from the tree.
        /// </summary>
        public void Clear()
        {
            m_root = null;
        }
    }

    /// <summary>
    /// This class represents an entry in the stale tree.
    /// </summary>
    public class KfsStaleTreeEntry
    {
        private SortedDictionary<string, KfsStaleTreeEntry> m_childTree =
            new SortedDictionary<string, KfsStaleTreeEntry>();

        private String m_name = "";

        /// <summary>
        /// If this tree is empty, then this entry is stale. Otherwise, this
        /// tree contains the references to the entries present in this entry.
        /// </summary>
        public SortedDictionary<string, KfsStaleTreeEntry> ChildTree
        {
            get
            {
                return m_childTree;
            }
        }

        /// <summary>
        /// Name of the entry. This field is "" for the root entry.
        /// </summary>
        public String Name
        {
            get
            {
                return m_name;
            }
        }

        public KfsStaleTreeEntry(String _name)
        {
            m_name = _name;
        }

        /// <summary>
        /// Is this entry stale?
        /// </summary>
        public bool IsStale()
        {
            return (m_childTree.Count == 0);
        }

        public void MarkStale()
        {
            m_childTree.Clear();
        }
    }

    /// <summary>
    /// This class represents a scanned object (a file or a directory).
    /// </summary>
    public abstract class KfsScanObject
    {
        protected String m_name;

        /// <summary>
        /// Name of the entry in the directory.
        /// </summary>
        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public KfsScanObject(String _name)
        {
            m_name = _name;
        }

        public override string ToString()
        {
            return m_name;
        }
    }

    public class KfsScanFile : KfsScanObject
    {
        public KfsScanFile(String _name) : base(_name) { }
    }

    public class KfsScanDirectory : KfsScanObject
    {
        public SortedDictionary<String, KfsScanObject> ChildTree =
                new SortedDictionary<String, KfsScanObject>();

        public KfsScanDirectory(String _name)
            : base(_name)
        {
        }

        public void Add(KfsScanObject _object)
        {
            Debug.Assert(!Contains(_object.Name));
            ChildTree.Add(_object.Name, _object);
        }

        public bool Contains(string _name)
        {
            return ChildTree.ContainsKey(_name);
        }
    }

    /// <summary>
    /// This class represents a scanned subtree that must be synchronized
    /// with the local view.
    /// </summary>
    public class KfsScan
    {
        private KfsShare m_share;

        /// <summary>
        /// Path of the updated subtree from the KFS share logical root.
        /// This is "" if the subtree is the whole share. This path does
        /// not begin nor end with a delimiter.
        /// </summary>
        private String m_subtreePath;

        /// <summary>
        /// Updated subtree object. If SubtreePath is "", then this object
        /// must correspond to the root directory. If the entry referred to
        /// by SubtreePath does not exist, this object is null. Otherwise, this
        /// object corresponds to the entry referred to by SubtreePath. Note
        /// that the algorithm assumes that the directories leading to
        /// SubtreePath actually exist even if SubtreeObject is null.
        /// </summary>
        private KfsScanObject m_subtreeObject;

        public KfsScan(KfsShare _share, String _path)
        {
            m_share = _share;
            m_subtreePath = _path;
        }

        /// <summary>
        /// Scan the content of SubtreePath. IMPORTANT: if SubtreePath does not
        /// exist on disk, then this method must update SubtreePath to the first 
        /// subtree that actually exist.
        /// </summary>
        public void Scan()
        {
            String fullPath = m_share.MakeAbsolute(m_subtreePath);
            Logging.Log("KfsScan() called (" + m_subtreePath + ")");

            // The target path is a file.
            if (File.Exists(fullPath) && KfsPath.BaseName(fullPath) != KfsShare.FsMarker)
            {
                Logging.Log("Found an existing file on disk.");
                m_subtreeObject = new KfsScanFile(KfsPath.BaseName(m_subtreePath));
                return;
            }

            // The target path is a directory. Scan it recursively.
            if (Directory.Exists(fullPath))
            {
                Logging.Log("Found a directory on disk");
                m_subtreeObject = new KfsScanDirectory(KfsPath.BaseName(m_subtreePath));
                ScanRecursive(m_subtreeObject as KfsScanDirectory, m_subtreePath);
                return;
            }

            // The target path does not exist. Find the first object that does exist.
            // This is tricky...
            while (true)
            {
                // Get the parent, if there is one. The parent of the root is
                // the root itself in this context.
                String parentRelPath = KfsPath.StripTrailingDelim(KfsPath.DirName(m_subtreePath));
                String parentFullPath = m_share.MakeAbsolute(parentRelPath);

                // Update the target path to match the file, unless the parent path is
                // the root itself (the root must not be a file).
                if (parentRelPath != "" && File.Exists(parentFullPath))
                {
                    m_subtreePath = parentRelPath;
                    m_subtreeObject = new KfsScanFile(KfsPath.BaseName(m_subtreePath));
                    return;
                }

                // Remember that the target path does not exist, unless the target path
                // is the root itself (someone might have deleted then created the root
                // quickly, there is a race condition here).
                if (m_subtreePath != "" && Directory.Exists(parentFullPath))
                {
                    m_subtreeObject = null;
                    return;
                }

                // This can occur if the root directory does not exist. Create it
                // again in this case.
                if (parentRelPath == "")
                {
                    if (!Directory.Exists(parentFullPath)) Directory.CreateDirectory(parentFullPath);
                    m_subtreePath = "";
                    m_subtreeObject = new KfsScanDirectory("");
                    return;
                }

                // Consider the parent directory.
                m_subtreePath = parentRelPath;
            }
        }

        /// <summary>
        /// Helper method for Scan().
        /// </summary>
        private void ScanRecursive(KfsScanDirectory subSubtree, String _path)
        {
            string fullPath = m_share.MakeAbsolute(_path);

            // Add all the files
            foreach (FileInfo f in new DirectoryInfo(fullPath).GetFiles())
            {
                subSubtree.Add(new KfsScanFile(KfsPath.BaseName(f.Name)));
            }

            // Add all the subdirectories.
            foreach (DirectoryInfo d in new DirectoryInfo(fullPath).GetDirectories())
            {
                KfsScanDirectory dir = new KfsScanDirectory(d.Name);
                subSubtree.Add(dir);
                ScanRecursive(dir, _path + "/" + d.Name);
            }
        }

        /// <summary>
        /// Synchronize the scanned paths with the local view.
        /// </summary>
        public void Sync()
        {
            if (m_subtreePath == "")
            {
                Debug.Assert(m_subtreeObject != null);
                Debug.Assert(m_subtreeObject is KfsScanDirectory);
                SyncRecursive(null, m_subtreeObject as KfsScanDirectory);
            }

            else
            {
                String[] pathComponents = KfsPath.SplitRelativePath(m_subtreePath);
                KfsLocalDirectory cur = m_share.LocalView.Root;

                // Iterate on all components except the last one.
                for (int i = 0; i < pathComponents.Length - 1; i++)
                {
                    String s = pathComponents[i];

                    // Create the dir 's' if it does not exist
                    // in the local view.
                    if (!cur.ContainsDirectory(s))
                    {
                        if (cur.Contains(s))
                            cur.ChildTree[s].RemoveFromView();

                        new KfsLocalDirectory(m_share, cur, s);
                    }

                    cur = cur.GetDirectory(s);
                }

                // Remove this non-existant local object from the view if
                // it exists.
                if (m_subtreeObject == null)
                {
                    if (cur.Contains(pathComponents[pathComponents.Length - 1]))
                        cur.GetObject(pathComponents[pathComponents.Length - 1]).RemoveFromView();
                }

                // Synchronize the object with the object in the view.
                else
                {
                    Debug.Assert(m_subtreeObject.Name == pathComponents[pathComponents.Length - 1]);
                    SyncRecursive(cur, m_subtreeObject);
                }
            }
        }

        /// <summary>
        /// Helper method for Sync().
        /// </summary>
        private void SyncRecursive(KfsLocalDirectory dir, KfsScanObject obj)
        {
            // We're synchronizing a directory.
            if (obj is KfsScanDirectory)
            {
                KfsScanDirectory objAsDir = (KfsScanDirectory)obj;
                KfsLocalDirectory subDir = null;

                // We're synchronizing the root directory.
                if (dir == null)
                {
                    subDir = m_share.LocalView.Root;
                }

                else
                {
                    // Make sure the subdirectory exists.
                    if (!dir.ContainsDirectory(obj.Name))
                    {
                        if (dir.Contains(obj.Name))
                            dir.GetObject(obj.Name).RemoveFromView();

                        new KfsLocalDirectory(m_share, dir, obj.Name);
                    }

                    subDir = dir.GetDirectory(obj.Name);
                }

                // Remove the stale children.
                SortedDictionary<String, KfsLocalObject> subDirChildTree =
                    new SortedDictionary<String, KfsLocalObject>(subDir.ChildTree);
                foreach (KfsLocalObject o in subDirChildTree.Values)
                {
                    if (!objAsDir.Contains(o.Name))
                    {
                        o.RemoveFromView();
                    }
                }

                // Synchronize the children.
                foreach (KfsScanObject o in objAsDir.ChildTree.Values)
                {
                    SyncRecursive(subDir, o);
                }
            }

            // We're synchronizing a file.
            else
            {
                if (!dir.ContainsFile(obj.Name))
                {
                    if (dir.Contains(obj.Name))
                        dir.GetObject(obj.Name).RemoveFromView();

                    new KfsLocalFile(m_share, dir, obj.Name);
                }

                // Request the local status of the file to be updated.
                KfsServerFile serverFile = dir.GetFile(obj.Name).GetServerCounterpart();
                if (serverFile != null) serverFile.RequestUpdate();
            }
        }
    }

    /// <summary>
    /// This class contains the FileSystemWatcher instance and the methods to
    /// call when a filesystem event is received.
    /// </summary>
    public class KfsFsWatcher
    {
        /// <summary>
        /// The current instance of FileSystemWatcher, if any.
        /// </summary>
        private FileSystemWatcher Instance;

        /// <summary>
        /// Reference to the share. This reference is set to null when we no
        /// longer want to receive events from this watcher.
        /// </summary>
        private KfsShare Share;

        public KfsFsWatcher(KfsShare share)
        {
            Share = share;
            Instance = new FileSystemWatcher(Share.ShareFullPath);

            // Invoke event handlers in the UI thread
            Control c = (Control)Misc.MainForm;
            Instance.SynchronizingObject = c;
            
            // We must be notified of directory and file creation / renaming / file size change / file lastWrite change.
            Instance.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite;

            // Watch recursively
            Instance.IncludeSubdirectories = true;

            Instance.Changed += HandleOnWatcherChanged;
            Instance.Created += HandleOnWatcherChanged;
            Instance.Deleted += HandleOnWatcherChanged;
            Instance.Renamed += HandleOnWatcherChanged;
            Instance.Error += HandleOnWatcherError;
            Instance.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Called when a file/directory has been modified.
        /// </summary>
        private void HandleOnWatcherChanged(Object sender, FileSystemEventArgs e)
        {
            try
            {
                if (Share != null)
                    Share.OnWatcherChanged(sender, e);
            }

            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
        }

        /// <summary>
        /// Called when the watcher has failed.
        /// </summary>
        private void HandleOnWatcherError(object sender, ErrorEventArgs e)
        {
            try
            {
                if (Share != null)
                    Share.OnWatcherError(sender, e);
            }

            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
        }

        /// <summary>
        /// Request the watcher to stop watching.
        /// </summary>
        public void StopWatching()
        {
            Share = null;
            Instance.EnableRaisingEvents = false;
        }
    }
}