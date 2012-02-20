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

namespace kwm
{
    public partial class PagePleaseWait : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PagePleaseWait()
        {
            InitializeComponent();
            Name = "PagePleaseWait";
        }
        
        private void PagePleaseWait_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                // Only allow cancellation, nothing else.
                SetWizardButtons(WizardButtons.Cancel);
                lblReason.Text = m_wiz.PleaseWaitString;
                EnableWizardButton(WizardButtons.Cancel, true);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

