using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Wizard.UI;
using kwm.Utils;

namespace kwm.ConfigKPPWizard
{
    public partial class ConfigKPPWelcome : ExternalWizardPage
    {
        private ConfigKPPOrder Order;
        public ConfigKPPWelcome(ConfigKPPOrder order)
        {
            Order = order;
            InitializeComponent();
        }

        private void ConfigKPPWelcome_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Next);
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        private void ConfigKPPWelcome_WizardNext(object sender, WizardPageEventArgs e)
        {
            e.NewPage = Order.HaveAccount;
        }
    }
}
