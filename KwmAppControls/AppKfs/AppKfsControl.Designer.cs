namespace kwm.KwmAppControls.AppKfs
{
    partial class AppKfsControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppKfsControl));
            this.tvContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.lvContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.kfsToolbar = new System.Windows.Forms.ToolStrip();
            this.btnNew = new System.Windows.Forms.ToolStripSplitButton();
            this.btnImport = new System.Windows.Forms.ToolStripSplitButton();
            this.importFilesToolStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.importFolderToolStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.btnSyncAll = new System.Windows.Forms.ToolStripButton();
            this.btnViewOfflineFiles = new System.Windows.Forms.ToolStripButton();
            this.kfsTransferSplitter = new System.Windows.Forms.SplitContainer();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.StaleHeader = new System.Windows.Forms.Panel();
            this.btnResolve = new kwm.Utils.SplitButton();
            this.splitContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.advancedModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lnkStale = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.kfsFilesSplitter = new System.Windows.Forms.SplitContainer();
            this.tvFileTree = new kwm.Utils.KTreeView();
            this.lvFileList = new kwm.Utils.CustomListView();
            this.FileName = new System.Windows.Forms.ColumnHeader();
            this.fileSize = new System.Windows.Forms.ColumnHeader();
            this.TransferStatus = new System.Windows.Forms.ColumnHeader();
            this.ModifiedDate = new System.Windows.Forms.ColumnHeader();
            this.ModifiedBy = new System.Windows.Forms.ColumnHeader();
            this.Status = new System.Windows.Forms.ColumnHeader();
            this.TransferHeader = new System.Windows.Forms.Panel();
            this.btnCancelAll = new System.Windows.Forms.Button();
            this.lnkTransfers = new System.Windows.Forms.LinkLabel();
            this.btnClearErrors = new System.Windows.Forms.Button();
            this.picExpand = new System.Windows.Forms.PictureBox();
            this.kfsTransfers = new kwm.KwmAppControls.AppKfs.KfsTransfers();
            this.kfsToolbar.SuspendLayout();
            this.kfsTransferSplitter.Panel1.SuspendLayout();
            this.kfsTransferSplitter.Panel2.SuspendLayout();
            this.kfsTransferSplitter.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.StaleHeader.SuspendLayout();
            this.splitContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.kfsFilesSplitter.Panel1.SuspendLayout();
            this.kfsFilesSplitter.Panel2.SuspendLayout();
            this.kfsFilesSplitter.SuspendLayout();
            this.TransferHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picExpand)).BeginInit();
            this.SuspendLayout();
            // 
            // tvContextMenu
            // 
            this.tvContextMenu.Name = "tvContextMenu";
            this.tvContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // lvContextMenu
            // 
            this.lvContextMenu.Name = "lvContextMenu";
            this.lvContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // kfsToolbar
            // 
            this.kfsToolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnNew,
            this.btnImport,
            this.btnSyncAll,
            this.btnViewOfflineFiles});
            this.kfsToolbar.Location = new System.Drawing.Point(0, 0);
            this.kfsToolbar.Name = "kfsToolbar";
            this.kfsToolbar.Size = new System.Drawing.Size(572, 39);
            this.kfsToolbar.TabIndex = 1;
            this.kfsToolbar.Text = "toolStrip1";
            // 
            // btnNew
            // 
            this.btnNew.Enabled = false;
            this.btnNew.Image = ((System.Drawing.Image)(resources.GetObject("btnNew.Image")));
            this.btnNew.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(91, 36);
            this.btnNew.Text = "New ...";
            this.btnNew.ButtonClick += new System.EventHandler(this.btnNew_ButtonClick);
            this.btnNew.DropDownOpening += new System.EventHandler(this.btnNew_DropDownOpening);
            this.btnNew.DropDownClosed += new System.EventHandler(this.btnNew_DropDownClosed);
            this.btnNew.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.btnNew_DropDownItemClicked);
            // 
            // btnImport
            // 
            this.btnImport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importFilesToolStrip,
            this.importFolderToolStrip});
            this.btnImport.Enabled = false;
            this.btnImport.Image = ((System.Drawing.Image)(resources.GetObject("btnImport.Image")));
            this.btnImport.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnImport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(117, 36);
            this.btnImport.Text = "Import file(s)";
            this.btnImport.DropDownOpening += new System.EventHandler(this.btnImport_DropDownOpening);
            this.btnImport.DropDownClosed += new System.EventHandler(this.btnImport_DropDownClosed);
            this.btnImport.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.btnImport_DropDownItemClicked);
            // 
            // importFilesToolStrip
            // 
            this.importFilesToolStrip.Image = ((System.Drawing.Image)(resources.GetObject("importFilesToolStrip.Image")));
            this.importFilesToolStrip.Name = "importFilesToolStrip";
            this.importFilesToolStrip.Size = new System.Drawing.Size(157, 22);
            this.importFilesToolStrip.Text = "Import file(s)";
            // 
            // importFolderToolStrip
            // 
            this.importFolderToolStrip.Image = kwm.KwmAppControls.Properties.Resources.folderOpened;
            this.importFolderToolStrip.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.importFolderToolStrip.Name = "importFolderToolStrip";
            this.importFolderToolStrip.Size = new System.Drawing.Size(157, 22);
            this.importFolderToolStrip.Text = "Import a folder";
            // 
            // btnSyncAll
            // 
            this.btnSyncAll.Enabled = false;
            this.btnSyncAll.Image = kwm.KwmAppControls.Properties.Resources.btnSyncAll;
            this.btnSyncAll.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnSyncAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSyncAll.Name = "btnSyncAll";
            this.btnSyncAll.Size = new System.Drawing.Size(104, 36);
            this.btnSyncAll.Text = "Synchronize all";
            // 
            // btnViewOfflineFiles
            // 
            this.btnViewOfflineFiles.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnViewOfflineFiles.Enabled = false;
            this.btnViewOfflineFiles.Image = ((System.Drawing.Image)(resources.GetObject("btnViewOfflineFiles.Image")));
            this.btnViewOfflineFiles.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btnViewOfflineFiles.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnViewOfflineFiles.Name = "btnViewOfflineFiles";
            this.btnViewOfflineFiles.Size = new System.Drawing.Size(124, 36);
            this.btnViewOfflineFiles.Text = "View Offline Files";
            // 
            // kfsTransferSplitter
            // 
            this.kfsTransferSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kfsTransferSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.kfsTransferSplitter.Location = new System.Drawing.Point(0, 39);
            this.kfsTransferSplitter.Name = "kfsTransferSplitter";
            this.kfsTransferSplitter.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // kfsTransferSplitter.Panel1
            // 
            this.kfsTransferSplitter.Panel1.Controls.Add(this.splitContainer1);
            // 
            // kfsTransferSplitter.Panel2
            // 
            this.kfsTransferSplitter.Panel2.Controls.Add(this.kfsTransfers);
            this.kfsTransferSplitter.Panel2MinSize = 60;
            this.kfsTransferSplitter.Size = new System.Drawing.Size(572, 293);
            this.kfsTransferSplitter.SplitterDistance = 193;
            this.kfsTransferSplitter.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.StaleHeader);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(572, 193);
            this.splitContainer1.SplitterDistance = 57;
            this.splitContainer1.TabIndex = 2;
            // 
            // StaleHeader
            // 
            this.StaleHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.StaleHeader.Controls.Add(this.btnResolve);
            this.StaleHeader.Controls.Add(this.lnkStale);
            this.StaleHeader.Controls.Add(this.pictureBox1);
            this.StaleHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StaleHeader.Location = new System.Drawing.Point(0, 0);
            this.StaleHeader.Name = "StaleHeader";
            this.StaleHeader.Size = new System.Drawing.Size(572, 57);
            this.StaleHeader.TabIndex = 0;
            // 
            // btnResolve
            // 
            this.btnResolve.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResolve.AutoSize = true;
            this.btnResolve.DropDownMenu = this.splitContextMenu;
            this.btnResolve.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResolve.Location = new System.Drawing.Point(466, 8);
            this.btnResolve.Name = "btnResolve";
            this.btnResolve.Size = new System.Drawing.Size(97, 39);
            this.btnResolve.TabIndex = 5;
            this.btnResolve.Text = "Resolve";
            this.btnResolve.UseVisualStyleBackColor = true;
            this.btnResolve.Click += new System.EventHandler(this.btnResolve_Click);
            // 
            // splitContextMenu
            // 
            this.splitContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.advancedModeToolStripMenuItem});
            this.splitContextMenu.Name = "splitContextMenu";
            this.splitContextMenu.ShowImageMargin = false;
            this.splitContextMenu.Size = new System.Drawing.Size(135, 26);
            this.splitContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.splitContextMenu_Opening);
            // 
            // advancedModeToolStripMenuItem
            // 
            this.advancedModeToolStripMenuItem.Name = "advancedModeToolStripMenuItem";
            this.advancedModeToolStripMenuItem.ShowShortcutKeys = false;
            this.advancedModeToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
            this.advancedModeToolStripMenuItem.Text = "Advanced mode ...";
            this.advancedModeToolStripMenuItem.Click += new System.EventHandler(this.advancedModeToolStripMenuItem_Click);
            // 
            // lnkStale
            // 
            this.lnkStale.AutoEllipsis = true;
            this.lnkStale.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lnkStale.ForeColor = System.Drawing.Color.Red;
            this.lnkStale.Location = new System.Drawing.Point(57, 12);
            this.lnkStale.Name = "lnkStale";
            this.lnkStale.Size = new System.Drawing.Size(403, 35);
            this.lnkStale.TabIndex = 3;
            this.lnkStale.Text = "Stale view";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = kwm.KwmAppControls.Properties.Resources.warning48x48;
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.kfsFilesSplitter);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.TransferHeader);
            this.splitContainer2.Size = new System.Drawing.Size(572, 132);
            this.splitContainer2.SplitterDistance = 99;
            this.splitContainer2.TabIndex = 6;
            // 
            // kfsFilesSplitter
            // 
            this.kfsFilesSplitter.AllowDrop = true;
            this.kfsFilesSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kfsFilesSplitter.Location = new System.Drawing.Point(0, 0);
            this.kfsFilesSplitter.Name = "kfsFilesSplitter";
            // 
            // kfsFilesSplitter.Panel1
            // 
            this.kfsFilesSplitter.Panel1.Controls.Add(this.tvFileTree);
            // 
            // kfsFilesSplitter.Panel2
            // 
            this.kfsFilesSplitter.Panel2.AllowDrop = true;
            this.kfsFilesSplitter.Panel2.Controls.Add(this.lvFileList);
            this.kfsFilesSplitter.Size = new System.Drawing.Size(572, 99);
            this.kfsFilesSplitter.SplitterDistance = 95;
            this.kfsFilesSplitter.TabIndex = 1;
            // 
            // tvFileTree
            // 
            this.tvFileTree.AllowContextMenuOnNothing = false;
            this.tvFileTree.AllowDrop = true;
            this.tvFileTree.ContextMenuStrip = this.tvContextMenu;
            this.tvFileTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvFileTree.Enabled = false;
            this.tvFileTree.HideSelection = false;
            this.tvFileTree.LabelEdit = true;
            this.tvFileTree.Location = new System.Drawing.Point(0, 0);
            this.tvFileTree.Name = "tvFileTree";
            this.tvFileTree.PathSeparator = "/";
            this.tvFileTree.Size = new System.Drawing.Size(95, 99);
            this.tvFileTree.TabIndex = 0;
            this.tvFileTree.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.tvFileTree_AfterCollapse);
            this.tvFileTree.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tvFileTree_AfterExpand);
            // 
            // lvFileList
            // 
            this.lvFileList.AllowDrop = true;
            this.lvFileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FileName,
            this.fileSize,
            this.TransferStatus,
            this.ModifiedDate,
            this.ModifiedBy,
            this.Status});
            this.lvFileList.ContextMenuStrip = this.lvContextMenu;
            this.lvFileList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFileList.Enabled = false;
            this.lvFileList.HideSelection = false;
            this.lvFileList.LabelEdit = true;
            this.lvFileList.Location = new System.Drawing.Point(0, 0);
            this.lvFileList.Name = "lvFileList";
            this.lvFileList.Size = new System.Drawing.Size(473, 99);
            this.lvFileList.TabIndex = 1;
            this.lvFileList.UseCompatibleStateImageBehavior = false;
            this.lvFileList.View = System.Windows.Forms.View.Details;
            this.lvFileList.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.lvFileList_ColumnWidthChanged);
            this.lvFileList.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.lvFileList_ColumnWidthChanging);
            // 
            // FileName
            // 
            this.FileName.Text = "Name";
            this.FileName.Width = 130;
            // 
            // fileSize
            // 
            this.fileSize.Text = "Size";
            // 
            // TransferStatus
            // 
            this.TransferStatus.Text = "";
            this.TransferStatus.Width = 0;
            // 
            // ModifiedDate
            // 
            this.ModifiedDate.Text = "Date Modified";
            this.ModifiedDate.Width = 111;
            // 
            // ModifiedBy
            // 
            this.ModifiedBy.Text = "Modified By";
            this.ModifiedBy.Width = 102;
            // 
            // Status
            // 
            this.Status.Text = "Status";
            this.Status.Width = 96;
            // 
            // TransferHeader
            // 
            this.TransferHeader.Controls.Add(this.btnCancelAll);
            this.TransferHeader.Controls.Add(this.lnkTransfers);
            this.TransferHeader.Controls.Add(this.btnClearErrors);
            this.TransferHeader.Controls.Add(this.picExpand);
            this.TransferHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TransferHeader.Location = new System.Drawing.Point(0, 0);
            this.TransferHeader.Name = "TransferHeader";
            this.TransferHeader.Size = new System.Drawing.Size(572, 29);
            this.TransferHeader.TabIndex = 5;
            // 
            // btnCancelAll
            // 
            this.btnCancelAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancelAll.Location = new System.Drawing.Point(408, 3);
            this.btnCancelAll.Name = "btnCancelAll";
            this.btnCancelAll.Size = new System.Drawing.Size(75, 23);
            this.btnCancelAll.TabIndex = 5;
            this.btnCancelAll.Text = "Cancel all";
            this.btnCancelAll.UseVisualStyleBackColor = true;
            this.btnCancelAll.Click += new System.EventHandler(this.btnCancelAll_Click);
            // 
            // lnkTransfers
            // 
            this.lnkTransfers.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkTransfers.LinkColor = System.Drawing.Color.Black;
            this.lnkTransfers.Location = new System.Drawing.Point(29, 5);
            this.lnkTransfers.Name = "lnkTransfers";
            this.lnkTransfers.Size = new System.Drawing.Size(141, 18);
            this.lnkTransfers.TabIndex = 4;
            this.lnkTransfers.TabStop = true;
            this.lnkTransfers.Text = "Your current transfers ...";
            this.lnkTransfers.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkTransfers.VisitedLinkColor = System.Drawing.Color.Black;
            this.lnkTransfers.Click += new System.EventHandler(this.lnkTransfers_Click);
            // 
            // btnClearErrors
            // 
            this.btnClearErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearErrors.Location = new System.Drawing.Point(489, 3);
            this.btnClearErrors.Name = "btnClearErrors";
            this.btnClearErrors.Size = new System.Drawing.Size(75, 23);
            this.btnClearErrors.TabIndex = 2;
            this.btnClearErrors.Text = "Clear errors";
            this.btnClearErrors.UseVisualStyleBackColor = true;
            this.btnClearErrors.Click += new System.EventHandler(this.btnClearErrors_Click);
            // 
            // picExpand
            // 
            this.picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupExpand;
            this.picExpand.Location = new System.Drawing.Point(3, 4);
            this.picExpand.Name = "picExpand";
            this.picExpand.Size = new System.Drawing.Size(21, 21);
            this.picExpand.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picExpand.TabIndex = 3;
            this.picExpand.TabStop = false;
            this.picExpand.MouseLeave += new System.EventHandler(this.picExpand_MouseLeave);
            this.picExpand.Click += new System.EventHandler(this.picExpand_Click);
            this.picExpand.MouseEnter += new System.EventHandler(this.picExpand_MouseEnter);
            // 
            // kfsTransfers
            // 
            this.kfsTransfers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.kfsTransfers.Location = new System.Drawing.Point(0, 0);
            this.kfsTransfers.Name = "kfsTransfers";
            this.kfsTransfers.Size = new System.Drawing.Size(572, 96);
            this.kfsTransfers.TabIndex = 0;
            // 
            // AppKfsControl
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Controls.Add(this.kfsTransferSplitter);
            this.Controls.Add(this.kfsToolbar);
            this.Name = "AppKfsControl";
            this.Load += new System.EventHandler(this.AppKfsControl_Load);
            this.kfsToolbar.ResumeLayout(false);
            this.kfsToolbar.PerformLayout();
            this.kfsTransferSplitter.Panel1.ResumeLayout(false);
            this.kfsTransferSplitter.Panel2.ResumeLayout(false);
            this.kfsTransferSplitter.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.StaleHeader.ResumeLayout(false);
            this.StaleHeader.PerformLayout();
            this.splitContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.kfsFilesSplitter.Panel1.ResumeLayout(false);
            this.kfsFilesSplitter.Panel2.ResumeLayout(false);
            this.kfsFilesSplitter.ResumeLayout(false);
            this.TransferHeader.ResumeLayout(false);
            this.TransferHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picExpand)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip kfsToolbar;
        private System.Windows.Forms.ToolStripButton btnSyncAll;
        private System.Windows.Forms.ToolStripButton btnViewOfflineFiles;
        private System.Windows.Forms.ContextMenuStrip tvContextMenu;
        private System.Windows.Forms.ContextMenuStrip lvContextMenu;
        private System.Windows.Forms.SplitContainer kfsTransferSplitter;
        private System.Windows.Forms.SplitContainer kfsFilesSplitter;
        private kwm.Utils.KTreeView tvFileTree;
        private kwm.Utils.CustomListView lvFileList;
        private System.Windows.Forms.ColumnHeader FileName;
        private System.Windows.Forms.ColumnHeader TransferStatus;
        private System.Windows.Forms.ColumnHeader ModifiedDate;
        private System.Windows.Forms.ColumnHeader ModifiedBy;
        private System.Windows.Forms.ColumnHeader Status;
        private KfsTransfers kfsTransfers;
        private System.Windows.Forms.ColumnHeader fileSize;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel StaleHeader;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel TransferHeader;
        private System.Windows.Forms.LinkLabel lnkTransfers;
        private System.Windows.Forms.Button btnClearErrors;
        private System.Windows.Forms.PictureBox picExpand;
        private System.Windows.Forms.Label lnkStale;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private kwm.Utils.SplitButton btnResolve;
        private System.Windows.Forms.ContextMenuStrip splitContextMenu;
        private System.Windows.Forms.ToolStripMenuItem advancedModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSplitButton btnNew;
        private System.Windows.Forms.Button btnCancelAll;
        private System.Windows.Forms.ToolStripSplitButton btnImport;
        private System.Windows.Forms.ToolStripMenuItem importFilesToolStrip;
        private System.Windows.Forms.ToolStripMenuItem importFolderToolStrip;

    }
}
