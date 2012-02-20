using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Wizard.UI;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    /// <summary>
    /// This page allows the user to select a running application to share instead
    /// of his entire desktop.
    /// </summary>
    public partial class NewSession2 : NewSessionBasePage
    {
        public NewSession2()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            lstApps.LargeImageList = new ImageList();
            lstApps.SmallImageList = new ImageList();
            lstApps.LargeImageList.ColorDepth = ColorDepth.Depth32Bit;
            lstApps.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
            SetListViewContent();           
        }

        /// <summary>
        /// Completely rebuilds the listview.
        /// </summary>
        private void SetListViewContent()
        {
            List<Window> lstWindows = Window.GetApplications();
            lstApps.BeginUpdate();
            lstApps.Items.Clear();
            lstApps.SmallImageList.Images.Clear();
            lstApps.LargeImageList.Images.Clear();

            foreach (Window w in lstWindows)
            {
                AddWindowToListview(w);
            }

            lstApps.EndUpdate();
        }

        /// <summary>
        /// This function refresh the current list that is the same as in the listview
        /// We ask the system the new list of actual active window and test the current
        /// list to see if window are active or need to be remove.
        /// 
        /// At the end if some windows are new, we add to the current list 
        /// </summary>
        private void UpdateList()
        {
            List<Window> currentList = Window.GetApplications();

            List<String> currentListHWnds = new List<string>();
            foreach (Window w in currentList)
            {
                currentListHWnds.Add(w.HWnd);
            }

            // Add new windows
            foreach (Window w in currentList)
            {
                if (!lstApps.Items.ContainsKey(w.HWnd))
                {
                    // There is a Window we don't have in our listview
                    AddWindowToListview(w);
                }
            }

            // Remove old windows
            foreach (ListViewItem itm in lstApps.Items)
            {
                if (!currentListHWnds.Contains(itm.Name))
                {
                    itm.Remove();
                }
            }
        }

        /// <summary>
        /// Given a Window object, creates a new ListviewItem and
        /// adds it to lstApps. This correctly sets the Image stuff
        /// to get the App icon right.
        /// </summary>
        /// <param name="w"></param>
        private void AddWindowToListview(Window w)
        {
            lstApps.LargeImageList.Images.Add(w.HWnd, w.BigIcon);

            lstApps.SmallImageList.Images.Add(w.HWnd, w.BigIcon);
            ListViewItem item = new ListViewItem(w.Title, w.HWnd);
            item.Name = w.HWnd;

            lstApps.Items.Add(item);
        }

        /// <summary>
        /// Refreshes the list on each timer tick.
        /// </summary>
        private void OnListUpdateTimerTick(object sender, EventArgs e)
        {
            try
            {
                listUpdateTimer.Stop();
                UpdateList();
                listUpdateTimer.Start();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void UpdateButtons()
        {
            if (lstApps.SelectedItems.Count == 1)
            {
                SetWizardButtons(WizardButtons.Back | WizardButtons.Next | WizardButtons.Cancel);
                WizardConfig.SharedWindowHandle = lstApps.SelectedItems[0].Name;
                WizardConfig.SharedAppTitle = lstApps.SelectedItems[0].Text;
            }
            else
            {
                SetWizardButtons(WizardButtons.Back | WizardButtons.Cancel);
                WizardConfig.SharedWindowHandle = "";
                WizardConfig.SharedAppTitle = "";
            }
        }

        private void AppList_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.F5)
                {
                    if (listUpdateTimer.Enabled)
                    {
                        listUpdateTimer.Stop();
                    }
                    UpdateList();
                    SetListViewContent();
                    listUpdateTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSession2_SetActive(object sender, CancelEventArgs e)
        {
            try
            {
                listUpdateTimer.Start();

                UpdateButtons();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lstApps_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateButtons();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSession2_WizardBack(object sender, WizardPageEventArgs e)
        {
            try
            {
                listUpdateTimer.Stop();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void NewSession2_WizardNext(object sender, WizardPageEventArgs e)
        {
            try
            {
                listUpdateTimer.Stop();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void lstApps_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                PressButton(WizardButtons.Next);
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }

    public class Window
    {
        private delegate bool WNDENUMPROC(int hwnd, IntPtr lparam);
        [DllImport("user32")]
        private static extern int EnumDesktopWindows(int hDesktop, WNDENUMPROC lpfn, IntPtr lParam);
        [DllImport("user32")]
        private static extern int GetWindowLongA(int hwnd, int nIndex);
        private static int GetWindowLongPtr(int hwnd, int nIndex) { return GetWindowLongA(hwnd, nIndex); }
        [DllImport("user32")]
        private static extern int GetWindow(int hwnd, int uCmd);
        [DllImport("user32")]
        private static extern int GetShellWindow();
        [DllImport("user32")]
        private static extern int GetWindowTextA(int hwnd, StringBuilder str, int len);
        private static int GetWindowText(int hwnd, StringBuilder str, int len) { return GetWindowTextA(hwnd, str, len); }
        [DllImport("user32")]
        private static extern int GetWindowTextLengthA(int hwnd);
        private static int GetWindowTextLength(int hwnd) { return GetWindowTextLengthA(hwnd); }
        [DllImport("user32")]
        private static extern int SendMessageTimeout(int hWnd, int msg, int wParam, int lParam, int fuFlags, int uTimeout, ref IntPtr lpdwResult);
        [DllImport("user32")]
        private static extern int LoadIcon(IntPtr handle, int which);
        [DllImport("user32")]
        private static extern int GetLastError();
        [DllImport("user32")]
        private static extern int GetClassLongA(int hwnd, int nIndex);
        private static IntPtr GetClassLongPtr(int hwnd, int nIndex) { return new IntPtr(GetClassLongA(hwnd, nIndex)); }
        [DllImport("user32")]
        private static extern int GetClassName(int hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(int hWnd, IntPtr lpdwProcessId);
        [DllImport("user32")]
        private static extern int EnumThreadWindows(int dwThreadId, WNDENUMPROC lpfn, IntPtr lParam);

        private const int GWL_STYLE = (-16);
        private const int GW_OWNER = 4;
        private const int GWL_EXSTYLE = (-20);
        private const int WM_GETICON = 0x007F;
        private const int ICON_BIG = 0;
        private const int ICON_SMALL = 1;
        private const int ICON_SMALL2 = 2;
        private const int SMTO_ABORTIFHUNG = 0x0002;
        private const int HUNG_TIMEOUT = 256;
        private const int IDI_APPLICATION = 32512;
        private const int GCLP_HICON = (-14);
        private const int GCLP_HICONSM = (-34);
        private const int GCW_ATOM = (-32);


        /// <summary>
        /// Define for Window constant (WS_ and WS_EX)
        /// </summary>
        private const long WS_OVERLAPPED = 0x00000000;
        private const long WS_POPUP = 0x80000000;
        private const long WS_CHILD = 0x40000000;
        private const long WS_MINIMIZE = 0x20000000;
        private const long WS_VISIBLE = 0x10000000;
        private const long WS_DISABLED = 0x08000000;
        private const long WS_CLIPSIBLINGS = 0x04000000;
        private const long WS_CLIPCHILDREN = 0x02000000;
        private const long WS_MAXIMIZE = 0x01000000;
        private const long WS_CAPTION = 0x00C00000;
        private const long WS_BORDER = 0x00800000;
        private const long WS_DLGFRAME = 0x00400000;
        private const long WS_VSCROLL = 0x00200000;
        private const long WS_HSCROLL = 0x00100000;
        private const long WS_SYSMENU = 0x00080000;
        private const long WS_THICKFRAME = 0x00040000;
        private const long WS_GROUP = 0x00020000;
        private const long WS_TABSTOP = 0x00010000;
        private const long WS_MINIMIZEBOX = 0x00020000;
        private const long WS_MAXIMIZEBOX = 0x00010000;
        private const long WS_TILED = 0x00000000;
        private const long WS_ICONIC = 0x20000000;
        private const long WS_SIZEBOX = 0x00040000;
        private const long WS_OVERLAPPEDWINDOW = (0x00000000 | 0x00C00000 | 0x00080000 | 0x00040000 | 0x00020000 | 0x00010000);
        private const long WS_POPUPWINDOW = (0x80000000 | 0x00800000 | 0x00080000);
        private const long WS_CHILDWINDOW = 0x40000000;
        private const long WS_EX_DLGMODALFRAME = 0x00000001;
        private const long WS_EX_NOPARENTNOTIFY = 0x00000004;
        private const long WS_EX_TOPMOST = 0x00000008;
        private const long WS_EX_ACCEPTFILES = 0x00000010;
        private const long WS_EX_TRANSPARENT = 0x00000020;
        private const long WS_EX_MDICHILD = 0x00000040;
        private const long WS_EX_TOOLWINDOW = 0x00000080;
        private const long WS_EX_WINDOWEDGE = 0x00000100;
        private const long WS_EX_CLIENTEDGE = 0x00000200;
        private const long WS_EX_CONTEXTHELP = 0x00000400;
        private const long WS_EX_RIGHT = 0x00001000;
        private const long WS_EX_LEFT = 0x00000000;
        private const long WS_EX_RTLREADING = 0x00002000;
        private const long WS_EX_LTRREADING = 0x00000000;
        private const long WS_EX_LEFTSCROLLBAR = 0x00004000;
        private const long WS_EX_RIGHTSCROLLBAR = 0x00000000;
        private const long WS_EX_CONTROLPARENT = 0x00010000;
        private const long WS_EX_STATICEDGE = 0x00020000;
        private const long WS_EX_APPWINDOW = 0x00040000;
        private const long WS_EX_OVERLAPPEDWINDOW = (0x00000100 | 0x00000200);
        private const long WS_EX_PALETTEWINDOW = (0x00000100 | 0x00000080 | 0x00000008);
        private const long WS_EX_LAYERED = 0x00080000;
        private const long WS_EX_NOINHERITLAYOUT = 0x00100000;
        private const long WS_EX_LAYOUTRTL = 0x00400000;
        private const long WS_EX_NOACTIVATE = 0x08000000;

        private int m_hwnd;
        private String m_Title = "";
        private System.Drawing.Icon _bigIcon;
        private System.Drawing.Icon _smallIcon;

        private Window(int hwnd)
        {
            m_hwnd = hwnd;
            m_Title = this.GetText();
            BigIcon = GetBigIcon();
            SmallIcon = GetSmallIcon();
        }

        private Window(int hwnd, String title)
        {
            m_hwnd = hwnd;
            m_Title = title;
        }

        /// <summary>
        /// Gets the window's handle
        /// </summary>
        public String HWnd
        {
            get
            {
                return m_hwnd.ToString();
            }
        }

        /// <summary>
        /// Get the propeties of this class that represent the title of 
        /// the window. Set while construtor is called.
        /// </summary>
        /// <returns></returns>
        public String Title
        {
            get
            {
                return m_Title;
            }
        }
        public Icon BigIcon
        {
            get
            {
                return _bigIcon;
            }

            set
            {
                _bigIcon = value;
            }
        }

        public Icon SmallIcon
        {
            get
            {
                return _smallIcon;
            }
            set
            {
                _smallIcon = value;
            }
        }

        public string GetText()
        {
            int len = Window.GetWindowTextLength(m_hwnd) + 1;
            StringBuilder buff = new StringBuilder(len);
            int ret = GetWindowText(m_hwnd, buff, len);
            return buff.ToString();
        }

        public string GetClass()
        {
            StringBuilder buff = new StringBuilder(1024);
            GetClassName(m_hwnd, buff, 1024);
            return buff.ToString();
        }

        public Icon GetBigIcon()
        {
            Icon icon = null;
            IntPtr hIcon = new IntPtr(0);
            try
            {
                if (SendMessageTimeout(m_hwnd, WM_GETICON, ICON_BIG, 0,
                    SMTO_ABORTIFHUNG, HUNG_TIMEOUT, ref hIcon) == 0)
                {
                    int dwErr = GetLastError();
                    if (dwErr == 0 || dwErr == 1460)
                    {
                        throw new Exception();
                    }
                }

                if (hIcon.ToInt32() == 0)
                {
                    hIcon = GetClassLongPtr(m_hwnd, GCLP_HICON);
                }

                if (hIcon.ToInt32() == 0)
                {
                    throw new Exception();
                }

                icon = (Icon)Icon.FromHandle(hIcon).Clone();
            }
            catch (Exception)
            {
                IntPtr iptr = new IntPtr(0);
                hIcon = new IntPtr(LoadIcon(iptr, IDI_APPLICATION));
                icon = (Icon)Icon.FromHandle(hIcon).Clone();
            }

            return icon;
        }

        public Icon GetSmallIcon()
        {
            Icon icon;
            IntPtr hIcon = new IntPtr(0);
            try
            {
                if (SendMessageTimeout(m_hwnd, WM_GETICON, ICON_SMALL, 0,
                    SMTO_ABORTIFHUNG, HUNG_TIMEOUT, ref hIcon) == 0)
                {
                    int dwErr = GetLastError();
                    if (dwErr == 0 || dwErr == 1460)
                        throw new Exception();
                }

                if (hIcon.ToInt32() == 0)
                {
                    if (SendMessageTimeout(m_hwnd, WM_GETICON, ICON_SMALL2, 0,
                        SMTO_ABORTIFHUNG, HUNG_TIMEOUT, ref hIcon) == 0)
                    {
                        int dwErr = GetLastError();
                        if (dwErr == 0 || dwErr == 1460)
                            throw new Exception();
                    }
                }
                if (hIcon.ToInt32() == 0)
                {
                    hIcon = GetClassLongPtr(m_hwnd, GCLP_HICONSM);
                }

                if (hIcon.ToInt32() == 0)
                {
                    throw new Exception();
                }

                icon = Icon.FromHandle(hIcon);
            }

            catch (Exception)
            {
                icon = GetBigIcon();
            }

            return icon;
        }

        public int GetPid()
        {
            return GetWindowThreadProcessId(m_hwnd, (IntPtr)null);
        }


        /// <summary>
        /// Function to use with a delegate
        /// This function receive a window handle and a pointer to lparam that can be anything
        /// 
        /// For us we use this function to find active application associate with a visible and accessible 
        /// window.  Window can have to kind of style if create with CreateWindowEx
        /// 
        /// dwStyle and dwStyleEx.
        /// 
        /// Whate we need  : 
        /// 
        /// - Visible window : WS_VISIBLE  OR 
        /// 
        /// This properties forces a window to be on top and not to be in the task bar. 
        /// So this only property doesn't guarantee that the window will be really visble or drawn on the desktop
        /// 
        /// Properties that can make it wrong are :
        /// 
        /// WS_EX_TOOLWINDOW : this property makes the window not appearing in the task bar and tasks manager. 
        /// This kind of window is used as a floating bar. So its small, doesn't have an icon and can have 
        /// a system menu but ITS NOT AN APPLICATION            
        /// 
        /// next, 
        /// 
        /// we dont want child window. 
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static bool EnumDesktopWindows_cb(int hwnd, IntPtr lParam)
        {
            List<Window> list = (List<Window>)GCHandle.FromIntPtr(lParam).Target;

            int style = GetWindowLongPtr(hwnd, GWL_STYLE);
            int styleEx = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            if (((styleEx & WS_EX_TOOLWINDOW) == 0) && ((style & WS_VISIBLE) != 0) && ((style & WS_CHILD) == 0))
            {
                try
                {
                    Window w = new Window(hwnd);
                    list.Add(w);
                }
                catch (Exception ex)
                {
                    Logging.LogException(ex);
                }
            }
            return true;
        }

        /// <summary>
        /// If the mainwindowhandle is equal to 0 , its mean NO GRAPHICAL INTERFACE, 
        /// It means the process is probably a background or a zombie process        
        /// </summary>
        /// <returns>the list of active processes with a graphic interface</returns>
        public static List<Window> GetApplications()
        {
            List<Window> lstWindows = new List<Window>();
            Process[] procList;

            WNDENUMPROC edw_cb = new WNDENUMPROC(EnumDesktopWindows_cb);
            GCHandle gch = GCHandle.Alloc(lstWindows);

            try
            {
                procList = Process.GetProcesses();
                foreach (Process p in procList)
                {
                    if (p.MainWindowHandle != System.IntPtr.Zero)
                    {
                        foreach (System.Diagnostics.ProcessThread t in p.Threads)
                        {
                            EnumThreadWindows(t.Id, edw_cb, GCHandle.ToIntPtr(gch));
                        }
                    }
                }
                gch.Free();
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                Misc.KwmTellUser("Unable to get list of running processes. (" + e.ToString() + ")");
            }

            return lstWindows;
        }


        /* WS* styles == 0 are not tested (how do you do that ??) */
        private static void logWSProperties(long style, int hwnd)
        {
            if ((style & WS_POPUP) != 0)
            {
                Logging.Log(hwnd + " IS : WS_POPUP ");
            }

            if ((style & WS_CHILD) != 0)
            {
                Logging.Log(hwnd + " IS : WS_CHILD ");
            }

            if ((style & WS_MINIMIZE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_MINIMIZE ");
            }

            if ((style & WS_VISIBLE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_VISIBLE ");
            }

            if ((style & WS_DISABLED) != 0)
            {
                Logging.Log(hwnd + " IS : WS_DISABLED ");
            }

            if ((style & WS_CLIPSIBLINGS) != 0)
            {
                Logging.Log(hwnd + " IS : WS_CLIPSIBLINGS ");
            }

            if ((style & WS_CLIPCHILDREN) != 0)
            {
                Logging.Log(hwnd + " IS : WS_CLIPCHILDREN ");
            }

            if ((style & WS_MAXIMIZE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_MAXIMIZE ");
            }

            if ((style & WS_CAPTION) != 0)
            {
                Logging.Log(hwnd + " IS : WS_CAPTION ");
            }

            if ((style & WS_BORDER) != 0)
            {
                Logging.Log(hwnd + " IS : WS_BORDER ");
            }

            if ((style & WS_DLGFRAME) != 0)
            {
                Logging.Log(hwnd + " IS : WS_DLGFRAME");
            }

            if ((style & WS_VSCROLL) != 0)
            {
                Logging.Log(hwnd + " IS : WS_VSCROLL");
            }

            if ((style & WS_HSCROLL) != 0)
            {
                Logging.Log(hwnd + " IS : WS_HSCROLL");
            }

            if ((style & WS_SYSMENU) != 0)
            {
                Logging.Log(hwnd + " IS : WS_SYSMENU");
            }

            if ((style & WS_THICKFRAME) != 0)
            {
                Logging.Log(hwnd + " IS : WS_THICKFRAME");
            }

            if ((style & WS_GROUP) != 0)
            {
                Logging.Log(hwnd + " IS : WS_GROUP");
            }

            if ((style & WS_TABSTOP) != 0)
            {
                Logging.Log(hwnd + " IS : WS_TABSTOP");
            }

            if ((style & WS_MINIMIZEBOX) != 0)
            {
                Logging.Log(hwnd + " IS : WS_MINIMIZEBOX");
            }

            if ((style & WS_MAXIMIZEBOX) != 0)
            {
                Logging.Log(hwnd + " IS : WS_MAXIMIZEBOX");
            }

            if ((style & WS_ICONIC) != 0)
            {
                Logging.Log(hwnd + " IS : WS_ICONIC");
            }

            if ((style & WS_SIZEBOX) != 0)
            {
                Logging.Log(hwnd + " IS : WS_SIZEBOX");
            }

            if ((style & WS_OVERLAPPEDWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_OVERLAPPEDWINDOW");
            }

            if ((style & WS_POPUPWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_POPUPWINDOW");
            }
            if ((style & WS_CHILDWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_CHILDWINDOW");
            }
        }

        private static void logWSEXProperties(long style, int hwnd)
        {
            if ((style & WS_EX_DLGMODALFRAME) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_DLGMODALFRAME ");
            }

            if ((style & WS_EX_NOPARENTNOTIFY) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_NOPARENTNOTIFY ");
            }

            if ((style & WS_EX_TOPMOST) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_TOPMOST ");
            }

            if ((style & WS_EX_ACCEPTFILES) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_ACCEPTFILES ");
            }

            if ((style & WS_EX_TRANSPARENT) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_TRANSPARENT ");
            }

            if ((style & WS_EX_MDICHILD) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_MDICHILD ");
            }

            if ((style & WS_EX_TOOLWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_TOOLWINDOW ");
            }

            if ((style & WS_EX_WINDOWEDGE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_WINDOWEDGE ");
            }

            if ((style & WS_EX_CLIENTEDGE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_CLIENTEDGE ");
            }

            if ((style & WS_EX_CONTEXTHELP) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_CONTEXTHELP ");
            }

            if ((style & WS_EX_RIGHT) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_RIGHT ");
            }

            if ((style & WS_EX_RTLREADING) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_RTLREADING");
            }

            if ((style & WS_EX_LEFTSCROLLBAR) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_LEFTSCROLLBAR");
            }

            if ((style & WS_EX_CONTROLPARENT) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_CONTROLPARENT");
            }

            if ((style & WS_EX_STATICEDGE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_STATICEDGE");
            }

            if ((style & WS_EX_APPWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_APPWINDOW");
            }

            if ((style & WS_EX_OVERLAPPEDWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_OVERLAPPEDWINDOW");
            }

            if ((style & WS_EX_PALETTEWINDOW) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_PALETTEWINDOW");
            }

            if ((style & WS_EX_LAYERED) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_LAYERED");
            }

            if ((style & WS_EX_NOINHERITLAYOUT) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_NOINHERITLAYOUT");
            }

            if ((style & WS_EX_LAYOUTRTL) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_LAYOUTRTL");
            }

            if ((style & WS_EX_NOACTIVATE) != 0)
            {
                Logging.Log(hwnd + " IS : WS_EX_NOACTIVATE");
            }
        }
    }
}

