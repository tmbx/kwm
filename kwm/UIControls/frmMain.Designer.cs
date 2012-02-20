namespace kwm
{
    partial class frmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.splitContainerLeft = new System.Windows.Forms.SplitContainer();
            this.splitContainerWsMembers = new System.Windows.Forms.SplitContainer();
            this.btnNewKws = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tvWorkspaces = new kwm.Utils.KTreeView();
            this.tvWorkspacesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CmWorkOnline = new System.Windows.Forms.ToolStripMenuItem();
            this.CmWorkOffline = new System.Windows.Forms.ToolStripMenuItem();
            this.CmCollapse = new System.Windows.Forms.ToolStripMenuItem();
            this.CmExpand = new System.Windows.Forms.ToolStripMenuItem();
            this.separator1 = new System.Windows.Forms.ToolStripSeparator();
            this.CmCreateNewFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.separator2 = new System.Windows.Forms.ToolStripSeparator();
            this.CmRenameFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.CmRenameKws = new System.Windows.Forms.ToolStripMenuItem();
            this.CmDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.CmAdvanced = new System.Windows.Forms.ToolStripMenuItem();
            this.CmExport = new System.Windows.Forms.ToolStripMenuItem();
            this.CmRebuild = new System.Windows.Forms.ToolStripMenuItem();
            this.CmDisable = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.CmProperties = new System.Windows.Forms.ToolStripMenuItem();
            this.lnkInvite = new System.Windows.Forms.LinkLabel();
            this.btnInvite = new System.Windows.Forms.Button();
            this.txtInvite = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lstUsers = new System.Windows.Forms.ListView();
            this.lstUsersContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ResendInvitation = new System.Windows.Forms.ToolStripMenuItem();
            this.ResetPassword = new System.Windows.Forms.ToolStripMenuItem();
            this.Copy = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.Delete = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.ShowProperties = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainerRight = new System.Windows.Forms.SplitContainer();
            this.ApplicationsTabControl = new System.Windows.Forms.TabControl();
            this.tblHome = new System.Windows.Forms.TabPage();
            this.panelHome = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.picSecure = new System.Windows.Forms.PictureBox();
            this.lblSecureNote = new System.Windows.Forms.Label();
            this.lblByName = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblKAS = new System.Windows.Forms.Label();
            this.lblByOrgName = new System.Windows.Forms.Label();
            this.lblCreationDate = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblCreation = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblKasError = new System.Windows.Forms.Label();
            this.KwsTaskBtn = new System.Windows.Forms.Button();
            this.KwsStatus = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.noSelectionPane = new kwm.NoKwsPane();
            this.tblFileSharing = new System.Windows.Forms.TabPage();
            this.appKfsControl = new kwm.KwmAppControls.AppKfs.AppKfsControl(this.components);
            this.tblScreenShare = new System.Windows.Forms.TabPage();
            this.appAppSharing = new kwm.KwmAppControls.AppScreenSharingControl();
            this.appChatbox = new kwm.KwmAppControls.AppChatboxControl(this.components);
            this.myMainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FileContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.MmNewKwsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MmNewFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MmImportTeamboxesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MmExportTeamboxesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MmExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolsContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MmOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MmConfigurationWizardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MmDebuggingConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.MmAboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayIconMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.trayOpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.trayExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
            this.kasStatusBar = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.ProgressText = new System.Windows.Forms.ToolStripStatusLabel();
            this.Progress = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.splitContainerLeft.Panel1.SuspendLayout();
            this.splitContainerLeft.Panel2.SuspendLayout();
            this.splitContainerLeft.SuspendLayout();
            this.splitContainerWsMembers.Panel1.SuspendLayout();
            this.splitContainerWsMembers.Panel2.SuspendLayout();
            this.splitContainerWsMembers.SuspendLayout();
            this.tvWorkspacesContextMenu.SuspendLayout();
            this.lstUsersContextMenu.SuspendLayout();
            this.splitContainerRight.Panel1.SuspendLayout();
            this.splitContainerRight.Panel2.SuspendLayout();
            this.splitContainerRight.SuspendLayout();
            this.ApplicationsTabControl.SuspendLayout();
            this.tblHome.SuspendLayout();
            this.panelHome.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSecure)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.tblFileSharing.SuspendLayout();
            this.tblScreenShare.SuspendLayout();
            this.myMainMenu.SuspendLayout();
            this.FileContextMenuStrip.SuspendLayout();
            this.ToolsContextMenuStrip.SuspendLayout();
            this.AboutContextMenuStrip.SuspendLayout();
            this.trayIconMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            resources.ApplyResources(this.toolStripContainer1.ContentPanel, "toolStripContainer1.ContentPanel");
            this.toolStripContainer1.ContentPanel.Controls.Add(this.splitContainerLeft);
            resources.ApplyResources(this.toolStripContainer1, "toolStripContainer1");
            this.toolStripContainer1.Name = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.myMainMenu);
            // 
            // splitContainerLeft
            // 
            resources.ApplyResources(this.splitContainerLeft, "splitContainerLeft");
            this.splitContainerLeft.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerLeft.Name = "splitContainerLeft";
            // 
            // splitContainerLeft.Panel1
            // 
            this.splitContainerLeft.Panel1.Controls.Add(this.splitContainerWsMembers);
            // 
            // splitContainerLeft.Panel2
            // 
            this.splitContainerLeft.Panel2.Controls.Add(this.splitContainerRight);
            this.splitContainerLeft.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // splitContainerWsMembers
            // 
            resources.ApplyResources(this.splitContainerWsMembers, "splitContainerWsMembers");
            this.splitContainerWsMembers.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerWsMembers.MinimumSize = new System.Drawing.Size(100, 0);
            this.splitContainerWsMembers.Name = "splitContainerWsMembers";
            // 
            // splitContainerWsMembers.Panel1
            // 
            this.splitContainerWsMembers.Panel1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainerWsMembers.Panel1.Controls.Add(this.btnNewKws);
            this.splitContainerWsMembers.Panel1.Controls.Add(this.label2);
            this.splitContainerWsMembers.Panel1.Controls.Add(this.tvWorkspaces);
            // 
            // splitContainerWsMembers.Panel2
            // 
            this.splitContainerWsMembers.Panel2.Controls.Add(this.lnkInvite);
            this.splitContainerWsMembers.Panel2.Controls.Add(this.btnInvite);
            this.splitContainerWsMembers.Panel2.Controls.Add(this.txtInvite);
            this.splitContainerWsMembers.Panel2.Controls.Add(this.label4);
            this.splitContainerWsMembers.Panel2.Controls.Add(this.lstUsers);
            // 
            // btnNewKws
            // 
            resources.ApplyResources(this.btnNewKws, "btnNewKws");
            this.btnNewKws.Name = "btnNewKws";
            this.btnNewKws.UseVisualStyleBackColor = true;
            this.btnNewKws.Click += new System.EventHandler(this.btnNewKws_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.label2.Name = "label2";
            // 
            // tvWorkspaces
            // 
            this.tvWorkspaces.AllowContextMenuOnNothing = true;
            this.tvWorkspaces.AllowDrop = true;
            resources.ApplyResources(this.tvWorkspaces, "tvWorkspaces");
            this.tvWorkspaces.ContextMenuStrip = this.tvWorkspacesContextMenu;
            this.tvWorkspaces.FullRowSelect = true;
            this.tvWorkspaces.HideSelection = false;
            this.tvWorkspaces.LabelEdit = true;
            this.tvWorkspaces.Name = "tvWorkspaces";
            this.tvWorkspaces.ShowNodeToolTips = true;
            this.tvWorkspaces.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.tvWorkspaces_AfterCollapse);
            this.tvWorkspaces.DragLeave += new System.EventHandler(this.tvWorkspaces_DragLeave);
            this.tvWorkspaces.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.tvWorkspaces_AfterLabelEdit);
            this.tvWorkspaces.DragDrop += new System.Windows.Forms.DragEventHandler(this.tvWorkspaces_DragDrop);
            this.tvWorkspaces.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvWorkspaces_AfterSelect);
            this.tvWorkspaces.DragEnter += new System.Windows.Forms.DragEventHandler(this.tvWorkspaces_DragEnter);
            this.tvWorkspaces.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.tvWorkspaces_BeforeLabelEdit);
            this.tvWorkspaces.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tvWorkspaces_KeyDown);
            this.tvWorkspaces.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tvWorkspaces_AfterExpand);
            this.tvWorkspaces.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.tvWorkspaces_ItemDrag);
            this.tvWorkspaces.DragOver += new System.Windows.Forms.DragEventHandler(this.tvWorkspaces_DragOver);
            // 
            // tvWorkspacesContextMenu
            // 
            this.tvWorkspacesContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmWorkOnline,
            this.CmWorkOffline,
            this.CmCollapse,
            this.CmExpand,
            this.separator1,
            this.CmCreateNewFolder,
            this.separator2,
            this.CmRenameFolder,
            this.CmRenameKws,
            this.CmDelete,
            this.toolStripSeparator8,
            this.CmAdvanced,
            this.toolStripSeparator7,
            this.CmProperties});
            this.tvWorkspacesContextMenu.Name = "lstWorkspacesContextMenu";
            this.tvWorkspacesContextMenu.ShowImageMargin = false;
            resources.ApplyResources(this.tvWorkspacesContextMenu, "tvWorkspacesContextMenu");
            this.tvWorkspacesContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tvWorkspacesContextMenu_ItemClicked);
            this.tvWorkspacesContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.tvWorkspacesContextMenu_Opening);
            this.tvWorkspacesContextMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.tvWorkspacesContextMenu_Closing);
            // 
            // CmWorkOnline
            // 
            this.CmWorkOnline.Name = "CmWorkOnline";
            resources.ApplyResources(this.CmWorkOnline, "CmWorkOnline");
            this.CmWorkOnline.Click += new System.EventHandler(this.CmWorkOnline_Click);
            // 
            // CmWorkOffline
            // 
            this.CmWorkOffline.Name = "CmWorkOffline";
            resources.ApplyResources(this.CmWorkOffline, "CmWorkOffline");
            this.CmWorkOffline.Click += new System.EventHandler(this.CmWorkOffline_Click);
            // 
            // CmCollapse
            // 
            resources.ApplyResources(this.CmCollapse, "CmCollapse");
            this.CmCollapse.Name = "CmCollapse";
            this.CmCollapse.Click += new System.EventHandler(this.CmCollapse_Click);
            // 
            // CmExpand
            // 
            resources.ApplyResources(this.CmExpand, "CmExpand");
            this.CmExpand.Name = "CmExpand";
            this.CmExpand.Click += new System.EventHandler(this.CmExpand_Click);
            // 
            // separator1
            // 
            this.separator1.Name = "separator1";
            resources.ApplyResources(this.separator1, "separator1");
            // 
            // CmCreateNewFolder
            // 
            this.CmCreateNewFolder.Name = "CmCreateNewFolder";
            resources.ApplyResources(this.CmCreateNewFolder, "CmCreateNewFolder");
            this.CmCreateNewFolder.Click += new System.EventHandler(this.CmCreateNewFolder_Click);
            // 
            // separator2
            // 
            this.separator2.Name = "separator2";
            resources.ApplyResources(this.separator2, "separator2");
            // 
            // CmRenameFolder
            // 
            this.CmRenameFolder.Name = "CmRenameFolder";
            resources.ApplyResources(this.CmRenameFolder, "CmRenameFolder");
            // 
            // CmRenameKws
            // 
            this.CmRenameKws.Name = "CmRenameKws";
            resources.ApplyResources(this.CmRenameKws, "CmRenameKws");
            this.CmRenameKws.Click += new System.EventHandler(this.CmRename_Click);
            // 
            // CmDelete
            // 
            this.CmDelete.Name = "CmDelete";
            resources.ApplyResources(this.CmDelete, "CmDelete");
            this.CmDelete.Click += new System.EventHandler(this.CmDelete_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // CmAdvanced
            // 
            this.CmAdvanced.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmExport,
            this.CmRebuild,
            this.CmDisable});
            this.CmAdvanced.Name = "CmAdvanced";
            resources.ApplyResources(this.CmAdvanced, "CmAdvanced");
            // 
            // CmExport
            // 
            this.CmExport.Name = "CmExport";
            resources.ApplyResources(this.CmExport, "CmExport");
            this.CmExport.Click += new System.EventHandler(this.CmExport_Click);
            // 
            // CmRebuild
            // 
            this.CmRebuild.Name = "CmRebuild";
            resources.ApplyResources(this.CmRebuild, "CmRebuild");
            this.CmRebuild.Click += new System.EventHandler(this.CmRebuild_Click);
            // 
            // CmDisable
            // 
            this.CmDisable.Name = "CmDisable";
            resources.ApplyResources(this.CmDisable, "CmDisable");
            this.CmDisable.Click += new System.EventHandler(this.CmDisable_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // CmProperties
            // 
            this.CmProperties.Name = "CmProperties";
            resources.ApplyResources(this.CmProperties, "CmProperties");
            this.CmProperties.Click += new System.EventHandler(this.CmKwsProperties_Click);
            // 
            // lnkInvite
            // 
            resources.ApplyResources(this.lnkInvite, "lnkInvite");
            this.lnkInvite.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.lnkInvite.Name = "lnkInvite";
            this.lnkInvite.TabStop = true;
            this.lnkInvite.VisitedLinkColor = System.Drawing.Color.Blue;
            this.lnkInvite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkInvite_LinkClicked);
            // 
            // btnInvite
            // 
            resources.ApplyResources(this.btnInvite, "btnInvite");
            this.btnInvite.Image = global::kwm.Properties.Resources.btnAddFiles;
            this.btnInvite.Name = "btnInvite";
            this.btnInvite.UseVisualStyleBackColor = true;
            this.btnInvite.Click += new System.EventHandler(this.btnInvite_Click);
            // 
            // txtInvite
            // 
            resources.ApplyResources(this.txtInvite, "txtInvite");
            this.txtInvite.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtInvite.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtInvite.Name = "txtInvite";
            this.txtInvite.Enter += new System.EventHandler(this.txtInvite_Enter);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // lstUsers
            // 
            this.lstUsers.AllowColumnReorder = true;
            resources.ApplyResources(this.lstUsers, "lstUsers");
            this.lstUsers.ContextMenuStrip = this.lstUsersContextMenu;
            this.lstUsers.FullRowSelect = true;
            this.lstUsers.HideSelection = false;
            this.lstUsers.MultiSelect = false;
            this.lstUsers.Name = "lstUsers";
            this.lstUsers.ShowItemToolTips = true;
            this.lstUsers.UseCompatibleStateImageBehavior = false;
            this.lstUsers.View = System.Windows.Forms.View.Details;
            this.lstUsers.SelectedIndexChanged += new System.EventHandler(this.lstUsers_SelectedIndexChanged);
            // 
            // lstUsersContextMenu
            // 
            this.lstUsersContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ResendInvitation,
            this.ResetPassword,
            this.Copy,
            this.toolStripSeparator5,
            this.Delete,
            this.toolStripSeparator6,
            this.ShowProperties});
            this.lstUsersContextMenu.Name = "lstUsersContextMenu";
            resources.ApplyResources(this.lstUsersContextMenu, "lstUsersContextMenu");
            this.lstUsersContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.lstUsersContextMenu_ItemClicked);
            this.lstUsersContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.lstUsersContextMenu_Opening);
            this.lstUsersContextMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.lstUsersContextMenu_Closing);
            // 
            // ResendInvitation
            // 
            this.ResendInvitation.Name = "ResendInvitation";
            resources.ApplyResources(this.ResendInvitation, "ResendInvitation");
            this.ResendInvitation.Tag = "true";
            // 
            // ResetPassword
            // 
            this.ResetPassword.Name = "ResetPassword";
            resources.ApplyResources(this.ResetPassword, "ResetPassword");
            this.ResetPassword.Tag = "true";
            this.ResetPassword.Click += new System.EventHandler(this.resetPasswordToolStripMenuItem_Click);
            // 
            // Copy
            // 
            this.Copy.Name = "Copy";
            resources.ApplyResources(this.Copy, "Copy");
            this.Copy.Tag = "true";
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // Delete
            // 
            this.Delete.Name = "Delete";
            resources.ApplyResources(this.Delete, "Delete");
            this.Delete.Tag = "true";
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // ShowProperties
            // 
            this.ShowProperties.Name = "ShowProperties";
            resources.ApplyResources(this.ShowProperties, "ShowProperties");
            this.ShowProperties.Tag = "true";
            this.ShowProperties.Click += new System.EventHandler(this.UserProperties_Click);
            // 
            // splitContainerRight
            // 
            resources.ApplyResources(this.splitContainerRight, "splitContainerRight");
            this.splitContainerRight.Name = "splitContainerRight";
            // 
            // splitContainerRight.Panel1
            // 
            this.splitContainerRight.Panel1.Controls.Add(this.ApplicationsTabControl);
            // 
            // splitContainerRight.Panel2
            // 
            this.splitContainerRight.Panel2.Controls.Add(this.appChatbox);
            // 
            // ApplicationsTabControl
            // 
            resources.ApplyResources(this.ApplicationsTabControl, "ApplicationsTabControl");
            this.ApplicationsTabControl.Controls.Add(this.tblHome);
            this.ApplicationsTabControl.Controls.Add(this.tblFileSharing);
            this.ApplicationsTabControl.Controls.Add(this.tblScreenShare);
            this.ApplicationsTabControl.Name = "ApplicationsTabControl";
            this.ApplicationsTabControl.SelectedIndex = 0;
            // 
            // tblHome
            // 
            this.tblHome.Controls.Add(this.panelHome);
            this.tblHome.Controls.Add(this.noSelectionPane);
            resources.ApplyResources(this.tblHome, "tblHome");
            this.tblHome.Name = "tblHome";
            this.tblHome.UseVisualStyleBackColor = true;
            // 
            // panelHome
            // 
            this.panelHome.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelHome.Controls.Add(this.groupBox2);
            this.panelHome.Controls.Add(this.groupBox1);
            this.panelHome.Controls.Add(this.lblName);
            resources.ApplyResources(this.panelHome, "panelHome");
            this.panelHome.Name = "panelHome";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.picSecure);
            this.groupBox2.Controls.Add(this.lblSecureNote);
            this.groupBox2.Controls.Add(this.lblByName);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.lblKAS);
            this.groupBox2.Controls.Add(this.lblByOrgName);
            this.groupBox2.Controls.Add(this.lblCreationDate);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.lblCreation);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // picSecure
            // 
            this.picSecure.Image = global::kwm.Properties.Resources.NewLock_OK_24x24;
            resources.ApplyResources(this.picSecure, "picSecure");
            this.picSecure.Name = "picSecure";
            this.picSecure.TabStop = false;
            // 
            // lblSecureNote
            // 
            resources.ApplyResources(this.lblSecureNote, "lblSecureNote");
            this.lblSecureNote.Name = "lblSecureNote";
            // 
            // lblByName
            // 
            this.lblByName.AutoEllipsis = true;
            resources.ApplyResources(this.lblByName, "lblByName");
            this.lblByName.Name = "lblByName";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // lblKAS
            // 
            this.lblKAS.AutoEllipsis = true;
            resources.ApplyResources(this.lblKAS, "lblKAS");
            this.lblKAS.Name = "lblKAS";
            // 
            // lblByOrgName
            // 
            this.lblByOrgName.AutoEllipsis = true;
            resources.ApplyResources(this.lblByOrgName, "lblByOrgName");
            this.lblByOrgName.Name = "lblByOrgName";
            // 
            // lblCreationDate
            // 
            this.lblCreationDate.AutoEllipsis = true;
            resources.ApplyResources(this.lblCreationDate, "lblCreationDate");
            this.lblCreationDate.Name = "lblCreationDate";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // lblCreation
            // 
            resources.ApplyResources(this.lblCreation, "lblCreation");
            this.lblCreation.Name = "lblCreation";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblKasError);
            this.groupBox1.Controls.Add(this.KwsTaskBtn);
            this.groupBox1.Controls.Add(this.KwsStatus);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // lblKasError
            // 
            this.lblKasError.AutoEllipsis = true;
            resources.ApplyResources(this.lblKasError, "lblKasError");
            this.lblKasError.ForeColor = System.Drawing.Color.Red;
            this.lblKasError.Name = "lblKasError";
            this.lblKasError.Click += new System.EventHandler(this.lblKasError_Click);
            // 
            // KwsTaskBtn
            // 
            resources.ApplyResources(this.KwsTaskBtn, "KwsTaskBtn");
            this.KwsTaskBtn.Name = "KwsTaskBtn";
            this.KwsTaskBtn.UseVisualStyleBackColor = true;
            this.KwsTaskBtn.Click += new System.EventHandler(this.KwsTaskBtn_Click);
            // 
            // KwsStatus
            // 
            resources.ApplyResources(this.KwsStatus, "KwsStatus");
            this.KwsStatus.Name = "KwsStatus";
            // 
            // lblName
            // 
            resources.ApplyResources(this.lblName, "lblName");
            this.lblName.Name = "lblName";
            this.lblName.SizeChanged += new System.EventHandler(this.lblName_SizeChanged);
            // 
            // noSelectionPane
            // 
            resources.ApplyResources(this.noSelectionPane, "noSelectionPane");
            this.noSelectionPane.Name = "noSelectionPane";
            // 
            // tblFileSharing
            // 
            this.tblFileSharing.Controls.Add(this.appKfsControl);
            resources.ApplyResources(this.tblFileSharing, "tblFileSharing");
            this.tblFileSharing.Name = "tblFileSharing";
            this.tblFileSharing.Tag = "1";
            this.tblFileSharing.UseVisualStyleBackColor = true;
            // 
            // appKfsControl
            // 
            resources.ApplyResources(this.appKfsControl, "appKfsControl");
            this.appKfsControl.Name = "appKfsControl";
            // 
            // tblScreenShare
            // 
            this.tblScreenShare.Controls.Add(this.appAppSharing);
            resources.ApplyResources(this.tblScreenShare, "tblScreenShare");
            this.tblScreenShare.Name = "tblScreenShare";
            this.tblScreenShare.Tag = "2";
            this.tblScreenShare.UseVisualStyleBackColor = true;
            // 
            // appAppSharing
            // 
            resources.ApplyResources(this.appAppSharing, "appAppSharing");
            this.appAppSharing.Name = "appAppSharing";
            // 
            // appChatbox
            // 
            resources.ApplyResources(this.appChatbox, "appChatbox");
            this.appChatbox.Name = "appChatbox";
            // 
            // myMainMenu
            // 
            resources.ApplyResources(this.myMainMenu, "myMainMenu");
            this.myMainMenu.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.myMainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.myMainMenu.Name = "myMainMenu";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.fileToolStripMenuItem.DropDown = this.FileContextMenuStrip;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            // 
            // FileContextMenuStrip
            // 
            this.FileContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem1,
            this.toolStripSeparator2,
            this.MmImportTeamboxesToolStripMenuItem,
            this.MmExportTeamboxesToolStripMenuItem,
            this.toolStripSeparator1,
            this.MmExitToolStripMenuItem});
            this.FileContextMenuStrip.Name = "FileContextMenuStrip";
            this.FileContextMenuStrip.OwnerItem = this.fileToolStripMenuItem;
            this.FileContextMenuStrip.ShowImageMargin = false;
            resources.ApplyResources(this.FileContextMenuStrip, "FileContextMenuStrip");
            // 
            // newToolStripMenuItem1
            // 
            this.newToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MmNewKwsToolStripMenuItem,
            this.MmNewFolderToolStripMenuItem});
            this.newToolStripMenuItem1.Name = "newToolStripMenuItem1";
            resources.ApplyResources(this.newToolStripMenuItem1, "newToolStripMenuItem1");
            // 
            // MmNewKwsToolStripMenuItem
            // 
            this.MmNewKwsToolStripMenuItem.Name = "MmNewKwsToolStripMenuItem";
            resources.ApplyResources(this.MmNewKwsToolStripMenuItem, "MmNewKwsToolStripMenuItem");
            this.MmNewKwsToolStripMenuItem.Click += new System.EventHandler(this.MmNewKwsToolStripMenuItem_Click);
            // 
            // MmNewFolderToolStripMenuItem
            // 
            this.MmNewFolderToolStripMenuItem.Name = "MmNewFolderToolStripMenuItem";
            resources.ApplyResources(this.MmNewFolderToolStripMenuItem, "MmNewFolderToolStripMenuItem");
            this.MmNewFolderToolStripMenuItem.Click += new System.EventHandler(this.MmNewFolderToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // MmImportTeamboxesToolStripMenuItem
            // 
            this.MmImportTeamboxesToolStripMenuItem.Name = "MmImportTeamboxesToolStripMenuItem";
            resources.ApplyResources(this.MmImportTeamboxesToolStripMenuItem, "MmImportTeamboxesToolStripMenuItem");
            this.MmImportTeamboxesToolStripMenuItem.Click += new System.EventHandler(this.Import_Click);
            // 
            // MmExportTeamboxesToolStripMenuItem
            // 
            this.MmExportTeamboxesToolStripMenuItem.Name = "MmExportTeamboxesToolStripMenuItem";
            resources.ApplyResources(this.MmExportTeamboxesToolStripMenuItem, "MmExportTeamboxesToolStripMenuItem");
            this.MmExportTeamboxesToolStripMenuItem.Click += new System.EventHandler(this.Export_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // MmExitToolStripMenuItem
            // 
            this.MmExitToolStripMenuItem.Name = "MmExitToolStripMenuItem";
            resources.ApplyResources(this.MmExitToolStripMenuItem, "MmExitToolStripMenuItem");
            this.MmExitToolStripMenuItem.Click += new System.EventHandler(this.OnFileExitClick);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDown = this.ToolsContextMenuStrip;
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            resources.ApplyResources(this.toolsToolStripMenuItem, "toolsToolStripMenuItem");
            // 
            // ToolsContextMenuStrip
            // 
            this.ToolsContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MmOptionsToolStripMenuItem});
            this.ToolsContextMenuStrip.Name = "ToolsContextMenuStrip";
            this.ToolsContextMenuStrip.OwnerItem = this.toolsToolStripMenuItem;
            this.ToolsContextMenuStrip.ShowImageMargin = false;
            resources.ApplyResources(this.ToolsContextMenuStrip, "ToolsContextMenuStrip");
            // 
            // MmOptionsToolStripMenuItem
            // 
            this.MmOptionsToolStripMenuItem.Name = "MmOptionsToolStripMenuItem";
            resources.ApplyResources(this.MmOptionsToolStripMenuItem, "MmOptionsToolStripMenuItem");
            this.MmOptionsToolStripMenuItem.Click += new System.EventHandler(this.OnToolsOptionsFormClick);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDown = this.AboutContextMenuStrip;
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
            // 
            // AboutContextMenuStrip
            // 
            this.AboutContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MmConfigurationWizardToolStripMenuItem,
            this.MmDebuggingConsoleToolStripMenuItem,
            this.toolStripSeparator3,
            this.MmAboutToolStripMenuItem});
            this.AboutContextMenuStrip.Name = "AboutContextMenuStrip";
            this.AboutContextMenuStrip.OwnerItem = this.helpToolStripMenuItem;
            this.AboutContextMenuStrip.ShowImageMargin = false;
            resources.ApplyResources(this.AboutContextMenuStrip, "AboutContextMenuStrip");
            // 
            // MmConfigurationWizardToolStripMenuItem
            // 
            this.MmConfigurationWizardToolStripMenuItem.Name = "MmConfigurationWizardToolStripMenuItem";
            resources.ApplyResources(this.MmConfigurationWizardToolStripMenuItem, "MmConfigurationWizardToolStripMenuItem");
            this.MmConfigurationWizardToolStripMenuItem.Click += new System.EventHandler(this.MmConfigurationWizardToolStripMenuItem_Click);
            // 
            // MmDebuggingConsoleToolStripMenuItem
            // 
            this.MmDebuggingConsoleToolStripMenuItem.Name = "MmDebuggingConsoleToolStripMenuItem";
            resources.ApplyResources(this.MmDebuggingConsoleToolStripMenuItem, "MmDebuggingConsoleToolStripMenuItem");
            this.MmDebuggingConsoleToolStripMenuItem.Click += new System.EventHandler(this.OnShowConsoleFormClick);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // MmAboutToolStripMenuItem
            // 
            this.MmAboutToolStripMenuItem.Name = "MmAboutToolStripMenuItem";
            resources.ApplyResources(this.MmAboutToolStripMenuItem, "MmAboutToolStripMenuItem");
            this.MmAboutToolStripMenuItem.Click += new System.EventHandler(this.OnShowAboutFormClick);
            // 
            // ViewContextMenuStrip
            // 
            this.ViewContextMenuStrip.Name = "ViewContextMenuStrip";
            this.ViewContextMenuStrip.ShowCheckMargin = true;
            this.ViewContextMenuStrip.ShowImageMargin = false;
            resources.ApplyResources(this.ViewContextMenuStrip, "ViewContextMenuStrip");
            // 
            // trayIcon
            // 
            this.trayIcon.ContextMenuStrip = this.trayIconMenu;
            resources.ApplyResources(this.trayIcon, "trayIcon");
            this.trayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.trayIcon_MouseDoubleClick);
            // 
            // trayIconMenu
            // 
            this.trayIconMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.trayOpenToolStripMenuItem,
            this.toolStripSeparator4,
            this.trayExitToolStripMenuItem});
            this.trayIconMenu.Name = "trayIconMenu";
            this.trayIconMenu.ShowImageMargin = false;
            resources.ApplyResources(this.trayIconMenu, "trayIconMenu");
            // 
            // trayOpenToolStripMenuItem
            // 
            this.trayOpenToolStripMenuItem.Name = "trayOpenToolStripMenuItem";
            resources.ApplyResources(this.trayOpenToolStripMenuItem, "trayOpenToolStripMenuItem");
            this.trayOpenToolStripMenuItem.Click += new System.EventHandler(this.trayOpenToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // trayExitToolStripMenuItem
            // 
            this.trayExitToolStripMenuItem.Name = "trayExitToolStripMenuItem";
            resources.ApplyResources(this.trayExitToolStripMenuItem, "trayExitToolStripMenuItem");
            this.trayExitToolStripMenuItem.Click += new System.EventHandler(this.trayExitToolStripMenuItem_Click);
            // 
            // BottomToolStripPanel
            // 
            resources.ApplyResources(this.BottomToolStripPanel, "BottomToolStripPanel");
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // TopToolStripPanel
            // 
            resources.ApplyResources(this.TopToolStripPanel, "TopToolStripPanel");
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // RightToolStripPanel
            // 
            resources.ApplyResources(this.RightToolStripPanel, "RightToolStripPanel");
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // LeftToolStripPanel
            // 
            resources.ApplyResources(this.LeftToolStripPanel, "LeftToolStripPanel");
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            // 
            // ContentPanel
            // 
            resources.ApplyResources(this.ContentPanel, "ContentPanel");
            // 
            // kasStatusBar
            // 
            this.kasStatusBar.Name = "kasStatusBar";
            resources.ApplyResources(this.kasStatusBar, "kasStatusBar");
            // 
            // toolStripDropDownButton1
            // 
            resources.ApplyResources(this.toolStripDropDownButton1, "toolStripDropDownButton1");
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.ShowDropDownArrow = false;
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // ProgressText
            // 
            resources.ApplyResources(this.ProgressText, "ProgressText");
            this.ProgressText.Name = "ProgressText";
            // 
            // Progress
            // 
            this.Progress.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.Progress.Name = "Progress";
            resources.ApplyResources(this.Progress, "Progress");
            // 
            // frmMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.toolStripContainer1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.myMainMenu;
            this.Name = "frmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.SizeChanged += new System.EventHandler(this.frmMain_SizeChanged);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.VisibleChanged += new System.EventHandler(this.frmMain_VisibleChanged);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.LocationChanged += new System.EventHandler(this.frmMain_LocationChanged);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.splitContainerLeft.Panel1.ResumeLayout(false);
            this.splitContainerLeft.Panel2.ResumeLayout(false);
            this.splitContainerLeft.ResumeLayout(false);
            this.splitContainerWsMembers.Panel1.ResumeLayout(false);
            this.splitContainerWsMembers.Panel1.PerformLayout();
            this.splitContainerWsMembers.Panel2.ResumeLayout(false);
            this.splitContainerWsMembers.Panel2.PerformLayout();
            this.splitContainerWsMembers.ResumeLayout(false);
            this.tvWorkspacesContextMenu.ResumeLayout(false);
            this.lstUsersContextMenu.ResumeLayout(false);
            this.splitContainerRight.Panel1.ResumeLayout(false);
            this.splitContainerRight.Panel2.ResumeLayout(false);
            this.splitContainerRight.ResumeLayout(false);
            this.ApplicationsTabControl.ResumeLayout(false);
            this.tblHome.ResumeLayout(false);
            this.panelHome.ResumeLayout(false);
            this.panelHome.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSecure)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tblFileSharing.ResumeLayout(false);
            this.tblScreenShare.ResumeLayout(false);
            this.myMainMenu.ResumeLayout(false);
            this.myMainMenu.PerformLayout();
            this.FileContextMenuStrip.ResumeLayout(false);
            this.ToolsContextMenuStrip.ResumeLayout(false);
            this.AboutContextMenuStrip.ResumeLayout(false);
            this.trayIconMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayIconMenu;
        private System.Windows.Forms.ToolStripMenuItem trayOpenToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem trayExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
        private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
        private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
        private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
        private System.Windows.Forms.ToolStripContentPanel ContentPanel;
        private System.Windows.Forms.ToolStripStatusLabel kasStatusBar;
        private System.Windows.Forms.ContextMenuStrip tvWorkspacesContextMenu;
        private System.Windows.Forms.ToolStripMenuItem CmWorkOnline;
        private System.Windows.Forms.ToolStripMenuItem CmWorkOffline;
        private System.Windows.Forms.ToolStripSeparator separator1;
        private System.Windows.Forms.ToolStripMenuItem CmDelete;
        private System.Windows.Forms.SplitContainer splitContainerLeft;
        private System.Windows.Forms.SplitContainer splitContainerWsMembers;
        public System.Windows.Forms.ListView lstUsers;
        private System.Windows.Forms.SplitContainer splitContainerRight;
        private kwm.KwmAppControls.AppChatboxControl appChatbox;
        private System.Windows.Forms.TabControl ApplicationsTabControl;
        private System.Windows.Forms.TabPage tblHome;
        private System.Windows.Forms.TabPage tblScreenShare;
        private kwm.KwmAppControls.AppScreenSharingControl appAppSharing;
        private System.Windows.Forms.TabPage tblFileSharing;
        private kwm.KwmAppControls.AppKfs.AppKfsControl appKfsControl;
        private System.Windows.Forms.ToolStripStatusLabel ProgressText;
        private System.Windows.Forms.ToolStripProgressBar Progress;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private kwm.Utils.KTreeView tvWorkspaces;
        private System.Windows.Forms.ToolStripMenuItem CmExpand;
        private System.Windows.Forms.ToolStripMenuItem CmCollapse;
        private System.Windows.Forms.ToolStripMenuItem CmRenameKws;
        private System.Windows.Forms.ToolStripMenuItem CmCreateNewFolder;
        private System.Windows.Forms.ToolStripSeparator separator2;
        private System.Windows.Forms.Panel panelHome;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblByName;
        private System.Windows.Forms.Label lblByOrgName;
        private System.Windows.Forms.Label lblCreationDate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblCreation;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblKasError;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button KwsTaskBtn;
        private System.Windows.Forms.Label lblKAS;
        private System.Windows.Forms.Label KwsStatus;
        private System.Windows.Forms.Label lblName;
        private NoKwsPane noSelectionPane;
        private System.Windows.Forms.MenuStrip myMainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip FileContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MmImportTeamboxesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MmExportTeamboxesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MmExitToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip ToolsContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MmOptionsToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip AboutContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MmAboutToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip ViewContextMenuStrip;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.Button btnNewKws;
        private System.Windows.Forms.ContextMenuStrip lstUsersContextMenu;
        private System.Windows.Forms.ToolStripMenuItem ResetPassword;
        private System.Windows.Forms.TextBox txtInvite;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnInvite;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem MmNewKwsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MmNewFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MmConfigurationWizardToolStripMenuItem;
        private System.Windows.Forms.PictureBox picSecure;
        private System.Windows.Forms.Label lblSecureNote;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem MmDebuggingConsoleToolStripMenuItem;
        private System.Windows.Forms.LinkLabel lnkInvite;
        private System.Windows.Forms.ToolStripMenuItem ResendInvitation;
        private System.Windows.Forms.ToolStripMenuItem Copy;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem Delete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem ShowProperties;
        private System.Windows.Forms.ToolStripMenuItem CmAdvanced;
        private System.Windows.Forms.ToolStripMenuItem CmRebuild;
        private System.Windows.Forms.ToolStripMenuItem CmExport;
        private System.Windows.Forms.ToolStripMenuItem CmDisable;
        private System.Windows.Forms.ToolStripMenuItem CmProperties;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem CmRenameFolder;
    }
}

