using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace kwm.KwmAppControls
{
    public class NewSessionWizard : Wizard.UI.WizardSheet
    {
        /// <summary>
        /// Wizard configuration. Set by the instantiator of the Wizard object.
        /// </summary>
        public NewSessionWizardConfig WizardConfig;

        public NewSessionWizard()
        {
            InitializeComponent();

            this.Icon = kwm.KwmAppControls.Properties.Resources.Teambox;
            
            this.Pages.Add(new NewSession1());
            this.Pages.Add(new NewSession2());
            this.Pages.Add(new NewSession3());
            this.Pages.Add(new NewSessionFinish());

            ResizeToFit();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            // 
            // NewSessionWizard
            // 
            this.AcceptButton = this.nextButton;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(384, 141);
            this.Name = "NewSessionWizard";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Screen Sharing creation wizard";
            this.ResumeLayout(false);

        }
    }

    /// <summary>
    /// Base class for the various wizard pages of the New Session wizard.
    /// </summary>
    public class NewSessionBasePage : Wizard.UI.InternalWizardPage
    {
        protected NewSessionWizardConfig WizardConfig
        {
            get { return ((NewSessionWizard)GetWizard()).WizardConfig; }
        }
    }

    /// <summary>
    /// Represent the information gathered by the Wizard. Default values should
    /// be set in this object before calling the Wizard.
    /// </summary>
    public class NewSessionWizardConfig
    {
        /// <summary>
        /// Name of the session creator. Used to construct a user friendly
        /// session subject.
        /// </summary>
        public String CreatorName;

        /// <summary>
        /// Desktop sharing mode. False means Single Application mode.
        /// </summary>
        public bool ShareDeskop;

        /// <summary>
        /// Window handle of the Single Application to be shared.
        /// </summary>
        public string SharedWindowHandle;

        /// <summary>
        /// Window title of the Single Application to be shared.
        /// </summary>
        public string SharedAppTitle;

        /// <summary>
        /// Allow remote control.
        /// </summary>
        public bool SupportMode;

        /// <summary>
        /// Session's subject.
        /// </summary>
        public string SessionSubject;

        /// <summary>
        /// Set to false when the wizard completes successfully.
        /// </summary>
        public bool Cancel = true;
    }
}

