using kwm.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Iesi.Collections.Generic;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum UploadManagerStatus
    {
        /// <summary>
        /// No operation in progress.
        /// </summary>
        Idle,

        /// <summary>
        /// Waiting for upload ticket.
        /// </summary>
        Ticket,

        /// <summary>
        /// Upload batch in progress.
        /// </summary>
        Batch,

        /// <summary>
        /// Waiting for phase 2 event.
        /// </summary>
        Phase2
    }

    /// <summary>
    /// Logic to manage the uploads.
    /// </summary>
    public class KfsUploadManager
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Tree of file uploads indexed by order ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsFileUpload> OrderTree = new SortedDictionary<UInt64, KfsFileUpload>();

        /// <summary>
        /// Tree of files that have been uploaded indexed by order ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsFileUpload> DoneTree = new SortedDictionary<UInt64, KfsFileUpload>();

        /// <summary>
        /// Current upload ticket, if any.
        /// </summary>
        public byte[] Ticket;

        /// <summary>
        /// Current upload thread, if any.
        /// </summary>
        public KfsUploadThread TransferThread;

        /// <summary>
        /// Upload status.
        /// </summary>
        public UploadManagerStatus Status = UploadManagerStatus.Idle;

        /// <summary>
        /// If Status is "batch" or "phase2", then we collect all phase 2 commit IDs
        /// because we need to know when our changes have been applied in the UI view.
        /// </summary>
        public SortedSet<UInt64> CommitIDSet = new SortedSet<UInt64>();

        /// <summary>
        /// Commit ID we are waiting for.
        /// </summary>
        public UInt64 WantedCommitID;

        public KfsUploadManager(KfsShare S)
        {
            Share = S;
            Share.UploadManager = this;
        }

        /// <summary>
        /// Queue the upload of the file specified. The method checks if the
        /// file is already being transferred. If so, null is returned, otherwise
        /// the file is copied in the upload directory and the reference to the
        /// file upload is returned.
        /// </summary>
        public KfsFileUpload QueueUpload(String path)
        {
            // If the file is already being transferred, abort.
            if (Share.IsTransferringFile(path)) return null;

            String fullPath = Share.MakeAbsolute(path);
            UInt64 trackedInode;
            String trackedPath;
            UInt64 updateCommitID;

            // Locate the file, if it exists.
            KfsServerFile f = Share.ServerView.GetObjectByPath(path) as KfsServerFile;

            // The file exists and it is not a ghost. Queue as update.
            if (f != null && !f.IsGhost())
            {
                trackedInode = f.Inode;
                trackedPath = "";
                updateCommitID = f.CommitID;
            }

            // The file does not exist or it is ghost. Queue as create. The caller
            // will get rid of the ghost if it exists.
            else
            {
                GetTrackingInodeAndPath(path, out trackedInode, out trackedPath);
                updateCommitID = 0;
            }

            // Copy the file in the upload directory.
            UInt64 orderID = Share.TransferOrderID++;
            String uploadPath = GetTemporaryPath(orderID);
            if (File.Exists(uploadPath))
                File.SetAttributes(uploadPath, FileAttributes.Normal);

            if (new FileInfo(fullPath).Length < 10 * 1024 * 1024)
                Base.SafeCopy(fullPath, uploadPath, true);

            else if (!Misc.CopyFile(fullPath, uploadPath, false, true, false, true, true))
                throw new Exception("Unable to copy " + path);

            // Compute the hash and the size.
            byte[] hash;
            UInt64 size;
            KfsHash.GetHashAndSize(fullPath, out hash, out size);

            // Queue the upload.
            KfsFileUpload u = new KfsFileUpload(Share, orderID, trackedInode, trackedPath,
                                                path, uploadPath, size, hash, updateCommitID);
            OrderTree[u.OrderID] = u;
            Share.RegisterFileTransfer(u);

            return u;
        }

        /// <summary>
        /// Cancel the upload of the file with the specified order ID.
        /// </summary>
        public void CancelUpload(UInt64 orderID)
        {
            if (!OrderTree.ContainsKey(orderID)) return;
            KfsFileUpload f = OrderTree[orderID];

            // Cancel the upload immediately if it is queued.
            if (f.Status == FileTransferStatus.Queued)
                RemoveUpload(f);

            // Ask the upload thread to confirm the cancellation, if required.
            else if (f.Status == FileTransferStatus.Batched && !f.CancelFlag)
            {
                TransferThread.PostToWorker(new UploadCancelMsg(TransferThread, orderID));
                f.CancelFlag = true;
            }
        }

        /// <summary>
        /// Ask an upload ticket.
        /// </summary>
        public void AskTicket()
        {
            Debug.Assert(Status == UploadManagerStatus.Idle);
            Debug.Assert(Ticket == null);
            Share.AskUploadTicket();
            Status = UploadManagerStatus.Ticket;
        }

        /// <summary>
        /// Start a new file transfer batch.
        /// </summary>
        public void StartBatch()
        {
            Debug.Assert(OrderTree.Count > 0);
            Debug.Assert(Status == UploadManagerStatus.Idle);
            Debug.Assert(TransferThread == null);
            Debug.Assert(Ticket != null);

            try
            {
                // Build the payload and the tree.
                KfsPhase1Payload p = new KfsPhase1Payload();
                SortedDictionary<UInt64, KfsUploadBatchFile> tree = new SortedDictionary<UInt64, KfsUploadBatchFile>();

                foreach (KfsFileUpload f in OrderTree.Values)
                {
                    Debug.Assert(f.Status == FileTransferStatus.Queued);
                    f.Status = FileTransferStatus.Batched;

                    // Request creation.
                    if (f.UpdateCommitID == 0)
                    {
                        // Use the tracked inode if it still exists, otherwise use the
                        // last full path.
                        KfsServerObject o = Share.ServerView.GetObjectByInode(f.TrackedInode);
                        if (o == null)
                        {
                            o = Share.ServerView.Root;
                            f.TrackedInode = o.Inode;
                            f.TrackedPath = f.LastFullPath;
                        }

                        Debug.Assert(o is KfsServerDirectory);
                        Debug.Assert(f.TrackedPath != "");
                        p.AddCreateOp(true, o.Inode, o.CommitID, f.TrackedPath);
                    }

                    // Request update.
                    else
                    {
                        Debug.Assert(f.TrackedPath == "");
                        p.AddUpdateOp(f.TrackedInode, f.UpdateCommitID);
                    }

                    tree[f.OrderID] = new KfsUploadBatchFile(f.OrderID, f.UploadPath, f.Hash);
                }
                
                // Start the transfer.
                TransferThread = new KfsUploadThread(Share, Ticket, tree, p);
                TransferThread.Start();

                Status = UploadManagerStatus.Batch;
                Ticket = null;
            }

            catch (Exception ex)
            {
                Share.FatalError(ex);
            }
        }

        /// <summary>
        /// Stop the upload manager if it is operating. The pipeline is not
        /// notified.
        /// </summary>
        public void CancelOperation()
        {
            if (Status == UploadManagerStatus.Ticket ||
                Status == UploadManagerStatus.Phase2) GoIdle();
            else if (TransferThread != null) TransferThread.RequestCancellation();
        }

        /// <summary>
        /// Clear all the completed uploads.
        /// </summary>
        public void ClearAllUploadedFiles()
        {
            SortedDictionary<UInt64, KfsFileUpload> tree = new SortedDictionary<UInt64, KfsFileUpload>(DoneTree);
            foreach (KfsFileUpload f in tree.Values) RemoveUpload(f);
        }

        /// <summary>
        /// Update the full path of every upload.
        /// </summary>
        public void UpdateLastFullPath()
        {
            foreach (KfsFileUpload f in OrderTree.Values) f.UpdateLastFullPath();
        }

        /// <summary>
        /// Add the file upload to the list of errors, unless 'reason' is null.
        /// </summary>
        public void ReportError(String reason, KfsFileUpload f)
        {
            if (reason != null)
            {
                f.Error = new KfsTransferError(TransferErrorType.Upload, f, reason);
                Share.OnTransferError(f.Error);
            }
        }

        /// <summary>
        /// This method is called when a upload ticket reply is received.
        /// </summary>
        public void OnTicket(AnpMsg m)
        {
            Debug.Assert(Status == UploadManagerStatus.Ticket);
            Debug.Assert(Ticket == null);

            // Store the ticket and run the pipeline.
            if (m.Type == KAnpType.KANP_RES_KFS_UPLOAD_REQ)
            {
                Status = UploadManagerStatus.Idle;
                Ticket = m.Elements[0].Bin;
                Share.Pipeline.Run("got upload ticket", false);
            }

            // On failure, cancel all uploads.
            else
            {
                CancelOnError(m.Elements[0].String);
                Share.Pipeline.Run("could not obtain upload ticket", true);
            }
        }

        /// <summary>
        /// This method is called when a phase 2 server event is received.
        /// </summary>
        public void OnPhase2Event(UInt64 CommitID)
        {
            // We're interested in the commit ID.
            if (Status == UploadManagerStatus.Batch)
                CommitIDSet.Add(CommitID);

            else if (Status == UploadManagerStatus.Phase2 && CommitID == WantedCommitID)
                OnCommitIDReceived();
        }

        /// <summary>
        /// This method is called when the upload of a file has been cancelled as
        /// requested.
        /// </summary>
        public void OnFileUploadCancelled(UInt64 orderID)
        {
            Debug.Assert(OrderTree.ContainsKey(orderID));
            KfsFileUpload f = OrderTree[orderID];
            Debug.Assert(f.Status == FileTransferStatus.Batched);
            RemoveUpload(f);
        }

        /// <summary>
        /// This method is called when the upload of a file has been completed.
        /// </summary>
        public void OnFileUploadCompleted(UInt64 orderID, bool successFlag, String reason)
        {
            Debug.Assert(OrderTree.ContainsKey(orderID));
            KfsFileUpload f = OrderTree[orderID];
            Debug.Assert(f.Status == FileTransferStatus.Batched);

            if (successFlag)
            {
                f.Status = FileTransferStatus.Transferred;
                f.BytesTransferred = f.Size;
                DoneTree[orderID] = f;
                Share.RequestStatusViewUpdate("OnFileUploadCompleted");
            }

            else
            {
                ReportError(reason, f);
                RemoveUpload(f);
            }

            Share.Pipeline.Run("file upload completed", false);
        }

        /// <summary>
        /// This method is called when the upload of a file has progressed.
        /// </summary>
        public void OnFileUploadProgression(UInt64 orderID, UInt64 bytesTransferred)
        {
            Debug.Assert(OrderTree.ContainsKey(orderID));
            KfsFileUpload f = OrderTree[orderID];
            Debug.Assert(f.Status == FileTransferStatus.Batched);
            f.BytesTransferred = bytesTransferred;
            // FIXME : implement a Share.RequestXferUiUpdate
            Share.UpdateXferUI();
            
        }

        /// <summary>
        /// This method is called when a file upload batch has been completed.
        /// 'reason' is null if the thread has been cancelled (on failure).
        /// </summary>
        public void OnBatchCompleted(bool successFlag, String reason, UInt64 commitID)
        {
            // Join with the upload batch thread.
            Debug.Assert(TransferThread != null);
            TransferThread = null;

            // The transfer batch failed.
            if (!successFlag)
            {
                // Cancel the upload of all files and do a full run to clear the
                // uploaded files.
                CancelOnError(reason);
                Share.Pipeline.Run("file upload batch failed", true);
            }

            // The transfer batch succeeded.
            else
            {
                // We already got the server event. Call the handler function.
                if (CommitIDSet.Contains(commitID))
                    OnCommitIDReceived();

                // We're now waiting for the phase 2 event.
                else
                {
                    Status = UploadManagerStatus.Phase2;
                    WantedCommitID = commitID;
                }
            }

            // Delete the files in the upload directory if all the files have
            // been uploaded.
            if (OrderTree.Count == DoneTree.Count) DeleteUploadedFiles();
        }

        /// <summary>
        /// Remove the specified upload.
        /// </summary>
        private void RemoveUpload(KfsFileUpload f)
        {
            OrderTree.Remove(f.OrderID);
            if (DoneTree.ContainsKey(f.OrderID)) DoneTree.Remove(f.OrderID);
            Share.MetaDataManager.RemoveUpload(f.OrderID);
            Share.UnregisterFileTransfer(f);
        }

        /// <summary>
        /// Obtain the server-side inode and path used to track the file
        /// specified. trackedInode is set to the deepest server-side directory
        /// that contains this file and trackedPath is set to the relative path
        /// to the file from this directory.
        /// </summary>
        private void GetTrackingInodeAndPath(String path, out UInt64 trackedInode, out string trackedPath)
        {
            String[] c = KfsPath.SplitRelativePath(path);
            KfsServerDirectory cur = Share.ServerView.Root;

            for (int i = 0; i < c.Length - 1; i++)
            {
                KfsServerDirectory dir = cur.GetChildDir(c[i]);

                if (dir != null)
                {
                    cur = dir;
                }
                
                else
                {
                    trackedInode = cur.Inode;
                    trackedPath = "";

                    for (int j = i; j < c.Length; j++)
                    {
                        if (j != i) trackedPath += "/";
                        trackedPath += c[j];
                    }

                    return;
                }
            }

            trackedInode = cur.Inode;
            trackedPath = c[c.Length - 1];
        }

        /// <summary>
        /// Return a path to upload the file specified.
        /// </summary>
        private String GetTemporaryPath(UInt64 orderID)
        {
            return Share.UploadDirPath + "file_" + orderID;
        }

        /// <summary>
        /// Delete all previously uploaded files.
        /// </summary>
        private void DeleteUploadedFiles()
        {
            foreach (FileInfo f in (new DirectoryInfo(Share.UploadDirPath)).GetFiles())
            {
                try
                {
                    f.Attributes = FileAttributes.Normal;
                    f.Delete();
                }
                catch (Exception ex)
                {
                    Logging.LogException(ex);
                }
            }
        }

        /// <summary>
        /// This method is called when the wanted commit ID has been received.
        /// </summary>
        private void OnCommitIDReceived()
        {
            // Go idle and do a full run to force the event to be applied. Note, this may
            // be a re-entrent call depending on where we are called. We don't care.
            GoIdle();
            Share.Pipeline.Run("wanted upload commit ID received", true);
        }

        /// <summary>
        /// Cancel all the queued and batched uploads when an error occurs and
        /// go idle. 'reason' is null if the thread has been cancelled.
        /// </summary>
        private void CancelOnError(String reason)
        {
            SortedDictionary<UInt64, KfsFileUpload> tree = new SortedDictionary<UInt64, KfsFileUpload>(OrderTree);
            foreach (KfsFileUpload f in tree.Values)
            {
                if (f.Status == FileTransferStatus.Queued || f.Status == FileTransferStatus.Batched)
                {
                    ReportError(reason, f);
                    RemoveUpload(f);
                }
            }

            GoIdle();
        }

        /// <summary>
        /// Tidy-up the state after an upload batch has been completed or an
        /// error occured.
        /// </summary>
        private void GoIdle()
        {
            Status = UploadManagerStatus.Idle;
            CommitIDSet.Clear();
            WantedCommitID = 0;
        }
    }

    /// <summary>
    /// Represent the upload of a file.
    /// </summary>
    public class KfsFileUpload : KfsFileTransfer
    {
        // Since non-added local files do not always have an associated inode,
        // we cannot track file uploads by inode. Therefore, we track file
        // uploads by path. This is inherently dangerous since the path is
        // liable to change if one of the containing directories or the file
        // itself is moved or deleted. As a best effort, we keep track of the
        // inode of the deepest server directory that contains our file, or
        // the inode of the server file itself, and the path from there to our
        // file. Since the tracked inode can be deleted under our feet, we
        // also keep the full path we have last observed as a fallback. We use
        // the order ID as the unique identifier for this object.

        /// <summary>
        /// Tracked inode. This is 0 for the root of the share.
        /// </summary>
        public UInt64 TrackedInode;

        /// <summary>
        /// Relative path from the tracked inode. This is "" if the tracked inode is
        /// the uploaded file itself.
        /// </summary>
        public String TrackedPath;

        /// <summary>
        /// Path to the file in the upload directory.
        /// </summary>
        public String UploadPath;

        /// <summary>
        /// Size of the file uploaded.
        /// </summary>
        public UInt64 Size;

        /// <summary>
        /// Hash of the file.
        /// </summary>
        public byte[] Hash;

        /// <summary>
        /// This field is 0 if the file is being created. Otherwise, this field is
        /// set to the commit ID of the file being updated.
        /// </summary>
        public UInt64 UpdateCommitID;

        public KfsFileUpload(KfsShare s, UInt64 orderID, UInt64 trackedInode, String trackedPath,
                             String lastFullPath, String uploadPath, 
                             UInt64 size, byte[] hash, UInt64 updateCommitID)
            : base(s, orderID, lastFullPath)
        {
            TrackedInode = trackedInode;
            TrackedPath = trackedPath;
            UploadPath = uploadPath;
            Hash = hash;
            Size = size;
            UpdateCommitID = updateCommitID;
        }

        /// <summary>
        /// Update the full path using the information obtained from the tracked
        /// inode.
        /// </summary>
        public void UpdateLastFullPath()
        {
            // We never cancel a batched upload even if it seems to have
            // disappeared. In the case of the upload of a new file, the file may
            // simply have been moved around. In any case, it is best to let the
            // user finish his upload so that he may retrieve his file even if it
            // was deleted on the server while it was being uploaded. In the case
            // of a queued file, the situation may change until the file gets
            // batched. For simplicity, we don't try to be too clever here and we
            // let the server tell us whether we are attempting an invalid upload.
            KfsServerObject o = Share.ServerView.GetObjectByInode(TrackedInode);

            if (o != null)
            {
                String newPath = o.RelativePath;
                if (TrackedPath != "" && newPath != "") newPath += "/";

                newPath += TrackedPath;

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
    /// Thread that uploads a batch of files.
    /// </summary>
    public class KfsUploadThread : KfsTransferThread
    {
        /// <summary>
        /// Tree of batched files indexed by order ID.
        /// </summary>
        private SortedDictionary<UInt64, KfsUploadBatchFile> OrderTree;

        /// <summary>
        /// Array of batched files.
        /// </summary>
        private KfsUploadBatchFile[] FileArray;

        /// <summary>
        /// Phase 1 payload that must be executed to start the upload. The payload
        /// contains one operation for each file in the OrderTree, in the same order.
        /// </summary>
        private KfsPhase1Payload Phase1Payload;

        /// <summary>
        /// Commit ID received from the server.
        /// </summary>
        private UInt64 CommitID;

        /// <summary>
        /// True if there are files to transfer.
        /// </summary>
        private bool HaveUploadFlag = false;

        // Index in the file array corresponding to the file currently being
        // uploaded.
        private int UploadIndex;

        /// <summary>
        /// File currently being uploaded.
        /// </summary>
        private FileStream UploadedFile;

        /// <summary>
        /// Size of the remaining data to read in the uploaded file. 
        /// </summary>
        private Int64 RemainingSize;

        // Preferred maximum size of an upload message.
        private const UInt32 MAX_UPLOAD_SIZE = (256 * 1024);

        // Preferred minimum size of an upload chunk.
        private const UInt32 MIN_UPLOAD_CHUNK_SIZE = (64 * 1024);

        public KfsUploadThread(KfsShare share, byte[] ticket,
                               SortedDictionary<UInt64, KfsUploadBatchFile> orderTree,
                               KfsPhase1Payload phase1Payload)
            : base(share, ticket)
        {
            Debug.Assert(orderTree.Count > 0);
            OrderTree = orderTree;
            Phase1Payload = phase1Payload;
            FileArray = new KfsUploadBatchFile[OrderTree.Count];
            OrderTree.Values.CopyTo(FileArray, 0);
        }

        /// <summary>
        /// Called to cancel the upload of the file specified.
        /// </summary>
        public void CancelUploadHandler(UInt64 OrderID)
        {
            KfsUploadBatchFile f = OrderTree[OrderID];
            if (f.Status == BatchStatus.Queued || f.Status == BatchStatus.Started)
            {
                f.Status = BatchStatus.Cancelled;
                PostToUI(new UiUploadCancelledMsg(Share, OrderID));
            }
        }

        /// <summary>
        /// Perform phase 1.
        /// </summary>
        private void PerformPhase1()
        {
            // Send the phase 1 message.
            AnpMsg m = SendPhase1Message(Phase1Payload);

            // Get the commit ID.
            CommitID = m.Elements[0].UInt64;

            // Analyze the reply.
            int pos = 2;

            for (UInt32 i = 0; i < m.Elements[1].UInt32; i++, pos += 2)
            {
                // The server refused the upload. Notify the UI.
                if (m.Elements[pos].UInt32 == 0)
                {
                    FileArray[i].Status = BatchStatus.Error;
                    String reason = "server refused upload (" + m.Elements[pos + 1].String + ")";
                    PostToUI(new UiUploadCompletedMsg(Share, FileArray[i].OrderID, false, reason));
                }

                // The server accepted at least one upload.
                else HaveUploadFlag = true;
            }
        }

        /// <summary>
        /// Perform phase 2.
        /// </summary>
        private void PerformPhase2()
        {
            // Set the upload index to -1 and pass to the next file.
            UploadIndex = -1;
            UploadNextFile();
            AnpMsg m;

            // Loop until we're done.
            while (UploadIndex != FileArray.Length)
            {
                // Create the next message.
                List<UInt64> CompletedArray;
                CreateUploadMsg(out m, out CompletedArray);

                // The message is non-empty.
                if (m != null)
                {
                    // Send the message.
                    SendAnpMsg(m);

                    // Receive the confirmation.
                    m = GetAnpMsg();
                    if (m.Type == KAnpType.KANP_RES_FAIL) throw new Exception(m.Elements[1].String);
                    if (m.Type != KAnpType.KANP_RES_OK) throw new Exception("expected RES_OK in phase 2");
                }

                // Send completion notifications to the UI.
                foreach (UInt64 orderID in CompletedArray)
                    PostToUI(new UiUploadCompletedMsg(Share, orderID, true, null));

                // Send a progression message, if required.
                HandleProgression();
            }

            // Get the confirmation.
            m = GetAnpMsg();
            if (m.Type == KAnpType.KANP_RES_FAIL) throw new Exception(m.Elements[1].String);
            if (m.Type != KAnpType.KANP_RES_OK) throw new Exception("expected RES_OK in phase 2");
        }

        /// <summary>
        /// Create the next upload message.
        /// </summary>
        private void CreateUploadMsg(out AnpMsg m, out List<UInt64> CompletedArray)
        {
            m = Share.CreateTransferMsg(KAnpType.KANP_CMD_KFS_PHASE_2);
            CompletedArray = new List<UInt64>();

            // Add the number of submessages field. It will be updated later.
            m.AddUInt32(0);

            // Count the number of submessages.
            int nbSub = 0;

            // Loop until the message is full or we run out of files to upload.
            while (m.PayloadSize() < MAX_UPLOAD_SIZE && UploadIndex != FileArray.Length)
            {
                KfsUploadBatchFile ubf = FileArray[UploadIndex];

                // The transfer of the current file was denied by the
                // server during phase 1. Do not attempt to talk about
                // it in phase 2.
                if (ubf.Status == BatchStatus.Error)
                {
                    UploadNextFile();
                    continue;
                }

                // The transfer of the current file has been cancelled. Add an 
                // abort submessage and pass to the next file.
                if (ubf.Status == BatchStatus.Cancelled)
                {
                    m.AddUInt32(2);
                    m.AddUInt32(KAnpType.KANP_KFS_SUBMESSAGE_ABORT);
                    nbSub++;
                    UploadNextFile();
                    continue;
                }

                // The current file is closed. Open the file and set the
                // remaining size.
                if (UploadedFile == null)
                {
                    Debug.Assert(ubf.Status == BatchStatus.Queued);
                    ubf.Status = BatchStatus.Started;
                    UploadedFile = new FileStream(ubf.TransferPath, FileMode.Open, FileAccess.Read);
                    RemainingSize = UploadedFile.Length;
                }

                // Add a chunk submessage.
                if (RemainingSize > 0)
                {
                    Debug.Assert(ubf.Status == BatchStatus.Started);

                    // Compute the chunk size.
                    UInt32 chunkSize = Math.Max(MIN_UPLOAD_CHUNK_SIZE, MAX_UPLOAD_SIZE - m.PayloadSize());
                    chunkSize = (UInt32)Math.Min((Int64)chunkSize, RemainingSize);
                    RemainingSize -= chunkSize;

                    // Read the chunk.
                    byte[] chunkData = new byte[chunkSize];
                    UploadedFile.Read(chunkData, 0, (Int32)chunkSize);

                    // Add the chunk submessage.
                    m.AddUInt32(3);
                    m.AddUInt32(KAnpType.KANP_KFS_SUBMESSAGE_CHUNK);
                    m.AddBin(chunkData);
                    nbSub++;
                }

                // Add a commit submessage, remember that the transfer of the file
                // is being completed in this message and pass to the next file.
                if (RemainingSize == 0)
                {
                    Debug.Assert(ubf.Status == BatchStatus.Started);
                    ubf.Status = BatchStatus.Done;

                    m.AddUInt32(3);
                    m.AddUInt32(KAnpType.KANP_KFS_SUBMESSAGE_COMMIT);
                    m.AddBin(ubf.Hash);
                    nbSub++;

                    CompletedArray.Add(ubf.OrderID);
                    UploadNextFile();
                }
            }

            // Update the number of submessages.
            m.Elements[0].UInt32 = (UInt32)nbSub;

            // If there are no submessages, don't bother sending the message.
            if (nbSub == 0) m = null;
        }

        /// <summary>
        /// Send a progression message, if required.
        /// </summary>
        private void HandleProgression()
        {
            if (UploadIndex == FileArray.Length) return;
            KfsUploadBatchFile ubf = FileArray[UploadIndex];
            if (ubf.Status != BatchStatus.Started) return;
            UInt64 bytesTransferred = (UInt64)UploadedFile.Length - (UInt64)RemainingSize;
            PostToUI(new UiUploadProgressionMsg(Share, ubf.OrderID, bytesTransferred));
        }

        /// <summary>
        /// Pass to the next file to upload, if there is one.
        /// </summary>
        private void UploadNextFile()
        {
            Debug.Assert(UploadIndex < FileArray.Length);
            CloseUploadedFile();

            while (++UploadIndex < FileArray.Length)
            {
                KfsUploadBatchFile ubf = FileArray[UploadIndex];

                // The transfer of this file was accepted by the server.
                if (ubf.Status != BatchStatus.Done) break;
            }
        }

        /// <summary>
        /// Close the current uploaded file if it is open.
        /// </summary>
        private void CloseUploadedFile()
        {
            try
            {
                if (UploadedFile != null)
                {
                    UploadedFile.Close();
                    UploadedFile = null;
                }
            }

            catch (Exception) { }
        }

        protected override void OnTunnelConnected()
        {
            // Negociate the role.
            NegociateRole();

            // Perform phase 1.
            PerformPhase1();

            // Perform phase 2 if we have files to upload.
            if (HaveUploadFlag) PerformPhase2();
        }

        protected override void OnCompletion()
        {
            base.OnCompletion();

            CloseUploadedFile();

            if (Status == WorkerStatus.Cancelled)
                Share.UploadManager.OnBatchCompleted(false, null, 0);

            else if (Status == WorkerStatus.Failed)
                Share.UploadManager.OnBatchCompleted(false, FailException.Message, 0);

            else if (Status == WorkerStatus.Success && HaveUploadFlag)
                Share.UploadManager.OnBatchCompleted(true, null, CommitID);
            else if (Status == WorkerStatus.Success)
                Share.UploadManager.OnBatchCompleted(false, "No file could be uploaded", 0);
            else Debug.Assert(false);
        }
    }

    /// <summary>
    /// Represent a file in an upload batch.
    /// </summary>
    public class KfsUploadBatchFile : KfsTransferBatchFile
    {
        /// <summary>
        /// Hash of the file.
        /// </summary>
        public byte[] Hash;

        public KfsUploadBatchFile(UInt64 orderID, String transferPath, byte[] hash)
            : base(orderID, transferPath)
        {
            Hash = hash;
        }
    }

    /// <summary>
    /// Represent a message sent to an upload thread.
    /// </summary>
    public abstract class UploadTransferMsg : WorkerThreadMsg
    {
        /// <summary>
        /// Reference to the upload thread.
        /// </summary>
        public KfsUploadThread UploadThread;

        public UploadTransferMsg(KfsUploadThread uploadThread)
        {
            UploadThread = uploadThread;
        }
    }

    /// <summary>
    /// Request a file upload to be cancelled.
    /// </summary>
    public class UploadCancelMsg : UploadTransferMsg
    {
        UInt64 OrderID;

        public UploadCancelMsg(KfsUploadThread uploadThread, UInt64 orderID)
            : base(uploadThread)
        {
            OrderID = orderID;
        }

        public override void Run()
        {
            UploadThread.CancelUploadHandler(OrderID);
        }
    }

    /// <summary>
    /// Message sent when a file upload is cancelled.
    /// </summary>
    public class UiUploadCancelledMsg : UiTransferMsg
    {
        UInt64 OrderID;

        public UiUploadCancelledMsg(KfsShare share, UInt64 orderID)
            : base(share)
        {
            OrderID = orderID;
        }

        public override void Run()
        {
            Share.UploadManager.OnFileUploadCancelled(OrderID);
        }
    }

    /// <summary>
    /// Message sent when the upload of a file has been completed.
    /// </summary>
    public class UiUploadCompletedMsg : UiTransferMsg
    {
        UInt64 OrderID;
        bool SuccessFlag;
        String Reason;

        public UiUploadCompletedMsg(KfsShare share, UInt64 orderID, bool successFlag, String reason)
            : base(share)
        {
            OrderID = orderID;
            SuccessFlag = successFlag;
            Reason = reason;
        }

        public override void Run()
        {
            Share.UploadManager.OnFileUploadCompleted(OrderID, SuccessFlag, Reason);
        }
    }

    /// <summary>
    /// Message sent when the upload of a file has progressed.
    /// </summary>
    public class UiUploadProgressionMsg : UiTransferMsg
    {
        UInt64 OrderID;
        UInt64 BytesTransferred;

        public UiUploadProgressionMsg(KfsShare share, UInt64 orderID, UInt64 bytesTransferred)
            : base(share)
        {
            OrderID = orderID;
            BytesTransferred = bytesTransferred;
        }

        public override void Run()
        {
            Share.UploadManager.OnFileUploadProgression(OrderID, BytesTransferred);
        }
    }
}