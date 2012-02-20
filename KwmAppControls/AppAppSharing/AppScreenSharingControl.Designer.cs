namespace kwm.KwmAppControls
{
    partial class AppScreenSharingControl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppScreenSharingControl));
            this.panelRunningSessions = new System.Windows.Forms.Panel();
            this.boxRunning = new System.Windows.Forms.GroupBox();
            this.splitLiveSession = new System.Windows.Forms.SplitContainer();
            this.boxMyRunning = new System.Windows.Forms.GroupBox();
            this.runningCtrl = new kwm.KwmAppControls.RunningOverlayControl();
            this.splitAlreadySharingWarning = new System.Windows.Forms.SplitContainer();
            this.lblHelp = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblStart = new System.Windows.Forms.Label();
            this.lblWsName = new System.Windows.Forms.LinkLabel();
            this.lblAlreadyDoingSomething2 = new System.Windows.Forms.Label();
            this.lblAlreadyDoingSomething1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.durationTimer = new System.Windows.Forms.Timer(this.components);
            this.boxRunning.SuspendLayout();
            this.splitLiveSession.Panel1.SuspendLayout();
            this.splitLiveSession.Panel2.SuspendLayout();
            this.splitLiveSession.SuspendLayout();
            this.boxMyRunning.SuspendLayout();
            this.splitAlreadySharingWarning.Panel1.SuspendLayout();
            this.splitAlreadySharingWarning.Panel2.SuspendLayout();
            this.splitAlreadySharingWarning.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelRunningSessions
            // 
            this.panelRunningSessions.AutoScroll = true;
            this.panelRunningSessions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRunningSessions.Location = new System.Drawing.Point(3, 20);
            this.panelRunningSessions.Name = "panelRunningSessions";
            this.panelRunningSessions.Size = new System.Drawing.Size(365, 81);
            this.panelRunningSessions.TabIndex = 6;
            this.panelRunningSessions.Scroll += new System.Windows.Forms.ScrollEventHandler(this.panelRunningSessions_Scroll);
            this.panelRunningSessions.MouseClick += new System.Windows.Forms.MouseEventHandler(this.panelRunningSessions_MouseClick);
            // 
            // boxRunning
            // 
            this.boxRunning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.boxRunning.AutoSize = true;
            this.boxRunning.Controls.Add(this.panelRunningSessions);
            this.boxRunning.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxRunning.Location = new System.Drawing.Point(20, 19);
            this.boxRunning.Name = "boxRunning";
            this.boxRunning.Size = new System.Drawing.Size(371, 104);
            this.boxRunning.TabIndex = 6;
            this.boxRunning.TabStop = false;
            this.boxRunning.Text = "Screen Sharing Sessions";
            // 
            // splitLiveSession
            // 
            this.splitLiveSession.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitLiveSession.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitLiveSession.IsSplitterFixed = true;
            this.splitLiveSession.Location = new System.Drawing.Point(0, 0);
            this.splitLiveSession.Name = "splitLiveSession";
            this.splitLiveSession.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitLiveSession.Panel1
            // 
            this.splitLiveSession.Panel1.Controls.Add(this.boxMyRunning);
            // 
            // splitLiveSession.Panel2
            // 
            this.splitLiveSession.Panel2.Controls.Add(this.splitAlreadySharingWarning);
            this.splitLiveSession.Size = new System.Drawing.Size(572, 372);
            this.splitLiveSession.SplitterDistance = 96;
            this.splitLiveSession.TabIndex = 7;
            // 
            // boxMyRunning
            // 
            this.boxMyRunning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.boxMyRunning.Controls.Add(this.runningCtrl);
            this.boxMyRunning.Location = new System.Drawing.Point(20, 10);
            this.boxMyRunning.Name = "boxMyRunning";
            this.boxMyRunning.Size = new System.Drawing.Size(542, 73);
            this.boxMyRunning.TabIndex = 1;
            this.boxMyRunning.TabStop = false;
            this.boxMyRunning.Text = "My running session";
            // 
            // runningCtrl
            // 
            this.runningCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.runningCtrl.Location = new System.Drawing.Point(3, 16);
            this.runningCtrl.Name = "runningCtrl";
            this.runningCtrl.Size = new System.Drawing.Size(536, 54);
            this.runningCtrl.TabIndex = 0;
            // 
            // splitAlreadySharingWarning
            // 
            this.splitAlreadySharingWarning.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitAlreadySharingWarning.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitAlreadySharingWarning.IsSplitterFixed = true;
            this.splitAlreadySharingWarning.Location = new System.Drawing.Point(0, 0);
            this.splitAlreadySharingWarning.Name = "splitAlreadySharingWarning";
            // 
            // splitAlreadySharingWarning.Panel1
            // 
            this.splitAlreadySharingWarning.Panel1.Controls.Add(this.lblHelp);
            this.splitAlreadySharingWarning.Panel1.Controls.Add(this.pictureBox2);
            this.splitAlreadySharingWarning.Panel1.Controls.Add(this.btnStart);
            this.splitAlreadySharingWarning.Panel1.Controls.Add(this.lblStart);
            this.splitAlreadySharingWarning.Panel1.Controls.Add(this.boxRunning);
            this.splitAlreadySharingWarning.Panel1.SizeChanged += new System.EventHandler(this.splitAlreadySharingWarning_Panel1_SizeChanged);
            // 
            // splitAlreadySharingWarning.Panel2
            // 
            this.splitAlreadySharingWarning.Panel2.Controls.Add(this.lblWsName);
            this.splitAlreadySharingWarning.Panel2.Controls.Add(this.lblAlreadyDoingSomething2);
            this.splitAlreadySharingWarning.Panel2.Controls.Add(this.lblAlreadyDoingSomething1);
            this.splitAlreadySharingWarning.Panel2.Controls.Add(this.pictureBox1);
            this.splitAlreadySharingWarning.Size = new System.Drawing.Size(572, 272);
            this.splitAlreadySharingWarning.SplitterDistance = 407;
            this.splitAlreadySharingWarning.TabIndex = 0;
            // 
            // lblHelp
            // 
            this.lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHelp.AutoSize = true;
            this.lblHelp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHelp.Location = new System.Drawing.Point(45, 132);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(351, 13);
            this.lblHelp.TabIndex = 9;
            this.lblHelp.Text = "To connect to a Screen Sharing session, click on one of the above links.";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(23, 131);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(16, 16);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 8;
            this.pictureBox2.TabStop = false;
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.SystemColors.Control;
            this.btnStart.Image = ((System.Drawing.Image)(resources.GetObject("btnStart.Image")));
            this.btnStart.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnStart.Location = new System.Drawing.Point(167, 168);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(68, 61);
            this.btnStart.TabIndex = 4;
            this.btnStart.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // lblStart
            // 
            this.lblStart.AutoSize = true;
            this.lblStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStart.Location = new System.Drawing.Point(102, 232);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(216, 20);
            this.lblStart.TabIndex = 7;
            this.lblStart.Text = "Click to share your screen";
            this.lblStart.Click += new System.EventHandler(this.lblStart_Click);
            // 
            // lblWsName
            // 
            this.lblWsName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWsName.AutoEllipsis = true;
            this.lblWsName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWsName.Location = new System.Drawing.Point(13, 131);
            this.lblWsName.Name = "lblWsName";
            this.lblWsName.Size = new System.Drawing.Size(138, 16);
            this.lblWsName.TabIndex = 11;
            this.lblWsName.TabStop = true;
            this.lblWsName.Text = "RND-MNGT";
            this.lblWsName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblWsName.VisitedLinkColor = System.Drawing.Color.Blue;
            this.lblWsName.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblWsName_LinkClicked);
            // 
            // lblAlreadyDoingSomething2
            // 
            this.lblAlreadyDoingSomething2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAlreadyDoingSomething2.Location = new System.Drawing.Point(8, 157);
            this.lblAlreadyDoingSomething2.Name = "lblAlreadyDoingSomething2";
            this.lblAlreadyDoingSomething2.Size = new System.Drawing.Size(150, 43);
            this.lblAlreadyDoingSomething2.TabIndex = 10;
            this.lblAlreadyDoingSomething2.Text = "You will not be able to join or create a session while your other session is stil" +
                "l running.";
            // 
            // lblAlreadyDoingSomething1
            // 
            this.lblAlreadyDoingSomething1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAlreadyDoingSomething1.Location = new System.Drawing.Point(8, 80);
            this.lblAlreadyDoingSomething1.Name = "lblAlreadyDoingSomething1";
            this.lblAlreadyDoingSomething1.Size = new System.Drawing.Size(150, 43);
            this.lblAlreadyDoingSomething1.TabIndex = 8;
            this.lblAlreadyDoingSomething1.Text = "You are already hosting a Screen Sharing Session in the workspace:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::kwm.KwmAppControls.Properties.Resources.warning48x48;
            this.pictureBox1.Location = new System.Drawing.Point(53, 19);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(48, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // durationTimer
            // 
            this.durationTimer.Interval = 1000;
            this.durationTimer.Tick += new System.EventHandler(this.timerUpdateDuration_Tick);
            // 
            // AppScreenSharingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitLiveSession);
            this.Name = "AppScreenSharingControl";
            this.Size = new System.Drawing.Size(572, 372);
            this.boxRunning.ResumeLayout(false);
            this.splitLiveSession.Panel1.ResumeLayout(false);
            this.splitLiveSession.Panel2.ResumeLayout(false);
            this.splitLiveSession.ResumeLayout(false);
            this.boxMyRunning.ResumeLayout(false);
            this.splitAlreadySharingWarning.Panel1.ResumeLayout(false);
            this.splitAlreadySharingWarning.Panel1.PerformLayout();
            this.splitAlreadySharingWarning.Panel2.ResumeLayout(false);
            this.splitAlreadySharingWarning.Panel2.PerformLayout();
            this.splitAlreadySharingWarning.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Panel panelRunningSessions;
        private System.Windows.Forms.GroupBox boxRunning;
        private System.Windows.Forms.SplitContainer splitLiveSession;
        private RunningOverlayControl runningCtrl;
        private System.Windows.Forms.GroupBox boxMyRunning;
        private System.Windows.Forms.SplitContainer splitAlreadySharingWarning;
        private System.Windows.Forms.Label lblAlreadyDoingSomething1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer durationTimer;
        private System.Windows.Forms.Label lblAlreadyDoingSomething2;
        private System.Windows.Forms.LinkLabel lblWsName;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label lblHelp;
    }
}
