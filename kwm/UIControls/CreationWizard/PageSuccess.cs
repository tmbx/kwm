using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    public partial class PageSuccess : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PageSuccess()
        {
            InitializeComponent();
        }

        private void PageSuccess_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(Wizard.UI.WizardButtons.Finish);

                if (m_wiz.CreateOp != null)
                {
                    lblOp.Text = "Teambox creation:";
                    lblExplanation.Text = 
                        "Congratulations! Your Teambox has been successfully created." + Environment.NewLine +
                        "Please press on Finish to go back to your Teambox manager.";
                }

                else
                {
                    lblOp.Text = "Teambox invitation:";
                    lblExplanation.Text =
                        "Congratulations! Your invitations have been successfully processed. " +
                        "Your recipients will receive an email in their inbox in the next minutes." + 
                        Environment.NewLine +
                        "Please press on Finish to go back to your Teambox manager.";
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

