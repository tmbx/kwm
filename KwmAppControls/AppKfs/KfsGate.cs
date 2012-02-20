using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using kwm.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Collections.Specialized;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// Codes describing the buttons presented to the user when the user
    /// tries to perform an operation that requires a choice to be made:
    /// "s": Skip / Skip all.
    /// "w": Overwrite / Overwrite all.
    /// "c": Cancel (shown as "OK" when alone).
    /// "k": OK button.
    /// 
    /// Codes describing the action chosen by the user:
    /// "s": Skip the conflicting objets.
    /// "w": Overwrite the conflicting objects.
    /// 
    /// Codes describing the default action to undertake when a choice is
    /// needed from the user:
    /// "a": Ask the user to choose the appropriate action.
    /// "s": Skip the conflicting objets.
    /// "w": Overwrite the conflicting objects.

    /// <summary>
    /// Allow fine-tuning of the default actions taken by the KFS
    /// gate in response of a conflict.  The default behavior of a new
    /// instance is to ask the user each time.
    /// </summary>
    public class GateConfirmActions
    {
        /// <summary>
        /// Default action in case a file already exists.
        /// </summary>
        public String fileExistsAction = "a";

        /// <summary>
        /// Default action in case a directory already exists.
        /// </summary>
        public String dirExistsAction = "a";

        /// <summary>
        /// Default action in case a file is being transferred.
        /// </summary>
        public String inTransferAction = "a";

        /// <summary>
        /// Default action in case of a type conflict.
        /// </summary>
        public String typeConflictAction = "a";

        public String otherUploadAction = "a";
    }

    /// <summary>
    /// Represent the type of a path on the filesystem or in the share.
    /// </summary>
    public enum GatePathType
    {
        /// <summary>
        /// The path does not exist.
        /// </summary>
        None,

        /// <summary>
        /// The path is a file.
        /// </summary>
        File,

        /// <summary>
        /// The path is a directory.
        /// </summary>
        Dir,

        /// <summary>
        /// The path information is stale.
        /// </summary>
        Stale
    }

    /// <summary>
    /// Base exception class used by the gate.
    /// </summary>
    public class GateException : Exception
    {
        public GateException() { }
        public GateException(String message) : base(message) { }
    }

    /// <summary>
    /// This exception is thrown when the user cancelled an operation.
    /// </summary>
    public class GateCancelledException : GateException { }

    /// <summary>
    /// This exception is thrown when the view of the share is invalid.
    /// </summary>
    public class GateSyncException : GateException { }

    /// <summary>
    /// This exception is thrown when the user tried to perform an illegal
    /// operation. 'Message' contains the message to display to the user.
    /// </summary>
    public class GateIllegalException : GateException
    {
        public GateIllegalException(String message) : base(message) { }
    }

    /// <summary>
    /// This exception is thrown when a file involved in an operation is
    /// already being transferred. 'Message' contains the message to
    /// display to the user.
    /// </summary>
    public class GateInTransferException : GateException
    {
        public GateInTransferException(String message) : base(message) { }
    }

    /// <summary>
    /// This class represents a gate to the KFS functionalities which are
    /// exposed to the UI. All calls incoming from the UI should be made on
    /// this object.
    /// </summary>
    public class KfsGate
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        public KfsShare Share;

        /// <summary>     
        /// Number of times the gate has been entered by the UI. Normally, when
        /// we enter the gate, the pipeline should be stalled, since it is
        /// either actively executing or it has re-entered the UI by displaying
        /// a modal dialog which prevents the access to the gate (or allows it
        /// under very controlled conditions). When this field is higher than
        /// 0, the pipeline won't run even if it is requested to.
        /// </summary>
        public int EntryCount = 0;

        public KfsGate(KfsShare s)
        {
            Share = s;
            Share.Gate = this;
        }

        /// <summary>
        /// Called when the gate is entered by the UI.
        /// </summary>
        public void GateEntry(string _reason)
        {
            Logging.Log("GateEntry (" + (EntryCount + 1) + ") : " + _reason);

            // Increment the entry count.
            EntryCount++;

            // Our status view may be stale. If so, refresh it so we don't
            // undertake silly actions.
            Share.UpdateStatusViewIfNeeded();
        }

        /// <summary>
        /// Called when the gate has been exited by the UI. 
        /// </summary>
        public void GateExit(string _reason)
        {
            Logging.Log("GateExit (" + (EntryCount - 1) + ") : " + _reason);

            if (EntryCount <= 0)
            {
                Logging.LogException(new Exception("Count already at 0."));
                EntryCount = 0;
                return;
            }
            // Decrement the entry count.
            
            EntryCount--;

            // Run the pipeline as needed and force it to synchronize itself.
            if (EntryCount == 0)
            {
                Share.Pipeline.RunQueued("Gate Exit", true);
            }
        }

        /// <summary>
        /// Add external files to the share.
        /// </summary>
        /// <param name="destRelPath">Relative path to the destination directory</param>
        /// <param name="toAdd">Explicit list of the items to add. Pass null to prompt the user for files or a directory.</param>
        /// <param name="addDir">True to prompt for a directory instead of files.</param>
        public void AddExternalFiles(String destRelPath, string[] toAdd, bool addDir, GateConfirmActions defaultActions)
        {
            GateEntry("AddExternalFiles");

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("AddExternalFiles() called", AllowedOpStatus.None);

            try
            {
                // Get the sets of directories and files to add.
                SortedSet<String> fileSet = new SortedSet<String>();
                SortedSet<String> dirSet = new SortedSet<String>();
                List<String> copySrc = new List<string>();
                List<String> copyDst = new List<string>();

                // Make sure the destination still exists.
                if (GetInternalPathType(destRelPath) != GatePathType.Dir) 
                    throw new GateSyncException();                

                String[] srcArray = null;
                if (toAdd == null)
                {
                    if (addDir)
                    {
                        // Prompt the user for the directories to add.
                        srcArray = ShowSelectFolderDialog(Share.MakeAbsolute(destRelPath));
                    }
                    else
                    {
                        // Prompt the user for the files to add.
                        srcArray = ShowSelectMultiFileDialog(Share.MakeAbsolute(destRelPath));
                    }
                }

                else
                {
                    srcArray = toAdd;
                }

                // If someting in srcArray is a parent folder of destRelPath, deny the Add.
                foreach (string p in srcArray)
                {
                    if (Directory.Exists(p) &&
                        KfsPath.GetWindowsFilePath(Share.MakeAbsolute(destRelPath), true).StartsWith(p))
                    {
                        throw new GateIllegalException("Cannot add " + Path.GetFileName(p) + ": The destination folder is a subfolder of the source folder.");
                    }
                }

                // Add the missing local directories.
                Share.AddMissingLocalDirectories(destRelPath);

                // Add the missing server directories.
                dirSet.Add(destRelPath);

                // Handle the sources selected.
                AddExternalFilesRecursive(KfsPath.AddTrailingSlash(destRelPath),
                                          srcArray, copySrc, copyDst, fileSet, dirSet, true,
                                          ref defaultActions);

                if (!Misc.CopyFiles(copySrc, copyDst, true, true, false, false, false, true))
                    return;

                // Add the files and directories.
                AddFilesCommon(fileSet, dirSet);
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("AddExternalFiles");
        }

        /// <summary>
        /// Add the "Not Added" files and directories to the share.
        /// </summary>
        /// <param name="pathArray">List of relative paths that must be added (files or directories)</param>
        public void AddInternalFiles(List<String> pathArray)
        {
            GateEntry("AddInternalFiles");

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("AddInternalFiles() called", AllowedOpStatus.None);

            try
            {
                String inTransferAction = "a";

                // Get the sets of directories and files to add.
                SortedSet<String> fileSet = new SortedSet<String>();
                SortedSet<String> dirSet = new SortedSet<String>();

                foreach (String path in pathArray)
                {
                    foreach (KfsStatusPath sp in Share.StatusView.GetPathArray(path, false))
                    {
                        // We found a file to add.
                        if (sp.Status == PathStatus.NotAdded)
                        {
                            // The file doesn't exist anymore, abort.
                            if (GetInternalPathType(sp.Path) != GatePathType.File)
                                throw new GateSyncException();

                            // The file is being transferred. Skip it.
                            if (Share.IsTransferringFile(sp.Path))
                            {
                                String prompt = "The file " + sp.Path + " is already being transferred.";
                                ShowChoiceDialog(prompt, "sc", ref inTransferAction);
                                continue;
                            }

                            // Add the file.
                            fileSet.Add(sp.Path);

                            // Add the parent directory in the set of directories to add,
                            // in case the parent directory doesn't exist remotely.
                            dirSet.Add(sp.Parent.Path);
                        }

                        // We found a directory to add.
                        else
                        {
                            // The directory was replaced by a file.
                            if (GetInternalPathType(sp.Path) == GatePathType.File)
                                throw new GateSyncException();

                            // Add the directory.
                            dirSet.Add(sp.Path);
                        }
                    }
                }

                // Add the files and directories.
                AddFilesCommon(fileSet, dirSet);
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("AddInternalFiles");
        }

        /// <summary>
        /// Revert the given paths to their current server version.
        /// </summary>
        public void RevertChanges(List<String> pathArray)
        {
            GateEntry("RevertChanges");

            List<String> toSync = new List<string>();

            foreach(String path in pathArray)
            {
                // Delete the file directly.
                File.Delete(Share.MakeAbsolute(path));

                // We know the file is no longer present on the disk.
                // A partial scan will confirm this later.
                KfsLocalObject lo = Share.LocalView.GetObjectByPath(path) as KfsLocalObject;
                lo.RemoveFromView();

                toSync.Add(path);
            }

            // Update the status view. Since we have removed
            // local files, the status of the files deleted will
            // switch to Not Downloaded.
            Share.RequestStatusViewUpdate("Reverting changes.");
            Share.UpdateStatusViewIfNeeded();
            
            // Request the download of the deleted files.
            if (toSync.Count > 0)
                SynchronizePath(toSync, false);

            GateExit("RevertChanges");
        }

        /// <summary>
        /// Add the given paths to the system's clipboard.
        /// </summary>
        public void Copy(List<String> pathArray)
        {
            GateEntry("Copy");

            StringCollection FileDropList = new StringCollection();

            foreach (String relPath in pathArray)
            {
                // We only add files that are locally present.
                if (CanCopy(relPath))
                {
                    FileDropList.Add(Share.MakeAbsolute(relPath));
                }
            }

            if (FileDropList.Count > 0)
                Clipboard.SetFileDropList(FileDropList);

            GateExit("Copy");
        }

        /// <summary>
        /// Copy the files and folders referenced in the Windows clipboard 
        /// FileDrop data to our local store. Do not add them to the Share.
        /// </summary>
        public void Paste(String destRelPath)
        {
            GateEntry("Paste");

            Debug.Assert(Clipboard.ContainsFileDropList());

            String destFullPath = KfsPath.AddTrailingBackslash(Share.MakeAbsolute(destRelPath));
            
            // Get the Clipboard content.
            StringCollection ToPaste = Clipboard.GetFileDropList();

            // Make sure we have something to paste.
            if (ToPaste.Count < 1)
                return;

            // If the Clipboard content comes from the same location than
            // destFullPath, automatically rename the target items. There is
            // no way (except by programmation) for multiple items to come from 
            // a different location. Just compare the first item.
            bool autoRename = (KfsPath.DirName(ToPaste[0]).ToLower() == destFullPath.ToLower());

            List<String> Source = new List<String>();
            List<String> Dest = new List<String>();

            foreach (String path in ToPaste)
            {
                Source.Add(path);
                Dest.Add(destFullPath + KfsPath.BaseName(path));
            }

            Misc.CopyFiles(Source, Dest, true, false, autoRename, false, false, true);

            GateExit("Paste");
        }
        /// <summary>
        /// Create a new document (from the ShellNew context menu) on the filesystem. Add the document
        /// to the share if AddToShare is set to true.
        /// </summary>
        /// <param name="relPath">Relative path in which the file must be added, not slash terminated.</param>
        /// <param name="filename">Filename of the new document.</param>
        public void AddNewDocument(String relPath, String filename, NewDocument doc, bool AddToShare)
        {
            GateEntry("AddNewDocument");

            try
            {
                string fullPath = Share.MakeAbsolute(relPath);

                // If adding the file to the share, create it in
                // a temporary location and call AddExternal on the files.
                if (AddToShare)
                {

                    String tempDir = Path.GetTempFileName();
                    File.Delete(tempDir);
                    Directory.CreateDirectory(tempDir);

                    doc.DoVerbAction(KfsPath.AddTrailingSlash(tempDir), filename);

                    List<String> newFile = new List<string>();
                    newFile.Add(KfsPath.AddTrailingSlash(tempDir) + filename);
                    AddExternalFiles(relPath, newFile.ToArray(), false, new GateConfirmActions());
                }
                // Otherwise, create the file where it was asked.
                else
                {
                    doc.DoVerbAction(KfsPath.AddTrailingSlash(fullPath), filename);
                }
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("AddDirectory");
        }

        /// <summary>
        /// Add the directory having the relative path specified.
        /// </summary>
        public void AddDirectory(String relPath)
        {
            GateEntry("AddDirectory");

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("AddDirectory() called", AllowedOpStatus.None);

            try
            {
                // Either the target path must exist as a directory, or
                // the target path must not exist and its parent must exist
                // as a directory.
                KfsPhase1Payload payload = new KfsPhase1Payload();
                KfsStatusPath ts = Share.StatusView.GetPath(relPath);

                // The target path exists.
                if (ts != null && ts.Status != PathStatus.ServerGhost)
                {
                    // It's not a directory.
                    if (ts.Status != PathStatus.Directory) throw new GateSyncException();
                }

                // The target path does not exist. Check the parent.
                else
                {
                    KfsStatusPath ps = Share.StatusView.GetPath(KfsPath.DirName(relPath));

                    // The parent is not a directory.
                    if (ps == null || ps.Status != PathStatus.Directory) throw new GateSyncException();
                }

                // Create the missing directories.
                Share.AddMissingLocalDirectories(relPath);
                AddMissingServerDirectories(relPath, payload);

                // Request the creation on the server.
                Debug.Assert(Share.MetaDataManager.Status == MetaDataManagerStatus.Idle);
                if (payload.OpList.Count > 0)
                    Share.MetaDataManager.QueueOperation(payload, null, MetaDataTask.Mkdir);
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("AddDirectory");
        }

        /// <summary>
        /// Synchronize one or more files that are in a Modified Stale status.
        /// </summary>
        /// <param name="pathArray">List of paths to resolve</param>
        /// <param name="overwriteLocalChanges">Overwrite local or remote</param>
        public void ResolveFileConflict(List<string> pathArray, bool overwriteLocalChanges)
        {
            GateEntry("ResolveFileConflict");

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("ResolveFileConflict() called", AllowedOpStatus.None);

            try
            {
                foreach (string s in pathArray)
                {
                    KfsStatusPath sp = Share.StatusView.GetPath(s);
                    Debug.Assert(sp.Status == PathStatus.ModifiedStale);
                    
                    KfsServerFile f = sp.ServerObject as KfsServerFile;
                    Debug.Assert(f != null);
                    Debug.Assert(!Share.IsTransferringFile(sp.Path));

                    if (overwriteLocalChanges)
                    {
                        File.Delete(Share.MakeAbsolute(f.RelativePath));
                        Share.DownloadManager.QueueDownload(f.CurrentVersion, false);
                    }
                    else
                    {
                        Share.UploadManager.QueueUpload(sp.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            
            GateExit("ResolveFileConflict");
        }

        /// <summary>
        /// Attempt to resolve all existing type conflicts.
        /// </summary>
        public void ResolveTypeConflicts()
        {
            Debug.Assert(Share.TypeConflictFlag);

            GateEntry("ResolveTypeConflicts");

            try
            {
                // Disallow all user operations until the pipeline stabilized.
                Share.DisallowUserOp("ResolveTypeConflicts() called", AllowedOpStatus.None);

                foreach (KfsStatusPath s in Share.StatusView.Root.ChildTree.Values)
                {
                    if (s.IsDir())
                        ResolveTypeConflictRecursive(s);
                    else if (s.IsTypeConflict())
                        DoResolveTypeConflict(s);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("ResolveTypeConflicts");
        }

        /// <summary>
        /// Helper method for ResolveTypeConflicts.
        /// </summary>
        /// <param name="sp"></param>
        /// <returns>True to cancel the operation.</returns>
        private void ResolveTypeConflictRecursive(KfsStatusPath sp)
        {
            Debug.Assert(sp.IsDir());
            foreach (KfsStatusPath s in sp.ChildTree.Values)
            {
                if (s.IsDir())
                {
                    ResolveTypeConflictRecursive(s);
                }
                else if (s.IsTypeConflict())
                {
                    DoResolveTypeConflict(s);
                }
                // else: no problem, keep going.
            }
        }

        /// <summary>
        /// Interact with the user in order to resolve the type conflict in question.
        /// </summary>
        /// <param name="sp"></param>
        private void DoResolveTypeConflict(KfsStatusPath sp)
        {
            Debug.Assert(sp.IsTypeConflict());
            string startLocation = "";
            if (sp.Parent == null)
                startLocation = Share.MakeAbsolute("");
            else
                startLocation = Share.MakeAbsolute(sp.Parent.Path);

            bool delete = true;
            string newName = "";
            while (true)
            {
                FrmResolveTypeConflict frm = new FrmResolveTypeConflict(startLocation);
                string local = (sp.Status == PathStatus.DirFileConflict ? "directory" : "file");
                string remote = (sp.Status == PathStatus.DirFileConflict ? "file" : "directory");

                string msg = "Your local {0} \"{1}\" conflicts with a remote {2} that has the same name. You can either delete your local {0}, or rename it to something else.";

                frm.radioDelete.Checked = delete;
                frm.radioRename.Checked = !delete;
                frm.txtNewName.Text = newName;

                frm.lblText.Text = string.Format(msg, local, sp.Path, remote);
                frm.radioDelete.Text = "Delete \"" + sp.Name + "\"";
                frm.radioRename.Text = "Rename \"" + sp.Name + "\" to ...";
                Misc.OnUiEntry();
                DialogResult r = frm.ShowDialog();
                Misc.OnUiExit();

                if (r != DialogResult.OK)
                    throw new GateCancelledException();

                delete = frm.Delete;
                newName = frm.txtNewName.Text;

                string itmPath = Share.MakeAbsolute(sp.Path);

                if (File.Exists(itmPath) || Directory.Exists(itmPath))
                    File.SetAttributes(itmPath, FileAttributes.Normal);

                if (frm.Delete)
                {
                    try
                    {
                        // Perform local deletion.
                        if (File.Exists(itmPath))
                        {
                            File.Delete(itmPath);
                        }
                        else if (Directory.Exists(itmPath))
                        {
                            Directory.Delete(itmPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Misc.KwmTellUser("Unable to perform deletion : " + ex.Message);
                        continue;
                    }
                }
                else
                {
                    // Perform local rename.
                    try
                    {
                        File.Move(itmPath, KfsPath.DirName(itmPath) + frm.txtNewName.Text);
                    }
                    catch (Exception ex)
                    {
                        Misc.KwmTellUser("Unable to perform rename : " + ex.Message);
                        continue;
                    }
                }
                return;
            }
        }
        /// <summary>
        /// Synchronize the specified relative paths recursively.
        /// </summary>
        public void SynchronizePath(List<String> pathArray, bool openWhenDownloaded)
        {
            GateEntry("SynchronizePath");

            bool canUpload = Share.AllowedOp == AllowedOpStatus.All;
            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("SynchronizePath() called", AllowedOpStatus.None);

            try
            {
                foreach (String path in pathArray)
                {
                    KfsStatusPath sp = Share.StatusView.GetPath(path);
                    if (sp != null)
                    {
                        if (openWhenDownloaded)
                            Debug.Assert(sp.Status == PathStatus.NotDownloaded ||
                                         sp.Status == PathStatus.UnmodifiedStale);

                        SynchronizePathRecursive(sp, openWhenDownloaded, canUpload);
                    }
                }
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("SynchronizePath");
        }

        /// <summary>
        /// Delete the selected relative paths recursively.
        /// </summary>
        public void DeletePath(List<String> pathArray)
        {
            GateEntry("DeletePath");

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("DeletePath() called", AllowedOpStatus.None);

            try
            {
                // Check for transfers.
                ThrowIfContainFileTransfer(pathArray);

                // Get the entries to delete, both locally and remotely.
                HashedSet<String> seenSet = new HashedSet<String>();
                List<String> fileArray = new List<String>();
                List<String> dirArray = new List<String>();
                KfsPhase1Payload payload = new KfsPhase1Payload();

                foreach (String path in pathArray)
                {
                    // Get the paths in leaf-first order, so that we delete entries in
                    // directories before their parents.
                    foreach (KfsStatusPath sp in Share.StatusView.GetPathArray(path, true))
                    {
                        // Skip already processed paths.
                        if (seenSet.Contains(sp.Path)) continue;

                        // Remember that we have processed that path.
                        seenSet.Add(sp.Path);

                        // Sanity check.
                        if (sp.IsUndetermined() || sp.IsTypeConflict()) throw new GateSyncException();

                        // Handle remote deletion.
                        if (sp.HasServerFile())
                            payload.AddDeleteOp(true, sp.ServerObject.Inode, sp.ServerObject.CommitID);

                        else if (sp.HasServerDir())
                            payload.AddDeleteOp(false, sp.ServerObject.Inode, sp.ServerObject.CommitID);

                        // Handle local deletion.
                        if (sp.HasLocalFile()) fileArray.Add(sp.Path);
                        else if (sp.HasLocalDir()) dirArray.Add(sp.Path);
                    }
                }

                // Delete the files and directories locally.
                foreach (String path in fileArray)
                {
                    try
                    {
                        string absPath = Share.MakeAbsolute(path);
                        File.SetAttributes(absPath, FileAttributes.Normal);
                        File.Delete(absPath);
                    }
                    // Silently ignore NotFoundExceptions since we WANT
                    // to have this path removed from the disk.
                    catch (FileNotFoundException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }

                }
                foreach (String path in dirArray)
                {
                    try
                    {
                        string absPath = Share.MakeAbsolute(path);
                        File.SetAttributes(absPath, FileAttributes.Normal);
                        Directory.Delete(Share.MakeAbsolute(path));
                    }
                    // Silently ignore NotFoundExceptions since we WANT
                    // to have this path removed from the disk.
                    catch (FileNotFoundException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                }

                // Request the deletion on the server.
                Debug.Assert(Share.MetaDataManager.Status == MetaDataManagerStatus.Idle);
                if (payload.OpList.Count > 0)
                    Share.MetaDataManager.QueueOperation(payload, null, MetaDataTask.Delete);
            }

            catch (IOException ex)
            {
                Misc.KwmTellUser("Unable to delete the selected file or folder : " +
                                  Environment.NewLine + Environment.NewLine + ex.Message,
                                  "Error Deleting File or Folder",
                                  MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("DeletePath");
        }

        /// <summary>
        /// Move the selected paths to the destination specified. The selected
        /// paths must all be rooted in the same directory. dstRelPath must 
        /// not end with a slash.
        /// </summary>
        public void MovePath(List<String> pathArray, String dstRelPath)
        {
            // Get out early if there is nothing to move.
            if (pathArray.Count == 0) return;

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("MovePath() called", AllowedOpStatus.None);

            GateEntry("MovePath");

            try
            {
                // Make sure the destination still exists.
                if (GetInternalPathType(dstRelPath) != GatePathType.Dir) throw new GateSyncException();

                // Check for transfers.
                List<String> transferPathArray = new List<String>(pathArray);
                transferPathArray.Add(dstRelPath);
                ThrowIfContainFileTransfer(pathArray);

                // Validate the move and extract the source names.
                String fromLoc = KfsPath.DirName(pathArray[0]);
                List<String> srcArray = new List<String>();

                foreach (String path in pathArray)
                {
                    Debug.Assert(KfsPath.DirName(path) == fromLoc);

                    if (dstRelPath.StartsWith(path))
                        throw new GateIllegalException("Sorry, cannot move directory into subdirectory " +
                                                       "of itself.");
                    srcArray.Add(KfsPath.BaseName(path));
                }

                // Handle the sources selected.
                String toLoc = dstRelPath;
                if (toLoc != "") toLoc += "/";

                KfsPhase1Payload payload = new KfsPhase1Payload();
                String typeConflictAction = "a";
                MovePathRecursive(fromLoc, toLoc, srcArray, payload, true, ref typeConflictAction);

                // Request the move on the server.
                Debug.Assert(Share.MetaDataManager.Status == MetaDataManagerStatus.Idle, "Share.MetaDataManager.Status = " + Share.MetaDataManager.Status.ToString());
                if (payload.OpList.Count > 0)
                    Share.MetaDataManager.QueueOperation(payload, null, MetaDataTask.Move);
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("MovePath");
        }

        /// <summary>
        /// Move a given relative path to a new name, in the same directory.
        /// </summary>
        public void RenamePath(string srcPath, string newName)
        {
            Debug.Assert(KfsPath.IsValidFileName(newName));

            // Disallow all user operations until the pipeline stabilized.
            Share.DisallowUserOp("RenamePath() called", AllowedOpStatus.None);
            GateEntry("RenamePath");

            try
            {
                // Check that the new name has no invalid characters.
                if (!KfsPath.IsValidFileName(newName))
                    throw new GateIllegalException("Invalid name (" + newName + "). Note that files and folders cannot contain any of the following characters:\n*, \\, /, ?, :, |, <, >.");

                // Get the relative path to the source directory.
                String dirPath = KfsPath.DirName(srcPath);

                // Make sure the source exists.
                GatePathType srcType = GetInternalPathType(srcPath);
                if (srcType != GatePathType.Dir && srcType != GatePathType.File)
                    throw new GateSyncException();

                // Make sure the destination does not exist.
                String dstPath = dirPath + newName;
                GatePathType dstType = GetInternalPathType(dstPath);
                if (dstType != GatePathType.None)
                    throw new GateIllegalException("A file or folder with the name you specified " +
                                                   "already exists. Please specify a different name.");

                // Check for transfers.
                List<String> transferPathArray = new List<String>();
                transferPathArray.Add(srcPath);
                transferPathArray.Add(dstPath);
                ThrowIfContainFileTransfer(transferPathArray);

                // Get information about the source and the destination.
                bool fileFlag = (srcType == GatePathType.File);
                KfsServerObject srcObject = Share.ServerView.GetObjectByPath(srcPath);

                // The source exists only locally.
                if (srcObject == null)
                {
                    // Move the file or directory locally.
                    if (fileFlag) File.Move(Share.MakeAbsolute(srcPath), Share.MakeAbsolute(dstPath));
                    else Directory.Move(Share.MakeAbsolute(srcPath), Share.MakeAbsolute(dstPath));
                }

                // Ask the server to move the file.
                else
                {
                    KfsPhase1Payload payload = new KfsPhase1Payload();
                    KfsServerObject dirObject = Share.ServerView.GetObjectByPath(dirPath);
                    payload.AddMoveOp(fileFlag,
                                      srcObject.Inode,
                                      srcObject.CommitID,
                                      dirObject.Inode,
                                      dirObject.CommitID,
                                      newName);

                    // Request the move on the server.
                    Debug.Assert(Share.MetaDataManager.Status == MetaDataManagerStatus.Idle);
                    Share.MetaDataManager.QueueOperation(payload, null, MetaDataTask.Move);
                }
            }

            catch (Exception ex)
            {
                HandleException(ex);
            }

            GateExit("RenamePath");
        }

        /// <summary>
        /// Prompt the user to choose one or several directories to add to the share. 
        /// An array containing the paths to the chosen directories is returned.
        /// At the moment, the dialog can only allow the selection of one directory at
        /// a time, but it would be cool to allow a multiselection.
        /// </summary>
        private string[] ShowSelectFolderDialog(String destFullPath)
        {
            List<string> retValue = new List<string>();
            FolderBrowserDialog dial = new FolderBrowserDialog();
            
            dial.ShowNewFolderButton = false;            
            dial.Description = "Select the folder to add in the workspace " + Share.App.Helper.GetKwsName();

            if (Misc.ApplicationSettings.KfsAddExternalFilesPath != "" &&
                Directory.Exists(Misc.ApplicationSettings.KfsAddExternalFilesPath))
                dial.SelectedPath = Misc.ApplicationSettings.KfsAddExternalFilesPath;
            else
                dial.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            Misc.OnUiEntry();
            DialogResult res = dial.ShowDialog();
            Misc.OnUiExit();

            if (res != DialogResult.OK)
                throw new GateCancelledException();

            Debug.Assert(dial.SelectedPath != "");

            // Remember where we added the files so we present the same path next time.
            Misc.ApplicationSettings.KfsAddExternalFilesPath = KfsPath.DirName(dial.SelectedPath);
            Misc.ApplicationSettings.Save();

            retValue.Add(dial.SelectedPath);
            return retValue.ToArray();
        }

        /// <summary>
        /// Prompt the user to choose one or several files. An array containing
        /// the paths to the chosen files is returned.
        /// </summary>
        private string[] ShowSelectMultiFileDialog(String destFullPath)
        {
            OpenFileDialog dial = new System.Windows.Forms.OpenFileDialog();
            if (Misc.ApplicationSettings.KfsAddExternalFilesPath != "" &&
                Directory.Exists(Misc.ApplicationSettings.KfsAddExternalFilesPath))
                dial.InitialDirectory = Misc.ApplicationSettings.KfsAddExternalFilesPath;
            else
                dial.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            dial.Multiselect = true;
            dial.SupportMultiDottedExtensions = true;

            dial.Title = "Select files to add in the workspace " + Share.App.Helper.GetKwsName();

            Misc.OnUiEntry();
            DialogResult res = dial.ShowDialog(Misc.MainForm);
            Misc.OnUiExit();

            if (res != DialogResult.OK)
                throw new GateCancelledException();

            Debug.Assert(dial.FileNames.Length > 0);

            // Remember where we added the files so we present the same path next time.
            Misc.ApplicationSettings.KfsAddExternalFilesPath = KfsPath.DirName(dial.FileNames[0]);
            Misc.ApplicationSettings.Save();

            return dial.FileNames;
        }

        /// <summary>
        /// Show a dialog asking the user to choose an action.
        /// </summary>
        /// <param name="prompt">Text describing the choice to make</param>
        /// <param name="choice">Codes describing the possible choices</param>
        /// <param name="defaultAction">Code describing the default action to take for the choice</param>
        /// <returns>Action taken by the user</returns>
        private String ShowChoiceDialog(String prompt, String choice, ref String defaultAction)
        {
            // Use the provided default action if available.
            if (defaultAction != "" && defaultAction != "a") return defaultAction;

            string caption = "Select action";

            if (choice.Contains("w") && choice.Contains("s") && choice.Contains("c"))
            {
                KMsgBoxResult res = KMsgBox.Show(prompt,
                                                 caption,
                                                 KMsgBoxButton.OverwriteSkipCancel);
                switch (res)
                {
                    case KMsgBoxResult.Skip:
                        return "s";
                    case KMsgBoxResult.SkipAll:
                        defaultAction = "s";
                        return "s";
                    case KMsgBoxResult.Overwrite:
                        return "w";
                    case KMsgBoxResult.OverwriteAll:
                        defaultAction = "w";
                        return "w";
                    case KMsgBoxResult.Cancel:
                        throw new GateCancelledException();
                    default:
                        Debug.Assert(false, res + " is not a valid KMsgBoxResult in this context (\"wsc\")");
                        break;
                }
            }

            else if (choice.Contains("s") && choice.Contains("c"))
            {
                KMsgBoxResult res = KMsgBox.Show(prompt,
                                                 caption,
                                                 KMsgBoxButton.SkipCancel);
                switch (res)
                {
                    case KMsgBoxResult.Skip:
                        return "s";
                    case KMsgBoxResult.SkipAll:
                        defaultAction = "s";
                        return "s";
                    case KMsgBoxResult.Cancel:
                        throw new GateCancelledException();
                    default:
                        Debug.Assert(false, res + " is not a valid KMsgBoxResult in this context (\"sc\")");
                        break;
                }
            }

            // Fatal error detected, tell the user and cancel the operation.
            else if (choice == "c")
            {
                KMsgBox.Show(prompt, caption, KMsgBoxButton.OK);
                throw new GateCancelledException();
            }

            // Trapped an exception, already cancelling the operation.
            else if (choice == "k")
            {
                KMsgBox.Show(prompt, caption, KMsgBoxButton.OK);
                return "k";
            }

            // Eeek.
            Debug.Assert(false, "invalid choice (" + choice + ")");
            return "";
        }

        /// <summary>
        /// Helper method for AddExternalFiles(). 'curLoc' must end with a
        /// delimiter when non-empty.
        /// </summary>
        private void AddExternalFilesRecursive(String curLoc,
                                               String[] srcArray,
                                               List<String> copySrc,
                                               List<String> copyDst,
                                               SortedSet<String> uploadFileSet,
                                               SortedSet<String> uploadDirSet,
                                               bool topLevelFlag,
                                               ref GateConfirmActions defaultActions)
        {
            // Pass every source.
            foreach (String srcFullPath in srcArray)
            {
                // Extract the source path information.
                String srcName = KfsPath.BaseName(srcFullPath);
                String destRelPath = curLoc + srcName;
                String destFullPath = Share.MakeAbsolute(destRelPath);

                // Get the local and destination path types.
                GatePathType srcType = GetExternalPathType(srcFullPath);
                GatePathType destType = GetInternalPathType(destRelPath);

                // The information is stale, abort.
                if (srcType == GatePathType.None) throw new GateSyncException();

                if (destType == GatePathType.Stale) throw new GateSyncException();

                // There is a type conflict. Skip the source.
                if (destType != GatePathType.None && srcType != destType)
                {
                    String prompt = "The file or directory " + destRelPath +
                                    " conflicts with a file or directory on the server.";
                    ShowChoiceDialog(prompt, "sc", ref defaultActions.typeConflictAction);
                    continue;
                }

                // The file is being transferred. Skip it.
                if (Share.IsTransferringFile(destRelPath))
                {
                    String prompt = "The file " + destRelPath + " is already being transferred.";
                    ShowChoiceDialog(prompt, "sc", ref defaultActions.inTransferAction);
                    continue;
                }

                // The source is a file.
                if (srcType == GatePathType.File)
                {
                    KfsServerFile f = Share.ServerView.GetObjectByPath(destRelPath) as KfsServerFile;

                    // The destination does not exist.
                    if (f == null || f.IsGhost())
                    {
                        // Void.
                    }

                    // Someone is already uploading the file. Skip the source or
                    // clobber the destination.
                    else if (!f.UploaderSet.IsEmpty)
                    {
                        String prompt = "The file " + destRelPath + " is being uploaded by another user.";
                        String res = ShowChoiceDialog(prompt, "swc", ref defaultActions.otherUploadAction);
                        if (res == "s") continue;
                    }

                    // The destination already exists. Skip the source or overwrite
                    // the destination.
                    else if (topLevelFlag && destType == GatePathType.File)
                    {
                        String prompt = "The file " + destRelPath + " already exists.";
                        String res = ShowChoiceDialog(prompt, "swc", ref defaultActions.fileExistsAction);
                        if (res == "s") continue;
                    }

                    // FIXME move this to another thread, eventually.
                    // LB: not sure this is a good idea: there are sync issues.
                    // ADJ: Using the Shell to do the copy seems good enough for now.
                    // Copy the file in the share.
                    if (File.Exists(destFullPath))
                        File.SetAttributes(destFullPath, FileAttributes.Normal);

                    copySrc.Add(srcFullPath);
                    copyDst.Add(destFullPath);

                    // Add the file to the file set.
                    uploadFileSet.Add(destRelPath);
                }

                // The source is a directory.
                else
                {
                    // The destination directory does not exist.
                    if (destType == GatePathType.None)
                    {
                        // Create the destination directory.
                        Directory.CreateDirectory(destFullPath);
                    }

                    // The directory already exists.
                    else if (topLevelFlag)
                    {
                        // Skip or overwrite the directory.
                        String prompt = "This folder already contains a folder named '" + destRelPath + "'." + Environment.NewLine +
                            "If the files in the existing folder have the same name as files in the folder you are adding, they will be replaced. Do you still want to add this folder?";
                        String res = ShowChoiceDialog(prompt, "swc", ref defaultActions.dirExistsAction);
                        if (res == "s") continue;
                    }

                    // Add the directory to the directory set.
                    uploadDirSet.Add(destRelPath);

                    // Add the files and directories contained in the source directory.
                    String[] subArray = Directory.GetFileSystemEntries(srcFullPath);
                    AddExternalFilesRecursive(destRelPath + "/", subArray, copySrc, copyDst, uploadFileSet, uploadDirSet, false, ref defaultActions);
                }
            }
        }

        /// <summary>
        /// Helper method for AddExternalFiles() and AddInternalFiles().
        /// </summary>
        /// <param name="fileSet">Set of files to upload</param>
        /// <param name="dirSet">Set of directories to create remotely</param>
        private void AddFilesCommon(SortedSet<String> fileSet, SortedSet<String> dirSet)
        {
            KfsPhase1Payload payload = new KfsPhase1Payload();
            SortedDictionary<UInt64, KfsFileUpload> uploadTree = new SortedDictionary<UInt64, KfsFileUpload>();

            // Add the missing server directories.
            foreach (String path in dirSet)
            {
                if (path == "" || KfsPath.IsValidFileName(KfsPath.BaseName(path)))
                    AddMissingServerDirectories(path, payload);
                else
                    throw new GateIllegalException("Invalid folder name (" + path + "). Note that folders cannot contain any of the following characters:\n*, \\, /, ?, :, |, <, >.");
            }

            // Queue the upload of the files.
            try
            {
                foreach (String path in fileSet)
                {
                    if (!KfsPath.IsValidFileName(KfsPath.BaseName(path)))
                        throw new GateIllegalException("Invalid file name (" + path + "). Note that files cannot contain any of the following characters:\n*, \\, /, ?, :, |, <, >.");
                    // Skip files that are unmodified.
                    KfsServerFile f = Share.ServerView.GetObjectByPath(path) as KfsServerFile;

                    if (f != null)
                    {
                        f.UpdateLocalStatus();
                        if (f.CurrentLocalStatus == LocalStatus.Unmodified) continue;
                    }

                    // Get rid of the ghost, if needed.
                    DeleteGhostIfNeeded(path, payload);
                    KfsFileUpload upload = Share.UploadManager.QueueUpload(path);

                    // File can't already be queued for upload,
                    // as the UI is frozen on a modal dialog and
                    // we already checked if any files we are 
                    // adding is being transfered in AddFiles*.
                    Debug.Assert(upload != null);

                    uploadTree[upload.OrderID] = upload;
                }
            }

            // Cancel the queued uploads.
            catch (Exception)
            {
                foreach (KfsFileUpload upload in uploadTree.Values)
                    Share.UploadManager.CancelUpload(upload.OrderID);

                throw;
            }

            // Ask the meta-data manager to perform the phase 1 operations.
            Debug.Assert(Share.MetaDataManager.Status == MetaDataManagerStatus.Idle);
            if (payload.OpList.Count > 0)
                Share.MetaDataManager.QueueOperation(payload, uploadTree, MetaDataTask.Cleanup);
        }

        /// <summary>
        /// Helper method for SynchronizePath().
        /// </summary>
        private void SynchronizePathRecursive(KfsStatusPath sp, bool openWhenDownloaded, bool canUpload)
        {
            if (openWhenDownloaded)
                Debug.Assert(sp.Status == PathStatus.NotDownloaded ||
                             sp.Status == PathStatus.UnmodifiedStale);

            // Synchronize file.
            if (sp.Status == PathStatus.NotDownloaded ||
                (sp.Status == PathStatus.ModifiedCurrent && canUpload) ||
                sp.Status == PathStatus.UnmodifiedStale)
            {
                KfsServerFile f = sp.ServerObject as KfsServerFile;
                Debug.Assert(f != null);

                if (Share.IsTransferringFile(sp.Path)) return;

                // Queue download.
                if ((sp.Status == PathStatus.NotDownloaded && sp.ServerObject.HasCurrentVersion()) ||
                    sp.Status == PathStatus.UnmodifiedStale)
                {
                    Share.DownloadManager.QueueDownload(f.CurrentVersion, openWhenDownloaded);
                }

                // Queue upload.
                else if (sp.Status == PathStatus.ModifiedCurrent)
                {
                    Share.UploadManager.QueueUpload(sp.Path);
                }
                else
                {
                    // Not synchronizable.
                }
            }

            // Synchronize directory.
            else if (sp.HasServerDir())
            {
                // Create the directory locally if needed.
                Share.AddMissingLocalDirectories(sp.Path);

                // Synchronize the children.
                foreach (KfsStatusPath child in sp.ChildTree.Values)
                    SynchronizePathRecursive(child, openWhenDownloaded, canUpload);
            }
        }

        /// <summary>
        /// Helper method for MovePath(). 'fromLoc' and 'toLoc' must end with a
        /// delimiter when non-empty.
        /// </summary>
        private bool MovePathRecursive(String fromLoc,
                                       String toLoc,
                                       List<String> srcArray,
                                       KfsPhase1Payload payload,
                                       bool topLevelFlag,
                                       ref String typeConflictAction)
        {
            // Remember whether we moved all the sources we had to move.
            bool allMovedFlag = true;

            // Add the missing local directories for the destination.
            Share.AddMissingLocalDirectories(toLoc);

            // Move the sources.
            String fileExistAction = "a";
            String dirExistAction = "a";

            foreach (String srcName in srcArray)
            {
                // Get the objects involved.
                String srcRelPath = fromLoc + srcName;
                String dstRelPath = toLoc + srcName;
                String srcFullPath = Share.MakeAbsolute(srcRelPath);
                String dstFullPath = Share.MakeAbsolute(dstRelPath);
                KfsStatusPath srcStatus = Share.StatusView.GetPath(srcRelPath);
                KfsStatusPath dstStatus = Share.StatusView.GetPath(dstRelPath);
                Debug.Assert(srcStatus != null, "srcStatus == null");
                KfsServerObject srcObject = srcStatus.ServerObject;
                KfsServerObject dstObject = (dstStatus == null) ? null : dstStatus.ServerObject;
                GatePathType srcType = GetInternalPathType(srcRelPath);
                GatePathType dstType = GetInternalPathType(dstRelPath);

                // Stale types, get out of here.
                if (srcType == GatePathType.None ||
                    srcType == GatePathType.Stale ||
                    dstType == GatePathType.Stale)
                    throw new GateSyncException();

                // There is a type conflict.
                else if (dstType != GatePathType.None && srcType != dstType)
                {
                    String prompt = "The entry " + srcRelPath + " conflicts with the entry " + dstRelPath + ".";
                    ShowChoiceDialog(prompt, "sc", ref typeConflictAction);
                    allMovedFlag = false;
                    continue;
                }

                // Add the 'to' directory remotely if the source exists remotely.
                if (srcObject != null) AddMissingServerDirectories(toLoc, payload);

                // We are in a directory-directory situation.
                if (srcType == GatePathType.Dir && dstType == GatePathType.Dir)
                {
                    if (topLevelFlag)
                    {
                        // Skip or overwrite the directory.
                        String prompt = "This folder already contains a folder named '" + dstRelPath + "'." + Environment.NewLine +
                                "If the files in the existing folder have the same name as files in the folder you are moving, they will be replaced. Do you still want to move this folder?";
                        String res = ShowChoiceDialog(prompt, "swc", ref dirExistAction);
                        if (res == "s")
                        {
                            allMovedFlag = false;
                            continue;
                        }
                    }

                    // Move all files and directories contained in the source directory.
                    String newFromLoc = srcRelPath + "/";
                    String newToLoc = dstRelPath + "/";
                    List<String> newSrcArray = new List<String>();
                    foreach (String name in srcStatus.ChildTree.Keys) newSrcArray.Add(name);
                    bool subMovedFlag = MovePathRecursive(newFromLoc, newToLoc, newSrcArray, payload,
                                                          false, ref typeConflictAction);

                    // All subdirectories and files were moved.
                    if (subMovedFlag)
                    {
                        // Ask the server to delete the source if it exists remotely.
                        if (srcStatus.HasServerDir())
                        {
                            payload.AddDeleteOp(false, srcObject.Inode, srcObject.CommitID);
                        }

                        // Otherwise, delete the source locally since the server won't do it
                        // for us.
                        else
                        {
                            File.SetAttributes(srcFullPath, FileAttributes.Normal);
                            Directory.Delete(srcFullPath);
                        }
                    }

                    // Not all subdirectories and files were moved.
                    else allMovedFlag = false;
                }

                // We are in a file-none, file-file or dir-none situation.
                else
                {
                    // Remember whether we are in a file-none or file-file situation.
                    bool fileFlag = (srcType == GatePathType.File);

                    // The destination exists.
                    if (dstStatus != null)
                    {
                        // Skip the source or overwrite the destination.
                        if (topLevelFlag && dstStatus.OnServerAndNotGhost())
                        {
                            String prompt = "The file " + dstRelPath + " already exists.";
                            String res = ShowChoiceDialog(prompt, "swc", ref fileExistAction);
                            if (res == "s")
                            {
                                allMovedFlag = false;
                                continue;
                            }
                        }

                        // Delete the destination file locally if it exists.
                        if (File.Exists(dstFullPath))
                        {
                            File.SetAttributes(dstFullPath, FileAttributes.Normal);
                            File.Delete(dstFullPath);
                        }
                    }

                    // The source exists only locally.
                    if (srcObject == null)
                    {
                        // The destination exists.
                        if (dstStatus != null)
                        {
                            // If the destination file exists remotely and it is not a ghost,
                            // pretend its current version was downloaded. This will make the
                            // source appear as ModifiedCurrent or UnmodifiedCurrent.
                            if (dstStatus.OnServerAndNotGhost())
                            {
                                KfsServerFile f = dstObject as KfsServerFile;
                                Debug.Assert(f != null);
                                f.UpdateDownloadVersion(f.CurrentVersion, true);
                            }
                        }

                        // Move the file or directory locally.
                        if (fileFlag) File.Move(srcFullPath, dstFullPath);
                        else Directory.Move(srcFullPath, dstFullPath);
                    }

                    // The source exists remotely.
                    else
                    {
                        // The destination file also exists remotely. Remove it.
                        if (dstObject != null)
                            payload.AddDeleteOp(true, dstObject.Inode, dstObject.CommitID);

                        // Ask the server to move the file.
                        UInt64 parentInode;
                        UInt64 parentCommitID;
                        String parentRelPath;
                        GetServerObjectPath(dstRelPath, out parentInode, out parentCommitID, out parentRelPath);
                        payload.AddMoveOp(fileFlag,
                                          srcObject.Inode,
                                          srcObject.CommitID,
                                          parentInode,
                                          parentCommitID,
                                          parentRelPath);
                    }
                }
            }

            return allMovedFlag;
        }

        /// <summary>
        /// Throw a GateInTransferException if one of the specified path is
        /// being transferred or contains a path that is being transferred.
        /// </summary>
        private void ThrowIfContainFileTransfer(List<String> pathArray)
        {
            foreach (String path in pathArray)
                foreach (KfsStatusPath sp in Share.StatusView.GetPathArray(path, false))
                    if (Share.IsTransferringFile(sp.Path))
                        throw new GateInTransferException("The file " + sp.Path + "is being transferred." +
                                                          "Please try again later.");
        }

        /// <summary>
        /// Return the type of the specified full path outside the share.
        /// </summary>
        private GatePathType GetExternalPathType(string fullPath)
        {
            if (File.Exists(fullPath)) return GatePathType.File;
            else if (Directory.Exists(fullPath)) return GatePathType.Dir;
            else return GatePathType.None;
        }

        /// <summary>
        /// Return the type of the specified relative path in the share.
        /// </summary>
        private GatePathType GetInternalPathType(string relPath)
        {
            // Check what the status view says about the path and validate
            // the information.
            String fullPath = Share.MakeAbsolute(relPath);
            KfsStatusPath sp = Share.StatusView.GetPath(relPath);

            if (sp == null || sp.Status == PathStatus.ServerGhost)
            {
                if (File.Exists(fullPath) || Directory.Exists(fullPath)) return GatePathType.Stale;
                return GatePathType.None;
            }

            else if (sp.IsUndetermined() || sp.IsTypeConflict())
            {
                return GatePathType.Stale;
            }

            else if (sp.IsDir())
            {
                if (sp.HasLocalDir()) return Directory.Exists(fullPath) ? GatePathType.Dir : GatePathType.Stale;
                return (!File.Exists(fullPath)) ? GatePathType.Dir : GatePathType.Stale;
            }

            else
            {
                Debug.Assert(sp.IsFile());
                if (sp.HasLocalFile()) return File.Exists(fullPath) ? GatePathType.File : GatePathType.Stale;
                return (!Directory.Exists(fullPath)) ? GatePathType.File : GatePathType.Stale;
            }
        }

        /// <summary>
        /// Handle an exception thrown inside a gate handler method.
        /// </summary>
        private void HandleException(Exception ex)
        {
            String dummy = "";

            // Ignore cancellation exceptions.
            if (ex is GateCancelledException)
            {
            }

            // Handle out-of-sync exceptions.
            else if (ex is GateSyncException)
            {
                String prompt = "Your files are being synchronized. Please try again later.";
                ShowChoiceDialog(prompt, "k", ref dummy);
            }

            // Handle illegal exceptions.
            else if (ex is GateIllegalException)
            {
                String prompt = ex.Message;
                ShowChoiceDialog(prompt, "k", ref dummy);
            }

            // Handle in-transfer exceptions.
            else if (ex is GateInTransferException)
            {
                String prompt = ex.Message;
                ShowChoiceDialog(prompt, "k", ref dummy);
            }

            // Handle generic exceptions.
            else
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Add the missing server directories on the relative path specified in
        /// the payload specified.
        /// </summary>
        private void AddMissingServerDirectories(String path, KfsPhase1Payload payload)
        {
            String cur = "";
            UInt64 inode = 0;
            UInt64 commitID = 0;
            String rel = "";

            foreach (String c in KfsPath.SplitRelativePath(path))
            {
                if (cur != "") cur += "/";
                cur += c;

                KfsServerObject o = Share.ServerView.GetObjectByPath(cur);

                // The current directory doesn't exist. Create it.
                if (o == null)
                {
                    if (rel != "") rel += "/";
                    rel += c;
                    payload.AddCreateOp(false, inode, commitID, rel);
                }

                // The current directory is a ghost. Delete the ghost and
                // replace it by a directory.
                else if (o.IsGhost())
                {
                    Debug.Assert(rel == "");
                    DeleteGhost(o, payload);
                    rel = c;
                    payload.AddCreateOp(false, inode, commitID, rel);
                }

                // The current directory already exists in the server view.
                else
                {
                    Debug.Assert(rel == "");
                    Debug.Assert(o is KfsServerDirectory);
                    inode = o.Inode;
                    commitID = o.CommitID;
                }
            }
        }

        /// <summary>
        /// Return the inode, commit ID and relative path used to refer to the
        /// specified server object.
        /// </summary>
        private void GetServerObjectPath(String path, out UInt64 inode, out UInt64 commitID, out String rel)
        {
            String cur = "";
            inode = 0;
            commitID = 0;
            rel = "";

            foreach (String c in KfsPath.SplitRelativePath(path))
            {
                if (cur != "") cur += "/";
                cur += c;

                KfsServerObject o = Share.ServerView.GetObjectByPath(cur);

                // The current directory exists in the server view.
                if (o != null && o is KfsServerDirectory)
                {
                    Debug.Assert(rel == "");
                    inode = o.Inode;
                    commitID = o.CommitID;
                }

                // The current directory doesn't exist.
                else
                {
                    if (rel != "") rel += "/";
                    rel += c;
                }
            }
        }

        /// <summary>
        /// Add an operation to delete the ghost specified in the payload
        /// specified.
        /// </summary>
        private void DeleteGhost(KfsServerObject o, KfsPhase1Payload p)
        {
            Debug.Assert(o.IsGhost());
            p.AddDeleteOp(true, o.Inode, o.CommitID);
        }

        /// <summary>
        /// Add an operation to delete the ghost specified in the payload
        /// specified if it exists.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="p"></param>
        private void DeleteGhostIfNeeded(String path, KfsPhase1Payload p)
        {
            KfsServerObject o = Share.ServerView.GetObjectByPath(path);
            if (o != null && o.IsGhost()) DeleteGhost(o, p);
        }

        /// <summary>
        /// Return true if the file path specified can be opened.
        /// </summary>
        /// <param name="itm"></param>
        /// <returns></returns>
        public bool CanOpen(KListViewItem itm)
        {
            if (Share.AllowedOp == AllowedOpStatus.None)
                return false;

            if (itm.Status != PathStatus.NotAdded &&
                itm.Status != PathStatus.ModifiedCurrent &&
                itm.Status != PathStatus.ModifiedStale &&
                itm.Status != PathStatus.UnmodifiedCurrent &&
                itm.Status != PathStatus.UnmodifiedStale &&
                itm.Status != PathStatus.NotDownloaded &&
                itm.Status != PathStatus.Undetermined)
            {
                return false;
            }

            // Allow opening if the file is not being downloaded. Since we don't yet
            // have the transfer direction handy, we assume NotAdded or ModifiedCurrent files
            // currently in transfer are necessarily being uploaded. A file being uploaded can
            // safely be opened.
            return !Share.IsTransferringFile(itm.Path) || itm.Status == PathStatus.NotAdded || itm.Status == PathStatus.ModifiedCurrent;
        }

        /// <summary>
        /// Return true if the file path specified can be saved as.
        /// </summary>
        public bool CanSaveAs(KListViewItem itm)
        {
            if (itm.Status != PathStatus.NotAdded &&
                itm.Status != PathStatus.ModifiedCurrent &&
                itm.Status != PathStatus.ModifiedStale &&
                itm.Status != PathStatus.UnmodifiedCurrent &&
                itm.Status != PathStatus.UnmodifiedStale)
            {
                return false;
            }

            // Allow only if the file is not being transfered, unless we are uploading it.
            return !Share.IsTransferringFile(itm.Path) || itm.Status == PathStatus.ModifiedCurrent;
        }

        /// <summary>
        /// Return true if the file path specified can be reverted to the latest
        /// server version.
        /// </summary>
        public bool CanRevert(KListViewItem itm)
        {
            return (itm.Status == PathStatus.ModifiedCurrent &&
                    !Share.IsTransferringFile(itm.Path));
        }

        /// <summary>
        /// Return true if the KListViewItem can be copied in the clipboard.
        /// </summary>
        public bool CanCopy(KListViewItem itm)
        {
            return CanCopy(itm.Path);
        }

        /// <summary>
        /// Return if the file path can be copied in the clipboard.
        /// </summary>
        /// <param name="relPath"></param>
        /// <returns></returns>
        private bool CanCopy(String relPath)
        {
            KfsStatusPath sp = Share.StatusView.GetPath(relPath);

            // We can copy just about anything, except files
            // that are not present, in any conflict or undetermined.
            // Files also must not be in transfer.
            if (sp != null &&
                sp.Status != PathStatus.NotAdded &&
                sp.Status != PathStatus.ModifiedCurrent &&
                sp.Status != PathStatus.ModifiedStale &&
                sp.Status != PathStatus.UnmodifiedCurrent &&
                sp.Status != PathStatus.UnmodifiedStale &&
                sp.Status != PathStatus.Directory)
            {
                return false;
            }

            // A nonexistent directory cannot be copied.
            if (sp.Status == PathStatus.Directory &&
                !Directory.Exists(Share.MakeAbsolute(relPath)))
                return false;

            return !Share.IsTransferringFile(relPath);
        }

        /// <summary>
        /// Return wether the share can accept a paste operation. At the moment, 
        /// a paste will only trigger a local filesystem copy. A share is always 
        /// ready to handle local filesystem modifications, so we only have to check
        /// the clipboard content.
        /// This may change if we want to add the pasted stuff to the share
        /// right away, as we might want to make sure the Share is ready to add
        /// new files.
        /// </summary>
        public bool CanPaste()
        {
            return Clipboard.ContainsFileDropList();
        }

        public bool CanResolveFileConflict(KListViewItem itm)
        {
            if (Share.AllowedOp == AllowedOpStatus.None)
                return false;

            return (itm.Status == PathStatus.ModifiedStale && !Share.IsTransferringFile(itm.Path));

        }
        /// <summary>
        /// Return true if the ListViewItem specified can be synchronized.
        /// </summary>
        /// <param name="itm"></param>
        /// <returns></returns>
        public bool CanSynchronize(KListViewItem itm)
        {
            if (Share.AllowedOp == AllowedOpStatus.None)
                return false;

            // Assume a directory present server-side
            // can be synchronized. If nothing inside of 
            // it can be synchronized, so be it.
            if (itm.Status == PathStatus.Directory && itm.OnServer)
                return true;

            if ((itm.Status == PathStatus.ModifiedCurrent && Share.AllowedOp == AllowedOpStatus.All) ||
                (itm.Status == PathStatus.NotDownloaded && itm.HasCurrentVersion && (Share.AllowedOp == AllowedOpStatus.All || Share.AllowedOp == AllowedOpStatus.Download)) ||
                itm.Status == PathStatus.UnmodifiedStale && (Share.AllowedOp == AllowedOpStatus.All || Share.AllowedOp == AllowedOpStatus.Download))
            {
                return !Share.IsTransferringFile(itm.Path);
            }

            // Other statuses can't be synchronized
            return false;
        }

        /// <summary>
        /// Return true if the Treenode can be synchronized.
        /// </summary>
        public bool CanSynchronize(KTreeNode node)
        {
            // Assume a directory present server-side
            // can be synchronized. If nothing inside of 
            // it can be synchronized, so be it.

            return node.OnServer && Share.AllowedOp != AllowedOpStatus.None;
        }

        /// <summary>
        /// Return true if the Treenode specified can
        /// be added to the share.
        /// </summary>
        public bool CanAdd(KTreeNode node)
        {
            return !node.OnServer && Share.AllowedOp == AllowedOpStatus.All;
        }

        /// <summary>
        /// Return true if the ListviewItem can
        /// be added to the share.
        /// </summary>
        /// <param name="itm"></param>
        /// <returns></returns>
        public bool CanAdd(KListViewItem itm)
        {
            if (Share.AllowedOp != AllowedOpStatus.All)
                return false;

            if (itm.Status != PathStatus.NotAdded &&
              !(itm.Status == PathStatus.Directory && !itm.OnServer) ||
               (itm.Status != PathStatus.Directory && Share.IsTransferringFile(itm.Path)))
            {
                return false;
            }
            
            return true;

        }

        /// <summary>
        /// Return true if the ListviewItem specified can be deleted or moved.
        /// </summary>
        public bool CanDeleteOrMove(KListViewItem itm)
        {
            return CanDeleteOrMoveInternal(Share.StatusView.GetPathArray(itm.Path, false));
        }

        /// <summary>
        /// Return true if the KTreeNode specified can be deleted or moved.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool CanDeleteOrMove(KTreeNode node)
        {
            return (node.Name != "" && 
                    CanDeleteOrMoveInternal(Share.StatusView.GetPathArray(node.KFullPath, false)));
        }

        private bool CanDeleteOrMoveInternal(List<KfsStatusPath> lst)
        {
            if (Share.AllowedOp != AllowedOpStatus.All)
                return false;
   
            foreach (KfsStatusPath s in lst)
            {
                if (Share.IsTransferringFile(s.Path) ||
                    s.Status == PathStatus.Undetermined)
                    return false;
            }

            return true;
        }
    }
}