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
    public partial class ConfigKPPSuccess : InternalWizardPage
    {
        private ConfigKPPOrder Order;
        public ConfigKPPSuccess(ConfigKPPOrder order)
        {
            Order = order;
            InitializeComponent();
        }

        private void ConfigKPPSuccess_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                // This is a hack so that cancel leave the acceptbutton which seems buggy.
                EnableCancelButton(false); EnableCancelButton(true);
                // End of the hack.
                SetWizardButtons(WizardButtons.Finish);
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        private void ConfigKPPSuccess_WizardBack(object sender, WizardPageEventArgs e)
        {
            e.NewPage = Order.Credentials;
        }

        private void ConfigKPPSuccess_WizardFinish(object sender, CancelEventArgs e)
        {
            try
            {
                ((ConfigKPPWizard)GetWizard()).SaveCreds();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                Misc.HandleException(ex);
            }
        }
    }
}
