namespace kwm
{
    partial class frmOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmOptions));
            this.optionTabs = new System.Windows.Forms.TabControl();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.lblRestart = new System.Windows.Forms.Label();
            this.btnStorePathBrowse = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.rbFileStoreCustom = new System.Windows.Forms.RadioButton();
            this.rbFileStoreMyDocs = new System.Windows.Forms.RadioButton();
            this.txtCustomStorePath = new System.Windows.Forms.TextBox();
            this.boxEvent = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtNotifDuration = new System.Windows.Forms.TextBox();
            this.chkShowNotification = new System.Windows.Forms.CheckBox();
            this.tabOTC = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.grpAttachmentManagement = new System.Windows.Forms.GroupBox();
            this.lblMb = new System.Windows.Forms.Label();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.txtThreshold = new System.Windows.Forms.NumericUpDown();
            this.radAMAlwaysAsk = new System.Windows.Forms.RadioButton();
            this.radAMNeverUse = new System.Windows.Forms.RadioButton();
            this.radAMAlwaysUse = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.chkOtcEnabled = new System.Windows.Forms.CheckBox();
            this.chkSkurlEnabled = new System.Windows.Forms.CheckBox();
            this.tabDebugging = new System.Windows.Forms.TabPage();
            this.btnViewKcsLogs = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkUseCustomKas = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtKasPort = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtKasAddr = new System.Windows.Forms.TextBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.cboLogLevel = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.chkKppMsoLogging = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ktlsDebug = new System.Windows.Forms.RadioButton();
            this.ktlsMin = new System.Windows.Forms.RadioButton();
            this.ktlsNone = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkEnableDebugging = new System.Windows.Forms.CheckBox();
            this.chkLogToFile = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.CustomPathBrowseDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.optionTabs.SuspendLayout();
            this.tabConfig.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.boxEvent.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabOTC.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.grpAttachmentManagement.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtThreshold)).BeginInit();
            this.tabDebugging.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // optionTabs
            // 
            this.optionTabs.Controls.Add(this.tabConfig);
            this.optionTabs.Controls.Add(this.tabOTC);
            this.optionTabs.Controls.Add(this.tabDebugging);
            resources.ApplyResources(this.optionTabs, "optionTabs");
            this.optionTabs.Name = "optionTabs";
            this.optionTabs.SelectedIndex = 0;
            // 
            // tabConfig
            // 
            this.tabConfig.Controls.Add(this.groupBox8);
            this.tabConfig.Controls.Add(this.boxEvent);
            resources.ApplyResources(this.tabConfig, "tabConfig");
            this.tabConfig.Name = "tabConfig";
            this.tabConfig.UseVisualStyleBackColor = true;
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.lblRestart);
            this.groupBox8.Controls.Add(this.btnStorePathBrowse);
            this.groupBox8.Controls.Add(this.label4);
            this.groupBox8.Controls.Add(this.rbFileStoreCustom);
            this.groupBox8.Controls.Add(this.rbFileStoreMyDocs);
            this.groupBox8.Controls.Add(this.txtCustomStorePath);
            resources.ApplyResources(this.groupBox8, "groupBox8");
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.TabStop = false;
            // 
            // lblRestart
            // 
            resources.ApplyResources(this.lblRestart, "lblRestart");
            this.lblRestart.ForeColor = System.Drawing.Color.Red;
            this.lblRestart.Name = "lblRestart";
            // 
            // btnStorePathBrowse
            // 
            resources.ApplyResources(this.btnStorePathBrowse, "btnStorePathBrowse");
            this.btnStorePathBrowse.Name = "btnStorePathBrowse";
            this.btnStorePathBrowse.UseVisualStyleBackColor = true;
            this.btnStorePathBrowse.Click += new System.EventHandler(this.btnStorePathBrowse_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // rbFileStoreCustom
            // 
            resources.ApplyResources(this.rbFileStoreCustom, "rbFileStoreCustom");
            this.rbFileStoreCustom.Name = "rbFileStoreCustom";
            this.rbFileStoreCustom.UseVisualStyleBackColor = true;
            // 
            // rbFileStoreMyDocs
            // 
            resources.ApplyResources(this.rbFileStoreMyDocs, "rbFileStoreMyDocs");
            this.rbFileStoreMyDocs.Checked = true;
            this.rbFileStoreMyDocs.Name = "rbFileStoreMyDocs";
            this.rbFileStoreMyDocs.TabStop = true;
            this.rbFileStoreMyDocs.UseVisualStyleBackColor = true;
            this.rbFileStoreMyDocs.CheckedChanged += new System.EventHandler(this.rbFileStoreMyDocs_CheckedChanged);
            // 
            // txtCustomStorePath
            // 
            resources.ApplyResources(this.txtCustomStorePath, "txtCustomStorePath");
            this.txtCustomStorePath.Name = "txtCustomStorePath";
            // 
            // boxEvent
            // 
            this.boxEvent.Controls.Add(this.pictureBox1);
            this.boxEvent.Controls.Add(this.label5);
            this.boxEvent.Controls.Add(this.txtNotifDuration);
            this.boxEvent.Controls.Add(this.chkShowNotification);
            resources.ApplyResources(this.boxEvent, "boxEvent");
            this.boxEvent.Name = "boxEvent";
            this.boxEvent.TabStop = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::kwm.Properties.Resources.notificationScreenShot;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // txtNotifDuration
            // 
            resources.ApplyResources(this.txtNotifDuration, "txtNotifDuration");
            this.txtNotifDuration.Name = "txtNotifDuration";
            // 
            // chkShowNotification
            // 
            resources.ApplyResources(this.chkShowNotification, "chkShowNotification");
            this.chkShowNotification.Name = "chkShowNotification";
            this.chkShowNotification.UseVisualStyleBackColor = true;
            this.chkShowNotification.CheckedChanged += new System.EventHandler(this.chkShowNotification_CheckedChanged);
            // 
            // tabOTC
            // 
            this.tabOTC.Controls.Add(this.groupBox5);
            resources.ApplyResources(this.tabOTC, "tabOTC");
            this.tabOTC.Name = "tabOTC";
            this.tabOTC.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.grpAttachmentManagement);
            this.groupBox5.Controls.Add(this.label6);
            this.groupBox5.Controls.Add(this.chkOtcEnabled);
            this.groupBox5.Controls.Add(this.chkSkurlEnabled);
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // grpAttachmentManagement
            // 
            this.grpAttachmentManagement.Controls.Add(this.lblMb);
            this.grpAttachmentManagement.Controls.Add(this.lblThreshold);
            this.grpAttachmentManagement.Controls.Add(this.txtThreshold);
            this.grpAttachmentManagement.Controls.Add(this.radAMAlwaysAsk);
            this.grpAttachmentManagement.Controls.Add(this.radAMNeverUse);
            this.grpAttachmentManagement.Controls.Add(this.radAMAlwaysUse);
            resources.ApplyResources(this.grpAttachmentManagement, "grpAttachmentManagement");
            this.grpAttachmentManagement.Name = "grpAttachmentManagement";
            this.grpAttachmentManagement.TabStop = false;
            // 
            // lblMb
            // 
            resources.ApplyResources(this.lblMb, "lblMb");
            this.lblMb.Name = "lblMb";
            // 
            // lblThreshold
            // 
            resources.ApplyResources(this.lblThreshold, "lblThreshold");
            this.lblThreshold.Name = "lblThreshold";
            // 
            // txtThreshold
            // 
            this.txtThreshold.Increment = new decimal(new int[] {
            500,
            0,
            0,
            0});
            resources.ApplyResources(this.txtThreshold, "txtThreshold");
            this.txtThreshold.Maximum = new decimal(new int[] {
            -1486618625,
            232830643,
            0,
            0});
            this.txtThreshold.Name = "txtThreshold";
            this.txtThreshold.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // radAMAlwaysAsk
            // 
            resources.ApplyResources(this.radAMAlwaysAsk, "radAMAlwaysAsk");
            this.radAMAlwaysAsk.Name = "radAMAlwaysAsk";
            this.radAMAlwaysAsk.TabStop = true;
            this.radAMAlwaysAsk.UseVisualStyleBackColor = true;
            // 
            // radAMNeverUse
            // 
            resources.ApplyResources(this.radAMNeverUse, "radAMNeverUse");
            this.radAMNeverUse.Name = "radAMNeverUse";
            this.radAMNeverUse.TabStop = true;
            this.radAMNeverUse.UseVisualStyleBackColor = true;
            this.radAMNeverUse.CheckedChanged += new System.EventHandler(this.radAMNeverUse_CheckedChanged);
            // 
            // radAMAlwaysUse
            // 
            resources.ApplyResources(this.radAMAlwaysUse, "radAMAlwaysUse");
            this.radAMAlwaysUse.Name = "radAMAlwaysUse";
            this.radAMAlwaysUse.TabStop = true;
            this.radAMAlwaysUse.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // chkOtcEnabled
            // 
            resources.ApplyResources(this.chkOtcEnabled, "chkOtcEnabled");
            this.chkOtcEnabled.Name = "chkOtcEnabled";
            this.chkOtcEnabled.UseVisualStyleBackColor = true;
            this.chkOtcEnabled.CheckedChanged += new System.EventHandler(this.chkOtcEnabled_CheckedChanged);
            // 
            // chkSkurlEnabled
            // 
            resources.ApplyResources(this.chkSkurlEnabled, "chkSkurlEnabled");
            this.chkSkurlEnabled.Name = "chkSkurlEnabled";
            this.chkSkurlEnabled.UseVisualStyleBackColor = true;
            this.chkSkurlEnabled.CheckedChanged += new System.EventHandler(this.chkSkurlEnabled_CheckedChanged);
            // 
            // tabDebugging
            // 
            this.tabDebugging.Controls.Add(this.btnViewKcsLogs);
            this.tabDebugging.Controls.Add(this.groupBox3);
            this.tabDebugging.Controls.Add(this.groupBox7);
            this.tabDebugging.Controls.Add(this.groupBox4);
            this.tabDebugging.Controls.Add(this.groupBox2);
            this.tabDebugging.Controls.Add(this.groupBox1);
            resources.ApplyResources(this.tabDebugging, "tabDebugging");
            this.tabDebugging.Name = "tabDebugging";
            this.tabDebugging.UseVisualStyleBackColor = true;
            // 
            // btnViewKcsLogs
            // 
            resources.ApplyResources(this.btnViewKcsLogs, "btnViewKcsLogs");
            this.btnViewKcsLogs.Name = "btnViewKcsLogs";
            this.btnViewKcsLogs.UseVisualStyleBackColor = true;
            this.btnViewKcsLogs.Click += new System.EventHandler(this.btnViewKcsLogs_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.chkUseCustomKas);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.txtKasPort);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.txtKasAddr);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // chkUseCustomKas
            // 
            resources.ApplyResources(this.chkUseCustomKas, "chkUseCustomKas");
            this.chkUseCustomKas.Name = "chkUseCustomKas";
            this.chkUseCustomKas.UseVisualStyleBackColor = true;
            this.chkUseCustomKas.CheckedChanged += new System.EventHandler(this.chkUseCustomKas_CheckedChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // txtKasPort
            // 
            resources.ApplyResources(this.txtKasPort, "txtKasPort");
            this.txtKasPort.Name = "txtKasPort";
            this.txtKasPort.Validating += new System.ComponentModel.CancelEventHandler(this.txtKasPort_Validating);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // txtKasAddr
            // 
            resources.ApplyResources(this.txtKasAddr, "txtKasAddr");
            this.txtKasAddr.Name = "txtKasAddr";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.btnApply);
            this.groupBox7.Controls.Add(this.label3);
            this.groupBox7.Controls.Add(this.cboLogLevel);
            resources.ApplyResources(this.groupBox7, "groupBox7");
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.TabStop = false;
            // 
            // btnApply
            // 
            resources.ApplyResources(this.btnApply, "btnApply");
            this.btnApply.Name = "btnApply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // cboLogLevel
            // 
            this.cboLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLogLevel.FormattingEnabled = true;
            this.cboLogLevel.Items.AddRange(new object[] {
            resources.GetString("cboLogLevel.Items"),
            resources.GetString("cboLogLevel.Items1"),
            resources.GetString("cboLogLevel.Items2")});
            resources.ApplyResources(this.cboLogLevel, "cboLogLevel");
            this.cboLogLevel.Name = "cboLogLevel";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.chkKppMsoLogging);
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // chkKppMsoLogging
            // 
            resources.ApplyResources(this.chkKppMsoLogging, "chkKppMsoLogging");
            this.chkKppMsoLogging.Name = "chkKppMsoLogging";
            this.chkKppMsoLogging.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.ktlsDebug);
            this.groupBox2.Controls.Add(this.ktlsMin);
            this.groupBox2.Controls.Add(this.ktlsNone);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // ktlsDebug
            // 
            resources.ApplyResources(this.ktlsDebug, "ktlsDebug");
            this.ktlsDebug.Name = "ktlsDebug";
            this.ktlsDebug.UseVisualStyleBackColor = true;
            // 
            // ktlsMin
            // 
            resources.ApplyResources(this.ktlsMin, "ktlsMin");
            this.ktlsMin.Name = "ktlsMin";
            this.ktlsMin.TabStop = true;
            this.ktlsMin.UseVisualStyleBackColor = true;
            // 
            // ktlsNone
            // 
            resources.ApplyResources(this.ktlsNone, "ktlsNone");
            this.ktlsNone.Checked = true;
            this.ktlsNone.Name = "ktlsNone";
            this.ktlsNone.TabStop = true;
            this.ktlsNone.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkEnableDebugging);
            this.groupBox1.Controls.Add(this.chkLogToFile);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // chkEnableDebugging
            // 
            resources.ApplyResources(this.chkEnableDebugging, "chkEnableDebugging");
            this.chkEnableDebugging.Name = "chkEnableDebugging";
            this.chkEnableDebugging.UseVisualStyleBackColor = true;
            this.chkEnableDebugging.CheckedChanged += new System.EventHandler(this.chkEnableDebugging_CheckedChanged);
            // 
            // chkLogToFile
            // 
            resources.ApplyResources(this.chkLogToFile, "chkLogToFile");
            this.chkLogToFile.Name = "chkLogToFile";
            this.chkLogToFile.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // CustomPathBrowseDialog
            // 
            resources.ApplyResources(this.CustomPathBrowseDialog, "CustomPathBrowseDialog");
            // 
            // frmOptions
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.optionTabs);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmOptions";
            this.ShowInTaskbar = false;
            this.optionTabs.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.boxEvent.ResumeLayout(false);
            this.boxEvent.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabOTC.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.grpAttachmentManagement.ResumeLayout(false);
            this.grpAttachmentManagement.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtThreshold)).EndInit();
            this.tabDebugging.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl optionTabs;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TabPage tabDebugging;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton ktlsDebug;
        private System.Windows.Forms.RadioButton ktlsMin;
        private System.Windows.Forms.RadioButton ktlsNone;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkEnableDebugging;
        private System.Windows.Forms.CheckBox chkLogToFile;
        private System.Windows.Forms.GroupBox boxEvent;
        private System.Windows.Forms.CheckBox chkShowNotification;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox chkKppMsoLogging;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboLogLevel;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnViewKcsLogs;
        private System.Windows.Forms.TabPage tabConfig;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.RadioButton rbFileStoreMyDocs;
        private System.Windows.Forms.TextBox txtCustomStorePath;
        private System.Windows.Forms.Button btnStorePathBrowse;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton rbFileStoreCustom;
        private System.Windows.Forms.FolderBrowserDialog CustomPathBrowseDialog;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox chkUseCustomKas;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtKasPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtKasAddr;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtNotifDuration;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblRestart;
        private System.Windows.Forms.TabPage tabOTC;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox chkOtcEnabled;
        private System.Windows.Forms.CheckBox chkSkurlEnabled;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox grpAttachmentManagement;
        private System.Windows.Forms.RadioButton radAMAlwaysAsk;
        private System.Windows.Forms.RadioButton radAMNeverUse;
        private System.Windows.Forms.RadioButton radAMAlwaysUse;
        private System.Windows.Forms.NumericUpDown txtThreshold;
        private System.Windows.Forms.Label lblThreshold;
        private System.Windows.Forms.Label lblMb;
    }
}