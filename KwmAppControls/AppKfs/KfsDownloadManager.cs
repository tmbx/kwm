using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum DownloadManagerStatus
    {
        /// <summary>
        /// No operation in progress.
        /// </summary>
        Idle,

        /// <summary>
        /// Waiting for download ticket.
        /// </summary>
        Ticket,

        /// <summary>
        /// Download batch in progress.
        /// </summary>
        Batch
    }

    /// <summary>
    /// Logic to manage the file downloads.
    /// </summary>
    public class KfsDownloadManager
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Tree of file downloads indexed by inode ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsFileDownload> InodeTree = new SortedDictionary<UInt64, KfsFileDownload>();

        /// <summary>
        /// Tree of file downloads indexed by order ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsFileDownload> OrderTree = new SortedDictionary<UInt64, KfsFileDownload>();

        /// <summary>
        /// Tree of files that have been downloaded indexed by order ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsFileDownload> DoneTree = new SortedDictionary<UInt64, KfsFileDownload>();

        /// <summary>
        /// Current download ticket, if any.
        /// </summary>
        public byte[] Ticket;

        /// <summary>
        /// Current download thread, if any.
        /// </summary>
        public KfsDownloadThread TransferThread;

        /// <summary>
        ///  Download status.
        /// </summary>
        public DownloadManagerStatus Status = DownloadManagerStatus.Idle;

        public KfsDownloadManager(KfsShare S)
        {
            Share = S;
            Share.DownloadManager = this;
        }

        /// <summary>
        /// Return the full path to the file having the version specified in
        /// the cache.
        /// </summary>
        public String VersionCachePath(KfsServerFileVersion version)
        {
            return Share.CacheDirPath + "File_" + version.Inode + "_" + version.CommitID;
        }

        /// <summary>
        /// Return true if the file having the version specified was fully
        /// downloaded in the cache.
        /// </summary>
        public bool HasFullVersion(KfsServerFileVersion version)
        {
            String path = VersionCachePath(version);
            if (File.Exists(path)) return (KfsPath.GetFileSize(path) == version.Size);
            return false;
        }

        /// <summary>
        /// Queue the download of the specified file version. The method checks
        /// if the file is already being downloaded. If the file has already been
        /// downloaded, the file is added to the DoneTree immediately.
        /// </summary>
        public void QueueDownload(KfsServerFileVersion version, bool openFlag)
        {
            // The download is already queued.
            if (InodeTree.ContainsKey(version.Inode))
            {
                if (openFlag) InodeTree[version.Inode].OpenWhenDownloadedFlag = true;
                return;
            }

            // Queue the file download.
            UInt64 orderID = Share.TransferOrderID++;
            String relPath = Share.ServerView.GetObjectByInode(version.Inode).RelativePath;
            KfsFileDownload f = new KfsFileDownload(Share, orderID, relPath, version, openFlag);
            InodeTree[f.Version.Inode] = f;
            OrderTree[f.OrderID] = f;
            Share.RegisterFileTransfer(f);

            // The file is already in the cache.
            if (HasFullVersion(version))
            {
                f.Status = FileTransferStatus.Batched;
                OnFileDownloadCompleted(f.OrderID);
            }

            // The file is empty. Create it immediately.
            else if (version.Size == 0)
            {
                Directory.CreateDirectory(Share.CacheDirPath);

                File.Create(VersionCachePath(version)).Close();
                f.Status = FileTransferStatus.Batched;
                OnFileDownloadCompleted(f.OrderID);
            }
        }

        /// <summary>
        /// Cancel the download of the file with the specified order ID.
        /// </summary>
        public void CancelDownload(UInt64 orderID)
        {
            Logging.Log("CancelDownload(" + orderID + ") called.");
            if (!OrderTree.ContainsKey(orderID)) return;
            KfsFileDownload f = OrderTree[orderID];

            // Cancel the download immediately if it is queued or tranfered.
            if (f.Status == FileTransferStatus.Queued || f.Status == FileTransferStatus.Transferred)
                RemoveDownload(f);

            // Ask the download thread to confirm the cancellation, if required.
            else if (f.Status == FileTransferStatus.Batched && !f.CancelFlag)
            {
                TransferThread.PostToWorker(new DownloadCancelMsg(TransferThread, orderID));
                f.CancelFlag = true;
            }
            
            
        }

        /// <summary>
        /// Ask a download ticket.
        /// </summary>
        public void AskTicket()
        {
            Debug.Assert(Status == DownloadManagerStatus.Idle);
            Debug.Assert(Ticket == null);
            Share.AskDownloadTicket();
            Status = DownloadManagerStatus.Ticket;
        }

        /// <summary>
        /// Start a new file transfer batch.
        /// </summary>
        public void StartBatch()
        {
            Debug.Assert(OrderTree.Count > 0);
            Debug.Assert(Status == DownloadManagerStatus.Idle);
            Debug.Assert(TransferThread == null);
            Debug.Assert(Ticket != null);

            try
            {
                // Build the tree.
                SortedDictionary<UInt64, KfsDownloadBatchFile> tree = new SortedDictionary<UInt64, KfsDownloadBatchFile>();

                foreach (KfsFileDownload f in OrderTree.Values)
                {
                    Debug.Assert(f.Status == FileTransferStatus.Queued);
                    f.Status = FileTransferStatus.Batched;
                    tree[f.OrderID] = new KfsDownloadBatchFile(f.OrderID,
                                                               f.Version,
                                                               VersionCachePath(f.Version));
                }

                // Start the transfer.
                TransferThread = new KfsDownloadThread(Share, Ticket, tree);
                TransferThread.Start();

                Status = DownloadManagerStatus.Batch;
                Ticket = null;
            }

            catch (Exception ex)
            {
                Share.FatalError(ex);
            }
        }

        /// <summary>
        /// Stop the download manager if it is operating. The pipeline is not
        /// notified.
        /// </summary>
        public void CancelOperation()
        {
            if (Status == DownloadManagerStatus.Ticket) GoIdle();
            else if (TransferThread != null) TransferThread.RequestCancellation();
        }

        /// <summary>
        /// Move the transferred files in the share.
        /// </summary>
        public void MoveAllDownloadedFiles()
        {
            SortedDictionary<UInt64, KfsFileDownload> tree = new SortedDictionary<UInt64, KfsFileDownload>(DoneTree);
            foreach (KfsFileDownload f in tree.Values) MoveDownloadedFile(f);
        }

        /// <summary>
        /// Move a file in the share.
        /// </summary>
        public void MoveDownloadedFile(KfsFileDownload f)
        {
            try
            {
                // Get the corresponding server file.
                KfsServerFile s = Share.ServerView.GetObjectByInode(f.Version.Inode) as KfsServerFile;

                // The file has been deleted on the server. 
                if (s == null) throw new Exception("cannot open file, it has been deleted on the server");

                // Update the local status and check whether the move is safe.
                s.UpdateLocalStatus();

                if (s.CurrentLocalStatus == LocalStatus.Error)
                    throw new Exception("cannot open file: " + s.LocalError);

                if (s.CurrentLocalStatus == LocalStatus.Modified)
                    throw new Exception("cannot update file: local copy has been modified");

                // Create the missing parent directories, if possible.
                Share.AddMissingLocalDirectories(s.Parent.RelativePath);

                // Move the file at its proper location.
                if (File.Exists(s.FullPath))
                    File.SetAttributes(s.FullPath, FileAttributes.Normal);

                if (new FileInfo(f.CachePath).Length < 10 * 1024 * 1024)
                    File.Copy(f.CachePath, s.FullPath, true);
                else if (!Misc.CopyFile(f.CachePath, s.FullPath, false, true, false, true, true))
                    throw new Exception("Unable to copy " + s.Name);

                // Update the downloaded version.
                s.UpdateDownloadVersion(f.Version, true);

                // Update the local status of the downloaded file.
                s.UpdateLocalStatus();

                // Open the file for the user.
                if (f.OpenWhenDownloadedFlag)
                    Misc.OpenFileInWorkerThread(s.FullPath);
            }

            catch (Exception ex)
            {
                ReportError(ex.Message, f);
            }

            finally
            {
                RemoveDownload(f);
            }
        }

        /// <summary>
        /// Update the full path of every download.
        /// </summary>
        public void UpdateLastFullPath()
        {
            foreach (KfsFileDownload f in OrderTree.Values) f.UpdateLastFullPath();
        }

        /// <summary>
        /// Delete files from the cache if it is full.
        /// </summary>
        public void ClearCacheIfNeeded()
        {
            try
            {
                // Compute the total size.
                UInt64 totalSize = 0;

                foreach (String s in Directory.GetFiles(Share.CacheDirPath))
                    totalSize += KfsPath.GetFileSize(s);

                // Clear the cache.
                if (totalSize > KfsShare.MaxCacheSize)
                {
                    Logging.Log("Clearing download cache.");
                    foreach (String s in Directory.GetFiles(Share.CacheDirPath)) File.Delete(s);
                }
            }

            catch (Exception ex)
            {
                Logging.Log(2, "Failed to clear the download cache: " + ex.Message + ".");
            }
        }

        /// <summary>
        /// Add the file download to the list of errors, unless 'reason' is null.
        /// </summary>
        public void ReportError(String reason, KfsFileDownload f)
        {
            if (reason != null)
            {
                f.Error = new KfsTransferError(TransferErrorType.Download, f, reason);
                Share.OnTransferError(f.Error);
            }
        }

        /// <summary>
        /// This method is called when a download ticket reply is received.
        /// </summary>
        public void OnTicket(AnpMsg m)
        {
            Debug.Assert(Status == DownloadManagerStatus.Ticket);
            Debug.Assert(Ticket == null);

            // Store the ticket and run the pipeline.
            if (m.Type == KAnpType.KANP_RES_KFS_DOWNLOAD_REQ)
            {
                Status = DownloadManagerStatus.Idle;
                Ticket = m.Elements[0].Bin;
                Share.Pipeline.Run("got download ticket", false);
            }

            // On failure, cancel all downloads.
            else
            {
                CancelOnError(m.Elements[0].String);
                Share.Pipeline.Run("could not obtain download ticket", true);
            }
        }

        /// <summary>
        /// This method is called when the download of a file has been cancelled as
        /// requested.
        /// </summary>
        public void OnFileDownloadCancelled(UInt64 orderID)
        {
            Debug.Assert(OrderTree.ContainsKey(orderID));
            KfsFileDownload f = OrderTree[orderID];
            Debug.Assert(f.Status == FileTransferStatus.Batched);
            RemoveDownload(f);
        }

        /// <summary>
        /// This method is called when the download of a file has been completed.
        /// </summary>
        public void OnFileDownloadCompleted(UInt64 orderID)
        {
            Debug.Assert(OrderTree.ContainsKey(orderID));
            KfsFileDownload f = OrderTree[orderID];
            Debug.Assert(f.Status == FileTransferStatus.Batched);
            f.Status = FileTransferStatus.Transferred;
            f.BytesTransferred = f.Version.Size;
            DoneTree[orderID] = f;
            Share.Pipeline.Run("file download completed", false);
        }

        /// <summary>
        /// This method is called when the download of a file has progressed.
        /// </summary>
        public void OnFileDownloadProgression(UInt64 orderID, UInt64 bytesTransferred)
        {
            Debug.Assert(OrderTree.ContainsKey(orderID));
            KfsFileDownload f = OrderTree[orderID];
            Debug.Assert(f.Status == FileTransferStatus.Batched);
            f.BytesTransferred = bytesTransferred;
            // FIXME : implement a Share.RequestXferUiUpdate
            Share.UpdateXferUI();
        }

        /// <summary>
        /// This method is called when a file download batch has been completed.
        /// 'successFlag' is true if the thread has been cancelled (this is not
        /// an error).
        /// </summary>
        public void OnBatchCompleted(bool successFlag, String reason)
        {
            // Join with the download batch thread.
            Debug.Assert(TransferThread != null);
            TransferThread = null;

            // The transfer batch failed.
            if (!successFlag)
            {
                // Cancel the download of all queued and batched files and do a
                // full run to clear the downloaded files.
                CancelOnError(reason);
                Share.Pipeline.Run("file download batch failed", true);
            }

            // The transfer batch succeeded.
            else
            {
                // If the transfers of some files were cancelled, then the download
                // thread may have bailed out early. Mark the non-cancelled batched
                // files as non-batched, otherwise cancel their transfers.
                SortedDictionary<ulong, KfsFileDownload> orderTreeCopy = 
                    new SortedDictionary<ulong, KfsFileDownload>(OrderTree);

                foreach (KfsFileDownload f in orderTreeCopy.Values)
                {
                    if (f.Status == FileTransferStatus.Batched)
                    {
                        if (f.CancelFlag) OnFileDownloadCancelled(f.OrderID);
                        else f.Status = FileTransferStatus.Queued;
                    }
                }

                // Go idle and run the pipeline.
                GoIdle();
                Share.Pipeline.Run("file download batch finished", false);
            }
        }

        /// <summary>
        /// Remove the specified download.
        /// </summary>
        private void RemoveDownload(KfsFileDownload f)
        {
            InodeTree.Remove(f.Version.Inode);
            OrderTree.Remove(f.OrderID);
            if (DoneTree.ContainsKey(f.OrderID)) DoneTree.Remove(f.OrderID);
            Share.UnregisterFileTransfer(f);
        }

        /// <summary>
        /// Cancel all the queued and batched downloads when an error occurs and
        /// go idle.
        /// </summary>
        private void CancelOnError(String reason)
        {
            SortedDictionary<UInt64, KfsFileDownload> tree = new SortedDictionary<UInt64, KfsFileDownload>(OrderTree);
            foreach (KfsFileDownload f in tree.Values)
            {
                if (f.Status == FileTransferStatus.Queued || f.Status == FileTransferStatus.Batched)
                {
                    ReportError(reason, f);
                    RemoveDownload(f);
                }
            }

            GoIdle();
        }

        /// <summary>
        /// Tidy-up the state after a download batch has been completed or an
        /// error occured.
        /// </summary>
        private void GoIdle()
        {
            Status = DownloadManagerStatus.Idle;
        }
    }

    /// <summary>
    /// Represent the download of a file.
    /// </summary>
    public class KfsFileDownload : KfsFileTransfer
    {
        /// <summary>
        /// Version being downloaded.
        /// </summary>
        public KfsServerFileVersion Version;

        /// <summary>
        /// True if the file needs to be opened when downloaded.
        /// </summary>
        public bool OpenWhenDownloadedFlag;

        // Path to the file in the cache.
        public String CachePath { get { return Share.DownloadManager.VersionCachePath(Version); } }

        public KfsFileDownload(KfsShare s, UInt64 orderID, String lastFullPath,
                               KfsServerFileVersion version, bool openFlag)
            : base(s, orderID, lastFullPath)
        {
            Version = version;
            OpenWhenDownloadedFlag = openFlag;
        }

        /// <summary>
        /// Update the full path using the information obtained from the tracked
        /// inode.
        /// </summary>
        public void UpdateLastFullPath()
        {
            KfsServerFile f = Share.ServerView.GetObjectByInode(Version.Inode) as KfsServerFile;

            // If the tracked inode disappeared, the downloaded file is no longer
            // needed. Raise an error and cancel the download.
            if (f == null)
            {
                Share.DownloadManager.ReportError("file was deleted on the server", this);
                Share.DownloadManager.CancelDownload(OrderID);
            }

            // Update the path.
            else
            {
                String newPath = f.RelativePath;

                if (LastFullPath != newPath)
                {
                    Share.UnregisterFileTransfer(this);
                    LastFullPath = newPath;
                    Share.RegisterFileTransfer(this);
                }
            }
        }
    }

    /// <summary>
    /// Thread that downloads a batch of files.
    /// </summary>
    public class KfsDownloadThread : KfsTransferThread
    {
        /// <summary>
        /// Tree of batched files indexed by order ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsDownloadBatchFile> OrderTree;

        /// <summary>
        /// Array of batched files.
        /// </summary>
        private KfsDownloadBatchFile[] FileArray;

        // Index in the file array corresponding to the file currently being
        // downloaded. When all files have been downloaded, the index is equal
        // to the length of the file array.
        private int DownloadIndex = 0;

        /// <summary>
        /// File currently being downloaded.
        /// </summary>
        private FileStream DownloadedFile;

        /// <summary>
        /// Size of the remaining data to read in the downloaded file. 
        /// </summary>
        private UInt64 RemainingSize;

        public KfsDownloadThread(KfsShare s, byte[] ticket,
                                 SortedDictionary<UInt64, KfsDownloadBatchFile> orderTree)
            : base(s, ticket)
        {
            Debug.Assert(orderTree.Count > 0);
            OrderTree = orderTree;
            FileArray = new KfsDownloadBatchFile[OrderTree.Count];
            OrderTree.Values.CopyTo(FileArray, 0);
        }

        /// <summary>
        /// Called to cancel the download of the file specified.
        /// </summary>
        public void CancelDownloadHandler(UInt64 orderID)
        {
            Logging.Log("CancelDownloadHandler(" + orderID + ") called.");
            KfsDownloadBatchFile f = OrderTree[orderID];
            if (f.Status == BatchStatus.Queued || f.Status == BatchStatus.Started)
            {
                f.Status = BatchStatus.Cancelled;
                PostToUI(new UiDownloadCancelledMsg(Share, orderID));
            }
        }

        protected override void OnTunnelConnected()
        {
            // Negociate the role.
            NegociateRole();

            // Send the download message.
            SendDownloadMessage();

            // Download the files.
            DownloadFiles();
        }

        /// <summary>
        /// Extend the cancellation check to check if the current downloaded file
        /// has been cancelled.
        /// </summary>
        protected override void CheckCancellation()
        {
            base.CheckCancellation();
            if (DownloadIndex != FileArray.Length &&
                FileArray[DownloadIndex].Status == BatchStatus.Cancelled)
            {
                throw new WorkerCancellationException();
            }
        }

        protected override void OnCompletion()
        {
            base.OnCompletion();

            CloseDownloadedFile(true);

            if (Status == WorkerStatus.Cancelled)
                Share.DownloadManager.OnBatchCompleted(true, null);

            else if (Status == WorkerStatus.Failed)
                Share.DownloadManager.OnBatchCompleted(false, FailException.Message);

            else if (Status == WorkerStatus.Success)
                Share.DownloadManager.OnBatchCompleted(true, null);

            else Debug.Assert(false);
        }

        /// <summary>
        /// Send the download data command message.
        /// </summary>
        private void SendDownloadMessage()
        {
            AnpMsg m = Share.CreateTransferMsg(KAnpType.KANP_CMD_KFS_DOWNLOAD_DATA);
            m.AddBin(Ticket);
            m.AddUInt32((UInt32)FileArray.Length);

            foreach (KfsDownloadBatchFile f in FileArray)
            {
                f.GetDownloadOffset();
                m.AddUInt64(f.Version.Inode);
                m.AddUInt64(f.DownloadOffset);
                m.AddUInt64(f.Version.CommitID);
            }

            SendAnpMsg(m);
        }

        /// <summary>
        /// Download the batched files.
        /// </summary>
        private void DownloadFiles()
        {
            // Receive ANP messages until we've received all the data.
            while (DownloadIndex != FileArray.Length)
            {
                // Handle the next message.
                AnpMsg m = GetAnpMsg();
                if (m.Type == KAnpType.KANP_RES_FAIL) throw new Exception(m.Elements[1].String);
                if (m.Type != KAnpType.KANP_RES_KFS_DOWNLOAD_DATA)
                    throw new Exception("expected KANP_RES_KFS_DOWNLOAD_DATA");
                HandleDownloadReply(m);
            }

            if (DownloadedFile != null) throw new Exception("missing final data chunks");
        }

        /// <summary>
        /// Handle a download data reply message.
        /// </summary>
        private void HandleDownloadReply(AnpMsg m)
        {
            // Get the number of submessages.
            UInt32 nbSub = m.Elements[0].UInt32;

            // Handle the submessages.
            int curPos = 1;

            for (UInt32 i = 0; i < nbSub; i++)
            {
                UInt32 subType = m.Elements[curPos + 1].UInt32;

                // Start the download of the current file.
                if (subType == KAnpType.KANP_KFS_SUBMESSAGE_FILE)
                {
                    UInt64 totalSize = m.Elements[curPos + 2].UInt64;
                    UInt64 remainingSize = m.Elements[curPos + 3].UInt64;
                    StartDownload(totalSize, remainingSize);
                }

                // Retrieve the next chunk.
                else if (subType == KAnpType.KANP_KFS_SUBMESSAGE_CHUNK)
                {
                    byte[] chunk = m.Elements[curPos + 2].Bin;
                    HandleChunk(chunk);
                }

                // Oops.
                else throw new Exception("unexpected submessage type " + subType);

                // Check for completion of the current file. Don't do this in
                // HandleChunk() since it is not guaranteed that there is a chunk
                // for a file.
                if (DownloadedFile != null && RemainingSize == 0) EndDownload();

                // Pass to the next submessage.
                curPos += (int)m.Elements[curPos].UInt32;

                CheckCancellation();
            }

            // Send a progression message, if required.
            HandleProgression();
        }

        /// <summary>
        /// Start the download of the current file.
        /// </summary>
        private void StartDownload(UInt64 totalSize, UInt64 remainingSize)
        {
            // Check if the server is sending us unwanted files.
            if (DownloadIndex == FileArray.Length) throw new Exception("too many files received");
            if (DownloadedFile != null) throw new Exception("missing data chunks");

            // Check for cancellation.
            KfsDownloadBatchFile dbf = FileArray[DownloadIndex];
            CheckCancellation();
            Debug.Assert(dbf.Status == BatchStatus.Queued);

            // Validate the information about the file.
            if (totalSize != dbf.Version.Size) throw new Exception("unexpected file size");
            if (remainingSize != dbf.DownloadSize)
                throw new Exception("unexpected remaining size");

            // Start the download, open the file and seek at the right position.
            dbf.Status = BatchStatus.Started;
            RemainingSize = remainingSize;
            Directory.CreateDirectory(Share.CacheDirPath);
            DownloadedFile = new FileStream(dbf.TransferPath, FileMode.OpenOrCreate, FileAccess.Write);
            DownloadedFile.Seek((Int32)dbf.DownloadOffset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Handle the chunk received from the server.
        /// </summary>
        private void HandleChunk(byte[] chunk)
        {
            if (DownloadedFile == null) throw new Exception("unexpected chunk submessage");
            if ((UInt64)chunk.Length > RemainingSize) throw new Exception("unexpected chunk size");
            DownloadedFile.Write(chunk, 0, chunk.Length);
            RemainingSize -= (UInt32)chunk.Length;
        }

        /// <summary>
        /// End the download of the current file.
        /// </summary>
        private void EndDownload()
        {
            Debug.Assert(DownloadedFile != null);
            Debug.Assert(RemainingSize == 0);
            KfsDownloadBatchFile dbf = FileArray[DownloadIndex];
            Debug.Assert(dbf.Status == BatchStatus.Started);

            // Close the file.
            CloseDownloadedFile(false);

            // Notify the UI that we completed the download.
            dbf.Status = BatchStatus.Done;
            PostToUI(new UiDownloadCompletedMsg(Share, dbf.OrderID));

            // Pass to the next file, if any.
            DownloadIndex++;
        }

        /// <summary>
        /// Send a progression message, if required.
        /// </summary>
        private void HandleProgression()
        {
            if (DownloadIndex == FileArray.Length) return;
            KfsDownloadBatchFile dbf = FileArray[DownloadIndex];
            if (dbf.Status != BatchStatus.Started) return;
            UInt64 bytesTransferred = dbf.DownloadSize - RemainingSize;
            PostToUI(new UiDownloadProgressionMsg(Share, dbf.OrderID, bytesTransferred));
        }

        /// <summary>
        /// Close the current downloaded file if it is open.
        /// </summary>
        /// <param name="silentFlag">True if exceptions should be ignored</param>
        private void CloseDownloadedFile(bool silentFlag)
        {
            try
            {
                if (DownloadedFile != null)
                {
                    DownloadedFile.Close();
                    DownloadedFile = null;
                }
            }

            catch (Exception)
            {
                if (!silentFlag) throw;
            }
        }
    }

    /// <summary>
    /// Represent a file in a download batch.
    /// </summary>
    public class KfsDownloadBatchFile : KfsTransferBatchFile
    {
        /// <summary>
        /// Version being downloaded.
        /// </summary>
        public KfsServerFileVersion Version;

        /// <summary>
        /// Download offset in the file.
        /// </summary>
        public UInt64 DownloadOffset = 0;

        /// <summary>
        /// Size of the data to download, based on the size of the file and
        /// the download offset.
        /// </summary>
        public UInt64 DownloadSize
        {
            get { return Version.Size - DownloadOffset; }
        }

        public KfsDownloadBatchFile(UInt64 orderID, KfsServerFileVersion version, String transferPath)
            : base(orderID, transferPath)
        {
            Version = version;
        }

        /// <summary>
        /// Make sure the file has a valid size and get the download offset.
        /// </summary>
        public void GetDownloadOffset()
        {
            if (File.Exists(TransferPath))
            {
                UInt64 size = KfsPath.GetFileSize(TransferPath);

                // The file is too large or we got it in full. This is unexpected
                // so delete the file.
                if (size >= Version.Size) File.Delete(TransferPath);

                // Update the download offset.
                else DownloadOffset = size;
            }
        }
    }

    /// <summary>
    /// Represent a message sent to a download thread.
    /// </summary>
    public abstract class DownloadTransferMsg : WorkerThreadMsg
    {
        /// <summary>
        /// Reference to the download thread.
        /// </summary>
        public KfsDownloadThread DownloadThread;

        public DownloadTransferMsg(KfsDownloadThread downloadThread)
        {
            DownloadThread = downloadThread;
        }
    }

    /// <summary>
    /// Request a file download to be cancelled.
    /// </summary>
    public class DownloadCancelMsg : DownloadTransferMsg
    {
        UInt64 OrderID;

        public DownloadCancelMsg(KfsDownloadThread downloadThread, UInt64 orderID)
            : base(downloadThread)
        {
            OrderID = orderID;
        }

        public override void Run()
        {
            DownloadThread.CancelDownloadHandler(OrderID);
        }
    }

    /// <summary>
    /// Message sent when a file download is cancelled.
    /// </summary>
    public class UiDownloadCancelledMsg : UiTransferMsg
    {
        UInt64 OrderID;

        public UiDownloadCancelledMsg(KfsShare share, UInt64 orderID)
            : base(share)
        {
            OrderID = orderID;
        }

        public override void Run()
        {
            Share.DownloadManager.OnFileDownloadCancelled(OrderID);
        }
    }

    /// <summary>
    /// Message sent when the download of a file has been completed.
    /// </summary>
    public class UiDownloadCompletedMsg : UiTransferMsg
    {
        UInt64 OrderID;

        public UiDownloadCompletedMsg(KfsShare share, UInt64 orderID)
            : base(share)
        {
            OrderID = orderID;
        }

        public override void Run()
        {
            Share.DownloadManager.OnFileDownloadCompleted(OrderID);
        }
    }

    /// <summary>
    /// Message sent when the download of a file has progressed.
    /// </summary>
    public class UiDownloadProgressionMsg : UiTransferMsg
    {
        UInt64 OrderID;
        UInt64 BytesTransferred;

        public UiDownloadProgressionMsg(KfsShare share, UInt64 orderID, UInt64 bytesTransferred)
            : base(share)
        {
            OrderID = orderID;
            BytesTransferred = bytesTransferred;
        }

        public override void Run()
        {
            Share.DownloadManager.OnFileDownloadProgression(OrderID, BytesTransferred);
        }
    }
}