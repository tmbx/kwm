using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm.Utils
{

    public partial class frmKMsgBox : Form
    {
        [DllImport("Shell32.dll")]
        private extern static int ExtractIconEx(string libName, int iconIndex, IntPtr[] largeIcon, IntPtr[] smallIcon, int nIcons);

        static private IntPtr[] m_largeIcon = null;
        static private IntPtr[] m_smallIcon = null;

        static public IntPtr[] largeIcon
        {
            get
            {
                if (m_largeIcon == null)
                {
                    m_largeIcon = new IntPtr[250];
                    m_smallIcon = new IntPtr[250];
                    ExtractIconEx("shell32.dll", 0, largeIcon, smallIcon, 250);
                }
                return m_largeIcon;
            }
        }
        static public IntPtr[] smallIcon
        {
            get
            {
                if (m_smallIcon == null)
                {
                    m_largeIcon = new IntPtr[250];
                    m_smallIcon = new IntPtr[250];
                    ExtractIconEx("shell32.dll", 0, largeIcon, smallIcon, 250);
                }
                return m_smallIcon;
            }
        }

        static public Icon GetIcon(MessageBoxIcon MIcon)
        {
            Icon icn;
            switch (MIcon)
            {
                case MessageBoxIcon.Information:
                    icn = Icon.FromHandle(largeIcon[221]);
                    break;
                case MessageBoxIcon.Error:
                    icn = Icon.FromHandle(largeIcon[109]);
                    break;
                case MessageBoxIcon.None:
                    icn = Icon.FromHandle(largeIcon[52]);
                    break;
                case MessageBoxIcon.Question:
                    icn = Icon.FromHandle(largeIcon[23]);
                    break;
                default:
                    throw new Exception("Invalid icon requested");

            }
            return icn;
        }

        private int m_retval = -1;

        public int Retval
        {
            get { return m_retval; }
        }

        public frmKMsgBox()
        {
            InitializeComponent();
        }

        public frmKMsgBox(String _text, String _title, MessageBoxIcon _icon, string[] _buttons, int timeout)
            : this(_title, _text, frmKMsgBox.GetIcon(_icon), _buttons, timeout)
        {
            if (_icon == MessageBoxIcon.None)
            {
                this.PromptImage.Margin = new Padding(0);
                this.PromptImage.Size = new Size(0, 0);
            }
        }

        public frmKMsgBox(String _text, String _title, Icon _icon, string[] _buttons, int timeout)
            : this()
        {
            this.lblText.Text = _text;
            this.Text = _title;
            this.PromptImage.Image = new Bitmap(_icon.ToBitmap(), 38, 38);
            int i = 0;
            foreach (string btnName in _buttons)
            {
                Button btn = new Button();
                btn.Text = btnName;
                btn.Click += new EventHandler(btn_Click);
                btn.KeyDown += new KeyEventHandler(frmKMsgBox_KeyDown);
                btn.Name = i.ToString();
                btn.AutoSize = true;
                this.ButtonsHolder.Controls.Add(btn);
                i++;
            }

            if (timeout > 0)
            {
                timer.Interval = timeout * 1000;
                timer.Start();
            }

            this.AcceptButton = (Button)this.ButtonsHolder.Controls[this.ButtonsHolder.Controls.Count - 1];

            ResizeForm(_text);
        }

        void btn_Click(object sender, EventArgs e)
        {
            Button btn = (Button) sender;
            this.m_retval = Int32.Parse(btn.Name);
            this.Close();
        }

        private void ResizeForm(string m_text)
        {
            Graphics g = this.lblText.CreateGraphics();

            //Height
            int text_height = this.lblText.Margin.Top + (int)g.MeasureString(m_text, this.lblText.Font).Height + this.lblText.Margin.Bottom;
            int icon_height = this.PromptImage.Margin.Top + this.PromptImage.Size.Height + this.PromptImage.Margin.Bottom;
            Control btn = this.ButtonsHolder.Controls[0];
            if (icon_height > text_height)
                this.Height = icon_height;
            else
                this.Height = text_height;
            this.tableLayoutPanel.Left = 10;
            this.tableLayoutPanel.Top = 10;
            this.tableLayoutPanel.Size = new Size(this.tableLayoutPanel.Parent.ClientRectangle.Width - 20, this.tableLayoutPanel.Parent.ClientRectangle.Height - 20);
            this.tableLayoutPanel.RowStyles[1].Height = btn.Height + btn.Margin.Top + btn.Margin.Bottom;
            this.Height += (int)this.tableLayoutPanel.RowStyles[1].Height + 20 + this.Size.Height - this.ClientRectangle.Height + 3; // ??? Why do we need +3 ???


            // Width
            this.tableLayoutPanel.ColumnStyles[0].Width = this.PromptImage.Margin.Left + this.PromptImage.Size.Width + this.PromptImage.Margin.Right;
            int width_text = (int)g.MeasureString(m_text, this.lblText.Font).Width;;

            int width_btn = 0;
            foreach (Control ctl in this.ButtonsHolder.Controls)
            {
                width_btn += ctl.Width + ctl.Margin.Left + ctl.Margin.Right;
            }
            this.ButtonsHolder.Width = width_btn + 3;

            if (width_btn > width_text)
                this.Width = width_btn;
            else
                this.Width = width_text;
            this.Width += this.PromptImage.Size.Width + this.PromptImage.Margin.Left + this.PromptImage.Margin.Right + 20 + this.Size.Width - this.ClientRectangle.Width;
            g.Dispose();
        }

        private void frmKMsgBox_KeyDown(object sender, KeyEventArgs e)
        {
            /* -1 is returned by default */
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    public enum KMsgBoxButton
    {
        /// <summary>
        /// Overwrite, OverwriteAll, Skip, Skip all, cancel
        /// </summary>
        OverwriteSkipCancel = 0,
        /// <summary>
        /// Skip, Skip all, cancel
        /// </summary>
        SkipCancel,
        /// <summary>
        /// OK
        /// </summary>
        OK,
        /// <summary>
        /// OK, Cancel
        /// </summary>
        OKCancel,
        /// <summary>
        /// Accept, Cancel
        /// </summary>
        AcceptCancel
    }

    /* When defining new KMsgBoxButtons, declare meaningful
     * constants in here and assign them an index 
     * (starting at 0) in the frmMsgBox.
     * FIXME : won't work nicely if we have, for example, 
     * two different sets of buttons both ending with Cancel 
     * but that have a different number of buttons. Can't have
     * two enum items with the same name but different value ...*/
    public enum KMsgBoxResult
    {
        Overwrite = 0,
        OverwriteAll,
        Skip,
        SkipAll,
        Cancel,
        OK,
        Accept
    }

    public class KMsgBox
    {
        private string m_text;
        private string m_caption;
        private KMsgBoxButton m_buttons;
        private Icon m_icon;
        private int m_timeout;
        private KMsgBoxResult m_result;

        public KMsgBoxResult Result
        {
            get { return m_result; }
            set { m_result = value; }
        }

        public KMsgBox(string text, string caption, KMsgBoxButton buttons, Icon icon, int timeout)
        {
            m_text = text;
            m_caption = caption;
            m_buttons = buttons;
            m_icon = icon;
            m_timeout = timeout;
        }

        public KMsgBox(string text, string caption, KMsgBoxButton buttons)
            : this(text, caption, buttons, Icon.FromHandle(frmKMsgBox.largeIcon[66]), 0) { }

        public KMsgBox(string text, string caption, KMsgBoxButton buttons, MessageBoxIcon icon)
            : this(text, caption, buttons, frmKMsgBox.GetIcon(icon), 0) { }

        public KMsgBox(string text, string caption, KMsgBoxButton buttons, int timeout)
            : this(text, caption, buttons, Icon.FromHandle(frmKMsgBox.largeIcon[66]), timeout) { }

        public KMsgBox(string text, string caption, KMsgBoxButton buttons, MessageBoxIcon icon, int timeout)
            : this(text, caption, buttons, frmKMsgBox.GetIcon(icon), timeout) { }

        public KMsgBoxResult Show()
        {
            m_result = Show(m_text, m_caption, m_buttons, m_icon, m_timeout);
            return m_result;
        }

        public static KMsgBoxResult Show(string text, string caption, KMsgBoxButton buttons)
        {
            return Show(text, caption, buttons, Icon.FromHandle(frmKMsgBox.largeIcon[66]), 0);
        }

        /// <summary>
        /// Create a new dialog, using a standard MessageBoxIcon.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caption"></param>
        /// <param name="buttons"></param>
        /// <param name="icon">Desired MessageBoxIcon. Not all are supported.</param>
        /// <returns></returns>
        public static KMsgBoxResult Show(string text, string caption, KMsgBoxButton buttons, MessageBoxIcon icon)
        {
            return Show(text, caption, buttons, frmKMsgBox.GetIcon(icon), 0);
        }

        public static KMsgBoxResult Show(string text, string caption, KMsgBoxButton buttons, int timeout)
        {
            return Show(text, caption, buttons, Icon.FromHandle(frmKMsgBox.largeIcon[66]), timeout);
        }

        public static KMsgBoxResult Show(string text, string caption, KMsgBoxButton buttons, MessageBoxIcon icon, int timeout)
        {
            return Show(text, caption, buttons, frmKMsgBox.GetIcon(icon), timeout);
        }

        public static KMsgBoxResult Show(string text, string caption, KMsgBoxButton buttons, Icon icon, int timeout)
        {
            Misc.EnsureNoInvokeRequired();

            // Fail fast.
            if (!Misc.IsUiStateOK()) return KMsgBoxResult.Cancel;

            Misc.UiBroker.RequestShowMainForm();
            Misc.UiBroker.OnUiEntry();

            try
            {
                frmKMsgBox msgbox;

                switch (buttons)
                {
                    case KMsgBoxButton.OverwriteSkipCancel:
                        {
                            msgbox = new frmKMsgBox(
                                text,
                                caption,
                                icon,
                                new string[] { "Overwrite", "Overwrite All", "Skip", "Skip All", "Cancel" },
                                timeout);

                            msgbox.ShowDialog(Misc.MainForm);

                            if (msgbox.Retval == -1)
                                return KMsgBoxResult.Cancel;
                            else
                            {
                                switch (msgbox.Retval)
                                {
                                    case 0:
                                        return KMsgBoxResult.Overwrite;
                                    case 1:
                                        return KMsgBoxResult.OverwriteAll;
                                    case 2:
                                        return KMsgBoxResult.Skip;
                                    case 3:
                                        return KMsgBoxResult.SkipAll;
                                    case 4:
                                        return KMsgBoxResult.Cancel;
                                    default:
                                        throw new IndexOutOfRangeException("Unexpected KMsgBox result " + msgbox.Retval);
                                }
                            }
                        }
                    case KMsgBoxButton.OK:
                        {
                            DialogResult res = MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            if (res == DialogResult.OK)
                                return KMsgBoxResult.OK;

                            return KMsgBoxResult.Cancel;
                        }
                    case KMsgBoxButton.SkipCancel:
                        {
                            msgbox = new frmKMsgBox(
                               text,
                               caption,
                               icon,
                               new string[] { "Skip", "Skip All", "Cancel" },
                               timeout);

                            msgbox.ShowDialog(Misc.MainForm);

                            if (msgbox.Retval == -1)
                                return KMsgBoxResult.Cancel;
                            else
                            {
                                switch (msgbox.Retval)
                                {
                                    case 0:
                                        return KMsgBoxResult.Skip;
                                    case 1:
                                        return KMsgBoxResult.SkipAll;
                                    case 2:
                                        return KMsgBoxResult.Cancel;
                                    default:
                                        throw new IndexOutOfRangeException("Unexpected KMsgBox result " + msgbox.Retval);
                                }
                            }
                        }
                    case KMsgBoxButton.OKCancel:
                        {
                            msgbox = new frmKMsgBox(
                               text,
                               caption,
                               icon,
                               new string[] { "OK", "Cancel" },
                               timeout);

                            msgbox.ShowDialog(Misc.MainForm);

                            if (msgbox.Retval == -1)
                                return KMsgBoxResult.Cancel;
                            else
                            {
                                switch (msgbox.Retval)
                                {
                                    case 0:
                                        return KMsgBoxResult.OK;
                                    case 1:
                                        return KMsgBoxResult.Cancel;
                                    default:
                                        throw new IndexOutOfRangeException("Unexpected KMsgBox result " + msgbox.Retval);
                                }
                            }
                        }
                    case KMsgBoxButton.AcceptCancel:
                        {
                            msgbox = new frmKMsgBox(
                               text,
                               caption,
                               icon,
                               new string[] { "Accept", "Cancel" },
                               timeout);

                            msgbox.ShowDialog(Misc.MainForm);

                            if (msgbox.Retval == -1)
                                return KMsgBoxResult.Cancel;
                            else
                            {
                                switch (msgbox.Retval)
                                {
                                    case 0:
                                        return KMsgBoxResult.Accept;
                                    case 1:
                                        return KMsgBoxResult.Cancel;
                                    default:
                                        throw new IndexOutOfRangeException("Unexpected KMsgBox result " + msgbox.Retval);
                                }
                            }
                        }
                    default:
                        throw new Exception("Undefined KMsgButton");
                }
            }

            finally
            {
                Misc.UiBroker.OnUiExit();
            }
        }
    }
}