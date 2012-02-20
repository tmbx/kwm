using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Collections;

using kwm.Utils;

namespace kwm
{
    /// <summary>
    /// This class wraps around the balloon tooltip functionality of a NotifyIcon object.
    /// It allows balloon-like notification that comes from 
    /// a given tray icon. Planned usage: notify when a global network error
    /// occurs (i.e. KAS got disconnected) or if an updated software version
    /// is available.
    /// </summary>
    public class TrayIconNotifier
    {
        /// <summary>
        /// Reference to the frmMain's tray icon object.
        /// </summary>
        private NotifyIcon m_trayIcon;

        public TrayIconNotifier(NotifyIcon _i)
        {
            m_trayIcon = _i;
            //m_trayIcon.BalloonTipClicked += new KasEventHandler(onBalloonClicked);
        }
       
        /// <summary>
        /// Show a notification balloon.
        /// </summary>
        public void showBalloon(String _title, String _text, ToolTipIcon _icon)
        {
            showBalloon(3, _title, _text, _icon);
        }

        private void showBalloon(int _timeout, String _title, String _text, ToolTipIcon _icon)
        {
            if (m_trayIcon.Visible)
                m_trayIcon.ShowBalloonTip(_timeout, _title, _text, _icon);
        }
    }
}
