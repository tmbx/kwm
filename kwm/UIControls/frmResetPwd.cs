using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    public partial class frmResetPwd : frmKBaseForm
    {
        /// <summary>
        /// Password entered by the user. This is meaningfull only if DialogResult == OK.
        /// </summary>
        public String NewPassword
        {
            get { return txtPwd.Text; }
        }

        public frmResetPwd()
        {
            InitializeComponent();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            btnOK.Enabled = txtPwd.Text != "" && txtConfirm.Text != "" &&
                            txtPwd.Text == txtConfirm.Text;
        }

        private void txtPwd_TextChanged(object sender, EventArgs e)
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

        private void txtConfirm_TextChanged(object sender, EventArgs e)
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
    } 
}