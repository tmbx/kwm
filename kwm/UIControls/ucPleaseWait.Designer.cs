namespace kwm
{
    partial class ucPleaseWait
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
            this.picWait = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picWait)).BeginInit();
            this.SuspendLayout();
            // 
            // picWait
            // 
            this.picWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picWait.Image = global::kwm.Properties.Resources.please_wait;
            this.picWait.Location = new System.Drawing.Point(0, 0);
            this.picWait.Name = "picWait";
            this.picWait.Size = new System.Drawing.Size(32, 32);
            this.picWait.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picWait.TabIndex = 4;
            this.picWait.TabStop = false;
            // 
            // ucPleaseWait
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.picWait);
            this.Name = "ucPleaseWait";
            this.Size = new System.Drawing.Size(32, 32);
            ((System.ComponentModel.ISupportInitialize)(this.picWait)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picWait;
    }
}
