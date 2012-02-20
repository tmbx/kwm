using System;
using System.Collections.Generic;
using System.Text;

using kwm.Utils;
using kwm.KwmAppControls.AppKfs;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    /// <summary>
    /// This class wraps the actual AppKfsNotificationItem since more than
    /// one type of notification could occur in a single Phase1 message.
    /// </summary>
    public class KfsNotification
    {
        /// <summary>
        /// List of NotificationItems that must be displayed to the user.
        /// </summary>
        public List<AppKfsNotificationItem> Items = new List<AppKfsNotificationItem>();

        /// <summary>
        /// List of server ops received from the server.
        /// </summary>
        private List<KfsServerOp> m_incomingServerOps;

        List<KfsServerOp> m_createOps = new List<KfsServerOp>();
        List<KfsServerOp> m_deleteOps = new List<KfsServerOp>();
        List<KfsServerOp> m_moveOps = new List<KfsServerOp>();
        List<KfsServerOp> m_updateOps = new List<KfsServerOp>();
        List<KfsServerOp> m_phase2Ops = new List<KfsServerOp>();

        public KfsNotification(IAppHelper _helper, List<KfsServerOp> _operations, SortedDictionary<UInt64, List<KfsServerOp>> seenPhase1Ops)
        {
            m_incomingServerOps = _operations;

            SeparateServerOpByType(seenPhase1Ops);

            if (m_createOps.Count > 0)
                Items.Add(new AppKfsCreateItem(_helper, m_createOps));

            if (m_deleteOps.Count > 0)
                Items.Add(new AppKfsDeleteItem(_helper, m_deleteOps));

            if (m_moveOps.Count > 0)
                Items.Add(new AppKfsMoveItem(_helper, m_moveOps));

            if (m_updateOps.Count > 0)
                Items.Add(new AppKfsUpdateItem(_helper, m_updateOps));

            if (m_phase2Ops.Count > 0)
                Items.Add(new AppKfsPhase2Item(_helper, m_phase2Ops, seenPhase1Ops));
        }

        /// <summary>
        /// Split a list of server operations into four lists, one for each 
        /// type of operation. Excludes non-notifiable operations from the lists.
        /// </summary>
        private void SeparateServerOpByType(SortedDictionary<UInt64, List<KfsServerOp>> seenPhase1Ops)
        {
            foreach (KfsServerOp op in m_incomingServerOps)
            {
                if (op is KfsCreateServerOp)
                    m_createOps.Add(op as KfsCreateServerOp);

                else if (op is KfsDeleteServerOp)
                    m_deleteOps.Add(op as KfsDeleteServerOp);

                else if (op is KfsMoveServerOp)
                    m_moveOps.Add(op as KfsMoveServerOp);

                else if (op is KfsUpdateServerOp)
                    m_updateOps.Add(op as KfsUpdateServerOp);

                else if (op is KfsPhase2ServerOp)
                {
                    KfsPhase2ServerOp o = op as KfsPhase2ServerOp;

                    // Notify only if at least one file has been successfully uploaded,
                    // and if we already see the corresponding phase1 operation. This is necessary
                    // since we do not want to serialize all the phase1 operations: we just do not 
                    // notify for a phase2 when a KWM restart occurs between the phase1 and the phase2 
                    // event.
                    if (o.UploadArray.Count > 0 && seenPhase1Ops.ContainsKey(o.CommitID))
                        m_phase2Ops.Add(op as KfsPhase2ServerOp);
                }

                else
                    Debug.Assert(false, "Invalid server operation (" + op.GetType() + ")");
            }
        }
    }

    public class AppKfsCreateItem : AppKfsNotificationItem
    {
        private KfsCreateServerOp FirstOp;

        public AppKfsCreateItem(IAppHelper _helper, List<KfsServerOp> _operations)
            : base(_helper, _operations) 
        {
            Debug.Assert(m_ops.Count > 0);

            FirstOp = m_ops[0] as KfsCreateServerOp;

            Debug.Assert(FirstOp != null);
        }

        /// <summary>
        /// Return a human-readable string representing a series of create
        /// operations. Since no usability scenario allows it, we assume that 
        /// all the operations were done by the same user.
        /// </summary>
        protected override string GetMessage()
        {
            Debug.Assert(m_ops.Count > 0);
            
            // We assume the entire ops set comes from the same user.
            String who = m_helper.GetUserDisplayName(FirstOp.UserID);

            if (m_ops.Count == 1)
            {
                if (FirstOp.IsFile)
                    return who + " created the file " + FormatName(FirstOp.Name) + ".";
                else
                    return who + " created the directory " + FormatName(FirstOp.Name) + ".";
            }

            int nbFiles = 0;
            int nbDirs = 0;

            foreach(KfsCreateServerOp op in m_ops)
            {
                if (op.IsFile)
                    nbFiles++;
                else 
                    nbDirs++;
            }

            // Only new files.
            if (nbFiles > 0 && nbDirs == 0)
                return who + " added " + nbFiles + " new files.";

            // Only new dirs.
            else if (nbDirs > 0 && nbFiles == 0)
                return who + " created " + nbDirs + " new directories.";

            // New files and new dirs.
            else
                return who + " added " + nbFiles + " new files and created " + nbDirs + " new directories.";
        }
    }

    public class AppKfsDeleteItem : AppKfsNotificationItem
    {
        private KfsDeleteServerOp FirstOp;

        public AppKfsDeleteItem(IAppHelper _helper, List<KfsServerOp> _operations)
            : base(_helper, _operations)
        {
            Debug.Assert(m_ops.Count > 0);

            FirstOp = m_ops[0] as KfsDeleteServerOp;

            Debug.Assert(FirstOp != null);
        }

        protected override String GetMessage()
        {
            Debug.Assert(m_ops.Count > 0);

            // We assume the entire ops set comes from the same user.
            String who = m_helper.GetUserDisplayName(FirstOp.UserID);

            if (m_ops.Count == 1)
                return who + " deleted " + FormatName(FirstOp.Name) + ".";

            int nbFiles = 0;
            int nbDirs = 0;

            foreach (KfsDeleteServerOp op in m_ops)
            {
                if (op.IsFile)
                    nbFiles++;
                else
                    nbDirs++;
            }

            // Only files.
            if (nbFiles > 0 && nbDirs == 0)
                return who + " deleted " + nbFiles + " files.";

            // Only dirs.
            else if (nbDirs > 0 && nbFiles == 0)
                return who + " deleted " + nbDirs + " directories.";

            // Files and dirs.
            else
                return who + " deleted " + nbFiles + " files and " + nbDirs + " directories.";
        }
    }

    public class AppKfsMoveItem : AppKfsNotificationItem
    {
        private KfsMoveServerOp FirstOp;

        public AppKfsMoveItem(IAppHelper _helper, List<KfsServerOp> _operations)
            : base(_helper, _operations)
        {
            Debug.Assert(m_ops.Count > 0);

            FirstOp = m_ops[0] as KfsMoveServerOp;

            Debug.Assert(FirstOp != null);
        }

        protected override string GetMessage()
        {
         	Debug.Assert(m_ops.Count > 0);

            KfsShare Share = FirstOp.Share;

            // We assume the entire ops set comes from the same user.
            String who = m_helper.GetUserDisplayName(FirstOp.UserID);

            if (m_ops.Count == 1)
            {
                // A real move from one dir to another. Tell that foo.txt was moved to /foo/bar/...
                if (FirstOp.DestDirPath.ToLower() != FirstOp.SrcDirPath.ToLower())
                {
                    return who + " moved " + FormatName(FirstOp.DestName) + " to " + FormatName(FirstOp.DestDirPath) + ".";
                }

                // Single rename.
                else
                {
                    return who + " renamed " + FormatName(FirstOp.SrcName) + " to " + FormatName(FirstOp.DestName) + ".";
                }
            }

            int nbFiles = 0;
            int nbDirs = 0;

            // Track if the destination of all the ops are the same. If so, 
            // we can give a specific message to the user, otherwise stay generic.
            bool diffDest = false;
            UInt64 initialDestInode = FirstOp.DestinationDirInode;
            foreach (KfsMoveServerOp op in m_ops)
            {
                // As soon as we get a DestinationInode different
                // from the first op, set diffDest.
                if (initialDestInode != op.DestinationDirInode)
                    diffDest = true;

                if (op.IsFile)
                    nbFiles++;
                else
                    nbDirs++;
            }

            String msg;

            // Only new files.
            if (nbFiles > 0 && nbDirs == 0)
                msg = who + " moved " + nbFiles + " files";

            // Only new dirs.
            else if (nbDirs > 0 && nbFiles == 0)
                msg = who + " moved " + nbDirs + " directories";

            // New files and new dirs.
            else
                msg = who + " moved " + nbFiles + " files and " + nbDirs + " directories";

            if (diffDest)
            {
                return msg + ".";
            }

            else
            {
                KfsServerObject o = FirstOp.Share.ServerView.GetObjectByInode(initialDestInode);
                Debug.Assert(o is KfsServerDirectory);
                return msg + " to " + FormatName(o.RelativePath);
            }
        }
    }

    public class AppKfsUpdateItem : AppKfsNotificationItem
    {
        private KfsUpdateServerOp FirstOp;

        public AppKfsUpdateItem(IAppHelper _helper, List<KfsServerOp> _operations)
            : base(_helper, _operations) 
        {
            Debug.Assert(m_ops.Count > 0);

            FirstOp = m_ops[0] as KfsUpdateServerOp;

            Debug.Assert(FirstOp != null);
        }

        /// <summary>
        /// Return a human-readable string representing a series of create
        /// operations. Since no usability scenario allows it, we assume that 
        /// all the operations were done by the same user.
        /// </summary>
        protected override string GetMessage()
        {
            Debug.Assert(m_ops.Count > 0);

            // We assume the entire ops set comes from the same user.
            String who = m_helper.GetUserDisplayName(FirstOp.UserID);

            if (m_ops.Count == 1)
                return who + " updated " + FormatName(FirstOp.Name);

           return who + " updated " + m_ops.Count + " files.";
        }
    }

    public class AppKfsPhase2Item : AppKfsNotificationItem
    {
        private KfsPhase2ServerOp FirstOp;
        private List<KfsServerOp> m_seenPhase1Ops;

        public AppKfsPhase2Item(IAppHelper _helper, List<KfsServerOp> _operations, SortedDictionary<UInt64, List<KfsServerOp>> seenPhase1Ops)
            : base(_helper, _operations)
        {
            Debug.Assert(m_ops.Count > 0);
            
            FirstOp = m_ops[0] as KfsPhase2ServerOp;
            Debug.Assert(seenPhase1Ops.ContainsKey(FirstOp.CommitID));
            // FIXME seenPhase1Ops might be empty if the KWM is closed between the phase1 and the phase2 event.
            m_seenPhase1Ops = seenPhase1Ops[FirstOp.CommitID];
            seenPhase1Ops.Remove(FirstOp.CommitID);

            Debug.Assert(FirstOp != null);
        }

        /// <summary>
        /// Return a human-readable string representing a phase2 event.
        /// Since no usability scenario allows it, we assume that 
        /// all the operations were done by the same user.
        /// </summary>
        protected override string GetMessage()
        {
            Debug.Assert(FirstOp.UploadArray.Count > 0);

            // We assume the entire ops set comes from the same user.
            String who = m_helper.GetUserDisplayName(FirstOp.UserID);
            
            String uploadedFileName = "";

            // Look for the server op that contains the inode of the 
            // first uploaded file as reported in the phase2 event.
            foreach (KfsServerOp o in m_seenPhase1Ops)
            {
                if (o is KfsCreateServerOp)
                {
                    KfsCreateServerOp op = o as KfsCreateServerOp;
                    if (op.CreatedInode == FirstOp.UploadArray[0].Inode)
                    {
                        uploadedFileName = ((KfsCreateServerOp)o).Name;
                        break;
                    }
                }

                else
                {
                    KfsUpdateServerOp op = o as KfsUpdateServerOp;
                    if (op.Inode == FirstOp.UploadArray[0].Inode)
                    {
                        uploadedFileName = ((KfsUpdateServerOp)o).Name;
                        break;
                    }
                }
            }

            Debug.Assert(uploadedFileName != "");

            String msg = who + " finished uploading " + uploadedFileName;

            if (FirstOp.UploadArray.Count > 1)
                msg += " and " + (FirstOp.UploadArray.Count - 1) + " other files.";
            else
                msg += ".";

            return msg;
        }

    }

    public abstract class AppKfsNotificationItem : NotificationItem
    {
        private const int TIME_BETWEEN_POPUP = 5;

        protected List<KfsServerOp> m_ops;

        public override String EventText
        {
            get { return GetMessage(); }
        }
        
        public AppKfsNotificationItem(IAppHelper _helper, List<KfsServerOp> _operations)
            : base(null, KAnpType.KANP_NS_KFS, _helper)
        {
            m_ops = _operations;
        }

        /// <summary>
        /// Format a human-readable message based on a list of server ops.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetMessage();

        public override String GetSimplifiedFormattedDetail()
        {
            return EventText;
        }

        public override String GetFullFormattedDetail()
        {
            return GetLongFormattedDate + GetSimplifiedFormattedDetail();
        }
        public override string GetSimpleDetail()
        {
            return EventText;
        }

        public override string GetFullDetail()
        {
            return GetLongFormattedDate + "==>" + GetSimpleDetail();
        }

        /// <summary>
        /// Return the formatted item name before it can be shown to the user.
        /// </summary>
        /// <param name="_msg"></param>
        /// <returns></returns>
        protected String FormatName(String _name)
        {
            if (_name == "") return "the root";
            return "'" + _name + "'";
        }
    }

    /// <summary>
    /// Notification item specific to the file downloaded event.
    /// </summary>
    public class KfsFileDownloadedNotificationItem : NotificationItem
    {
        /// <summary>
        /// ID of the user who downloaded the file.
        /// </summary>
        private UInt32 m_downloaderUID;

        /// <summary>
        /// User ID of the user who uploaded the version of the file being 
        /// downloaded.
        /// </summary>
        private UInt64 m_uploaderUID;

        private String m_fileName;

        private String m_fileRelativePath;

        public KfsFileDownloadedNotificationItem(AnpMsg _msg, IAppHelper _helper, KfsServerFile f)
            : base(_msg, KAnpType.KANP_NS_KFS, _helper)
        {
            m_downloaderUID = _msg.Elements[2].UInt32;
            m_uploaderUID = f.CurrentVersion.UserID;
            m_fileName = f.Name;
            m_fileRelativePath = f.RelativePath;

            m_notificationToTake = NotificationEffect.ShowPopup;
        }

        public override string EventText
        {
            get
            {
                return m_helper.GetUserDisplayName(m_downloaderUID) + " is downloading " + m_fileName;
            }
        }

        public override String GetSimplifiedFormattedDetail()
        {
            return EventText;
        }

        public override String GetFullFormattedDetail()
        {
            return GetLongFormattedDate + GetSimplifiedFormattedDetail();
        }
        public override string GetSimpleDetail()
        {
            return EventText;
        }

        public override string GetFullDetail()
        {
            return GetLongFormattedDate + "==>" + GetSimpleDetail();
        }
    }
}
