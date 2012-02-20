using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Drawing;
using Tbx.Utils;

namespace kwm.Utils
{
    public class KTreeView : TreeView
    {
        /////////////////////////////////////////////
        // Stuff to set scroll position.

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int GetScrollPos(int hWnd, int nBar);

        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        private const int SB_HORZ = 0x0;
        private const int SB_VERT = 0x1;
        
        public Point GetScrollPos()
        {
            return new Point(
                GetScrollPos((int)this.Handle, SB_HORZ),
                GetScrollPos((int)this.Handle, SB_VERT));
        }

        public void SetScrollPos(Point scrollPosition)
        {
            SetScrollPos((IntPtr)this.Handle, SB_HORZ, scrollPosition.X, true);
            SetScrollPos((IntPtr)this.Handle, SB_VERT, scrollPosition.Y, true);
        }
        /////////////////////////////////////


        private TreeNode m_clickedNode;
        private TreeNode m_tempNode;

        /// <summary>
        /// Set to true if you want to prevent the context menu
        /// from being shown.
        /// </summary>
        private bool m_preventedRightClick = false;

        private bool m_allowContextMenuOnNothing = false;
        [Category("Show ContextMenuStrip on empty click")]
        [Description("Indicates whether the ContextMenuStrip (if present) should be shown even if the click was done on an empty area of the TreeView.")]
        public bool AllowContextMenuOnNothing
        {
            get
            {
                return m_allowContextMenuOnNothing;
            }
            set
            {
                m_allowContextMenuOnNothing = value;
            }
        }

        /// <summary>
        /// Gets or sets whether or not the TV context menu
        /// should be shown if a right click is done on an
        /// empty area of the treeview.
        /// </summary>
        public TreeNode ClickedNode
        {
            get
            {
                return m_clickedNode;
            }
        }

        /// <summary>
        /// Set clickedNode when a right-click occurs 
        /// anywhere in the treeview
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
#if true
            if (e.Button == MouseButtons.Right)
            {
                if (this.GetNodeAt(e.Location) != null ||
                    AllowContextMenuOnNothing)
                {
                    Logging.Log(1, "OnMouseDown, right button used.");
                    // m_clickedNode can be set to null if 
                    // no node is under this click.
                    m_clickedNode = this.GetNodeAt(e.Location);
                    m_tempNode = this.SelectedNode;
                    this.SelectedNode = m_clickedNode;
                    m_preventedRightClick = false;
                }
                else
                {
                    m_preventedRightClick = true;
                }
            }
#endif
            
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Only allow double click events when the LEFT button is used.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
#if true
            if (e.Button == MouseButtons.Left)
#endif
                base.OnMouseDoubleClick(e);
        }

        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            m_clickedNode = e.Node;

            base.OnAfterSelect(e);
        }

        /// <summary>
        /// See if we must show the context menu in case
        /// of a click on an empty space.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
#if true
            int WM_CONTEXTMENU = 0x7b;

            if (m.Msg == WM_CONTEXTMENU)
            {
                // Prevent context menu if click was on nothing and 
                // we don't want menus in this case.
                if (m_preventedRightClick)
                {
                    return;
                }
            }
#endif
            base.WndProc(ref m);
        }

        protected override void OnContextMenuStripChanged(EventArgs e)
        {
#if true
            if (this.ContextMenuStrip != null)
                this.ContextMenuStrip.Closing += HandleOnContextMenuStripClosing;
#endif
            base.OnContextMenuStripChanged(e);
        }

        private void HandleOnContextMenuStripClosing(Object sender, ToolStripDropDownClosingEventArgs args)
        {
#if true
            // Revert the SelectedNode to what was selected before only if no
            // menu item was selected.
            if (args.CloseReason != ToolStripDropDownCloseReason.ItemClicked)
            {
                if (m_tempNode != null)
                {
                    Logging.Log(1, "HandleOnContextMenuStripClosing. Reverting Clicked annd SelectedNode to " + m_tempNode.Name);
                    this.SelectedNode = m_tempNode;
                    //m_clickedNode = m_tempNode;
                    m_tempNode = null;
                }
            }
#endif
        }
    }
}
