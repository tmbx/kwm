namespace kwm.KwmAppControls
{
    partial class AppChatboxControl
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
            this.txtChatMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.ChatWindowContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.Copy = new System.Windows.Forms.ToolStripMenuItem();
            this.SelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.MsgBoardTitle = new System.Windows.Forms.Label();
            this.rtbChatWindow = new System.Windows.Forms.RichTextBox();
            this.ChatWindowContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtChatMessage
            // 
            this.txtChatMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtChatMessage.Location = new System.Drawing.Point(4, 119);
            this.txtChatMessage.Margin = new System.Windows.Forms.Padding(0);
            this.txtChatMessage.Multiline = true;
            this.txtChatMessage.Name = "txtChatMessage";
            this.txtChatMessage.Size = new System.Drawing.Size(588, 20);
            this.txtChatMessage.TabIndex = 1;
            this.txtChatMessage.TextChanged += new System.EventHandler(this.txtChatMessage_TextChanged);
            this.txtChatMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtChatMessage_KeyDown);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(595, 119);
            this.btnSend.Margin = new System.Windows.Forms.Padding(0);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(63, 20);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // ChatWindowContextMenu
            // 
            this.ChatWindowContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Copy,
            this.SelectAll});
            this.ChatWindowContextMenu.Name = "CutCopyPasteContextMenu";
            this.ChatWindowContextMenu.Size = new System.Drawing.Size(129, 48);
            this.ChatWindowContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.CutCopyPasteContextMenu_Opening);
            // 
            // Copy
            // 
            this.Copy.Name = "Copy";
            this.Copy.Size = new System.Drawing.Size(128, 22);
            this.Copy.Text = "Copy";
            this.Copy.Click += new System.EventHandler(this.Copy_Click);
            // 
            // SelectAll
            // 
            this.SelectAll.Name = "SelectAll";
            this.SelectAll.Size = new System.Drawing.Size(128, 22);
            this.SelectAll.Text = "Select All";
            this.SelectAll.Click += new System.EventHandler(this.SelectAll_Click);
            // 
            // MsgBoardTitle
            // 
            this.MsgBoardTitle.AutoSize = true;
            this.MsgBoardTitle.Location = new System.Drawing.Point(4, 5);
            this.MsgBoardTitle.Name = "MsgBoardTitle";
            this.MsgBoardTitle.Size = new System.Drawing.Size(81, 13);
            this.MsgBoardTitle.TabIndex = 5;
            this.MsgBoardTitle.Text = "Message Board";
            this.MsgBoardTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // rtbChatWindow
            // 
            this.rtbChatWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbChatWindow.ContextMenuStrip = this.ChatWindowContextMenu;
            this.rtbChatWindow.Location = new System.Drawing.Point(4, 22);
            this.rtbChatWindow.Name = "rtbChatWindow";
            this.rtbChatWindow.Size = new System.Drawing.Size(654, 96);
            this.rtbChatWindow.TabIndex = 6;
            this.rtbChatWindow.Text = "";
            this.rtbChatWindow.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.txtChatWindow_LinkClicked);
            this.rtbChatWindow.SizeChanged += new System.EventHandler(this.txtChatWindow_SizeChanged);
            this.rtbChatWindow.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtChatWindow_KeyPress);
            // 
            // AppChatboxControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Controls.Add(this.rtbChatWindow);
            this.Controls.Add(this.MsgBoardTitle);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtChatMessage);
            this.Name = "AppChatboxControl";
            this.Size = new System.Drawing.Size(661, 139);
            this.ChatWindowContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtChatMessage;
        private System.Windows.Forms.Label MsgBoardTitle;
        private System.Windows.Forms.ContextMenuStrip ChatWindowContextMenu;
        private System.Windows.Forms.ToolStripMenuItem Copy;
        private System.Windows.Forms.ToolStripMenuItem SelectAll;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.RichTextBox rtbChatWindow;
    }
}
