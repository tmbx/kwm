namespace kwm.KwmAppControls
{
    partial class FrmConflictResolution
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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.radioToServer = new System.Windows.Forms.RadioButton();
            this.radioToLocal = new System.Windows.Forms.RadioButton();
            this.picToLocal = new System.Windows.Forms.PictureBox();
            this.picToServer = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picToLocal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picToServer)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(274, 536);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.EnabledChanged += new System.EventHandler(this.btnOK_EnabledChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(378, 536);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // radioToServer
            // 
            this.radioToServer.AutoSize = true;
            this.radioToServer.Location = new System.Drawing.Point(30, 174);
            this.radioToServer.Name = "radioToServer";
            this.radioToServer.Size = new System.Drawing.Size(14, 13);
            this.radioToServer.TabIndex = 5;
            this.radioToServer.TabStop = true;
            this.radioToServer.UseVisualStyleBackColor = true;
            this.radioToServer.CheckedChanged += new System.EventHandler(this.radioToServer_CheckedChanged);
            // 
            // radioToLocal
            // 
            this.radioToLocal.AutoSize = true;
            this.radioToLocal.Location = new System.Drawing.Point(30, 408);
            this.radioToLocal.Name = "radioToLocal";
            this.radioToLocal.Size = new System.Drawing.Size(14, 13);
            this.radioToLocal.TabIndex = 6;
            this.radioToLocal.TabStop = true;
            this.radioToLocal.UseVisualStyleBackColor = true;
            this.radioToLocal.CheckedChanged += new System.EventHandler(this.radioToLocal_CheckedChanged);
            // 
            // picToLocal
            // 
            this.picToLocal.Location = new System.Drawing.Point(65, 313);
            this.picToLocal.Name = "picToLocal";
            this.picToLocal.Size = new System.Drawing.Size(388, 207);
            this.picToLocal.TabIndex = 4;
            this.picToLocal.TabStop = false;
            this.picToLocal.Click += new System.EventHandler(this.picToLocal_Click);
            // 
            // picToServer
            // 
            this.picToServer.Location = new System.Drawing.Point(65, 95);
            this.picToServer.Name = "picToServer";
            this.picToServer.Size = new System.Drawing.Size(388, 207);
            this.picToServer.TabIndex = 3;
            this.picToServer.TabStop = false;
            this.picToServer.Click += new System.EventHandler(this.picToServer_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(27, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(426, 66);
            this.label1.TabIndex = 8;
            this.label1.Text = "label1";
            // 
            // FrmConflictResolution
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(469, 569);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.radioToLocal);
            this.Controls.Add(this.radioToServer);
            this.Controls.Add(this.picToLocal);
            this.Controls.Add(this.picToServer);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FrmConflictResolution";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Conflict Resolution";
            ((System.ComponentModel.ISupportInitialize)(this.picToLocal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picToServer)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.PictureBox picToServer;
        private System.Windows.Forms.PictureBox picToLocal;
        private System.Windows.Forms.RadioButton radioToServer;
        private System.Windows.Forms.RadioButton radioToLocal;
        private System.Windows.Forms.Label label1;
    }
}