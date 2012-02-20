namespace kwm
{
    partial class PageSuccessWithWarnings
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
            this.label4 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.invitationWarning = new kwm.UIControls.CreationWizard.ucInvitationWarning();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(444, 29);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Unable to invite some users";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.DarkGreen;
            this.label4.Location = new System.Drawing.Point(263, 54);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 25);
            this.label4.TabIndex = 16;
            this.label4.Text = "Success!";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Location = new System.Drawing.Point(18, 54);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(190, 25);
            this.label6.TabIndex = 13;
            this.label6.Text = "Teambox creation:";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::kwm.Properties.Resources.GreenCheck2;
            this.pictureBox2.Location = new System.Drawing.Point(382, 31);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(48, 48);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 18;
            this.pictureBox2.TabStop = false;
            // 
            // invitationWarning
            // 
            this.invitationWarning.Location = new System.Drawing.Point(3, 83);
            this.invitationWarning.Message = "";
            this.invitationWarning.Name = "invitationWarning";
            this.invitationWarning.Size = new System.Drawing.Size(440, 246);
            this.invitationWarning.TabIndex = 19;
            // 
            // PageSuccessWithWarnings
            // 
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.invitationWarning);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label6);
            this.Name = "PageSuccessWithWarnings";
            this.Size = new System.Drawing.Size(444, 329);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PageSuccessWithWarnings_SetActive);
            this.Controls.SetChildIndex(this.label6, 0);
            this.Controls.SetChildIndex(this.label4, 0);
            this.Controls.SetChildIndex(this.invitationWarning, 0);
            this.Controls.SetChildIndex(this.pictureBox2, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.PictureBox pictureBox2;
        private kwm.UIControls.CreationWizard.ucInvitationWarning invitationWarning;


    }
}
