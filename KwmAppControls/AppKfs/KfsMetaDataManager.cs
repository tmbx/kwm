using kwm.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Iesi.Collections.Generic;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum MetaDataTask
    {
        /// <summary>
        /// No operation in progress.
        /// </summary>
        None,

        /// <summary>
        /// Create directories.
        /// </summary>
        Mkdir,

        /// <summary>
        /// Delete files.
        /// </summary>
        Delete,

        /// <summary>
        /// Move files.
        /// </summary>
        Move,

        /// <summary>
        /// Make missing directories and/or delete ghosts.
        /// </summary>
        Cleanup
    }

    public enum MetaDataManagerStatus
    {
        /// <summary>
        /// No operation in progress.
        /// </summary>
        Idle,

        /// <summary>
        /// Operation queued.
        /// </summary>
        Queued,

        /// <summary>
        /// Waiting for upload ticket.
        /// </summary>
        Ticket,

        /// <summary>
        /// Executing operation.
        /// </summary>
        Exec,

        /// <summary>
        /// Waiting for phase 1 event.
        /// </summary>
        Phase1
    }

    /// <summary>
    /// Logic to manage the meta-data operations, i.e. create directory, delete
    /// file/directory, move file/directory and clean-up things.
    /// </summary>
    public class KfsMetaDataManager
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Meta-data thread.
        /// </summary>
        public KfsMetaDataThread TransferThread;

        /// <summary>
        /// Phase 1 payload that must be executed.
        /// </summary>
        public KfsPhase1Payload Phase1Payload;

        /// <summary>
        /// Tree of file uploads tied to the phase 1 payload indexed by order ID.
        /// </summary>
        public SortedDictionary<UInt64, KfsFileUpload> UploadTree;

        /// <summary>
        /// Current upload ticket, if any.
        /// </summary>
        public byte[] Ticket;

        /// <summary>
        /// Description of what we are trying to do with the meta-data operations.
        /// </summary>
        public MetaDataTask Task = MetaDataTask.None;

        /// <summary>
        /// Operation status.
        /// </summary>
        public MetaDataManagerStatus Status = MetaDataManagerStatus.Idle;

        /// <summary>
        /// If the status is "exec" or "phase1", then we collect all phase 1 commit IDs
        /// because we need to know when our changes have been applied on the UI view.
        /// </summary>
        public SortedSet<UInt64> CommitIDSet = new SortedSet<UInt64>();

        /// <summary>
        /// Commit ID we are waiting for.
        /// </summary>
        public UInt64 WantedCommitID;

        public KfsMetaDataManager(KfsShare s)
        {
            Share = s;
            Share.MetaDataManager = this;
        }

        /// <summary>
        /// Queue a meta-data operation for execution.
        /// </summary>
        public void QueueOperation(KfsPhase1Payload payload, SortedDictionary<UInt64, KfsFileUpload> uploadTree,
                                   MetaDataTask task)
        {
            Debug.Assert(Status == MetaDataManagerStatus.Idle);
            Phase1Payload = payload;
            if (uploadTree == null) UploadTree = new SortedDictionary<UInt64, KfsFileUpload>();
            else UploadTree = uploadTree;
            Task = task;
            Status = MetaDataManagerStatus.Queued;
        }

        /// <summary>
        /// Ask an upload ticket.
        /// </summary>
        public void AskTicket()
        {
            Debug.Assert(Status == MetaDataManagerStatus.Queued);
            Debug.Assert(Ticket == null);
            Share.AskUploadTicket();
            Status = MetaDataManagerStatus.Ticket;
        }

        /// <summary>
        /// Begin executing the operation.
        /// </summary>
        public void StartOperation()
        {
            Debug.Assert(Status == MetaDataManagerStatus.Queued);
            Debug.Assert(TransferThread == null);
            Debug.Assert(Phase1Payload != null);
            Debug.Assert(Ticket != null);

            try
            {
                // Start the transfer.
                TransferThread = new KfsMetaDataThread(Share, Ticket, Phase1Payload);
                TransferThread.Start();

                Status = MetaDataManagerStatus.Exec;
                Phase1Payload = null;
                Ticket = null;
            }

            catch (Exception ex)
            {
                Share.FatalError(ex);
            }
        }

        /// <summary>
        /// Stop the meta data manager if it is operating. The pipeline is not
        /// notified.
        /// </summary>
        public void CancelOperation()
        {
            if (Status == MetaDataManagerStatus.Ticket ||
                Status == MetaDataManagerStatus.Queued ||
                Status == MetaDataManagerStatus.Phase1) GoIdle();
            else if (TransferThread != null) TransferThread.RequestCancellation();
        }

        /// <summary>
        /// Remove the upload specified from the upload tree if it exists.
        /// </summary>
        public void RemoveUpload(UInt64 orderID)
        {
            if (UploadTree != null && UploadTree.ContainsKey(orderID)) UploadTree.Remove(orderID);
        }

        /// <summary>
        /// This method is called when a upload ticket reply is received.
        /// </summary>
        public void OnTicket(AnpMsg m)
        {
            Debug.Assert(Status == MetaDataManagerStatus.Ticket);
            Debug.Assert(Phase1Payload != null);
            Debug.Assert(Ticket == null);

            // Store the ticket and run the pipeline.
            if (m.Type == KAnpType.KANP_RES_KFS_UPLOAD_REQ)
            {
                Status = MetaDataManagerStatus.Queued;
                Ticket = m.Elements[0].Bin;
                Share.Pipeline.Run("got meta-data ticket", false);
            }

            // On failure, cancel all uploads.
            else
            {
                ReportError(m.Elements[0].String);
                Share.Pipeline.Run("could not obtain meta-data ticket", true);
            }
        }

        /// <summary>
        /// This method is called when a phase 1 server event is received.
        /// </summary>
        public void OnPhase1Event(UInt64 CommitID)
        {
            // We're interested in the commit ID.
            if (Status == MetaDataManagerStatus.Exec)
                CommitIDSet.Add(CommitID);

            else if (Status == MetaDataManagerStatus.Phase1 && CommitID == WantedCommitID)
                OnCommitIDReceived();
        }

        /// <summary>
        /// This method is called when the operation has been completed.
        /// 'reason' is null if the thread has been cancelled.
        /// </summary>
        public void OnCompletion(bool successFlag, String reason, UInt64 commitID)
        {
            // Join with the transfer thread.
            Debug.Assert(TransferThread != null);
            TransferThread = null;

            // The operation failed.
            if (!successFlag)
            {
                ReportError(reason);
                Share.Pipeline.Run("meta-data execution failed", true);
            }

            // The operation succeeded.
            else
            {
                // We already got the server event. Call the handler function.
                if (CommitIDSet.Contains(commitID))
                    OnCommitIDReceived();

                // We're now waiting for the phase 1 event.
                else
                {
                    Status = MetaDataManagerStatus.Phase1;
                    WantedCommitID = commitID;
                }
            }
        }

        /// <summary>
        /// This method is called when the wanted commit ID has been received.
        /// </summary>
        private void OnCommitIDReceived()
        {
            // Go idle, then do a full run to force the UI view to be refreshed.
            GoIdle();
            Share.Pipeline.Run("wanted meta-data commit ID received", true);
        }

        /// <summary>
        /// This method is called when an error occurs with the meta-data
        /// operation. 'reason' is null if the thread has been cancelled.
        /// </summary>
        private void ReportError(String reason)
        {
            // Cancel all tied uploads.
            SortedDictionary<UInt64, KfsFileUpload> tree = new SortedDictionary<UInt64, KfsFileUpload>(UploadTree);
            foreach (KfsFileUpload f in tree.Values)
            {
                Debug.Assert(f.Status == FileTransferStatus.Queued);
                Share.UploadManager.CancelUpload(f.OrderID);
            }

            // Remember the failure information.
            TransferErrorType errorType = TransferErrorType.Mkdir;
            if (Task == MetaDataTask.Mkdir) errorType = TransferErrorType.Mkdir;
            else if (Task == MetaDataTask.Delete) errorType = TransferErrorType.Delete;
            else if (Task == MetaDataTask.Move) errorType = TransferErrorType.Move;
            else if (Task == MetaDataTask.Cleanup) errorType = TransferErrorType.Cleanup;
            else Debug.Assert(false);

            // Go idle.
            GoIdle();

            // Report the error to the user.
            if (reason != null) Share.OnTransferError(new KfsTransferError(errorType, null, reason));
        }

        /// <summary>
        /// Tidy-up the state after a transfer has been completed or when an
        /// error has occurred.
        /// </summary>
        private void GoIdle()
        {
            Phase1Payload = null;
            UploadTree = null;
            Task = MetaDataTask.None;
            Status = MetaDataManagerStatus.Idle;
            CommitIDSet.Clear();
            WantedCommitID = 0;
        }
    }

    /// <summary>
    /// Thread that performs a meta-data operation.
    /// </summary>
    public class KfsMetaDataThread : KfsTransferThread
    {
        /// <summary>
        /// Phase 1 payload that must be executed.
        /// </summary>
        private KfsPhase1Payload Phase1Payload;

        /// <summary>
        /// Commit ID received from the server.
        /// </summary>
        private UInt64 CommitID;

        public KfsMetaDataThread(KfsShare s, byte[] ticket, KfsPhase1Payload payload)
            : base(s, ticket)
        {
            Phase1Payload = payload;
        }

        protected override void OnTunnelConnected()
        {
            // Negociate the role.
            NegociateRole();

            // Send the phase 1 message.
            AnpMsg m = SendPhase1Message(Phase1Payload);

            // Get the commit ID.
            CommitID = m.Elements[0].UInt64;

            // Analyze the reply.
            int pos = 2;

            for (UInt32 i = 0; i < m.Elements[1].UInt32; i++, pos += 2)
            {
                // The server refused the operation.
                if (m.Elements[pos].UInt32 == 0)
                {
                    String reason = "operation denied (" + m.Elements[pos + 1].String + ")";
                    throw new Exception(reason);
                }
            }
        }

        protected override void OnCompletion()
        {
            if (Status == WorkerStatus.Cancelled)
                Share.MetaDataManager.OnCompletion(false, null, 0);

            else if (Status == WorkerStatus.Failed)
                Share.MetaDataManager.OnCompletion(false, FailException.Message, 0);

            else if (Status == WorkerStatus.Success)
                Share.MetaDataManager.OnCompletion(true, null, CommitID);

            else Debug.Assert(false);
        }
    }
}