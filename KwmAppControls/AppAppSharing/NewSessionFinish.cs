using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Wizard.UI;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public partial class NewSessionFinish : Wizard.UI.ExternalWizardPage
    {
        private NewSessionWizardConfig WizardConfig
        {
            get
            {
                return ((NewSessionWizard)GetWizard()).WizardConfig;
            }
        }

        public NewSessionFinish()
        {
            InitializeComponent();
        }

        private void UpdateReviewSettings()
        {
            lblReview.Text = "- Sharing Mode: ";
            if (WizardConfig.ShareDeskop)
                lblReview.Text += "Entire Desktop";
            else
                lblReview.Text += "Single application (" + WizardConfig.SharedAppTitle + ")";

            lblReview.Text += Environment.NewLine + Environment.NewLine;

            if (WizardConfig.SupportMode)
                lblReview.Text += "- This Session will allow participants to control your computer.";
            else
                lblReview.Text += "- This Session will NOT allow participants to control your computer.";
        }

        private void NewSessionFinish_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Back | WizardButtons.Finish | WizardButtons.Cancel);
                
                EnableWizardButton(WizardButtons.Cancel, false); EnableWizardButton(WizardButtons.Cancel, true);

                string subject;

                if (WizardConfig.ShareDeskop)
                    subject = WizardConfig.CreatorName + "'s Desktop";
                else
                    subject = WizardConfig.SharedAppTitle;

                if (WizardConfig.SupportMode)
                    subject += " (Support mode)";

                txtSessionSubject.Text = subject;

                UpdateReviewSettings();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSessionFinish_WizardFinish(object sender, CancelEventArgs e)
        {
            try
            {
                WizardConfig.SessionSubject = txtSessionSubject.Text;
                WizardConfig.Cancel = false;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtSessionSubject_TextChanged(object sender, EventArgs e)
        {
            try
            {
                EnableWizardButton(WizardButtons.Finish, txtSessionSubject.Text.Trim() != "");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

