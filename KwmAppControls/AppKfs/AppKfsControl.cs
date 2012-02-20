using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public enum UIAction
    {
        Add,
        DeleteOrMove,
        Open,
        Synchronize,
        SaveAs,
        Revert,
        Copy,
        Paste
    }

    public partial class AppKfsControl : BaseAppControl
    {
        /// <summary>
        /// Internal class that represents the control-specific settings
        /// that may differ from one base app to another.
        /// </summary>
        [Serializable]
        public class AppKfsControlSettings : ControlSettings
        {
            /// <summary>
            /// Serialization version. For future use.
            /// </summary>
            public int SerializationVersion = 1;

            /// <summary>
            /// Path to the currently selected directory in the TV.
            /// </summary>
            public string SelectedDir;

            /// <summary>
            /// Is the selected dir expanded or not.
            /// </summary>
            public bool IsSelectedDirExpanded;

            public AppKfsControlSettings()
            {
                SelectedDir = "";
            }
        }

        #region Class members
        /// <summary>
        /// Set to true whenever a context menu is opened.
        /// </summary>
        private bool m_contextMenuOpenedFlag = false;

        /// <summary>
        /// Set to true whenever an item from a context menu
        /// is clicked.
        /// </summary>
        private bool m_itemClickedFlag = false;

        /// <summary>
        /// Contains all the different possible menu items 
        /// for the Listview contextual menu
        /// </summary>
        private Hashtable m_lvMenuItems = new Hashtable();

        /// <summary>
        /// Contains all the different possible menu items 
        /// for the Treeview contextual menu
        /// </summary>
        private Hashtable m_tvMenuItems = new Hashtable();

        /// <summary>
        /// Manages the ImageList of our listview that contains the
        /// icons for the different files and folders.
        /// </summary>
        private ImageListManager m_lvImageListMgr = new ImageListManager();

        /// <summary>
        /// Cancel treeview label edits unless the user explicitly
        /// selects the Rename context menu item or F2.
        /// </summary>
        private bool m_tvRenameAskedByMenu = false;

        /// <summary>
        /// There are three ways to rename a file in the listview.
        /// 1. Right-click / Rename.
        /// 2. Click and wait a little.
        /// 3. F2.
        /// In all three cases, we call BeginEdit on the listview item.
        /// However, the Gate has been entered only in scenario 1. We must
        /// thus enter the gate if we are in scenario 2 or 3 in the BeginEdit event.
        /// </summary>
        private bool m_lvRenamedAskedByMenu = false;

        /// <summary>
        /// True if folder expand / collapse events should be ignored.
        /// This is set to true when the treeview is being rebuilt.
        /// </summary>
        private bool m_ignoreExpandFlag = false;

        /// <summary>
        /// Used to track the Listview item made selected when dragging over it.
        /// </summary>
        private KListViewItem m_dragDropLvTempDest = null;

        /// <summary>
        /// Used to track the Treenode made selected when dragging over it.
        /// Allow showing the node over the drag and drop location as Selected,
        /// and revert back to the original one after the operation completes.
        /// </summary>
        private KTreeNode m_dragDropTvTempDest = null;
        private KTreeNode m_dragDropTvOriginalSelectedNode = null;

        /// <summary>
        /// Used to know wether or not we want to update the Listview
        /// when the TV selection changes. Usually set to true,
        /// except when drag-and-drop'ing on the TV.
        /// </summary>
        private bool m_updateLvOnTvSelectionChange = true;

        /// <summary>
        /// Used to track wether or not the share is in a state
        /// that allows the user to drop external files into the share
        /// </summary>
        private bool m_canDrop = true;

        /// <summary>
        /// Used to track wether the selected items can be dragged.
        /// </summary>
        private bool m_canDrag = true;

        /// <summary>
        /// Used to track wether to enter the gate or not on
        /// the lvFileList_DragEnter event.
        /// </summary>
        private bool m_isDraggingItem = false;

        /// <summary>
        /// Track wether the current dragged item is
        /// over the TV or LV. Used to know if we must exit the
        /// gate on the low level button up mouse event or not.
        /// </summary>
        private bool m_isDragDropInsideControls = false;

        /// <summary>
        /// Reference to the mouse hook callback. Necessary to declare
        /// it here because if the reference to the delegate goes out of
        /// scope, calling the callback will fail.
        /// </summary>
        private static Syscalls.HookProc m_mouseHookCallback;

        /// <summary>
        /// Mouse hook id. Required when we want to unhook the hook.
        /// </summary>
        private IntPtr m_mouseHookID;

        /// <summary>
        /// Set to true when the control gets loaded, not before.
        /// </summary>
        private bool m_canSaveWindowSettings = false;

        /// <summary>
        /// When the Workspace wants to disable the application, set this to true.
        /// This allows the share to manipulate correctly the status of the
        /// different available actions (see UpdateEnabledUIActions(...))
        /// </summary>
        private bool m_forceDisableUI = false;

        /// <summary>
        /// In the lvBeforeLabelEdit event handler, it is possible that we exit
        /// the gate. In that process, it is possible that the UI needs to be updated.
        /// When this happens, the listview is cleared and rebuilt, while we are still
        /// in the BeforeLabelEdit handler. The clearance of the items generates a spurious,
        /// unexpected AfterLabelEdit event that gets treated while we are in the middle 
        /// of the BeforeLabelEdit event. We must ignore the AfterLabelEdit event when
        /// this scenario happens.
        /// </summary>
        private bool m_inLvBeforeLabelEdit = false;

        /// <summary>
        /// Every time we are asked to update the UI, this gets incremented.
        /// We check this value before and after entering the gate. If the counter
        /// is different before and after, cancel the user operation : it means the
        /// share changed right under the user's feet.
        /// </summary>
        private UInt64 m_uiUpdateCounter = 0;

        /// <summary>
        /// Remember if the user wants to fly solo in this workspace or not.
        /// </summary>
        private bool m_flySoloFlag = false;

        private Cursor MoveDrop = null;

        #endregion

        #region Getters / Setters

        private AppKfs SrcApp
        {
            get { return m_srcApp as AppKfs; }
        }

        private KfsGate Gate
        {
            get { return SrcApp.Share.Gate; }
        }

        private AppKfsControlSettings Settings
        {
            get
            {
                if (SrcApp.Settings == null)
                    SrcApp.Settings = new AppKfsControl.AppKfsControlSettings();

                return (AppKfsControlSettings)SrcApp.Settings;
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor with a parent control
        /// </summary>
        /// <param name="container"></param>
        public AppKfsControl(IContainer container)
            : base()
        {
            container.Add(this);
            InitializeComponent();

            /* Create a default image list in our treeview so that
             * it contains icons for Opened and Closed folders */
            ImageList tvImageList = new ImageList();
            tvImageList.ColorDepth = ColorDepth.Depth32Bit;
            tvImageList.ImageSize = new Size(16, 16);

            Bitmap img = kwm.KwmAppControls.Properties.Resources.folderOpened;
            img.MakeTransparent();
            tvImageList.Images.Add("FolderOpened", img);

            img = kwm.KwmAppControls.Properties.Resources.folderClosed;
            img.MakeTransparent();
            tvImageList.Images.Add("FolderClosed", img);

            tvFileTree.ImageList = tvImageList;

            lvFileList.SmallImageList = m_lvImageListMgr.ImgList;

            CreateTvMenuItems();
            CreateLvMenuItems();

            CreateNewFileButton();

            SetLvColumnWidth();

            m_mouseHookCallback = new Syscalls.HookProc(LowLevelMouseProc);

            MoveDrop = Syscalls.CreateCursor(kwm.KwmAppControls.Properties.Resources.move_drop, 0, 0);

            HookEventHandlers();

            ListViewSorter.Comparer[] comparers = new ListViewSorter.Comparer[]{
                  new ListViewSorter.Comparer( ListViewSorter.CompareFileItem ),
                  new ListViewSorter.Comparer( ListViewSorter.NonComparable ),
                  new ListViewSorter.Comparer( ListViewSorter.NonComparable),
                  new ListViewSorter.Comparer( ListViewSorter.CompareDates ),
                  new ListViewSorter.Comparer( ListViewSorter.CompareStrings),
                  new ListViewSorter.Comparer( ListViewSorter.CompareStrings) };

            ListViewSorter listViewSorter = new ListViewSorter(lvFileList, comparers);
        }

        /// <summary>
        /// Hooks event handlers manually to the various controls.
        /// This is a good practice since when cut and pasting
        /// controls on a form, the events are no longer hooked
        /// to their handlers, which is a major PITA.
        /// </summary>
        private void HookEventHandlers()
        {
            btnImport.ButtonClick += btnImport_Click;
            importFilesToolStrip.Click += importFilesToolStrip_Click;
            importFolderToolStrip.Click += addFolderToolStrip_Click;
            btnSyncAll.Click += btnSyncAll_Click;
            btnViewOfflineFiles.Click += btnViewOfflineFiles_Click;
            tvFileTree.AfterSelect += tvFileTree_AfterSelect;
            lvFileList.DoubleClick += lvFileList_DoubleClick;
            lvContextMenu.Opening += lvContextMenu_Opening;
            tvContextMenu.Opening += tvContextMenu_Opening;
            tvContextMenu.Closing += tvContextMenu_Closing;
            lvContextMenu.Closing += lvContextMenu_Closing;
            tvContextMenu.ItemClicked += tvContextMenu_ItemClicked;
            lvContextMenu.ItemClicked += lvContextMenu_ItemClicked;
            lvFileList.AfterLabelEdit += lvFileList_AfterLabelEdit;
            tvFileTree.BeforeLabelEdit += tvFileTree_BeforeLabelEdit;
            tvFileTree.AfterLabelEdit += tvFileTree_AfterLabelEdit;
            tvFileTree.KeyDown += tvFileTree_KeyDown;
            lvFileList.KeyDown += lvFileList_KeyDown;
            lvFileList.BeforeLabelEdit += lvFileList_BeforeLabelEdit;
            lvFileList.ItemDrag += lvFileList_ItemDrag;
            lvFileList.DragEnter += lvFileList_DragEnter;
            lvFileList.DragOver += lvFileList_DragOver;
            lvFileList.DragLeave += lvFileList_DragLeave;
            lvFileList.DragDrop += lvFileList_DragDrop;
            this.GiveFeedback += lvFileList_GiveFeedback;
            tvFileTree.DragEnter += tvFileTree_DragEnter;
            tvFileTree.DragOver += tvFileTree_DragOver;
            tvFileTree.DragLeave += tvFileTree_DragLeave;
            tvFileTree.DragDrop += tvFileTree_DragDrop;
            kfsFilesSplitter.SplitterMoved += kfsSplitter_SplitterMoved;
        }
        #endregion

        /// <summary>
        /// Return true if the application has a KfsShare object.
        /// </summary>
        private bool HasShare()
        {
            return (SrcApp != null && SrcApp.Share != null);
        }

        /// <summary>
        /// Return true if we can work online. This implies that we have a share.
        /// </summary>
        private bool CanWorkOnline()
        {
            return (GetRunLevel() == KwsRunLevel.Online);
        }

        /// <summary>
        /// Return true if we can work offline. This implies that we have a share.
        /// </summary>
        private bool CanWorkOffline()
        {
            return (GetRunLevel() >= KwsRunLevel.Offline);
        }

        #region Base class overrides

        public override void SetDataSource(KwsApp _newApp)
        {
            if (HasShare()) SrcApp.Share.Deactivate();
            base.SetDataSource(_newApp);
        }

        protected override void UpdateControls()
        {
            // FIXME: we want to support work offline eventually.
            bool _enable = CanWorkOnline();

            // Activate the share if it is not active. Do not do this in 
            // SetDataSource(), the share does not necessarily exist yet.
            if (_enable) SrcApp.Share.Activate();

            // Link the transfers to the application.
            kfsTransfers.Init(SrcApp);

            // Remember that whatever the AppKfs thinks, 
            // the whole control must be disabled.
            m_forceDisableUI = !_enable;

            tvFileTree.Enabled = _enable;
            lvFileList.Enabled = _enable;
            kfsTransfers.Enabled = _enable;
            btnViewOfflineFiles.Enabled = CanWorkOffline();

            // Call the generic UI updater.
            UpdateUI();
        }

        private void UpdateUI()
        {
            // FIXME: we want to support work offline eventually.
            bool _enable = CanWorkOnline();

            try
            {
                Logging.Log("AppKFS UpdateUI called.");

                tvFileTree.BeginUpdate();
                lvFileList.BeginUpdate();

                // Skip this for better performances. Clearing before any LV
                // update will ensure the right icons are displayed, but might
                // consume some CPU power for nothing.
                m_lvImageListMgr.Clear();

                UpdateUIActions();
                UpdateTV();
                UpdateStaleStatus();

                // Remember the number of transfers before the update.
                int nbXfers = kfsTransfers.NbActiveTransfers;

                // Perform the update, if required.
                if (_enable) kfsTransfers.UpdateTransfers();

                // Expand if we only have new xfers.
                if (nbXfers == 0 && kfsTransfers.NbActiveTransfers > 0)
                    ExpandTransfers();

                // Collapse if we have nothing.
                if (kfsTransfers.NbItems == 0)
                    CollapseTransfers();

                btnClearErrors.Enabled = kfsTransfers.NbErrors > 0;
                btnCancelAll.Enabled = kfsTransfers.NbActiveTransfers > 0;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                tvFileTree.EndUpdate();
                lvFileList.EndUpdate();
            }
        }

        public override uint ID
        {
            get
            {
                return KAnpType.KANP_NS_KFS;
            }
        }

        protected override void RegisterAppEventHandlers()
        {
            base.RegisterAppEventHandlers();

            if (m_srcApp != null)
            {
                SrcApp.OnUIUpdateRequired += HandleOnUiUpdateRequired;
                SrcApp.OnUIXferUpdateRequired += HandleOnUiXferUpdateRequired;
            }
        }

        protected override void UnregisterAppEventHandlers()
        {
            base.UnregisterAppEventHandlers();
            if (m_srcApp != null)
            {
                SrcApp.OnUIUpdateRequired -= HandleOnUiUpdateRequired;
                SrcApp.OnUIXferUpdateRequired -= HandleOnUiXferUpdateRequired;
            }
        }
        #endregion

        #region Class methods

        /// <summary>
        /// Intended to create once and for all every possible
        /// Menu item displayed by the TreeView.
        /// </summary>
        private void CreateTvMenuItems()
        {
            ToolStripMenuItem item;

            item = new ToolStripMenuItem("Expand", null, HandleTreeviewExpandMenu, "Expand");
            item.Font = new Font(item.Font, FontStyle.Bold);
            m_tvMenuItems.Add("Expand", item);

            item = new ToolStripMenuItem("Synchronize", null, HandleTreeviewSyncMenu, "Synchronize");
            item.Image = kwm.KwmAppControls.Properties.Resources.btnSyncAll;
            m_tvMenuItems.Add("Synchronize", item);

            item = new ToolStripMenuItem("Delete", null, HandleTreeviewDeleteMenu, "Delete");
            m_tvMenuItems.Add("Delete", item);

            item = new ToolStripMenuItem("Rename", null, HandleTreeviewRenameMenu, "Rename");
            m_tvMenuItems.Add("Rename", item);
        }

        /// <summary>
        /// Intended to create once and for all every possible menu items displayed 
        /// by the ListView. Also populates the toolbar button "New".
        /// </summary>
        private void CreateLvMenuItems()
        {
            ToolStripMenuItem item;

            // ToolStrip container for "Add Files" and "Add Folder".
            item = new ToolStripMenuItem("Import...", kwm.KwmAppControls.Properties.Resources.ImportGen_16x16, null, "AddFiles");
            item.DropDown = new ContextMenuStrip();
            
            // Make sure clicks on any ToolStripMenuItem in this container trigger 
            // the same action as items directly in the root container.
            item.DropDown.ItemClicked += lvContextMenu_ItemClicked;

            ToolStripMenuItem addItem;

            addItem = new ToolStripMenuItem("File(s)", kwm.KwmAppControls.Properties.Resources.ImportGen_16x16, HandleListviewAddFilesMenu, "DoAddFiles");
            item.DropDown.Items.Add(addItem);

            addItem = new ToolStripMenuItem("Folder", kwm.KwmAppControls.Properties.Resources.folderOpened, HandleListviewAddFolderMenu, "DoAddFolder");
            addItem.ImageTransparentColor = Color.Magenta;
            item.DropDown.Items.Add(addItem);

            // Add the container to our hashtable.
            m_lvMenuItems.Add("AddFiles", item);

            // ToolStrip container for "New" context menu.
            item = new ToolStripMenuItem("New", null, null, "New");
            item.DropDown = new ContextMenuStrip();
            item.DropDown.ItemClicked += lvContextMenu_ItemClicked;

            m_lvMenuItems.Add("New", item);

            CreateNewDocMenuItems(item.DropDown.Items);

            item = new ToolStripMenuItem("Open", null, HandleListviewOpenMenu, "Open");
            item.Font = new Font(item.Font, FontStyle.Bold);
            m_lvMenuItems.Add("Open", item);

            item = new ToolStripMenuItem("Synchronize",  kwm.KwmAppControls.Properties.Resources.btnSyncAll, HandleListviewSyncMenu, "Synchronize");
            m_lvMenuItems.Add("Synchronize", item);

            item = new ToolStripMenuItem("Rename", null, HandleListviewRenameMenu, "Rename");
            m_lvMenuItems.Add("Rename", item);

            item = new ToolStripMenuItem("Delete", null, HandleListviewDeleteMenu, "Delete");
            m_lvMenuItems.Add("Delete", item);

            /* Shown when a file's status is set to NotAdded, in opposition to AddFiles
             * that is shown when no item is selected and we show a File browse
             * dialog. */
            item = new ToolStripMenuItem("Share local file(s)", null, HandleListviewDoAddMenu, "DoAdd");
            item.Image = kwm.KwmAppControls.Properties.Resources.btnAddFiles;
            m_lvMenuItems.Add("DoAdd", item);

            item = new ToolStripMenuItem("Resolve Conflict", null, HandleListviewResolveConflictMenu, "ResolveConflict");
            m_lvMenuItems.Add("ResolveConflict", item);

            item = new ToolStripMenuItem("Resolve Conflicts", null, HandleListviewResolveTypeConflictsMenu, "ResolveTypeConflicts");
            m_lvMenuItems.Add("ResolveTypeConflicts", item);

            item = new ToolStripMenuItem("Save as...", null, HandleListviewSaveAsMenu, "SaveAs");
            m_lvMenuItems.Add("SaveAs", item);

            item = new ToolStripMenuItem("Revert your changes", null, HandleListviewRevertChangesMenu, "Revert");
            m_lvMenuItems.Add("Revert", item);

            item = new ToolStripMenuItem("Copy", null, HandleListviewCopyMenu, "Copy");
            m_lvMenuItems.Add("Copy", item);

            item = new ToolStripMenuItem("Paste", null, HandleListviewPasteMenu, "Paste");
            m_lvMenuItems.Add("Paste", item);
        }

        /// <summary>
        /// Populate the New... ToolStripButton from the toolbar.
        /// </summary>
        private void CreateNewFileButton()
        {
            CreateNewDocMenuItems(btnNew.DropDownItems);
        }

        private void CreateNewDocMenuItems(ToolStripItemCollection col)
        {
            ToolStripMenuItem newFolderItem = new ToolStripMenuItem("Folder", kwm.KwmAppControls.Properties.Resources.newfolder_16x16, HandleListviewCreateDirectoryMenu, "CreateDir");
            newFolderItem.ImageTransparentColor = Color.Magenta;

            col.Add(newFolderItem);

            col.Add(new ToolStripSeparator());

            List<NewDocument> newDocs = Misc.GetNewDocs(false);

            foreach (NewDocument doc in newDocs)
            {
                ToolStripMenuItem newItm = new ToolStripMenuItem(doc.DisplayName, doc.TypeIcon.ToBitmap(), HandleNewDocClicked, null);
                newItm.Tag = doc;
                col.Add(newItm);
            }
        }

        private void SetLvColumnWidth()
        {
            try
            {
                lvFileList.Columns[0].Width = Misc.ApplicationSettings.KfsNameColumnWidth;
                lvFileList.Columns[1].Width = Misc.ApplicationSettings.KfsSizeColumnWidth;
                lvFileList.Columns[3].Width = Misc.ApplicationSettings.KfsModDateColumnWidth;
                lvFileList.Columns[4].Width = Misc.ApplicationSettings.KfsModByColumnWidth;
                lvFileList.Columns[5].Width = Misc.ApplicationSettings.KfsStatusColumnWidth;
            }
            catch(Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void SetItemClickedFlag()
        {
            Logging.Log("SetItemClickedFlag");
            m_itemClickedFlag = true;
        }

        private void UnsetItemClickedFlag()
        {
            Logging.Log("UnsetItemClickedFlag");
            m_itemClickedFlag = false;
        }

        private void HandleTreeviewExpandMenu(object sender, EventArgs e)
        {
            try
            {
                KTreeNode node = (KTreeNode)tvFileTree.ClickedNode;
                Logging.Log(1, "HandleTreeviewExpandMenu. ClickedNode : " + node.Name);

                Debug.Assert(node != null, "Node cannot be null");

                if (node.IsExpanded)
                {
                    node.Collapse();
                }
                else
                {
                    node.Expand();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleTreeviewExpandMenu");
            }
        }

        private void HandleTreeviewSyncMenu(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(Gate.CanSynchronize(((KTreeNode)tvFileTree.ClickedNode)));

                List<string> path = new List<string>();
                path.Add(((KTreeNode)tvFileTree.ClickedNode).KFullPath);
                Gate.SynchronizePath(path, false);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleTreeviewSyncMenu");
            }
        }

        private void HandleTreeviewDeleteMenu(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(Gate.CanDeleteOrMove((KTreeNode)tvFileTree.ClickedNode));
                string folderName = ((KTreeNode)tvFileTree.ClickedNode).Text;
                String msg = "Are sure you want to remove the folder '" + folderName + "' and all its contents?";
                DialogResult res = Misc.KwmTellUser(msg, "Confirm Folder Delete",
                                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    List<string> toDelete = new List<string>();
                    toDelete.Add(((KTreeNode)tvFileTree.ClickedNode).KFullPath);
                    Gate.DeletePath(toDelete);
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleTreeviewDeleteMenu");
            }
        }

        /// <summary>
        /// This action is quite different from the others : it does not take
        /// immediate action. Instead, it puts the selected item in label edit
        /// mode. We must stay inside the gate between the BeginEdit and AfterLabelEdit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleTreeviewRenameMenu(object sender, EventArgs e)
        {
            try
            {
                m_tvRenameAskedByMenu = true;
                tvFileTree.ClickedNode.BeginEdit();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void HandleTreeviewAddMenu(object sender, EventArgs e)
        {
            try
            {
                // No validation is done here since we will only ever have
                // a single selection in the treeview, and the status of
                // the selection has already been checked in ContextMenu_opening.
                Debug.Assert(Gate.CanAdd(((KTreeNode)tvFileTree.ClickedNode)));

                List<string> toAdd = new List<string>();

                toAdd.Add(((KTreeNode)tvFileTree.ClickedNode).KFullPath);

                Gate.AddInternalFiles(toAdd);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
            finally
            {
                OnActionCompleted("HandleTreeviewAddMenu");
            }
        }

        private void HandleListviewCreateDirectoryMenu(object sender, EventArgs e)
        {
            try
            {
                CreateFolder();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewCreateDirectoryMenu");
            }
        }

        /// <summary>
        /// Prompt for a new folder name, sanitize it until good and
        /// create it in the share.
        /// </summary>
        private void CreateFolder()
        {
            string FolderName = GetValidItemName("New Folder", "New folder", "Enter the new folder name");

            if (FolderName != "")
            {
                // Construct the new folder relative path
                string newDirPath = KfsPath.AddTrailingSlash(((KTreeNode)tvFileTree.ClickedNode).KFullPath);
                newDirPath += FolderName;
                Gate.AddDirectory(newDirPath);
            }
        }

        private void HandleNewDocClicked(object sender, EventArgs e)
        {
            try
            {
                // Sender could be a ToolStripSeparator or any other uninteresting
                // control.
                if (!(sender is ToolStripMenuItem)) return;

                ToolStripMenuItem item = (ToolStripMenuItem)sender;

                Debug.Assert(item.Tag != null);

                NewDocument doc = item.Tag as NewDocument;
                
                string NewFileName = GetValidItemName("New " + doc.DisplayName + doc.Extension, "New document", "Enter the new document name");

                if (NewFileName != "")
                {
                    string newDocPath = ((KTreeNode)tvFileTree.ClickedNode).KFullPath;
                    Gate.AddNewDocument(newDocPath, NewFileName, doc, false);
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleOnNewDocumentClicked");
            }
        }

        private void HandleListviewAddFilesMenu(object sender, EventArgs e)
        {
            try
            {                
                KTreeNode currentDir = (KTreeNode)tvFileTree.SelectedNode;
                String destRelPath = (currentDir == null) ? "" : currentDir.KFullPath;
                Gate.AddExternalFiles(destRelPath, null, false, new GateConfirmActions());
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewAddFilesMenu");
            }
        }

        private void HandleListviewAddFolderMenu(object sender, EventArgs e)
        {
            try
            {
                KTreeNode currentDir = (KTreeNode)tvFileTree.SelectedNode;
                Gate.AddExternalFiles(currentDir.KFullPath, null, true, new GateConfirmActions());
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewAddFolderMenu");
            }
        }

        private void HandleListviewOpenMenu(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("HandleListviewOpenMenu called.");
                if (lvFileList.SelectedItems.Count == 1 &&
                    ((KListViewItem)lvFileList.SelectedItems[0]).Status == PathStatus.Directory)
                {
                    OpenFolder(((KListViewItem)lvFileList.SelectedItems[0]).Path);
                }
                else
                {
                    OpenSelectedFiles();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewOpenMenu");
            }
        }

        private void HandleListviewSyncMenu(object sender, EventArgs e)
        {
            try
            {
                List<string> toSync = new List<string>();

                foreach (KListViewItem itm in lvFileList.SelectedItems)
                {
                    if (Gate.CanSynchronize(itm))
                        toSync.Add(itm.Path);
                }

                Debug.Assert(toSync.Count > 0);

                Gate.SynchronizePath(toSync, false);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewSyncMenu");
            }
        }


        private void HandleListviewDeleteMenu(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(lvFileList.SelectedItems.Count > 0);

                string msg = "Are sure you want to delete these " +
                             lvFileList.SelectedIndices.Count +
                             " items?";
                DialogResult res = Misc.KwmTellUser(msg, "Confirm Multiple File Delete",
                                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;

                // Sanitize
                List<string> toDelete = new List<string>();
                foreach (KListViewItem itm in lvFileList.SelectedItems)
                {
                    if (Gate.CanDeleteOrMove(itm))
                        toDelete.Add(itm.Path);
                }

                Debug.Assert(toDelete.Count > 0);

                Gate.DeletePath(toDelete);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewDeleteMenu");
            }
        }

        /// This action is quite different from the others : it does not take
        /// immediate action. Instead, it puts the selected item in label edit
        /// mode. We must stay inside the gate between the BeginEdit and AfterLabelEdit.
        /// If the item is not movable, the BeginEdit event will be cancelled
        /// in its event handler.
        private void HandleListviewRenameMenu(object sender, EventArgs e)
        {
            string s = "HandleListviewRenameMenu";
            try
            {
                // Enter gate only if we were not triggered by the Rename menu.

                if (m_lvRenamedAskedByMenu)
                {
                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        Gate.GateExit(s += " refused, share has been modified.");
                        return;
                    }
                }
                Debug.Assert(lvFileList.SelectedItems.Count == 1);
                m_lvRenamedAskedByMenu = true;

                lvFileList.SelectedItems[0].BeginEdit();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void HandleListviewDoAddMenu(object sender, EventArgs e)
        {
            try
            {
                List<string> toAdd = new List<string>();

                foreach (KListViewItem itm in lvFileList.SelectedItems)
                {
                    if (Gate.CanAdd(itm))
                        toAdd.Add(itm.Path);
                }

                Debug.Assert(toAdd.Count > 0);

                Gate.AddInternalFiles(toAdd);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewDoAddMenu");
            }
        }

        /// <summary>
        /// This method handles only one conflictual item at a time. It
        /// would be easy to modify in order to allow multiple items at a time
        /// and asking for the desired action to do on all the selected items.
        /// Modifying the frmConflictResolution form is what would take the more time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleListviewResolveConflictMenu(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(lvFileList.SelectedItems.Count == 1);

                KListViewItem itm = lvFileList.SelectedItems[0] as KListViewItem;

                String message = "This file has already been modified by " +
                    itm.ModifiedBy + "." + Environment.NewLine + Environment.NewLine +
                    "Do you want to overwrite " + itm.ModifiedBy + "'s changes with yours, " +
                    "or do you want to overwrite your changes with " + itm.ModifiedBy + "'s?";

                FrmConflictResolution frm = new FrmConflictResolution(message);

                Misc.OnUiEntry();
                DialogResult res = frm.ShowDialog();
                Misc.OnUiExit();
                if (res != DialogResult.OK) return;

                List<string> toResolve = new List<string>();
                toResolve.Add(itm.Path);

                Gate.ResolveFileConflict(toResolve, frm.Action == FrmConflictResolution.ConflictAction.Download);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewResolveConflictMenu");
            }
        }

        private void HandleListviewResolveTypeConflictsMenu(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(lvFileList.SelectedItems.Count == 1);
                Debug.Assert(SrcApp.Share.TypeConflictFlag);

                Gate.ResolveTypeConflicts();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewResolveTypeConflictsMenu");
            }
        }

        private void HandleListviewSaveAsMenu(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("HandleListviewSaveAsMenu() called.");

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.CheckFileExists = false;
                dlg.CheckPathExists = true;

                dlg.AddExtension = false;
                dlg.CreatePrompt = false;
                dlg.OverwritePrompt = true;
                dlg.Title = "Save this file to ...";
                dlg.FileName = GetSelectedItem().FileName;
                dlg.Filter = "All Files (*.*) | *.*";

                if (Misc.ApplicationSettings.KfsSaveAsPath != "" &&
                    Directory.Exists(Misc.ApplicationSettings.KfsSaveAsPath))
                {
                    dlg.InitialDirectory = Misc.ApplicationSettings.KfsSaveAsPath;
                }
                else
                {
                    dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                Misc.OnUiEntry();
                DialogResult res = dlg.ShowDialog();
                Misc.OnUiExit();

                if (res == DialogResult.OK)
                {
                    Debug.Assert(dlg.FileNames.Length == 1);

                    Misc.ApplicationSettings.KfsSaveAsPath = Path.GetDirectoryName(dlg.FileName);
                    Misc.ApplicationSettings.Save();
                    Misc.CopyFile(SrcApp.Share.MakeAbsolute(GetSelectedItem().Path), dlg.FileName, true, true, false, false, false);
                }   
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewSaveAsMenu");
            }
        }

        private void HandleListviewRevertChangesMenu(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("HandleListviewRevertChangesMenu() called.");
                String msg = "This operation will replace the selected files by the latest " +
                             "version present on the server. Any unsynchronized changes will be lost. " +
                             " Are you sure you want to continue?";
                DialogResult res = Misc.KwmTellUser(msg, "Revert changes",
                                                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;

                List<String> toRevert = new List<string>();

                // Ignore directories.
                foreach (KListViewItem itm in lvFileList.SelectedItems)
                {
                    if (itm.Status == PathStatus.ModifiedCurrent)
                        toRevert.Add(itm.Path);
                }

                Debug.Assert(toRevert.Count > 0);

                Gate.RevertChanges(toRevert);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewRevertChangesMenu");
            }
        }

        private void HandleListviewCopyMenu(object sender, EventArgs e)
        {
            try
            {
                LvCopy();                
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewCopyMenu");
            }
        }

        private void HandleListviewPasteMenu(object sender, EventArgs e)
        {
            try
            {
                Paste();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("HandleListviewPasteMenu");
            }
        }

        /// <summary>
        /// Perform the necessary operations in order to copy the selected
        /// items in the Listview to the Windows clipboard.
        /// </summary>
        private void LvCopy()
        {
            List<String> toCopy = new List<string>();

            foreach (KListViewItem itm in lvFileList.SelectedItems)
            {
                if (Gate.CanCopy(itm))
                    toCopy.Add(itm.Path);
            }

            if (toCopy.Count > 0)
                Gate.Copy(toCopy);
        }

        /// <summary>
        /// Perform the necessary operations in order to paste the copy the 
        /// Windows clipboard content to the selected directory.
        /// </summary>
        private void Paste()
        {
            KTreeNode currentDir = tvFileTree.ClickedNode as KTreeNode;
            Debug.Assert(currentDir != null);

            Gate.Paste(currentDir.KFullPath);
        }
        /// <summary>
        /// Prompt the user for a new file or folder name. The name must be unique in the
        /// currently selected folder.
        /// </summary>
        /// <param name="DefaultText">Default string to be proposed (i.e. "New Folder")</param>
        /// <returns>The unique name, "" if the user cancels.</returns>
        private string GetValidItemName(String DefaultText, String InputboxTitle, String InputboxPrompt)
        {
            if (lvFileList.Items.Find(DefaultText, false).Length != 0)
            {
                int i = 2;

                String DefaultTextNoExt = Path.GetFileNameWithoutExtension(DefaultText);
                String DefaultTextExt = Path.GetExtension(DefaultText);

                while (lvFileList.Items.Find(DefaultTextNoExt + " (" + i + ")" + DefaultTextExt, false).Length != 0)
                {
                    i++;
                }

                DefaultText = DefaultTextNoExt + " (" + i + ")" + DefaultTextExt;
            }

            do
            {
                InputBoxResult res = InputBox.Show(InputboxPrompt, InputboxTitle, DefaultText, null);

                if (!res.OK)
                    break;

                res.Text = res.Text.Trim();

                if (!KfsPath.IsValidFileName(res.Text))
                {
                    Misc.KwmTellUser("Invalid file or folder name: cannot contain any of the following characters:" +
                                      Environment.NewLine +
                                      "*, \\, /, ?, :, |, <, >", "Error creating new File or Folder", MessageBoxIcon.Error);
                    continue;
                }

                if (lvFileList.Items.Find(res.Text, false).Length != 0)
                {
                    Misc.KwmTellUser("A file or folder with the name you specified already exists. Specify a different name.", "Error creating new file or folder", MessageBoxIcon.Error);
                    DefaultText = res.Text;
                    continue;
                }

                return res.Text;
            }
            while (true);

            return "";
        }

        /// <summary>
        /// Check if a generic action can be taken on a given item
        /// (Open, Delete)
        /// </summary>
        /// <param name="itm"></param>
        /// <returns></returns>
        private bool IsItemValidForAction(KListViewItem itm)
        {
            return (itm.Status == PathStatus.Directory ||
                    itm.Status == PathStatus.ModifiedCurrent ||
                    itm.Status == PathStatus.NotDownloaded ||
                    itm.Status == PathStatus.UnmodifiedCurrent ||
                    itm.Status == PathStatus.UnmodifiedStale ||
                    itm.Status == PathStatus.NotAdded);
        }

        /// <summary>
        /// Must be called after a menu item's handler has executed.
        /// </summary>
        private void OnActionCompleted(string _caller)
        {
            UnsetItemClickedFlag();
            Gate.GateExit(_caller);
        }

        /// <summary>
        /// Select the given folder in the treeview. The
        /// ListView will be automatically refreshed with
        /// that new folder's content.
        /// </summary>
        /// <param name="relativePath"></param>
        private void OpenFolder(string relativePath)
        {
            KTreeNode node = GetNodeFromPath(relativePath);
            Debug.Assert(node != null, "Node cannot be null");
            tvFileTree.SelectedNode = node;
        }

        /// <summary>
        /// Open all the files that are selected in the Listview.
        /// </summary>
        private void OpenSelectedFiles()
        {
            List<string> toOpen = new List<string>();
            List<string> toSyncAndOpen = new List<string>();

            if (lvFileList.SelectedItems.Count > 10)
            {
                String msg = "You are about to open " + lvFileList.SelectedItems.Count + 
                             " items. This process can be very long and cause your computer" +
                             " to stop responding during the operation." +
                             " Are you sure you want to continue?";
                DialogResult res = Misc.KwmTellUser(msg, "Long operation warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (res != DialogResult.Yes) return;
            }

            foreach (KListViewItem itm in lvFileList.SelectedItems)
            {
                if (Gate.CanSynchronize(itm) && 
                    (itm.Status == PathStatus.NotDownloaded ||
                     itm.Status == PathStatus.UnmodifiedStale))
                {
                    toSyncAndOpen.Add(itm.Path);
                }
                else if (itm.Status == PathStatus.NotDownloaded ||
                         itm.Status == PathStatus.UnmodifiedStale)
                {
                    // Can't download this file
                    Misc.KwmTellUser("Unable to open " + itm.Path + ": download is not allowed. Please try again later.");
                }
                else if (Gate.CanOpen(itm))
                {
                    toOpen.Add(SrcApp.Share.MakeAbsolute(itm.Path));
                }
                else if (itm.Status == PathStatus.Directory)
                {
                    continue;
                }
                else
                {
                    Misc.KwmTellUser("Unable to open " + itm.Path + ".");
                }
            }

            Gate.SynchronizePath(toSyncAndOpen, true);

            foreach (string fullPath in toOpen)
                Misc.OpenFileInWorkerThread(fullPath);
        }

        private KTreeNode GetNodeFromPath(string path)
        {
            TreeNode node = tvFileTree.Nodes[0];
            TreeNode[] nodes = node.Nodes.Find(path, true);
            if (nodes.Length != 1)
                return null;
            else
                return nodes[0] as KTreeNode;
        }

        /// <summary>
        /// Sets the proper flags needed whenever a context menu is opening.
        /// </summary>
        /// <returns>True to cancel the menu opening, false otherwise.
        /// Cancellation can be required if the UI has changed after entering the gate.</returns>
        private bool ContextMenuOpening()
        {
            if (!m_contextMenuOpenedFlag)
            {
                String s = "ContextMenuOpening";
                UInt64 c = m_uiUpdateCounter;
                Gate.GateEntry(s);
                if (c != m_uiUpdateCounter)
                {
                    Gate.GateExit(s += " refused, share has been modified.");
                    m_contextMenuOpenedFlag = false;
                    return true;
                }

                if (m_itemClickedFlag)
                {
                    Debug.Assert(false, "ContextMenuOpening : m_itemClickedFlag is true and should not be.");
                    UnsetItemClickedFlag();
                }
            }

            m_contextMenuOpenedFlag = true;

            return false;
        }

        private void ContextMenuClosing()
        {
            Logging.Log("ContextMenuClosing called.");
            // Exit gate only if we did not click on an item:
            // the item handler will take care of leaving the 
            // gate by itself.
            if (!m_itemClickedFlag)
                Gate.GateExit("ContextMenuClosing");

            m_contextMenuOpenedFlag = false;
        }

        /// <summary>
        /// Return true if all selected items are 'add-able'
        /// </summary>
        /// <returns></returns>
        private bool EntireLvSelectionIsNotAdded()
        {
            foreach (KListViewItem itm in lvFileList.SelectedItems)
            {
                // If any item is either not NotAdded AND not a local dir, 
                // or a file that is being transferred, return false.
                if (itm.Status != PathStatus.NotAdded &&
                   !(itm.Status == PathStatus.Directory && !itm.OnServer))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Return true if the Listview selected items all have the
        /// same PathStatus
        /// </summary>
        /// <param name="_desiredStatus"></param>
        /// <returns></returns>
        private bool EntireLvSelectionHasStatus(PathStatus _desiredStatus)
        {
            foreach (KListViewItem itm in lvFileList.SelectedItems)
            {
                if (itm.Status != _desiredStatus)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// This method is used by Drag and Drop events to set the appropriate
        /// allowed effect on the Listview.
        /// We allow drop if the destination is different than the source.
        /// </summary>
        private void SetLvDragDropEffect(DragEventArgs e)
        {
            bool isKfsItem = IsDragDropKfsItem(e);
            bool isFileDrop = IsDragDropFileDrop(e);
            bool isMsoDrop = IsDragDropMsoDrop(e);

            bool isASubDir = false;
            
            // Allow only FileDrop and KfsItem data format to be dropped in here.
            if (!(isKfsItem || isFileDrop || isMsoDrop) || !m_canDrop || !m_canDrag)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            // Set the selected node properly.
            Point pt = lvFileList.PointToClient(new Point(e.X, e.Y));
            KListViewItem itemUnder = (KListViewItem)lvFileList.GetItemAt(pt.X, pt.Y);

            // Unselect the temp item.
            if (m_dragDropLvTempDest != null && !IsPathInDragSelection(m_dragDropLvTempDest.Path, e))
                m_dragDropLvTempDest.Selected = false;

            // See if we must select another temp item.
            if (itemUnder != null && itemUnder.Status == PathStatus.Directory)
            {
                if (isMsoDrop || !IsPathInDragSelection(itemUnder.Path, e))
                {
                    // If the mouse is over a directory, select it.
                    itemUnder.Selected = true;
                    m_dragDropLvTempDest = itemUnder;
                }
            }

            // Validate if the destination is valid.

            if (isMsoDrop)
            {
                if (itemUnder != null && itemUnder.Status != PathStatus.Directory)
                    e.Effect = DragDropEffects.None;
                else
                    e.Effect = DragDropEffects.Copy;

                return;
            }

            string srcFullPath = GetDragDropSourceDir(e);
            string dstRelativePath = GetLvDragDropDestDir(e);

            if (isKfsItem)
            {
                if (srcFullPath == dstRelativePath || itemUnder == null || itemUnder.Status != PathStatus.Directory || IsPathInDragSelection(itemUnder.Path, e))
                    e.Effect = DragDropEffects.None;
                else
                    e.Effect = DragDropEffects.Copy;
            }
            else if (isFileDrop)
            {
                foreach (string p in (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop))
                {
                    if (Directory.Exists(p) &&
                    KfsPath.GetWindowsFilePath(SrcApp.Share.MakeAbsolute(dstRelativePath), true).StartsWith(p))
                    {
                        isASubDir = true;
                        break;
                    }
                }
                if (srcFullPath == KfsPath.GetWindowsFilePath(SrcApp.Share.MakeAbsolute(dstRelativePath), false) ||
                    (itemUnder != null &&  IsPathInDragSelection(itemUnder.Path, e)) || isASubDir)
                    e.Effect = DragDropEffects.None;
                else
                    e.Effect = DragDropEffects.Copy;
            }
            
        }

        private void SetTvDragDropEffect(DragEventArgs e)
        {
            // FIXME: allow MSO drop in the treeview.
            bool isKfsItem = IsDragDropKfsItem(e);
            bool isFileDrop = IsDragDropFileDrop(e);
            bool isASubDir = false;

            // Allow only KfsItem data format to be dropped in here.
            if (!(isKfsItem || isFileDrop) || !m_canDrop || !m_canDrag)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            string srcFullPath = GetDragDropSourceDir(e);
            string dstRelativePath = GetTvDragDropDestDir(e);

            Point pt = tvFileTree.PointToClient(new Point(e.X, e.Y));
            KTreeNode nodeUnder = (KTreeNode)tvFileTree.GetNodeAt(pt);
            // See if we must select another temp item.
            if (nodeUnder != null)
            {
                // If the mouse is over a directory, select it.
                tvFileTree.SelectedNode = nodeUnder;
                m_dragDropTvTempDest = nodeUnder;
            }
            if (isKfsItem)
            {
                List<KListViewItem> dirs = DirectoriesInSelection((KListViewItem[])e.Data.GetData("KfsItem"));
                foreach (KListViewItem item in dirs)
                {
                    if (dstRelativePath.StartsWith(item.Path))
                    {
                        isASubDir = true;
                        break;
                    }
                }

                // Validate if the destination is valid.
                if (nodeUnder == null || dstRelativePath == srcFullPath || isASubDir || IsPathInDragSelection(dstRelativePath, e))
                    e.Effect = DragDropEffects.None;
                else
                    e.Effect = DragDropEffects.Copy;
            }
            else if (isFileDrop)
            {
                foreach (string p in (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop))
                {
                    if (Directory.Exists(p) &&
                    KfsPath.GetWindowsFilePath(SrcApp.Share.MakeAbsolute(dstRelativePath), true).StartsWith(p))
                    {
                        isASubDir = true;
                        break;
                    }
                }
                if (srcFullPath == KfsPath.GetWindowsFilePath(SrcApp.Share.MakeAbsolute(dstRelativePath), false) ||
                    (IsPathInDragSelection(dstRelativePath, e)) || isASubDir)
                    e.Effect = DragDropEffects.None;
                else
                    e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// Return true if the given relative path (slash terminated)
        /// is one of the items being moved.
        /// </summary>
        private bool IsPathInDragSelection(string relPath, DragEventArgs e)
        {

            bool isKfsItem = IsDragDropKfsItem(e);
            bool isFileDrop = IsDragDropFileDrop(e);
            bool isMsoDrop = IsDragDropMsoDrop(e);

            Debug.Assert(isKfsItem || isFileDrop || isMsoDrop);

            // Always compare with slashes so that this works in all cases.
            //relPath = KfsPath.AddTrailingSlash(relPath);

            if (isKfsItem)
            {
                string[] FileNames = GetKListViewItemPath((KListViewItem[])e.Data.GetData("KfsItem"));
                foreach (string s in FileNames)
                {
                    if (relPath == s)
                        return true;
                }
            }
            else if (isFileDrop)
            {
                // Check for absolute paths
                string[] FileNames = (String[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string s in FileNames)
                {
                    if (SrcApp.Share.MakeAbsolute(relPath) == KfsPath.AddTrailingSlash(s))
                        return true;
                }
            }

            // Always return false if dragging an MSO object.

            return false;
        }

        private string[] GetKListViewItemPath(KListViewItem[] items)
        {
            return Array.ConvertAll<KListViewItem, string>(items, delegate(KListViewItem src) { return src.Path; });
        }

        /// <summary>
        /// Return a list of all the selected KListViewItems that are directories.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private List<KListViewItem> DirectoriesInSelection(KListViewItem[] items)
        {
            List<KListViewItem> dirs = new List<KListViewItem>();
            foreach (KListViewItem item in items)
            {
                if (item.Status == PathStatus.Directory)
                {
                    dirs.Add(item);
                }
            }
            return dirs;
        }

        /// <summary>
        /// Return the path of the dropped items.
        /// If the Data Format is of type KfsItem, the relative
        /// path will be returned.
        /// If the Data Format is of type FileDrop, the file system
        /// path will be returned.
        /// Do not call on other formats, it would not make sense.
        /// </summary>
        private string GetDragDropSourceDir(DragEventArgs e)
        {
            bool IsFileDrop = IsDragDropFileDrop(e);
            bool IsKfsItem = IsDragDropKfsItem(e);

            Debug.Assert(IsFileDrop || IsKfsItem);

            string[] sources;
            if (IsKfsItem)
                sources = GetKListViewItemPath((KListViewItem[])e.Data.GetData("KfsItem"));
            else
                sources = (String[])e.Data.GetData(DataFormats.FileDrop);

            Debug.Assert(sources != null);
            Debug.Assert(sources.Length > 0);

            // All the source files or folder come from the same location.
            string dirName = KfsPath.DirName(sources[0]);

            // Remove trailing slash
            if (dirName != "")
                dirName = dirName.Remove(dirName.Length - 1);

            return dirName;
        }

        /// <summary>
        /// Return true if the DragDrop item is of type KfsItem.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsDragDropKfsItem(DragEventArgs e)
        {
            return e.Data.GetDataPresent("KfsItem");
        }

        /// <summary>
        /// Return true if the DragDrop item is of type FileDrop.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsDragDropFileDrop(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        /// <summary>
        /// Return true if the DragDrop item is an Outlook message.
        /// </summary>
        private bool IsDragDropMsoDrop(DragEventArgs e)
        {
            return (e.Data.GetDataPresent("FileGroupDescriptor") &&
                    e.Data.GetDataPresent("FileGroupDescriptorW") &&
                    e.Data.GetDataPresent("FileContents"));
        }

        /// <summary>
        /// Return the relative path of the current Listview drop location.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private string GetLvDragDropDestDir(DragEventArgs e)
        {
            Point pt = lvFileList.PointToClient(new Point(e.X, e.Y));
            KListViewItem itemUnder = (KListViewItem)lvFileList.GetItemAt(pt.X, pt.Y);
            if (itemUnder != null)
            {
                if (itemUnder.Status == PathStatus.Directory)
                {
                    return itemUnder.Path;
                }
            }
            return ((KTreeNode)tvFileTree.ClickedNode).KFullPath;

        }

        /// <summary>
        /// Return the relative path of the current Treeview drop location.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private string GetTvDragDropDestDir(DragEventArgs e)
        {
            Point pt = tvFileTree.PointToClient(new Point(e.X, e.Y));
            KTreeNode node = (KTreeNode)tvFileTree.GetNodeAt(pt);

            if (node != null)
                return node.KFullPath;
            else
                return "";

        }

        /// <summary>
        /// Return the selected KListViewItem. Do not call if more
        /// than one item can be selected.
        /// </summary>
        /// <returns></returns>
        private KListViewItem GetSelectedItem()
        {
            Debug.Assert(lvFileList.SelectedItems.Count == 1);
            return lvFileList.SelectedItems[0] as KListViewItem;
        }

        /// <summary>
        /// Called when the Share tells us to update the UI view.
        /// </summary>
        public void HandleOnUiUpdateRequired(object _sender, EventArgs _args)
        {
            m_uiUpdateCounter++;
            UpdateUI();
        }

        public void HandleOnUiXferUpdateRequired(object _sender, EventArgs _args)
        {
            try
            {
                kfsTransfers.UpdateTransfers();
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
        }

        /// <summary>
        /// Add the right menus to the Listview context menu.
        /// This function does not bother with the enabled/disabled
        /// status of the menus (except for the ResolveConflict one)..
        /// </summary>
        private void UpdateLvMenus()
        {
            bool singleSelection = lvFileList.SelectedItems.Count == 1;

            if (lvFileList.SelectedItems.Count == 0)
            {
                Logging.Log("No selection");
                lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Paste"]);

                lvContextMenu.Items.Add(new ToolStripSeparator());
                lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["AddFiles"]);
                lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["New"]);
                return;
            }

            if (singleSelection)
            {
                KListViewItem SelectedItem = (KListViewItem)lvFileList.SelectedItems[0];
                // Modified-Stale conflict
                if (SelectedItem.Status == PathStatus.ModifiedStale)
                {
                    Logging.Log("Modified-Stale conflict");
                    lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["ResolveConflict"]);

                    ((ToolStripItem)m_lvMenuItems["ResolveConflict"]).Enabled =
                        Gate.CanResolveFileConflict(SelectedItem);

                    return;
                }

                // File-Dir or Dir-File conflict
                else if (SelectedItem.Status == PathStatus.DirFileConflict ||
                         SelectedItem.Status == PathStatus.FileDirConflict)
                {
                    Logging.Log("File-Dir or Dir-File conflict");
                    lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["ResolveTypeConflicts"]);
                    return;
                }
            }

            // Default
            Logging.Log("Default LV menu.");
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Open"]);
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["SaveAs"]);
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Revert"]);

            lvContextMenu.Items.Add(new ToolStripSeparator());

            // Present the Add menu only if at least one item can be added.
            if (CanAnySelectedItemDo(UIAction.Add))
                lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["DoAdd"]);
            
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Synchronize"]);

            lvContextMenu.Items.Add(new ToolStripSeparator());
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Copy"]);

            lvContextMenu.Items.Add(new ToolStripSeparator());

            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Delete"]); 
            
            lvContextMenu.Items.Add((ToolStripItem)m_lvMenuItems["Rename"]);

            
        }

        /// <summary>
        /// Given the current context, enables or disables the 
        /// various ListView context menu items.
        /// </summary>
        private void UpdateLvMenuStatuses()
        {
            ((ToolStripItem)m_lvMenuItems["DoAdd"]).Enabled = CanAnySelectedItemDo(UIAction.Add);
            ((ToolStripItem)m_lvMenuItems["Open"]).Enabled = CanAnySelectedItemDo(UIAction.Open);
            ((ToolStripItem)m_lvMenuItems["Synchronize"]).Enabled = CanAnySelectedItemDo(UIAction.Synchronize);

            bool canDelOrMove = CanAnySelectedItemDo(UIAction.DeleteOrMove);
            ((ToolStripItem)m_lvMenuItems["Delete"]).Enabled = canDelOrMove;
            ((ToolStripItem)m_lvMenuItems["Rename"]).Enabled = canDelOrMove && lvFileList.SelectedItems.Count == 1;

            ((ToolStripItem)m_lvMenuItems["AddFiles"]).Enabled = !m_forceDisableUI && (SrcApp.Share.AllowedOp == AllowedOpStatus.All);
            ((ToolStripItem)m_lvMenuItems["New"]).Enabled = !m_forceDisableUI && (SrcApp.Share.AllowedOp == AllowedOpStatus.All);

            ((ToolStripItem)m_lvMenuItems["SaveAs"]).Enabled = CanAnySelectedItemDo(UIAction.SaveAs);
            ((ToolStripItem)m_lvMenuItems["Revert"]).Enabled = CanAnySelectedItemDo(UIAction.Revert);

            ((ToolStripItem)m_lvMenuItems["Copy"]).Enabled = CanAnySelectedItemDo(UIAction.Copy);
            ((ToolStripItem)m_lvMenuItems["Paste"]).Enabled = Gate.CanPaste();
        }

        /// <summary>
        /// Add the right menus to the Treeview context menu.
        /// This function does not bother with the enabled/disabled
        /// status of the menus.
        /// </summary>
        private void UpdateTvMenus()
        {

            KTreeNode clickedNode = tvFileTree.ClickedNode as KTreeNode;
            Debug.Assert(clickedNode != null);

            tvContextMenu.Items.Add((ToolStripItem)m_tvMenuItems["Expand"]);

            /* Text is Expand by default */
            if (clickedNode.IsExpanded)
                tvContextMenu.Items["Expand"].Text = "Collapse";
            else
                tvContextMenu.Items["Expand"].Text = "Expand";


            tvContextMenu.Items.Add(new ToolStripSeparator());
            tvContextMenu.Items.Add((ToolStripItem)m_tvMenuItems["Synchronize"]);

            tvContextMenu.Items.Add(new ToolStripSeparator());
            tvContextMenu.Items.Add((ToolStripItem)m_tvMenuItems["Delete"]);
            tvContextMenu.Items.Add((ToolStripItem)m_tvMenuItems["Rename"]);
        }

        /// <summary>
        /// Given the current context, enables or disables the 
        /// various Treeview context menu items.
        /// </summary>
        private void UpdateTvMenuStatuses()
        {
            KTreeNode clickedNode = tvFileTree.ClickedNode as KTreeNode;
            Debug.Assert(clickedNode != null);

            // Can the clicked node be synchronized?
            ((ToolStripItem)m_tvMenuItems["Synchronize"]).Enabled = Gate.CanSynchronize((KTreeNode)tvFileTree.ClickedNode);

            // Renaming the root is forbidden.
            bool canDelOrMove = Gate.CanDeleteOrMove(tvFileTree.ClickedNode as KTreeNode) && tvFileTree.ClickedNode.Name != "";
            tvContextMenu.Items["Rename"].Enabled = canDelOrMove;
            tvContextMenu.Items["Delete"].Enabled = canDelOrMove;

            // Does the clicked node has childrens? This action can be
            // chosen by the user regardless of the Share's status.
            if (tvFileTree.ClickedNode.Nodes.Count == 0)
                tvContextMenu.Items["Expand"].Enabled = false;
            else
                tvContextMenu.Items["Expand"].Enabled = true;
        }

        /// <summary>
        /// Update user actions that are triggable from outside the context menus
        /// and that must be updated LIVE (i.e. toolbar buttons).
        /// </summary>
        private void UpdateUIActions()
        {
            bool enabled = (CanWorkOnline() &&
                            SrcApp.Share.AllowedOp == AllowedOpStatus.All &&
                            !m_forceDisableUI); 
            btnNew.Enabled = enabled;
            btnSyncAll.Enabled = enabled;
            btnImport.Enabled = enabled;
        }

        private void UpdateLV()
        {
            Bitmap img = kwm.KwmAppControls.Properties.Resources.folderClosed;
            img.MakeTransparent();
            m_lvImageListMgr.AddIcon(img, "FolderClosed");

            lvFileList.Items.Clear();

            KTreeNode parent = (KTreeNode)tvFileTree.SelectedNode;
            if (parent == null)
                return;
            
            foreach (KeyValuePair<string, KListViewItem> content in parent.Childs)
            {
                KListViewItem i = content.Value as KListViewItem;

                Debug.Assert(i != null);
                if (i.Status != PathStatus.ServerGhost)
                    lvFileList.Items.Add((KListViewItem)content.Value);
            }
        }

        private void UpdateTV()
        {
            Logging.Log("UpdateTV");
            tvFileTree.Nodes.Clear();
            bool updateTvFlag = false;
            bool updateLvFlag = true;

            m_ignoreExpandFlag = true;

            if (CanWorkOffline())
            {
                // The status view is not up to date, request an update.
                if (SrcApp.Share.StatusView.Root == null)
                    SrcApp.Share.RequestStatusViewUpdate("UpdateTV()");
                
                // We need to update the tree view.
                else
                    updateTvFlag = true;
            }

            if (updateTvFlag)
            {
                KfsStatusPath rootStatusPath = SrcApp.Share.StatusView.Root;

                // Add the TV root.
                KTreeNode root = new KTreeNode(rootStatusPath, m_lvImageListMgr, SrcApp.Helper);

                root.Name = "";

                tvFileTree.Nodes.Add(root);
                
                foreach (KfsStatusPath p in rootStatusPath.ChildTree.Values)
                {
                    if (p.Status == PathStatus.Directory) UpdateTV(p, root);
                }

                root.Expand();

                if (Settings.SelectedDir == "")
                {
                    tvFileTree.SelectedNode = tvFileTree.Nodes[0];
                }
                else
                {
                    TreeNode[] nodes = tvFileTree.Nodes.Find(Settings.SelectedDir, true);

                    if (nodes.Length == 1)
                        tvFileTree.SelectedNode = nodes[0];
                    else
                    {
                        // The initially selected folder was moved or deleted.
                        // Select the root. This causes an implicit update.
                        tvFileTree.SelectedNode = root;
                        updateLvFlag = false;
                    }
                }

                if (Settings.IsSelectedDirExpanded)
                    tvFileTree.SelectedNode.Expand();
            }

            m_ignoreExpandFlag = false;

            if (updateLvFlag) UpdateLV();
        }

        private void UpdateTV(KfsStatusPath path, KTreeNode node)
        {
            KTreeNode self = new KTreeNode(path, m_lvImageListMgr, SrcApp.Helper);
            self.Name = path.Path;

            node.Nodes.Add(self);

            foreach (KfsStatusPath p in path.ChildTree.Values)
            {
                if (p.Status == PathStatus.Directory)
                    UpdateTV(p, self);
            }

            KfsLocalDirectory localDir = SrcApp.Share.LocalView.GetObjectByPath(path.Path) as KfsLocalDirectory;
            KfsServerDirectory serverDir = SrcApp.Share.ServerView.GetObjectByPath(path.Path) as KfsServerDirectory;
            if (localDir != null && localDir.ExpandedFlag ||
                serverDir != null && serverDir.ExpandedFlag)
            {
                self.Expand();
            }
        }

        /// <summary>
        /// This is used in order to disable some menu items
        /// if none of the selected items can handle a given action.
        /// It serves a "be nicer to the user" purpose.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private bool CanAnySelectedItemDo(UIAction action)
        {
            switch (action)
            {
                case UIAction.Add:
                    foreach (KListViewItem itm in lvFileList.SelectedItems)
                    {
                        if (Gate.CanAdd(itm))
                            return true;
                    }
                    return false;

                case UIAction.Open:
                    foreach (KListViewItem itm in lvFileList.SelectedItems)
                    {
                        if (itm.Status == PathStatus.Directory || Gate.CanOpen(itm))
                            return true;
                    }
                    return false;

                case UIAction.Synchronize:
                    foreach (KListViewItem itm in lvFileList.SelectedItems)
                    {
                        if (Gate.CanSynchronize(itm))
                            return true;
                    }
                    return false;

                case UIAction.DeleteOrMove:
                    foreach (KListViewItem itm in lvFileList.SelectedItems)
                    {
                        if (Gate.CanDeleteOrMove(itm))
                            return true;
                    }
                    return false;

                case UIAction.SaveAs:
                    if (lvFileList.SelectedItems.Count != 1)
                        return false;

                    return Gate.CanSaveAs(lvFileList.SelectedItems[0] as KListViewItem);

                case UIAction.Revert:
                    foreach (KListViewItem itm in lvFileList.SelectedItems)
                    {
                        if (Gate.CanRevert(itm))
                            return true;
                            
                    }
                    return false;

                case UIAction.Copy:
                    foreach (KListViewItem itm in lvFileList.SelectedItems)
                    {
                        if (Gate.CanCopy(itm))
                            return true;
                    }
                    return false; 

                default:
                    Debug.Assert(false, "Wrong usage of CanAnySelectedItemDo.");
                    return false;
            }
        }

        private void SetItemDraggingStatus(bool isDragging)
        {
            m_isDraggingItem = isDragging;
        }

        /// <summary>
        /// Go in stale or live view.
        /// </summary>
        private void UpdateStaleStatus()
        {
            if (!CanWorkOffline()) GoLive();
            else if (SrcApp.Share.ServerOpFsFailFlag) GoStale(false);
            else if (SrcApp.Share.TypeConflictFlag) GoStale(true);
            else GoLive();
        }

        /// <summary>
        /// Set the appropriate UI when the unified view becomes stale.
        /// </summary>
        private void GoStale(bool typeConflict)
        {
            splitContainer1.Panel1Collapsed = false;

            string staleMsg = "";

            if (typeConflict)
            {
                staleMsg = "One or more files have conflicting types.";
            }
            else
            {
                staleMsg = "Unable to apply changes to your workspace.";
            }

            staleMsg += " Click on \"Resolve\" to fix the situation.";

            lnkStale.Text = staleMsg;

            splitContainer2.Enabled = m_flySoloFlag;
        }

        /// <summary>
        /// Set the appropriate UI when the unified view is not stale anymore.
        /// </summary>
        private void GoLive()
        {
            splitContainer1.Panel1Collapsed = true;
            splitContainer2.Enabled = true;
            m_flySoloFlag = false;
        }
        #endregion

        /*
         * Event handlers for Designer-created graphical elements
         * All these methods are TOP-LEVEL HANDLERS
         */
        /// <summary>
        /// This is a direct action from the UI, not already protected by the gate. 
        /// Make sure we enter the Gate and exit.
        /// </summary>
        private void btnImport_Click(object sender, EventArgs e)
        {
            string s = "btnAddFiles_Click";
            try
            {
                UInt64 c = m_uiUpdateCounter;
                Gate.GateEntry(s);
                if (c != m_uiUpdateCounter)
                {
                    s += " refused, share has been modified.";
                    return;
                }

                KTreeNode currentDir = (KTreeNode)tvFileTree.SelectedNode;
                Gate.AddExternalFiles(currentDir.KFullPath, null, false, new GateConfirmActions());
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                Gate.GateExit(s);
            }
        }


        private void importFilesToolStrip_Click(object sender, EventArgs e)
        {
            try
            {
                KTreeNode currentDir = (KTreeNode)tvFileTree.SelectedNode;
                Gate.AddExternalFiles(currentDir.KFullPath, null, false, new GateConfirmActions());
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("importFilesToolStrip_Click");
            }
        }

        private void addFolderToolStrip_Click(object sender, EventArgs e)
        {
            try
            {
                KTreeNode currentDir = (KTreeNode)tvFileTree.SelectedNode;
                Gate.AddExternalFiles(currentDir.KFullPath, null, true, new GateConfirmActions());
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                OnActionCompleted("addFolderToolStrip_Click");
            }
        }

        private void btnSyncAll_Click(object sender, EventArgs e)
        {
            string s = "btnSyncAll_Click";
            try
            {
                UInt64 c = m_uiUpdateCounter;
                Gate.GateEntry(s);
                if (c != m_uiUpdateCounter)
                {
                    s += " refused, share has been modified.";
                    return;
                }

                List<string> toSync = new List<string>();
                toSync.Add("");
                Gate.SynchronizePath(toSync, false);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                Gate.GateExit(s);
            }
        }

        private void btnViewOfflineFiles_Click(object sender, EventArgs e)
        {
            try
            {
                String ExplorePath;

                KTreeNode selectedNode = tvFileTree.SelectedNode as KTreeNode;
                if (selectedNode != null)
                    ExplorePath = SrcApp.Share.MakeAbsolute(selectedNode.KFullPath);
                else
                    ExplorePath = SrcApp.Share.ShareFullPath;

                Syscalls.ShellExecute(IntPtr.Zero, "explore", ExplorePath, null, null, Syscalls.SW.SW_NORMAL);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvFileTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (m_updateLvOnTvSelectionChange)
                {
                    Settings.SelectedDir = ((KTreeNode)e.Node).KFullPath;
                    Settings.IsSelectedDirExpanded = e.Node.IsExpanded;
                    SrcApp.SetDirty("Changing the SelectedNode");
                    UpdateLV();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_DoubleClick(object sender, EventArgs e)
        {
            string s = "lvFileList_DoubleClick";
            try
            {
                UInt64 c = m_uiUpdateCounter;
                Gate.GateEntry(s);
                if (c != m_uiUpdateCounter)
                {
                    s += " refused, share has been modified.";
                    return;
                }

                if (lvFileList.SelectedItems.Count != 1)
                    return;

                KListViewItem itm = (KListViewItem)lvFileList.SelectedItems[0];
                if (itm.Status == PathStatus.Directory)
                    OpenFolder(itm.Path);
                else
                    OpenSelectedFiles();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                Gate.GateExit(s);
            }
        }

        private void lvContextMenu_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                // If the UI has changed under our feet, do not allow 
                // the context menu to be shown.
                if (ContextMenuOpening())
                {
                    e.Cancel = true;
                    return;
                }

                /* If no menu items were in lvContextMenu when this was
                 * called, e.Cancel is set to true by the system */
                e.Cancel = false;
                lvContextMenu.Items.Clear();

                // 1. What menus do we show.
                UpdateLvMenus();

                // 2. What menus are enabled and what are not.
                UpdateLvMenuStatuses();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvContextMenu_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                if (tvFileTree.ClickedNode == null)
                {
                    e.Cancel = true;
                }

                // If the UI has changed under our feet, do not allow 
                // the context menu to be shown.
                if (ContextMenuOpening())
                {
                    e.Cancel = true;
                    return;
                }

                // If no menu items were in lvContextMenu when this was
                // called, e.Cancel is set to true by the system.
                e.Cancel = false;
                tvContextMenu.Items.Clear();

                // 1. What menus do we show.
                UpdateTvMenus();

                // 2. What menus are enabled and what are not.
                UpdateTvMenuStatuses();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
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

        private void tvContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                if (!(e.ClickedItem is ToolStripMenuItem)) return;
                SetItemClickedFlag();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void lvContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                // It is possible that ClickedItem is not a ToolStripMenuItem, it can
                // also be a ToolStripSeparator.
                ToolStripMenuItem i = e.ClickedItem as ToolStripMenuItem;
                if (i == null || i.HasDropDownItems) return;
                
                SetItemClickedFlag();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void tvFileTree_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                // AfterLabelEdit is NOT called when we cancel here.
                // Also do not allow a rename on the root.
                if (e.Node.Name == "" ||
                    !Gate.CanDeleteOrMove((KTreeNode)e.Node))
                {
                    e.CancelEdit = true;

                    if (m_tvRenameAskedByMenu)
                    {
                        m_tvRenameAskedByMenu = false;
                        Gate.GateExit("tvFileTree_BeforeLabelEdit");
                    }
                    return;
                }

                // Enter gate if the labelEdit does not come from the Rename menu
                if (!m_tvRenameAskedByMenu)
                {
                    string s = "tvFileList_BeforeLabelEdit";
                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        e.CancelEdit = true;
                        Gate.GateExit(s + " refused, share has been modified.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void tvFileTree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            bool goBackInEditMode = false;
            try
            {
                string Label = "";
                if (e.Label != null)
                    Label = e.Label.Trim();
                if (Label == "" || Label == e.Node.Text)
                    return;

                if (!KfsPath.IsValidFileName(Label))
                {
                    Misc.KwmTellUser("Invalid folder name. Note that folders cannot contain any of the following characters:" +
                                      Environment.NewLine +
                                      "*, \\, /, ?, :, |, <, >", "Error Renaming File or Folder", MessageBoxIcon.Error);
                    goBackInEditMode = true;
                    return;
                }

                // FIXME Check if destination already exist
                if (Label != null && e.Node.Text != Label)
                {
                    KTreeNode node = (KTreeNode)e.Node;
                    // Do not allow Gate.Exit in BeforeLabelEdit that 
                    // would get triggered if RenamePath causes the status
                    // view to get updated.
                    m_tvRenameAskedByMenu = false;
                    Gate.RenamePath(node.KFullPath, Label);
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                if (goBackInEditMode)
                {
                    // Make sure we simulate a Rename menu click
                    // if its not already the case, since we don't
                    // want to enter the gate again in beforeLabelEdit.
                    m_tvRenameAskedByMenu = true;
                    e.Node.BeginEdit();
                }
                else
                {
                    OnActionCompleted("tvFileTree_AfterLabelEdit");
                    m_tvRenameAskedByMenu = false;
                }

                // Do not modify the item ourself, 
                // let the pipeline update us when its done.
                e.CancelEdit = true;
            }
        }

        private void tvFileTree_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (tvFileTree.ClickedNode == null)
                    return;

                if (e.KeyCode == Keys.F2)
                {
                    // Gate will be entered in the BeginEdit event
                    // since it was not asked by the context menu system.
                    Logging.Log("tvFileTree_KeyDown : F2 presed.");
                    tvFileTree.ClickedNode.BeginEdit();
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    // Don't even think about deleting the root.
                    if (tvFileTree.ClickedNode.Name == "")
                        return;
                    // Enter gate directly here, as if the context
                    // menu was clicked.
                    string s = "tvFileTree_KeyDown (Delete)";
                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        Gate.GateExit(s + " refused, share has been modified.");
                        return;
                    }

                    HandleTreeviewDeleteMenu(sender, e);
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_KeyDown(object sender, KeyEventArgs e)
        {
            // Be carefull to enter the gate if starting an operation on the share,
            // and to exit it when the operation is completed. When calling menu handlers,
            // the gate gets exited automatically.
            try
            {
                int count = lvFileList.SelectedItems.Count;

                // Actions that can occur whatever the selection is.
                if (e.Control && e.KeyCode == Keys.A)
                {
                    foreach (KListViewItem i in lvFileList.Items)
                        i.Selected = true;

                    return;
                }
                else if (e.Control && e.KeyCode == Keys.C)
                {
                    string s = "lvFileList_KeyDown (Copy)";
                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        Gate.GateExit(s + " refused, share has been modified.");
                        return;
                    }

                    HandleListviewCopyMenu(sender, e);
                }
                else if (e.Control && e.KeyCode == Keys.V)
                {
                    if (Gate.CanPaste())
                    {
                        string s = "lvFileList_KeyDown (Paste)";
                        UInt64 c = m_uiUpdateCounter;
                        Gate.GateEntry(s);
                        if (c != m_uiUpdateCounter)
                        {
                            Gate.GateExit(s + " refused, share has been modified.");
                            return;
                        }

                        HandleListviewPasteMenu(sender, e);
                    }
                }

                if (count > 0)
                {
                    if (e.KeyCode == Keys.F2 && count == 1)
                    {
                        lvFileList.SelectedItems[0].BeginEdit();
                    }
                    else if (e.KeyCode == Keys.Delete)
                    {
                        string s = "lvFileList_KeyDown (Delete)";
                        UInt64 c = m_uiUpdateCounter;
                        Gate.GateEntry(s);
                        if (c != m_uiUpdateCounter)
                        {
                            Gate.GateExit(s + " refused, share has been modified.");
                            return;
                        }

                        HandleListviewDeleteMenu(sender, e);
                    }
                    else if (e.KeyCode == Keys.Enter)
                    {
                        string s = "lvFileList_KeyDown (Enter)";
                        UInt64 c = m_uiUpdateCounter;
                        Gate.GateEntry(s);
                        if (c != m_uiUpdateCounter)
                        {
                            Gate.GateExit(s + " refused, share has been modified.");
                            return;
                        }

                        // Open the selected folder, if more than one item is selected,
                        // do our best and open all the selected files. Selected folders
                        // will be ignored.
                        KListViewItem itm = (KListViewItem)lvFileList.SelectedItems[0];
                        if (itm.Status == PathStatus.Directory && count == 1)
                            OpenFolder(itm.Path);
                        
                        else
                            OpenSelectedFiles();

                        Gate.GateExit(s);
                    }
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            try
            {
                m_inLvBeforeLabelEdit = true;
                // Do not allow editing this label if we can't rename the item.
                if (!Gate.CanDeleteOrMove(lvFileList.Items[e.Item] as KListViewItem))
                {
                    e.CancelEdit = true;
                    if (m_lvRenamedAskedByMenu)
                    {
                        m_lvRenamedAskedByMenu = false;
                        Gate.GateExit("lvFileList_BeforeLabelEdit");
                    }
                    return;
                }
                // Enter gate if the labelEdit does not come from the Rename menu
                if (!m_lvRenamedAskedByMenu)
                {
                    string s = "lvFileList_BeforeLabelEdit";
                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        e.CancelEdit = true;
                        Gate.GateExit(s + " refused, share has been modified.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                m_inLvBeforeLabelEdit = false;
            }
        }

        private void lvFileList_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            // Exit if we are still in BeforeLabelEdit
            if (m_inLvBeforeLabelEdit)
                return;

            bool goBackInEditMode = false;
            try
            {
                string Label = "";
                if (e.Label != null)
                    Label = e.Label.Trim();
                if (Label == "" || Label == lvFileList.Items[e.Item].Text)
                    return;

                KListViewItem item = (KListViewItem)lvFileList.Items[e.Item];

                if (!KfsPath.IsValidFileName(Label))
                {
                    Misc.KwmTellUser("Invalid file name. Note that files cannot contain any of the following characters:" +
                                      Environment.NewLine +
                                      "*, \\, /, ?, :, |, <, >", "Error Renaming File or Folder", MessageBoxIcon.Error);
                    goBackInEditMode = true;
                    return;
                }

                ListViewItem[] items = lvFileList.Items.Find(Label, false);
                if (items.Length != 0)
                {
                    Misc.KwmTellUser("Cannot rename " + item.Text + ": A file or folder with the name you specified already exists. Specify a different file name.", "Error Renaming File or Folder", MessageBoxIcon.Error);
                    goBackInEditMode = true;
                    return;
                }

                if (Label != null && lvFileList.Items[e.Item].Text != Label)
                {
                    Gate.RenamePath(item.Path, Label);
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                if (goBackInEditMode)
                {
                    // Make sure we simulate a Rename menu click
                    // if its not already the case, since we don't
                    // want to enter the gate again in beforeLabelEdit.
                    m_lvRenamedAskedByMenu = true;
                    lvFileList.Items[e.Item].BeginEdit();
                }
                else
                {
                    // Leave the gate only if we're done
                    OnActionCompleted("lvFileList_AfterLabelEdit");
                    // Reset this flag only if we're done
                    m_lvRenamedAskedByMenu = false;
                }

                // Do not modify the item in any way. If the rename was a success, 
                // let the pipeline update us when its done. Otherwise, just leave the
                // item as it is.
                e.CancelEdit = true;
            }
        }

        private void lvFileList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string s = "lvFileList_ItemDrag";
            Logging.Log(1, s);
            try
            {
                if (e.Button != MouseButtons.Left)
                    return;

                m_canDrag = true;

                UInt64 c = m_uiUpdateCounter;
                Gate.GateEntry(s);
                if (c != m_uiUpdateCounter)
                {
                    s += " refused, share has been modified.";
                    m_canDrag = false;
                }
                else
                {
                    // If any selected item cannot be deleted or moved,
                    // remember that we can't allow the operation.
                    foreach (KListViewItem i in lvFileList.SelectedItems)
                    {
                        if (!Gate.CanDeleteOrMove(i))
                        {
                            m_canDrag = false;
                            break;
                        }
                    }
                }

                HookMouseHook();

                SetItemDraggingStatus(true);

                // Our DataObject can contain two different data types: KfsItems
                // and FileDrop. FileDrop is recognized by other applications, so
                // we only add the files that are present locally int he FileDrop list.
                List<KListViewItem> kfsItems = new List<KListViewItem>();
                List<String> fileDropItems = new List<String>();
                String fullPath;

                foreach (KListViewItem i in lvFileList.SelectedItems)
                {
                    kfsItems.Add(i);
                    fullPath = SrcApp.Share.MakeAbsolute(i.Path);
                    if (File.Exists(fullPath) || Directory.Exists(fullPath))
                        fileDropItems.Add(fullPath);
                }

                // Fill our DataObject with the KfsItem data type.
                DataObject data = new DataObject("KfsItem", kfsItems.ToArray());

                // If any of these items have a local copy, add them.
                if (fileDropItems.Count > 0)
                    data.SetData(DataFormats.FileDrop, fileDropItems.ToArray());
                
                // Perform the Drap and Drop operation.
                DoDragDrop(data, DragDropEffects.Copy);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            try
            {
                if (m_isDragDropInsideControls && e.Effect == DragDropEffects.Copy)
                {
                    Cursor.Current = MoveDrop;
                    e.UseDefaultCursors = false;
                }
                else
                    e.UseDefaultCursors = true;
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                e.UseDefaultCursors = true;
            }
        }


        private void lvFileList_DragEnter(object sender, DragEventArgs e)
        {
            string s = "lvFileList_DragEnter";
            try
            {
                // Enter gate only if the drag and drop operation
                // comes from outside our program
                if (!m_isDraggingItem)
                {
                    m_canDrop = true;

                    HookMouseHook();
                    SetItemDraggingStatus(true);

                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        s += " refused, share has been modified.";
                        m_canDrop = false;
                    }

                    if (m_forceDisableUI || SrcApp.Share.AllowedOp != AllowedOpStatus.All)
                        m_canDrop = false;
                }

                Debug.Assert(!m_isDragDropInsideControls);
                m_isDragDropInsideControls = true;

                SetLvDragDropEffect(e);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                SetLvDragDropEffect(e);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_DragLeave(object sender, EventArgs e)
        {
            try
            {
                Logging.Log(1, "lvFileList_DragLeave");
                if (m_dragDropLvTempDest != null)
                    m_dragDropLvTempDest.Selected = false;

                m_dragDropLvTempDest = null;
                m_isDragDropInsideControls = false;

                if (!m_isDraggingItem)
                    Gate.GateExit("lvFileList_DragLeave : not dragging anymore.");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Logging.Log(1, "lvFileList_DragDrop");
                bool IsKfsItem = IsDragDropKfsItem(e);
                bool IsFileDrop = IsDragDropFileDrop(e);
                bool IsMsoDrop = IsDragDropMsoDrop(e);

                Debug.Assert(IsKfsItem || IsFileDrop || IsMsoDrop);

                // Add files to the share if they come from outside the KWM,
                // move them if they come from inside.
                if (IsKfsItem)
                {
                    // Unselect the destination.
                    if (m_dragDropLvTempDest != null)
                        m_dragDropLvTempDest.Selected = false;

                    string[] FileNames = GetKListViewItemPath((KListViewItem[])e.Data.GetData("KfsItem"));
                    Gate.MovePath(new List<string>(FileNames), GetLvDragDropDestDir(e));
                }
                else if (IsFileDrop)
                {
                    string[] FileNames = (String[])e.Data.GetData(DataFormats.FileDrop);
                    Gate.AddExternalFiles(GetLvDragDropDestDir(e), FileNames, false, new GateConfirmActions());

                }

                else if (IsMsoDrop)
                {

                    // Wrap standard IDataObject in OutlookDataObject
                    OutlookDataObject dataObject = new OutlookDataObject(e.Data);
                    
                    // Get the names and data streams of the dropped files.
                    string[] filenames = (string[])dataObject.GetData("FileGroupDescriptorW");
                    MemoryStream[] filestreams = (MemoryStream[])dataObject.GetData("FileContents");

                    // This directory will contain the files to add to the share. We do not
                    // save the data from the Outlook item directly there since we want to
                    // change file names when dupes exist.
                    String finalDir = Path.GetTempPath() + Path.GetRandomFileName();
                    
                    Directory.CreateDirectory(finalDir);

                    for (int fileIndex = 0; fileIndex < filenames.Length; fileIndex++)
                    {
                        // Use the fileindex to get the name and data stream
                        string filename = filenames[fileIndex];
                        MemoryStream filestream = filestreams[fileIndex];

                        // Save the file stream to a random directory under its desired filename.
                        String tempDir = Path.GetTempPath() + Path.GetRandomFileName();
                        String tmpFilePath = tempDir + "\\" + filename;
                        Directory.CreateDirectory(tempDir);
                        using (FileStream outputStream = File.Create(tmpFilePath))
                        {
                            filestream.WriteTo(outputStream);
                        }
                        Misc.CopyFileAndRenameOnCol(tmpFilePath, finalDir + "\\");

                        Directory.Delete(tempDir, true);
                    }

                    List<String> toAdd = new List<string>();
                    DirectoryInfo di = new DirectoryInfo(finalDir);

                    foreach(FileInfo i in di.GetFiles())
                        toAdd.Add(i.FullName);

                    Gate.AddExternalFiles(GetLvDragDropDestDir(e), toAdd.ToArray(), false, new GateConfirmActions());
                    
                    // Remove the final dir.
                    di.Delete(true);
                }

                SetItemDraggingStatus(false);
                m_dragDropLvTempDest = null;
                m_isDragDropInsideControls = false;

                Gate.GateExit("lvFileList_DragDrop");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvFileTree_DragEnter(object sender, DragEventArgs e)
        {
            string s = "tvFileTree_DragEnter";
            try
            {
                Logging.Log(1, s);

                // Enter gate only if the drag and drop operation
                // comes from outside our program.
                if (!m_isDraggingItem)
                {
                    m_canDrop = true;

                    UInt64 c = m_uiUpdateCounter;
                    Gate.GateEntry(s);
                    if (c != m_uiUpdateCounter)
                    {
                        s += " refused, share has been modified.";
                        m_canDrop = false;
                    }
                }

                m_updateLvOnTvSelectionChange = false;

                m_dragDropTvOriginalSelectedNode = (KTreeNode)tvFileTree.SelectedNode;

                Debug.Assert(!m_isDragDropInsideControls);
                m_isDragDropInsideControls = true;

                SetTvDragDropEffect(e);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvFileTree_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                SetTvDragDropEffect(e);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvFileTree_DragLeave(object sender, EventArgs e)
        {
            try
            {
                Logging.Log(1, "tvFileTree_DragLeave");
                Debug.Assert(m_dragDropTvOriginalSelectedNode != null);

                tvFileTree.SelectedNode = m_dragDropTvOriginalSelectedNode;
                m_updateLvOnTvSelectionChange = true;
                m_isDragDropInsideControls = false;

                if (!m_isDraggingItem)
                    Gate.GateExit("lvFileList_DragLeave : not dragging anymore.");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvFileTree_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Logging.Log(1, "tvFileTree_DragDrop");
                m_updateLvOnTvSelectionChange = true;

                tvFileTree.SelectedNode = m_dragDropTvOriginalSelectedNode;
                m_dragDropLvTempDest = null;

                bool IsKfsItem = IsDragDropKfsItem(e);
                bool IsFileDrop = IsDragDropFileDrop(e);

                Debug.Assert(IsKfsItem || IsFileDrop);
                if (IsKfsItem)
                {
                    string[] FileNames = GetKListViewItemPath((KListViewItem[])e.Data.GetData("KfsItem"));
                    Gate.MovePath(new List<string>(FileNames), GetTvDragDropDestDir(e));
                }
                else if (IsFileDrop)
                {
                    string[] FileNames = (String[])e.Data.GetData(DataFormats.FileDrop);
                    Gate.AddExternalFiles(GetTvDragDropDestDir(e), FileNames, false, new GateConfirmActions());
                }

                m_isDraggingItem = false;
                m_dragDropLvTempDest = null;
                m_isDragDropInsideControls = false;

                Gate.GateExit("tvFileTree_DragDrop");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void HookMouseHook()
        {
            // Hook our mouse hook. It will be unhooked when the 
            // left or right mouse button gets released.
            if (!DesignMode)
            {
                m_mouseHookID = Syscalls.SetWindowsHookEx(14, m_mouseHookCallback, Syscalls.GetModuleHandle(null), 0);
            }
        }

        private IntPtr LowLevelMouseProc(int nCode, uint wParam, IntPtr lParam)
        {
            // WM_LBUTTONUP 0x0202, WM_RBUTTONUP 0x0205 
            if (wParam == 0x0202 || wParam == 0x0205)
            {
                if (Syscalls.UnhookWindowsHookEx(m_mouseHookID) == 0)
                    throw new Exception("LowLevelMouseProc, UnhookWindowsHookEx returned 0.\n" + Syscalls.GetLastErrorStringMessage());

                SetItemDraggingStatus(false);

                // See if we must exit the gate here.
                if (!m_isDragDropInsideControls)
                {
                    Gate.GateExit("LowLevelMouseProc : WM_LBUTTONUP outside interesting controls");
                }
            }

            // Finished with this
            return Syscalls.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private void kfsSplitter_SplitterMoved(object sender, SplitterEventArgs e)
        {
            try
            {
                if (m_canSaveWindowSettings)
                {
                    Misc.ApplicationSettings.KfsSplitterSplitDistance = kfsFilesSplitter.SplitterDistance;
                    Misc.ApplicationSettings.Save();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void AppKfsControl_Load(object sender, EventArgs e)
        {
            kfsFilesSplitter.SplitterDistance = Misc.ApplicationSettings.KfsSplitterSplitDistance;
            m_canSaveWindowSettings = true;
        }

        private void picExpand_Click(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("picExpand_Click");
                if (kfsTransferSplitter.Panel2Collapsed)
                    ExpandTransfers();
                else
                    CollapseTransfers();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void picExpand_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                if (kfsTransferSplitter.Panel2Collapsed)
                    picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupCollapseHot;
                else
                    picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupExpandHot;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void picExpand_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                if (kfsTransferSplitter.Panel2Collapsed)
                    picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupCollapse;
                else
                    picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupExpand;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lnkTransfers_Click(object sender, EventArgs e)
        {
            try
            {
                Logging.Log("picExpand_Click");
                if (kfsTransferSplitter.Panel2Collapsed)
                    ExpandTransfers();
                else
                    CollapseTransfers();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void ExpandTransfers()
        {
            kfsTransferSplitter.Panel2Collapsed = false;
            picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupExpand;
            btnClearErrors.Visible = true;
            btnCancelAll.Visible = true;
        }

        private void CollapseTransfers()
        {
            kfsTransferSplitter.Panel2Collapsed = true;
            picExpand.Image = kwm.KwmAppControls.Properties.Resources.GroupCollapse;
            btnClearErrors.Visible = false;
            btnCancelAll.Visible = false;
        }

        private void btnClearErrors_Click(object sender, EventArgs e)
        {
            try
            {
                kfsTransfers.ClearErrors();

                Debug.Assert(kfsTransfers.NbErrors == 0);

                if (kfsTransfers.NbErrors == 0)
                    btnClearErrors.Enabled = false;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnCancelAll_Click(object sender, EventArgs e)
        {
            try
            {
                kfsTransfers.CancelAllXfers();
                if (kfsTransfers.NbActiveTransfers == 0)
                    btnCancelAll.Enabled = false;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            try
            {
                // Deny resizing hidden columns. Ugly.
                if (e.ColumnIndex == 2)
                {
                    e.Cancel = true;
                    e.NewWidth = 0;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnResolve_Click(object sender, EventArgs e)
        {
            try
            {
                if (SrcApp.Share.ServerOpFsFailFlag)
                {
                    SrcApp.Share.Pipeline.Run("User wants to resolve Stale view", true);
                    if (SrcApp.Share.ServerOpFsFailFlag)
                        Misc.KwmTellUser(SrcApp.Share.ServerOpFsFailExplanation, MessageBoxIcon.Error);
                }
                else if (SrcApp.Share.TypeConflictFlag)
                {
                    Gate.ResolveTypeConflicts();
                }
                else
                {
                    // Everything is fine now.
                }

            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void errorDetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Misc.KwmTellUser(SrcApp.Share.ServerOpFsFailExplanation, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void advancedModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_flySoloFlag)
                {
                    splitContainer2.Enabled = false;
                }
                else
                {
                    String msg = "This mode is for experts only. It will give you " +
                                 "access to your files, but beware that what you will " +
                                 "see is NOT synchronized with the server." +
                                 Environment.NewLine + Environment.NewLine +
                                 "Are you sure you want to continue?";
                    DialogResult res = Misc.KwmTellUser(msg, "Teambox Workspace Manager", 
                                                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (res != DialogResult.Yes) return;

                    Logging.Log(1, "Going to Fly Solo.");
                    splitContainer2.Enabled = true;
                }

                m_flySoloFlag = !m_flySoloFlag;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void splitContextMenu_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                if (m_flySoloFlag)
                    advancedModeToolStripMenuItem.Text = "Simple mode ...";
                else
                    advancedModeToolStripMenuItem.Text = "Advanced mode ...";
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lvFileList_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            try
            {
                if (m_canSaveWindowSettings)
                {
                    Misc.ApplicationSettings.KfsNameColumnWidth = lvFileList.Columns[0].Width;
                    Misc.ApplicationSettings.KfsSizeColumnWidth = lvFileList.Columns[1].Width;
                    Misc.ApplicationSettings.KfsModDateColumnWidth = lvFileList.Columns[3].Width;
                    Misc.ApplicationSettings.KfsModByColumnWidth = lvFileList.Columns[4].Width;
                    Misc.ApplicationSettings.KfsStatusColumnWidth = lvFileList.Columns[5].Width;

                    Misc.ApplicationSettings.Save();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnNew_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
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

        private void btnImport_DropDownOpening(object sender, EventArgs e)
        {
            try
            {
                // If the UI has changed under our feet, we should not allow 
                // the context menu to be shown. However there does not
                // seem to be a way to prevent it from being showed.
                ContextMenuOpening();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnImport_DropDownClosed(object sender, EventArgs e)
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

        private void btnImport_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
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
        private void btnNew_DropDownOpening(object sender, EventArgs e)
        {
            try
            {
                // If the UI has changed under our feet, we should not allow 
                // the context menu to be shown. However there does not
                // seem to be a way to prevent it from being showed.
                ContextMenuOpening();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnNew_DropDownClosed(object sender, EventArgs e)
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

        private void btnNew_ButtonClick(object sender, EventArgs e)
        {
            try
            {
                btnNew.ShowDropDown();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void tvExpandCollapseHelper(KTreeNode node, bool expandFlag)
        {
            if (!HasShare() || m_ignoreExpandFlag) return;
            String path = node.KFullPath;
            KfsLocalDirectory localDir = SrcApp.Share.LocalView.GetObjectByPath(path) as KfsLocalDirectory;
            KfsServerDirectory serverDir = SrcApp.Share.ServerView.GetObjectByPath(path) as KfsServerDirectory;
            if (localDir != null) localDir.ExpandedFlag = expandFlag;
            if (serverDir != null) serverDir.ExpandedFlag = expandFlag;
            SrcApp.SetDirty("folder expansion changed");
        }

        private void tvFileTree_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            tvExpandCollapseHelper((KTreeNode)e.Node, false);
        }

        private void tvFileTree_AfterExpand(object sender, TreeViewEventArgs e)
        {
            tvExpandCollapseHelper((KTreeNode)e.Node, true);
        }
    }
}
