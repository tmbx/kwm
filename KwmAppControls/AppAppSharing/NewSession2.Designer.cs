namespace kwm.KwmAppControls
{
    partial class NewSession2
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
            this.lstApps = new System.Windows.Forms.ListView();
            this.Application = new System.Windows.Forms.ColumnHeader();
            this.label1 = new System.Windows.Forms.Label();
            this.listUpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // Banner
            // 
            this.Banner.Size = new System.Drawing.Size(432, 50);
            this.Banner.Subtitle = "";
            this.Banner.Title = "Which application would you like to share?";
            // 
            // lstApps
            // 
            this.lstApps.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Application});
            this.lstApps.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstApps.FullRowSelect = true;
            this.lstApps.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lstApps.HideSelection = false;
            this.lstApps.Location = new System.Drawing.Point(12, 100);
            this.lstApps.MultiSelect = false;
            this.lstApps.Name = "lstApps";
            this.lstApps.Size = new System.Drawing.Size(408, 209);
            this.lstApps.TabIndex = 1;
            this.lstApps.UseCompatibleStateImageBehavior = false;
            this.lstApps.View = System.Windows.Forms.View.Details;
            this.lstApps.SelectedIndexChanged += new System.EventHandler(this.lstApps_SelectedIndexChanged);
            this.lstApps.DoubleClick += new System.EventHandler(this.lstApps_DoubleClick);
            this.lstApps.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AppList_KeyDown);
            // 
            // Application
            // 
            this.Application.Text = "Application";
            this.Application.Width = 482;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(269, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Please select which application you would like to share:";
            // 
            // listUpdateTimer
            // 
            this.listUpdateTimer.Interval = 1000;
            this.listUpdateTimer.Tick += new System.EventHandler(this.OnListUpdateTimerTick);
            // 
            // NewSession2
            // 
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lstApps);
            this.Name = "NewSession2";
            this.Size = new System.Drawing.Size(432, 323);
            this.WizardBack += new Wizard.UI.WizardPageEventHandler(this.NewSession2_WizardBack);
            this.WizardNext += new Wizard.UI.WizardPageEventHandler(this.NewSession2_WizardNext);
            this.SetActive += new System.ComponentModel.CancelEventHandler(this.NewSession2_SetActive);
            this.Controls.SetChildIndex(this.Banner, 0);
            this.Controls.SetChildIndex(this.lstApps, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lstApps;
        private System.Windows.Forms.ColumnHeader Application;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer listUpdateTimer;
    }
}
