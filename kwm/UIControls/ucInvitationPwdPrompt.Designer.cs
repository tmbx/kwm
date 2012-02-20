namespace kwm
{
    partial class ucInvitationPwdPrompt
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
            this.components = new System.ComponentModel.Container();
            this.chkUseSamePwd = new System.Windows.Forms.CheckBox();
            this.txtSamePwd = new System.Windows.Forms.TextBox();
            this.panelPwdPrompt = new System.Windows.Forms.Panel();
            this.lblPwdPromptText = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.panelMore = new System.Windows.Forms.Panel();
            this.lnkWhy = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panelMore.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // chkUseSamePwd
            // 
            this.chkUseSamePwd.AutoSize = true;
            this.chkUseSamePwd.Location = new System.Drawing.Point(26, 144);
            this.chkUseSamePwd.Name = "chkUseSamePwd";
            this.chkUseSamePwd.Size = new System.Drawing.Size(201, 17);
            this.chkUseSamePwd.TabIndex = 2;
            this.chkUseSamePwd.Text = "Use the same password for everyone";
            this.chkUseSamePwd.UseVisualStyleBackColor = true;
            this.chkUseSamePwd.Visible = false;
            this.chkUseSamePwd.CheckedChanged += new System.EventHandler(this.chkUseSamePwd_CheckedChanged);
            // 
            // txtSamePwd
            // 
            this.txtSamePwd.Location = new System.Drawing.Point(46, 164);
            this.txtSamePwd.Name = "txtSamePwd";
            this.txtSamePwd.Size = new System.Drawing.Size(181, 20);
            this.txtSamePwd.TabIndex = 3;
            this.txtSamePwd.UseSystemPasswordChar = true;
            this.txtSamePwd.Visible = false;
            // 
            // panelPwdPrompt
            // 
            this.panelPwdPrompt.AutoScroll = true;
            this.panelPwdPrompt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPwdPrompt.Location = new System.Drawing.Point(26, 38);
            this.panelPwdPrompt.Name = "panelPwdPrompt";
            this.panelPwdPrompt.Size = new System.Drawing.Size(344, 93);
            this.panelPwdPrompt.TabIndex = 4;
            // 
            // lblPwdPromptText
            // 
            this.lblPwdPromptText.Location = new System.Drawing.Point(56, 0);
            this.lblPwdPromptText.Name = "lblPwdPromptText";
            this.lblPwdPromptText.Size = new System.Drawing.Size(314, 35);
            this.lblPwdPromptText.TabIndex = 0;
            this.lblPwdPromptText.Text = "<pwd prompt text>";
            // 
            // panelMore
            // 
            this.panelMore.Controls.Add(this.lnkWhy);
            this.panelMore.Controls.Add(this.label2);
            this.panelMore.Location = new System.Drawing.Point(3, 194);
            this.panelMore.Name = "panelMore";
            this.panelMore.Size = new System.Drawing.Size(394, 36);
            this.panelMore.TabIndex = 5;
            // 
            // lnkWhy
            // 
            this.lnkWhy.AutoSize = true;
            this.lnkWhy.Location = new System.Drawing.Point(342, 13);
            this.lnkWhy.Name = "lnkWhy";
            this.lnkWhy.Size = new System.Drawing.Size(35, 13);
            this.lnkWhy.TabIndex = 1;
            this.lnkWhy.TabStop = true;
            this.lnkWhy.Text = "Why?";
            this.lnkWhy.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkWhy_LinkClicked);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(40, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(284, 19);
            this.label2.TabIndex = 0;
            this.label2.Text = "Some of the users you invited do not require a password.";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::kwm.Properties.Resources.NewLock_OK_24x24;
            this.pictureBox1.Location = new System.Drawing.Point(26, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // ucInvitationPwdPrompt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.panelMore);
            this.Controls.Add(this.chkUseSamePwd);
            this.Controls.Add(this.txtSamePwd);
            this.Controls.Add(this.lblPwdPromptText);
            this.Controls.Add(this.panelPwdPrompt);
            this.Name = "ucInvitationPwdPrompt";
            this.Size = new System.Drawing.Size(400, 235);
            this.panelMore.ResumeLayout(false);
            this.panelMore.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblPwdPromptText;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.TextBox txtSamePwd;
        private System.Windows.Forms.CheckBox chkUseSamePwd;
        private System.Windows.Forms.Panel panelPwdPrompt;
        private System.Windows.Forms.Panel panelMore;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel lnkWhy;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
