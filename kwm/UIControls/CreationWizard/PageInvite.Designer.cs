namespace kwm
{
    partial class PageInvite
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
            this.inviteControl = new kwm.ucInviteToKws();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(444, 34);
            this.Banner.Subtitle = "";
            this.Banner.Title = "You can invite people to your new Teambox right away.";
            // 
            // inviteControl
            // 
            this.inviteControl.InvitationString = "";
            this.inviteControl.KwsName = "<My new Teambox>";
            this.inviteControl.Location = new System.Drawing.Point(17, 40);
            this.inviteControl.Message = "";
            this.inviteControl.Name = "inviteControl";
            this.inviteControl.Size = new System.Drawing.Size(375, 282);
            this.inviteControl.TabIndex = 1;
            // 
            // PageInvite
            // 
            this.Controls.Add(this.inviteControl);
            this.Name = "PageInvite";
            this.Size = new System.Drawing.Size(444, 333);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.PageInvite_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PageInvite_SetActive);
            this.Controls.SetChildIndex(this.inviteControl, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private ucInviteToKws inviteControl;
    }
}
