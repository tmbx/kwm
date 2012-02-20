using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Wizard.UI;
using System.Diagnostics;
using Tbx.Utils;
using kwm.KwmAppControls;

namespace kwm
{
    public partial class PagePromptPwds : Wizard.UI.InternalWizardPage
    {

        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PagePromptPwds()
        {
            InitializeComponent();

            pwdPromptControl.OnChange += new EventHandler<EventArgs>(pwdPromptControl_OnChange);
        }

        void pwdPromptControl_OnChange(object sender, EventArgs e)
        {
            try
            {
                EnableWizardButton(WizardButtons.Next, pwdPromptControl.IsInputValid());
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void PagePromptPwds_SetActive(object sender, CancelEventArgs e)
        {
            
            pwdPromptControl.InviteParams = m_wiz.OpInviteParams;

            pwdPromptControl.UpdateUI();

            SetWizardButtons(WizardButtons.Back | WizardButtons.Next | WizardButtons.Cancel);
            EnableWizardButton(WizardButtons.Next, pwdPromptControl.IsInputValid());
        }

        private void PagePromptPwds_WizardNext(object sender, Wizard.UI.WizardPageEventArgs e)
        {
            try
            {
                e.Cancel = true;

                foreach (KwsInviteOpUser u in pwdPromptControl.RequiredPwds)
                    u.Pwd = pwdPromptControl.GetPassword(u.EmailAddress);

                if (m_wiz.CreateOp != null)
                    m_wiz.CreateOp.WizPasswordPageNext();
                else if (m_wiz.InviteOp != null) 
                    m_wiz.InviteOp.WizPasswordPageNext();
                
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

