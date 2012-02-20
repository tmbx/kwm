namespace kwm
{
    partial class PageInviteSuccessWithWarnings : Wizard.UI.InternalWizardPage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ucInvitationWarning1 = new kwm.UIControls.CreationWizard.ucInvitationWarning();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(444, 41);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Unable to invite some users";
            // 
            // ucInvitationWarning1
            // 
            this.ucInvitationWarning1.Location = new System.Drawing.Point(4, 45);
            this.ucInvitationWarning1.Message = "";
            this.ucInvitationWarning1.Name = "ucInvitationWarning1";
            this.ucInvitationWarning1.Size = new System.Drawing.Size(440, 246);
            this.ucInvitationWarning1.TabIndex = 1;
            // 
            // PageInviteSuccessWithWarnings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ucInvitationWarning1);
            this.Name = "PageInviteSuccessWithWarnings";
            this.Size = new System.Drawing.Size(444, 300);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PageInviteSuccessWithWarnings_SetActive);
            this.Controls.SetChildIndex(this.ucInvitationWarning1, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private kwm.UIControls.CreationWizard.ucInvitationWarning ucInvitationWarning1;
    }
}
