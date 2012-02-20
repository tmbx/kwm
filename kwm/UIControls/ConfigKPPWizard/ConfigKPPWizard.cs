using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Wizard.UI;


namespace kwm.ConfigKPPWizard
{
    public partial class ConfigKPPWizard : WizardSheet
    {
        ConfigKPPCredentials creds;
        public ConfigKPPWizard(WmKmodBroker broker)
        {
            InitializeComponent();
            this.AcceptButton = this.nextButton;
            this.Icon = Properties.Resources.teambox;

            ConfigKPPOrder order = new ConfigKPPOrder();
            order.HaveAccount = order.Credentials = "ConfigKPPCredentials";
            creds = new ConfigKPPCredentials(order, broker);
            this.Pages.Add(new ConfigKPPWelcome(order));
            this.Pages.Add(new ConfigKPPCreateAccount(order));
            this.Pages.Add(creds);
            this.Pages.Add(new ConfigKPPSuccess(order));

            ResizeToFit();
        }

        public void SaveCreds()
        {
            creds.SaveCredentials();
        }
    }
}
