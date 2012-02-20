using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Tbx.Utils;
using Wizard.UI;

namespace kwm
{
    public partial class ConfigKPPCredentials : InternalWizardPage
    {
        private KmodQuery m_query = null;
        private WmKmodBroker m_broker = null;
        private ConfigKPPOrder Order;
        private bool LoginSuccess = false;

        public new bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if (value)
                    SetWizardButtons(WizardButtons.Back | WizardButtons.Next | WizardButtons.Cancel);
                else
                    SetWizardButtons(WizardButtons.Cancel);
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
            if (m_query != null)
            {
                m_query.Cancel();
                m_query = null;
            }
        }

        private void ConfigKPPPage3_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Next | WizardButtons.Back | WizardButtons.Cancel);

                rbHaveAccount.Checked = true;

                creds.Focus();
                
                creds.ResetError();
                LoginSuccess = false;

                UpdateNextButton();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
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
                    m_query = new KmodQuery();
                    m_query.Submit(m_broker, new K3pCmd[] { cmd }, OnLoginResult);
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
            if (m_query != null)
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
            ClearKmodQuery();
            K3p.kmo_server_info_ack ack = query.OutMsg as K3p.kmo_server_info_ack;
            
            if (ack != null)
            {
                creds.Token = ack.Token;
                WmLoginTicketQuery ticketQuery = new WmLoginTicketQuery();
                SaveCredentials();
                m_query = ticketQuery;
                ticketQuery.Submit(m_broker, WmWinRegistry.Spawn(), OnTicketResult);
            }

            else
            {
                K3p.kmo_server_info_nack nack = query.OutMsg as K3p.kmo_server_info_nack;

                if (nack != null)
                {
                    if (nack.Error.StartsWith("cannot resolve ")) creds.SetServerError(nack.Error);
                    else creds.SetCredError(nack.Error);
                }

                else
                {
                    creds.SetServerError(query.OutDesc);
                }

                this.Enabled = true;
            }
        }

        /// <summary>
        /// Called when the ticket query results are available.
        /// </summary>
        private void OnTicketResult(WmLoginTicketQuery query)
        {
            ClearKmodQuery();

            query.UpdateRegistry(WmWinRegistry.Spawn());
            if (query.Res != WmLoginTicketQueryRes.OK) creds.SetServerError(query.OutDesc);
            else LoginSuccess = true;

            this.Enabled = true;

            // Pass to the next page.
            if (LoginSuccess) this.PressButton(WizardButtons.Next);
        }

        /// <summary>
        /// Set the 'Next' button enabled or disable, according to the status of
        /// the window.
        /// </summary>
        private void UpdateNextButton()
        {
            EnableWizardButton(WizardButtons.Next, (creds.KpsAddress != "" &&
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
                Base.HandleException(ex);
            }
        }

        /// <summary>
        /// Save the configuration to the registry.
        /// </summary>
        public void SaveCredentials()
        {
            creds.Save(rbHaveAccount.Checked);
        }

        private void lnkWhatsThis_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                String title = "Teambox license";
                String content = "This information should have been provided to you by your network administrator or a Teambox sales representative.";
                frmInfoForm f = new frmInfoForm(title, content);
                f.StartPosition = FormStartPosition.Manual;
                Point p = new Point(lnkWhatsThis.Left + lnkWhatsThis.Width + 10, lnkWhatsThis.Top + lnkWhatsThis.Height - f.Height - 20);
                f.Location = lnkWhatsThis.Parent.PointToScreen(p);
                f.ShowDialog();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}
