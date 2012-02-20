using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm
{
    public partial class PageSuccessWithWarnings : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PageSuccessWithWarnings()
        {
            InitializeComponent();
        }

        private void PageSuccessWithWarnings_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(Wizard.UI.WizardButtons.Finish);
                // This is the workspace creation success page.
                invitationWarning.Message = m_wiz.OpInviteParams.GetErroneousInviteesText();                
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

