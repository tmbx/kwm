using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace kwm.Utils
{
    /// <summary>
    /// This class represents an item of the Customlistview. 
    /// If not used in a Customlistview, the new features of this class will have no effect.
    /// This object can contain subitems like the base one. If subitems are of type 
    /// CustomListviewSubitem, they must be added to the CustomListviewItem by the custom 
    /// method AddSubItem. Otherwise, CustomListviewSubitem will act like a normal subitem.
    /// 
    /// To use an Icon, you must set an icon and the var showIcon to true. If showIcon is not
    /// set to true, the icon will remain hidden.
    /// 
    /// The icon is containd in a picturebox in order to catch events. Its not directly 
    /// the icon that is drawn on the listview, it is the picturebox.
    /// 
    /// The PictureBox is private and needs to use addIconOnclickEvent to add the event of user click. 
    /// If we need new events, please add the proper functions according to this template. 
    /// 
    /// *****
    /// Note that the custom item and subitem are almost the same but not in the same file 
    /// or base class, because in the future they can have different action or behavior. 
    /// SO DONT FORGET TO UPDATE MODIFICATION OR FIX IN CustomListViewSubItem
    /// *****
    /// </summary>
    public class CustomListViewItem : System.Windows.Forms.ListViewItem
    {
        //Constant right and left specifie if icon is at the right or the left of the text        
        public const int RIGHT = 0 ;
        public const int CENTER = 1;
        public const int LEFT = 2;

        public System.Drawing.Bitmap icon = null;
        public System.Windows.Forms.PictureBox iconContainer = new System.Windows.Forms.PictureBox();
        public bool showIcon = false;
       
        private int _iconPosition = RIGHT;
        public int iconPosition
        {
            get { return _iconPosition; }
            set
            {
                if (value < 0 && value > LEFT)
                {
                    _iconPosition = RIGHT;
                }
                else
                {
                    _iconPosition = value;
                }
            }
        }

        #region Contructor
        public CustomListViewItem()
        {                       
        }
        
        public CustomListViewItem(String text)
        {
            this.Text = text;
        }

        public CustomListViewItem(Bitmap icon)
        {
            this.icon = icon;
        }

        public CustomListViewItem(Bitmap icon, int position)
        {
            this.icon = icon;
            this.iconPosition = position;           
        }

        public CustomListViewItem(String text, Bitmap icon, int iconPosition)
        {
            this.Text = text;
            this.icon = icon;
            this.iconPosition = iconPosition;
        }

        public CustomListViewItem(String text, Bitmap icon, int iconPosition, bool showIcon)
        {
            this.Text = text;
            this.icon = icon;
            this.iconPosition = iconPosition;
            this.showIcon = showIcon;
        }
        #endregion 

        /// <summary>
        /// Method to draw the text and the icon 
        /// This method must be called by the OnPaint event of the list view.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="boundLimit"></param>
        public void drawItem(System.Drawing.Graphics g, System.Drawing.Rectangle boundLimit)
        {
            if (canUseIcon())
            {
                iconContainer.Size = new Size(boundLimit.Height-1, boundLimit.Height-1);
                iconContainer.BackgroundImage = new Bitmap(icon, boundLimit.Height - 1, boundLimit.Height - 1);

                switch (iconPosition)
                {
                    case RIGHT:
                        {
                            g.DrawString(Text, this.Font, new SolidBrush(ForeColor), boundLimit.X, boundLimit.Y);
                            int x = boundLimit.X + boundLimit.Width - boundLimit.Height;
                            int y = boundLimit.Y;                           
                            iconContainer.Location = new Point(x,y);                                                    
                        }
                        break;
                    case LEFT:
                        {                           
                            iconContainer.Location = new Point(boundLimit.X, boundLimit.Y);                         
                            g.DrawString(Text, this.Font, new System.Drawing.SolidBrush(ForeColor), boundLimit.X + boundLimit.Height, boundLimit.Y);
                        }
                        break;
                    default:
                        {
                            // For the center position (or any invalid value) 
                            // of the icon, we draw text normally, 
                            // and if the text is too long, too bad. 
                            // In fact the center position exists to be used without text.
                            g.DrawString(Text, this.Font, new System.Drawing.SolidBrush(ForeColor), boundLimit.X, boundLimit.Y);
                            iconContainer.Location = new Point(boundLimit.Width / 2 - boundLimit.Height, boundLimit.Y);
                        }
                        break;
                }
                this.ListView.Controls.Add(iconContainer);
            }
            else
            {
                ///If no icon, or cant use it we draw normally
                g.DrawString(Text, this.Font, new System.Drawing.SolidBrush(ForeColor), boundLimit.X, boundLimit.Y);
            }
        }       

        #region utility function 

        /// <summary>
        /// This function test if its valid to draw the icon
        /// If the icon is set and showIcon to true, we can
        /// draw.
        /// </summary>
        /// <returns></returns>
        private bool canUseIcon()
        {  
            return (icon != null) && showIcon;
        }

        public void AddCustomSubItem(CustomListViewSubItem subItem)
        {
            this.SubItems.Add(subItem);
            subItem.parent = this;
        }
        
        public void RegisterOnIconClickEvent(EventHandler target)
        {
            iconContainer.Click += target;
        }

        #endregion
    }
}
