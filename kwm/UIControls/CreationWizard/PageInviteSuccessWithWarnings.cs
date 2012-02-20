using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// Page to display when an invite operation was successfull but with warnings.
    /// </summary>
    public partial class PageInviteSuccessWithWarnings : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PageInviteSuccessWithWarnings()
        {
            InitializeComponent();
        }

        private void PageInviteSuccessWithWarnings_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                SetWizardButtons(Wizard.UI.WizardButtons.Finish);
                ucInvitationWarning1.Message = m_wiz.InviteOp.InviteParams.GetErroneousInviteesText();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}
