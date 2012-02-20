using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using Tbx.Utils;

namespace kwm
{
    public partial class frmOptions : frmKBaseForm
    {
        private bool m_ignoreStorePathRadioChange = true;

        /// <summary>
        /// Track the original value of the KppMsoDebug checkbox.
        /// </summary>
        private bool KppMsoDebugChecked;

        public frmOptions()
        {
            InitializeComponent();
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            cboLogLevel.SelectedIndex = 0;

            lblRestart.Text = "Changes will take effect when you restart your " + Base.GetKwmString() + ".";

            LoadSettings();
        }

        /// <summary>
        /// Load the settings from ApplicationSettings and reflect
        /// them in the UI.
        /// </summary>
        private void LoadSettings()
        {
            chkEnableDebugging.Checked = Misc.ApplicationSettings.KwmEnableDebugging;
            chkLogToFile.Checked = Misc.ApplicationSettings.KwmLogToFile;
            chkLogToFile.Enabled = chkEnableDebugging.Checked;

            if (Misc.ApplicationSettings.ktlstunnelLoggingLevel == 0)
                ktlsNone.Checked = true;
            else if (Misc.ApplicationSettings.ktlstunnelLoggingLevel == 1)
                ktlsMin.Checked = true;
            else
                ktlsDebug.Checked = true;

            txtKasAddr.Text = Misc.ApplicationSettings.CustomKasAddress;
            txtKasPort.Text = Misc.ApplicationSettings.CustomKasPort;

            chkUseCustomKas.Checked = Misc.ApplicationSettings.UseCustomKas;

            chkShowNotification.Checked = Misc.ApplicationSettings.ShowNotification;

            // Get the delay in seconds.
            int delay = (Misc.ApplicationSettings.NotificationDelay / 1000);
            txtNotifDuration.Text = delay.ToString();

            UpdateNotifDurationStatus();

            KppMsoDebugChecked = chkKppMsoLogging.Checked;

            String storePath = Misc.ApplicationSettings.KfsStorePath;
            if (storePath == "")
            {
                rbFileStoreMyDocs.Checked = true;
            }
            else
            {
                rbFileStoreCustom.Checked = true;
                txtCustomStorePath.Text = storePath;
            }

            m_ignoreStorePathRadioChange = false;

            if (Misc.IsOtcInstalled)
            {
                chkKppMsoLogging.Enabled = true;
                optionTabs.TabPages["tabOTC"].Enabled = true; 
                LoadOtcSettings();
            }
            else
            {
                chkKppMsoLogging.Enabled = false;
                optionTabs.TabPages["tabOTC"].Enabled = false;
            }
        }

        /// <summary>
        /// Save the options in the application settings. Called by the
        /// one who opened the form, according to the DialogResult.
        /// </summary>
        public void SaveSettings()
        {
            Misc.ApplicationSettings.KwmEnableDebugging = chkEnableDebugging.Checked;
            Misc.ApplicationSettings.KwmLogToFile = chkLogToFile.Checked;

            if (ktlsNone.Checked)
                Misc.ApplicationSettings.ktlstunnelLoggingLevel = 0;
            else if(ktlsMin.Checked)
                Misc.ApplicationSettings.ktlstunnelLoggingLevel = 1;
            else
                Misc.ApplicationSettings.ktlstunnelLoggingLevel = 2;

            Misc.ApplicationSettings.CustomKasAddress = txtKasAddr.Text;
            Misc.ApplicationSettings.CustomKasPort = txtKasPort.Text;
            Misc.ApplicationSettings.UseCustomKas = chkUseCustomKas.Checked;

            Misc.ApplicationSettings.ShowNotification = chkShowNotification.Checked;

            if (rbFileStoreCustom.Checked)
                Misc.ApplicationSettings.KfsStorePath = txtCustomStorePath.Text;
            else
                Misc.ApplicationSettings.KfsStorePath = "";

            Misc.ApplicationSettings.Save();

            if (Misc.IsOtcInstalled)
                SaveOtcSetting();
        }

        private void LoadOtcSettings()
        {
            RegistryKey kwmKey = null;
            try
            {
                kwmKey = Registry.CurrentUser.OpenSubKey(Base.GetOtcRegKey());

                int val;
                bool AMdefault = true, AMask = true;

                // Evaluate the tri-state radio buttons describing the state 
                // of the attachment management feature in Outlook.

                if (kwmKey != null && Int32.TryParse(kwmKey.GetValue("ManagedAttachmentDefault", "0") as String, out val))
                    AMdefault = val > 0 ? true : false;
                else
                    AMdefault = false;

                if (kwmKey != null && Int32.TryParse(kwmKey.GetValue("ManagedAttachmentAsk", "0") as String, out val))
                    AMask = val > 0 ? true : false;
                else
                    AMdefault = false;

                // Threshold is stored in bytes.
                if (kwmKey != null && Int32.TryParse(kwmKey.GetValue("ManagedAttachmentThreshold", "1024") as String, out val))
                    txtThreshold.Value = val / 1024;
                else
                    txtThreshold.Value = 1;

                if (AMask)
                    radAMAlwaysAsk.Checked = true;
                else if (!AMask && !AMdefault)
                    radAMNeverUse.Checked = true;
                else if (!AMask && AMdefault)
                    radAMAlwaysUse.Checked = true;

                if (kwmKey != null && Int32.TryParse(kwmKey.GetValue("EnableOtc", "1") as String, out val))
                    chkOtcEnabled.Checked = val > 0;
                else
                    chkOtcEnabled.Checked = true;

                if (kwmKey != null && Int32.TryParse(kwmKey.GetValue("EnableSkurl", "1") as String, out val))
                    chkSkurlEnabled.Checked = val > 0;
                else
                    chkSkurlEnabled.Checked = true;

                if (kwmKey != null && Int32.TryParse(kwmKey.GetValue("EnableOtcDebugging", "0") as String, out val))
                    chkKppMsoLogging.Checked = val > 0;
                else
                    chkKppMsoLogging.Checked = false;

                chkSkurlEnabled.Enabled = chkOtcEnabled.Checked;
                grpAttachmentManagement.Enabled = chkSkurlEnabled.Checked;
            }

            finally
            {
                if (kwmKey != null) kwmKey.Close();
            }
        }

        private void SaveOtcSetting()
        {
            RegistryKey kwmKey = null;
            try
            {
                kwmKey = Registry.CurrentUser.OpenSubKey(Base.GetOtcRegKey(), true);

                if (kwmKey == null) throw new Exception("unable to open HKCU/" + Base.GetOtcRegKey() + "for writing. Outlook Teambox Connector options will not be saved.");

                kwmKey.SetValue("EnableOtc", chkOtcEnabled.Checked ? "1" : "0");
                kwmKey.SetValue("EnableSkurl", chkOtcEnabled.Checked && chkSkurlEnabled.Checked ? "1" : "0");
                kwmKey.SetValue("EnableOtcDebugging", chkKppMsoLogging.Checked ? "1" : "0");
                
                // Threshold in stored in bytes.
                kwmKey.SetValue("ManagedAttachmentThreshold", txtThreshold.Value * 1024);

                if (radAMAlwaysUse.Checked)
                {
                    kwmKey.SetValue("ManagedAttachmentDefault", "1");
                    kwmKey.SetValue("ManagedAttachmentAsk", "0");
                }
                else if (radAMNeverUse.Checked)
                {
                    kwmKey.SetValue("ManagedAttachmentDefault", "0");
                    kwmKey.SetValue("ManagedAttachmentAsk", "0");
                }
                else if (radAMAlwaysAsk.Checked)
                {
                    kwmKey.SetValue("ManagedAttachmentDefault", "1");
                    kwmKey.SetValue("ManagedAttachmentAsk", "1");
                }
            }
            finally
            {
                if (kwmKey != null) kwmKey.Close();
            }
        }

        /// <summary>
        /// Apply a preset of log settings in the UI.
        /// </summary>
        /// <param name="i"></param>
        private void SetLogLevel(int i)
        {
            switch (i)
            {
                // Default (KppMso + Kwm File + Kwm Console);
                case 1:
                    chkKppMsoLogging.Checked = true;
                    chkEnableDebugging.Checked = true;
                    chkLogToFile.Checked = true;
                    ktlsNone.Checked = true;
                    break;
                // Maximum
                case 2:
                    chkKppMsoLogging.Checked = true;
                    chkEnableDebugging.Checked = true;
                    chkLogToFile.Checked = true;
                    ktlsDebug.Checked = true;
                    break;
                // None
                case 0:
                default:
                    chkKppMsoLogging.Checked = false;
                    chkEnableDebugging.Checked = false;
                    chkLogToFile.Checked = false;
                    ktlsNone.Checked = true;
                    break;
            }
        }

        private void chkUseCustomKas_CheckedChanged(object sender, EventArgs e)
        {
            txtKasAddr.Enabled = chkUseCustomKas.Checked;
            txtKasPort.Enabled = chkUseCustomKas.Checked;
        }

        private void txtKasPort_Validating(object sender, CancelEventArgs e)
        {
            if (!Base.IsNumeric(txtKasPort.Text) ||
                Double.Parse(txtKasPort.Text) < 1 || Double.Parse(txtKasPort.Text) > 65536)
            {
                errorProvider.SetError(txtKasPort, "Port must be a numeric value between 1 and 65536.");
            }
            else
            {
                errorProvider.SetError(txtKasPort, "");
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (rbFileStoreCustom.Checked &&
                !Directory.Exists(txtCustomStorePath.Text) &&
                Misc.KwmTellUser("The path to your file store does not exist. Are you sure you want to continue?", "Invalid path", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            try
            {
                SetLogLevel(cboLogLevel.SelectedIndex);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnViewKcsLogs_Click(object sender, EventArgs e)
        {
            try
            {
                String LogPath = Base.GetKcsLogFilePath();
                Syscalls.ShellExecute(IntPtr.Zero, "explore", LogPath, null, null, Syscalls.SW.SW_NORMAL);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }   
        }

        private void chkEnableDebugging_CheckedChanged(object sender, EventArgs e)
        {
            chkLogToFile.Enabled = chkEnableDebugging.Checked;
        }

        private void rbFileStoreMyDocs_CheckedChanged(object sender, EventArgs e)
        {
            txtCustomStorePath.Enabled = rbFileStoreCustom.Checked;
            btnStorePathBrowse.Enabled = rbFileStoreCustom.Checked;
            if (!m_ignoreStorePathRadioChange)
                lblRestart.Visible = true;
        }

        private void btnStorePathBrowse_Click(object sender, EventArgs e)
        {
            String defaultPath = txtCustomStorePath.Text;
            if (defaultPath == "") defaultPath = Misc.GetKfsDefaultStorePath();

            CustomPathBrowseDialog.SelectedPath = defaultPath;

            DialogResult res = CustomPathBrowseDialog.ShowDialog();
            if (res != DialogResult.OK) return;

            txtCustomStorePath.Text = CustomPathBrowseDialog.SelectedPath;
        }

        private void chkShowNotification_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateNotifDurationStatus();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }            
        }

        private void UpdateNotifDurationStatus()
        {
            txtNotifDuration.Enabled = chkShowNotification.Checked;
        }

        private void chkOtcEnabled_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                chkSkurlEnabled.Enabled = chkOtcEnabled.Checked;
                grpAttachmentManagement.Enabled = chkOtcEnabled.Checked;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }   
        }

        private void chkSkurlEnabled_CheckedChanged(object sender, EventArgs e)
        {
            grpAttachmentManagement.Enabled = chkSkurlEnabled.Checked;
        }

        private void radAMNeverUse_CheckedChanged(object sender, EventArgs e)
        {
            lblThreshold.Enabled = !radAMNeverUse.Checked;
            txtThreshold.Enabled = !radAMNeverUse.Checked;
            lblMb.Enabled = !radAMNeverUse.Checked;
        }
    }
}