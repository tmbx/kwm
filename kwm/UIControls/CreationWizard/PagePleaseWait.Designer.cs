namespace kwm
{
    partial class PagePleaseWait
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
            this.lblReason = new System.Windows.Forms.Label();
            this.ucPleaseWait1 = new kwm.ucPleaseWait();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(432, 29);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Operation in progress...";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(149, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Please wait...";
            // 
            // lblReason
            // 
            this.lblReason.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblReason.Location = new System.Drawing.Point(23, 115);
            this.lblReason.Name = "lblReason";
            this.lblReason.Size = new System.Drawing.Size(388, 42);
            this.lblReason.TabIndex = 4;
            this.lblReason.Text = "Looking up who needs a password";
            // 
            // ucPleaseWait1
            // 
            this.ucPleaseWait1.Location = new System.Drawing.Point(370, 42);
            this.ucPleaseWait1.Name = "ucPleaseWait1";
            this.ucPleaseWait1.Size = new System.Drawing.Size(32, 32);
            this.ucPleaseWait1.TabIndex = 5;
            // 
            // PagePleaseWait
            // 
            this.Controls.Add(this.ucPleaseWait1);
            this.Controls.Add(this.lblReason);
            this.Controls.Add(this.label1);
            this.Name = "PagePleaseWait";
            this.Size = new System.Drawing.Size(432, 186);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PagePleaseWait_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.lblReason, 0);
            this.Controls.SetChildIndex(this.ucPleaseWait1, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblReason;
        private kwm.ucPleaseWait ucPleaseWait1;



    }
}
