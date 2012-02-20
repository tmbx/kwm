using System;
using System.Drawing;
using Tbx.Utils;
using kwm.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// This wraps a CustomListViewItem with some CustomSubItems
    /// to be easily usable in our Kfs application
    /// </summary>
    public class KListViewItem : CustomListViewItem
    {
        private const string SizeKey = "Size";
        private const string TransferStatusKey = "TransferStatus";
        private const string ModifiedDateKey = "ModifiedDate";
        private const string ModifiedByKey = "ModifiedBy";
        private const string StatusKey = "Status";
        private const string CommitIDKey = "CommitID";

        private string m_fileName;
        private DateTime m_modifiedDate;
        private string m_modifiedBy;
        private UInt64 m_size;
        private PathStatus m_status;
        private bool m_onServer;
        private string m_path;
        private bool m_hasCurrentVersion = false;

        private bool m_isFile = false;
        private bool m_isDirectory = false;

        private IAppHelper m_helper; 

        /// <summary>
        /// Name of this item in its parent directory.
        /// </summary>
        public String FileName
        {
            get { return m_fileName; }
        }

        /// <summary>
        /// When was this file last modified.
        /// </summary>
        public DateTime ModifiedDate
        {
            get { return m_modifiedDate; }
        }

        /// <summary>
        /// Who last modified this file.
        /// </summary>
        public String ModifiedBy
        {
            get { return m_modifiedBy; }
        }

        /// <summary>
        /// If this item is a directory, does it exist remotely?
        /// </summary>
        public bool OnServer
        {
            get { return m_onServer; }
        }

        /// <summary>
        /// Status of this item.
        /// </summary>
        public PathStatus Status
        {
            get { return m_status; }
        }

        /// <summary>
        /// Relative path of this item from the root.
        /// </summary>
        public String Path
        {
            get { return m_path; }
        }

        public UInt64 Size
        {
            get { return m_size; }
        }

        public bool HasCurrentVersion
        {
            get { return m_hasCurrentVersion; }
        }

        public bool IsFile
        {
            get { return m_isFile; }
        }

        public bool IsDirectory
        {
            get { return m_isDirectory; }
        }
        /* Do not allow anyone to create such an item without arguments */
        private KListViewItem()
        {
        }

        public KListViewItem(KfsStatusPath obj, IAppHelper _helper)
        {

            /* Create all subitems, without any text in them */
            CustomListViewSubItem subItem;
            m_helper = _helper;

            subItem = new CustomListViewSubItem();
            subItem.Name = SizeKey;
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = TransferStatusKey;
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = ModifiedDateKey;
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = ModifiedByKey;
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = StatusKey;
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = CommitIDKey;
            AddCustomSubItem(subItem);

            UpdateInfos(obj);
        }

        public void UpdateInfos(KfsStatusPath obj)
        {
            m_fileName = obj.Name;

            if ((obj.IsDir() && obj.HasServerDir()) ||
                (obj.IsFile() && obj.HasServerFile() && !((KfsServerFile)obj.ServerObject).HasCurrentVersion()))
            {
                // Get the creator if obj is a directory on the server or if its a new file
                // that has no current version yet.
                m_modifiedBy = m_helper.GetUserDisplayName((UInt32)obj.ServerObject.CreationUserID);
            }
            else if(obj.IsFile() && obj.HasServerFile() && 
                ((KfsServerFile)obj.ServerObject).HasCurrentVersion() &&
                (obj.Status !=  PathStatus.ModifiedCurrent && obj.Status != PathStatus.ModifiedStale))
            {
                // We have a file and its current version, and it is NOT modified locally.
                m_modifiedBy = m_helper.GetUserDisplayName(((KfsServerFile)obj.ServerObject).CurrentVersion.UserID);
            }
            else
            {
                m_modifiedBy = m_helper.GetUserDisplayName(m_helper.GetKwmUserID());
            }                

            m_modifiedDate = obj.LastModifiedDate;

            m_path = obj.Path;
            m_status = obj.Status;
            m_onServer = obj.OnServer();
            
            m_size = obj.Size;

            m_isFile = obj.IsFile();
            m_isDirectory = obj.IsDir();

            this.Text = m_fileName;
            this.Name = m_fileName;
            
            // FIXME : need a way to distinguish between an upload and a download.
            // Always set the icon, play with ShowIcon to display it or not.
            // This crashes the kwm with a Corrupted memory exception. See if the ressource is ok.
            /*((CustomListViewSubItem)SubItems[TransferStatusKey]).icon = KwmAppControls.Properties.Resources.download;
            if (obj.Share.IsTransferringFile(m_path))
                ((CustomListViewSubItem)SubItems[TransferStatusKey]).showIcon = true;
            else
                ((CustomListViewSubItem)SubItems[TransferStatusKey]).showIcon = true;
             * */

            SubItems[ModifiedDateKey].Text = m_modifiedDate.ToString();
            SubItems[ModifiedByKey].Text = m_modifiedBy;

            if (m_size != UInt64.MaxValue)
                SubItems[SizeKey].Text = Base.GetHumanFileSize(m_size);
            else if (m_status == PathStatus.Directory)
                SubItems[SizeKey].Text = "";
            else
                SubItems[SizeKey].Text = "In progress...";

            if (m_status == PathStatus.Directory)
            {
                if (obj.OnServer())
                    SubItems[StatusKey].Text = "";
                else
                    SubItems[StatusKey].Text = "Not Added";
            }
            else
            {
                SubItems[StatusKey].Text = Base.GetEnumDescription(m_status);
                m_hasCurrentVersion = (obj.HasServerFile() && ((KfsServerFile)obj.ServerObject).HasCurrentVersion());
            }

            Font strikeout = new Font(this.Font, FontStyle.Strikeout);
            Font standard = new Font(this.Font, FontStyle.Regular);
            Font italic = new Font(this.Font, FontStyle.Italic);

            // Set default color and style
            this.Font = standard;
            this.ForeColor = Color.Black;

            // Modify color / style for specific statuses.
            switch (m_status)
            {
                case PathStatus.NotAdded:
                    this.Font = italic;
                    break;
                case PathStatus.Directory:
                    if (!m_onServer)
                        this.Font = italic;
                    break;                    
                case PathStatus.DirFileConflict:
                case PathStatus.FileDirConflict:
                case PathStatus.ModifiedStale:
                    this.ForeColor = Color.Red;
                    break;
                case PathStatus.NotDownloaded:
                    this.ForeColor = Color.DarkGray;
                    break;
            }
        }
    }
}
