using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using kwm.Utils;
using System.Security.Principal;
using Microsoft.Win32;
// LGPL library taken from CodeProject
using CodePoints;
using System.Threading;
using System.Text;
using Tbx.Utils;

namespace kwm
{
    public enum KwmOtherProcessState
    {
        /// <summary>
        /// No conflicting kwm process.
        /// </summary>
        None,

        /// <summary>
        /// A kwm process started by our user is running in our session.
        /// </summary>
        OurInCurrentSession,

        /// <summary>
        /// A kwm process started by our user is running in another session.
        /// </summary>
        OurInOtherSession,

        /// <summary>
        /// A kwm process not started by our user is running in our session.
        /// </summary>
        NotOurInCurrentSession
    }

    public static class Program
    {
        /// <summary>
        /// ID of the message sent to import a workspace. Using a high ID in
        /// case the message lands in the message queue of the wrong process. 
        /// </summary>
        public const int ImportKwsMsgID = 7775632;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#if UNITTEST
            return UnitTestMain(args);
#else
            try
            {
                return KwmMain(args);
            }

            catch (Exception ex)
            {
                Misc.KwmTellUser(ex.ToString());
                return -1;
            }
#endif
        }

        static int UnitTestMain(string[] args)
        {
            Application.Run(new KwmUnitTest());
            return 0;
        }

        static int KwmMain(string[] args)
        {
            try
            {
                // Set the reference to the error handler.
                Base.HandleErrorCallback = Misc.HandleError;

                Logging.SetLoggingLevel(LoggingLevel.Normal);

                // Parse the command line options.
                KwmCommandLine cmdLine = new KwmCommandLine();
                try
                {
                    if (!cmdLine.Parse(args))
                        return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Option error: " + ex.Message);
                    cmdLine.usage();
                    return 0;
                }

                // We can handle fatal errors at this point.
                Misc.FatalErrorMsgOKFlag = true;

                // Determine the other process state.
                KwmOtherProcessState otherState;
                Process otherProcess;
                GetOtherProcessState(out otherState, out otherProcess);

                // Another KWM we own is running in our session.
                if (otherState == KwmOtherProcessState.OurInCurrentSession)
                {
                    // Send the other KWM a message to import the credentials.
                    if (cmdLine.ImportKwsPath != "") SendImportMsgToOtherKwm(otherProcess, cmdLine.ImportKwsPath);

                    // Show the instance of the other KWM.
                    SwitchToOtherKwm();

                    return 0;
                }

                else if (otherState == KwmOtherProcessState.NotOurInCurrentSession)
                    throw new Exception("A " + Base.GetKwmString() + " started by another user is already running.");

                else if (otherState == KwmOtherProcessState.OurInOtherSession)
                    throw new Exception("Your " + Base.GetKwmString() + " is already running in another session.");
            
                // Create a spawner instance.
                KwmSpawner.Instance = new KwmSpawner();

                // Spawn the Workspace Manager.
                WorkspaceManager wm = KwmSpawner.Instance.Spawn();

                // Exit if the WM could not be spawned.
                if (wm == null) return 0;

                // Clear the spawner instance to avoid leaks.
                KwmSpawner.Instance = null;

                // Export the Credentials from the WM to the given path.
                if (cmdLine.ExportKwsPath != "")
                {
                    // FIXME, this is not gonna work from here.
                    return 0;
                }

                // Set the import data, if any.
                if (cmdLine.ImportKwsPath != "")
                {
                    try
                    {
                        WmImportData data = WorkspaceManager.GetImportDataFromFile(cmdLine.ImportKwsPath);
                        wm.ImportData.Add(data);
                    }

                    catch (Exception ex)
                    {
                        WmUiBroker.TellUserAboutImportError(ex);
                        return 0;
                    }
                }

                // Initialize the main form and run the application.
                Application.Run(wm.UiBroker.InitMainForm());
                return 0;
            }

            catch (Exception e)
            {
                // frmMain is now disposed: do not use it anymore.
                Base.InvokeUiControl = null;
                Misc.MainForm = null;
                Misc.UiBroker = null;

                Logging.LogException(e);
                Misc.KwmTellUser(e.ToString());
                return -1;
            }
        }

        /// <summary>
        /// Send a message to import the workspace credentials to the other
        /// KWM process.
        /// </summary>
        private static void SendImportMsgToOtherKwm(Process process, String path)
        {
            IntPtr handle = GetOtherKwmHandle();

            // Wait 10 seconds for the other process to finish initializing.
            if (handle != IntPtr.Zero && process.WaitForInputIdle(10 * 1000))
            {
                // Send the message.
                Misc.COPYDATASTRUCT cds;
                cds.dwData = new IntPtr(ImportKwsMsgID);
                cds.lpData = path;
                cds.cbData = Encoding.Default.GetBytes(path).Length + 1;
                Misc.SendMessage(handle.ToInt32(), Misc.WM_COPYDATA, 0, ref cds);
            }
        }

        /// <summary>
        /// Show in foreground the other kwm that is running in our session.
        /// </summary>
        private static void SwitchToOtherKwm()
        {
            IntPtr handle = GetOtherKwmHandle();
            if (handle != IntPtr.Zero)
            {
                Syscalls.ShowWindowAsync(handle, (int)Syscalls.WindowStatus.SW_SHOWNORMAL);
                Base.KSetForegroundWindow(handle);
            }
        }

        /// <summary>
        /// Return the handle to the other KWM process, or IntPtr.Zero if none.
        /// </summary>
        private static IntPtr GetOtherKwmHandle()
        {
            RegistryKey regKey = null;

            try
            {
                regKey = Registry.CurrentUser.OpenSubKey(Base.GetKwmRegKey());
                if (regKey != null)
                {
                    int handle = Int32.Parse((String)regKey.GetValue("kwmWindowHandle"));
                    if (handle != 0) return new IntPtr(handle);
                }

                return IntPtr.Zero;
            }

            finally
            {
                if (regKey != null) regKey.Close();
            }
        }

        /// <summary>
        /// Determine the state of another kwm instance that is related to our instance.
        /// </summary>
        private static void GetOtherProcessState(out KwmOtherProcessState state, out Process process)
        {
            Process currentProcess = Process.GetCurrentProcess();
            String procName = currentProcess.ProcessName;

            Process[] processes = Process.GetProcessesByName(procName);
            WindowsIdentity userWI = WindowsIdentity.GetCurrent();

            foreach (Process p in processes)
            {
                String stringSID = Base.GetProcessSid(p);

                // Ignore our current process.
                if (p.Id == currentProcess.Id)
                    continue;

                bool InOurSession = currentProcess.SessionId == p.SessionId;

                // Set the reference to the process.
                process = p;

                // This process has been started by the current user.
                if (String.Compare(stringSID, userWI.User.Value, true) == 0)
                {
                    if (InOurSession) state = KwmOtherProcessState.OurInCurrentSession;
                    else state = KwmOtherProcessState.OurInOtherSession;
                    return;
                }

                if (InOurSession)
                {
                    state = KwmOtherProcessState.NotOurInCurrentSession;
                    return;
                }
            }

            process = null;
            state = KwmOtherProcessState.None;
        }
    }

    /// <summary>
    /// This class manages the program options and usage.
    /// </summary>
    public class KwmCommandLine
    {
        /// <summary>
        /// Path to the directory where the workspaces will be exported.
        /// </summary>
        public String ExportKwsPath = "";

        /// <summary>
        /// Path to the workspace credentials file to import.
        /// </summary>
        public String ImportKwsPath = "";

        /// <summary>
        /// Parse the command line. Return false if the program must exit.
        /// </summary>
        public bool Parse(string[] args)
        {
            // True if a fatal error message must be displayed.
            bool fatalFlag = false;
            String fatalMsg = "";

            int c = 0;
            while ((c = GetOpt.GetOptions(args, "e:i:M:S:")) != -1)
            {
                switch ((char)c)
                {
                    // Export switch.
                    case 'e':
                        ExportKwsPath = GetOpt.Text;
                        if (ExportKwsPath == "" || ExportKwsPath == null) 
                            throw new Exception("empty export path");
                        break;

                    case 'i':
                        ImportKwsPath = GetOpt.Text;
                        if (ImportKwsPath == "" || ImportKwsPath == null)
                            throw new Exception("empty import path");
                        break;
                    
                    // Fatal error message switch.
                    case 'M':
                        fatalFlag = true;
                        fatalMsg = GetOpt.Text;
                        break;

                    case '?':
                    case ':':
                    default:
                        usage();
                        return false;
                }
            }

            if (fatalFlag)
            {
                Misc.KwmTellUser(fatalMsg, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Display the program usage.
        /// </summary>
        public void usage()
        {
            Console.WriteLine("Usage : \nkwm.exe [(-e]) <path-to-file>] [-M <fatal-message>]");
        }
    }
}