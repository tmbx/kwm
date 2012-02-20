namespace kwm.ConfigKPPWizard
{
    partial class ConfigKPPCreateAccount
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigKPPCreateAccount));
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.signup = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Subtitle = "Limited functionnalities when no account is configured.";
            this.Banner.Title = "No account";
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.SystemColors.Control;
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(47, 72);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(347, 118);
            this.richTextBox1.TabIndex = 1;
            this.richTextBox1.TabStop = false;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBox1_LinkClicked);
            // 
            // signup
            // 
            this.signup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.signup.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.signup.Location = new System.Drawing.Point(334, 194);
            this.signup.Name = "signup";
            this.signup.Size = new System.Drawing.Size(95, 23);
            this.signup.TabIndex = 2;
            this.signup.Text = "Signup";
            this.signup.UseVisualStyleBackColor = true;
            this.signup.Visible = false;
            this.signup.Click += new System.EventHandler(this.signup_Click);
            // 
            // ConfigKPPCreateAccount
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.signup);
            this.Name = "ConfigKPPCreateAccount";
            this.Size = new System.Drawing.Size(432, 220);
            this.WizardBack += new Wizard.UI.WizardPageEventHandler(this.ConfigKPPCreateAccount_WizardBack);
            this.WizardFinish += new System.ComponentModel.CancelEventHandler(this.ConfigKPPCreateAccount_WizardFinish);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.ConfigKPPCreateAccount_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.ConfigKPPCreateAccount_SetActive);
            this.Controls.SetChildIndex(this.signup, 0);
            this.Controls.SetChildIndex(this.richTextBox1, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button signup;
    }
}
