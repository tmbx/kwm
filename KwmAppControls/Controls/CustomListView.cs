using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace kwm.Utils
{
    /// <summary>
    /// This is a custom list view to use in collaboration with 
    /// customListItem and customListViewSubItem
    /// 
    /// The only difference is that when drawing items and subitems the 
    /// control test the type of the item and subitem. If it's a custom one, 
    /// the control call the method drawItem of that custom item. If not 
    /// draw the item normally
    /// </summary>
    class CustomListView : ListView
    {
        
        public CustomListView() : base()
        {
            this.OwnerDraw = false;
        }
        /*
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            //base.OnDrawItem(e);
            
            Type t = (new CustomListViewItem()).GetType();

            if (e.Item.GetType() == t || e.Item.GetType().BaseType == t)
            {
                ((CustomListViewItem)e.Item).drawItem(e.Graphics, e.Item.GetBounds(ItemBoundsPortion.Entire));
            }
            else
            {
                base.OnDrawItem(e); ;
            }
             
        }
        */
        /*protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex > 0)
            {
                Type t = (new CustomListViewSubItem()).GetType();

                if (e.SubItem.GetType() == t)
                {
                    ((CustomListViewSubItem)e.SubItem).drawItem(e.Graphics, e.SubItem.Bounds);
                }
                else
                {
                    base.OnDrawSubItem(e);
                }
            }
        }*/
        /*
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                // Store the column text alignment, letting it default
                // to Left if it has not been set to Center or Right.
                switch (e.Header.TextAlign)
                {
                    case HorizontalAlignment.Center:
                        sf.Alignment = StringAlignment.Center;
                        break;
                    case HorizontalAlignment.Right:
                        sf.Alignment = StringAlignment.Far;
                        break;
                }

                // Draw the standard header background.
                e.DrawBackground();

                sf.LineAlignment = StringAlignment.Center;

                sf.FormatFlags = StringFormatFlags.NoWrap;
                // Draw the header text.
                e.Graphics.DrawString(e.Header.Text, e.Font, new SolidBrush(e.ForeColor), e.Bounds, sf);
            }
        }*/
    }
}
