using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Wizard.UI;
using kwm.Utils;
using Tbx.Utils;
using kwm.KwmAppControls;

namespace kwm
{
    public partial class PageInvite : Wizard.UI.InternalWizardPage
    {
        private frmCreateKwsWizard m_wiz
        {
            get { return (frmCreateKwsWizard)GetWizard(); }
        }

        public PageInvite()
        {
            InitializeComponent();
            inviteControl.OnInviteListChanged += HandleOnInviteListChanged;
        }

        private void UpdateNextButton()
        {
            EnableWizardButton(WizardButtons.Next,
                               (inviteControl.IsEmailAddressListValid() &&
                               (m_wiz.InviteOp == null || inviteControl.GetEmailAddressList().Count > 0)));
        }

        private void PageInvite_SetActive(object sender, CancelEventArgs e)
        {
            if (m_wiz.CreateOp != null)
            {
                SetWizardButtons(WizardButtons.Back | WizardButtons.Next | WizardButtons.Cancel);
                inviteControl.KwsName = m_wiz.CreateOp.Creds.KwsName;

                // Make sure the invitation textbox does not have focus
                // so that a simple Enter can proceed with the creation.
                this.Banner.Select();
            }

            else
            {
                SetWizardButtons(WizardButtons.Next | WizardButtons.Cancel);
                inviteControl.KwsName = m_wiz.InviteOp.m_kws.CoreData.Credentials.KwsName;
                inviteControl.InvitationString = m_wiz.InviteOp.Invitees;
                inviteControl.Select();

                // Fast track to the next page if requested.
                if (m_wiz.InviteOp.SkipFirstPage)
                {
                    this.Enabled = false;
                    PressButton(WizardButtons.Next);
                    EnableWizardButton(WizardButtons.Next, false);
                    return;
                }
            }

            UpdateNextButton();
        }

        private void HandleOnInviteListChanged(object sender, EventArgs args)
        {
            UpdateNextButton();
        }

        private void PageInvite_WizardNext(object sender, WizardPageEventArgs e)
        {
            try
            {
                if (m_wiz.CreateOp != null)
                {
                    m_wiz.CreateOp.InviteParams = inviteControl.GetInvitationParams();
                    m_wiz.CreateOp.WizInvitePageNext();
                }
                else
                {
                    m_wiz.InviteOp.InviteParams = inviteControl.GetInvitationParams();
                    m_wiz.InviteOp.InviteParams.FlagInvitedUsers(m_wiz.InviteOp.m_kws.CoreData.UserInfo);
                    m_wiz.InviteOp.Invitees = inviteControl.InvitationString;
                    m_wiz.InviteOp.WizInvitePageNext();
                }

                e.Cancel = true;
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}

