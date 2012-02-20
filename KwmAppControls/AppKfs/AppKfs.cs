using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using kwm.Utils;
using System.Runtime.Serialization;
using System.Collections.Specialized;
using System.Threading;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    [Serializable]
    public sealed class AppKfs : KwsApp, ISerializable
    {
        /// <summary>
        /// Reference to the share, if any.
        /// </summary>
        KfsShare m_share = null;

        [field: NonSerialized]
        public event EventHandler<EventArgs> OnUIUpdateRequired;

        [field: NonSerialized]
        public event EventHandler<EventArgs> OnUIXferUpdateRequired;

        public override UInt32 AppID { get { return KAnpType.KANP_NS_KFS; } }

        /// <summary>
        /// Reference to the share, if any.
        /// </summary>
        public KfsShare Share
        {
            get { return m_share; }
        }

        public AppKfs(IAppHelper _helper)
            : base(_helper)
        {
        }

        public AppKfs(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            m_share = info.GetValue("m_Share", typeof(KfsShare)) as KfsShare;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("m_Share", m_share);
        }

        public override void Initialize(IAppHelper appHelper)
        {
            base.Initialize(appHelper);
            if (m_share != null) m_share.Initialize();
        }


        public override void NormalizeState()
        {
            if (m_share == null) m_share = new KfsShare(this); 
            m_share.NormalizeState();
        }

        public override void PrepareForRebuild(KwsRebuildInfo rebuildInfo)
        {
            if (rebuildInfo.DeleteLocalDataFlag) DeleteLocalFiles();

            // Create a new share object if this is not a soft upgrade.
            if (!rebuildInfo.UpgradeFlag) m_share = new KfsShare(this);

            base.PrepareForRebuild(rebuildInfo);
            NormalizeState();
        }

        public override void DeleteAllData()
        {
            base.DeleteAllData();
            DeleteLocalFiles();
        }

        public override KwsAnpEventStatus HandleAnpEvent(AnpMsg msg)
        {
            // For compatibility purposes.
            if (msg.ID <= Share.CompatLastKwsEventID) return KwsAnpEventStatus.Processed;

            if (msg.Type == KAnpType.KANP_EVT_KFS_PHASE_1)
            {
                Share.OnPhase1Event(msg);
                return KwsAnpEventStatus.Processed;
            }

            else if (msg.Type == KAnpType.KANP_EVT_KFS_PHASE_2)
            {
                Share.OnPhase2Event(msg);
                return KwsAnpEventStatus.Processed;
            }

            else if (msg.Type == KAnpType.KANP_EVT_KFS_DOWNLOAD)
            {
                OnDownloadEvent(msg);
                return KwsAnpEventStatus.Processed;
            }

            else return KwsAnpEventStatus.Unprocessed;
        }

        public override void RequestStart()
        {
            // We have nothing to do here; the KFS share pipeline will run 
            // when a notification is received.
            Debug.Assert(Share != null);
            base.RequestStart();
        }

        public override void RequestStop()
        {
            // Ask the share to stop and run the pipeline. The pipeline
            // will notify the workspace when it has stopped.
            if (Share != null)
            {
                Share.TryStop();
                Share.Pipeline.Run("RequestStop() called", true);
            }
            
            // We're already stopped.
            else base.RequestStop();
        }

        public override void OnKwsStatusChanged(KwsRunLevel newRunLevel, bool newCaughtUpFlag)
        {
            // We have a share.
            if (Share != null)
            {
                // We were online and we're no longer online; make sure
                // we're not waiting for messages that won't come.
                if (NotifiedRunLevel == KwsRunLevel.Online && newRunLevel != KwsRunLevel.Online)
                    Share.OnNoLongerOnline();

                // Run the KFS pipeline on state changes.
                if (NotifiedRunLevel != newRunLevel || NotifiedCaughtUpFlag != newCaughtUpFlag)
                    Share.Pipeline.Run("Notification received", false);
            }

            base.OnKwsStatusChanged(newRunLevel, newCaughtUpFlag);
        }

        /// <summary>
        /// Notify user if a file has been downloaded in our Public workspace.
        /// </summary>
        private void OnDownloadEvent(AnpMsg msg)
        {
            // If this is not related to our public workspace, or if we triggered
            // the event ourself, ignore.
            if (!Helper.IsPublicKws() ||
                msg.Elements[2].UInt32 == Helper.GetKwmUser().UserID)
            {
                return;
            }

            UInt64 inode = msg.Elements[4].UInt64;
            KfsServerFile f = Share.ServerView.GetObjectByInode(inode) as KfsServerFile;
            if (f == null)
            {
                Logging.Log(2, "A user downloaded some deleted file, ignoring.");
                return;
            }

            Helper.NotifyUser(new KfsFileDownloadedNotificationItem(msg, Helper, f));
        }

        /// <summary>
        /// Delete all the local KFS files (local store, cache and upload dir).
        /// </summary>
        private void DeleteLocalFiles()
        {
            try
            {
                Directory.Delete(Share.ShareFullPath, true);
            }
            catch (Exception) { }
            
            try
            {
                Directory.Delete(Share.CacheDirPath, true);
            }
            catch (Exception) {}

            try
            {
                Directory.Delete(Share.UploadDirPath, true);
            }
            catch (Exception) {}

            try
            {
                File.Delete(Share.AppliedOpFilePath);
                File.Delete(Share.AppliedOpFilePath + ".tmp");
            }
            catch (Exception) { }
        }

        public void DoOnUIUpdateRequired()
        {
            if (OnUIUpdateRequired != null)
                OnUIUpdateRequired(this, new EventArgs());
        }

        public void DoOnXferUpdateRequired()
        {
            if (OnUIXferUpdateRequired != null)
                OnUIXferUpdateRequired(this, new EventArgs());
        }
    }
}