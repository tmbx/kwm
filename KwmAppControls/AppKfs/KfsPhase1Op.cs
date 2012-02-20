using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// Represent the payload (list of operations) of a phase 1 command.
    /// </summary>
    public class KfsPhase1Payload
    {
        /// <summary>
        /// List of operations present in this message, in order.
        /// </summary>
        public List<KfsPhase1Op> OpList = new List<KfsPhase1Op>();

        /// <summary>
        /// Add a create operation to the list.
        /// </summary>
        public void AddCreateOp(bool IsFile, UInt64 ParentInode, UInt64 ParentCommitID, String Path)
        {
            KfsCreatePhase1Op O = new KfsCreatePhase1Op();
            O.IsFile = IsFile;
            O.ParentInode = ParentInode;
            O.ParentCommitID = ParentCommitID;
            O.Path = Path;
            AddOp(O);
        }

        /// <summary>
        /// Add an update operation to the list.
        /// </summary>
        public void AddUpdateOp(UInt64 Inode, UInt64 CommitID)
        {
            KfsUpdatePhase1Op O = new KfsUpdatePhase1Op();
            O.Inode = Inode;
            O.CommitID = CommitID;
            AddOp(O);
        }

        /// <summary>
        /// Add a delete operation to the list.
        /// </summary>
        public void AddDeleteOp(bool IsFile, UInt64 Inode, UInt64 CommitID)
        {
            KfsDeletePhase1Op O = new KfsDeletePhase1Op();
            O.IsFile = IsFile;
            O.Inode = Inode;
            O.CommitID = CommitID;
            AddOp(O);
        }

        /// <summary>
        /// Add a move operation to the list.
        /// </summary>
        public void AddMoveOp(bool IsFile, UInt64 MovedInode, UInt64 MovedCommitID,
                              UInt64 ParentInode, UInt64 ParentCommitID, String Path)
        {
            KfsMovePhase1Op O = new KfsMovePhase1Op();
            O.IsFile = IsFile;
            O.MovedInode = MovedInode;
            O.MovedCommitID = MovedCommitID;
            O.ParentInode = ParentInode;
            O.ParentInode = ParentInode;
            O.ParentCommitID = ParentCommitID;
            O.Path = Path;
            AddOp(O);
        }

        /// <summary>
        /// Add the operations of the payload to the ANP message specified.
        /// </summary>
        public void AddToMsg(AnpMsg M)
        {
            M.AddUInt32((UInt32)OpList.Count);
            foreach (KfsPhase1Op F in OpList) F.AddToMsg(M);
        }

        /// <summary>
        /// Add an operation to the operation list. The operation is not added
        /// if it is already present in the list.
        /// </summary>
        private void AddOp(KfsPhase1Op O)
        {
            foreach (KfsPhase1Op F in OpList) if (F.Equals(O)) return;
            OpList.Add(O);
        }
    }

    /// <summary>
    /// Represent an operation in a phase 1 command.
    /// </summary>
    public abstract class KfsPhase1Op
    {
        /// <summary>
        /// Add this operation to the ANP message specified.
        /// </summary>
        public abstract void AddToMsg(AnpMsg M);
    }

    /// <summary>
    /// Represent a create file/directory operation in a phase 1 command.
    /// </summary>
    public class KfsCreatePhase1Op : KfsPhase1Op
    {
        public bool IsFile;
        public UInt64 ParentInode;
        public UInt64 ParentCommitID;
        public String Path;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is KfsCreatePhase1Op)
            {
                KfsCreatePhase1Op O = obj as KfsCreatePhase1Op;
                return (IsFile == O.IsFile &&
                        ParentInode == O.ParentInode &&
                        ParentCommitID == O.ParentCommitID &&
                        Path == O.Path);
            }

            return false;
        }

        public override void AddToMsg(AnpMsg M)
        {
            M.AddUInt32(5);
            M.AddUInt32(IsFile ? KAnpType.KANP_KFS_OP_CREATE_FILE : KAnpType.KANP_KFS_OP_CREATE_DIR);
            M.AddUInt64(ParentInode);
            M.AddUInt64(ParentCommitID);
            M.AddString(Path);
        }
    }
    
    /// <summary>
    /// Represent an update file operation in a phase 1 command.
    /// </summary>
    public class KfsUpdatePhase1Op : KfsPhase1Op
    {
        public UInt64 Inode;
        public UInt64 CommitID;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is KfsUpdatePhase1Op)
            {
                KfsUpdatePhase1Op O = obj as KfsUpdatePhase1Op;
                return (Inode == O.Inode && CommitID == O.CommitID);
            }

            return false;
        }

        public override void AddToMsg(AnpMsg M)
        {
            M.AddUInt32(4);
            M.AddUInt32(KAnpType.KANP_KFS_OP_UPDATE_FILE);
            M.AddUInt64(Inode);
            M.AddUInt64(CommitID);
        }
    }

    /// <summary>
    /// Represent a delete file/directory operation in a phase 1 command.
    /// </summary>
    public class KfsDeletePhase1Op : KfsPhase1Op
    {
        public bool IsFile;
        public UInt64 Inode;
        public UInt64 CommitID;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is KfsDeletePhase1Op)
            {
                KfsDeletePhase1Op O = obj as KfsDeletePhase1Op;
                return (IsFile == O.IsFile &&
                        Inode == O.Inode &&
                        CommitID == O.CommitID);
            }

            return false;
        }

        public override void AddToMsg(AnpMsg M)
        {
            M.AddUInt32(4);
            M.AddUInt32(IsFile ? KAnpType.KANP_KFS_OP_DELETE_FILE : KAnpType.KANP_KFS_OP_DELETE_DIR);
            M.AddUInt64(Inode);
            M.AddUInt64(CommitID);
        }
    }

    /// <summary>
    /// Represent a move file/directory operation in a phase 1 command.
    /// </summary>
    public class KfsMovePhase1Op : KfsPhase1Op
    {
        public bool IsFile;
        public UInt64 MovedInode;
        public UInt64 MovedCommitID;
        public UInt64 ParentInode;
        public UInt64 ParentCommitID;
        public String Path;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is KfsMovePhase1Op)
            {
                KfsMovePhase1Op O = obj as KfsMovePhase1Op;
                return (IsFile == O.IsFile &&
                        MovedInode == O.MovedInode &&
                        MovedCommitID == O.MovedCommitID &&
                        ParentInode == O.ParentInode &&
                        ParentCommitID == O.ParentCommitID &&
                        Path == O.Path);
            }

            return false;
        }

        public override void AddToMsg(AnpMsg M)
        {
            M.AddUInt32(7);
            M.AddUInt32(IsFile ? KAnpType.KANP_KFS_OP_MOVE_FILE : KAnpType.KANP_KFS_OP_MOVE_DIR);
            M.AddUInt64(MovedInode);
            M.AddUInt64(MovedCommitID);
            M.AddUInt64(ParentInode);
            M.AddUInt64(ParentCommitID);
            M.AddString(Path);
        }
    }
}