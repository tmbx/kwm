using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Wizard.UI;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public partial class NewSession3 : NewSessionBasePage
    {
        public NewSession3()
        {
            InitializeComponent();
        }

        private void NewSession3_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Back | WizardButtons.Next | WizardButtons.Cancel);
                EnableWizardButton(WizardButtons.Cancel, false); EnableWizardButton(WizardButtons.Cancel, true);
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSession3_WizardBack(object sender, WizardPageEventArgs e)
        {
            try
            {
                if (WizardConfig.ShareDeskop)
                    e.NewPage = "NewSession1";
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSession3_WizardNext(object sender, WizardPageEventArgs e)
        {
            try
            {
                WizardConfig.SupportMode = radioGiveControl.Checked;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void radioGiveControl_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (radioGiveControl.Checked && Misc.ApplicationSettings.AppSharingWarnOnSupportSession)
                {
                    SupportModeWarning warn = new SupportModeWarning();
                    Misc.OnUiEntry();
                    warn.ShowDialog();
                    Misc.OnUiExit();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                radioGiveControl.Checked = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            try
            {
                radioNoControl.Checked = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

