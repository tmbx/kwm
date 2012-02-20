using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using System.Collections;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum ColumnKey
    {
        [Base.Description("Order ID")]
        OrderID,
        [Base.Description("File")]
        File,
        [Base.Description("Size")]
        Progress,
        [Base.Description("Direction")]
        Direction,
        [Base.Description("Status")]
        Status
    }

    public partial class KfsTransfers : UserControl
    {
        /// <summary>
        /// Reference to our base application.
        /// </summary>
        private AppKfs m_srcApp;

        /// <summary>
        /// Set to true whenever an item from the context menu
        /// is clicked.
        /// </summary>
        private bool m_itemClickedFlag = false;

        /// <summary>
        /// Set to true whenever a context menu is opened.
        /// </summary>
        private bool m_contextMenuOpenedFlag = false;

        /// <summary>
        /// Contains all the different possible menu items 
        /// for the Listview contextual menu
        /// </summary>
        private Hashtable m_lvMenuItems = new Hashtable();

        /// <summary>
        /// Return the number of items contained in the listview
        /// </summary>
        public int NbItems
        {
            get { return lvTransfers.Items.Count; }
        }

        /// <summary>
        /// Return the number of transfers in error in the listview.
        /// </summary>
        public int NbErrors
        {
            get
            {
                int err = 0;
                foreach (KfsFileTransferItem i in lvTransfers.Items)
                {
                    if (i.InError)
                        err++;
                }

                return err;
            }
        }

        public int NbActiveTransfers
        {
            get
            {
                return NbItems - NbErrors;
            }
        }

        private KfsShare Share
        {
            get { return m_srcApp.Share; }

        }

        public KfsTransfers()
        {
            InitializeComponent();
            
            InitColumns();

            CreateLvMenuItems();
        }

        /// <summary>
        /// Initialize the listview columns.
        /// </summary>
        private void InitColumns()
        {
            ColumnHeader h;
            h = new ColumnHeader();
            h.Name = ColumnKey.OrderID.ToString();
            h.Text = Base.GetEnumDescription(ColumnKey.OrderID);
            h.Width = 0;
            this.lvTransfers.Columns.Add(h);

            h = new ColumnHeader();
            h.Name = ColumnKey.Direction.ToString();
            h.Text = Base.GetEnumDescription(ColumnKey.Direction);
            // FIXME : remove text, add icon
            h.Width = 0;
            this.lvTransfers.Columns.Add(h);
            

            h = new ColumnHeader();
            h.Name = ColumnKey.Status.ToString();
            h.Text = Base.GetEnumDescription(ColumnKey.Status);
            // FIXME : remove text, add icon
            h.Width = 80;
            this.lvTransfers.Columns.Add(h);

            h = new ColumnHeader();
            h.Name = ColumnKey.File.ToString();
            h.Text = Base.GetEnumDescription(ColumnKey.File);
            h.Width = 300;
            this.lvTransfers.Columns.Add(h);

            h = new ColumnHeader();
            h.Name = ColumnKey.Progress.ToString();
            h.Text = Base.GetEnumDescription(ColumnKey.Progress);
            h.TextAlign = HorizontalAlignment.Right;
            // FIXME : remove text, add icon
            h.Width = 100;
            this.lvTransfers.Columns.Add(h);
        }

        /// <summary>
        /// Create the contextual menu items.
        /// </summary>
        private void CreateLvMenuItems()
        {
            ToolStripMenuItem item;

            item = new ToolStripMenuItem("Cancel transfer", null, HandleListviewCancel, "Cancel");
            m_lvMenuItems.Add("Cancel", item);
        }

        /// <summary>
        /// Init the KfsTransfer for a new Workspace.
        /// </summary>
        public void Init(AppKfs _app)
        {
            m_srcApp = _app;
        }

        /// <summary>
        /// Populate the listview with the current uploads / downloads
        /// </summary>
        /// <returns>The number of items in the list view.</returns>
        public void UpdateTransfers()
        {
            try
            {
                lvTransfers.BeginUpdate();

                // Get a unified list of current transfers.
                SortedDictionary<UInt64, KfsFileTransfer> currentTransfers = new SortedDictionary<UInt64, KfsFileTransfer>();

                SortedDictionary<UInt64, KfsFileTransfer> transfersInError = new SortedDictionary<UInt64, KfsFileTransfer>();

                foreach (KeyValuePair<UInt64, KfsFileUpload> val in Share.UploadManager.OrderTree)
                {
                    currentTransfers.Add(val.Key, val.Value);
                }

                foreach (KeyValuePair<UInt64, KfsFileDownload> val in Share.DownloadManager.OrderTree)
                {
                    currentTransfers.Add(val.Key, val.Value);
                }

                List<int> toRemove = new List<int>();
                int j = 0;
                foreach (KfsTransferError t in Share.TransferErrorArray)
                {
                    // Only report transfer errors.
                    if (t.Type == TransferErrorType.Download || t.Type == TransferErrorType.Upload)
                    {
                        transfersInError.Add(t.FileTransfer.OrderID, t.FileTransfer);
                    }
                    else
                    {
                        Misc.KwmTellUser("Unable to complete your operation : " + t.Reason);
                        toRemove.Add(j);
                        Logging.Log(2, "Metadata error. Type : " + t.Type + ", Error : " + t.Reason);
                    }
                    j++;
                }
                foreach (int k in toRemove)
                {
                    Share.TransferErrorArray.RemoveAt(k);
                }


                // Update the existing transfer list :
                // 1. Update items that are still in transfer.
                // 2. Update items that are in error.
                // 3. Remove done items.
                foreach (KfsFileTransferItem i in lvTransfers.Items)
                {
                    if (currentTransfers.ContainsKey(i.OrderID))
                        i.UpdateInfos(currentTransfers[i.OrderID]);
                    else if (transfersInError.ContainsKey(i.OrderID))
                        i.UpdateInfos(transfersInError[i.OrderID]);
                    else
                        i.Remove();
                }

                // Add new transfers. This is a performance killer.
                foreach (KeyValuePair<UInt64, KfsFileTransfer> val in currentTransfers)
                {
                    if (!lvTransfers.Items.ContainsKey(val.Value.OrderID.ToString()))
                        lvTransfers.Items.Add(new KfsFileTransferItem(val.Value));
                }

                foreach (KeyValuePair<UInt64, KfsFileTransfer> val in transfersInError)
                {
                    if (!lvTransfers.Items.ContainsKey(val.Value.OrderID.ToString()))
                        lvTransfers.Items.Add(new KfsFileTransferItem(val.Value));
                }
            }

            finally
            {
                lvTransfers.EndUpdate();
            }
        }

        /// <summary>
        /// Clear any transfer that is in error from the listview.
        /// </summary>
        public void ClearErrors()
        {
            Debug.Assert(Share != null);

            foreach (KfsFileTransferItem i in lvTransfers.Items)
            {
                if (i.InError)
                    i.Remove();
            }
            Share.TransferErrorArray.Clear();
        }

        /// <summary>
        /// Cancel all existing transfers.
        /// </summary>
        public void CancelAllXfers()
        {
            foreach (KfsFileTransferItem i in lvTransfers.Items)
            {
                if (i.Direction)
                    // An upload
                    Share.UploadManager.CancelUpload(i.OrderID);
                else
                    // A download
                    Share.DownloadManager.CancelDownload(i.OrderID);
            }
        }

        private void HandleListviewCancel(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(lvTransfers.SelectedItems.Count > 0);

                foreach (KfsFileTransferItem i in lvTransfers.SelectedItems)
                {
                    if (i.Direction)
                        // An upload
                        Share.UploadManager.CancelUpload(i.OrderID);
                    else
                        // A download
                        Share.DownloadManager.CancelDownload(i.OrderID);
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewCancel");
            }
        }

        private void OnActionCompleted(string reason)
        {
            UnsetItemClickedFlag();
            Share.Gate.GateExit(reason);
        }

        private void ContextMenuOpening()
        {
            if (!m_contextMenuOpenedFlag)
            {
                Share.Gate.GateEntry("KfsTransfers::ContextMenuOpening");
                if (m_itemClickedFlag)
                {
                    Debug.Assert(false, "lvContextMenu_Opening : m_itemClickedFlag is true and should not be.");
                    UnsetItemClickedFlag();
                }
            }

            m_contextMenuOpenedFlag = true;
        }

        private void ContextMenuClosing()
        {
            // Exit gate only if we did not click on an item:
            // the item handler will take care of leaving the 
            // gate by itself.
            if (!m_itemClickedFlag)
                Share.Gate.GateExit("ContextMenuClosing");

            m_contextMenuOpenedFlag = false;
        }

        private void SetItemClickedFlag()
        {
            m_itemClickedFlag = true;
        }

        private void UnsetItemClickedFlag()
        {
            m_itemClickedFlag = false;
        }

        /// <summary>
        /// Add the appropriate menus to the context menu.
        /// </summary>
        private void UpdateLvMenus()
        {
            lvContextMenu.Items.Clear();
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Cancel"]);
        }

        /// <summary>
        /// Correctly set the Enabled status to the context menu items.
        /// </summary>
        private void UpdateLvMenuStatuses()
        {
            lvContextMenu.Items["Cancel"].Enabled = CanAnyItemBeCanceled();
        }

        private bool CanAnyItemBeCanceled()
        {
            foreach (KfsFileTransferItem i in lvTransfers.SelectedItems)
            {
                if ((i.Status == FileTransferStatus.Batched ||
                    i.Status == FileTransferStatus.Queued) &&
                    !i.InError)
                {
                    return true;
                }
            }
            return false;
        }

        private void lvContextMenu_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                ContextMenuOpening();

                if (lvTransfers.SelectedItems.Count < 1)
                {
                    e.Cancel = true;
                    ContextMenuClosing();
                    return;
                }

                // If no menu items were in lvContextMenu when this was
                // called, e.Cancel is set to true by the system.
                e.Cancel = false;

                UpdateLvMenus();

                UpdateLvMenuStatuses();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            try
            {
                ContextMenuClosing();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                SetItemClickedFlag();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void lvTransfers_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            try
            {
                if (e.ColumnIndex < 2)
                {
                    e.Cancel = true;
                    e.NewWidth = 0;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }
    }

    public class KfsFileTransferItem : CustomListViewItem
    {
        /// <summary>
        /// Transfer Order ID. Used to identify a specific transfer
        /// when talking with a transfer manager.
        /// </summary>
        public UInt64 OrderID;

        /// <summary>
        /// True for an upload, false for a download
        /// </summary>
        public bool Direction;

        /// <summary>
        /// Status of this transfer.
        /// </summary>
        public FileTransferStatus Status;

        /// <summary>
        /// Relative path of the file being transfered.
        /// </summary>
        public string File;

        /// <summary>
        /// Number of bytes that have been transfered.
        /// </summary>
        public UInt64 BytesTransfered;

        /// <summary>
        /// Total bytes to transfer.
        /// </summary>
        public UInt64 BytesToTransfer;

        /// <summary>
        /// Transfer error. Null if no error.
        /// </summary>
        public KfsTransferError Error;

        public bool InError
        {
            get { return (Error != null); }
        }

        public KfsFileTransferItem(KfsFileTransfer xfer)
        {
            /* Create all subitems, without any text in them */
            CustomListViewSubItem subItem;

            subItem = new CustomListViewSubItem();
            subItem.Name = ColumnKey.Direction.ToString();
            AddCustomSubItem(subItem);
            
            subItem = new CustomListViewSubItem();
            subItem.Name = ColumnKey.Status.ToString();
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = ColumnKey.File.ToString();
            AddCustomSubItem(subItem);

            subItem = new CustomListViewSubItem();
            subItem.Name = ColumnKey.Progress.ToString();
            AddCustomSubItem(subItem);
            UpdateInfos(xfer);
        }

        public void UpdateInfos(KfsFileTransfer xfer)
        {
            // Update persistant data

            OrderID = xfer.OrderID;
            Direction = xfer is KfsFileUpload;
            Status = xfer.Status;
            File = xfer.LastFullPath;
            BytesTransfered = xfer.BytesTransferred;

            if (xfer is KfsFileUpload)
            {
                BytesToTransfer = ((KfsFileUpload)xfer).Size;
            }
            else
            {
                BytesToTransfer = ((KfsFileDownload)xfer).Version.Size;
            }

            Error = xfer.Error;

            // Update subitems
            this.Name = OrderID.ToString();
            this.Text = this.Name;

            if (InError)
            {
                this.SubItems[ColumnKey.Status.ToString()].Text = "Failed";
                this.SubItems[ColumnKey.Progress.ToString()].Text = Error.Reason;
                this.ForeColor = Color.Red;
            }
            else
            {
                this.SubItems[ColumnKey.Status.ToString()].Text = Base.GetEnumDescription(Status);
                this.SubItems[ColumnKey.Progress.ToString()].Text = Base.GetHumanFileSize(BytesTransfered) + "/" + Base.GetHumanFileSize(BytesToTransfer);
                this.ForeColor = Color.Black;
            }
            this.SubItems[ColumnKey.Direction.ToString()].Text = Direction ? "U" : "D";
            this.SubItems[ColumnKey.File.ToString()].Text = File;
            
        }
    }
}
