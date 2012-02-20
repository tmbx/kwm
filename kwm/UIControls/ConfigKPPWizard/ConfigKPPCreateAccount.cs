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
using Microsoft.Win32;
using System.Diagnostics;

namespace kwm.ConfigKPPWizard
{
    public partial class ConfigKPPCreateAccount : InternalWizardPage
    {
        private ConfigKPPOrder Order;
        public ConfigKPPCreateAccount(ConfigKPPOrder order)
        {
            Order = order;
            InitializeComponent();
        }

        private void ConfigKPPCreateAccount_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Finish | WizardButtons.Back);
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Misc.OpenFileInWorkerThread(e.LinkText);
                
                // FIXME uncomment this when the website has the "try now"
                // section available. Also make btnSignup visible again in Designer.
                //SetWizardButtons(WizardButtons.Next | WizardButtons.Back);
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        private void ConfigKPPCreateAccount_WizardBack(object sender, WizardPageEventArgs e)
        {
            e.NewPage = Order.HaveAccount;
        }

        private void ConfigKPPCreateAccount_WizardNext(object sender, WizardPageEventArgs e)
        {
            e.NewPage = Order.Credentials;
        }

        private void signup_Click(object sender, EventArgs e)
        {
            try
            {
                SetWizardButtons(WizardButtons.Next);
                Misc.OpenFileInWorkerThread("http://www.teambox.co/");
                this.PressButton(WizardButtons.Next);
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }

        private void ConfigKPPCreateAccount_WizardFinish(object sender, CancelEventArgs e)
        {
            try
            {
                ((ConfigKPPWizard)GetWizard()).SaveCreds();
            }
            catch (Exception ex)
            {
                Misc.HandleException(ex);
            }
        }
    }
}
