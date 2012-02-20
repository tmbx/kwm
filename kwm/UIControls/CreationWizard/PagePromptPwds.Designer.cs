namespace kwm
{
    partial class PagePromptPwds
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pwdPromptControl = new kwm.ucInvitationPwdPrompt();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(411, 40);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Secure Teambox passwords";
            // 
            // pwdPromptControl
            // 
            this.pwdPromptControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.pwdPromptControl.AutoScroll = true;
            this.pwdPromptControl.Location = new System.Drawing.Point(3, 46);
            this.pwdPromptControl.Name = "pwdPromptControl";
            this.pwdPromptControl.Size = new System.Drawing.Size(401, 248);
            this.pwdPromptControl.TabIndex = 1;
            // 
            // PagePromptPwds
            // 
            this.Controls.Add(this.pwdPromptControl);
            this.Name = "PagePromptPwds";
            this.Size = new System.Drawing.Size(411, 298);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.PagePromptPwds_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PagePromptPwds_SetActive);
            this.Controls.SetChildIndex(this.pwdPromptControl, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private ucInvitationPwdPrompt pwdPromptControl;
    }
}
