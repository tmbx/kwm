using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using UserInactivityMonitoring;
using System.Timers;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public partial class RunningOverlay : Form
    {
        private bool m_isDragging = false;
        private Point m_mouseLocation = new Point(0, 0);

        public RunningOverlay()
        {
            InitializeComponent();
        }

        public RunningOverlay(AppScreenSharing _app, AppSharingSession _sess) : this()
        {
            Debug.Assert(_app != null);

            this.Text = "Screen Sharing in '" + _app.Helper.GetKwsName() + "'";

            overlayCtrl.InitControl(_app, _sess);
            SetLocation();
        }

        /// <summary>
        /// Set the overlay's location according to the saved informations.
        /// </summary>
        private void SetLocation()
        {
            Rectangle r = SystemInformation.VirtualScreen;
            Point loc = Misc.ApplicationSettings.ScreenSharingOverlayPos;

            bool saveFlag = false;

            // If the saved location is too much to the left or to the right,
            // move it back to the left side.
            if (loc.X < r.X || loc.X > r.Width)
            {
                saveFlag = true;
                loc.X = r.X;
            }

            // Conversely, if the saved location is too much to top 
            // or to the bottom, move it back to the top.
            if (loc.Y < r.Y || loc.Y > r.Height)
            {
                saveFlag = true;
                loc.Y = r.Y;
            }

            if (saveFlag)
            {
                Misc.ApplicationSettings.ScreenSharingOverlayPos = loc;
                Misc.ApplicationSettings.Save();
            }

            this.Location = loc;
        }

        private void RunningOverlay_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    m_isDragging = true;
                    m_mouseLocation = e.Location;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void RunningOverlay_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                    m_isDragging = false;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void RunningOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (m_isDragging)
                {
                    Point t = new Point();
                    t.X = Location.X + (e.X - m_mouseLocation.X);
                    t.Y = Location.Y + (e.Y - m_mouseLocation.Y);
                    this.Location = t;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void RunningOverlay_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Misc.ApplicationSettings.ScreenSharingOverlayPos = this.Location;
                Misc.ApplicationSettings.Save();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void RunningOverlay_LocationChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Location.Y < 10)
                    this.Location = new Point(this.Location.X, 0);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex, true);
            }
        }
    }
}