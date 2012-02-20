using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public partial class FrmResolveTypeConflict : Form
    {
        /// <summary>
        /// Set to true if the desired action is to 
        /// delete the local stuff, false to rename.
        /// </summary>
        public bool Delete = false;

        private string m_sharePath = "";

        public FrmResolveTypeConflict()
        {
            InitializeComponent();
            UpdateBtnOK();
        }

        /// <summary>
        /// Set the share path.
        /// </summary>
        /// <param name="parentPath"></param>
        public FrmResolveTypeConflict(string sharePath) : this()
        {
            m_sharePath = sharePath;
        }

        private void radioRename_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                txtNewName.Enabled = radioRename.Checked;
                UpdateBtnOK();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", m_sharePath); 
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                Delete = radioDelete.Checked;
                this.Close();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void txtNewName_TextChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateBtnOK();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }

        private void UpdateBtnOK()
        {
            btnOK.Enabled = radioDelete.Checked || txtNewName.Text != "";
        }
    }
}