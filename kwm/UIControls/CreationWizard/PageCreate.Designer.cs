namespace kwm
{
    partial class PageCreate
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
            this.lblSecure = new System.Windows.Forms.Label();
            this.rbSecure = new System.Windows.Forms.RadioButton();
            this.lblStd = new System.Windows.Forms.Label();
            this.rbStd = new System.Windows.Forms.RadioButton();
            this.txtKwsName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(444, 36);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Tell us a bit more about your new Teambox.";
            // 
            // lblSecure
            // 
            this.lblSecure.Location = new System.Drawing.Point(37, 95);
            this.lblSecure.Name = "lblSecure";
            this.lblSecure.Size = new System.Drawing.Size(358, 46);
            this.lblSecure.TabIndex = 13;
            this.lblSecure.Text = "<Dynamic  text>";
            this.lblSecure.Click += new System.EventHandler(this.lblSecure_Click);
            // 
            // rdSecure
            // 
            this.rbSecure.AutoSize = true;
            this.rbSecure.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbSecure.Location = new System.Drawing.Point(17, 73);
            this.rbSecure.Name = "rdSecure";
            this.rbSecure.Size = new System.Drawing.Size(65, 17);
            this.rbSecure.TabIndex = 12;
            this.rbSecure.Text = "Secure";
            this.rbSecure.UseVisualStyleBackColor = true;
            // 
            // lblStd
            // 
            this.lblStd.Location = new System.Drawing.Point(37, 42);
            this.lblStd.Name = "lblStd";
            this.lblStd.Size = new System.Drawing.Size(358, 30);
            this.lblStd.TabIndex = 11;
            this.lblStd.Text = "<Dynamic Text>";
            this.lblStd.Click += new System.EventHandler(this.lblStd_Click);
            // 
            // rbStd
            // 
            this.rbStd.AutoSize = true;
            this.rbStd.Checked = true;
            this.rbStd.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbStd.Location = new System.Drawing.Point(17, 18);
            this.rbStd.Name = "rbStd";
            this.rbStd.Size = new System.Drawing.Size(127, 17);
            this.rbStd.TabIndex = 10;
            this.rbStd.TabStop = true;
            this.rbStd.Text = "Standard (default)";
            this.rbStd.UseVisualStyleBackColor = true;
            // 
            // txtKwsName
            // 
            this.txtKwsName.Location = new System.Drawing.Point(107, 50);
            this.txtKwsName.Name = "txtKwsName";
            this.txtKwsName.Size = new System.Drawing.Size(236, 20);
            this.txtKwsName.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Teambox Name:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::kwm.Properties.Resources.NewLock_OK_24x24;
            this.pictureBox1.Location = new System.Drawing.Point(7, 96);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.TabIndex = 14;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.rbStd);
            this.groupBox1.Controls.Add(this.lblSecure);
            this.groupBox1.Controls.Add(this.lblStd);
            this.groupBox1.Controls.Add(this.rbSecure);
            this.groupBox1.Location = new System.Drawing.Point(19, 83);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(409, 152);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Teambox Type";
            // 
            // PageCreate
            // 
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtKwsName);
            this.Controls.Add(this.label1);
            this.Name = "PageCreate";
            this.Size = new System.Drawing.Size(444, 256);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.PageCreate_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PageCreate_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.txtKwsName, 0);
            this.Controls.SetChildIndex(this.groupBox1, 0);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSecure;
        private System.Windows.Forms.RadioButton rbSecure;
        private System.Windows.Forms.Label lblStd;
        private System.Windows.Forms.RadioButton rbStd;
        private System.Windows.Forms.TextBox txtKwsName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}
