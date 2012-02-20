namespace kwm
{
    partial class ucInviteToKws
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
            this.lblIntro = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtRecipients = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lnkAddPersonalMsg = new System.Windows.Forms.LinkLabel();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.lnkCancel = new System.Windows.Forms.LinkLabel();
            this.lblKwsName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblIntro
            // 
            this.lblIntro.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIntro.Location = new System.Drawing.Point(4, 4);
            this.lblIntro.Name = "lblIntro";
            this.lblIntro.Size = new System.Drawing.Size(110, 21);
            this.lblIntro.TabIndex = 0;
            this.lblIntro.Text = "Invite people to";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(141, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Enter some email addresses:";
            // 
            // txtRecipients
            // 
            this.txtRecipients.AcceptsReturn = true;
            this.txtRecipients.Location = new System.Drawing.Point(12, 56);
            this.txtRecipients.Multiline = true;
            this.txtRecipients.Name = "txtRecipients";
            this.txtRecipients.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtRecipients.Size = new System.Drawing.Size(357, 55);
            this.txtRecipients.TabIndex = 1;
            this.txtRecipients.TextChanged += new System.EventHandler(this.txtRecipients_TextChanged);
            this.txtRecipients.Enter += new System.EventHandler(this.txtRecipients_Enter);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(9, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(364, 16);
            this.label3.TabIndex = 6;
            this.label3.Text = "Use commas to separate emails: joe@pharma-p.com,  jane@pharma-p.com";
            // 
            // lnkAddPersonalMsg
            // 
            this.lnkAddPersonalMsg.AutoSize = true;
            this.lnkAddPersonalMsg.Location = new System.Drawing.Point(9, 142);
            this.lnkAddPersonalMsg.Name = "lnkAddPersonalMsg";
            this.lnkAddPersonalMsg.Size = new System.Drawing.Size(132, 13);
            this.lnkAddPersonalMsg.TabIndex = 7;
            this.lnkAddPersonalMsg.TabStop = true;
            this.lnkAddPersonalMsg.Text = "Add a personal message...";
            this.lnkAddPersonalMsg.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkPersonalizedMsg_LinkClicked);
            // 
            // txtMessage
            // 
            this.txtMessage.AcceptsReturn = true;
            this.txtMessage.Location = new System.Drawing.Point(12, 169);
            this.txtMessage.MaxLength = 1024;
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessage.Size = new System.Drawing.Size(357, 74);
            this.txtMessage.TabIndex = 8;
            this.txtMessage.Visible = false;
            // 
            // lnkCancel
            // 
            this.lnkCancel.AutoSize = true;
            this.lnkCancel.Location = new System.Drawing.Point(133, 142);
            this.lnkCancel.Name = "lnkCancel";
            this.lnkCancel.Size = new System.Drawing.Size(40, 13);
            this.lnkCancel.TabIndex = 9;
            this.lnkCancel.TabStop = true;
            this.lnkCancel.Text = "Cancel";
            this.lnkCancel.Visible = false;
            this.lnkCancel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkCancel_LinkClicked);
            // 
            // lblKwsName
            // 
            this.lblKwsName.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblKwsName.Location = new System.Drawing.Point(108, 4);
            this.lblKwsName.Name = "lblKwsName";
            this.lblKwsName.Size = new System.Drawing.Size(254, 21);
            this.lblKwsName.TabIndex = 11;
            this.lblKwsName.Text = "<My new Teambox>";
            // 
            // ucInviteToKws
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblKwsName);
            this.Controls.Add(this.lnkCancel);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.lnkAddPersonalMsg);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtRecipients);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblIntro);
            this.Name = "ucInviteToKws";
            this.Size = new System.Drawing.Size(377, 252);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRecipients;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.LinkLabel lnkAddPersonalMsg;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.LinkLabel lnkCancel;
        private System.Windows.Forms.Label lblKwsName;
    }
}
