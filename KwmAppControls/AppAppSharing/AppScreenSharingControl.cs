using System;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using kwm.Utils;
using System.IO;
using Microsoft.Win32;
using System.Drawing;
using System.Collections.Generic;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public sealed partial class AppScreenSharingControl : BaseAppControl
    {
        /// <summary>
        /// Reference to the overlay form. Visible only when a session
        /// is hosted on this kwm.
        /// </summary>
        [NonSerialized]
        private RunningOverlay m_overlayForm = null;

        /// <summary>
        /// Keep a reference to the app that is currently
        /// hosting a screen sharing session.
        /// </summary>
        [NonSerialized]
        private AppScreenSharing m_appRunningASession = null;

        /// <summary>
        /// Keep track wether or not the control is enabled or not.
        /// </summary>
        [NonSerialized]
        private bool m_enabled = false;
        
        private AppScreenSharing SrcApp
        {
            get
            {
                return (AppScreenSharing)m_srcApp;
            }
        }

        public AppScreenSharingControl()
        {
            InitializeComponent();
        }

        public override UInt32 ID
        {
            get
            {
                return KAnpType.KANP_NS_VNC;
            }
        }

        protected override void UnregisterAppEventHandlers()
        {
            base.UnregisterAppEventHandlers();

            if (m_srcApp != null)
            {
                ((AppScreenSharing)m_srcApp).OnVncSessionStart -= HandleOnVncSessionStart;
                ((AppScreenSharing)m_srcApp).OnVncSessionEnd -= HandleOnVncSessionEnd;
                ((AppScreenSharing)m_srcApp).OnStateChanged -= HandleOnStateChanged;
            }
        }

        protected override void RegisterAppEventHandlers()
        {
            base.RegisterAppEventHandlers();

            if (m_srcApp != null)
            {
                ((AppScreenSharing)m_srcApp).OnVncSessionStart += HandleOnVncSessionStart;
                ((AppScreenSharing)m_srcApp).OnVncSessionEnd += HandleOnVncSessionEnd;
                ((AppScreenSharing)m_srcApp).OnStateChanged += HandleOnStateChanged;
            }
        }

        // Overriding the base app mechanism because it was
        // not designed properly and does not suit well the
        // Screen Sharing application.
        protected override void UpdateControls()
        {
            UpdateUI(GetRunLevel() == KwsRunLevel.Online && !SrcApp.Helper.IsPublicKws());
        }

        /// <summary>
        /// Update the entire UI.
        /// </summary>
        /// <param name="_enabled">True if the control should be enabled
        /// for the user, false otherwise.</param>
        private void UpdateUI(bool _enabled)
        {
            // We can't be enabled but not be bound to any application.
            Debug.Assert(!(SrcApp == null && _enabled));

            m_enabled = _enabled;

            panelRunningSessions.Controls.Clear();
            
            // Disable ourself if not bound to any Workspace.
            if (SrcApp == null)
            {
                durationTimer.Enabled = false;
                UpdateStatus();
                this.Enabled = _enabled;
                return;
            }

            boxRunning.Text = "Screen Sharing Sessions in '" + SrcApp.Helper.GetKwsName() + "': ";

            // Make sure the list is properly sorted by CreationDate, starting
            // by the oldest one.
            SrcApp.SharingSessions.Sort();

            // Get a list of LinkLabels, each representing a running
            // AppSharingSession.
            
            int i = 0;
            List<LinkLabel> sessions = new List<LinkLabel>();
            foreach (AppSharingSession ses in SrcApp.SharingSessions)
            {
                if (ses.Status == AppSharingSession.AppSharingSessionStatus.RUNNING)
                {
                    LinkLabel ll = new LinkLabel();

                    ll.Font = new Font(ll.Font.Name, ll.Font.Size, ll.Font.Style, ll.Font.Unit);

                    ll.AutoEllipsis = true;
                    ll.AutoSize = true;
                    ll.Name = ses.ID.ToString();
                    ll.Text = SrcApp.Helper.GetUserDisplayName(ses.CreatorID) + " (" + ses.Subject + ")  " + ses.DurationDisplay;
                    ll.LinkClicked += new LinkLabelLinkClickedEventHandler(linkClicked);
                    ll.Location = new Point(5, i * ll.Height + 5);
                    
                    sessions.Add(ll);

                    i++;
                }
            }

            if (sessions.Count == 0)
            {
                Label l = new Label();
                l.Text = "No Screen Sharing Session is running at this moment.";
                l.Size = new Size(275, l.Height);
                l.Font = new Font(l.Font, FontStyle.Regular);
                l.Location = new Point(10, 20);
                l.Visible = true;
                panelRunningSessions.Controls.Add(l);
            }
            else
            {
                panelRunningSessions.Controls.AddRange(sessions.ToArray());
            }

            durationTimer.Enabled = _enabled && i > 0;

            this.Enabled = _enabled;

            UpdateStatus();
            panelRunningSessions.Visible = true;
        }

        /// <summary>
        /// Refreshes the LinkLabel text to reflect the duration.
        /// </summary>
        private void RefreshUI()
        {
            foreach (Control c in panelRunningSessions.Controls)
            {
                if (c is LinkLabel)
                {
                    AppSharingSession ses = SrcApp.SharingSessions[UInt64.Parse(c.Name)];
                    LinkLabel temp = c as LinkLabel;
                    temp.Text = SrcApp.Helper.GetUserDisplayName(ses.CreatorID) + " (" + ses.Subject + ")  " + ses.DurationDisplay;
                }
            }
        }

        /// <summary>
        /// Update the UI SplitContainers.
        /// </summary>
        private void UpdateStatus()
        {
            // Hide if we are not running any session, or if we are running
            // a session in this workspace.
            if (m_appRunningASession == null ||
                SrcApp == null ||
                m_appRunningASession == SrcApp)
            {
                splitAlreadySharingWarning.Panel2Collapsed = true;
            }
            else
            {
                splitAlreadySharingWarning.Panel2Collapsed = false;
            }

            if (m_appRunningASession != null)
            {
                lblWsName.Text = m_appRunningASession.Helper.GetKwsName();
            }

            if (SrcApp != null &&
                SrcApp.CurrentState == AppSharingState.Started)
            {
                runningCtrl.Enabled = true;
                splitLiveSession.Panel1Collapsed = false;
            }
            else
            {
                splitLiveSession.Panel1Collapsed = true;
            }
        }

        /* AppSharing event handlers */
        public void HandleOnVncSessionStart(object _sender, EventArgs _args)
        {
            OnVncSessionEventArgs args = (OnVncSessionEventArgs)_args; 
            Logging.Log("HandleOnVncSessionStart : " + Environment.NewLine + args.ToString());

            if (args.Session.CreatorID == SrcApp.Helper.GetKwmUserID() && 
                SrcApp.CurrentState == AppSharingState.Started)
            {
                m_overlayForm = new RunningOverlay(m_appRunningASession, args.Session);
                m_overlayForm.Show();

                runningCtrl.InitControl(SrcApp, args.Session);
            }

            if (SrcApp.NotifiedCaughtUpFlag) UpdateUI(true);
        }

        public void HandleOnStateChanged(object _sender, EventArgs _args)
        {
            UpdateControls();
        }

        public void HandleOnVncSessionEnd(object _sender, EventArgs _args)
        {
            if (!SrcApp.NotifiedCaughtUpFlag) return;

            OnVncSessionEventArgs args = (OnVncSessionEventArgs)_args;

            Logging.Log("HandleOnVncSessionEnd : " + Environment.NewLine + args.ToString());

            UpdateUI(true);
        }

        /// <summary>
        /// Handles when the session running on this KWM ends. Used
        /// in order to update the overlay.
        /// </summary>
        public void HandleOnRunningSessionEnd(object _sender, EventArgs _args)
        {
            OnVncSessionEventArgs arg = _args as OnVncSessionEventArgs;

            // We might have already stopped and null'ed m_appRunningASession.
            if (m_appRunningASession != null &&
                arg.Session.CreatorID == m_appRunningASession.Helper.GetKwmUserID())
            {
                StopOverlay();
                UpdateUI(true);
            }
        }

        /// <summary>
        /// Called when we want to stop the running session without waiting
        /// for the network event.
        /// </summary>
        public void HandleOnLocalStop(object sender, EventArgs args)
        {
            StopOverlay();
            UpdateUI(true);
        }

        /// <summary>
        /// If we are running a session, unregister the relevant event handler
        /// and destroy the overlay.
        /// </summary>
        private void StopOverlay()
        {
            if (m_appRunningASession != null)
                m_appRunningASession = null;

            if (m_overlayForm != null)
            {
                m_overlayForm.Close();
                m_overlayForm.Dispose();
                m_overlayForm = null;
            }
                
            runningCtrl.DeinitControl();
        }
        /// <summary>
        /// Called when this KWM starts a new session.
        /// </summary>
        private void OnCreatingNewSession()
        {
            if (m_appRunningASession != null)
            {
                m_appRunningASession.OnVncSessionEnd -= HandleOnRunningSessionEnd;
            }

            m_appRunningASession = SrcApp;
            m_appRunningASession.OnVncSessionEnd += HandleOnRunningSessionEnd;
            m_appRunningASession.OnLocalStop += HandleOnLocalStop;
        }

        private AppSharingSession GetSessionFromID(UInt64 _sessionID)
        {
            return ((AppScreenSharing)m_srcApp).SharingSessions[_sessionID];
        }

        private void Terminate()
        {
            /* Wait for OnVncSessionEnd before changing our state */
            SrcApp.TerminateSession();
        }

        private void timerUpdateDuration_Tick(object sender, EventArgs e)
        {
            durationTimer.Enabled = false;
            RefreshUI();
            durationTimer.Enabled = true;
        }

        private void linkClicked(object sender, LinkLabelLinkClickedEventArgs args)
        {
            try
            {
                Debug.Assert(sender.GetType() == typeof(LinkLabel));
                LinkLabel l = (LinkLabel)sender;

                if (m_appRunningASession != null)
                {
                    Misc.KwmTellUser("You are already hosting a Screen Sharing session in \"" + m_appRunningASession.Helper.GetKwsName() + "\". You must end it before you can join another session.");
                    return;
                }

                if (SrcApp.CurrentState == AppSharingState.ConnectTicket ||
                         SrcApp.CurrentState == AppSharingState.Connected)
                {
                    Misc.KwmTellUser("You are already connected to a Screen Sharing session. You cannot join two at a time.");
                    return;
                }

                if (SrcApp.CurrentState == AppSharingState.StartTicket ||
                    SrcApp.CurrentState == AppSharingState.Started)
                {
                    Misc.KwmTellUser("You are already hosting a Screen Sharing session in this " + Base.GetKwsString() + ". You cannot connect to a session while hosting one.");
                    return;
                }

                this.Cursor = Cursors.WaitCursor;
                SrcApp.ConnectToSession(UInt64.Parse(l.Name));
                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void panelRunningSessions_Scroll(object sender, ScrollEventArgs e)
        {
            try
            {
                panelRunningSessions.Refresh();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void panelRunningSessions_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                panelRunningSessions.Focus();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                StartClicked();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Common code to execute when tue user wants to start a new SS.
        /// </summary>
        private void StartClicked()
        {
            if (SrcApp.CurrentState == AppSharingState.ConnectTicket ||
                    SrcApp.CurrentState == AppSharingState.Connected)
            {
                Misc.KwmTellUser("You cannot create a new session while you are connected to a Screen Sharing session.");
                return;
            }

            if (SrcApp.CurrentState == AppSharingState.StartTicket ||
                SrcApp.CurrentState == AppSharingState.Started)
            {
                Misc.KwmTellUser("You are already hosting a Screen Sharing session in this Workspace. You cannot host two sessions at the same time.");
                return;
            }

            if (m_appRunningASession != null)
            {
                Misc.KwmTellUser("You are already hosting a Screen Sharing session in \"" + m_appRunningASession.Helper.GetKwsName() + "\". You must end it before you can join another session.");
                return;
            }


            NewSessionWizard wiz = new NewSessionWizard();
            wiz.WizardConfig = new NewSessionWizardConfig();

            wiz.WizardConfig.CreatorName = SrcApp.Helper.GetKwmUser().UiSimpleGivenName;
            Misc.OnUiEntry();
            wiz.ShowDialog();
            Misc.OnUiExit();

            if (wiz.WizardConfig.Cancel)
                return;

            SrcApp.StartSession(wiz.WizardConfig.ShareDeskop ? "0" : wiz.WizardConfig.SharedWindowHandle,
                                wiz.WizardConfig.SessionSubject,
                                wiz.WizardConfig.SupportMode);

            OnCreatingNewSession();
        }

        private void lblWsName_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Debug.Assert(m_appRunningASession != null);

                m_appRunningASession.Helper.ActivateKws();

            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void splitAlreadySharingWarning_Panel1_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                int x = splitAlreadySharingWarning.Panel1.Width;

                // Size from the top of the panel to the bottom of the help label.
                int headerSize = lblHelp.Location.Y;

                // Size from the bottom of boxRunning to the botton of the panel.
                int panelFreeSize = splitAlreadySharingWarning.Panel1.Height - headerSize;

                // Space between the start button and its label.
                int lblSpacer = 15;

                // Height of the "group" made of btnStart and lblStart + a spacer.
                int startHeight = btnStart.Height + lblSpacer + lblStart.Height;

                btnStart.Location = new Point((x - btnStart.Width) / 2, Math.Max(headerSize + (panelFreeSize / 2) - (startHeight / 2), boxRunning.Location.Y + boxRunning.Height + lblSpacer));
                lblStart.Location = new Point((x - lblStart.Width) / 2, btnStart.Location.Y + btnStart.Height + lblSpacer);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lblStart_Click(object sender, EventArgs e)
        {
            try
            {
                StartClicked();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }

    
    [Serializable]
    public class AppSharingSessionList : ArrayList
    {
        // Simple accessor by sessionID
        public AppSharingSession this[UInt64 _sessionID]
        {
            get
            {
                foreach (AppSharingSession w in this)
                {
                    if (w.ID == _sessionID)
                    {
                        return w;
                    }
                }
                throw new ArgumentOutOfRangeException("_sessionID", "The Screen Sharing Session ID " + _sessionID + " does not exist in the list.");
            }
        }
    }

    [Serializable]
    public class AppSharingSession : IComparable
    {
        private UInt64 m_sessionID;
        private String m_strSubject;
        private UInt64 m_creationTime;
        private UInt64 m_endTime;
        private UInt32 m_creatorID;
        private String m_creatorDisplayName;

        /* This is used to compensate the offset between 
         * the local and server's clock */
        private DateTime m_localCreationTime;

        public String Subject
        {
            get
            {
                return m_strSubject;
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return Base.KDateToDateTime(m_creationTime);
            }
        }

        public DateTime LocalCreationTime
        {
            get
            {
                return m_localCreationTime;
            }
        }

        public DateTime EndTime
        {
            get
            {
                return Base.KDateToDateTime(m_endTime);
            }
        }

        public UInt64 EndTimeUInt64
        {
            set
            {
                m_endTime = value;
            }
        }

        public UInt32 CreatorID
        {
            get
            {
                return m_creatorID;
            }
        }

        public String CreatorDisplayName
        {
            get
            {
                return m_creatorDisplayName;
            }
        }

        public UInt64 ID
        {
            get
            {
                return m_sessionID;
            }
        }

        public AppSharingSessionStatus Status
        {
            get
            {
                if (m_endTime == 0)
                {
                    return AppSharingSessionStatus.RUNNING;
                }
                else
                {
                    return AppSharingSessionStatus.ENDED;
                }
            }
        }

        /// <summary>
        /// Get the time interval between Now and the session's creation date.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                TimeSpan duration;
                if (Status == AppSharingSession.AppSharingSessionStatus.RUNNING)
                {
                    duration = DateTime.Now - LocalCreationTime;

                    if (duration < TimeSpan.Zero)
                        duration = TimeSpan.Zero;
                }
                else
                {
                    duration = EndTime - CreationTime;
                }

                return duration;
            }
        }

        /// <summary>
        /// Get a human-readable representation of this session's duration.
        /// </summary>
        public String DurationDisplay
        {
            get
            {
                String strDuration;
                TimeSpan dur = Duration;

                if (dur.Days == 0)
                    strDuration = String.Format("{0:d2}:{1:d2}:{2:d2}", dur.Hours, dur.Minutes, dur.Seconds);
                else if (dur.Days == 1)
                    strDuration = String.Format("{0} day, {1:d2}:{2:d2}:{3:d2}", dur.Days, dur.Hours, dur.Minutes, dur.Seconds);
                else
                    strDuration = String.Format("{0} days, {1:d2}:{2:d2}:{3:d2}", dur.Days, dur.Hours, dur.Minutes, dur.Seconds);

                return strDuration;
            }
        }
        public AppSharingSession(UInt64 _sessionID, UInt32 _creatorID, String _creatorDisplayName, String _subject, UInt64 _creationTime, UInt64 _endTime)
        {
            m_sessionID = _sessionID;
            m_creatorID = _creatorID;
            m_creatorDisplayName = _creatorDisplayName;
            m_strSubject = _subject;
            m_creationTime = _creationTime;
            m_endTime = _endTime;
            m_localCreationTime = DateTime.Now;
        }

        public override string ToString()
        {
            return m_creatorDisplayName + " (" + m_strSubject + ")";
        }

        public enum AppSharingSessionStatus
        {
            [Base.Description("Live")]
            RUNNING,
            [Base.Description("Ended")]
            ENDED
        }
        
        /// <summary>
        /// Compare the CreationTime attributes. A CreationTime is
        /// less than another CreationTime if it is closer to Now. This
        /// is the opposite behavior of the stock DateTime.CompareTo() method.
        /// </summary>
        public int CompareTo(object obj)
        {
            if (obj is AppSharingSession)
            {
                AppSharingSession t = obj as AppSharingSession;
                return (-1 * this.CreationTime.CompareTo(t.CreationTime));
            }
            else
            {
                throw new ArgumentException("object is not an AppSharingSession");
            }
        }
    }

    public class OnVncSessionEventArgs : EventArgs
    {
        private AppSharingSession m_session;

        public AppSharingSession Session
        {
            get
            {
                return m_session;
            }
        }

        public OnVncSessionEventArgs(AppSharingSession _session)
        {
            m_session = _session;
        }

        public override string ToString()
        {
            return m_session.ToString();
        }
    }
}
