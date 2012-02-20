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
    public partial class NewSessionWelcome : Wizard.UI.ExternalWizardPage
    {
        public NewSessionWelcome()
        {
            InitializeComponent();
        }

        private void NewSessionWelcome_SetActive(object sender, CancelEventArgs e)
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
    }
}

