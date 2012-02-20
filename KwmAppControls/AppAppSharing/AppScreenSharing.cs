using System;
using System.Collections.Generic;
using System.Text;
using kwm.Utils;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using UserInactivityMonitoring;
using System.Timers;
using System.Runtime.Serialization;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public enum AppSharingState
    {
        /// <summary>
        /// Inactive.
        /// </summary>
        Idle,

        /// <summary>
        /// Waiting for a Connect ticket.
        /// </summary>
        ConnectTicket,

        /// <summary>
        /// Connected to a session.
        /// </summary>
        Connected,

        /// <summary>
        /// Waiting for a Start ticket.
        /// </summary>
        StartTicket,

        /// <summary>
        /// Started session running.
        /// </summary>
        Started
    }

    /// <summary>
    /// Backend class for the ScreenSharing application.
    /// </summary>
    [Serializable]
    public sealed class AppScreenSharing : KwsApp
    {
        /// <summary>
        /// Fired when a Screen Sharing session has been started.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnVncSessionStart;


        /// <summary>
        /// Fired when a Screen Sharing session has stopped.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnVncSessionEnd;

        /// <summary>
        /// Fired when we want to stop the application without waiting
        /// for a network event.
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnLocalStop;

        /// <summary>
        /// Notify the app control when our AppSharingState changes
        /// </summary>
        [field: NonSerialized]
        public event EventHandler<EventArgs> OnStateChanged;

        /// <summary>
        /// List of all the sessions ever received.
        /// </summary>
        private AppSharingSessionList m_lstSharingSessions;

        /* Keep handle on VNC processes */
        [NonSerialized]
        RawProcess m_serverProcess = null;
        
        [NonSerialized]
        RawProcess m_dummyServerProcess = null;
        
        [NonSerialized]
        RawProcess m_viewerProcess = null;
        
        [NonSerialized]
        private IInactivityMonitor inactivityMonitor;

        /// <summary>
        /// Registry location of the VNC listening port (server's or 
        /// client's, depending on the situation).
        /// </summary>
        [NonSerialized]
        private const String m_portRegItem = "ListeningPortForVNC";

        /// <summary>
        /// Registry location of the flag set when m_portRegItem
        /// is completly written.
        /// </summary>
        [NonSerialized]
        private const String m_portRegItemWritten = "ListeningPortForVNCWritten";

        /// <summary>
        /// Tunnel used to connect the client or the server.
        /// </summary>
        [NonSerialized]
        IAnpTunnel m_tunnel = null;

        /// <summary>
        /// Result of the KANP_CMD_VNC_START_TICKET or 
        /// KANP_CMD_VNC_CONNECT_TICKET. It is OK to have only 
        /// one, since we can only connect or serve one session at
        /// a time.
        /// </summary>
        [NonSerialized]
        private byte[] m_ticket;

        /// <summary>
        /// Important : do not affect this variable directly. Use the 
        /// CurrentState setter since it notifies the app control of
        /// a change automatically. 
        /// </summary>
        [NonSerialized]
        private AppSharingState m_state = AppSharingState.Idle;

        /// <summary>
        /// Keep track of which session ID we are currently joined to.
        /// </summary>
        [NonSerialized]
        private UInt64 m_connectedSessionID = 0;

        /// <summary>
        /// ID of the screen sharing session we created.
        /// </summary>
        [NonSerialized]
        private UInt64 m_createdSessionID = 0;

        public override UInt32 AppID { get { return KAnpType.KANP_NS_VNC; } }

        public AppSharingSessionList SharingSessions
        {
            get
            {
                return m_lstSharingSessions;
            }
        }

        public AppSharingState CurrentState
        {
            get
            {
                return m_state;
            }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    DoOnStateChanged();
                }
            }
        }

        public UInt64 CurrentSessionID
        {
            get
            {
                return m_connectedSessionID;
            }
        }

        public AppScreenSharing(IAppHelper _helper)
            : base(_helper)
        {
            m_lstSharingSessions = new AppSharingSessionList();
        }

        ~AppScreenSharing()
        {
            KillProcesses();
        }

        public override void ProcessCommand(AnpMsg msg)
        {
            switch (msg.Type)
            {
                case OAnpType.OANP_CMD_START_SCREEN_SHARE:
                    OAnpType.OanpScreenShareFlags flags = (OAnpType.OanpScreenShareFlags) msg.Elements[1].UInt32;
                    StartSession("0", "On the fly screen share", (flags & OAnpType.OanpScreenShareFlags.GiveControl) != 0);
                    break;
                default:
                    base.ProcessCommand(msg);
                    break;
            }
        }

        public override KwsAnpEventStatus HandleAnpEvent(AnpMsg msg)
        {
            switch (msg.Type)
            {
                case KAnpType.KANP_EVT_VNC_START:
                    AppSharingSession newSession = new AppSharingSession(
                        msg.Elements[3].UInt64,
                        msg.Elements[2].UInt32,
                        Helper.GetUserDisplayName(msg.Elements[2].UInt32),
                        msg.Elements[4].String,
                        msg.Elements[1].UInt64,
                        0);
                    m_lstSharingSessions.Add(newSession);

                    // Notify if this session comes from a different user than our id.
                    if (m_createdSessionID != newSession.ID)
                        Helper.NotifyUser(new ScreenSharingNotificationItem(msg, Helper));
                    
                    DoOnVncSessionStart(new OnVncSessionEventArgs(newSession));
                    return KwsAnpEventStatus.Processed;

                case KAnpType.KANP_EVT_VNC_END:
                    AppSharingSession ses = m_lstSharingSessions[msg.Elements[3].UInt64];
                    ses.EndTimeUInt64 = msg.Elements[1].UInt64;

                    AppSharingState currentState = m_state;

                    bool notify = true;

                    // If we were connected to this session, disconnect from it and notify
                    // our user.
                    if (m_state == AppSharingState.Connected && m_connectedSessionID == ses.ID)
                    {
                        notify = false;
                        ResetToIdle();
                        Misc.KwmTellUser("This Screen Sharing session has been closed.", MessageBoxIcon.Exclamation);
                    }

                    // If we started this session, close it and reset our state.
                    // Do not tell our user since in most cases the session was 
                    // deliberatly ended by him.
                    if (m_createdSessionID == ses.ID)
                    {
                        m_createdSessionID = 0;

                        notify = false;
                        if (inactivityMonitor != null)
                            inactivityMonitor.Enabled = false;

                        ResetToIdle();
                    }

                    // Notify if we were not connected to the session and we did not started it.
                    if (notify)
                        Helper.NotifyUser(new ScreenSharingNotificationItem(msg, Helper));

                    DoOnVncSessionEnd(new OnVncSessionEventArgs(ses));
                    return KwsAnpEventStatus.Processed;
            }

            return KwsAnpEventStatus.Unprocessed;
        }

        /// <summary>
        /// Handles when a Start Ticket is received.
        /// </summary>
        /// <param name="_context"></param>
        private void OnStartTicketResponse(KasQuery ctx)
        {
            Debug.Assert(m_state == AppSharingState.StartTicket);

            AnpMsg cmd = ctx.Cmd;
            AnpMsg res = ctx.Res;

            if (m_state != AppSharingState.StartTicket)
            {
                Logging.Log(2, "Received spurious Screen Sharing Start ticket.");
                return;
            }

            if (res.Type == KAnpType.KANP_RES_VNC_START_TICKET)
            {
                m_ticket = res.Elements[0].Bin;

                /* Start MetaVNC (specify the desired window's handle)*/
                int handle = Int32.Parse((String)ctx.MetaData[0]);
                bool supportSession = (bool)ctx.MetaData[2];

                String vncServerPath = "\"" + Base.GetKcsInstallationPath() + @"vnc\kappserver.exe" + "\"";
                String args = (handle == 0) ? " -shareall" : " -sharehwnd " + (String)ctx.MetaData[0];

                // If a window is being shared (not the desktop), set 
                // it visible and in foreground.
                if (handle != 0)
                {
                    IntPtr hWnd = new IntPtr(handle);

                    if (Syscalls.IsIconic(hWnd))
                        Syscalls.ShowWindowAsync(hWnd, (int)Syscalls.WindowStatus.SW_RESTORE);

                    Base.KSetForegroundWindow(hWnd);
                }

                /* Remove any indication of previous server's listening port */
                RegistryKey port = Registry.CurrentUser.OpenSubKey(Base.GetKwmRegKey(), true);
                if (port == null)
                {
                    ResetToIdle();
                    Misc.KwmTellUser("could not delete old server's port info.");
                    return;
                }

                port.DeleteValue(m_portRegItem, false);
                port.DeleteValue(m_portRegItemWritten, false);
                port.Close();

                // Set registry options in order to allow/deny
                // remote control.
                SetSupportSessionMode(supportSession);

                Logging.Log("Starting the first instance of MetaVNC (no args).");

                m_serverProcess = new RawProcess(vncServerPath);
                m_serverProcess.CreationFlags = (uint)Syscalls.CREATION_FLAGS.CREATE_NO_WINDOW;
                m_serverProcess.InheritHandles = false;
                Syscalls.STARTUPINFO si = new Syscalls.STARTUPINFO();
                si.dwFlags = 1;
                m_serverProcess.StartupInfo = si;

                m_serverProcess.ProcessEnd += HandleOnProcessEnd;

                m_serverProcess.Start();

                Logging.Log("Started.");

                /* Wait until we can get our hands on the process's window handle */
                int retries = 15;
                bool found = false;
                while (retries > 0)
                {
                    Logging.Log("Trying to start WinVNC ( " + retries + ")");

                    IntPtr serverHandle = Syscalls.FindWindow("WinVNC Tray Icon", 0);
                    if (serverHandle != IntPtr.Zero)
                    {
                        found = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(250);
                    retries--;
                }

                if (!found)
                    throw new Exception("Screen sharing error: server could not be started.");

                /* Start another process of metaVNC.
                 * This is needed to pass the window handle
                 * we want, it was easier to do this that way
                 * than to pass it directly on the cmd line when 
                 * starting the process the first time. */
                Logging.Log("Starting MetaVNC for the second time (this is normal): " + vncServerPath + args);
                m_dummyServerProcess = new RawProcess(vncServerPath + args);
                m_dummyServerProcess.CreationFlags = (uint)Syscalls.CREATION_FLAGS.CREATE_NO_WINDOW;
                m_dummyServerProcess.InheritHandles = false;
                m_dummyServerProcess.StartupInfo = si;
                m_dummyServerProcess.ProcessEnd += HandleOnProcessEnd;

                m_dummyServerProcess.Start();

                /* Find out on which port we need to tell
                 * ktlstunnel to connect to the server */
                RegistryKey kwmKey = Registry.CurrentUser.OpenSubKey(Base.GetKwmRegKey());
                if (kwmKey == null)
                {
                    ResetToIdle();
                    Misc.KwmTellUser("Screen sharing error: could not read server's listening port: HKCU\\" + Base.GetKwmRegKey() + "\\" + m_portRegItem);
                    return;
                }

                object readPort = null;
                object portOK = null;

                // Actively poll the synchronization key for 6 seconds.
                retries = 30;
                found = false;

                while (retries > 0)
                {
                    portOK = kwmKey.GetValue(m_portRegItemWritten);

                    if (portOK != null)
                    {
                        found = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(200);
                    retries--;
                }

                if (!found)
                {
                    if (kwmKey != null)
                        kwmKey.Close();

                    ResetToIdle();
                    Misc.KwmTellUser("Screen sharing error: a timeout occured while waiting for the server's listening port.");
                    return;
                }

                // At this stage we can presume the port was
                // correctly really written by the VNC server.
                readPort = kwmKey.GetValue(m_portRegItem);
                kwmKey.Close();

                if (port == null)
                {
                    ResetToIdle();
                    Misc.KwmTellUser("Screen sharing error: unable to read the server's listening port.");
                    return;
                }

                Logging.Log("Screen Sharing server's port: " + (int)readPort);

                /* Connect a new tunnel */
                m_tunnel = Helper.CreateTunnel();
                m_tunnel.Connect("localhost", (int)readPort);

                /* Negociate role */
                AnpMsg inMsg = Helper.NewKAnpMsg(KAnpType.KANP_CMD_MGT_SELECT_ROLE);
               
                inMsg.AddUInt32(KAnpType.KANP_KCD_ROLE_APP_SHARE);
                m_tunnel.SendMsg(inMsg);
                AnpMsg outMsg = m_tunnel.GetMsg();

                if (outMsg.Type == KAnpType.KANP_RES_FAIL)
                {
                    ResetToIdle();
                    Misc.KwmTellUser(outMsg.Elements[1].String);
                    return;
                }

                Debug.Assert(outMsg.Type == KAnpType.KANP_RES_OK);

                AnpMsg startSession = Helper.NewKAnpMsg(KAnpType.KANP_CMD_VNC_START_SESSION);
                startSession.AddBin(m_ticket);
                startSession.AddString((String)ctx.MetaData[1]);

                m_tunnel.SendMsg(startSession);
                outMsg = m_tunnel.GetMsg();
                if (outMsg.Type == KAnpType.KANP_RES_FAIL)
                {
                    ResetToIdle();
                    Misc.KwmTellUser(outMsg.Elements[1].String);
                    return;
                }
                Debug.Assert(outMsg.Type == KAnpType.KANP_RES_VNC_START_SESSION);

                m_createdSessionID = outMsg.Elements[0].UInt64;

                /* Disconnect tunnel so it can connect to metaVNC server */
                Logging.Log("About to call disconnect on ktlstunnel.");
                m_tunnel.Disconnect();
                CurrentState = AppSharingState.Started;
                m_connectedSessionID = 0;

                StopInactivityMonitor();

                // 10 minutes
                inactivityMonitor = MonitorCreator.CreateInstance(MonitorType.GlobalHookMonitor);
                inactivityMonitor.Interval = 600000;
                inactivityMonitor.Elapsed += new ElapsedEventHandler(HandleSessionTimeout);
                inactivityMonitor.Enabled = true;
            }
            else if (res.Type == KAnpType.KANP_RES_FAIL)
            {
                ResetToIdle();
                Misc.KwmTellUser(res.Elements[1].String);
            }
            else
            {
                Logging.Log(2, "unexpected response in OnStartTicketResponse");
            }
        }

        private void OnConnectTicketResponse(KasQuery ctx)
        {
            Debug.Assert(m_state == AppSharingState.ConnectTicket);

            AnpMsg cmd = ctx.Cmd;
            AnpMsg res = ctx.Res;

            if (m_state != AppSharingState.ConnectTicket)
            {
                Logging.Log(2, "Received spurious Screen Sharing Connect ticket.");
                return;
            }

            if (res.Type == KAnpType.KANP_RES_VNC_CONNECT_TICKET)
            {
                m_ticket = res.Elements[0].Bin;

                /* Remove any indication of previous
                 * client's listening port */

                RegistryKey port = Registry.CurrentUser.OpenSubKey(Base.GetKwmRegKey(), true);
                if (port == null)
                {
                    ResetToIdle();
                    Misc.KwmTellUser("could not delete old client's port info.");
                    return;
                }

                port.DeleteValue(m_portRegItem, false);
                port.DeleteValue(m_portRegItemWritten, false);
                port.Close();

                /* Start client. Force one parameter so that it does not 
                 * prompt for connection parameters */
                String vncClientPath = "\"" + Base.GetKcsInstallationPath() + @"vnc\kappviewer.exe" + "\"";
                String args = " /shared /notoolbar /disableclipboard /encoding tight /compresslevel 9 localhost";

                m_viewerProcess = new RawProcess(vncClientPath + args);
                m_viewerProcess.InheritHandles = false;
                m_viewerProcess.ProcessEnd += HandleOnProcessEnd;
                m_viewerProcess.Start();

                RegistryKey kwmKey = Registry.CurrentUser.OpenSubKey(Base.GetKwmRegKey());
                if (kwmKey == null)
                {
                    ResetToIdle();
                    Misc.KwmTellUser("could not read the client's listening port.");
                    return;
                }
                
                object _port = null;
                object _portOK = null;

                // Actively poll the synchronization 
                // key for 6 secs 
                int retries = 30;
                bool found = false;

                while (retries > 0)
                {
                    _portOK = kwmKey.GetValue(m_portRegItemWritten);

                    if (_portOK != null)
                    {
                        found = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(200);
                    retries--;
                }

                if (!found)
                {
                    if (kwmKey != null)
                        kwmKey.Close();

                    ResetToIdle();
                    Misc.KwmTellUser("a timeout occured while waiting for the client's listening port.");
                    return;
                }

                // At this stage we can presume the port was
                // correctly really written by the VNC server.
                _port = kwmKey.GetValue(m_portRegItem);
                kwmKey.Close();

                if (_port == null)
                {
                    ResetToIdle();
                    Misc.KwmTellUser("could not read client's listening port.");
                    return;
                }

                Logging.Log("Read VNC client's port: " + (int)_port);

                /* Connect a new tunnel */
                IAnpTunnel tunnel = Helper.CreateTunnel();
                tunnel.Connect("localhost", (int)_port);

                /* Negociate role */
                AnpMsg inMsg = Helper.NewKAnpMsg(KAnpType.KANP_CMD_MGT_SELECT_ROLE);
                inMsg.AddUInt32(KAnpType.KANP_KCD_ROLE_APP_SHARE);
                tunnel.SendMsg(inMsg);
                AnpMsg outMsg = tunnel.GetMsg();

                if (outMsg.Type == KAnpType.KANP_RES_FAIL)
                {
                    ResetToIdle();
                    Misc.KwmTellUser(outMsg.Elements[1].String);
                    return;
                }

                Debug.Assert(outMsg.Type == KAnpType.KANP_RES_OK);

                AnpMsg startSession = Helper.NewKAnpMsg(KAnpType.KANP_CMD_VNC_CONNECT_SESSION);
                startSession.AddBin(m_ticket);
                tunnel.SendMsg(startSession);
                outMsg = tunnel.GetMsg();

                if (outMsg.Type == KAnpType.KANP_RES_FAIL)
                {
                    ResetToIdle();
                    Misc.KwmTellUser(outMsg.Elements[1].String);
                    return;
                }

                Debug.Assert(outMsg.Type == KAnpType.KANP_RES_OK);

                Logging.Log("About to call disconnect");
                tunnel.Disconnect();
                CurrentState = AppSharingState.Connected;
            }
            else if (res.Type == KAnpType.KANP_RES_FAIL)
            {
                ResetToIdle();
                Misc.KwmTellUser(res.Elements[1].String);
            }
            else
            {
                Logging.Log("unexpected response in OnConnectTicketResponse");
            }
        }

        public override void PrepareForRebuild(KwsRebuildInfo rebuildInfo)
        {
            // Make sure we are idle before rebuilding the workspace.
            ResetToIdle();
            m_lstSharingSessions.Clear();

            base.PrepareForRebuild(rebuildInfo);
        }

        public override void RequestStop()
        {
            ResetToIdle();
            base.RequestStop();
        }

        /// <summary>
        /// Connect to the given session.
        /// </summary>
        public void ConnectToSession(UInt64 _sessionID)
        {
            Debug.Assert(m_state == AppSharingState.Idle);
            CurrentState = AppSharingState.ConnectTicket;
            m_connectedSessionID = _sessionID;

            /* Ask for a ticket */
            AnpMsg request = Helper.NewKAnpCmd(KAnpType.KANP_CMD_VNC_CONNECT_TICKET);
            request.AddUInt64(_sessionID);
            PostKasQuery(request, null, OnConnectTicketResponse);
        }

        /// <summary>
        /// Creates a new app sharing session.
        /// </summary>
        /// <param name="_windowHandle">Handle of the window we want to share</param>
        /// <param name="_subject">Session subject</param>
        /// <param name="_supportMode">Allow remote users to control the session (keyboard+mouse events)</param>
        public void StartSession(String _windowHandle, String _subject, bool _supportMode)
        {
            Debug.Assert(m_state == AppSharingState.Idle);

            CurrentState = AppSharingState.StartTicket;

            AnpMsg request = Helper.NewKAnpCmd(KAnpType.KANP_CMD_VNC_START_TICKET);
            PostKasQuery(request, new Object[] { _windowHandle, _subject, _supportMode }, OnStartTicketResponse);
        }

        /// <summary>
        /// Called when the user has been idle after X seconds.
        /// </summary>
        private void HandleSessionTimeout(object sender, EventArgs e)
        {
            try
            {
                if (Misc.MainForm.InvokeRequired)
                {
                    Logging.Log("Invoking in HandleSessionTimeout");
                    Misc.MainForm.BeginInvoke(new EventHandler(HandleSessionTimeout), new object[] { sender, e });
                    return;
                }

                if (CurrentState == AppSharingState.Started)
                {
                    this.TerminateSession();
                    Misc.KwmTellUser("Your Screen Sharing session has been terminated because it has been idle for too long.");
                }
                else
                {
                    Logging.Log(1, "Received spurious idle notification.");
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
            finally
            {
                if (inactivityMonitor != null)
                    inactivityMonitor.Enabled = false;
            }
        }

        /// <summary>
        /// Leave the session currently connected to.
        /// </summary>
        public void LeaveSession()
        {
            Debug.Assert(m_state == AppSharingState.Connected);
            ResetToIdle();
        }

        /// <summary>
        /// Terminate the started session.
        /// </summary>
        public void TerminateSession()
        {
            Debug.Assert(m_state == AppSharingState.Started);
            ResetToIdle();
        }

        /// <summary>
        /// Sets kappserver to allow or deny remote inputs.
        /// </summary>
        /// <param name="_support">True to enable support mode (remote inputs)</param>
        private void SetSupportSessionMode(bool _support)
        {
            // Create key if it does not exist
            RegistryKey remoteInputs = null;
            try
            {
                remoteInputs = Registry.CurrentUser.OpenSubKey(Misc.GetVncServerRegKey() + "\\server", true);
                if (remoteInputs == null)
                {
                    Registry.CurrentUser.CreateSubKey(Misc.GetVncServerRegKey());
                    Registry.CurrentUser.CreateSubKey(Misc.GetVncServerRegKey() + "\\server");
                }

                // Try again.
                remoteInputs = Registry.CurrentUser.OpenSubKey(Misc.GetVncServerRegKey() + "\\server", true);
                if (remoteInputs == null)
                    throw new Exception("Unable to set the Support Mode option.");

                remoteInputs.SetValue("InputsEnabled", _support ? 1 : 0, RegistryValueKind.DWord);
            }
            finally
            {
                if (remoteInputs != null)
                    remoteInputs.Close();
            }
        }

        private void DoOnVncSessionStart(OnVncSessionEventArgs _args)
        {
            if (OnVncSessionStart != null)
                OnVncSessionStart(this, _args);
        }

        private void DoOnVncSessionEnd(OnVncSessionEventArgs _args)
        {
            if (OnVncSessionEnd != null)
                OnVncSessionEnd(this, _args);
        }

        private void DoOnLocalStop()
        {
            if (OnLocalStop != null)
                OnLocalStop(this, null);
        }

        private void DoOnStateChanged()
        {
            if (OnStateChanged != null)
                OnStateChanged(this, new EventArgs());
        }

        /// <summary>
        /// Callback when one of our child processes (VNC client or server) die.
        /// </summary>
        private void HandleOnProcessEnd(object _sender, EventArgs _args)
        {
            if (Misc.MainForm.InvokeRequired)
            {
                Misc.MainForm.BeginInvoke(new EventHandler(HandleOnProcessEnd), new object[] { _sender, _args });
                return;
            }

            RawProcess.ProcEndEventArgs args = (RawProcess.ProcEndEventArgs)_args;
            RawProcess process = (RawProcess)_sender;
            if (process == m_dummyServerProcess)
            {
                Logging.Log("m_dummyServerProcess exited: " + args.ExitCode);
                m_dummyServerProcess = null;
            }
            else if (process == m_serverProcess)
            {
                Logging.Log("m_serverProcess exited");
                // Do not change our CurrentState here: we will catch the
                // SessionEnd event sometime.
                m_serverProcess = null;
            }
            else if (process == m_viewerProcess)
            {
                Logging.Log("m_viewerProcess exited");
                CurrentState = AppSharingState.Idle;
                m_viewerProcess = null;
            }
            else
            {
                Logging.Log("Unknown process exited! (" + process.CommandLine + ")");
            }
        }

        /// <summary>
        /// Set state to idle and kill all running child processes.
        /// </summary>
        private void ResetToIdle()
        {
            CurrentState = AppSharingState.Idle;

            m_connectedSessionID = 0;

            StopInactivityMonitor();

            KillProcesses();

            DoOnLocalStop();
        }

        private void KillProcesses()
        {
            if (m_dummyServerProcess != null)
                m_dummyServerProcess.Terminate();

            if (m_serverProcess != null)
                m_serverProcess.Terminate();

            if (m_viewerProcess != null)
                m_viewerProcess.Terminate();

            if (m_tunnel != null)
                m_tunnel.Terminate();
        }

        private void StopInactivityMonitor()
        {
            if (inactivityMonitor != null)
            {
                inactivityMonitor.Enabled = false;
                inactivityMonitor.Dispose();
                inactivityMonitor = null;
            }
        }
    }
}
