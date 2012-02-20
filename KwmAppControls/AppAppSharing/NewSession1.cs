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
    public partial class NewSession1 : NewSessionBasePage
    {
        public NewSession1()
        {
            InitializeComponent();
            WizardNext += HandleOnWizardNext;
        }

        private void NewSession1_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Next | WizardButtons.Cancel);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void HandleOnWizardNext(object sender, WizardPageEventArgs args)
        {
            if (radioDesk.Checked)
                args.NewPage = "NewSession3";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                radioDesk.Checked = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void picApp_Click(object sender, EventArgs e)
        {
            try
            {
                radioApp.Checked = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSession1_WizardNext(object sender, WizardPageEventArgs e)
        {
            try
            {
                WizardConfig.ShareDeskop = radioDesk.Checked;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

