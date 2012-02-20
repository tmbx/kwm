using kwm.Utils;
using System;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum TransferErrorType
    {
        /// <summary>
        /// A file could not be downloaded.
        /// </summary>
        Download,

        /// <summary>
        /// A file could not be uploaded.
        /// </summary>
        Upload,

        /// <summary>
        /// A directory could not be created.
        /// </summary>
        Mkdir,

        /// <summary>
        /// Some files or directories could not be deleted.
        /// </summary>
        Delete,

        /// <summary>
        /// Some files or directories could not be moved.
        /// </summary>
        Move,

        /// <summary>
        /// Some directories could not be created or some ghosts could not be
        /// deleted. This is used when files are added and there are missing
        /// server directories or ghosts in the way.
        /// </summary>
        Cleanup
    }

    public enum FileTransferStatus
    {
        /// <summary>
        /// The file has been queued for transfer, but it is not part of the
        /// current transfer batch.
        /// </summary>
        [Base.Description("Waiting...")]
        Queued,

        /// <summary>
        /// The file is present in the current transfer batch.
        /// </summary>
        [Base.Description("In progress")]
        Batched,

        /// <summary>
        /// The file has been transferred.
        /// </summary>
        [Base.Description("Done")]
        Transferred
    }

    public enum BatchStatus
    {
        /// <summary>
        /// The file has not been transferred yet.
        /// </summary>
        Queued,

        /// <summary>
        /// The file is being transferred.
        /// </summary>
        Started,

        /// <summary>
        /// The file has been transferred.
        /// </summary>
        Done,

        /// <summary>
        /// The transfer has been cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The server refused the file.
        /// </summary>
        Error
    }

    // Represent a transfer error.
    public class KfsTransferError
    {
        /// <summary>
        /// Error type.
        /// </summary>
        public TransferErrorType Type;

        /// <summary>
        /// Failed transfer, in the case of a download / upload.
        /// </summary>
        public KfsFileTransfer FileTransfer;

        /// <summary>
        /// String describing the error.
        /// </summary>
        public String Reason;

        public KfsTransferError(TransferErrorType type, KfsFileTransfer fileTransfer, String reason)
        {
            Type = type;
            FileTransfer = fileTransfer;
            Reason = reason;
        }
    }

    /// <summary>
    /// Represent the transfer of a file (download or upload).
    /// </summary>
    public class KfsFileTransfer
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        /// <summary>
        /// Unique ID associated to the transfer.
        /// </summary>
        public UInt64 OrderID;

        /// <summary>
        /// Status the transfer.
        /// </summary>
        public FileTransferStatus Status = FileTransferStatus.Queued;

        /// <summary>
        /// Number of bytes transferred.
        /// </summary>
        public UInt64 BytesTransferred = 0;

        /// <summary>
        /// Last observed full path.
        /// </summary>
        public String LastFullPath;

        /// <summary>
        /// True if we have requested cancellation to the transfer thread.
        /// </summary>
        public bool CancelFlag = false;

        /// <summary>
        /// Transfer error object. Null if no error.
        /// </summary>
        public KfsTransferError Error = null;

        public bool InError
        {
            get { return (Error != null); }
        }

        public KfsFileTransfer(KfsShare s, UInt64 orderID, String lastFullPath)
        {
            Share = s;
            OrderID = orderID;
            LastFullPath = lastFullPath;
        }
    }

    /// <summary>
    /// Thread that transfers a batch of files or performs other meta-data
    /// operations.
    /// </summary>
    public abstract class KfsTransferThread : KwmTunnelThread
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        protected KfsShare Share;

        /// <summary>
        /// Ticket for the transfer.
        /// </summary>
        protected byte[] Ticket;

        public KfsTransferThread(KfsShare share, byte[] ticket)
            : base(share.App.Helper, null, 0)
        {
            Share = share;
            Ticket = ticket;
        }

        /// <summary>
        /// Negociate the file transfer role.
        /// </summary>
        protected void NegociateRole()
        {
            AnpMsg m = Share.CreateTransferMsg(KAnpType.KANP_CMD_MGT_SELECT_ROLE);
            m.AddUInt32(KAnpType.KANP_KCD_ROLE_FILE_XFER);
            SendAnpMsg(m);
            m = GetAnpMsg();
            if (m.Type == KAnpType.KANP_RES_FAIL) throw new Exception(m.Elements[1].String);
            if (m.Type != KAnpType.KANP_RES_OK) throw new Exception("expected RES_OK in role negociation");
        }

        /// <summary>
        /// Send a phase 1 message to the KCD and retrieve the reply.
        /// </summary>
        protected AnpMsg SendPhase1Message(KfsPhase1Payload payload)
        {
            AnpMsg m = Share.CreateTransferMsg(KAnpType.KANP_CMD_KFS_PHASE_1);
            m.AddBin(Ticket);
            m.AddUInt64(0);
            payload.AddToMsg(m);
            SendAnpMsg(m);
            m = GetAnpMsg();
            if (m.Type == KAnpType.KANP_RES_FAIL) throw new Exception(m.Elements[1].String);
            if (m.Type != KAnpType.KANP_RES_KFS_PHASE_1) throw new Exception("expected RES_KFS_PHASE_1");
            return m;
        }

        protected override void OnCompletion()
        {
            base.OnCompletion();
        }
    }

    /// <summary>
    /// Represent a file in a transfer batch.
    /// </summary>
    public class KfsTransferBatchFile
    {
        /// <summary>
        /// Same as in KfsFileTransfer.
        /// </summary>
        public UInt64 OrderID;

        /// <summary>
        /// Path to the file in the transfer directory.
        /// </summary>
        public String TransferPath;

        // Transfer status.
        public BatchStatus Status = BatchStatus.Queued;

        public KfsTransferBatchFile(UInt64 orderID, String transferPath)
        {
            OrderID = orderID;
            TransferPath = transferPath;
        }
    }

    /// <summary>
    /// Represent a message sent by a transfer thread.
    /// </summary>
    public abstract class UiTransferMsg : UiThreadMsg
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        public UiTransferMsg(KfsShare share)
        {
            Share = share;
        }
    }
}