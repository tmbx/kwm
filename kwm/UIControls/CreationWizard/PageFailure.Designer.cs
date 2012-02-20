namespace kwm
{
    partial class PageFailure
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PageFailure));
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblExplain = new System.Windows.Forms.Label();
            this.rtbFailureReason = new System.Windows.Forms.RichTextBox();
            this.btnRunConfig = new System.Windows.Forms.Button();
            this.lblOp = new System.Windows.Forms.Label();
            this.btnRetry = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(444, 0);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Teambox creation failed";
            this.Banner.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(274, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 29);
            this.label2.TabIndex = 6;
            this.label2.Text = "Failed";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(384, 20);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // lblExplain
            // 
            this.lblExplain.Location = new System.Drawing.Point(20, 107);
            this.lblExplain.Name = "lblExplain";
            this.lblExplain.Size = new System.Drawing.Size(342, 34);
            this.lblExplain.TabIndex = 7;
            this.lblExplain.Text = "Your teambox could not be created. More details are available below.";
            // 
            // rtbFailureReason
            // 
            this.rtbFailureReason.Location = new System.Drawing.Point(23, 149);
            this.rtbFailureReason.Name = "rtbFailureReason";
            this.rtbFailureReason.ReadOnly = true;
            this.rtbFailureReason.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.rtbFailureReason.Size = new System.Drawing.Size(409, 55);
            this.rtbFailureReason.TabIndex = 8;
            this.rtbFailureReason.Text = "";
            // 
            // btnRunConfig
            // 
            this.btnRunConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRunConfig.Location = new System.Drawing.Point(357, 217);
            this.btnRunConfig.Name = "btnRunConfig";
            this.btnRunConfig.Size = new System.Drawing.Size(75, 23);
            this.btnRunConfig.TabIndex = 10;
            this.btnRunConfig.Text = "Configure...";
            this.btnRunConfig.UseVisualStyleBackColor = true;
            this.btnRunConfig.Click += new System.EventHandler(this.btnRunConfig_Click);
            // 
            // lblOp
            // 
            this.lblOp.AutoSize = true;
            this.lblOp.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOp.ForeColor = System.Drawing.Color.Black;
            this.lblOp.Location = new System.Drawing.Point(18, 43);
            this.lblOp.Name = "lblOp";
            this.lblOp.Size = new System.Drawing.Size(190, 25);
            this.lblOp.TabIndex = 13;
            this.lblOp.Text = "Teambox creation:";
            // 
            // btnRetry
            // 
            this.btnRetry.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRetry.Location = new System.Drawing.Point(357, 246);
            this.btnRetry.Name = "btnRetry";
            this.btnRetry.Size = new System.Drawing.Size(75, 23);
            this.btnRetry.TabIndex = 14;
            this.btnRetry.Text = "Retry";
            this.btnRetry.UseVisualStyleBackColor = true;
            this.btnRetry.Click += new System.EventHandler(this.btnRetry_Click);
            // 
            // PageFailure
            // 
            this.Controls.Add(this.btnRetry);
            this.Controls.Add(this.lblOp);
            this.Controls.Add(this.btnRunConfig);
            this.Controls.Add(this.rtbFailureReason);
            this.Controls.Add(this.lblExplain);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox1);
            this.Name = "PageFailure";
            this.Size = new System.Drawing.Size(444, 273);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.PageFailure_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.pictureBox1, 0);
            this.Controls.SetChildIndex(this.label2, 0);
            this.Controls.SetChildIndex(this.lblExplain, 0);
            this.Controls.SetChildIndex(this.rtbFailureReason, 0);
            this.Controls.SetChildIndex(this.btnRunConfig, 0);
            this.Controls.SetChildIndex(this.lblOp, 0);
            this.Controls.SetChildIndex(this.btnRetry, 0);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblExplain;
        private System.Windows.Forms.RichTextBox rtbFailureReason;
        private System.Windows.Forms.Button btnRunConfig;
        private System.Windows.Forms.Label lblOp;
        private System.Windows.Forms.Button btnRetry;


    }
}
