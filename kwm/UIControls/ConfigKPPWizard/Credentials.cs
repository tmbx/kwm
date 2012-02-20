using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using kwm.Utils;
using System.Diagnostics;

namespace kwm.ConfigKPPWizard
{
    public partial class Credentials : UserControl
    {
        private const String KPSOnline = "kps01.teambox.co";
        public String Token = null;

        /// <summary>
        /// Event fired whenever the content of a field in this 
        /// user control changed. Used to enable or disable the 
        /// 'Next' button.
        /// </summary>
        [Description("One of the configuration fields changed.")]
        public event EventHandler<EventArgs> OnCredFieldChange;

        public Credentials()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Read the existing KPS settings from the registry. Must never throw since this
        /// is called from a constructor.
        /// </summary>
        public void PopulateFromRegistry()
        {
            RegistryKey regKey = null;
            try
            {
                regKey = Registry.CurrentUser.CreateSubKey(Misc.GetKPPMsoRegKey());
                if (regKey != null)
                {
                    String kpsAddr = (string)regKey.GetValue("KPS_Address", KpsCredentials.DefaultKpsAddress);
                    if (kpsAddr == "")
                        KPSAdress = KpsCredentials.DefaultKpsAddress;
                    else
                        KPSAdress = kpsAddr;

                    UserName = (string)regKey.GetValue("Login", "");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }
        }

        private void HaveEval_CheckedChanged(object sender, EventArgs e)
        {
            txtKpsAddress.Enabled = !UseDefaultServer.Checked;

            if (UseDefaultServer.Checked)
            {
                txtKpsAddress.Text = KpsCredentials.DefaultKpsAddress;
                txtUsername.Focus();
            }
            else
            {
                txtKpsAddress.Focus();
            }
        }

        public String UserName
        {
            get { return txtUsername.Text; }
            set { txtUsername.Text = value; }
        }

        public String Password
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value; }
        }

        public String KPSAdress
        {
            get
            {
                if (UseDefaultServer.Checked)
                    return KpsCredentials.DefaultKpsAddress;
                else
                    return txtKpsAddress.Text;
            }
            set
            {
                if (value == "")
                    value = KpsCredentials.DefaultKpsAddress;

                UseDefaultServer.Checked = (value == KpsCredentials.DefaultKpsAddress);
                txtKpsAddress.Text = value;
            }
        }

        public void ResetError()
        {
            KPSAdressLBL.ForeColor = Color.Black;
            PasswordLBL.ForeColor = Color.Black;
            UserNameLBL.ForeColor = Color.Black;
            ErrorMsgTip.SetToolTip(KPSAdressLBL, "");
            ErrorMsgTip.SetToolTip(PasswordLBL, "");
            ErrorMsgTip.SetToolTip(UserNameLBL, "");
        }

        public void SetServerError(String errorStr)
        {
            KPSAdressLBL.ForeColor = Color.Red;
            ErrorMsgTip.SetToolTip(KPSAdressLBL, errorStr);
        }

        public void SetCredError(String errorStr)
        {
            PasswordLBL.ForeColor = Color.Red;
            UserNameLBL.ForeColor = Color.Red;
            ErrorMsgTip.SetToolTip(PasswordLBL, errorStr);
            ErrorMsgTip.SetToolTip(UserNameLBL, errorStr);
        }

        /// <summary>
        /// Fill a login command with the required parameters and return it.
        /// </summary>
        public K3p.K3pLoginTest GetLoginCommand()
        {
            K3p.K3pLoginTest cmd = new K3p.K3pLoginTest();
            cmd.Info.kps_login = UserName;
            cmd.Info.kps_secret = Password;
            cmd.Info.secret_is_pwd = 1;
            cmd.Info.kps_net_addr = KPSAdress;
            cmd.Info.kps_port_num = 443;
            return cmd;
	}

        /// <summary>
        /// Save the KPS address, login and ticket to the registry if 
        /// accountFlag is set, clears the fields otherwise.
        /// </summary>
        public void Save(bool accountFlag)
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey(Misc.GetKPPMsoRegKey());
            if (regKey == null)
                throw new Exception("Unable to save the settings to the registry.");

            try
            {
                if (accountFlag)
                {
                    regKey.SetValue("Auth", 1);
                    regKey.SetValue("KPS_Private", 1);
                    regKey.SetValue("KPS_Address", KPSAdress);
                    regKey.SetValue("Login", UserName);
                    regKey.SetValue("Ticket", Token);
                }
                else
                {
                    regKey.SetValue("Auth", 0);
                    regKey.SetValue("KPS_Private", 0);
                    regKey.SetValue("KPS_Address", "");
                    regKey.SetValue("Login", "");
                    regKey.SetValue("Ticket", "");
                }

                // In both cases, let Outlook know we changed. Also remember
                // to always save the KPS login ticket. Prompting for a password
                // is no longer supported (for now at least).
                regKey.SetValue("Config_Status", 1);
                regKey.SetValue("SaveTicket", 1);
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }
        }

        private void DoOnCredFieldChange()
        {
            if (OnCredFieldChange != null)
                OnCredFieldChange(this, EventArgs.Empty);
        }

        /// <summary>
        /// Common handler for the three textboxes. 
        /// </summary>
        private void TextBox_Enter(object sender, EventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void txtKpsAddress_TextChanged(object sender, EventArgs e)
        {
            DoOnCredFieldChange();
        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {
            DoOnCredFieldChange();
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            DoOnCredFieldChange();
        }
    }
}

