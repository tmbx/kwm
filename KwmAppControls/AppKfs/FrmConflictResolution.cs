using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;

namespace kwm.KwmAppControls
{
    public partial class FrmConflictResolution : Form
    {
        private ConflictAction m_action = ConflictAction.Cancel;

        private Image toLocal;
        private Image toLocalGrey;
        private Image toServer;
        private Image toServerGrey;

        public ConflictAction Action
        {
            get
            {
                return m_action;
            }
        }

        public enum ConflictAction
        {
            Upload,
            Download,
            Cancel
        }

        public FrmConflictResolution(String _message)
        {
            InitializeComponent();
            toLocal = kwm.KwmAppControls.Properties.Resources.tolocal;
            toLocalGrey = kwm.KwmAppControls.Properties.Resources.tolocal_grey;
            toServer = kwm.KwmAppControls.Properties.Resources.toserver;
            toServerGrey = kwm.KwmAppControls.Properties.Resources.toserver_grey;

            picToLocal.Image = toLocalGrey;
            picToServer.Image = toServerGrey;
            m_action = ConflictAction.Cancel;
            label1.Text = _message;
            //radioToServer.Checked = true;
        }

        private void radioToServer_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUIStatus();
        }

        private void radioToLocal_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUIStatus();
        }

        private void UpdateUIStatus()
        {
            if (radioToServer.Checked)
            {
                picToServer.Image = toServer;
                picToLocal.Image = toLocalGrey;
            }
            else if(radioToLocal.Checked)
            {
                picToServer.Image = toServerGrey;
                picToLocal.Image = toLocal;
            }
          
            btnOK.Enabled = (radioToServer.Checked || radioToLocal.Checked);

            if (radioToLocal.Checked)
                m_action = ConflictAction.Download;
            else if (radioToServer.Checked)
                m_action = ConflictAction.Upload;
            else
                /* Should not happen if radio buttons behave */
                m_action = ConflictAction.Cancel;
        }

        private void picToServer_Click(object sender, EventArgs e)
        {
            radioToServer.Checked = true;
        }

        private void picToLocal_Click(object sender, EventArgs e)
        {
            radioToLocal.Checked = true;
        }

        private void btnOK_EnabledChanged(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
                btnOK.Focus();
        }
    }
}