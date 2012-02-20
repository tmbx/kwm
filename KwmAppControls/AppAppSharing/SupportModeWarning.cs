using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls
{
    public partial class SupportModeWarning : Form
    {

        public SupportModeWarning()
        {
            InitializeComponent();
            Icon = kwm.KwmAppControls.Properties.Resources.Teambox;
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            try
            {
                Misc.ApplicationSettings.AppSharingWarnOnSupportSession = !chkDoNotShowWarning.Checked;
                Misc.ApplicationSettings.Save();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}