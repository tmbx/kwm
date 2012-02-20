namespace kwm
{
    partial class PageSuccess
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
            this.lblOp = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblExplanation = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(444, 29);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Teambox creation successful";
            // 
            // lblOp
            // 
            this.lblOp.AutoSize = true;
            this.lblOp.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOp.ForeColor = System.Drawing.Color.Black;
            this.lblOp.Location = new System.Drawing.Point(7, 78);
            this.lblOp.Name = "lblOp";
            this.lblOp.Size = new System.Drawing.Size(190, 25);
            this.lblOp.TabIndex = 6;
            this.lblOp.Text = "Teambox creation:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::kwm.Properties.Resources.GreenCheck2;
            this.pictureBox1.Location = new System.Drawing.Point(377, 55);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // lblExplanation
            // 
            this.lblExplanation.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExplanation.Location = new System.Drawing.Point(9, 152);
            this.lblExplanation.Name = "lblExplanation";
            this.lblExplanation.Size = new System.Drawing.Size(416, 78);
            this.lblExplanation.TabIndex = 7;
            this.lblExplanation.Text = "Congratulations! Your Teambox has been successfully created. \r\n\r\nPlease press on " +
                "Finish to return to your Teambox manager.";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.DarkGreen;
            this.label4.Location = new System.Drawing.Point(267, 78);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 25);
            this.label4.TabIndex = 10;
            this.label4.Text = "Success!";
            // 
            // PageSuccess
            // 
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lblExplanation);
            this.Controls.Add(this.lblOp);
            this.Controls.Add(this.pictureBox1);
            this.Name = "PageSuccess";
            this.Size = new System.Drawing.Size(444, 262);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PageSuccess_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.pictureBox1, 0);
            this.Controls.SetChildIndex(this.lblOp, 0);
            this.Controls.SetChildIndex(this.lblExplanation, 0);
            this.Controls.SetChildIndex(this.label4, 0);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblOp;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblExplanation;
        private System.Windows.Forms.Label label4;


    }
}
