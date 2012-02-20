using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Wizard.UI;
using kwm.Utils;
using System.Collections;

namespace kwm.ConfigKPPWizard
{
    public partial class ConfigKPPCredentials : InternalWizardPage
    {
        private KmodQuery m_loginQuery = null;
        private WmKmodBroker m_broker = null;
        private ConfigKPPOrder Order;
        private bool LoginSuccess = false;

        public new bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if (value)
                    SetWizardButtons(WizardButtons.Back | WizardButtons.Next);
                else
                    SetWizardButtons(WizardButtons.None);
                base.Enabled = value;
            }
        }

        public ConfigKPPCredentials(ConfigKPPOrder order, WmKmodBroker broker)
        {
            Order = order;
            InitializeComponent();
            creds.PopulateFromRegistry();
            m_broker = broker;
            QueryCancel += OnCancellation;
        }

        /// <summary>
        /// Cancel and clear the current KMOD query, if there is one.
        /// </summary>
        private void ClearKmodQuery()
        {
            if (m_loginQuery != null)
            {
                m_loginQuery.Cancel();
                m_loginQuery = null;
            }
        }

        private void ConfigKPPPage3_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Next | WizardButtons.Back);
                EnableNextButton(false);
                rbHaveAccount.Checked = true;

                creds.Focus();

                creds.ResetError();
                LoginSuccess = false;

                UpdateNextButton();
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        private void HaveAccountRB_CheckedChanged(object sender, EventArgs e)
        {
            creds.Enabled = rbHaveAccount.Checked;
            UpdateNextButton();
        }

        private void ConfigKPPCredentials_WizardBack(object sender, WizardPageEventArgs e)
        {
            e.NewPage = "ConfigKPPWelcome";
        }

        private void ConfigKPPCredentials_WizardNext(object sender, WizardPageEventArgs e)
        {
            // Clear the current query if there is one.
            ClearKmodQuery();

            if (rbHaveAccount.Checked)
            {
                if (LoginSuccess)
                {
                    e.NewPage = "ConfigKPPSuccess";
                }
                else
                {
                    this.Enabled = false;
                    e.Cancel = true;

                    // Submit the login query.
                    K3p.K3pLoginTest cmd = creds.GetLoginCommand();
                    m_loginQuery = new KmodQuery();
                    m_loginQuery.Submit(m_broker, new K3pCmd[] { cmd }, OnLoginResult);
                }
            }

            else
            {
                e.NewPage = "ConfigKPPCreateAccount";
            }
        }

        /// <summary>
        /// Called when this page is cancelled.
        /// </summary>
        private void OnCancellation(Object sender, CancelEventArgs e)
        {
            // Cancelling while query is taking place, only cancel query, not wizard.
            if (m_loginQuery != null)
            {
                ClearKmodQuery();
                e.Cancel = true;
                this.Enabled = true;
            }
        }

        /// <summary>
        /// Called when the login query results are available.
        /// </summary>
        private void OnLoginResult(KmodQuery query)
        {
            K3p.kmo_server_info_ack ack = query.OutMsg as K3p.kmo_server_info_ack;
            K3p.kmo_server_info_nack nack = query.OutMsg as K3p.kmo_server_info_nack;

            if (ack != null)
            {
                LoginSuccess = true;
                creds.Token = ack.Token;
            }

            else if (nack != null)
            {
                if (nack.Error.StartsWith("cannot resolve ")) creds.SetServerError(nack.Error);
                else creds.SetCredError(nack.Error);
            }

            else
            {
                creds.SetServerError(query.OutDesc);
            }

            this.Enabled = true;

            // Clear the current query.
            ClearKmodQuery();

            // Pass to the next page.
            if (LoginSuccess) this.PressButton(WizardButtons.Next);
        }

        /// <summary>
        /// Set the 'Next' button enabled or disable, according to the status of
        /// the window.
        /// </summary>
        private void UpdateNextButton()
        {
            EnableNextButton((creds.KPSAdress != "" &&
                             creds.UserName != "" &&
                             creds.Password != "") ||
                             rbNoAccount.Checked);
        }
        
        private void creds_OnCredFieldChange(object sender, EventArgs e)
        {
            try
            {
                if (this.GetWizard() != null)
                    UpdateNextButton();
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        /// <summary>
        /// Save the configuration to the registry.
        /// </summary>
        public void SaveCredentials()
        {
            creds.Save(rbHaveAccount.Checked);
        }
    }
}
