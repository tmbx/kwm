using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// Represent an object on the user's workstation.
    /// </summary>
    [Serializable]
    public class KfsLocalObject
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Parent directory. This is null for the root.
        /// </summary>
        public KfsLocalDirectory Parent;

        /// <summary>
        /// Name of the object in the parent directory. This is "" for the root.
        /// </summary>
        public String Name;

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
                KfsLocalDirectory cur = Parent;
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
                return Share.ShareFullPath + RelativePath;
            }
        }

        /// <summary>
        /// This constructor creates the object and inserts it in the view.
        /// </summary>
        /// <param name="S">Share</param>
        /// <param name="P">Parent directory</param>
        /// <param name="N">Name</param>
        public KfsLocalObject(KfsShare S, KfsLocalDirectory P, String N)
        {
            Share = S;
            Parent = P;
            Name = N;
            AddToView();
        }

        /// <summary>
        /// Add the object at the appropriate location in the local view.
        /// </summary>
        public void AddToView()
        {
            if (IsRoot())
            {
                Debug.Assert(Name == "");
                Debug.Assert(this is KfsLocalDirectory);
                Debug.Assert(Share.LocalView.Root == null);
                Share.LocalView.Root = this as KfsLocalDirectory;
            }

            else
            {
                Debug.Assert(Name != "");
                Debug.Assert(!Parent.ChildTree.ContainsKey(Name));
                Parent.ChildTree[Name] = this;
            }
        }

        /// <summary>
        /// Remove the object from the local view.
        /// </summary>
        public void RemoveFromView()
        {
            if (IsRoot())
            {
                Debug.Assert(this is KfsLocalDirectory);
                Share.LocalView.Root = null;
            }

            else
            {
                Debug.Assert(Parent.ChildTree[Name] == this);
                Parent.ChildTree.Remove(Name);
            }
        }

        /// <summary>
        /// Return true if this object is the root directory.
        /// </summary>
        public bool IsRoot()
        {
            return (Parent == null);
        }
    }

    /// <summary>
    /// Represent a directory on the user's workstation.
    /// </summary>
    [Serializable]
    public class KfsLocalDirectory : KfsLocalObject
    {
        /// <summary>
        /// Tree that maps directory entry names to KfsLocalObject.
        /// </summary>
        public SortedDictionary<String, KfsLocalObject> ChildTree = new SortedDictionary<string, KfsLocalObject>();

        /// <summary>
        /// True if the directory is expanded.
        /// </summary>
        public bool ExpandedFlag = false;

        public KfsLocalDirectory(KfsShare S, KfsLocalDirectory P, String N)
            : base(S, P, N)
        {
        }

        /// <summary>
        /// Returns true if a directory exists under that name
        /// in this directory.
        /// </summary>
        public bool ContainsDirectory(string _name)
        {
            return (ChildTree.ContainsKey(_name) &&
                ChildTree[_name] is KfsLocalDirectory);
        }

        /// <summary>
        /// Returns true if a file exists under that name
        /// in this directory.
        /// </summary>
        public bool ContainsFile(string _name)
        {
            return (ChildTree.ContainsKey(_name) &&
                ChildTree[_name] is KfsLocalFile);
        }

        /// <summary>
        /// Returns true if anything under that name exists 
        /// in this directory.
        /// </summary>
        public bool Contains(string _name)
        {
            return ChildTree.ContainsKey(_name);
        }

        public KfsLocalDirectory GetDirectory(string _name)
        {
            Debug.Assert(ChildTree.ContainsKey(_name));
            Debug.Assert(ChildTree[_name] is KfsLocalDirectory);
            return ChildTree[_name] as KfsLocalDirectory;
        }

        public KfsLocalFile GetFile(string _name)
        {
            Debug.Assert(ChildTree.ContainsKey(_name));
            Debug.Assert(ChildTree[_name] is KfsLocalFile);
            return ChildTree[_name] as KfsLocalFile;
        }

        /// <summary>
        /// Get a given local object in this directory
        /// </summary>
        public KfsLocalObject GetObject(string _name)
        {
            Debug.Assert(ChildTree.ContainsKey(_name));
            return ChildTree[_name];
        }
    }

    /// <summary>
    /// Represent a file on the user's workstation.
    /// </summary>
    [Serializable]
    public class KfsLocalFile : KfsLocalObject
    {
        public KfsLocalFile(KfsShare S, KfsLocalDirectory P, String N)
            : base(S, P, N)
        {
        }

        /// <summary>
        /// Return the server file corresponding to the local file, if it
        /// exists and it has a current version.
        /// </summary>
        public KfsServerFile GetServerCounterpart()
        {
            KfsServerFile counterpart = Share.ServerView.GetObjectByPath(RelativePath) as KfsServerFile;
            if (counterpart == null || counterpart.CurrentVersion == null) return null;
            return counterpart;
        }
    }

    /// <summary>
    /// Represent the content of the share on the user's workstation.
    /// </summary>
    [Serializable]
    public class KfsLocalView
    {
        /// <summary>
        /// Reference to the KFS share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Reference to the local root directory.
        /// </summary>
        public KfsLocalDirectory Root;

        public KfsLocalView(KfsShare S)
        {
            Share = S;
            Share.LocalView = this;

            // Create the root directory.
            new KfsLocalDirectory(Share, null, "");
        }

        /// <summary>
        /// Return the object having the path specified, if any.
        /// </summary>
        public KfsLocalObject GetObjectByPath(String path)
        {
            String[] components = KfsPath.SplitRelativePath(path);
            KfsLocalDirectory cur = Root;

            for (int i = 0; i < components.Length; i++)
            {
                String c = components[i];
                if (!cur.ChildTree.ContainsKey(c)) return null;
                KfsLocalObject o = cur.ChildTree[c];
                if (i == components.Length - 1) return o;
                cur = o as KfsLocalDirectory;
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

        /// <summary>
        /// Create and delete the filesystem marker file.
        /// </summary>
        public void WriteFsMarker()
        {
            FileStream s = null;

            try
            {
                String path = Share.FsMarkerFilePath;

                // Create the marker file if it does not exist.
                if (!File.Exists(path))
                {
                    s = new FileStream(path, FileMode.Create, FileAccess.Write);
                    s.Close();
                    s = null;
                }

                // Delete the marker.
                File.Delete(path);
            }

            finally
            {
                if (s != null) s.Close();
            }
        }

        /// <summary>
        /// Helper method for GetPathArray().
        /// </summary>
        private void GetPathArrayRecursive(List<String> a, KfsLocalDirectory c, bool lf)
        {
            if (!lf) a.Add(c.RelativePath);
            foreach (KfsLocalObject o in c.ChildTree.Values)
            {
                if (o is KfsLocalDirectory)
                {
                    GetPathArrayRecursive(a, o as KfsLocalDirectory, lf);
                }
                else
                {
                    a.Add(o.RelativePath);
                }
            }
            if (lf) a.Add(c.RelativePath);
        }
    }
}