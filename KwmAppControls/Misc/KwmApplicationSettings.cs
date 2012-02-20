using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Drawing;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm.Utils
{
    public sealed class KwmApplicationSettings : ApplicationSettingsBase
    {
        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("false")]
        public bool KwmEnableDebugging
        {
            get { return (bool)this["KwmEnableDebugging"]; }
            set
            {
                this["KwmEnableDebugging"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("false")]
        public bool KwmLogToFile
        {
            get { return (bool)this["KwmLogToFile"]; }
            set
            {
                this["KwmLogToFile"] = value;
            }
        }

        /// <summary>
        /// This property controls ktlstunnel and kmod debugging status. Do
        /// not rename since it will mess up the settings xml file.
        /// </summary>
        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("0")]
        public int ktlstunnelLoggingLevel
        {
            get { return (int)this["ktlstunnelLoggingLevel"]; }
            set
            {
                this["ktlstunnelLoggingLevel"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public string CustomKasAddress
        {
            get { return (string)this["CustomKasAddress"]; }
            set
            {
                this["CustomKasAddress"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public string CustomKasPort
        {
            get { return (string)this["CustomKasPort"]; }
            set
            {
                this["CustomKasPort"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("false")]
        public bool UseCustomKas
        {
            get { return (bool)this["UseCustomKas"]; }
            set
            {
                this["UseCustomKas"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("true")]
        public bool ShowNotification
        {
            get { return (bool)this["ShowNotification"]; }
            set
            {
                this["ShowNotification"] = value;
            }
        }

        /// <summary>
        /// Delay before tray notification starts hiding itself (in milliseconds).
        /// </summary>
        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("8000")]
        public int NotificationDelay
        {
            get { return (int)this["NotificationDelay"]; }
            set
            {
                this["NotificationDelay"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("0,0")]
        public System.Drawing.Point MainWindowPosition
        {
            get { return (System.Drawing.Point)this["MainWindowPosition"]; }
            set
            {
                this["MainWindowPosition"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("800,593")]
        public System.Drawing.Size MainWindowSize
        {
            get { return (System.Drawing.Size)this["MainWindowSize"]; }
            set
            {
                this["MainWindowSize"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("Normal")]
        public System.Windows.Forms.FormWindowState MainWindowState
        {
            get { return (System.Windows.Forms.FormWindowState)this["MainWindowState"]; }
            set
            {
                this["MainWindowState"] = value;
            }
        }

        /// <summary>
        /// Should the window be restored "Normal" or "Maximzed"
        /// when being displayed from the "Minimized" state?
        /// </summary>
        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("Normal")]
        public System.Windows.Forms.FormWindowState MainWindowStateAfterMinimize
        {
            get { return (System.Windows.Forms.FormWindowState)this["MainWindowStateAfterMinimize"]; }
            set
            {
                Debug.Assert(value != System.Windows.Forms.FormWindowState.Minimized);
                Logging.Log("Setting MainWindowStateAfterMinimize to " + value);
                this["MainWindowStateAfterMinimize"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("189")]
        public int MainWindow_SplitPanelLeftSplitSize
        {
            get { return (int)this["MainWindow_SplitPanelLeftSplitSize"]; }
            set
            {
                this["MainWindow_SplitPanelLeftSplitSize"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("395")]
        public int MainWindow_SplitPanelRightSplitSize
        {
            get { return (int)this["MainWindow_SplitPanelRightSplitSize"]; }
            set
            {
                this["MainWindow_SplitPanelRightSplitSize"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("352")]
        public int MainWindow_SplitPanelWsMembersSplitSize
        {
            get { return (int)this["MainWindow_SplitPanelWsMembersSplitSize"]; }
            set
            {
                this["MainWindow_SplitPanelWsMembersSplitSize"] = value;
            }
        }


        /// <summary>
        /// Full path where the Choose file dialog will open
        /// itself when the user wants to add external files
        /// to a share.
        /// </summary>
        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public string KfsAddExternalFilesPath
        {
            get { return (string)this["KfsAddExternalFilesPath"]; }
            set
            {
                this["KfsAddExternalFilesPath"] = value;
            }
        }

        /// <summary>
        /// Full path to the InitialDirectory of the Save As dialog.
        /// </summary>
        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public string KfsSaveAsPath
        {
            get { return (string)this["KfsSaveAsPath"]; }
            set
            {
                this["KfsSaveAsPath"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("98")]
        public int KfsSplitterSplitDistance
        {
            get { return (int)this["KfsSplitterSplitDistance"]; }
            set
            {
                this["KfsSplitterSplitDistance"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public string ImportWsPath
        {
            get { return (string)this["ImportWsPath"]; }
            set
            {
                this["ImportWsPath"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public string ExportWsPath
        {
            get { return (string)this["ExportWsPath"]; }
            set
            {
                this["ExportWsPath"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("true")]
        public bool AppSharingWarnOnSupportSession
        {
            get { return (bool)this["AppSharingWarnOnSupportSession"]; }
            set
            {
                this["AppSharingWarnOnSupportSession"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("0,0")]
        public Point ScreenSharingOverlayPos
        {
            get { return (Point)this["ScreenSharingOverlayPos"]; }
            set
            {
                this["ScreenSharingOverlayPos"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("130")]
        public int KfsNameColumnWidth
        {
            get { return (int)this["KfsNameColumnWidth"]; }
            set
            {
                this["KfsNameColumnWidth"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("60")]
        public int KfsSizeColumnWidth
        {
            get { return (int)this["KfsSizeColumnWidth"]; }
            set
            {
                this["KfsSizeColumnWidth"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("110")]
        public int KfsModDateColumnWidth
        {
            get { return (int)this["KfsModDateColumnWidth"]; }
            set
            {
                this["KfsModDateColumnWidth"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("102")]
        public int KfsModByColumnWidth
        {
            get { return (int)this["KfsModByColumnWidth"]; }
            set
            {
                this["KfsModByColumnWidth"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("96")]
        public int KfsStatusColumnWidth
        {
            get { return (int)this["KfsStatusColumnWidth"]; }
            set
            {
                this["KfsStatusColumnWidth"] = value;
            }
        }

        [UserScopedSettingAttribute()]
        [SettingsManageabilityAttribute(SettingsManageability.Roaming)]
        [DefaultSettingValueAttribute("")]
        public String KfsStorePath
        {
            get { return (String)this["KfsStorePath"]; }
            set
            {
                this["KfsStorePath"] = value;
            }
        }
    }
}
