using System;
using System.Drawing;
using System.Windows.Forms;
using kwm.Utils;
using System.Collections;
using kwm.KwmAppControls;
using kwm.KwmAppControls.AppKfs;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Win32;
using System.IO;
using System.Media;
using Tbx.Utils;

/* Methods used to modify the UI elements.
 * NOTICE : these methods assume the caller is running
 * in the UI thread. This is a convention that is not enforced
 * by this code. It is forbidden to access UI elements from another
 * thread than the one that created the elements.
 */

namespace kwm
{
    public partial class frmMain : frmKBaseForm
    {
        private ToolStripItem itemClicked = null;
        /// <summary>
        /// Reference to the UI broker.
        /// </summary>
        private WmUiBroker m_uiBroker;

        /// <summary>
        /// Log messages to a file.
        /// </summary>
        private CFileLogger m_fileLogger;

        /// <summary>
        /// Reference to the console window which display logged messages.
        /// </summary>
        private ConsoleWindow m_consoleWindow;

        /// <summary>
        /// Handle the generation of balloon notifications.
        /// </summary>
        private TrayIconNotifier m_trayNotifier;

        /// <summary>
        /// Used to change icon at each second for notification when the UI is 
        /// not visible.
        /// </summary>
        private Timer m_blinkingTimer;

        /// <summary>
        /// Track the current icon for the flashing sequence.
        /// </summary>
        private int m_currentTrayIcon = 0;

        /// <summary>
        /// Tree mapping applications IDs to application controls.
        /// </summary>
        private SortedDictionary<UInt32, BaseAppControl> m_appControlTree;

        /// <summary>
        /// Set to true if /minimized is passed on command line, 
        /// false otherwise. Check frmMain_VisibleChanged() event handler.
        /// </summary>
        private bool m_startMinimized = false;

        /// <summary>
        /// Set this flag when we explicitly want to shutdown the application.
        /// As long as it is set to false, form_closing will cancel the close.
        /// </summary>
        private bool m_exitFlag = false;

        /// <summary>
        /// Used to track what was the previous window state when handling the 
        /// SizeChanged event.
        /// </summary>
        private FormWindowState m_lastWindowState;

        /// <summary>
        /// Used to track whether we should save the various main form
        /// window settings on LocationChanged and SizeChanged events.
        /// These events are fired while we are still in the constructor and we
        /// must ignore them until the constructor is done.
        /// </summary>
        private bool m_saveWindowSettingsFlag = false;

        /// <summary>
        /// When we're updating a selection in the UI, or when we're dragging
        /// and dropping workspaces in the TreeView, we do not want to update
        /// the UI whenever the selection changes. This flag is set to true 
        /// when selection event must be ignored.   
        /// </summary>
        private bool m_ignoreSelectFlag = false;

        /// <summary>
        /// Information about the drag and drop operation in tvWorkspaces, if any.
        /// </summary>
        private TvWorkspacesDragInfo m_tvWorkspacesDragInfo = new TvWorkspacesDragInfo();

        /// <summary>
        /// Prevent reentrance in the SizeChanged event handler of the main form.
        /// </summary>
        private bool m_SizeChangedHandlerFlag = false;

        /// <summary>
        /// Last task associated to the workspace task button in the home tab.
        /// </summary>
        private KwsTask m_kwsTaskBtnTask = KwsTask.WorkOnline;

        /// <summary>
        /// Reference to the workspace browser.
        /// </summary>
        public KwsBrowser Browser { get { return m_uiBroker.Browser; } }

        /// <summary>
        /// True if the browser tree does not match tvWorkspaces.
        /// </summary>
        public bool StaleBrowser { get { return m_uiBroker.RebuildBrowserFlag; } }

        /// <summary>
        /// Array containing all the required icons to create the Tray Icon 
        /// blinking animation.
        /// </summary>
        private Icon[] m_blinkingIcons = new Icon[16];

        public SortedDictionary<UInt32, BaseAppControl> AppControlTree
        {
            get { return m_appControlTree; }
        }

        /// <summary>
        /// Initialize the main form.
        /// </summary>
        public void Initialize(WmUiBroker broker)
        {
            m_uiBroker = broker;

            // Initialize the resource manager. This cannot be done before
            // this form is loaded.
            Misc.InitResourceMngr(typeof(frmMain).Assembly);

            // Save the main form handle so that another instance of KWM can
            // send messages to it. There is a race condition here that is
            // difficult to fix.
            RegistryKey key = Registry.CurrentUser.CreateSubKey(Base.GetKwmRegKey());
            key.SetValue("kwmWindowHandle", Handle, RegistryValueKind.String);

            InitializeComponent();
            this.AllowDrop = true;
            ApplicationsTabControl.AllowDrop = true;
            tblFileSharing.AllowDrop = true;

            InitializeImages();

            m_trayNotifier = new TrayIconNotifier(trayIcon);

            ApplyOptions();

            // Set some visual controls properties.
            CenterTitleLabel();
            lstUsers.Columns.Add(Misc.GetString("Members"), -2);

            InitializeApplications();

            Logging.Log("KWM started.");

            // Handle minimization.
            this.MinimumSize = this.Size;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "/minimized")
                {
                    m_startMinimized = true;
                    break;
                }
            }

            // Initialize our blinking array.
            for (int i = 0; i < 16; i++)
            {
                String obj = "anim" + (i < 10 ? "0" : "") + i;
                Bitmap b = (Bitmap)kwm.Properties.Resources.ResourceManager.GetObject(obj);
                m_blinkingIcons[i] = Misc.MakeIcon(b, 16, true);
            }

            trayIcon.Icon = m_blinkingIcons[0];

            // Handle blinking icons.
            m_blinkingTimer = new Timer();
            m_blinkingTimer.Interval = 100;
            m_blinkingTimer.Tick += new EventHandler(TrayIconFlashTimer_Tick);
            this.Activated += new EventHandler(frmMain_Activated);

            // Restore the window state.
            m_lastWindowState = this.WindowState;
            RestoreMainFormSettings();

            noSelectionPane.UiBroker = broker;
            // SaveWindowSettingsFlag will be set to true in the frmMain_Shown event handler.
        }

        /// <summary>
        /// Initialize the list of images used by the KWM.
        /// </summary>
        private void InitializeImages()
        {
            tvWorkspaces.ImageList = new ImageList();
            tvWorkspaces.ImageList.ColorDepth = ColorDepth.Depth32Bit;
            tvWorkspaces.ImageList.Images.Add(Color.Green.Name, kwm.Properties.Resources.work_online_green_16x16);
            tvWorkspaces.ImageList.Images.Add(Color.Red.Name, kwm.Properties.Resources.disabled_red_16x16);
            tvWorkspaces.ImageList.Images.Add(Color.Gray.Name, kwm.Properties.Resources.work_offline_gray_16x16);
            tvWorkspaces.ImageList.Images.Add(Color.Blue.Name, kwm.Properties.Resources.connecting_blue_16x16);

            Bitmap img = kwm.Properties.Resources.folderClosed;
            img.MakeTransparent();
            tvWorkspaces.ImageList.Images.Add("FolderClosed", img);

            img = kwm.Properties.Resources.folderOpened;
            img.MakeTransparent();
            tvWorkspaces.ImageList.Images.Add("FolderOpened", img);
        }

        /// <summary>
        /// Initialize the workspace application controls.
        /// </summary>
        private void InitializeApplications()
        {
            m_appControlTree = new SortedDictionary<UInt32, BaseAppControl>();
            m_appControlTree.Add(KAnpType.KANP_NS_CHAT, appChatbox);
            m_appControlTree.Add(KAnpType.KANP_NS_VNC, appAppSharing);
            m_appControlTree.Add(KAnpType.KANP_NS_KFS, appKfsControl);
            m_uiBroker.UpdateAppControlDataSource(null);
        }

        /// <summary>
        /// Save all the relevant window settings in order to restore the window
        /// just like it was when the program was shut down.
        /// </summary>
        private void SaveMainFormSettings()
        {
            Logging.Log(1, "SaveMainFormSettings called. WindowState : " + this.WindowState);
            Misc.ApplicationSettings.MainWindowState = this.WindowState;

            // MainWindowPosition and MainWindowSize are updated in real time
            // from the various event handlers.
            Misc.ApplicationSettings.MainWindow_SplitPanelLeftSplitSize = splitContainerLeft.SplitterDistance;
            Misc.ApplicationSettings.MainWindow_SplitPanelWsMembersSplitSize = splitContainerWsMembers.SplitterDistance;
            Misc.ApplicationSettings.Save();
        }

        /// <summary>
        /// Restore the window state using the saved settings.
        /// </summary>
        private void RestoreMainFormSettings()
        {
            // If the location is set to an invalid value, force it to a valid one.
            if (Misc.ApplicationSettings.MainWindowPosition.X < 0 || Misc.ApplicationSettings.MainWindowPosition.Y < 0)
                this.Location = new Point(0, 0);
            else
                this.Location = Misc.ApplicationSettings.MainWindowPosition;

            if (m_startMinimized)
                this.WindowState = FormWindowState.Minimized;
            else
                this.WindowState = Misc.ApplicationSettings.MainWindowState;

            this.Size = Misc.ApplicationSettings.MainWindowSize;

            this.splitContainerLeft.SplitterDistance = Misc.ApplicationSettings.MainWindow_SplitPanelLeftSplitSize;
            this.splitContainerWsMembers.SplitterDistance = Misc.ApplicationSettings.MainWindow_SplitPanelWsMembersSplitSize;
        }

        /// <summary>
        /// Called on startup and when the user changes its options.
        /// </summary>
        private void ApplyOptions()
        {
            bool debugFlag = Misc.ApplicationSettings.KwmEnableDebugging;
            bool fileFlag = Misc.ApplicationSettings.KwmLogToFile && debugFlag;

            // Adjust the logging stuff.
            if (fileFlag && m_fileLogger == null)
            {
                m_fileLogger = new CFileLogger(Misc.GetKwmLogFilePath(), "kwm-" + Base.GetLogFileName());
                Logging.RegisterLogHandler(m_fileLogger);
            }

            else if (!fileFlag && m_fileLogger != null)
            {
                m_fileLogger.CloseAndUnregister();
                m_fileLogger = null;
            }

            Logging.SetLoggingLevel(debugFlag ? LoggingLevel.Debug : LoggingLevel.Normal);
        }

        /// <summary>
        /// Make the KWM visible if it is minimized or not in the foreground.
        /// </summary>
        public void ShowMainForm()
        {
            Logging.Log("ShowMainForm() called.");

            StopBlinking();

            if (Syscalls.IsIconic(this.Handle))
                Syscalls.ShowWindowAsync(this.Handle, (int)Syscalls.WindowStatus.SW_RESTORE);

            this.Visible = true;
            Syscalls.SetForegroundWindow(this.Handle);
            this.Activate();
        }

        /// <summary>
        /// Updates the form's UI (Home tab info, title bar, workspace users).
        /// </summary>
        public void UpdateUI(Workspace kws)
        {
            SetHomeTabInfos(kws);

            // Set the title bar.
            if (kws == null)
            {
                this.Text = Base.GetKwmString();
            }

            else
            {
                this.Text = Base.GetKwmString() + " - " + kws.GetKwsName() + " (" + Base.GetEnumDescription(kws.LoginStatus) + ")";
            }

            // Update the invitation controls status.
            UpdateInviteStatus(kws);

            // Set the user list.
            UpdateKwsUserList(kws);
        }

        /// <summary>
        /// Update the invitation UI (button, textbox and linklabel) so that
        /// the controls are enabled at the right moment.
        /// </summary>
        private void UpdateInviteStatus(Workspace kws)
        {
            if (kws == null)
            {
                txtInvite.Enabled = false;
                btnInvite.Enabled = false;
                lnkInvite.Enabled = false;
                return;
            }

            bool enable = (kws.LoginStatus == KwsLoginStatus.LoggedIn &&
                           kws.IsOnlineCapable() &&
                          !kws.IsPublicKws());

            txtInvite.Enabled = enable;
            btnInvite.Enabled = enable;
            lnkInvite.Enabled = enable;
        }

        private void SetHomeTabInfos(Workspace kws)
        {
            if (kws == null)
            {
                panelHome.Visible = false;
                return;
            }

            panelHome.Visible = true;
            lblKAS.Text = kws.Kas.KasID.Host + ":" + kws.Kas.KasID.Port + " (ID: " + kws.GetExternalKwsID().ToString() + ")";

            if (kws.AppException != null)
            {
                lblKasError.Text = Base.FormatErrorMsg("application error", kws.AppException);
            }

            else if (kws.Kas.ErrorEx != null)
            {
                lblKasError.Text = Base.FormatErrorMsg("KAS error", kws.Kas.ErrorEx);
            }

            else if ((kws.Sm.GetCurrentTask() != KwsTask.WorkOnline ||
                     (kws.Kas.ConnStatus != KasConnStatus.Connected &&
                      kws.Kas.ConnStatus != KasConnStatus.Connecting)) &&
                     (kws.KasLoginHandler.LoginResult != KwsLoginResult.Accepted &&
                      kws.KasLoginHandler.LoginResult != KwsLoginResult.None))
            {
                lblKasError.Text = Base.FormatErrorMsg("login error", kws.KasLoginHandler.LoginResultString);

                if (kws.KasLoginHandler.LoginResult == KwsLoginResult.BadSecurityCreds &&
                    kws.KasLoginHandler.TicketRefusalString != "")
                {
                    lblKasError.Text += Environment.NewLine + Base.FormatErrorMsg(kws.KasLoginHandler.TicketRefusalString);
                }
            }

            else
            {
                lblKasError.Text = "";
            }

            // Determine the status to display and the task the user would
            // most likely want to undertake.
            KwsTask curTask = kws.Sm.GetCurrentTask();
            String statusText = "";
            bool btnEnabled = false;
            KwsTask btnTask = KwsTask.WorkOnline;
            String btnText = "Work online";

            if (curTask == KwsTask.Spawn)
            {
                statusText = "Creating " + Base.GetKwsString();
            }

            else if (curTask == KwsTask.Delete)
            {
                statusText = "Deleting " + Base.GetKwsString();
            }

            else if (curTask == KwsTask.Rebuild)
            {
                statusText = "Rebuilding " + Base.GetKwsString();
            }

            else if (curTask == KwsTask.Stop)
            {
                if (kws.MainStatus == KwsMainStatus.RebuildRequired)
                {
                    statusText = "Rebuild required";
                    btnEnabled = true;
                    btnTask = KwsTask.Rebuild;
                    btnText = "Rebuild " + Base.GetKwsString();
                }

                // Assume the workspace was disabled voluntarily and that
                // the user can work online. This is normally the case.
                else
                {
                    statusText = Base.GetKwsString() + " disabled";
                    btnEnabled = true;
                }
            }

            else if (curTask == KwsTask.WorkOffline)
            {
                statusText = "Working offline";
                btnEnabled = true;
            }

            else if (curTask == KwsTask.WorkOnline)
            {
                if (kws.IsOnlineCapable())
                {
                    statusText = "Working online";
                    btnEnabled = true;
                    btnTask = KwsTask.WorkOffline;
                    btnText = "Work offline";
                }

                // We're not currently connecting but we can request to.
                else if (kws.Sm.CanWorkOnline())
                {
                    statusText = "Working offline";
                    btnEnabled = true;
                }

                // We're connecting, allow disconnection.
                else
                {
                    statusText = "Connecting";
                    btnEnabled = true;
                    btnTask = KwsTask.WorkOffline;
                    btnText = "Cancel";
                }
            }

            // Update the information.
            KwsStatus.Text = statusText;
            KwsStatus.ForeColor = m_uiBroker.Browser.GetKwsNodeByKws(kws).GetKwsIconImageKey();
            KwsTaskBtn.Enabled = btnEnabled;
            KwsTaskBtn.Text = btnText;
            m_kwsTaskBtnTask = btnTask;

            // Set the workspace general information.
            picSecure.Visible = kws.CoreData.Credentials.SecureFlag;
            lblSecureNote.Visible = kws.CoreData.Credentials.SecureFlag;
            lblName.Text = kws.GetKwsName();

            KwsUser creator = kws.CoreData.UserInfo.Creator;
            if (creator != null)
            {
                lblByName.Text = creator.UiFullName;
                lblByOrgName.Text = creator.OrgName;
                lblCreationDate.Text = Base.KDateToDateTime(creator.InvitationDate).ToString();
            }

            else
            {
                lblByName.Text = lblByOrgName.Text = lblCreationDate.Text = "";
            }
        }

        /// <summary>
        /// Select the appropriate user in lstUsers.
        /// </summary>
        public void UpdateKwsUserSelection()
        {
            m_ignoreSelectFlag = true;

            // Reselect the previously selected user, if any.
            KwsBrowserKwsNode bNode = Browser.GetSelectedKwsNode();
            if (bNode != null && bNode.SelectedUser != null)
                lstUsers.Items[bNode.SelectedUser.UserID.ToString()].Selected = true;

            m_ignoreSelectFlag = false;
        }

        /// <summary>
        /// Update the workspace user list with the information from the
        /// workspace specified, if any.
        /// </summary>
        private void UpdateKwsUserList(Workspace kws)
        {
            lstUsers.BeginUpdate();

            lstUsers.Items.Clear();
            lstUsers.Enabled = (kws != null);

            if (kws != null)
            {
                // We want to display the user display name preferably.
                // If a dupe exists, show name and email address, and if no name
                // is present, just show the email address.
                foreach (KwsUser user in kws.CoreData.UserInfo.UserTree.Values)
                {
                    String userName = user.UiSimpleName;

                    UInt32 dupeID = IsNamePresent(userName);
                    if (dupeID != 0)
                    {
                        // If this username already exists in the UI, use the FullName instead.
                        userName = user.UiFullName;

                        // Go back to the initial dupe and change its display name to the FullName.
                        lstUsers.Items[dupeID.ToString()].Text = kws.CoreData.UserInfo.UserTree[dupeID].UiFullName;
                    }

                    ListViewItem item = new ListViewItem();
                    item.Name = user.UserID.ToString();
                    item.Text = userName;
                    item.ToolTipText = user.UiTooltipText;

                    // Mark ourself in a different way.
                    if (user.UserID == kws.CoreData.Credentials.UserID)
                    {
                        item.Font = new Font(item.Font, FontStyle.Bold);
                    }

                    lstUsers.Items.Add(item);
                }


                // Sort the list in alphabetical order.
                lstUsers.Sorting = SortOrder.Ascending;
                lstUsers.Sort();

                // Put the logged on user on top of the list.
                lstUsers.Sorting = SortOrder.None;
                ListViewItem i = lstUsers.Items[kws.CoreData.Credentials.UserID.ToString()];
                if (i != null)
                {
                    i.Remove();
                    lstUsers.Items.Insert(0, i);
                }

                UpdateKwsUserSelection();
            }

            lstUsers.EndUpdate();
        }

        /// <summary>
        /// Return the userID of a KwsUser if the username is already displayed 
        /// in the member list, 0 otherwise.
        /// </summary>
        public UInt32 IsNamePresent(String userName)
        {
            foreach (ListViewItem i in lstUsers.Items)
                if (i.Text == userName) return UInt32.Parse(i.Name);

            return 0;
        }

        /// <summary>
        /// Select the appropriate tree node in tvWorkspaces.
        /// </summary>
        public void UpdateTvWorkspacesSelection()
        {
            m_ignoreSelectFlag = true;

            TreeNode nodeToSelect = null;

            // Reselect the previously selected folder.
            if (Browser.SelectedFolder != null)
                nodeToSelect = GetTvWorkspacesTreeNodeByPath(Browser.SelectedFolder.FullPath);

            // Reselect the selected workspace.
            if (nodeToSelect == null && Browser.SelectedKws != null)
                nodeToSelect = GetTvWorkspacesTreeNodeByPath(Browser.GetSelectedKwsNode().FullPath);

            // Select the primary folder.
            if (nodeToSelect == null)
            {
                Browser.SelectedFolder = Browser.PrimaryFolder;
                nodeToSelect = GetTvWorkspacesTreeNodeByPath(Browser.SelectedFolder.FullPath);
            }

            // Update the selection in the UI.
            Debug.Assert(nodeToSelect != null);
            tvWorkspaces.SelectedNode = nodeToSelect;

            m_ignoreSelectFlag = false;
        }

        /// <summary>
        /// Update tvWorkspaces with the information contained in the browser.
        /// If rebuildFlag is true, then tvWorkspaces is stale and it will be 
        /// repopulated. Rebuilding should be avoided as much as possible 
        /// because the UI is so slow that selection errors occur. If
        /// reselectFlag is true, the previously selected folder / workspace
        /// will be reselected as much as possible on rebuild.
        /// </summary>
        public void UpdateTvWorkspacesList(bool rebuildFlag, bool reselectFlag)
        {
            tvWorkspaces.BeginUpdate();

            // Get the current scroll position.
            Point scrollPos = tvWorkspaces.GetScrollPos();

            if (rebuildFlag)
            {
                // Rebuild the treeview.
                tvWorkspaces.Nodes.Clear();
                AddTvWorkspacesChildNodes(Browser.RootNode, tvWorkspaces.Nodes);
            }

            else
            {
                ArrayList nodes = new ArrayList();
                Browser.RootNode.GetSubtreeNodes(nodes);
                foreach (KwsBrowserNode bNode in nodes)
                {
                    TreeNode tNode = GetTvWorkspacesTreeNodeByPath(bNode.FullPath);
                    if (tNode != null) UpdateTvWorkspacesTreeNode(tNode, bNode);
                }
            }

            tvWorkspaces.EndUpdate();

            // Reselect a tree node if possible. Do NOT call this code before
            // EndUpdate() has been called, it won't scroll properly.
            if (rebuildFlag && reselectFlag)
            {
                tvWorkspaces.BeginUpdate();
                UpdateTvWorkspacesSelection();
                tvWorkspaces.SetScrollPos(scrollPos);
                tvWorkspaces.EndUpdate();
            }
        }

        /// <summary>
        /// Insert the children of the parent folder specified recursively.
        /// </summary>
        private void AddTvWorkspacesChildNodes(KwsBrowserFolderNode bParent, TreeNodeCollection tnc)
        {
            foreach (KwsBrowserNode bNode in bParent.GetNodes())
            {
                KwsBrowserKwsNode bKwsNode = bNode as KwsBrowserKwsNode;
                KwsBrowserFolderNode bFolderNode = bNode as KwsBrowserFolderNode;
                if (bKwsNode != null && !bKwsNode.Kws.IsDisplayable()) continue;

                TreeNode tNode = CreateTvWorkspacesTreeNode(bNode);
                tnc.Add(tNode);
                if (bFolderNode != null)
                {
                    AddTvWorkspacesChildNodes(bFolderNode, tNode.Nodes);
                    if (bFolderNode.ExpandedFlag) tNode.Expand();
                }
            }
        }

        /// <summary>
        /// Return a TreeNode matching the specified browser node.
        /// </summary>
        private TreeNode CreateTvWorkspacesTreeNode(KwsBrowserNode bNode)
        {
            TreeNode tNode = new TreeNode();
            tNode.Name = bNode.ID;
            tNode.Tag = bNode.FullPath;
            UpdateTvWorkspacesTreeNode(tNode, bNode);
            return tNode;
        }

        /// <summary>
        /// Update the tree node information with the browser node specified.
        /// </summary>
        private void UpdateTvWorkspacesTreeNode(TreeNode tNode, KwsBrowserNode bNode)
        {
            String text = "";
            String imageKey = "";

            tNode.NodeFont = new Font(tvWorkspaces.Font, bNode.NotifyFlag ? FontStyle.Bold : FontStyle.Regular);
            tNode.BackColor = bNode.NotifyFlag ? Color.FromArgb(75, 255, 177, 0) : Color.White;

            if (bNode is KwsBrowserFolderNode)
            {
                KwsBrowserFolderNode folderNode = bNode as KwsBrowserFolderNode;
                text = folderNode.FolderName;
                imageKey = folderNode.ExpandedFlag ? "FolderOpened" : "FolderClosed";
            }

            else
            {
                KwsBrowserKwsNode kwsNode = bNode as KwsBrowserKwsNode;
                text = kwsNode.Kws.GetKwsName();
                imageKey = kwsNode.GetKwsIconImageKey().Name;
            }

            // Not sure if this is really necessary for performance; not taking
            // chances.
            if (tNode.Text != text) tNode.Text = text;
            if (tNode.ImageKey != imageKey) tNode.ImageKey = tNode.SelectedImageKey = imageKey;
        }

        /// <summary>
        /// Return the path to the currently selected node in tvWorkspaces, if
        /// any.
        /// </summary>
        private String GetTvWorkspacesSelectedPath()
        {
            if (tvWorkspaces.SelectedNode == null) return "";
            return (String)tvWorkspaces.SelectedNode.Tag;
        }

        /// <summary>
        /// Return the browser node corresponding to the currently selected 
        /// node in tvWorkspaces, if any.
        /// </summary>
        private KwsBrowserNode GetTvWorkspacesSelectedBrowserNode()
        {
            return Browser.GetNodeByPath(GetTvWorkspacesSelectedPath());
        }

        /// <summary>
        /// Return the tvWorkspaces node having the path specified, if any.
        /// </summary>
        private TreeNode GetTvWorkspacesTreeNodeByPath(String path)
        {
            TreeNodeCollection tnc = tvWorkspaces.Nodes;
            TreeNode curTreeNode = null;

            foreach (String ID in path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!tnc.ContainsKey(ID)) return null;
                curTreeNode = tnc[ID];
                tnc = curTreeNode.Nodes;
            }

            return curTreeNode;
        }

        /// <summary>
        /// Update the tabs for displaying the selected workspace, if any.
        /// </summary>
        public void UpdateKwsTabs()
        {
            bool publicFlag = (Browser.SelectedKws != null && Browser.SelectedKws.IsPublicKws());
            UpdateOneKwsTab(tblScreenShare, !publicFlag);
        }

        /// <summary>
        /// Hide or show one workspace application tab.
        /// </summary>
        private void UpdateOneKwsTab(TabPage tab, bool visibleFlag)
        {
            if (!visibleFlag && ApplicationsTabControl.TabPages.Contains(tab))
                ApplicationsTabControl.TabPages.Remove(tab);
            else if (visibleFlag && !ApplicationsTabControl.TabPages.Contains(tab))
                ApplicationsTabControl.TabPages.Add(tab);
        }

        /// <summary>
        /// Select the specified application tab.
        /// </summary>
        public void SetTabApplication(uint appNumber)
        {
            switch (appNumber)
            {
                case KAnpType.KANP_NS_CHAT: break;
                case KAnpType.KANP_NS_KFS: ApplicationsTabControl.SelectedTab = tblFileSharing; break;
                case KAnpType.KANP_NS_VNC: ApplicationsTabControl.SelectedTab = tblScreenShare; break;
                default: break;
            }
        }

        /// <summary>
        /// Initiate tray icon blinking animation.
        /// </summary>
        public void StartBlinking()
        {
            m_blinkingTimer.Start();
            trayIcon.Text = "New activity in your " + Base.GetKwmString();
        }

        /// <summary>
        /// Stop tray icon blinking animation.
        /// </summary>
        private void StopBlinking()
        {
            m_blinkingTimer.Stop();
            trayIcon.Icon = m_blinkingIcons[0];
            m_currentTrayIcon = 0;
            trayIcon.Text = Base.GetKwmString();
        }


        // frmMain event handlers.

        /// <summary>
        /// Called when the tray icon flash timer triggers.
        /// </summary>
        private void TrayIconFlashTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                m_blinkingTimer.Stop();

                trayIcon.Icon = m_blinkingIcons[m_currentTrayIcon];

                if (m_currentTrayIcon == 0)
                    System.Threading.Thread.Sleep(1000);

                m_currentTrayIcon = (m_currentTrayIcon + 1) % m_blinkingIcons.Length;

                m_blinkingTimer.Start();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void frmMain_LocationChanged(object sender, EventArgs e)
        {
            try
            {
                if (!m_saveWindowSettingsFlag || m_exitFlag) return;

                if (WindowState != FormWindowState.Minimized)
                {
                    // Only store sane values.
                    if (Location.X > 0 && Location.Y > 0)
                        Misc.ApplicationSettings.MainWindowPosition = this.Location;
                }

            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void frmMain_SizeChanged(object sender, EventArgs e)
        {
            if (m_SizeChangedHandlerFlag || m_exitFlag) return;

            try
            {
                m_SizeChangedHandlerFlag = true;

                if (this.WindowState == FormWindowState.Minimized)
                    Hide();

                if (!m_saveWindowSettingsFlag)
                    return;

                // WindowState has not changed and is Normal: this is just a Resize.
                if (m_lastWindowState == this.WindowState &&
                    m_lastWindowState == FormWindowState.Normal)
                {
                    Misc.ApplicationSettings.MainWindowSize = this.Size;
                }

                // The WindowState has changed.
                else if (m_lastWindowState != this.WindowState)
                {
                    // If going Minimized, remember in which state we
                    // must restore when we change from Minimized to anything else.
                    if (this.WindowState == FormWindowState.Minimized)
                        Misc.ApplicationSettings.MainWindowStateAfterMinimize = m_lastWindowState;

                    // In any case, remember in which state we must be when the 
                    // application starts again.
                    Misc.ApplicationSettings.MainWindowState = this.WindowState;
                }

                // Remember the new state.
                m_lastWindowState = this.WindowState;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                m_SizeChangedHandlerFlag = false;
            }
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("frmMain_Shown called. startMinimized = " + m_startMinimized);

                if (m_startMinimized)
                {
                    this.WindowState = FormWindowState.Minimized;
                    m_startMinimized = false;
                }
                StopBlinking();

                m_saveWindowSettingsFlag = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        void frmMain_Activated(object sender, EventArgs e)
        {
            try
            {
                if (m_exitFlag) return;

                StopBlinking();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// If the console window exists, hide or make it visible.
        /// </summary>
        private void frmMain_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (m_exitFlag) return;

                if (this.Visible || Focused)
                {
                    StopBlinking();
                }

                if (m_consoleWindow != null)
                {
                    if (this.Visible)
                    {
                        m_consoleWindow.Visible = MmDebuggingConsoleToolStripMenuItem.Checked;
                    }
                    else
                    {
                        m_consoleWindow.Hide();
                    }
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Logging.Log("frmMain_FormClosing");
                if (!m_exitFlag &&
                   e.CloseReason == CloseReason.UserClosing)
                {
                    Logging.Log("Setting WindowState to Minimized. Original one: " + WindowState);
                    e.Cancel = true;
                    Syscalls.ShowWindowAsync(this.Handle, (int)Syscalls.WindowStatus.SW_SHOWMINIMIZED);
                    return;
                }

                Logging.Log("CloseReason: " + e.CloseReason);

                // We're closing because the application is exiting.
                SaveMainFormSettings();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                ShowMainForm();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            try
            {
                lstUsers.Columns[0].Width = lstUsers.Width - 4;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void panel1_ClientSizeChanged(object sender, EventArgs e)
        {
            try
            {
                CenterTitleLabel();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Move the workspace name label to the center of its container,
        /// taking in consideration the picture of a lock in the case of
        /// a secure workspace.
        /// </summary>
        private void CenterTitleLabel()
        {
            int w = noSelectionPane.Width;
            int x = (w - lblName.Width) / 2;
            lblName.Location = new Point(x, lblName.Height);
        }

        private void lblName_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                CenterTitleLabel();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void Export_Click(object sender, EventArgs e)
        {
            try
            {
                string filename = GetExportFilePath("Export " + Base.GetKwsString() + " list as");

                if (filename != "")
                    m_uiBroker.RequestExportBrowser(filename, 0);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void Import_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dial = new System.Windows.Forms.OpenFileDialog();

                if (Misc.ApplicationSettings.ImportWsPath != "" && Directory.Exists(Misc.ApplicationSettings.ImportWsPath))
                    dial.InitialDirectory = Misc.ApplicationSettings.ImportWsPath;
                else
                    dial.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                dial.SupportMultiDottedExtensions = true;
                dial.Title = "Import " + Misc.GetString("Kwses");
                dial.Filter = Base.GetKwsString() + " list (*.wsl)|*.wsl|All files (*.*)|*.*";
                Misc.OnUiEntry();
                dial.ShowDialog(this);
                Misc.OnUiExit();

                if (dial.FileName != "")
                {
                    Misc.ApplicationSettings.ImportWsPath = Path.GetDirectoryName(dial.FileName);
                    Misc.ApplicationSettings.Save();
                    m_uiBroker.RequestImportBrowser(dial.FileName);
                }
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Called when the selection changes in tvWorkspaces. This is called
        /// both when the user genuinely selects a node and when we
        /// programmatically select a node. Normally this method should have
        /// no effect if we are the source of the selection.
        /// </summary>
        private void tvWorkspaces_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (m_ignoreSelectFlag) return;

            KwsBrowserNode bNode = m_uiBroker.Browser.GetNodeByPath((String)e.Node.Tag);
            if (bNode == null) return;

            if (bNode is KwsBrowserKwsNode)
                m_uiBroker.RequestSelectKws(((KwsBrowserKwsNode)bNode).Kws, false);
            else
                Browser.SelectedFolder = bNode as KwsBrowserFolderNode;
        }

        /// <summary>
        /// Helper for drag & drop. Return the treenode at the mouse position
        /// specified, if any.
        /// </summary>
        private TreeNode TvWorkspacesGetNodeByMousePos(int x, int y)
        {
            Point pos = tvWorkspaces.PointToClient(new Point(x, y));
            return tvWorkspaces.GetNodeAt(pos);
        }

        /// <summary>
        /// Helper for drag & drop. Update SrcNode from SrcPath. Return true if
        /// the source can be moved.
        /// </summary>
        private bool TvWorkspacesDragValidateSource()
        {
            TvWorkspacesDragInfo di = m_tvWorkspacesDragInfo;
            di.SrcNode = Browser.GetNodeByPath(di.SrcPath);
            if (di.SrcNode == null) return false;

            KwsBrowserFolderNode folder = di.SrcNode as KwsBrowserFolderNode;
            if (folder != null && folder.PermanentFlag) return false;

            return true;
        }

        /// <summary>
        /// Helper for drag & drop. Update the SrcNode, DstFolder and DstIndex 
        /// fields based on the destination tree node specified. Return true if
        /// the move is currently valid.
        /// </summary>
        private bool TvWorkspacesDragValidateDst(TreeNode tDstNode)
        {
            TvWorkspacesDragInfo di = m_tvWorkspacesDragInfo;

            // Revalidate the source.
            if (!TvWorkspacesDragValidateSource()) return false;

            // Get the destination node in the treeview and the browser, if any.
            KwsBrowserNode bDstNode = null;
            if (tDstNode != null) bDstNode = Browser.GetNodeByPath((String)tDstNode.Tag);

            // Cast the destination node to the appropriate type. If it is a
            // folder, the destination node is the destination folder.
            KwsBrowserKwsNode bDstKwsNode = bDstNode as KwsBrowserKwsNode;
            di.DstFolder = bDstNode as KwsBrowserFolderNode;

            // By default we insert at the beginning of the folder selected.
            di.DstIndex = 0;

            // We're moving a workspace.
            if (di.SrcNode is KwsBrowserKwsNode)
            {
                // Cannot move a workspace at the top level.
                if (bDstNode == null) return false;

                // A workspace is selected. The destination folder is the
                // parent folder and the insert position is one more than
                // the index of the selected workspace.
                if (bDstKwsNode != null)
                {
                    di.DstFolder = bDstKwsNode.Parent;
                    di.DstIndex = bDstKwsNode.Index;
                }
            }

            // We're moving a folder.
            else
            {
                // Cannot move a folder in the workspace area.
                if (bDstKwsNode != null) return false;

                // If there is no destination node, the destination node is the
                // root folder. We always insert at the end.
                if (di.DstFolder == null)
                {
                    di.DstFolder = Browser.RootNode;
                    di.DstIndex = Browser.RootNode.FolderNodes.Count;
                }
            }

            return (Browser.MoveCheck(di.SrcNode, di.DstFolder, di.SrcNode.Name, di.DstIndex) == null);
        }

        /// <summary>
        /// Helper for drag & drop. Return the effect Move is the move is 
        /// allowed, None otherwise.
        /// </summary>
        private DragDropEffects TvWorkspacesDragEffect(bool allowFlag)
        {
            return allowFlag ? DragDropEffects.Move : DragDropEffects.None;
        }

        /// <summary>
        /// Start the dragging of a node in tvWorkspaces.
        /// </summary>
        private void tvWorkspaces_ItemDrag(object sender, ItemDragEventArgs e)
        {
            Debug.Assert(m_ignoreSelectFlag == false);
            TvWorkspacesDragInfo di = m_tvWorkspacesDragInfo;
            TreeNode sourceNode = (TreeNode)e.Item;
            di.SrcPath = (String)sourceNode.Tag;
            bool allowFlag = !StaleBrowser && TvWorkspacesDragValidateSource();
            DoDragDrop(sourceNode, TvWorkspacesDragEffect(allowFlag));
        }

        private void tvWorkspaces_DragEnter(object sender, DragEventArgs e)
        {
            m_ignoreSelectFlag = true;
            e.Effect = TvWorkspacesDragEffect(!StaleBrowser);
        }

        private void tvWorkspaces_DragLeave(object sender, EventArgs e)
        {
            tvWorkspaces.SelectedNode = null;
            m_ignoreSelectFlag = false;
        }

        private void tvWorkspaces_DragOver(object sender, DragEventArgs e)
        {
            TvWorkspacesDragInfo di = m_tvWorkspacesDragInfo;
            TreeNode node = TvWorkspacesGetNodeByMousePos(e.X, e.Y);
            if (node == null) return;
            tvWorkspaces.SelectedNode = node;
            e.Effect = TvWorkspacesDragEffect(!StaleBrowser && TvWorkspacesDragValidateDst(node));

            Point pt = PointToClient(new Point(e.X, e.Y));
            double diff = (DateTime.Now - di.ScrollTime).TotalMilliseconds;

            // Scroll up quickly.
            if (pt.Y < tvWorkspaces.ItemHeight)
            {
                if (node.PrevVisibleNode != null)
                    node = node.PrevVisibleNode;
                node.EnsureVisible();
                di.ScrollTime = DateTime.Now;
            }

            // Scroll up slowly.
            else if (pt.Y < (tvWorkspaces.ItemHeight * 2))
            {
                if (diff > 250)
                {
                    node = node.PrevVisibleNode;
                    if (node != null) node = node.PrevVisibleNode;
                    if (node != null)
                    {
                        node.EnsureVisible();
                        di.ScrollTime = DateTime.Now;
                    }
                }
            }
        }

        private void tvWorkspaces_DragDrop(object sender, DragEventArgs e)
        {
            TvWorkspacesDragInfo di = m_tvWorkspacesDragInfo;
            TreeNode node = TvWorkspacesGetNodeByMousePos(e.X, e.Y);
            bool allowFlag = !StaleBrowser && TvWorkspacesDragValidateDst(node);
            if (allowFlag)
            {
                Browser.Move(di.SrcNode, di.DstFolder, di.SrcNode.Name, di.DstIndex);
                KwsBrowserKwsNode bNode = di.SrcNode as KwsBrowserKwsNode;

                // Do not change the order of these calls.
                m_uiBroker.RequestBrowserUiUpdate(true);
                if (bNode != null) m_uiBroker.RequestSelectKws(bNode.Kws, true);
            }

            m_ignoreSelectFlag = false;
        }

        private void tvWorkspaces_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            KwsBrowserNode bNode = Browser.GetNodeByPath((String)e.Node.Tag);
            if (StaleBrowser ||
                bNode == null ||
                bNode is KwsBrowserKwsNode ||
                ((KwsBrowserFolderNode)bNode).PermanentFlag)
            {
                e.CancelEdit = true;
            }
        }

        private void tvWorkspaces_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null) return;
            e.CancelEdit = true;

            if (StaleBrowser) return;

            String label = e.Label.Trim();
            KwsBrowserFolderNode f = Browser.GetFolderNodeByPath((String)e.Node.Tag);
            if (f == null) return;

            String reason = Browser.MoveCheck(f, f.Parent, label, f.Index);
            if (reason != null) return;

            Browser.Move(f, f.Parent, label, f.Index);

            // Don't update the browser list here, this confuses Windows.
            m_uiBroker.RequestBrowserUiUpdate(true);
        }

        private void tvWorkspaces_KeyDown(object sender, KeyEventArgs e)
        {
            if (StaleBrowser) return;

            if (e.KeyCode == Keys.F2)
            {
                if (tvWorkspaces.SelectedNode != null)
                {
                    if (IsSelectedTvNodePermNode())
                        SystemSounds.Beep.Play();
                    else
                        tvWorkspaces.SelectedNode.BeginEdit();
                }
            }

            else if (e.KeyCode == Keys.Delete)
            {
                KwsBrowserNode node = GetTvWorkspacesSelectedBrowserNode();

                if (node == null) return;

                if (IsSelectedTvNodePermNode())
                    SystemSounds.Beep.Play();

                else
                {
                    if (node is KwsBrowserFolderNode)
                        m_uiBroker.RequestDeleteFolder(node as KwsBrowserFolderNode);
                    else
                    {
                        Workspace kws = ((KwsBrowserKwsNode)node).Kws;
                        if (kws.Sm.CanDelete()) m_uiBroker.RequestRemoveKwsFromList(kws);
                        else SystemSounds.Beep.Play();
                    }
                }
            }
        }

        /// <summary>
        /// Return true if the selected Treeview node is a Permanent folder node,
        /// false otherwise.
        /// </summary>
        private bool IsSelectedTvNodePermNode()
        {
            KwsBrowserFolderNode bNode = Browser.GetNodeByPath((String)tvWorkspaces.SelectedNode.Tag) as KwsBrowserFolderNode;
            if (bNode != null && bNode.PermanentFlag) return true;

            return false;
        }

        private void tvWorkspacesContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                KwsBrowserNode node = GetTvWorkspacesSelectedBrowserNode();

                // Cast the node to the appropriate type.
                KwsBrowserFolderNode folderNode = node as KwsBrowserFolderNode;
                KwsBrowserKwsNode kwsNode = node as KwsBrowserKwsNode;
                Workspace kws = kwsNode == null ? null : kwsNode.Kws;

                // Adjust the items.
                ToolStripItemCollection items = tvWorkspacesContextMenu.Items;

                HideAllTvKwsItems(items);

                if (folderNode != null)
                    UpdateFolderMenus(items, folderNode);

                else if (kwsNode != null)
                    UpdateKwsMenus(items, kwsNode);

                // Click on an empty area.
                else
                {
                    items["CmCreateNewFolder"].Visible = true;
                    items["CmCreateNewFolder"].Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvWorkspacesContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            itemClicked = e.ClickedItem;
        }
        
        private void tvWorkspacesContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            // Cancel if the item clicked is supposed to be disabled. 
            // That boolean value is stored in the Tag property of the 
            // items. It is possible for an item to not contain anything
            // in the Tag property. It is the case for separators.
            // This poutine is done to allow a tooltip to be shown
            // when a user hovers over a disabled item.
            if (itemClicked != null &&
                (itemClicked.Tag as bool?).HasValue &&
                !(itemClicked.Tag as bool?).Value)
            {
                e.Cancel = true;
                itemClicked = null;
            }
        }

        /// <summary>
        /// Mark the content of the context menu of tvWorkspacestreeview as not visible.
        /// </summary>
        private void HideAllTvKwsItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem i in items)
                i.Visible = false;
        }

        /// <summary>
        /// Make the right menus visible when the selected node is a folder and
        /// set the Enabled property properly.
        /// </summary>
        private void UpdateFolderMenus(ToolStripItemCollection items, KwsBrowserFolderNode folderNode)
        {
            items["CmCollapse"].Visible = folderNode.ExpandedFlag;
            items["CmExpand"].Visible = !folderNode.ExpandedFlag;

            items["CmCollapse"].Enabled = folderNode.ExpandedFlag;
            items["CmExpand"].Enabled = (!folderNode.ExpandedFlag && folderNode.FolderNodes.Count > 0);

            items["separator1"].Visible = true;

            items["CmCreateNewFolder"].Visible = true;
            items["CmCreateNewFolder"].Enabled = true;

            items["separator2"].Visible = true;

            items["CmDelete"].Visible = true;
            items["CmDelete"].Enabled = !folderNode.PermanentFlag;

            items["CmRenameFolder"].Visible = true;
            items["CmRenameFolder"].Enabled = !folderNode.PermanentFlag;
        }

        /// <summary>
        /// Make the right menus visible when the selected node is a Workspace and
        /// set the Enabled property properly.
        /// </summary>
        private void UpdateKwsMenus(ToolStripItemCollection items, KwsBrowserKwsNode kwsNode)
        {
            Workspace kws = kwsNode.Kws;
            if (kws.Sm.CanWorkOnline())
                items["CmWorkOnline"].Visible = true;
            else
                items["CmWorkOffline"].Visible = true;

            items["separator1"].Visible = true;

            items["CmRenameKws"].Visible = true;
            items["CmDelete"].Visible = true;

            items["CmAdvanced"].Visible = true;

            ToolStripMenuItem advancedSubmenu = (ToolStripMenuItem)items["CmAdvanced"];
            advancedSubmenu.DropDownItems["CmDisable"].Visible = true;
            advancedSubmenu.DropDownItems["CmExport"].Visible = true;
            advancedSubmenu.DropDownItems["CmRebuild"].Visible = true;

            items["CmProperties"].Visible = true;

            SetKwsToolsStripItemStatus(items["CmWorkOnline"], KwsAction.Connect, kws);
            SetKwsToolsStripItemStatus(items["CmWorkOffline"], KwsAction.Disconnect, kws);
            
            SetKwsToolsStripItemStatus(items["CmRenameKws"], KwsAction.Rename, kws);

            SetKwsToolsStripItemStatus(advancedSubmenu.DropDownItems["CmDisable"], KwsAction.Disable, kws);
            SetKwsToolsStripItemStatus(advancedSubmenu.DropDownItems["CmExport"], KwsAction.Export, kws);
            SetKwsToolsStripItemStatus(advancedSubmenu.DropDownItems["CmRebuild"], KwsAction.Rebuild, kws);

            SetKwsToolsStripItemStatus(items["CmDelete"], KwsAction.RemoveFromList, kws);
        }

        /// <summary>
        /// Set the enabled status of the given item. If the item is disabled, a 
        /// tooltip string is set to explain why the command is unavailable.
        /// </summary>
        private void SetKwsToolsStripItemStatus(ToolStripItem item, KwsAction action, Workspace kws)
        {
            Debug.Assert(item != null && kws != null);
            
            String denyReason = "";
            item.Enabled = true;
            bool can = m_uiBroker.CanPerformKwsAction(action, kws, ref denyReason);
            if (can)
            {
                item.ForeColor = Color.Black;
            }
            else
            {
                item.ForeColor = Color.Gray;
            }
            item.Tag = can;

            item.ToolTipText = denyReason;
        }

        private void lstUsersContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                itemClicked = null;
                SetUserToolsStripItemStatus(lstUsersContextMenu.Items["Copy"], UserAction.Copy);
                SetUserToolsStripItemStatus(lstUsersContextMenu.Items["ShowProperties"], UserAction.ShowProperties);
                SetUserToolsStripItemStatus(lstUsersContextMenu.Items["Delete"], UserAction.Delete);
                SetUserToolsStripItemStatus(lstUsersContextMenu.Items["ResendInvitation"], UserAction.ResendInvitation);
                SetUserToolsStripItemStatus(lstUsersContextMenu.Items["ResetPassword"], UserAction.ResetPassword);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lstUsersContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            itemClicked = e.ClickedItem;
        }

        private void lstUsersContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            // Cancel if the item clicked is supposed to be disabled. 
            // That boolean value is stored in the Tag property of the 
            // items. It is possible for an item to not contain anything
            // in the Tag property. It is the case for separators.
            // This poutine is done to allow a tooltip to be shown
            // when a user hovers over a disabled item.
            if (itemClicked != null &&
                (itemClicked.Tag as bool?).HasValue &&
                !(itemClicked.Tag as bool?).Value)
            {
                e.Cancel = true;
                itemClicked = null;
            }
        }

        /// <summary>
        /// Set the enabled status of the given item. If the item is disabled, a 
        /// tooltip string is set to explain why the command is unavailable.
        /// </summary>
        private void SetUserToolsStripItemStatus(ToolStripItem item, UserAction action)
        {
            Debug.Assert(item != null);

            KwsUser targetUser = m_uiBroker.GetSelectedUser();
            
            String denyReason = "";
            bool can;
            item.Enabled = true;

            if (targetUser == null)
            {
                can = false;
                denyReason = "No user is selected.";
            }
            else
            {
                can = m_uiBroker.CanPerformUserAction(action, m_uiBroker.Browser.SelectedKws, targetUser, ref denyReason);
            }

            if (can)
            {
                item.ForeColor = Color.Black;
            }
            else
            {
                item.ForeColor = Color.Gray;
            }
            item.Tag = can;

            item.ToolTipText = denyReason;
        }
        
        private void lstUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (m_ignoreSelectFlag) return;
                if (Browser.SelectedKws == null) return;

                UInt32 userID = 0;
                if (lstUsers.SelectedItems.Count > 0)
                    userID = UInt32.Parse(lstUsers.SelectedItems[0].Name);

                m_uiBroker.RequestSelectUser(Browser.SelectedKws, userID, false);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Request to exit the application.
        /// </summary>
        private void Exit()
        {
            m_exitFlag = true;
            m_uiBroker.RequestQuit();
        }

        /// <summary>
        /// Prompt the user for the file path where his workspaces should be exported.
        /// </summary>
        private String GetExportFilePath(String title)
        {
            SaveFileDialog dial = new System.Windows.Forms.SaveFileDialog();

            if (Misc.ApplicationSettings.ExportWsPath != "" && Directory.Exists(Misc.ApplicationSettings.ExportWsPath))
                dial.InitialDirectory = Misc.ApplicationSettings.ExportWsPath;
            else
                dial.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            dial.SupportMultiDottedExtensions = true;
            dial.Title = title;
            dial.DefaultExt = "wsl";
            dial.Filter = Base.GetKwsString() + " list (*.wsl)|*.wsl|All files (*.*)|*.*";
            dial.AddExtension = true;
            Misc.OnUiEntry();
            dial.ShowDialog(this);
            Misc.OnUiExit();
            string filename = dial.FileName;
            dial.Dispose();

            // If the user did not cancel, save the path so the next time
            // we can set the starting path of the dialog.
            if (filename != "")
            {
                Misc.ApplicationSettings.ExportWsPath = Path.GetDirectoryName(filename);
                Misc.ApplicationSettings.Save();
            }
            return filename;
        }

        public void OnShowAboutFormClick(object sender, EventArgs e)
        {
            frmAbout about = new frmAbout();
            Misc.OnUiEntry();
            about.ShowDialog();
            Misc.OnUiExit();
        }

        public void OnShowConsoleFormClick(object sender, EventArgs e)
        {
            if (m_consoleWindow == null)
            {
                m_consoleWindow = new ConsoleWindow();
                Logging.RegisterLogHandler(m_consoleWindow);
                m_consoleWindow.OnConsoleClosing += OnConsoleFormClosing;
                m_consoleWindow.Show();
                UpdateConsoleAndEventWindowMenu();
            }

            else
            {
                m_consoleWindow.Close();
            }
        }

        private void OnConsoleFormClosing(object sender, EventArgs e)
        {
            m_consoleWindow = null;
            UpdateConsoleAndEventWindowMenu();
        }

        /// <summary>
        /// Update the console & event window menu item in the main form.
        /// </summary>
        private void UpdateConsoleAndEventWindowMenu()
        {
            MmDebuggingConsoleToolStripMenuItem.Checked = (m_consoleWindow != null);
        }

        public void OnToolsOptionsFormClick(object sender, EventArgs e)
        {
            try
            {
                frmOptions opt = new frmOptions();
                Misc.OnUiEntry();
                DialogResult res = opt.ShowDialog();
                Misc.OnUiExit();

                if (res == DialogResult.OK)
                {
                    opt.SaveSettings();
                    ApplyOptions();

                    // Notify the OTC that its settings have been modified.
                    m_uiBroker.RequestNotifyKwmStateChanged();
                }
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        public void OnFileExitClick(object sender, EventArgs e)
        {
            Exit();
        }

        public void OnTrayOpenClick(object sender, EventArgs e)
        {
            ShowMainForm();
        }

        private void KwsTaskBtn_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;

            Workspace kws = m_uiBroker.Browser.SelectedKws;
            if (kws == null) return;

            if (m_kwsTaskBtnTask == KwsTask.WorkOnline) m_uiBroker.RequestWorkOnline(kws);
            else if (m_kwsTaskBtnTask == KwsTask.WorkOffline) m_uiBroker.RequestWorkOffline(kws);
            else if (m_kwsTaskBtnTask == KwsTask.Rebuild) m_uiBroker.RequestRebuildKws(kws);
        }

        private void CmWorkOnline_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;
            KwsBrowserKwsNode node = GetTvWorkspacesSelectedBrowserNode() as KwsBrowserKwsNode;
            if (node == null) return;
            m_uiBroker.RequestWorkOnline(node.Kws);
        }

        private void CmWorkOffline_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;
            KwsBrowserKwsNode node = GetTvWorkspacesSelectedBrowserNode() as KwsBrowserKwsNode;
            if (node == null) return;
            m_uiBroker.RequestWorkOffline(node.Kws);
        }

        private void CmCreateNewFolder_Click(object sender, EventArgs e)
        {
            CreateNewFolder();
        }

        private void CmRename_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;
            TreeNode tNode = GetTvWorkspacesTreeNodeByPath(GetTvWorkspacesSelectedPath());
            if (tNode == null) return;
            tNode.BeginEdit();
        }

        private void CmExport_Click(object sender, EventArgs e)
        {
            try
            {
                if (StaleBrowser) return;
                KwsBrowserKwsNode node = GetTvWorkspacesSelectedBrowserNode() as KwsBrowserKwsNode;
                if (node == null) return;

                string filename = GetExportFilePath("Export " + Base.GetKwsString() + " as");

                if (filename != "")
                    m_uiBroker.RequestExportBrowser(filename, UInt64.Parse(node.ID));
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void CmRebuild_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;
            KwsBrowserKwsNode node = GetTvWorkspacesSelectedBrowserNode() as KwsBrowserKwsNode;
            if (node == null) return;
            m_uiBroker.RequestRebuildKws(node.Kws);
        }

        private void CmDisable_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;
            KwsBrowserKwsNode node = GetTvWorkspacesSelectedBrowserNode() as KwsBrowserKwsNode;
            if (node == null) return;
            m_uiBroker.RequestStopKws(node.Kws);
        }

        private void CmDelete_Click(object sender, EventArgs e)
        {
            if (StaleBrowser) return;
            KwsBrowserNode node = GetTvWorkspacesSelectedBrowserNode();
            if (node == null) return;

            // We're deleting a single workspace.
            if (node is KwsBrowserKwsNode)
                m_uiBroker.RequestRemoveKwsFromList((node as KwsBrowserKwsNode).Kws);

            // We're deleting a folder.
            else
                m_uiBroker.RequestDeleteFolder(node as KwsBrowserFolderNode);
        }

        private void cmExpandCollapseHelper(bool expandFlag)
        {
            if (StaleBrowser) return;
            KwsBrowserFolderNode bNode = GetTvWorkspacesSelectedBrowserNode() as KwsBrowserFolderNode;
            if (bNode == null) return;
            TreeNode tNode = GetTvWorkspacesTreeNodeByPath(bNode.FullPath);
            if (tNode == null) return;
            m_uiBroker.RequestSetFolderExpansion(bNode, expandFlag);
            if (expandFlag) tNode.Expand();
            else tNode.Collapse();
        }

        private void CmExpand_Click(object sender, EventArgs e)
        {
            cmExpandCollapseHelper(true);
        }

        private void CmCollapse_Click(object sender, EventArgs e)
        {
            cmExpandCollapseHelper(false);
        }

        private void tvExpandCollapseHelper(TreeNode node, bool expandFlag)
        {
            KwsBrowserFolderNode bNode = m_uiBroker.Browser.GetFolderNodeByPath((String)node.Tag);
            if (bNode == null) return;
            m_uiBroker.RequestSetFolderExpansion(bNode, expandFlag);
        }

        private void tvWorkspaces_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            tvExpandCollapseHelper(e.Node, false);
        }

        private void tvWorkspaces_AfterExpand(object sender, TreeViewEventArgs e)
        {
            tvExpandCollapseHelper(e.Node, true);
        }

        private void trayExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Exit();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        /// <summary>
        /// Set the background color of a workspace node in the browser. 
        /// </summary>
        private void SetWorkspaceBackColor(Workspace ws, Color color)
        {
            TreeNode[] nodes = tvWorkspaces.Nodes.Find(ws.InternalID.ToString(), true);
            if (nodes.Length == 1 && !(nodes[0].BackColor == Color.White && color == Color.White))
            {
                nodes[0].BackColor = color;

                nodes = tvWorkspaces.Nodes.Find("FOLDER0", true);
                if (nodes.Length == 1)
                    nodes[0].BackColor = color;
            }
        }

        private void trayOpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ShowMainForm();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Method called by the UI hooks when a user wants to create
        /// a new folder to manage his workspaces in tvWorkspaces.
        /// </summary>
        private void CreateNewFolder()
        {
            if (StaleBrowser) return;

            // Get the parent folder.
            KwsBrowserFolderNode parentFolder = Browser.RootNode;

            String selPath = GetTvWorkspacesSelectedPath();
            if (selPath != "")
            {
                KwsBrowserNode n = Browser.GetNodeByPath(selPath);
                if (n == null) return;

                // We want to create a new folder inside the selected folder.
                if (n is KwsBrowserFolderNode)
                {
                    parentFolder = n as KwsBrowserFolderNode;
                }

                // We want to create a new folder inside the folder of the
                // selected workspace.
                else
                {
                    if (((KwsBrowserKwsNode)n).Parent == null) return;
                    parentFolder = ((KwsBrowserKwsNode)n).Parent;
                }
            }

            // Insert the node in the browser.
            KwsBrowserFolderNode bNode = m_uiBroker.RequestCreateFolder(parentFolder);

            // Rebuild tvWorkspaces.
            UpdateTvWorkspacesList(true, false);

            // Select the newly created folder.
            TreeNode tNode = GetTvWorkspacesTreeNodeByPath(bNode.FullPath);
            tvWorkspaces.SelectedNode = tNode;
            tNode.BeginEdit();
        }

        private void btnNewKws_Click(object sender, EventArgs e)
        {
            try
            {
                m_uiBroker.RequestCreateKws();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }

        }

        private void resetPasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ToolStripItem i = sender as ToolStripItem;
                if (!(i.Tag as bool?).Value) return;

                m_uiBroker.RequestSetUserPassword();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Called to process the messages received by this window.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Misc.WM_COPYDATA)
            {
                Misc.COPYDATASTRUCT cds = (Misc.COPYDATASTRUCT)m.GetLParam(typeof(Misc.COPYDATASTRUCT));

                // This is our import workspace credentials message.
                if (cds.dwData.ToInt32() == Program.ImportKwsMsgID)
                {
                    Logging.Log("Received request to import workspace from window procedure.");

                    // Retrieve the workspace path. Clone the string to avoid
                    // any potential trouble.
                    String path = cds.lpData.Clone() as String;

                    // Perform the import. This is safe as we are in the UI
                    // context and we delay the import if we're not ready to
                    // process it yet.
                    m_uiBroker.RequestImportBrowser(path);
                }
            }

            base.WndProc(ref m);
        }

        private void btnInvite_Click(object sender, EventArgs e)
        {
            try
            {
                HandleInvitationClick(true);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lnkInvite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                HandleInvitationClick(false);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void HandleInvitationClick(bool skipFirstWizardPage)
        {
            UpdateInviteStatus(null);

            KwmCoreKwsOpRes res = m_uiBroker.RequestInviteToKws(m_uiBroker.Browser.SelectedKws, txtInvite.Text, skipFirstWizardPage);
            if (res == KwmCoreKwsOpRes.None || res == KwmCoreKwsOpRes.Success)
                txtInvite.Text = "";

            // Request a UI update to set back the invitation controls status
            // correctly since they were disabled at the beginning of this 
            // function.
            m_uiBroker.RequestSelectedKwsUiUpdate();
        }

        private void MmNewFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CreateNewFolder();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void MmNewKwsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                m_uiBroker.RequestCreateKws();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void MmConfigurationWizardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                m_uiBroker.ShowConfigWizard();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lblKasError_Click(object sender, EventArgs e)
        {
            if (lblKasError.Text != "")
                Misc.KwmTellUser(lblKasError.Text, MessageBoxIcon.Information);
        }

        private void txtInvite_Enter(object sender, EventArgs e)
        {
            // Refresh the autocompletion collection.
            AutoCompleteStringCollection col = new AutoCompleteStringCollection();
            foreach (String address in m_uiBroker.GetKwsEmailAddressSet()) col.Add(address);
            txtInvite.AutoCompleteCustomSource = col;
        }

        private void UserProperties_Click(object sender, EventArgs e)
        {
            try
            {
                frmUserProperties prop = new frmUserProperties(m_uiBroker);
                prop.ShowDialog();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void CmKwsProperties_Click(object sender, EventArgs e)
        {
            frmKwsProperties prop = new frmKwsProperties(m_uiBroker);
            prop.ShowDialog();
        }
    }

    /// <summary>
    /// This class contains information about the current drag and drop operation
    /// in tvWorkspaces, if any.
    /// </summary>
    public class TvWorkspacesDragInfo
    {
        /// <summary>
        /// Path to the node being dragged and dropped.
        /// </summary>
        public String SrcPath;

        /// <summary>
        /// Dragged node.
        /// </summary>
        public KwsBrowserNode SrcNode;

        /// <summary>
        /// Destination folder.
        /// </summary>
        public KwsBrowserFolderNode DstFolder;

        /// <summary>
        /// Destination name.
        /// </summary>
        public String DstName;

        /// <summary>
        /// Destination index.
        /// </summary>
        public int DstIndex;

        /// <summary>
        /// Time at which we last scrolled.
        /// </summary>
        public DateTime ScrollTime = DateTime.MinValue;

        public override string ToString()
        {
            return "SrcPath: " + SrcPath + ",  DstFolder:" + DstFolder + ", DstName: " + DstName + ", DstIndex: " + DstIndex;
        }
    }
}