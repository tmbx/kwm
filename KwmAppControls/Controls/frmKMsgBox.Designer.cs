namespace kwm.Utils
{
        partial class frmKMsgBox
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
                this.components = new System.ComponentModel.Container();
                this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
                this.PromptImage = new System.Windows.Forms.PictureBox();
                this.ButtonsHolder = new System.Windows.Forms.FlowLayoutPanel();
                this.lblText = new System.Windows.Forms.Label();
                this.timer = new System.Windows.Forms.Timer(this.components);
                this.tableLayoutPanel.SuspendLayout();
                ((System.ComponentModel.ISupportInitialize)(this.PromptImage)).BeginInit();
                this.SuspendLayout();
                // 
                // tableLayoutPanel
                // 
                this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                            | System.Windows.Forms.AnchorStyles.Left)
                            | System.Windows.Forms.AnchorStyles.Right)));
                this.tableLayoutPanel.ColumnCount = 2;
                this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 52F));
                this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
                this.tableLayoutPanel.Controls.Add(this.PromptImage, 0, 0);
                this.tableLayoutPanel.Controls.Add(this.ButtonsHolder, 0, 1);
                this.tableLayoutPanel.Controls.Add(this.lblText, 1, 0);
                this.tableLayoutPanel.Location = new System.Drawing.Point(10, 10);
                this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(10);
                this.tableLayoutPanel.Name = "tableLayoutPanel";
                this.tableLayoutPanel.RowCount = 2;
                this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
                this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
                this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
                this.tableLayoutPanel.Size = new System.Drawing.Size(175, 156);
                this.tableLayoutPanel.TabIndex = 0;
                // 
                // PromptImage
                // 
                this.PromptImage.Location = new System.Drawing.Point(3, 3);
                this.PromptImage.Margin = new System.Windows.Forms.Padding(3, 3, 10, 10);
                this.PromptImage.Name = "PromptImage";
                this.PromptImage.Size = new System.Drawing.Size(38, 38);
                this.PromptImage.TabIndex = 1;
                this.PromptImage.TabStop = false;
                // 
                // ButtonsHolder
                // 
                this.ButtonsHolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                            | System.Windows.Forms.AnchorStyles.Right)));
                this.tableLayoutPanel.SetColumnSpan(this.ButtonsHolder, 2);
                this.ButtonsHolder.Location = new System.Drawing.Point(0, 136);
                this.ButtonsHolder.Margin = new System.Windows.Forms.Padding(0);
                this.ButtonsHolder.Name = "ButtonsHolder";
                this.ButtonsHolder.Size = new System.Drawing.Size(175, 20);
                this.ButtonsHolder.TabIndex = 2;
                // 
                // lblText
                // 
                this.lblText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                            | System.Windows.Forms.AnchorStyles.Left)
                            | System.Windows.Forms.AnchorStyles.Right)));
                this.lblText.AutoSize = true;
                this.lblText.Location = new System.Drawing.Point(55, 3);
                this.lblText.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
                this.lblText.Name = "lblText";
                this.lblText.Size = new System.Drawing.Size(117, 123);
                this.lblText.TabIndex = 0;
                this.lblText.Text = "label1";
                // 
                // timer
                // 
                this.timer.Tick += new System.EventHandler(this.timer_Tick);
                // 
                // frmKMsgBox
                // 
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.ClientSize = new System.Drawing.Size(192, 172);
                this.Controls.Add(this.tableLayoutPanel);
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.Name = "frmKMsgBox";
                this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
                this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                this.Text = "frmKMsgBox";
                this.tableLayoutPanel.ResumeLayout(false);
                this.tableLayoutPanel.PerformLayout();
                ((System.ComponentModel.ISupportInitialize)(this.PromptImage)).EndInit();
                this.ResumeLayout(false);

            }

            #endregion

            private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
            private System.Windows.Forms.Label lblText;
            private System.Windows.Forms.PictureBox PromptImage;
            private System.Windows.Forms.FlowLayoutPanel ButtonsHolder;
            private System.Windows.Forms.Timer timer;
        }
}