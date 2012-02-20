namespace kwm.KwmAppControls
{
    partial class NewSession1
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
            this.label1 = new System.Windows.Forms.Label();
            this.radioDesk = new System.Windows.Forms.RadioButton();
            this.radioApp = new System.Windows.Forms.RadioButton();
            this.picApp = new System.Windows.Forms.PictureBox();
            this.picDesktop = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picApp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDesktop)).BeginInit();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(432, 50);
            this.Banner.Subtitle = "This wizard will guide you through the creation of your Screen Sharing Session.";
            this.Banner.Title = "Welcome to the Screen Sharing wizard";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 93);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(368, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Please specify what type of Screen Sharing session you would like to create:";
            // 
            // radioDesk
            // 
            this.radioDesk.AutoSize = true;
            this.radioDesk.Checked = true;
            this.radioDesk.Location = new System.Drawing.Point(32, 186);
            this.radioDesk.Name = "radioDesk";
            this.radioDesk.Size = new System.Drawing.Size(146, 17);
            this.radioDesk.TabIndex = 2;
            this.radioDesk.TabStop = true;
            this.radioDesk.Text = "Share your entire desktop";
            this.radioDesk.UseVisualStyleBackColor = true;
            // 
            // radioApp
            // 
            this.radioApp.AutoSize = true;
            this.radioApp.Location = new System.Drawing.Point(206, 186);
            this.radioApp.Name = "radioApp";
            this.radioApp.Size = new System.Drawing.Size(206, 17);
            this.radioApp.TabIndex = 3;
            this.radioApp.Text = "Share one of your running applications";
            this.radioApp.UseVisualStyleBackColor = true;
            // 
            // picApp
            // 
            this.picApp.Image = kwm.KwmAppControls.Properties.Resources.Application_48x48;
            this.picApp.Location = new System.Drawing.Point(260, 130);
            this.picApp.Name = "picApp";
            this.picApp.Size = new System.Drawing.Size(48, 48);
            this.picApp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picApp.TabIndex = 5;
            this.picApp.TabStop = false;
            this.picApp.Click += new System.EventHandler(this.picApp_Click);
            // 
            // picDesktop
            // 
            this.picDesktop.Image = kwm.KwmAppControls.Properties.Resources.Flip3D_64x64;
            this.picDesktop.Location = new System.Drawing.Point(76, 134);
            this.picDesktop.Name = "picDesktop";
            this.picDesktop.Size = new System.Drawing.Size(48, 39);
            this.picDesktop.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picDesktop.TabIndex = 4;
            this.picDesktop.TabStop = false;
            this.picDesktop.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // NewSession1
            // 
            this.Controls.Add(this.picApp);
            this.Controls.Add(this.radioApp);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.radioDesk);
            this.Controls.Add(this.picDesktop);
            this.Name = "NewSession1";
            this.Size = new System.Drawing.Size(432, 323);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.NewSession1_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.NewSession1_SetActive);
            this.Controls.SetChildIndex(this.picDesktop, 0);
            this.Controls.SetChildIndex(this.radioDesk, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.radioApp, 0);
            this.Controls.SetChildIndex(this.picApp, 0);
            this.Controls.SetChildIndex(this.Banner, 0);
            ((System.ComponentModel.ISupportInitialize)(this.picApp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDesktop)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioDesk;
        private System.Windows.Forms.RadioButton radioApp;
        private System.Windows.Forms.PictureBox picDesktop;
        private System.Windows.Forms.PictureBox picApp;
    }
}
