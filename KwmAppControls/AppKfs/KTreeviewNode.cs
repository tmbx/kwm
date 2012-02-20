using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using System.IO;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// Custom treenode that contains a list of all the KFS
    /// elements (directories and files) under that node.
    /// </summary>
    public class KTreeNode : TreeNode
    {
        private ImageListManager m_lvImageListMgr;

        private SortedDictionary<string, KListViewItem> m_childs;

        private String m_name;
        private PathStatus m_status;

        private bool m_onServer;

        private IAppHelper m_helper;

        /// <summary>
        /// Contains the full relative path of this node, not slash-terminated.
        /// Use this method instead of the FullPath base property since we 
        /// must remove the 'Share' text of the root node.
        /// </summary>
        public string KFullPath
        {
            get
            {
                string[] pathComponents = KfsPath.SplitRelativePath(FullPath);
                string retvalue = "";

                // Skip the first component that is the root's display text ("Share")
                for (int i = 1; i < pathComponents.Length; i++)
                {
                    if (retvalue == "")
                        retvalue += pathComponents[i];
                    else
                        retvalue += this.TreeView.PathSeparator + pathComponents[i];
                }
                return retvalue;
            }
        }

        public string FileName
        {
            get
            {
                return m_name;
            }
        }

        public PathStatus Status
        {
            get
            {
                return m_status;
            }
        }

        /// <summary>
        /// Contains the files present in this directory
        /// </summary>
        public SortedDictionary<string, KListViewItem> Childs
        {
            get
            {
                return m_childs;
            }
        }
        /// <summary>
        /// Is this folder present server-side or locally?
        /// </summary>
        public bool OnServer
        {
            get
            {
                return m_onServer;
            }
        }

        public KTreeNode(KfsStatusPath obj, ImageListManager mgr, IAppHelper _helper) : base()
        {
            Debug.Assert(obj.IsDir());
            m_helper = _helper;

            m_childs = new SortedDictionary<string, KListViewItem>();
            m_onServer = obj.OnServer();
            
            m_lvImageListMgr = mgr;

            this.Text = obj.IsRoot() ? "Share" : obj.Name;
            this.ImageKey = "FolderClosed";
            this.SelectedImageKey = "FolderOpened";
            m_status = obj.Status;
            m_name = obj.Name;

            CreateChilds(obj);
        }

        /// <summary>
        /// Populates the objects contained in this directory.
        /// </summary>
        /// <param name="obj"></param>
        private void CreateChilds(KfsStatusPath obj)
        {
            foreach (KeyValuePair<string, KfsStatusPath> content in obj.ChildTree)
            {
                KfsStatusPath val = content.Value as KfsStatusPath;
                Debug.Assert(val != null);

                KListViewItem newItem = new KListViewItem(val, m_helper);
                if (val.Status == PathStatus.Directory)
                    newItem.ImageKey = "FolderClosed";
                else if (val.HasLocalFile() && NeedsIconCheck(newItem.FileName))
                    newItem.ImageKey = m_lvImageListMgr.GetImageKey(val.Share.MakeAbsolute(val.Path));
                else
                    newItem.ImageKey = m_lvImageListMgr.GetImageKey(Path.GetExtension(newItem.FileName));

                newItem.Name = val.Name;

                m_childs.Add(val.Name, newItem);
            }
        }

        /// <summary>
        /// Return true if the file extension is usually subject to change.
        /// Totally arbitrary list.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool NeedsIconCheck(String filename)
        {
            return (Path.GetExtension(filename) == ".exe");
        }
    }
}
