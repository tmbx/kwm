using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
            RegistryKey kwmKey = null;
            try
            {
                kwmKey = Registry.LocalMachine.OpenSubKey(Base.GetKwmRegKey(), false);

                if (kwmKey == null)
                {
                    Logging.Log(2, "Unable to find KWM version.");
                    lblRegVersion.Text = "Unknown";
                }
                else
                {
                    lblRegVersion.Text = (String)kwmKey.GetValue("InstallVersion", "Unknown");
                }
            }

            catch (Exception ex)
            {
                Logging.LogException(ex);
            }
            finally
            {
                if (kwmKey != null) kwmKey.Close();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://www.teambox.co");
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}