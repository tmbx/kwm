using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public partial class RunningOverlayControl : UserControl
    {
        /// <summary>
        /// Reference to the AppScreenSharing application.
        /// </summary>
        private AppScreenSharing m_app;

        /// <summary>
        /// Session currently being run by us in the given SS application.
        /// </summary>
        private AppSharingSession m_session;

        /// <summary>
        /// Is the next icon to display the full one?
        /// (used to create a blinking effect)
        /// </summary>
        private bool m_fullIcon = true;

        public RunningOverlayControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the control with the right objects.
        /// </summary>
        /// <param name="_app"></param>
        /// <param name="_ses"></param>
        public void InitControl(AppScreenSharing _app, AppSharingSession _ses)
        {
            Debug.Assert(_app != null);
            Debug.Assert(_ses != null);
            Debug.Assert(_ses.Status == AppSharingSession.AppSharingSessionStatus.RUNNING);

            m_app = _app;
            m_session = _ses;

            durationTimer.Enabled = true;
            iconTimer.Enabled = true;

            UpdateLabels();
            UpdateDuration();
        }

        public void DeinitControl()
        {
            durationTimer.Enabled = false;
            iconTimer.Enabled = false;
            m_app = null;
            m_session = null;
        }

        private void UpdateLabels()
        {
            lblSubject.Text = m_session.Subject;
        }
        /// <summary>
        /// Update the Duration field. Must be called by a timer at every 1 sec.
        /// </summary>
        private void UpdateDuration()
        {
            TimeSpan duration = DateTime.Now - m_session.LocalCreationTime;

            String strDuration;
            if (duration.Days == 0)
            {
                strDuration = String.Format("{0:d2}:{1:d2}:{2:d2}", duration.Hours, duration.Minutes, duration.Seconds);
            }
            else if (duration.Days == 1)
            {
                strDuration = String.Format("{0} day, {1:d2}:{2:d2}:{3:d2}", duration.Days, duration.Hours, duration.Minutes, duration.Seconds);
            }
            else
            {
                strDuration = String.Format("{0} days, {1:d2}:{2:d2}:{3:d2}", duration.Days, duration.Hours, duration.Minutes, duration.Seconds);
            }

            lblText.Text = "(" + strDuration + ")";
        }

        private void iconTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (m_fullIcon)
                    picIcon.Image = kwm.KwmAppControls.Properties.Resources.full;
                else
                    picIcon.Image = kwm.KwmAppControls.Properties.Resources.empty;

                m_fullIcon = !m_fullIcon;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                // Do not allow another click.
                this.Enabled = false;
                m_app.TerminateSession();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void durationTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                UpdateDuration();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }
    }
}
