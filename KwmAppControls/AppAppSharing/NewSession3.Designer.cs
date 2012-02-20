namespace kwm.KwmAppControls
{
    partial class NewSession3
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewSession3));
            this.label1 = new System.Windows.Forms.Label();
            this.radioGiveControl = new System.Windows.Forms.RadioButton();
            this.radioNoControl = new System.Windows.Forms.RadioButton();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(432, 50);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Remote control option";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 95);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(376, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Would you like give full control of your computer to your session\'s participants?" +
                "";
            // 
            // radioGiveControl
            // 
            this.radioGiveControl.AutoSize = true;
            this.radioGiveControl.Location = new System.Drawing.Point(73, 184);
            this.radioGiveControl.Name = "radioGiveControl";
            this.radioGiveControl.Size = new System.Drawing.Size(82, 17);
            this.radioGiveControl.TabIndex = 2;
            this.radioGiveControl.Text = "Give control";
            this.radioGiveControl.UseVisualStyleBackColor = true;
            this.radioGiveControl.CheckedChanged += new System.EventHandler(this.radioGiveControl_CheckedChanged);
            // 
            // radioNoControl
            // 
            this.radioNoControl.AutoSize = true;
            this.radioNoControl.Checked = true;
            this.radioNoControl.Location = new System.Drawing.Point(216, 184);
            this.radioNoControl.Name = "radioNoControl";
            this.radioNoControl.Size = new System.Drawing.Size(115, 17);
            this.radioNoControl.TabIndex = 3;
            this.radioNoControl.TabStop = true;
            this.radioNoControl.Text = "Do not give control";
            this.radioNoControl.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.radioNoControl.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.InitialImage = null;
            this.pictureBox1.Location = new System.Drawing.Point(95, 146);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(39, 33);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(254, 145);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(32, 32);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 5;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // NewSession3
            // 
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.radioNoControl);
            this.Controls.Add(this.radioGiveControl);
            this.Controls.Add(this.label1);
            this.Name = "NewSession3";
            this.Size = new System.Drawing.Size(432, 323);
            this.WizardBack += new Wizard.UI.WizardPageEventHandler(this.NewSession3_WizardBack);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.NewSession3_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.NewSession3_SetActive);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.radioGiveControl, 0);
            this.Controls.SetChildIndex(this.radioNoControl, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.pictureBox1, 0);
            this.Controls.SetChildIndex(this.pictureBox2, 0);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioGiveControl;
        private System.Windows.Forms.RadioButton radioNoControl;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
    }
}
