using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
	public partial class TrayMessage
	{
        /// <summary>
        /// Minimal interval in seconds between two notifications.
        /// </summary>
        private const UInt32 MinNotifInterval = 10;

        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        private WorkspaceManager m_wm;

        /// <summary>
        /// Distance between the top of the screen and the top of the item when
        /// the item is at the bottom.
        /// </summary>
		private int m_upTop;

        /// <summary>
        /// Height of the screen.
        /// </summary>
		private int m_downTop;

        /// <summary>
        /// Horizontal position of the item.
        /// </summary>
		private int iLeft;
         
        /// <summary>
        /// True if the item is raising.
        /// </summary>
		private bool m_isShowing;

        /// <summary>
        /// True if the item is lowering.
        /// </summary>
		private bool m_isHiding;

        /// <summary>
        /// Height delta when the position of the item changes.
        /// </summary>
		private int m_heightDelta = 5;

        /// <summary>
        /// Notification item currently being displayed.
        /// </summary>
        private NotificationItem m_item = null;

        /// <summary>
        /// Date at which we last displayed a notification item.
        /// </summary>
        private DateTime m_lastNotificationDate = DateTime.MinValue;

        /// <summary>
        /// Pixel increment to use when animating a show or hide operation.
        /// </summary>
		public int HeightDelta
		{
		   get { return m_heightDelta; }
		   set { m_heightDelta = value; }
		}

        public NotificationItem CurrentItem
        {
            get { return m_item; }
        }

        /// <summary>
        /// For designer.
        /// </summary>
        public TrayMessage()
        {
            InitializeComponent();
        }

		public TrayMessage(WorkspaceManager wm)
		{
            m_wm = wm;
			InitializeComponent();
            InitShowProperties();
		}

        /// <summary>
        /// Initialize the form's location.
        /// </summary>
        private void InitShowProperties()
        {
            m_upTop = SystemInformation.WorkingArea.Height - this.Height;
            m_downTop = SystemInformation.WorkingArea.Height;
            iLeft = SystemInformation.WorkingArea.Width - this.Width;
            this.Top = m_downTop;
            this.Left = iLeft;
            m_isShowing = false;
            m_isHiding = false;
            this.TopMost = true;
            this.Visible = false;
            this.Refresh();
        }

        /// <summary>
        /// Show the tray message.
        /// </summary>
        /// <param name="item">What to display.</param>
        /// <param name="detail">Desired notification detail.</param>
        public void ShowMessage(NotificationItem item)
        {
            DateTime now = DateTime.Now;

            // If not enough time has passed since the last notification or we
            // haven't caught up or an item is being displayed, bail out
            if (m_lastNotificationDate.AddSeconds(MinNotifInterval) >= now ||
                !m_wm.GetKwsByInternalID(item.InternalWsID).KAnpState.CaughtUpFlag ||
                this.Visible)
            {
                return;
            }

            // Set the current item.
            LinkItem(item);

            // Set the last notification date.
            m_lastNotificationDate = now;

            lblTitle.Text = item.WorkspaceName;

            rtbNotification.Text = "";
            rtbNotification.Rtf = null;
            if (item.HasRtfText) rtbNotification.Rtf = item.EventRtf;
            else rtbNotification.Text = item.EventText;

            this.Top = m_downTop;
            this.Left = iLeft;

            // Make sure the modifications are applied. This is necessary
            // because under a certain amount of load, the old strings
            // are showed for half a second before being replaced by the real
            // content.
            this.Invalidate();

            timerAnimShow.Tick += new EventHandler(ShowMe);
            timerAnimShow.Interval = 10;
            timerAnimShow.Enabled = true;
            timerAnimShow.Start();

            timerHideDelay.Tick += new EventHandler(HideMessage);
            timerHideDelay.Interval = Misc.ApplicationSettings.NotificationDelay;
            timerHideDelay.Enabled = true;
            timerHideDelay.Start();

            Base.ShowInactiveTopmost(this);
            m_isShowing = true;
        }
        
        /// <summary>
        /// Store a reference to the item specified.
        /// </summary>
        private void LinkItem(NotificationItem item)
        {
            UnlinkItem();
            m_item = item;
            m_wm.GetKwsByInternalID(item.InternalWsID).AddRef();
        }

        /// <summary>
        /// Unlink the current item, if any.
        /// </summary>
        private void UnlinkItem()
        {
            if (m_item == null) return;
            m_wm.GetKwsByInternalID(m_item.InternalWsID).ReleaseRef();
            m_item = null;
        }

        /// <summary>
        /// Called when it's time to hide the message.
        /// </summary>
        private void HideMessage(object sender, EventArgs e)
        {
            try
            {
                timerHideDelay.Enabled = false;
                if (this.Visible)
                {
                    timerAnimHide.Tick += new EventHandler(HideMe);
                    timerAnimHide.Interval = 10;
                    timerAnimHide.Enabled = true;
                    timerAnimHide.Start();
                    m_isHiding = true;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Internal helper method to show the form. Called
        /// by timerAnimShow.
        /// </summary>
        private void ShowMe(object sender, EventArgs e)
		{
            try
            {
                if (m_isShowing)
                {
                    timerAnimShow.Enabled = false;

                    if (this.Top - m_heightDelta > m_upTop)
                    {
                        this.Top = this.Top - m_heightDelta;
                        this.Refresh();
                    }
                    else
                    {
                        this.Top = m_upTop;
                        this.Left = iLeft;
                        timerAnimShow.Enabled = false;
                        timerAnimShow.Stop();
                        m_isShowing = false;
                        return;
                    }
                    timerAnimShow.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
		}
		
        /// <summary>
        /// Internal helper method to hide the form. Called
        /// by timerAnimHide.
        /// </summary>
		private void HideMe(object sender, EventArgs e)
		{
            try
            {
                if (m_isHiding)
                {
                    timerAnimHide.Enabled = false;

                    if (this.Top + m_heightDelta < m_downTop)
                    {
                        this.Top = this.Top + m_heightDelta;
                        this.Refresh();
                    }
                    else
                    {
                        this.Top = m_downTop;
                        timerAnimHide.Enabled = false;
                        timerAnimHide.Stop();
                        this.Visible = false;
                        m_isHiding = false;
                        return;
                    }
                    timerAnimHide.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
		}

        /// <summary>
        /// Handle when the user clicks on any label. Triggers the Click
        /// custom event so that the WorkspaceManager gets notified.
        /// </summary>
        private void HandleClick()
        {
            Debug.Assert(m_item != null);
            m_wm.UiBroker.HandleOnTrayClick(m_item);
            this.Visible = false;
        }

        private void lblClose_MouseEnter(object sender, System.EventArgs e)
		{
            try
            {
                lblClose.Font = new Font(lblClose.Font, FontStyle.Bold);
                lblClose.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
		}

        private void lblClose_MouseLeave(object sender, System.EventArgs e)
		{
            try
            {
                lblClose.Font = new Font(lblClose.Font, FontStyle.Bold);
                lblClose.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
		}

        private void lblClose_Click(object sender, System.EventArgs e)
		{
            try
            {
                this.Visible = false;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
		}

        private void TrayMessage_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (!this.Visible)
                {
                    timerHideDelay.Enabled = false;

                    // Unlink the current item.
                    UnlinkItem();
                }
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void rtbNotification_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                HandleClick();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }   
	}
}
