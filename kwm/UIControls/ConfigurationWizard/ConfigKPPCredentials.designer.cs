namespace kwm
{
    partial class ConfigKPPCredentials
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
            this.label3 = new System.Windows.Forms.Label();
            this.rbNoAccount = new System.Windows.Forms.RadioButton();
            this.rbHaveAccount = new System.Windows.Forms.RadioButton();
            this.creds = new kwm.Credentials();
            this.lnkWhatsThis = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Subtitle = "Please provide your Teambox login and password. ";
            this.Banner.Title = "Teambox Credentials";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(40, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(267, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Choose the configuration that best matches your setup.";
            // 
            // rbNoAccount
            // 
            this.rbNoAccount.AutoSize = true;
            this.rbNoAccount.Location = new System.Drawing.Point(43, 242);
            this.rbNoAccount.Margin = new System.Windows.Forms.Padding(0);
            this.rbNoAccount.Name = "rbNoAccount";
            this.rbNoAccount.Size = new System.Drawing.Size(183, 17);
            this.rbNoAccount.TabIndex = 12;
            this.rbNoAccount.Text = "I do not have a Teambox license.";
            this.rbNoAccount.UseVisualStyleBackColor = true;
            // 
            // rbHaveAccount
            // 
            this.rbHaveAccount.AutoSize = true;
            this.rbHaveAccount.Checked = true;
            this.rbHaveAccount.Location = new System.Drawing.Point(43, 110);
            this.rbHaveAccount.Name = "rbHaveAccount";
            this.rbHaveAccount.Size = new System.Drawing.Size(150, 17);
            this.rbHaveAccount.TabIndex = 13;
            this.rbHaveAccount.TabStop = true;
            this.rbHaveAccount.Text = "I have a Teambox license:";
            this.rbHaveAccount.UseVisualStyleBackColor = true;
            this.rbHaveAccount.CheckedChanged += new System.EventHandler(this.HaveAccountRB_CheckedChanged);
            // 
            // creds
            // 
            this.creds.Location = new System.Drawing.Point(57, 133);
            this.creds.Name = "creds";
            this.creds.Password = "";
            this.creds.Size = new System.Drawing.Size(246, 106);
            this.creds.TabIndex = 14;
            this.creds.UserName = "";
            this.creds.OnCredFieldChange += new System.EventHandler<System.EventArgs>(this.creds_OnCredFieldChange);
            // 
            // lnkWhatsThis
            // 
            this.lnkWhatsThis.AutoSize = true;
            this.lnkWhatsThis.Location = new System.Drawing.Point(199, 112);
            this.lnkWhatsThis.Name = "lnkWhatsThis";
            this.lnkWhatsThis.Size = new System.Drawing.Size(65, 13);
            this.lnkWhatsThis.TabIndex = 15;
            this.lnkWhatsThis.TabStop = true;
            this.lnkWhatsThis.Text = "What\'s this?";
            this.lnkWhatsThis.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkWhatsThis_LinkClicked);
            // 
            // ConfigKPPCredentials
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lnkWhatsThis);
            this.Controls.Add(this.creds);
            this.Controls.Add(this.rbHaveAccount);
            this.Controls.Add(this.rbNoAccount);
            this.Controls.Add(this.label3);
            this.Name = "ConfigKPPCredentials";
            this.Size = new System.Drawing.Size(432, 303);
            this.WizardBack += new Wizard.UI.WizardPageEventHandler(this.ConfigKPPCredentials_WizardBack);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.ConfigKPPCredentials_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.ConfigKPPPage3_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.label3, 0);
            this.Controls.SetChildIndex(this.rbNoAccount, 0);
            this.Controls.SetChildIndex(this.rbHaveAccount, 0);
            this.Controls.SetChildIndex(this.creds, 0);
            this.Controls.SetChildIndex(this.lnkWhatsThis, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton rbNoAccount;
        private System.Windows.Forms.RadioButton rbHaveAccount;
        private Credentials creds;
        private System.Windows.Forms.LinkLabel lnkWhatsThis;
    }
}