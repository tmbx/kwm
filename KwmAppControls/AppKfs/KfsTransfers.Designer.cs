namespace kwm.KwmAppControls.AppKfs
{
    partial class KfsTransfers
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
            this.lvContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.lvTransfers = new kwm.Utils.CustomListView();
            this.SuspendLayout();
            // 
            // lvContextMenu
            // 
            this.lvContextMenu.Name = "lvContextMenu";
            this.lvContextMenu.Size = new System.Drawing.Size(61, 4);
            this.lvContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.lvContextMenu_ItemClicked);
            this.lvContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.lvContextMenu_Opening);
            this.lvContextMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.lvContextMenu_Closing);
            // 
            // lvTransfers
            // 
            this.lvTransfers.ContextMenuStrip = this.lvContextMenu;
            this.lvTransfers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvTransfers.FullRowSelect = true;
            this.lvTransfers.Location = new System.Drawing.Point(0, 0);
            this.lvTransfers.Name = "lvTransfers";
            this.lvTransfers.Size = new System.Drawing.Size(484, 119);
            this.lvTransfers.TabIndex = 1;
            this.lvTransfers.UseCompatibleStateImageBehavior = false;
            this.lvTransfers.View = System.Windows.Forms.View.Details;
            this.lvTransfers.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.lvTransfers_ColumnWidthChanging);
            // 
            // KfsTransfers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lvTransfers);
            this.Name = "KfsTransfers";
            this.Size = new System.Drawing.Size(484, 119);
            this.ResumeLayout(false);

        }

        #endregion

        private kwm.Utils.CustomListView lvTransfers;
        private System.Windows.Forms.ContextMenuStrip lvContextMenu;
    }
}
