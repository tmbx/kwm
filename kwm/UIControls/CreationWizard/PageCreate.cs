using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Wizard.UI;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm
{
    public partial class PageCreate : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PageCreate()
        {
            InitializeComponent();

            // Initialize the page with default values.
            txtKwsName.Text = "My New Teambox";
            rbSecure.Checked = false;

            txtKwsName.TextChanged += txtKwsName_TextChanged;

            lblStd.Text = Base.GetStdKwsDescription();
            lblSecure.Text = Base.GetSecureKwsDescription();
        }

        private void UpdateNextButton()
        {
            EnableWizardButton(WizardButtons.Next, Base.IsValidKwsName(txtKwsName.Text)); 
        }

        private void PageCreate_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Next | WizardButtons.Cancel);
                UpdateNextButton();

                // This is a hack so that cancel leave the acceptbutton which seems buggy.
                EnableWizardButton(WizardButtons.Cancel, false); EnableWizardButton(WizardButtons.Cancel, true); 
                // End of the hack.

                txtKwsName.Text = m_wiz.CreateOp.Creds.KwsName;
                txtKwsName.Select();
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtKwsName_TextChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateNextButton();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void PageCreate_WizardNext(object sender, Wizard.UI.WizardPageEventArgs e)
        {
            try
            {
                Debug.Assert(m_wiz.CreateOp != null);

                // Make sure the workspace name does not contain leading or trailing spaces.
                m_wiz.CreateOp.Creds.KwsName = txtKwsName.Text.Trim();
                m_wiz.CreateOp.Creds.SecureFlag = rbSecure.Checked;
                m_wiz.CreateOp.WizCreatePageNext();
                e.Cancel = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lblStd_Click(object sender, EventArgs e)
        {
            rbStd.Checked = true;
        }

        private void lblSecure_Click(object sender, EventArgs e)
        {
            rbSecure.Checked = true;
        }
    }
}

