using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm
{
    public partial class PageFailure : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PageFailure()
        {
            InitializeComponent();
        }

        private void UpdateButtons()
        {
            if (m_wiz.CreateOp != null)
            {
                btnRunConfig.Enabled = m_wiz.CreateOp.OpRes == KwmCoreKwsOpRes.InvalidCfg ||
                                       m_wiz.CreateOp.OpRes == KwmCoreKwsOpRes.NoPower;
                btnRetry.Enabled = m_wiz.CreateOp.OpRes == KwmCoreKwsOpRes.MiscError;
            }

            else
            {
                btnRetry.Enabled = false;
                btnRunConfig.Enabled = false;
            }
        }

        private void PageFailure_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                if (m_wiz.CreateOp != null)
                {
                    // Show the cancel button since the user may retry the creation.
                    SetWizardButtons(Wizard.UI.WizardButtons.Cancel);

                    lblOp.Text = "Teambox Creation";
                    lblExplain.Text = "Your Teambox could not be created. More details are available below.";

                    if (m_wiz.CreateOp.OpRes == KwmCoreKwsOpRes.InvalidCfg)
                        rtbFailureReason.Text = "In order to create new Teamboxes, you must have a valid Teambox license. If you already have a license, click on the Configure button to activate it. To purchase a license, contact your software vendor.";
                    else if (m_wiz.CreateOp.OpRes == KwmCoreKwsOpRes.NoPower)
                        rtbFailureReason.Text = "You are not authorized to create new Teamboxes. Please contact your system administrator.";
                    else
                        rtbFailureReason.Text = m_wiz.CreateOp.ErrorString;
                }
                else if (m_wiz.InviteOp != null)
                {
                    // Show the finish button since there is nothing the user
                    // can do except end the wizard and try again.
                    SetWizardButtons(Wizard.UI.WizardButtons.Finish);

                    lblOp.Text = "Teambox Invitation";
                    lblExplain.Text = "Your invitation failed. No user could be invited. More details are available below.";
                    rtbFailureReason.Text = m_wiz.InviteOp.ErrorString;
                }
                UpdateButtons();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnRetry_Click(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(m_wiz.CreateOp != null); 
                m_wiz.CreateOp.RetryOp(true);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnRunConfig_Click(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(m_wiz.CreateOp != null);
                DialogResult res = Misc.UiBroker.ShowConfigWizard();
                
                if (res == DialogResult.OK)
                    m_wiz.CreateOp.RetryOp(false);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

