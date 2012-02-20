using System;
using System.Windows.Forms;
using System.Resources;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Tbx.Utils;
using System.Reflection;

namespace kwm.Utils
{
    /// <summary>
    /// Limited reference to the UI broker.
    /// </summary>
    public interface IUiBroker
    {
        void OnUiEntry();
        void OnUiExit();
        void RequestShowMainForm();
        DialogResult ShowConfigWizard();
    }

    /// <summary>
    /// This class contains various static utility methods that can be useful 
    /// throughout the code.
    /// </summary>
    public class Misc
    {
        /// <summary>
        /// SendMessage() for use with WM_COPYDATA and COPYDATASTRUCT.
        /// </summary>
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);
        public const int WM_USER = 0x400;
        public const int WM_COPYDATA = 0x4A;
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }

        /// <summary>
        /// This flag indicates whether it is safe to display a fatal error
        /// message. This is false on startup to prevent recursive fatal 
        /// error spawners.
        /// </summary>
        public static bool FatalErrorMsgOKFlag = false;

        /// <summary>
        /// True if a fatal error is being handled.
        /// </summary>
        private static volatile bool m_fatalErrorCaughtFlag = false; 

        /// <summary>
        /// Reference to the main form, if any.
        /// </summary>
        public static Form MainForm = null;

        /// <summary>
        /// Limited reference to the UI broker, if any.
        /// </summary>
        public static IUiBroker UiBroker = null;

        private static ResourceManager m_rm;
        private static KwmApplicationSettings m_appSettings = new KwmApplicationSettings();

        /// <summary>
        /// Contain the list of NewDocuments present in Windows Registry.
        /// (The ShellNew trick to replicate the Windows shell context
        /// menu).
        /// </summary>
        private static List<NewDocument> NewDocs = null;

        // Singleton.
        private Misc()
        {
        }

        public static KwmApplicationSettings ApplicationSettings
        {
            get
            {
                return m_appSettings;
            }
        }

        public static String KwmVersion
        {
            get
            {
                RegistryKey version = null;
                try
                {
                    version = Registry.LocalMachine.OpenSubKey(Base.GetKwmRegKey(), false);
                }
                catch (Exception ex)
                {
                    Logging.LogException(ex);
                }

                if (version == null)
                {
                    Logging.Log(2, "Unable to find " + Base.GetKwmString() + " version.");
                    return "Unknown";
                }
                else
                {
                    return (String)version.GetValue("KWM_Version", "Unknown");
                }
            }
        }

        public static String GetVncServerRegKey()
        {
            return Base.GetKwmRegKey() + "\\vnc";
        }

        public static String GetKwmLogFilePath()
        {
            return Base.GetKcsLogFilePath() + "kwm\\";
        }

        public static String GetKmodDirPath()
        {
            return Base.GetKcsLogFilePath() + "kmod";
        }

        public static String GetKtlstunnelLogFilePath()
        {
            return Base.GetKcsLogFilePath() + "ktlstunnel\\";
        }

        public static String GetKwmRoamingStatePath()
        {
            return Base.GetKcsRoamingDataPath() + "kwm\\state\\";
        }

        public static String GetKwmLocalStatePath()
        {
            return Base.GetKcsLocalDataPath() + "kwm\\state\\";
        }

        public static String GetCorruptedWmPath()
        {
            return Base.GetKcsRoamingDataPath() + "kwm\\state\\Corrupted\\";
        }

        public static String GetKwmDbPath()
        {
            return GetKwmRoamingStatePath() + "local.db";
        }

        public static String GetKfsDefaultStorePath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Teambox Shares\\";
        }

        /// <summary>
        /// Don't ask, don't tell. Close your eyes. Go away. Life is too short
        /// to dick around with this crap.
        /// </summary>
        private static String EscapeArgForFatalError(String insanity)
        {
            insanity = insanity.Replace("\"", "\"\"");
            insanity = insanity.Replace("\\", "\"\\\\\"");
            return insanity;
        }

        /// <summary>
        /// Display an error message to the user and exit the application if 
        /// required.
        /// </summary>
        public static void HandleError(String errorMessage, bool fatalFlag)
        {
            string msg = "An error has been detected." +
                                     Environment.NewLine + Environment.NewLine +
                                     errorMessage +
                                     Environment.NewLine + Environment.NewLine;
            if (fatalFlag)
            {
                msg += "Please restart your " + Base.GetKwmString();
            }
            else
            {
                msg += "Please contact your technical support for further information.";
            }

            if (!fatalFlag)
            {
                Misc.KwmTellUser(msg, MessageBoxIcon.Error);
                return;
            }

            // The .Net framework is critically brain damaged when it comes to
            // handling fatal errors. There are basically two choices: exit
            // the process right away or try to get the application to display
            // the error and quit.
            //
            // The former choice is sane; there is no risk of further data
            // corruption if the process exits right away. The lack of any
            // error message is problematic however. We work around this by
            // spawning an external program, if possible, to report the error
            // before exiting the process.
            //
            // The second choice cannot be done sanely. If a MessageBox()
            // call is made immediately when the error is detected, then
            // the UI will be reentered and the damage may spread further.
            // Typically this causes multiple fatal error messages to
            // appear. After some investigation, I believe this is impossible
            // to prevent. The best available thing is ThreadAbortException,
            // which has weird semantics and is considered deprecated and
            // doesn't do the right thing in worker threads.

            // Exit right away.
            if (!FatalErrorMsgOKFlag || m_fatalErrorCaughtFlag)
                Environment.Exit(1);

            // We have caught a fatal error. Prevent the other threads from
            // spawning a fatal error. There is an inherent race condition
            // here which is best left alone; mutexes have no business here.
            m_fatalErrorCaughtFlag = true;

            // Spawn a program to display the message.
            try
            {
                String startupLine = '"' + Application.ExecutablePath + '"' + " ";
                startupLine += "\"-M\" \"" + EscapeArgForFatalError(msg) + "\"";
                RawProcess p = new RawProcess(startupLine);
                p.InheritHandles = false;
                p.Start();
            }

            // Ignore all exceptions.
            catch (Exception)
            {
            }

            // Get out.
            Environment.Exit(1);
        }

        /// <summary>
        /// Ensure that the caller is executing in UI context.
        /// </summary>
        public static void EnsureNoInvokeRequired()
        {
            if (IsUiStateOK() && MainForm.InvokeRequired)
                HandleError("Executing outside UI context", true);
        }

        /// <summary>
        /// Return true if the UI (main form, UI broker) can be entered safely.
        /// </summary>
        public static bool IsUiStateOK()
        {
            return (MainForm != null &&
                    !MainForm.IsDisposed && 
                    !MainForm.Disposing && 
                    UiBroker != null);
        }

        /// <summary>
        /// This method must be called once when the UI is entered.
        /// </summary>
        public static void OnUiEntry()
        {
            // Some dialogs do not check if the UI can be reentered.
            if (!IsUiStateOK()) return;
            UiBroker.OnUiEntry();
        }

        /// <summary>
        /// This method must be called once when the UI is exited.
        /// </summary>
        public static void OnUiExit()
        {
            if (!IsUiStateOK()) return;
            UiBroker.OnUiExit();
        }

        public static DialogResult KwmTellUser(String _message)
        {
            return KwmTellUser(_message, MessageBoxIcon.Information);
        }

        public static DialogResult KwmTellUser(String _message, MessageBoxIcon _icon)
        {
            return KwmTellUser(_message, Base.GetKwmString(), _icon);
        }

        public static DialogResult KwmTellUser(String _message, String _title, MessageBoxIcon _icon)
        {
            return KwmTellUser(_message, _title, MessageBoxButtons.OK, _icon);
        }

        public static DialogResult KwmTellUser(String _message, String _title,
                                               MessageBoxButtons _buttons, MessageBoxIcon _icon)
        {
            try
            {
                EnsureNoInvokeRequired();

                // The UI is in a bad state. Do not reference the main form or the
                // UI broker.
                if (!IsUiStateOK()) return MessageBox.Show(_message, _title, _buttons, _icon);

                // Display the message box properly.
                UiBroker.RequestShowMainForm();
                Misc.OnUiEntry();
                DialogResult res = MessageBox.Show(MainForm, _message, _title, _buttons, _icon);
                Misc.OnUiExit();
                return res;
            }

            catch (Exception ex)
            {
                Base.HandleException(ex, true);
                return DialogResult.Cancel;
            }
        }

        /// <summary>
        /// Show a little Help tooltip close to the mouse cursor.
        /// </summary>
        public static void ShowHelpTooltip(String text, Control sender)
        {            
            Help.ShowPopup(sender, text, new Point(Cursor.Position.X, Cursor.Position.Y + 20));
        }

        /// <summary>
        /// Return true if the Otc component has been installed, false otherwise
        /// or on any error.
        /// </summary>
        public static bool IsOtcInstalled
        {
            get
            {
                RegistryKey key = null;
                try
                {
                    key = Registry.LocalMachine.OpenSubKey(Base.GetKwmRegKey() + "\\Components");
                    if (key == null) return false;

                    int val;
                    if (Int32.TryParse(key.GetValue("OutlookConnector", "0") as String, out val))
                        return val > 0;

                    return false;
                }
                finally
                {
                    if (key != null) key.Close();
                }

            }
        }

        public static void InitResourceMngr(Assembly assembly)
        {
            m_rm = new ResourceManager("kwm.ressources", assembly);
        }

        public static String GetString(String _id)
        {
            if (m_rm == null) return _id;
            return m_rm.GetString(_id);
        }

        /// <summary>
        /// Converts an image into an icon.
        /// Taken from http://www.dreamincode.net/code/snippet1684.htm.
        /// </summary>
        /// <param name="img">The image that shall become an icon</param>
        /// <param name="size">The width and height of the icon. Standard
        /// sizes are 16x16, 32x32, 48x48, 64x64.</param>
        /// <param name="keepAspectRatio">Whether the image should be squashed into a
        /// square or whether whitespace should be put around it.</param>
        /// <returns>An icon.</returns>
        public static Icon MakeIcon(Image img, int size, bool keepAspectRatio)
        {
            Bitmap square = new Bitmap(size, size); // create new bitmap
            Graphics g = Graphics.FromImage(square); // allow drawing to it

            int x, y, w, h; // dimensions for new image

            if (!keepAspectRatio || img.Height == img.Width)
            {
                // just fill the square
                x = y = 0; // set x and y to 0
                w = h = size; // set width and height to size
            }
            else
            {
                // work out the aspect ratio
                float r = (float)img.Width / (float)img.Height;

                // set dimensions accordingly to fit inside size^2 square
                if (r > 1)
                { // w is bigger, so divide h by r
                    w = size;
                    h = (int)((float)size / r);
                    x = 0; y = (size - h) / 2; // center the image
                }
                else
                { // h is bigger, so multiply w by r
                    w = (int)((float)size * r);
                    h = size;
                    y = 0; x = (size - w) / 2; // center the image
                }
            }

            // make the image shrink nicely by using HighQualityBicubic mode
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, x, y, w, h); // draw image with specified dimensions
            g.Flush(); // make sure all drawing operations complete before we get the icon

            // following line would work directly on any image, but then
            // it wouldn't look as nice.
            return Icon.FromHandle(square.GetHicon());
        }

        /// <summary>
        /// Open a given file using the Windows Shell in a worker thread (Fire and forget
        /// way).
        /// </summary>
        public static void OpenFileInWorkerThread(string p)
        {
            OpenFileThread t = new OpenFileThread(p);
            t.Start();
        }

        /// <summary>
        /// Open a given file using the Windows Shell. 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns>True on error, false otherwise.</returns>
        public static bool OpenFile(string fullPath, ref string errorMsg)
        {
            bool error = false;
            try
            {
                Logging.Log("OpenFile(" + fullPath + ") called.");
                Process p = new Process();
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.FileName = fullPath;
                p.Start();
                Logging.Log("OpenFile: After p.Start()");
            }
            catch (Win32Exception)
            {
                Logging.Log("OpenFile(): Caught Win32Exception");
                // 1155 is the following error :
                // No application is associated with the specified file for this operation
                // Show the Open as... dialog in this case.
                if (Syscalls.GetLastError() == 1155)
                {
                    Syscalls.SHELLEXECUTEINFO info = new Syscalls.SHELLEXECUTEINFO();
                    info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
                    info.lpDirectory = Path.GetDirectoryName(fullPath);
                    info.lpFile = Path.GetFileName(fullPath);
                    info.nShow = (int)Syscalls.SW.SW_SHOWDEFAULT;
                    info.lpVerb = "openas";
                    info.fMask = Syscalls.SEE_MASK.ASYNCOK;
                    error = !Syscalls.ShellExecuteEx(ref info);
                }
                else
                {
                    errorMsg = Syscalls.GetLastErrorStringMessage();
                    error = true;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                error = true;
            }
            Logging.Log("OpenFile exiting.");
            return error;
        }

        /// <summary>
        /// Return the displayable name of a given application. 
        /// KAnp 'type' field must be masked with KAnpType.NAMESPACE_ID_MASK in
        /// order to get the right application ID.
        /// </summary>
        public static string GetApplicationName(UInt32 application)
        {
            switch (application)
            {
                case KAnpType.KANP_NS_CHAT: return "Message Board";
                case KAnpType.KANP_NS_KFS: return "Files";
                case KAnpType.KANP_NS_VNC: return "Screen sharing";
                case KAnpType.KANP_NS_WB: return "Whiteboard";
                case KAnpType.KANP_NS_PB: return "Public Workspace";
                default: return "Unknown";
            }
        }

        private static void PrintTreeviewContentRecursive(TreeNode treeNode)
        {
            // Print the node.
            Debug.WriteLine(treeNode.Text);
            // Print each node recursively.
            foreach (TreeNode tn in treeNode.Nodes)
            {
                PrintTreeviewContentRecursive(tn);
            }
        }

        /// <summary>
        /// Prints recursivly to the Debug output the nodes of the Treeview.
        /// </summary>
        public static void PrintTreeviewContent(TreeView treeView)
        {
            // Print each node recursively.
            TreeNodeCollection nodes = treeView.Nodes;
            foreach (TreeNode n in nodes)
            {
                PrintTreeviewContentRecursive(n);
            }
        }

        /// <summary>
        /// Copy the given file to the target directory. If the filename
        /// already exists, rename the destination file to a unique name.
        /// </summary>
        /// <param name="SourceFilePath">Full path of the file to copy.</param>
        /// <param name="DestDirPath">Full path to the target directory, backslash-terminated.</param>
        public static bool CopyFileAndRenameOnCol(String SourceFilePath,
                                                  String DestFilePath)
        {
            String srcFileName = Path.GetFileName(SourceFilePath);
            String srcFileNameNoExt = Path.GetFileNameWithoutExtension(SourceFilePath);
            String srcFileNameExt = Path.GetExtension(SourceFilePath);

            String dstFileName = srcFileName;

            if (File.Exists(DestFilePath + dstFileName))
            {
                int i = 1;
                while (File.Exists(DestFilePath + srcFileNameNoExt + " (" + i + ")" + srcFileNameExt))
                    i++;

                dstFileName = srcFileNameNoExt + " (" + i + ")" + srcFileNameExt;
            }


            List<String> S = new List<string>();
            List<String> D = new List<string>();

            S.Add(SourceFilePath);
            D.Add(DestFilePath + dstFileName);

            return CopyFiles(S, D, false, true, false, false, true, false);
        }

        /// <summary>
        /// Copy one file using the Shell API. Set LockUI to true if you want frmMain to be
        /// disabled during the operation, since the progress dialog is not modal.
        /// </summary>
        /// <returns>True if the operation completed with success, false if the user aborted or if an error occured.</returns>
        public static bool CopyFile(String SourceFilePath,
                                    String DestFilePath,
                                    bool DisplayErrors,
                                    bool SilentOverwrite,
                                    bool RenameDestOnCollision,
                                    bool SimpleProgress,
                                    bool LockUI)
        {
            List<String> S = new List<string>();
            List<String> D = new List<string>();

            S.Add(SourceFilePath);
            D.Add(DestFilePath);

            return CopyFiles(S, D, DisplayErrors, SilentOverwrite, RenameDestOnCollision, SimpleProgress, false, LockUI);
        }

        /// <summary>
        /// Copy one file using the Shell API. The progress dialog displayed is not modal.
        /// </summary>
        /// <param name="Sources">Source paths of the files to copy.</param>
        /// <param name="Destinations">Destination paths of the files to copy.</param>
        /// <param name="DisplayErrors">No user interface will be displayed if an error occurs.</param>
        /// <param name="SilentOverwrite">Do not prompt the user before overwritting the destination file. </param>
        /// <param name="RenameDestOnCollision">Give the file being operated on a new name in a move, copy, or rename operation if a file with the target name already exists.</param>
        /// <param name="SimpleProgress">Displays a progress dialog box but does not show the file names.</param>
        /// <param name="Silent">Does not display a progress dialog box. </param>
        /// <param name="LockUI">Set to true if you want the entire application to be disabled during the operation.</param>
        /// <returns>True if the operation completed with success, false if the user aborted or if an error occured.</returns>
        public static bool CopyFiles(List<String> Sources, List<String> Destinations, bool DisplayErrors, bool SilentOverwrite, bool RenameDestOnCollision, bool SimpleProgress, bool Silent, bool LockUI)
        {
            // Prepare the Shell.
            ShellLib.ShellFileOperation FileOp = new ShellLib.ShellFileOperation();
            FileOp.Operation = ShellLib.ShellFileOperation.FileOperations.FO_COPY;

            if (DisplayErrors)
                FileOp.OperationFlags |= ShellLib.ShellFileOperation.ShellFileOperationFlags.FOF_NOERRORUI;

            if (RenameDestOnCollision)
                FileOp.OperationFlags |= ShellLib.ShellFileOperation.ShellFileOperationFlags.FOF_RENAMEONCOLLISION;

            if (SilentOverwrite)
                FileOp.OperationFlags |= ShellLib.ShellFileOperation.ShellFileOperationFlags.FOF_NOCONFIRMATION;

            if (SimpleProgress)
                FileOp.OperationFlags |= ShellLib.ShellFileOperation.ShellFileOperationFlags.FOF_SIMPLEPROGRESS;


            FileOp.OwnerWindow = Misc.MainForm.Handle;
            FileOp.SourceFiles = Sources.ToArray();
            FileOp.DestFiles = Destinations.ToArray();

            // Disable the entire application while the copy is being done.
            // Necessary since we can't make the progress dialog modal.
            try
            {
                if (LockUI)
                    Misc.MainForm.Enabled = false;
                return FileOp.DoOperation();
            }

            finally
            {
                if (LockUI)
                    Misc.MainForm.Enabled = true;
            }
        }

        /// <summary>
        /// Copies the content of the given directory to another.
        /// </summary>
        public static void CopyDirContent(string sourceDirectory, string targetDirectory)
        {
            if (sourceDirectory == null)
                throw new ArgumentNullException("sourceDirectory");
            if (targetDirectory == null)
                throw new ArgumentNullException("targetDirectory");

            // Call the recursive method.
            CopyAllFiles(new DirectoryInfo(sourceDirectory), new DirectoryInfo(targetDirectory));
        }

        // Copies all files from one directory to another.
        private static void CopyAllFiles(DirectoryInfo source, DirectoryInfo target)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (target == null)
                throw new ArgumentNullException("target");

            // If the source doesn't exist, we have to throw an exception.
            if (!source.Exists)
                throw new DirectoryNotFoundException("Source directory not found: " + source.FullName);
            // If the target doesn't exist, we create it.
            if (!target.Exists)
                target.Create();

            // Get all files and copy them over.
            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }

            // Do the same for all sub directories.
            foreach (DirectoryInfo directory in source.GetDirectories())
            {
                CopyAllFiles(directory, new DirectoryInfo(Path.Combine(target.FullName, directory.Name)));
            }
        }

        /// <summary>
        /// Parse the Window Registry and build the list of items that 
        /// appear in the File / New Explorer menu.
        /// The list excludes undesired types such as shortcuts or briefcase objects.
        /// Set ForceRefresh to force the rebuild of the list.
        /// </summary>
        /// <returns></returns>
        public static List<NewDocument> GetNewDocs(bool ForceRefresh)
        {
            if (NewDocs != null && !ForceRefresh)
                return NewDocs;

            List<NewDocument> NewDocuments = new List<NewDocument>();
            
            RegistryKey HKCR = Registry.ClassesRoot;

            // Enumerate the content of HKCR. 
            // We are only interested in the extension keys (.xxx).
            foreach (String strExtensionKey in HKCR.GetSubKeyNames())
            {
                RegistryKey ExtensionKey = null;
                RegistryKey ProgIDKey = null;

                try
                {
                    if (!strExtensionKey.StartsWith("."))
                        continue;

                    String progID = "";

                    // 1. Get the ProgID value located in the (Default) value present
                    // in the key.
                    // Example: Key = HKEY_CLASSES_ROOT\.doc, value (Default) = Word.Document.8
                    ExtensionKey = HKCR.OpenSubKey(strExtensionKey);

                    progID = (String)ExtensionKey.GetValue("");

                    // Ignore invalid ProgIDs.
                    if (progID == null || progID == "")
                        continue;

                    // 2. Explore the key. Remember if we have a ShellNew key 
                    // exists right there (Example : HKEY_CLASSES_ROOT\.bfc\ShellNew).
                    // Also, remember if we saw a ProgID subkey. Do not confuse
                    // with the ProgID present in the (Default) value.
                    // The ProgID key should be there, but we never know. 
                    bool ShellNewKeyInRoot = false;
                    bool ProgIDKeyPresent = false;

                    foreach (String strSubkey in ExtensionKey.GetSubKeyNames())
                    {
                        if (strSubkey == "ShellNew")
                        {
                            ShellNewKeyInRoot = true;
                        }

                        else if (strSubkey == progID)
                        {
                            ProgIDKeyPresent = true;
                        }
                    }

                    // 4. Look for a ShellNew under <ProgID>.
                    // Example for the ProgID Word.Document.8 :
                    // HKEY_CLASSES_ROOT\.doc\Word.Document.8\ShellNew
                    if (ProgIDKeyPresent)
                    {
                        ProgIDKey = ExtensionKey.OpenSubKey(progID);
                        foreach (String strProgIDSubkey in ProgIDKey.GetSubKeyNames())
                        {
                            if (strProgIDSubkey == "ShellNew")
                            {
                                NewDocument doc = NewDocument.GetNewDoc(ProgIDKey, strExtensionKey, progID);
                                if (doc != null && IsNewDocumentWanted(doc))
                                    NewDocuments.Add(doc);
                            }
                        }
                    }
                    // 5. Fallback to the ShellNew key directly in root.
                    else if (ShellNewKeyInRoot)
                    {
                        NewDocument doc = NewDocument.GetNewDoc(ExtensionKey, strExtensionKey, progID);

                        if (doc != null && IsNewDocumentWanted(doc))
                            NewDocuments.Add(doc);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("An error has occured: " + ex.Message);
                    continue;
                }
                finally
                {
                    if (ExtensionKey != null)
                        ExtensionKey.Close();

                    if (ProgIDKey != null)
                        ProgIDKey.Close();
                }
            } // main foreach loop

            return NewDocuments;
        }

        /// <summary>
        /// Return true if the NewDocument is desired in our list.
        /// </summary>
        private static bool IsNewDocumentWanted(NewDocument doc)
        {
            if (doc == null)
                return false;

            return (doc.Extension != ".lnk" &&
                    doc.Extension != ".lnk2" &&
                    doc.Extension != ".bfc" &&
                    doc.Extension != ".jnl" &&
                    doc.Extension != ".zip");
        }

        /// <summary>
        /// Create a new XmlElement and add it to the specified XmlElement if parent is not null.
        /// </summary>
        public static XmlElement CreateXmlElement(XmlDocument doc, XmlElement parent, String name, String text)
        {
            XmlElement elem = doc.CreateElement(name);
            XmlText txtElem = doc.CreateTextNode(text);
            elem.AppendChild(txtElem);

            if (parent != null)
                parent.AppendChild(elem);

            return elem;
        }

        /// <summary>
        /// Return the child element 'name' in 'parent', if any.
        /// </summary>
        public static XmlElement GetXmlChildElement(XmlElement parent, String name)
        {
            XmlNodeList list = parent.GetElementsByTagName(name);
            return (list.Count == 0) ? null : list.Item(0) as XmlElement;
        }

        /// <summary>
        /// Return the value associated to the child element 'name' in
        /// 'parent'. The string 'defaultText' is returned if the child
        /// element is not found.
        /// </summary>
        public static String GetXmlChildValue(XmlElement parent, String name, String defaultText)
        {
            XmlElement elem = GetXmlChildElement(parent, name);
            return (elem == null) ? defaultText : elem.InnerText;
        }

        /// <summary>
        /// This method returns an exception when an unexpected reply is
        /// received from the KAS.
        /// </summary>
        public static Exception HandleUnexpectedKAnpReply(String command, AnpMsg msg)
        {
            try
            {
                if (msg.Type == KAnpType.KANP_RES_FAIL)
                    return new Exception("command " + command + " failed: " + msg.Elements[1].String);
            }

            // Handle malformed messages.
            catch (Exception ex)
            {
                return ex;
            }

            return new Exception("Unexpected KAnp reply " + msg.Type + " for command " + command);
        }
    }

    /// <summary> 
    /// Generic class to get the icon associated to a 
    /// given file type. The code should use Misc.GetBestIcon(), 
    /// not these helper functions.
    /// </summary>
    public class ExtractIcon
    {
        /// <summary>
        /// Get the associated Icon for a file or application, this method always returns
        /// an icon.  If the strPath is invalid or there is no idonc the default icon is returned
        /// </summary>
        /// <param name="strPath">full path to the file</param>
        /// <param name="bSmall">if true, the 16x16 icon is returned otherwise the 32x32</param>
        /// <returns></returns>
        public static Icon GetIcon(string strPath, bool bSmall)
        {
            Syscalls.SHFILEINFO info = new Syscalls.SHFILEINFO(true);
            int cbFileInfo = Marshal.SizeOf(info);
            Syscalls.SHGFI flags;
            if (bSmall)
                flags = Syscalls.SHGFI.Icon | Syscalls.SHGFI.SmallIcon | Syscalls.SHGFI.UseFileAttributes;
            else
                flags = Syscalls.SHGFI.Icon | Syscalls.SHGFI.LargeIcon | Syscalls.SHGFI.UseFileAttributes;

            if (Syscalls.SHGetFileInfo(strPath, 256, out info, (uint)cbFileInfo, flags) == 0)
            {
                Logging.Log("Error loading icon : " + Syscalls.GetLastErrorStringMessage());
                return kwm.KwmAppControls.Properties.Resources.DefaultFileIcon;
            }

            return GetManagedIcon(info.hIcon);
        }

        /* When getting an icon from ShGetFileInfo, we are responsible for freeing the
         * resource. The idea is to clone the icon in a managed object
         * and free the resource right away so the caller does not have to deal with this. */
        private static Icon GetManagedIcon(IntPtr hIcon)
        {
            Icon Clone = (Icon)Icon.FromHandle(hIcon).Clone();

            Syscalls.DestroyIcon(hIcon);

            return Clone;
        }
    }
}
