using System;
using System.Collections.Generic;
using System.Text;
using kwm.KwmAppControls;
using System.IO;
using kwm.Utils;
using System.Windows.Forms;
using System.Diagnostics;
using kwm.KwmAppControls.AppKfs;
using System.Collections;
using System.Xml;
using System.Drawing;
using Tbx.Utils;
using Iesi.Collections;

namespace kwm
{
    /// <summary>
    /// Actions that can be taken on a workspace user.
    /// </summary>
    public enum UserAction
    {
        /// <summary>
        /// Resend an invitation email to the user.
        /// </summary>
        ResendInvitation,

        /// <summary>
        /// Reset the user's password.
        /// </summary>
        ResetPassword,

        /// <summary>
        /// Disable the user account.
        /// </summary>
        ChangeDisabledAccountFlag,

        /// <summary>
        /// Delete the user from the workspace.
        /// </summary>
        Delete,

        /// <summary>
        /// Change the name of the user. Any user can set its own name, except when
        /// an Administrator has set the user's Administrative name. An administrator
        /// can set its own name or the Administrative name of any user.
        /// </summary>
        SetName,

        /// <summary>
        /// Change the role of the user (Administrator, Manager, User).
        /// </summary>
        ChangeRole,

        /// <summary>
        /// Copy the user's email address to the clipboard.
        /// </summary>
        Copy,

        /// <summary>
        /// Show the user properties dialog.
        /// </summary>
        ShowProperties
    }
    
    /// <summary>
    /// Actions that can be taken on a workspace.
    /// </summary>
    public enum KwsAction
    {
        /// <summary>
        /// Connect the workspace.
        /// </summary>
        Connect,

        /// <summary>
        /// Disconnect the workspace.
        /// </summary>
        Disconnect,

        /// <summary>
        /// Disable the workspace locally.
        /// </summary>
        Disable,

        /// <summary>
        /// Export the credential files.
        /// </summary>
        Export,

        /// <summary>
        /// Rebuild the workspace from the first event.
        /// </summary>
        Rebuild,

        /// <summary>
        /// Remove the workspace from the KWM.
        /// </summary>
        RemoveFromList,

        // The following actions require privileges.

        /// <summary>
        /// Delete the workspace from the server.
        /// </summary>
        DeleteFromServer,
        
        /// <summary>
        /// Set or unset the Secure flag.
        /// </summary>
        ChangeKwsType,

        /// <summary>
        /// Set or unset the moderation flag (freeze).
        /// </summary>
        ChangeModerationFlag,

        /// <summary>
        /// Set or unset the lock flag (deep freeze).
        /// </summary>
        ChangeLockFlag,

        /// <summary>
        /// Set or unset the KFS option to keep the deleted files
        /// on the server instead of permanently deleting them.
        /// </summary>
        ChangePreserveDeletedFlag,

        /// <summary>
        /// Change the workspace name.
        /// </summary>
        Rename
    }

    /// <summary>
    /// Broker class that handles the logic between the UI (frmMain) and
    /// the workspace manager.
    /// </summary>
    public class WmUiBroker : IUiBroker
    {
        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        private WorkspaceManager m_wm;

        /// <summary>
        /// Reference to the main form.
        /// </summary>
        private frmMain m_mainForm;

        /// <summary>
        /// Control used to display event notifications in the tray area.
        /// </summary>
        private TrayMessage m_trayMessage;

        /// <summary>
        /// Workspace list browser.
        /// </summary>
        public KwsBrowser Browser = null;

        /// <summary>
        /// Queue of WmGuiExecRequest that were posted to the workspace manager.
        /// </summary>
        public Queue<WmGuiExecRequest> WmGuiExecRequestQueue = new Queue<WmGuiExecRequest>();

        /// <summary>
        /// Number of times the UI has been reentered to display a dialog.
        /// We use this value to avoid spawning dialogs on top of dialogs.
        /// </summary>
        public UInt32 UiEntryCount = 0;

        /// <summary>
        /// True if the workspace browser should be updated in the UI.
        /// </summary>
        public bool UpdateBrowserFlag = false;

        /// <summary>
        /// True if the workspace browser should be fully rebuilt in the UI.
        /// This is the case if the list of workspaces and folders have been
        /// modified.
        /// </summary>
        public bool RebuildBrowserFlag = false;

        /// <summary>
        /// True if the selected workspace needs to be redrawn. This concerns
        /// the home tab and the user list. The application controls and the
        /// browser need not be refreshed.
        /// </summary>
        public bool UpdateSelectedKwsFlag = false;

        /// <summary>
        /// Reference to the posted GUI execution request used to refresh the
        /// UI, if any.
        /// </summary>
        public WmUiRefreshGer m_uiRefreshGer = null;

        public WmUiBroker(WorkspaceManager wm)
        {
            m_wm = wm;
            m_mainForm = new frmMain();
            m_trayMessage = new TrayMessage(wm);
            Browser = new KwsBrowser(wm);
        }

        /// <summary>
        /// Called by the Spawner to initalize the main form before the
        /// KWM application runs.
        /// </summary>
        public Form InitMainForm()
        {
            /* For internal evaluation only, do not include in a releasable version.
             * System.Media.SoundPlayer player = new System.Media.SoundPlayer(Properties.Resources.startup);
            player.Play();
            */
            m_mainForm.Initialize(this);
            m_mainForm.Load += m_wm.OnMainFormLoaded;
            return m_mainForm;
        }

        // IUiBroker interface implementation

        /// <summary>
        /// This method should be called when the UI is reentered.
        /// </summary>
        public void OnUiEntry()
        {
            UiEntryCount++;
        }

        /// <summary>
        /// This method should be called when the UI is exited.
        /// </summary>
        public void OnUiExit()
        {
            Debug.Assert(UiEntryCount > 0);
            UiEntryCount--;
            m_wm.Sm.HandleUiExit();
        }

        /// <summary>
        /// Make the KWM visible if it is minimized or not in the foreground.
        /// </summary>
        public void RequestShowMainForm()
        {
            m_mainForm.ShowMainForm();
        }

        /// <summary>
        /// Show the configuration wizard. Can be called by the UI after 
        /// a user action or automatically in WorkspaceManager.OnMainFormLoaded.
        /// </summary>
        public DialogResult ShowConfigWizard()
        {
            try
            {
                WindowWrapper wrapper = new WindowWrapper(Misc.MainForm.Handle);
                ConfigKPPWizard wiz = new ConfigKPPWizard(m_wm.KmodBroker);
                Misc.OnUiEntry();
                DialogResult res;
                if (wrapper == null) res = wiz.ShowDialog();
                else res = wiz.ShowDialog(wrapper);
                Misc.OnUiExit();
                return res;
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
                return DialogResult.Cancel;
            }
        }

        // End IUiBroker interface implementation

        /// <summary>
        /// Post a message to the UI thread.
        /// </summary>
        public void PostToUi(UiThreadMsg m)
        {
            Base.ExecInUI(new Base.EmptyDelegate(m.Run));
        }

        /// <summary>
        /// This method is called when the workspace manager is starting up to
        /// to have the broker update its state.
        /// </summary>
        public void UpdateOnStartup()
        {
            // Set a global reference to the main form. Do not set this 
            // reference earlier, the main form cannot be used until it is 
            // properly initialized.
            Base.InvokeUiControl = m_mainForm;
            Misc.MainForm = m_mainForm;

            // Likewise, set a reference to this object.
            Misc.UiBroker = this;

            // Reselect the current workspace, if any.
            RequestBrowserUiUpdate(true);
            SelectKwsInternal(Browser.SelectedKws);
        }

        public void RequestRun()
        {
            m_wm.Sm.RequestRun("I said so.");
        }

        /// <summary>
        /// Request the stale UI elements to be refreshed at the first 
        /// opportunity.
        /// </summary>
        private void RequestUiRefresh()
        {
            if (m_uiRefreshGer == null)
            {
                m_uiRefreshGer = new WmUiRefreshGer(this);
                m_wm.Sm.PostGuiExecRequest(m_uiRefreshGer, null);
            }
        }

        /// <summary>
        /// Called by the UI refresh GER event.
        /// </summary>
        public void OnUiRefreshGer(WmUiRefreshGer ger)
        {
            Debug.Assert(ger == m_uiRefreshGer);
            m_uiRefreshGer = null;
            UpdateUI();
        }

        /// <summary>
        /// Request the UI to update its list of workspaces. If rebuildFlag is
        /// true the UI will be notified that its list of workspaces no longer
        /// matches the browser list.
        /// </summary>
        public void RequestBrowserUiUpdate(bool rebuildFlag)
        {
            UpdateBrowserFlag = true;
            if (rebuildFlag) RebuildBrowserFlag = true;
            RequestUiRefresh();
        }

        /// <summary>
        /// Request the currently selected workspace or lack thereof to update
        /// its UI. 
        /// </summary>
        public void RequestSelectedKwsUiUpdate()
        {
            UpdateSelectedKwsFlag = true;
            RequestUiRefresh();
        }

        /// <summary>
        /// Request the refresh of the workspace information in the UI if it is
        /// selected.
        /// </summary>
        public void RequestKwsUiUpdateIfSelected(Workspace kws)
        {
            if (Browser.SelectedKws == kws) RequestSelectedKwsUiUpdate();
        }

        /// <summary>
        /// Process any pending UI update.
        /// </summary>
        public void UpdateUI()
        {
            if (UpdateBrowserFlag) UpdateBrowser();
            if (UpdateSelectedKwsFlag) UpdateSelectedKws();
        }

        /// <summary>
        /// Update the list of workspaces when required (deletion / creation / 
        /// status change of a workspace).
        /// </summary>
        private void UpdateBrowser()
        {
            bool rebuildFlag = RebuildBrowserFlag;
            UpdateBrowserFlag = RebuildBrowserFlag = false;
            m_mainForm.UpdateTvWorkspacesList(rebuildFlag, true);
        }

        /// <summary>
        /// Refresh the selected workspace, if any.
        /// </summary>
        private void UpdateSelectedKws()
        {
            UpdateSelectedKwsFlag = false;
            m_mainForm.UpdateUI(Browser.SelectedKws);
        }

        /// <summary>
        /// Select the specified workspace in the workspace browser and display
        /// it, if any. Unselect any selected folder node if any. The WM is
        /// not marked dirty since this call is made explicitly on startup.
        /// </summary>
        private void SelectKwsInternal(Workspace kws)
        {
            // Set the currently selected workspace and clear the currently
            // selected folder, if any.
            Browser.SelectedKws = kws;
            Browser.SelectedFolder = null;

            // Set the application controls data source.
            UpdateAppControlDataSource(kws);

            // The workspace and perhaps its parents are no longer notified.
            UnmarkNodeNotifyFlag(kws);

            // Update the workspace selection in the UI, if possible.
            if (!RebuildBrowserFlag) m_mainForm.UpdateTvWorkspacesSelection();

            // Update the tabs.
            m_mainForm.UpdateKwsTabs();

            // Request the update of the workspace.
            RequestSelectedKwsUiUpdate();
        }

        /// <summary>
        /// Update the application controls data source.
        /// </summary>
        public void UpdateAppControlDataSource(Workspace kws)
        {
            foreach (BaseAppControl ctrl in m_mainForm.AppControlTree.Values)
            {
                KwsApp app = (kws == null) ? null : kws.GetApp(ctrl.ID);

                try
                {
                    ctrl.SetDataSource(app);
                }

                // Handle the error and continue updating the controls source
                // if possible.
                catch (Exception ex)
                {
                    if (app != null) kws.HandleAppFailure(app, ex);
                    else Base.HandleException(ex, true);
                }
            }
        }

        /// <summary>
        /// Clear the selected user when a workspace is rebuilt.
        /// </summary>
        public void ClearSelectedUserOnRebuild(Workspace kws)
        {
            KwsBrowserKwsNode bNode = Browser.GetKwsNodeByKws(kws);
            bNode.SelectedUser = null;
            RequestKwsUiUpdateIfSelected(kws);
        }


        ////////////////////////////////////
        // Complex user request handlers. //
        ////////////////////////////////////

        /// <summary>
        /// Return true if the user is allowed to perform the requested action on
        /// the given user in the given workspace. If the user is not allowed, false is returned and denyReason
        /// is set with a user-friendly string.
        /// </summary>
        public bool CanPerformUserAction(UserAction action, Workspace kws, KwsUser target, ref String denyReason)
        {
            // To perform any action, we must be connected to the workspace.
            if (!kws.IsOnlineCapable())
            {
                denyReason = "You are not allowed to perform this operation: the " + Base.GetKwsString() + " is not connected.";
                return false;
            }

            switch (action)
            {
                case UserAction.ChangeRole:
                    {
                        denyReason = "Feature not available yet.";
                        return false;
                    }
                case UserAction.Copy:
                    {
                        return true;
                    }
                case UserAction.Delete:
                    {
                        denyReason = "Feature not available yet.";
                        return false;
                    }
                case UserAction.ChangeDisabledAccountFlag:
                    {
                        denyReason = "Feature not available yet.";
                        return false;
                    }
                case UserAction.ShowProperties:
                    {
                        return true;
                    }
                case UserAction.ResendInvitation:
                    {
                        denyReason = "Feature not available yet.";
                        return false;
                    }
                case UserAction.ResetPassword:
                    {
                        if (!kws.CoreData.Credentials.SecureFlag)
                        {
                            denyReason = "Feature available in Secure " + Base.GetKwsString() + " only.";
                            return false;
                        }

                        // Everyone can reset his own password. Only admins can set other
                        // people's.
                        if (target.UserID != kws.CoreData.Credentials.UserID &&
                            !kws.CoreData.Credentials.AdminFlag)
                        {
                            denyReason = "This functionnality is only available to " + Base.GetKwsString() + " Administrators and Managers.";
                            return false;
                        }

                        return true;
                    }
                case UserAction.SetName:
                    {
                        denyReason = "Changing the name of a user is not supported yet.";
                        
                        // FIXME adapt for new user powers.
                        if (kws.CoreData.Credentials.AdminFlag)
                        {
                            return false;
                        }

                        // A normal user cannot change his name if he has an
                        // Admin name set.
                        else if (kws.CoreData.Credentials.UserID == target.UserID &&
                                 target.HasAdminName())
                        {
                            denyReason = "Your name has been set by an Administrator. Only an Administrator may change it.";
                        }

                        // A normal user cannot change someone else's Admin name.
                        else if (kws.CoreData.Credentials.UserID != target.UserID)
                        {
                            denyReason = "Only Administrators can change someone else's username.";
                        }
                        
                        return false;
                    }
                default:
                    denyReason = "unknown action";
                    return false;
            }
        }

        /// <summary>
        /// Return true if the user is allowed to perform the requested action on
        /// the given user in the given workspace. If the user is not allowed, false is returned and denyReason
        /// is set with a user-friendly string.
        /// </summary>
        public bool CanPerformKwsAction(KwsAction action, Workspace targetKws, ref String denyReason)
        {
            String featureNotAvailable = "Feature not available yet.";
            String cantPerformNow = "This action cannot be performed at the moment.";
               
            // FIXME: when implementing this, don't forget to enforce
            // some limitations when dealing with a public workspace.
            // We don't want users to rename their public workspace, or to
            // change it between secure and standard. I don't even know what
            // that would do.
            denyReason = "";
            switch (action)
            {
                case KwsAction.ChangeKwsType:
                    {
                        denyReason = featureNotAvailable;
                        return false;
                    }
                case KwsAction.ChangeLockFlag:
                    {
                        denyReason = featureNotAvailable;
                        return false;
                    }
                case KwsAction.ChangeModerationFlag:
                    {
                        denyReason = featureNotAvailable;
                        return false;
                    }
                case KwsAction.Connect:
                    {
                        if (targetKws.Sm.CanWorkOnline()) return true;
                        else
                        {
                            denyReason = cantPerformNow;
                            return false;
                        }
                    }
                case KwsAction.DeleteFromServer:
                    {
                        denyReason = featureNotAvailable;
                        return false;
                    }
                case KwsAction.Disable:
                    {
                        if (targetKws.Sm.CanStop()) return true;
                        else
                        {
                            denyReason = cantPerformNow;
                            return false;
                        }
                    }
                case KwsAction.Disconnect:
                    {
                        if (targetKws.Sm.CanWorkOffline()) return true;
                        else
                        {
                            denyReason = cantPerformNow;
                            return false;
                        }
                    }
                case KwsAction.Export:
                    {
                        if( targetKws.Sm.CanExport()) return true;
                        else
                        {
                            denyReason = cantPerformNow;
                            return false;
                        }
                    }
                case KwsAction.ChangePreserveDeletedFlag:
                    {
                        denyReason = featureNotAvailable;
                        return false;
                    }
                case KwsAction.Rename:
                    {
                        denyReason = featureNotAvailable;
                        return false;
                    }
                case KwsAction.Rebuild:
                    {
                        if (targetKws.Sm.CanRebuild()) return true;
                        else
                        {
                            denyReason = cantPerformNow;
                            return false;
                        }
                    }
                case KwsAction.RemoveFromList:
                    {
                        if (targetKws.Sm.CanDelete()) return true;
                        else
                        {
                            denyReason = cantPerformNow;
                            return false;
                        }
                    }
                default:
                    {
                        denyReason = "unknown action";
                        return false;
                    }
            }
        }

        /// <summary>
        /// Ask the workspace manager to quit.
        /// </summary>
        public void RequestQuit()
        {
            m_wm.Sm.RequestStop();
        }

        /// <summary>
        /// Request the workspace specified, if any, to be selected. If 
        /// 'forcedFlag' is true, the selection will be changed even if the
        /// browser is stale. This method returns true if the selection has 
        /// changed as requested.
        /// </summary>
        public bool RequestSelectKws(Workspace kws, bool forcedFlag)
        {
            if (kws == Browser.SelectedKws ||
                (RebuildBrowserFlag && !forcedFlag) ||
                (kws != null && kws.MainStatus == KwsMainStatus.NotYetSpawned))
                return false;

            SelectKwsInternal(kws);

            // We're changing the WM state.
            m_wm.SetDirty();
            return true;
        }

        /// <summary>
        /// Request the 'intelligent' selection of the closest workspace node 
        /// to the workspace specified. In all cases, the specified workspace
        /// won't be selected after this call.
        /// </summary>
        public void RequestSelectOtherKws(Workspace kws)
        {
            KwsBrowserKwsNode node = Browser.GetKwsNodeByKws(kws);

            // Try closest up.
            for (int i = node.Index - 1; i >= 0; i--)
                if (RequestSelectKws(node.Parent.GetKwsNodeByIndex(i).Kws, true)) return;

            // Try closest down.
            for (int i = node.Index + 1; i < node.Parent.KwsNodes.Count; i++)
                if (RequestSelectKws(node.Parent.GetKwsNodeByIndex(i).Kws, true)) return;

            // Select parent folder.
            RequestSelectKws(null, true);
            Browser.SelectedFolder = node.Parent;
        }

        /// <summary>
        /// Request the specified user to be seleted in the specified
        /// workspace. If updateUIFlag is set, set the selection in the UI.
        /// </summary>
        public void RequestSelectUser(Workspace kws, UInt32 userID, bool updateUIFlag)
        {
            KwsBrowserKwsNode bNode = Browser.GetKwsNodeByKws(kws);

            // Validate the request.
            KwsUserInfo userInfo = kws.CoreData.UserInfo;
            if (!userInfo.IsUser(userID)) userID = 0;

            // Get the selected user, if any.
            KwsUser user = userInfo.GetUserByID(userID);
            if (user == null) return;

            // The user selection hasn't changed.
            if (user == bNode.SelectedUser) return;

            // We're changing the WM state.
            m_wm.SetDirty();

            // Update the selected user.
            bNode.SelectedUser = user;
            kws.FireKwsUserChanged(userID);

            if (updateUIFlag)
                RequestKwsUiUpdateIfSelected(kws);
        }

        /// <summary>
        /// Prompt the user for the new password and start the SetPwdCoreOp
        /// operation. Permissions have been checked before this call. If
        /// the check was not done properly, the server will deny the request
        /// and the raw error message will be displayed to the user.
        /// </summary>
        public void RequestSetUserPassword()
        {
            Workspace kws = Browser.SelectedKws;

            if (kws == null || m_mainForm.lstUsers.SelectedItems.Count != 1) return;

            Debug.Assert(kws.CoreData.Credentials.SecureFlag);
            KwsUser selectedUser = GetSelectedUser();
            if (selectedUser == null) return;

            frmResetPwd f = new frmResetPwd();

            Misc.OnUiEntry();
            DialogResult res = f.ShowDialog();
            Misc.OnUiExit();

            if (res != DialogResult.OK) return;

            kws.SetUserPwd(selectedUser.UserID, f.NewPassword);
        }

        /// <summary>
        /// Request the rebuilding of the workspace specified.
        /// </summary>
        public void RequestRebuildKws(Workspace kws)
        {
            if (!kws.Sm.CanRebuild()) return;

            KwsRebuildInfo info = null;
            String msg = "";

            // Use the scheduled rebuild.
            if (kws.MainStatus == KwsMainStatus.RebuildRequired)
            {
                info = kws.CoreData.RebuildInfo;
                msg = "You are about to rebuild this " + Base.GetKwsString() + ".";
            }

            // Schedule a complete rebuild.
            else
            {
                info = new KwsRebuildInfo();
                info.DeleteCachedEventsFlag = true;
                info.DeleteLocalDataFlag = true;
                msg = "You are about to rebuild this" + Base.GetKwsString() + " from its initial state." +
                      Environment.NewLine +
                      "This should only be used if you experience problems with this " + Base.GetKwsString() + ".";
            }

            msg += Environment.NewLine;
            if (info.DeleteLocalDataFlag)
                msg += Environment.NewLine +
                       "WARNING: any files you modified but did not synchronize back to the server will be lost!" +
                       Environment.NewLine +
                       Environment.NewLine;
            msg += "Are you sure you want to continue?";

            DialogResult res = Misc.KwmTellUser(msg, Base.GetKwmString(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;
            if (kws.Sm.CanRebuild())
            {
                kws.Sm.SetUserTask(KwsTask.WorkOnline);
                kws.Sm.WorsenRebuild(info);
                kws.Sm.RequestTaskSwitch(KwsTask.Rebuild);
            }
        }

        /// <summary>
        /// Request the workspace to work online.
        /// </summary>
        public void RequestWorkOnline(Workspace kws)
        {
            kws.Sm.SetUserTask(KwsTask.WorkOnline);
            kws.Sm.SetLoginType(KwsLoginType.All);
            kws.Sm.RequestTaskSwitch(KwsTask.WorkOnline);
        }

        /// <summary>
        /// Request the workspace to work offline.
        /// </summary>
        public void RequestWorkOffline(Workspace kws)
        {
            kws.Sm.SetUserTask(KwsTask.WorkOffline);
            kws.Sm.RequestTaskSwitch(KwsTask.WorkOffline);
        }

        /// <summary>
        /// Request the workspace to stop.
        /// </summary>
        public void RequestStopKws(Workspace kws)
        {
            kws.Sm.SetUserTask(KwsTask.Stop);
            kws.Sm.RequestTaskSwitch(KwsTask.Stop);
        }

        /// <summary>
        /// Request the creation of a folder. Return the node of the created folder.
        /// Warning: UpdateTvWorkspacesList() must be called after a call to this
        /// method.
        /// </summary>
        public KwsBrowserFolderNode RequestCreateFolder(KwsBrowserFolderNode parent)
        {
            // We're changing the WM state.
            m_wm.SetDirty();

            // Generate a default folder name.
            String baseName = "New folder";
            String name = baseName;
            int count = 0;
            while (parent.GetNodeByID("f_" + name) != null) name = baseName + " (" + ++count + ")";

            // Insert the node in the browser.
            return Browser.AddFolder(parent, name);
        }

        /// <summary>
        /// Request the creation of a new workspace. This will prompt the appropriate dialogs
        /// in order to gather the necessary information from the user.
        /// </summary>
        public void RequestCreateKws()
        {
            // Get the selected folder path and a sane default workspace name.

            KwsBrowserFolderNode folder = Browser.GetCurrentFolderNode();
            
            String baseKwsName = "My New " + Base.GetKwsString();
            String kwsName = baseKwsName;
            int count = 0;
            bool unique = false;

            // Loop until we find a unique workspace name in 'folder'.
            while(!unique) 
            {
                unique = true;
                foreach (KwsBrowserKwsNode n in folder.KwsNodes)
                {
                    if (n.Kws.CoreData.Credentials.KwsName == kwsName) 
                    {
                        unique = false;
                        kwsName = baseKwsName + " (" + ++count + ")";
                        break;
                    }
                }                
            }            

            Misc.OnUiEntry();
            (new KwmCreateKwsOp(m_wm, folder, kwsName)).StartOp();
            Misc.OnUiExit();
        }

        /// <summary>
        /// Request the invitation of users to the given workspace. If the skipFirstPageFlag
        /// is set to true, the wizard's initial page where the user can set a
        /// personalized invitation message is skipped.
        /// </summary>
        public KwmCoreKwsOpRes RequestInviteToKws(Workspace kws, String invitees, bool skipFirstPageFlag)
        {
            try
            {
                Misc.OnUiEntry();

                if (!kws.CoreData.Credentials.AdminFlag)
                {
                    Misc.KwmTellUser("Only a " + Base.GetKwsString() + " administrator may add new users. Please contact the " + Base.GetKwsString() + " creator.", "Permission denied", MessageBoxIcon.Information);
                    return KwmCoreKwsOpRes.None;
                }

                // Validate the email address only if the user uses the
                // quick invitation shortcut.
                if (skipFirstPageFlag && !Base.IsEmail(invitees))
                {
                    Misc.KwmTellUser("The email address you entered is not in a correct format. Please verify you entered the right email address and try again.", MessageBoxIcon.Error);
                    return KwmCoreKwsOpRes.None;
                }

                return (new KwmInviteOp(m_wm, kws, invitees, skipFirstPageFlag)).StartOp();
            }
            finally
            {
                Misc.OnUiExit();
            }
        }

        /// <summary>
        /// Request the deletion of the workspace specified.
        /// </summary>
        public void RequestRemoveKwsFromList(Workspace kws)
        {
            String msg = "You are about to remove a " + Base.GetKwsString() + " from your " + Base.GetKwmString() + "." +                         
                         Environment.NewLine +
                         "Are you sure you want to continue?";

            DialogResult res = Misc.KwmTellUser(msg, Base.GetKwmString(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;
            kws.Sm.RequestTaskSwitch(KwsTask.Delete);
        }

        /// <summary>
        /// Request the deletion of the folder specified.
        /// </summary>
        public void RequestDeleteFolder(KwsBrowserFolderNode folder)
        {
            // Cache the path, the browser content may change during the
            // prompt.
            String path = folder.FullPath;

            String msg = "Are you sure you want to remove the folder " + folder.Name + " and all its " + Base.GetKwsString() + "?";
            DialogResult res = Misc.KwmTellUser(msg, "Confirm Folder Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            // Update the folder node.
            folder = Browser.GetFolderNodeByPath(path);
            if (folder == null || folder.PermanentFlag) return;

            // We're changing the WM state.
            m_wm.SetDirty();

            // Request an UI update.
            RequestBrowserUiUpdate(true);

            // Get the list of folders and workspaces, depth first.
            List<KwsBrowserFolderNode> folderList = new List<KwsBrowserFolderNode>();
            List<KwsBrowserKwsNode> kwsList = new List<KwsBrowserKwsNode>();
            Browser.RecursiveList(false, folder, folderList, kwsList);

            // Move the workspace to the primary folder and delete them.
            foreach (KwsBrowserKwsNode kwsNode in kwsList)
            {
                Browser.Move(kwsNode, Browser.PrimaryFolder, kwsNode.Name, Browser.PrimaryFolder.KwsNodes.Count);
                kwsNode.Kws.Sm.RequestTaskSwitch(KwsTask.Delete);
            }

            // Delete the folders, depth first.
            foreach (KwsBrowserFolderNode folderNode in folderList)
            {
                // If we are deleting the selected folder, select something clever.
                if (Browser.SelectedFolder == folderNode)
                {
                    KwsBrowserFolderNode nextNodeToSelect = null;

                    // Try the following folder.
                    nextNodeToSelect = folderNode.Parent.GetFolderNodeByIndex(folderNode.Index + 1);

                    if (nextNodeToSelect == null)
                    {
                        // No following folder, try the previous one.
                        nextNodeToSelect = folderNode.Parent.GetFolderNodeByIndex(folderNode.Index - 1);
                    }

                    if (nextNodeToSelect != null)
                    {
                        // Select the folder found.
                        Browser.SelectedFolder = nextNodeToSelect;
                    }

                    else
                    {
                        // No more folders in folderNode.Parent. Select Parent.
                        Browser.SelectedFolder = folderNode.Parent;
                    }
                }

                Browser.RemoveFolder(folderNode);
            }
        }

        /// <summary>
        /// Set the 'expanded' flag value of a folder.
        /// </summary>
        public void RequestSetFolderExpansion(KwsBrowserFolderNode folder, bool expandFlag)
        {
            // We're changing the WM state.
            if (folder.ExpandedFlag != expandFlag)
            {
                m_wm.SetDirty();
                folder.ExpandedFlag = expandFlag;
            }
        }

        public void RequestExportBrowser(String exportPath, UInt64 kwsID)
        {
            m_wm.ExportKws(exportPath, kwsID);
        }

        public void RequestImportBrowser(String importPath)
        {
            try
            {
                // Get the import data.
                WmImportData data = WorkspaceManager.GetImportDataFromFile(importPath);

                // Add it to the workspace manager.
                m_wm.ImportData.Add(data);

                // Perform the import if the WM is ready for it.
                if (m_wm.MainStatus == WmMainStatus.Started)
                    m_wm.ProcessPendingKwsToImport();
            }

            catch (Exception ex)
            {
                TellUserAboutImportError(ex);
            }
        }

        public void RequestNotifyKwmStateChanged()
        {
            if (!m_wm.OutlookBroker.IsSessionOpen()) return;

            m_wm.OutlookBroker.SendNewKwmStateInfos();
        }


        /// <summary>
        /// Tell the user about an import error.
        /// 
        /// NOTE: the name 'teamboxes' MUST be hardcoded here because the
        /// flipping main form might not yet be initialized.
        /// </summary>
        public static void TellUserAboutImportError(Exception ex)
        {
            String msg = Base.FormatErrorMsg("One or more Teamboxes could not be imported", ex);
            Misc.KwmTellUser(msg, MessageBoxIcon.Error);
        }

        // *******************
        // This section contains the user notification logic.
        // *******************

        /// <summary>
        /// Called when a notification item is clicked on. The Main form is 
        /// shown if needed and the relevant workspace and application tabs 
        /// are automatically selected.
        /// </summary>
        public void HandleOnTrayClick(NotificationItem item)
        {
            if (!Misc.IsUiStateOK()) return;
            m_mainForm.ShowMainForm();
            RequestSelectKws(m_wm.GetKwsByInternalID(item.InternalWsID), true);
            m_mainForm.SetTabApplication(item.AppId);
        }

        /// <summary>
        /// Method used to update the UI in order to alert the user that 
        /// a new event was received in one of his workspaces.
        /// </summary>
        public void NotifyUser(NotificationItem item)
        {
            Debug.Assert(item != null);
            Workspace ws = m_wm.KwsTree[item.InternalWsID];

            // Show the tray message if configured to do so.
            if (Misc.ApplicationSettings.ShowNotification && item.WantShowPopup)
                m_trayMessage.ShowMessage(item);

            // Set the appropriate background color if the notification is not related to
            // the current workspace.
            if (Browser.GetSelectedKwsID() != item.InternalWsID && item.WantBoldWs)
            {
                if (MarkNodeNotifyFlag(ws))
                {
                    // Node did not have its Notify flag set, so we're changing
                    // the wmstate. Mark as dirty.
                    m_wm.SetDirty();
                    RequestBrowserUiUpdate(false);
                }
            }

            // If the main form is not the selected application, start the tray icon blinking.
            if (item.WantBlinkTrayIcon && m_mainForm.Handle != (IntPtr)Syscalls.GetForegroundWindow())
                m_mainForm.StartBlinking();
        }

        /// <summary>
        /// Mark the workspace and its parent folders as notified. Return true
        /// if the function changed a node, false otherwise.
        /// </summary>
        public bool MarkNodeNotifyFlag(Workspace kws)
        {
            KwsBrowserNode node = Browser.GetKwsNodeByKws(kws);

            if (node.NotifyFlag) return false;

            while (node != Browser.RootNode)
            {
                node.NotifyFlag = true;
                node = node.Parent;
            }

            return true;
        }

        /// <summary>
        /// Unmark the workspace as notified. Also unmarks the workspace parents
        /// as needed. Requests a UI update if required.
        /// </summary>
        private void UnmarkNodeNotifyFlag(Workspace kws)
        {
            if (kws == null) return;

            KwsBrowserNode node = Browser.GetKwsNodeByKws(kws);

            if (!node.NotifyFlag) return;

            while (node != Browser.RootNode)
            {
                node.NotifyFlag = false;
                node = node.Parent;
                if (ContainsNotifyFlagItems(node)) break;
            }

            RequestBrowserUiUpdate(false);
        }

        /// <summary>
        /// Return true if the folder contains any node that has
        /// the NotifyFlag set to true. The method is not recursive.
        /// </summary>
        private bool ContainsNotifyFlagItems(KwsBrowserNode node)
        {
            if (node is KwsBrowserKwsNode) return false;
            KwsBrowserFolderNode fNode = node as KwsBrowserFolderNode;
            foreach (KwsBrowserNode n in fNode.GetNodes())
                if (n.NotifyFlag) return true;
            return false;
        }

        /// <summary>
        /// This method should be called when an ANP message is transferred to
        /// or from a KAS.
        /// </summary>
        public void OnAnpMsgTransfer(WmKas kas, AnpMsg msg)
        {
            String text = "KAS " + kas.KasID.Host + " msg " + msg.ID + ": " + KAnpType.TypeToString(msg.Type);
            Logging.Log(text);
        }

        /// <summary>
        /// Return a set containing the email address of every user in every workspace.
        /// </summary>
        public SortedSet GetKwsEmailAddressSet()
        {
            SortedSet set = new SortedSet();
            foreach (Workspace kws in m_wm.KwsTree.Values)
                foreach (KwsUser user in kws.CoreData.UserInfo.UserTree.Values)
                    set.Add(user.EmailAddress);
            return set;
        }

        /// <summary>
        /// Return the selected user of the current workspace, null if none.
        /// </summary>
        public KwsUser GetSelectedUser()
        {
            Workspace kws = Browser.SelectedKws;
            if (kws == null || m_mainForm.lstUsers.SelectedItems.Count != 1) return null;

            String uid = m_mainForm.lstUsers.SelectedItems[0].Name;

            KwsUser selectedUser = kws.CoreData.UserInfo.UserTree[UInt32.Parse(uid)];
            return selectedUser;
        }
    }

    /// <summary>
    /// This class represents the tree containing the workspaces and their
    /// folders. 
    /// </summary>
    public class KwsBrowser
    {
        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        private WorkspaceManager m_wm;

        /// <summary>
        /// Tree mapping workspace IDs to their nodes.
        /// </summary>
        private SortedDictionary<UInt64, KwsBrowserKwsNode> m_kwsTree = new SortedDictionary<UInt64, KwsBrowserKwsNode>();

        /// <summary>
        /// Tree root node.
        /// </summary>
        public KwsBrowserFolderNode RootNode;

        /// <summary>
        /// Reference to the primary folder.
        /// </summary>
        public KwsBrowserFolderNode PrimaryFolder;

        /// <summary>
        /// Selected workspace, if any.
        /// </summary>
        public Workspace SelectedKws = null;

        /// <summary>
        /// Selected folder, if any. There can be both a selected folder and a
        /// selected workspace. However, only one node at a time can be selected 
        /// in the treeview. In that case, the selected folder node is
        /// selected in the tree view but the selected workspace is displayed
        /// in the right pane.
        /// </summary>
        public KwsBrowserFolderNode SelectedFolder = null;

        public KwsBrowser(WorkspaceManager wm)
        {
            m_wm = wm;

            // Create the root folder and the primary folder.
            RootNode = new KwsBrowserFolderNode("");
            RootNode.PermanentFlag = true;
            PrimaryFolder = AddFolder(RootNode, "My Teamboxes");
            PrimaryFolder.PermanentFlag = true;
        }

        /// <summary>
        /// Add a workspace at the end of the folder specified. Throw an
        /// exception on error.
        /// </summary>
        public KwsBrowserKwsNode AddKws(Workspace kws, KwsBrowserFolderNode dstFolder, bool firstFlag)
        {
            if (m_kwsTree.ContainsKey(kws.InternalID)) throw new Exception(Base.GetKwsString() + "already exist");
            if (dstFolder.IsRoot()) throw new Exception(Base.GetKwsString() + " must be in folders");
            KwsBrowserKwsNode node = new KwsBrowserKwsNode(kws);
            dstFolder.AddNode(node, firstFlag ? 0 : dstFolder.KwsNodes.Count);
            m_kwsTree[kws.InternalID] = node;
            return node;
        }

        /// <summary>
        /// Remove a workspace from the folder specified, if it exists. 
        /// </summary>
        public void RemoveKws(Workspace kws)
        {
            Debug.Assert(SelectedKws != kws);
            if (!m_kwsTree.ContainsKey(kws.InternalID)) return;
            KwsBrowserKwsNode node = m_kwsTree[kws.InternalID];
            node.Parent.RemoveNode(node);
            m_kwsTree.Remove(kws.InternalID);
        }

        /// <summary>
        /// Create the folder having the path specified if needed and return a
        /// reference to it. Throw an exception on error.
        /// </summary>
        public KwsBrowserFolderNode CreateFolderFromPath(String path)
        {
            KwsBrowserFolderNode curFolder = RootNode;
            if (path == "") return curFolder;
            ValidatePathFormat(path);
            foreach (String ID in SplitPath(path))
            {
                if (!ID.StartsWith("f_")) throw new Exception("invalid folder path");
                String name = ID.Substring(2);
                KwsBrowserFolderNode folder = curFolder.GetNodeByID(ID) as KwsBrowserFolderNode;
                if (folder == null) folder = AddFolder(curFolder, name);
                curFolder = folder;
            }
            return curFolder;
        }

        /// <summary>
        /// Add a folder with the name specified at the end of the folder
        /// specified. Throw an exception on error.
        /// </summary>
        public KwsBrowserFolderNode AddFolder(KwsBrowserFolderNode dstFolder, String name)
        {
            if (!KwsBrowserFolderNode.IsValidFolderName(name)) throw new Exception("invalid folder name");
            KwsBrowserFolderNode node = new KwsBrowserFolderNode(name);
            if (dstFolder.GetNodeByID(node.ID) != null) throw new Exception("destination already exists");
            dstFolder.AddNode(node, dstFolder.FolderNodes.Count);
            return node;
        }

        /// <summary>
        /// Remove the folder specified. Throw an exception on error.
        /// </summary>
        public void RemoveFolder(KwsBrowserFolderNode node)
        {
            Debug.Assert(SelectedFolder != node);
            if (node.PermanentFlag) throw new Exception("cannot remove permanent folder");
            if (node.GetNodes().Count != 0) throw new Exception("folder isn't empty");
            node.Parent.RemoveNode(node);
        }

        /// <summary>
        /// Check if the specified move is valid. If the move is valid, a null
        /// string is returned, otherwise a string describing the problem is 
        /// returned.
        /// </summary>
        public String MoveCheck(KwsBrowserNode srcNode, KwsBrowserFolderNode dstFolder, String dstName, int dstIndex)
        {
            String dstID = MakeIDForNodeName(srcNode, dstName);
            if ((srcNode is KwsBrowserFolderNode) && ((KwsBrowserFolderNode)srcNode).PermanentFlag)
                return "cannot move or rename this folder";
            if (srcNode is KwsBrowserKwsNode && dstFolder.IsRoot())
                return "workspaces must be in folders";
            if (srcNode is KwsBrowserKwsNode && dstID != srcNode.ID)
                return "cannot change workspace ID";
            if (srcNode is KwsBrowserFolderNode && !KwsBrowserFolderNode.IsValidFolderName(dstName))
                return "invalid destination name";
            if (srcNode == dstFolder || dstFolder.IsDescendantOf(srcNode)) return "invalid move";
            bool reindexFlag = (dstFolder.GetNodeByID(dstID) == srcNode);
            if (!reindexFlag && dstFolder.GetNodeByID(dstID) != null)
                return "destination already exists";
            if (!dstFolder.IsIndexValidForInsert(srcNode, dstIndex))
                return "invalid destination index";
            return null;
        }

        /// <summary>
        /// Move the node at the source specified to the destination specified
        /// with the index specified. Throw an exception on error.
        /// </summary>
        public void Move(KwsBrowserNode srcNode, KwsBrowserFolderNode dstFolder, String dstName, int dstIndex)
        {
            String MoveCheckRes = MoveCheck(srcNode, dstFolder, dstName, dstIndex);
            if (MoveCheckRes != null) throw new Exception(MoveCheckRes);

            // Adjust the destination index if we're moving the source node
            // to the last index of a different folder than its parent.
            if (dstFolder.GetNodeByID(srcNode.ID) == null &&
                dstIndex + 1 == dstFolder.KwsNodes.Count)
            {
                dstIndex++;
            }

            srcNode.Parent.RemoveNode(srcNode);
            if (srcNode is KwsBrowserFolderNode) ((KwsBrowserFolderNode)srcNode).FolderName = dstName;
            dstFolder.AddNode(srcNode, dstIndex);

            // The workspace list has changed.
            m_wm.OutlookBroker.OnKwsListChanged();
        }

        /// <summary>
        /// Return the folder node having the path specified, if any.
        /// </summary>
        public KwsBrowserFolderNode GetFolderNodeByPath(String path)
        {
            return GetNodeByPath(path) as KwsBrowserFolderNode;
        }

        /// <summary>
        /// Return the workspace node having the path specified, if any.
        /// </summary>
        public KwsBrowserKwsNode GetKwsNodeByPath(String path)
        {
            return GetNodeByPath(path) as KwsBrowserKwsNode;
        }

        /// <summary>
        /// Return the workspace node associated to the workspace specified, 
        /// if any.
        /// </summary>
        public KwsBrowserKwsNode GetKwsNodeByKws(Workspace kws)
        {
            if (!m_kwsTree.ContainsKey(kws.InternalID)) return null;
            return m_kwsTree[kws.InternalID];
        }

        /// <summary>
        /// Return the workspace node currently selected, if any.
        /// </summary>
        public KwsBrowserKwsNode GetSelectedKwsNode()
        {
            if (SelectedKws == null) return null;
            return GetKwsNodeByKws(SelectedKws);
        }

        /// <summary>
        /// Return the selected workspace ID, or 0 if none.
        /// </summary>
        public UInt64 GetSelectedKwsID()
        {
            return (SelectedKws == null ? 0 : SelectedKws.InternalID);
        }

        /// <summary>
        /// Return the selected folder node, if any. If none, return the 
        /// parent  folder node of the selected workspace, if any. If none,
        /// return PrimaryFolder.
        /// </summary>
        public KwsBrowserFolderNode GetCurrentFolderNode()
        {
            if (SelectedFolder != null) return SelectedFolder;
            KwsBrowserKwsNode selectedKws = GetSelectedKwsNode();
            if (selectedKws != null) return selectedKws.Parent;
            return PrimaryFolder;
        }

        /// <summary>
        /// Return the node having the path specified, if any.
        /// To get the root node, you can use the RootNode property
        /// directly.
        /// </summary>
        public KwsBrowserNode GetNodeByPath(String path)
        {
            if (!path.StartsWith("/")) return null;
            KwsBrowserNode currentNode = RootNode;
            foreach (String ID in SplitPath(path))
            {
                KwsBrowserFolderNode folder = currentNode as KwsBrowserFolderNode;
                if (folder == null) return null;
                currentNode = folder.GetNodeByID(ID);
                if (currentNode == null) return null;
            }

            return currentNode;
        }

        /// <summary>
        /// Decompose the path specified in its array of components.
        /// </summary>
        public static String[] SplitPath(String path)
        {
            return path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }
        
        /// <summary>
        /// Validate the format of the path specified.
        /// </summary>
        public void ValidatePathFormat(String path)
        {
            if (!path.StartsWith("/")) throw new Exception("path doesn't begin with slash");
            foreach (String c in SplitPath(path))
            {
                if (c.StartsWith("f_"))
                {
                    String name = c.Substring(2);
                    if (!KwsBrowserFolderNode.IsValidFolderName(name)) throw new Exception(name + " is not a valid folder name");
                }

                else
                {
                    UInt64.Parse(c);
                }
            }
        }

        /// <summary>
        /// Return the ID the node specified would have if it had the name
        /// specified.
        /// </summary>
        public String MakeIDForNodeName(KwsBrowserNode node, String name)
        {
            if (node is KwsBrowserKwsNode) return name;
            return "f_" + name;
        }

        /// <summary>
        /// Fill folderList and kwsList with the folders and workspaces found
        /// under the folder specified. The folder itself is added to the
        /// folder list before or after the subfolders depending on the value
        /// of addBeforeFlag.
        /// </summary>
        public void RecursiveList(bool addBeforeFlag,
                                  KwsBrowserFolderNode folder,
                                  List<KwsBrowserFolderNode> folderList,
                                  List<KwsBrowserKwsNode> kwsList)
        {
            if (addBeforeFlag) folderList.Add(folder);
            foreach (KwsBrowserFolderNode node in folder.FolderNodes)
                RecursiveList(addBeforeFlag, node, folderList, kwsList);
            foreach (KwsBrowserKwsNode node in folder.KwsNodes)
                kwsList.Add(node);
            if (!addBeforeFlag) folderList.Add(folder);
        }

        /// <summary>
        /// Export a flat list of all the paths present in the KwsBrowser to the given XmlElement.
        /// </summary>
        public void ExportToXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement browser = doc.CreateElement("KwsBrowser");
            browser.SetAttribute("version", "1");

            parent.AppendChild(browser);

            ArrayList nodes = new ArrayList();
            RootNode.GetSubtreeNodes(nodes);

            foreach (KwsBrowserNode node in nodes)
            {
                KwsBrowserFolderNode folder = node as KwsBrowserFolderNode;

                // Ignore workspace nodes.
                if (folder == null)
                    continue;
                
                XmlElement elem = doc.CreateElement("Folder");
                elem.SetAttribute("name", folder.FullPath);
                browser.AppendChild(elem);
            }
        }

        /// <summary>
        /// Convert the Xml representation of a KwsBrowser object to a
        /// flat list of all the full paths present.
        /// </summary>
        public static List<String> FromXml(XmlElement elem)
        {
            UInt32 version = UInt32.Parse(elem.GetAttribute("version"));

            List<String> folders = new List<String>();
            if (version == 1)
            {
                foreach (XmlNode node in elem.ChildNodes)
                {
                    XmlElement fElem = node as XmlElement;
                    if (fElem == null) throw new Exception("expected folder node");
                    folders.Add(fElem.GetAttribute("name"));
                }
            }
            else
            {
                throw new Exception("Unsupported KwsBrowser version ('" + version + "').");
            }
            return folders;
        }

        /// <summary>
        /// Return the serialized content of the browser.
        /// </summary>
        public SerializedKwsBrowser Serialize()
        {
            List<KwsBrowserFolderNode> folderList = new List<KwsBrowserFolderNode>();
            List<KwsBrowserKwsNode> kwsList = new List<KwsBrowserKwsNode>();
            RecursiveList(true, RootNode, folderList, kwsList);
            SerializedKwsBrowser skb = new SerializedKwsBrowser();

            foreach (KwsBrowserFolderNode fNode in folderList)
            {
                if (fNode.IsRoot()) continue;
                skb.FolderList.Add(new SerializedKwsBrowserFolder(fNode));
            }

            foreach (KwsBrowserKwsNode kNode in kwsList)
                skb.KwsList.Add(new SerializedKwsBrowserKws(kNode));

            skb.SelectedKwsID = (SelectedKws == null) ? 0 : SelectedKws.InternalID;

            return skb;
        }

        /// <summary>
        /// Deserialize the content of the browser, reorder kwsList 
        /// appropriately and populate kwsFolderList with the folder of each 
        /// workspace.
        /// </summary>
        public void Deserialize(SerializedKwsBrowser skb, List<Workspace> kwsList, out List<KwsBrowserFolderNode> kwsFolderList, out List<bool> notifyFlagsList)
        {
            // Recreate the browser folders, in order.
            foreach (SerializedKwsBrowserFolder skbf in skb.FolderList)
                CreateFolderFromPath(skbf.Path).ExpandedFlag = skbf.ExpandedFlag;

            // Create a mapping between internal IDs and non-referenced workspaces.
            SortedDictionary<UInt64, Workspace> unrefTree = new SortedDictionary<UInt64, Workspace>();
            foreach (Workspace kws in kwsList) unrefTree[kws.InternalID] = kws;

            // Populate kwsList and kwsFolderList.
            kwsList.Clear();
            kwsFolderList = new List<KwsBrowserFolderNode>();
            notifyFlagsList = new List<bool>();

            // Process the workspaces found in the browser, in order.
            foreach (SerializedKwsBrowserKws skbk in skb.KwsList)
            {
                if (unrefTree.ContainsKey(skbk.ID))
                {
                    kwsList.Add(unrefTree[skbk.ID]);
                    kwsFolderList.Add(CreateFolderFromPath(skbk.ParentPath));
                    notifyFlagsList.Add(skbk.NotifyFlag);
                    unrefTree.Remove(skbk.ID);
                }
            }
            
            // Process the unreferenced workspaces.
            foreach (Workspace kws in unrefTree.Values)
            {
                kwsList.Add(kws);
                kwsFolderList.Add(PrimaryFolder);
                notifyFlagsList.Add(false);
            }
        }
    }

    /// <summary>
    /// Represent a node in the workspace browser.
    /// </summary>
    public abstract class KwsBrowserNode
    {
        /// <summary>
        /// Reference to the parent folder, if any.
        /// </summary>
        public KwsBrowserFolderNode Parent;

        /// <summary>
        /// Flag to remember if this node should be displayed
        /// in a special way in the UI. This is set to true when
        /// an application wants to notify the user of some event.
        /// </summary>
        public bool NotifyFlag = false;

        /// <summary>
        /// Identifier of the node. The identifier is unique per folder.
        /// </summary>
        public abstract String ID
        {
            get;
        }

        /// <summary>
        /// Name of the node. This is the ID for a workspace and the folder
        /// name for a folder.
        /// </summary>
        public abstract String Name
        {
            get;
        }

        /// <summary>
        /// Index of the node in the parent folder.
        /// </summary>
        public int Index
        {
            get
            {
                Debug.Assert(Parent != null);
                int index = Parent.GetContainedNodeIndex(this);
                Debug.Assert(index != -1);
                return index;
            }
        }

        /// <summary>
        /// Full path to the node from the root, separated by '/'.
        /// </summary>
        public String FullPath
        {
            get
            {
                if (IsRoot()) return "/";

                String path = ID;
                KwsBrowserNode cur = Parent;
                while (!cur.IsRoot())
                {
                    path = cur.ID + "/" + path;
                    cur = cur.Parent;
                }

                return "/" + path;
            }
        }

        /// <summary>
        /// Return true if the node is the root.
        /// </summary>
        public bool IsRoot()
        {
            return (Parent == null);
        }

        /// <summary>
        /// Return true if this node is a descendant of the node specified.
        /// </summary>
        public bool IsDescendantOf(KwsBrowserNode folder)
        {
            if (Parent == null) return false;
            if (Parent == folder) return true;
            return Parent.IsDescendantOf(folder);
        }
    }

    /// <summary>
    /// Represent a workspace in the workspace browser.
    /// </summary>
    public class KwsBrowserKwsNode : KwsBrowserNode
    {
        public Workspace Kws;

        /// <summary>
        /// Reference to the selected user, if any.
        /// </summary>
        public KwsUser SelectedUser = null;

        public override string ID
        {
            get { return Kws.InternalID.ToString(); }
        }

        public override string Name
        {
            get { return ID; }
        }

        public KwsBrowserKwsNode(Workspace kws)
        {
            Kws = kws;
        }

        /// <summary>
        /// Return the color of the icon the workspace should have in the
        /// browser.
        /// </summary>
        public Color GetKwsIconImageKey()
        {
            KwsTask curTask = Kws.Sm.GetCurrentTask();

            if (curTask == KwsTask.Spawn ||
                curTask == KwsTask.Delete ||
                curTask == KwsTask.Rebuild)
            {
                return Color.Gray;
            }

            else if (curTask == KwsTask.Stop)
            {
                return Color.Red;
            }

            else if (curTask == KwsTask.WorkOffline)
            {
                return Color.Blue;
            }

            else if (curTask == KwsTask.WorkOnline)
            {
                if (Kws.IsOnlineCapable())
                {
                    return Color.Green;
                }

                // We're not currently connecting but we can request to.
                else if (Kws.Sm.CanWorkOnline())
                {
                    return Color.Blue;
                }

                // We're connecting, allow disconnection.
                else
                {
                    return Color.Gray;
                }
            }

            return Color.Red;
        }
    }

    /// <summary>
    /// Represent a folder in the workspace browser.
    /// </summary>
    public class KwsBrowserFolderNode : KwsBrowserNode
    {
        public String FolderName;
        public ArrayList FolderNodes = new ArrayList();
        public ArrayList KwsNodes = new ArrayList();
        public bool ExpandedFlag = true;
        public bool PermanentFlag = false;

        public override string ID
        {
            get { return "f_" + FolderName; }
        }

        public override string Name
        {
            get { return FolderName; }
        }

        public KwsBrowserFolderNode(String folderName)
        {
            FolderName = folderName;
        }

        /// <summary>
        /// Return true if the name specified is a valid folder name.
        /// </summary>
        public static bool IsValidFolderName(String name)
        {
            return Base.IsValidKwsName(name);
        }

        /// <summary>
        /// Return the list of folder nodes and workspace nodes in the
        /// folder, in this order.
        /// </summary>
        public ArrayList GetNodes()
        {
            ArrayList nodes = new ArrayList();
            foreach (KwsBrowserNode node in FolderNodes) nodes.Add(node);
            foreach (KwsBrowserNode node in KwsNodes) nodes.Add(node);
            return nodes;
        }

        /// <summary>
        /// Add all the nodes included in the subtree of this node (recursively),
        /// excluding this node.
        /// </summary>
        public void GetSubtreeNodes(ArrayList nodes)
        {
            foreach (KwsBrowserFolderNode node in FolderNodes)
            {
                nodes.Add(node);
                node.GetSubtreeNodes(nodes);
            }

            foreach (KwsBrowserNode node in KwsNodes) nodes.Add(node);
        }

        /// <summary>
        /// Return the list that could store the node specified.
        /// </summary>
        public ArrayList GetListForNode(KwsBrowserNode node)
        {
            if (node is KwsBrowserFolderNode) return FolderNodes;
            return KwsNodes;
        }

        /// <summary>
        /// Return the index of the node specified, or -1 if the node
        /// isn't in the folder.
        /// </summary>
        public int GetContainedNodeIndex(KwsBrowserNode node)
        {
            ArrayList nodes = GetListForNode(node);
            for (int i = 0; i < nodes.Count; i++) if (nodes[i] == node) return i;
            return -1;
        }

        /// <summary>
        /// Return the node having the ID specified, if any.
        /// </summary>
        public KwsBrowserNode GetNodeByID(String ID)
        {
            // We don't distinguish names by case since Windows does not
            // distinguish by case in the tree view.
            foreach (KwsBrowserNode node in GetNodes()) if (node.ID.ToLower() == ID.ToLower()) return node;
            return null;
        }

        /// <summary>
        /// Return the folder node having the index specified, if any.
        /// </summary>
        public KwsBrowserFolderNode GetFolderNodeByIndex(Int32 index)
        {
            if (index < 0 || index >= FolderNodes.Count) return null;
            return FolderNodes[index] as KwsBrowserFolderNode;
        }

        /// <summary>
        /// Return the workspace node having the index specified, if any.
        /// </summary>
        public KwsBrowserKwsNode GetKwsNodeByIndex(Int32 index)
        {
            if (index < 0 || index >= KwsNodes.Count) return null;
            return KwsNodes[index] as KwsBrowserKwsNode;
        }

        /// <summary>
        /// Return true if the index specified is valid for an insert of the 
        /// node specified.
        /// </summary>
        public bool IsIndexValidForInsert(KwsBrowserNode node, int index)
        {
            return (index >= 0 && index <= GetListForNode(node).Count);
        }

        /// <summary>
        /// Add the node specified at the position specified.
        /// </summary>
        public void AddNode(KwsBrowserNode node, int index)
        {
            Debug.Assert(GetContainedNodeIndex(node) == -1);
            Debug.Assert(IsIndexValidForInsert(node, index));
            GetListForNode(node).Insert(index, node);
            node.Parent = this;
        }

        /// <summary>
        /// Remove the node specified.
        /// </summary>
        public void RemoveNode(KwsBrowserNode node)
        {
            Debug.Assert(GetContainedNodeIndex(node) != -1);
            GetListForNode(node).Remove(node);
            node.Parent = null;
        }
    }

    /// <summary>
    /// This class is used to serialize and deserialize the workspace browser.
    /// We do not serialize the browser directly since we cannot store the
    /// references to the workspaces. Besides, this structure is easier to deal
    /// with for compatibility purposes.
    /// </summary>
    [Serializable]
    public class SerializedKwsBrowser
    {
        /// <summary>
        /// List of serialized folders, in order.
        /// </summary>
        public List<SerializedKwsBrowserFolder> FolderList = new List<SerializedKwsBrowserFolder>();

        /// <summary>
        /// List of serialized workspaces, in order.
        /// </summary>
        public List<SerializedKwsBrowserKws> KwsList = new List<SerializedKwsBrowserKws>();

        // ID of the selected workspace, if any.
        public UInt64 SelectedKwsID = 0;
    }

    /// <summary>
    /// Helper class for SerializedKwsBrowser.
    /// </summary>
    [Serializable]
    public class SerializedKwsBrowserFolder
    {
        /// <summary>
        /// Path to the folder.
        /// </summary>
        public String Path;

        /// <summary>
        /// True if the folder is expanded.
        /// </summary>
        public bool ExpandedFlag;

        public SerializedKwsBrowserFolder() { }

        public SerializedKwsBrowserFolder(KwsBrowserFolderNode node)
        {
            Path = node.FullPath;
            ExpandedFlag = node.ExpandedFlag;
        }
    }

    /// <summary>
    /// Helper class for SerializedKwsBrowser.
    /// </summary>
    [Serializable]
    public class SerializedKwsBrowserKws
    {
        /// <summary>
        /// Internal ID of the workspace.
        /// </summary>
        public UInt64 ID;

        /// <summary>
        /// Path to the parent folder.
        /// </summary>
        public String ParentPath;

        /// <summary>
        /// True if this node should appear specially in the UI.
        /// </summary>
        public bool NotifyFlag;

        public SerializedKwsBrowserKws() { }

        public SerializedKwsBrowserKws(KwsBrowserKwsNode node)
        {
            ID = node.Kws.InternalID;
            ParentPath = node.Parent.FullPath;
            NotifyFlag = node.NotifyFlag;
        }
    }

    /// <summary>
    /// This request is posted to refresh the UI.
    /// </summary>
    public class WmUiRefreshGer : GuiExecRequest
    {
        /// <summary>
        /// Reference to the UI broker.
        /// </summary>
        private WmUiBroker m_broker;

        public WmUiRefreshGer(WmUiBroker broker)
        {
            m_broker = broker;
        }

        public override void Run()
        {
            m_broker.OnUiRefreshGer(this);
        }
    }
}
