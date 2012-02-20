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
using Tbx.Utils;

namespace kwm
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
                SetWizardButtons(WizardButtons.Finish);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
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
                Base.HandleException(ex);
            }
        }
    }
}
