using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;
using Tbx.Utils;

namespace kwm
{
    public partial class ucInvitationPwdPrompt : UserControl
    {
        /// <summary>
        /// List of email addresses that require a password.
        /// </summary>
        public List<KwsInviteOpUser> RequiredPwds = null;

        /// <summary>
        /// List of email addresses that are already invited to the Teambox, if any.
        /// </summary>
        private List<KwsInviteOpUser> m_alreadyInvited = null;

        /// <summary>
        /// List of email addresses that are going to be invited by key id, if any.
        /// </summary>
        private List<KwsInviteOpUser> m_noPwdRequired = null;

        /// <summary>
        /// Invitation parameters.
        /// </summary>
        public KwsInviteOpParams InviteParams = null;

        /// <summary>
        /// Fired whenever the user changed some data in the control.
        /// </summary>
        public event EventHandler<EventArgs> OnChange;

        public ucInvitationPwdPrompt()
        {
            InitializeComponent();

            chkUseSamePwd.CheckedChanged += new EventHandler(ChangeEventHandler);
            txtSamePwd.TextChanged += new EventHandler(ChangeEventHandler);
            lblPwdPromptText.Text = Base.GetPwdPromptText();
        }

        public void UpdateUI()
        {
            RequiredPwds = new List<KwsInviteOpUser>();
            m_alreadyInvited = new List<KwsInviteOpUser>();
            m_noPwdRequired = new List<KwsInviteOpUser>();

            foreach (KwsInviteOpUser u in InviteParams.UserArray)
            {
                // The user has already been invited.
                if (u.AlreadyInvitedFlag) m_alreadyInvited.Add(u);
                
                // The user requires a password.
                else if (u.KeyID == 0) RequiredPwds.Add(u);

                // The user has a key id and has not been invited yet, needs
                // to be invited without a password.
                else m_noPwdRequired.Add(u);
            }

            FillRequiredPwds();

            chkUseSamePwd.TabIndex = RequiredPwds.Count + 1;
            txtSamePwd.TabIndex = chkUseSamePwd.TabIndex + 1;

            UpdateSamePwdControls();

            panelMore.Visible = (m_alreadyInvited.Count > 0 ||
                                m_noPwdRequired.Count > 0);
        }

        /// <summary>
        /// Return true if the user has set all the required passwords properly.
        /// </summary>
        public bool IsInputValid()
        {
            if (chkUseSamePwd.Checked && txtSamePwd.Text != "") return true;

            foreach (Control c in panelPwdPrompt.Controls)
                if (c is TextBox && ((TextBox)c).Text == "") return false;

            return true;
        }

        private void UpdateSamePwdControls()
        {
            panelPwdPrompt.Enabled = !chkUseSamePwd.Checked;
            txtSamePwd.Enabled = chkUseSamePwd.Checked;
            if (chkUseSamePwd.Checked) txtSamePwd.Select();
        }

        private void FillRequiredPwds()
        {
            panelPwdPrompt.Controls.Clear();

            List<Label> labels = new List<Label>();
            List<TextBox> tbs = new List<TextBox>();

            if (RequiredPwds.Count == 0)
            {
                Label l = new Label();
                l.AutoSize = true;
                l.Text = "An error occured, this form should not have been displayed.";
                l.Location = new Point(15, 15);
                panelPwdPrompt.Controls.Add(l);
                return;
            }

            int i = 0;
            foreach (KwsInviteOpUser u in RequiredPwds)
            {
                Label l = new Label();
                l.Text = u.EmailAddress;
                l.AutoSize = false;
                l.AutoEllipsis = true;
                l.Width = 150;
                l.Location = new Point(5, i * l.Height + 5);
                labels.Add(l);

                TextBox t = new TextBox();
                t.TextChanged += new EventHandler(ChangeEventHandler);
                t.Name = u.EmailAddress;
                t.UseSystemPasswordChar = true;
                t.Location = new Point(l.Width + 5, l.Location.Y);
                t.Width = 150;
                tbs.Add(t);
                i++;
            }

            panelPwdPrompt.Controls.AddRange(labels.ToArray());
            panelPwdPrompt.Controls.AddRange(tbs.ToArray());
            // Select the first password textbox.
            panelPwdPrompt.Controls[1].Select();
        }

        /// <summary>
        /// Return the password entered by the user for the given email address.
        /// This function is case sensitive.
        /// </summary>
        public String GetPassword(String emailAddress)
        {
            Debug.Assert(panelPwdPrompt.Controls.ContainsKey(emailAddress));
            Debug.Assert(panelPwdPrompt.Controls[emailAddress] is TextBox);
            Debug.Assert(chkUseSamePwd.Checked || panelPwdPrompt.Controls[emailAddress].Text != "");

            if (chkUseSamePwd.Checked) return txtSamePwd.Text;
            else return panelPwdPrompt.Controls[emailAddress].Text;
        }

        public void ChangeEventHandler(object sender, EventArgs e)
        {
            if (OnChange != null) OnChange(sender, e);
        }

        private void chkUseSamePwd_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSamePwdControls();
        }

        private void lnkWhy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                frmInvitationPwdPromptExpl f = new frmInvitationPwdPromptExpl();
                f.NoPwdRequired = m_noPwdRequired;
                f.AlreadyInvited = m_alreadyInvited;
                f.UpdateUI();
                f.ShowDialog();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}
