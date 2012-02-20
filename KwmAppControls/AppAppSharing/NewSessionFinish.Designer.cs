namespace kwm.KwmAppControls
{
    partial class NewSessionFinish
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.introductionLabel = new System.Windows.Forms.Label();
            this.txtSessionSubject = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblReview = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Sidebar
            // 
            this.Sidebar.BackgroundImage = kwm.KwmAppControls.Properties.Resources.left_banner;
            this.Sidebar.Size = new System.Drawing.Size(165, 323);
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(171, 19);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(245, 51);
            this.titleLabel.TabIndex = 4;
            this.titleLabel.Text = "You are one click away from starting your session!";
            // 
            // introductionLabel
            // 
            this.introductionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.introductionLabel.Location = new System.Drawing.Point(172, 75);
            this.introductionLabel.Name = "introductionLabel";
            this.introductionLabel.Size = new System.Drawing.Size(262, 83);
            this.introductionLabel.TabIndex = 5;
            this.introductionLabel.Text = "Please review the options you chose.\r\n\r\nWhen you are satisfied, please click on t" +
                "he Finish button below in order to start your session right away.";
            // 
            // txtSessionSubject
            // 
            this.txtSessionSubject.Location = new System.Drawing.Point(171, 278);
            this.txtSessionSubject.Name = "txtSessionSubject";
            this.txtSessionSubject.Size = new System.Drawing.Size(232, 20);
            this.txtSessionSubject.TabIndex = 6;
            this.txtSessionSubject.TextChanged += new System.EventHandler(this.txtSessionSubject_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(171, 259);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Session\'s Subject:";
            // 
            // lblReview
            // 
            this.lblReview.BackColor = System.Drawing.SystemColors.Control;
            this.lblReview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblReview.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblReview.Location = new System.Drawing.Point(175, 162);
            this.lblReview.Name = "lblReview";
            this.lblReview.Size = new System.Drawing.Size(228, 80);
            this.lblReview.TabIndex = 8;
            this.lblReview.Text = "label2";
            // 
            // NewSessionFinish
            // 
            this.Controls.Add(this.lblReview);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSessionSubject);
            this.Controls.Add(this.introductionLabel);
            this.Controls.Add(this.titleLabel);
            this.Name = "NewSessionFinish";
            this.Size = new System.Drawing.Size(432, 323);
            this.WizardFinish += new System.ComponentModel.CancelEventHandler(this.NewSessionFinish_WizardFinish);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.NewSessionFinish_SetActive);
            this.Controls.SetChildIndex(this.Sidebar, 0);
            this.Controls.SetChildIndex(this.titleLabel, 0);
            this.Controls.SetChildIndex(this.introductionLabel, 0);
            this.Controls.SetChildIndex(this.txtSessionSubject, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.lblReview, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label introductionLabel;
        private System.Windows.Forms.TextBox txtSessionSubject;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblReview;
    }
}
