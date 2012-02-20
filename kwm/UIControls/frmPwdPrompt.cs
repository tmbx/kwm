using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace kwm
{
    public partial class frmPwdPrompt : kwm.frmKBaseForm
    {
        /// <summary>
        /// Prompt test to display to the user.
        /// </summary>
        public String PromptText
        {
            get { return lblPrompt.Text; }
            set { lblPrompt.Text = value; }
        }

        public frmPwdPrompt()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Prompt the user for a password. This enters the UI: make sure you call OnUiEntry
        /// properly if need be.
        /// </summary>
        public static DialogResult ShowPrompt(String kwsName, bool failFlag, ref String kwsPwd)
        {
            frmPwdPrompt p = new frmPwdPrompt();

            // Populate the form.
            p.PromptText = "Enter your password for " + kwsName + ":";
            p.txtPwd.Text = kwsPwd;

            p.lblFailed.Visible = failFlag;
            // Prompt the user and set the return values.
            DialogResult res = p.ShowDialog();
            kwsPwd = p.txtPwd.Text;
            return res;
        }

        private void UpdateOkButton()
        {
            btnOK.Enabled = txtPwd.Text != "";
        }

        private void txtPwd_TextChanged(object sender, EventArgs e)
        {
            UpdateOkButton();
        }
    }
}

