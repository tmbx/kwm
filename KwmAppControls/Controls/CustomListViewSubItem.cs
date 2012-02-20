using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace kwm.Utils
{

    /// <summary>
    /// this class represent a subitem to use with custom listview and custom item. If
    /// not use with , it will work like a standard ITEM . 
    /// 
    /// Note to use with ListviewItem the subitem must be add with the appropriate function of 
    /// CustomListViewItem (addsubitem)
    /// 
    /// To use the Icon , you must set an icon and the var showIcon to true. If not the icon even if set will
    /// not appear. 
    /// 
    ///The icon is contain in a picturebox to have event. So its not directly the icon drawing on the listview but is box. 
    /// 
    /// So the box is private and need to use addIconOnclickEvent to add the event of user click. 
    /// If need new event please add the proper function. 
    /// 
    ///*****Note the custom item and subitem are almost the same but not in the same file or inherit because in the 
    /// future can have different action or behavior. SO DONT FORGET TO UPDATE MODIFICATION OR FIX IN CustomListViewSubItem
    /// </summary>
    public class CustomListViewSubItem : System.Windows.Forms.ListViewItem.ListViewSubItem
    {

        //Constant right and left specifie if icon is at the right or the left of the text        
        public const int RIGHT = 0;
        public const int CENTER = 1;
        public const int LEFT = 2;


        public System.Drawing.Bitmap icon = null;
        public System.Windows.Forms.PictureBox iconContainer = new System.Windows.Forms.PictureBox();
        public bool showIcon = false;

        public ListViewItem parent = null;

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
        /// <summary>
        /// Default construtor
        /// </summary>
        public CustomListViewSubItem()
        {                       
        }
        
        public CustomListViewSubItem(String text)
        {
            this.Text = text;
        }

        public CustomListViewSubItem(Bitmap icon)
        {
            this.icon = icon;
        }

        public CustomListViewSubItem(Bitmap icon, int position)
        {
            this.icon = icon;
            this.iconPosition = position;           
        }

        public CustomListViewSubItem(String text, Bitmap icon, int iconPosition)
        {
            this.Text = text;
            this.icon = icon;
            this.iconPosition = iconPosition;
        }

        public CustomListViewSubItem(String text, Bitmap icon, int iconPosition, bool showIcon)
        {
            this.Text = text;
            this.icon = icon;
            this.iconPosition = iconPosition;
            this.showIcon = showIcon;
        }
        #endregion 


        public void drawItem(System.Drawing.Graphics g,System.Drawing.Rectangle boundLimit)
        {
            if (canUseIcon() && parent != null)
            {

                iconContainer.Size = new Size(boundLimit.Height - 1, boundLimit.Height - 1);               
                iconContainer.BackgroundImage = new Bitmap(icon, boundLimit.Height - 1, boundLimit.Height - 1);                
                switch (iconPosition)
                {
                    case RIGHT:
                        {
                            g.DrawString(Text, this.Font, new SolidBrush(ForeColor), boundLimit.X, boundLimit.Y);
                            int x = boundLimit.X + boundLimit.Width - boundLimit.Height;
                            int y = boundLimit.Y;                            
                            iconContainer.Location = new Point(x, y);                                                       
                        }
                        break;
                    case CENTER:
                        {
                            ///For the center position of the icon, we draw text normally , and if the text is too long, too bad. 
                            ///In fact the  center position exist to use without text
                            g.DrawString(Text, this.Font, new System.Drawing.SolidBrush(ForeColor), boundLimit.X, boundLimit.Y);                            
                            iconContainer.Location = new Point(boundLimit.X + ((boundLimit.Width / 2) - boundLimit.Height), boundLimit.Y);                            
                        }
                        break;
                    case LEFT:
                        {                            
                            iconContainer.Location = new Point(boundLimit.X+1, boundLimit.Y);                            
                            g.DrawString(Text, this.Font, new System.Drawing.SolidBrush(ForeColor), boundLimit.X + boundLimit.Height+1, boundLimit.Y);
                        }
                        break;
                }
                parent.ListView.Controls.Add(iconContainer);               
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
        /// If the icon is set and the a some place we have set 
        /// showIcon to true, we cant draw because we dont have icon
        /// 
        /// So to know if we can draw icon we test both 
        /// 
        /// icon == null 
        /// showIcon
        /// </summary>
        /// <returns></returns>
        private bool canUseIcon()
        {
            return (icon != null) && showIcon;                
        }

        public void addIconOnclickEvent(EventHandler target)
        {
            iconContainer.Click += target;
        }


        #endregion
    
    }
}
