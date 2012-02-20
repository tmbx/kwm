namespace kwm
{
    partial class frmInvitationPwdPromptExpl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmInvitationPwdPromptExpl));
            this.panel1 = new System.Windows.Forms.Panel();
            this.splitBottom = new System.Windows.Forms.SplitContainer();
            this.panelAlreadyInvited = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.panelNoPwdReq = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.splitBottom.Panel1.SuspendLayout();
            this.splitBottom.Panel2.SuspendLayout();
            this.splitBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.splitBottom);
            this.panel1.Location = new System.Drawing.Point(0, 67);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(393, 297);
            this.panel1.TabIndex = 0;
            // 
            // splitBottom
            // 
            this.splitBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitBottom.IsSplitterFixed = true;
            this.splitBottom.Location = new System.Drawing.Point(0, 0);
            this.splitBottom.Name = "splitBottom";
            this.splitBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitBottom.Panel1
            // 
            this.splitBottom.Panel1.Controls.Add(this.panelAlreadyInvited);
            this.splitBottom.Panel1.Controls.Add(this.label2);
            // 
            // splitBottom.Panel2
            // 
            this.splitBottom.Panel2.Controls.Add(this.panelNoPwdReq);
            this.splitBottom.Panel2.Controls.Add(this.label3);
            this.splitBottom.Size = new System.Drawing.Size(393, 297);
            this.splitBottom.SplitterDistance = 128;
            this.splitBottom.TabIndex = 2;
            this.splitBottom.TabStop = false;
            // 
            // panelAlreadyInvited
            // 
            this.panelAlreadyInvited.AutoScroll = true;
            this.panelAlreadyInvited.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelAlreadyInvited.Location = new System.Drawing.Point(16, 52);
            this.panelAlreadyInvited.Name = "panelAlreadyInvited";
            this.panelAlreadyInvited.Size = new System.Drawing.Size(364, 66);
            this.panelAlreadyInvited.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(4, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(356, 30);
            this.label2.TabIndex = 1;
            this.label2.Text = "The following users already have access to this Teambox and will not be reinvited" +
                ". You do not need to set them another password.";
            // 
            // panelNoPwdReq
            // 
            this.panelNoPwdReq.AutoScroll = true;
            this.panelNoPwdReq.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelNoPwdReq.Location = new System.Drawing.Point(16, 72);
            this.panelNoPwdReq.Name = "panelNoPwdReq";
            this.panelNoPwdReq.Size = new System.Drawing.Size(364, 66);
            this.panelNoPwdReq.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(4, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(356, 56);
            this.label3.TabIndex = 3;
            this.label3.Text = resources.GetString("label3.Text");
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(92, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(288, 44);
            this.label1.TabIndex = 1;
            this.label1.Text = "So you invited some people to a Teambox and prompted for some passwords. You noti" +
                "ced that some people you wanted to invite are missing from the list? Here is why" +
                ".";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(16, 9);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(306, 373);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // frmInvitationPwdPromptExpl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(392, 405);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmInvitationPwdPromptExpl";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "";
            this.panel1.ResumeLayout(false);
            this.splitBottom.Panel1.ResumeLayout(false);
            this.splitBottom.Panel2.ResumeLayout(false);
            this.splitBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitBottom;
        private System.Windows.Forms.Panel panelAlreadyInvited;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panelNoPwdReq;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnClose;
    }
}
