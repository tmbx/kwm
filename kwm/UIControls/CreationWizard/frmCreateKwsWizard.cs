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

namespace kwm
{
    public partial class frmCreateKwsWizard : Wizard.UI.WizardSheet
    {
        /// <summary>
        /// Reference to the create operation, if any.
        /// </summary>
        public KwmCreateKwsOp CreateOp;

        /// <summary>
        /// Reference to the invite operation, if any.
        /// </summary>
        public KwmInviteOp InviteOp;

        /// <summary>
        /// Reference to the Kmod broker. Used to show the configuration
        /// wizard.
        /// </summary>
        public WmKmodBroker KmodBroker;

        /// <summary>
        /// If set to true, call Op.WizEntry() on the wizard's Load event.
        /// </summary>
        private bool m_reenterOnLoadFlag = false;

        private String m_pleaseWaitString = "";

        /// <summary>
        /// String describing the current operation that required the
        /// "Please wait" page to show up.
        /// </summary>
        public String PleaseWaitString
        {
            get { return m_pleaseWaitString; }
        }

        /// <summary>
        /// Invitation parameters of the current operation.
        /// </summary>
        public KwsInviteOpParams OpInviteParams
        {
            get
            {
                if (CreateOp != null) return CreateOp.InviteParams;
                if (InviteOp != null) return InviteOp.InviteParams;
                return null;
            }
        }

        private frmCreateKwsWizard()
        {
            InitializeComponent();
        }

        public frmCreateKwsWizard(KwsCoreOp op, WmKmodBroker kmodBroker)
            : this()
        {
            CreateOp = op as KwmCreateKwsOp;
            InviteOp = op as KwmInviteOp;

            KmodBroker = kmodBroker;

            this.AcceptButton = this.nextButton;
            this.Icon = Properties.Resources.TeamboxIcon;

            this.Text = (CreateOp != null ? "Teambox creation" : "Teambox invitation");

            Pages.Add(new PagePleaseWait());
            Pages.Add(new PageCreate());
            Pages.Add(new PageInvite());
            Pages.Add(new PagePromptPwds());
            Pages.Add(new PageSuccess());
            Pages.Add(new PageSuccessWithWarnings());
            Pages.Add(new PageInviteSuccessWithWarnings());
            Pages.Add(new PageFailure());

            ResizeToFit();
        }

        /// <summary>
        /// Override the behavior of the Cancel button click to prompt
        /// for confirmation.
        /// </summary>
        protected override void OnQueryCancel(CancelEventArgs e)
        {
            // Do not prompt for cancellation unless we are on the waiting dialog.
            if (GetActivePage().Name != "PagePleaseWait") return;

            KMsgBoxResult r = KMsgBox.Show("Are you sure you want to cancel?", "Confirmation required", KMsgBoxButton.OKCancel, MessageBoxIcon.Question);
            
            // FIXME Should we check for reentrance here?

            if (r == KMsgBoxResult.OK)
                base.OnQueryCancel(e);
            else
                e.Cancel = true;
        }

        /// <summary>
        /// Show the wizard at the "Please wait" page and call Op.WizEntry()
        /// when done.
        /// </summary>
        public void ShowAndReenter()
        {
            m_reenterOnLoadFlag = true;
            this.ShowDialog();
            if (CreateOp != null) CreateOp.CancelOp(true, "Canceled by user");
            if (InviteOp != null) InviteOp.CancelOp(true, "Canceled by user");
        }

        private void frmCreateKwsWizard_Load(object sender, EventArgs e)
        {
            try
            {
                if (m_reenterOnLoadFlag)
                {
                    // Proceed to normal reentry.   
                    if (CreateOp != null) CreateOp.WizEntry();
                    else if (InviteOp != null) InviteOp.WizEntry();
                }
            }

            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        public void ShowCreationPage()
        {
            SetActivePage("PageCreate");
        }

        public void ShowInvitePage()
        {
            SetActivePage("PageInvite");
        }

        public void ShowPasswordPage()
        {
            SetActivePage("PagePromptPwds");
        }

        public void ShowResults()
        {
            if (CreateOp != null)
            {
                if (CreateOp.OpRes == KwmCoreKwsOpRes.Success)
                {
                    if (CreateOp.InviteParams.HasInvitationErrors()) 
                        SetActivePage("PageSuccessWithWarnings");
                    else 
                        SetActivePage("PageSuccess");
                }

                else if (CreateOp.OpRes == KwmCoreKwsOpRes.InvalidCfg ||
                    CreateOp.OpRes == KwmCoreKwsOpRes.NoPower ||
                    CreateOp.OpRes == KwmCoreKwsOpRes.MiscError)
                {
                    SetActivePage("PageFailure");
                }

                else
                {
                    Debug.Assert(false);
                }
            }

            else if (InviteOp != null)
            {
                if (InviteOp.OpRes == KwmCoreKwsOpRes.MiscError)
                    SetActivePage("PageFailure");
                else if (InviteOp.InviteParams.HasInvitationErrors())
                    SetActivePage("PageInviteSuccessWithWarnings");
                else
                    SetActivePage("PageSuccess");
            }
        }
        
        public void ShowPleaseWait(String reason)
        {
            m_pleaseWaitString = reason;
            SetActivePage("PagePleaseWait");
        }        
    }
}

