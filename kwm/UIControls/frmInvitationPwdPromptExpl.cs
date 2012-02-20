using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;
using System.Diagnostics;

namespace kwm
{
    public partial class frmInvitationPwdPromptExpl : kwm.frmKBaseForm
    {
        /// <summary>
        /// List of email addresses that are already invited to the Teambox.
        /// </summary>
        public List<KwsInviteOpUser> AlreadyInvited = null;

        /// <summary>
        /// List of email addresses that are going to be invited by key id.
        /// </summary>
        public List<KwsInviteOpUser> NoPwdRequired = null;

        public frmInvitationPwdPromptExpl()
        {
            InitializeComponent();
        }

        public void UpdateUI()
        {
            Debug.Assert(AlreadyInvited != null || NoPwdRequired != null);

            splitBottom.Panel1Collapsed = (AlreadyInvited.Count == 0);
            splitBottom.Panel2Collapsed = (NoPwdRequired.Count == 0);

            if (AlreadyInvited.Count > 0) FillAlreadyInvited();
            if (NoPwdRequired.Count > 0) FillNoPwdReq();

            ResizeForm();
        }

        /// <summary>
        /// Fill the UI control with the email addresses that are already
        /// invited to the teambox.
        /// </summary>
        private void FillAlreadyInvited()
        {
            List<Label> labels = new List<Label>();

            int i = 0;
            foreach (KwsInviteOpUser u in AlreadyInvited)
            {
                Label l = new Label();
                l.Text = u.EmailAddress;
                l.AutoSize = false;
                l.Width = 150;
                l.Location = new Point(5, i * l.Height + 3);
                labels.Add(l);
                i++;
            }

            panelAlreadyInvited.Controls.AddRange(labels.ToArray());
        }

        /// <summary>
        /// Fill the UI control with the email addresses that will be invited
        /// using a key id instead of a password.
        /// </summary>
        private void FillNoPwdReq()
        {
            List<Label> labels = new List<Label>();

            int i = 0;
            foreach (KwsInviteOpUser u in NoPwdRequired)
            {
                Label l = new Label();
                l.Text = u.EmailAddress;
                l.AutoSize = false;
                l.Width = 150;
                l.Location = new Point(5, i * l.Height + 3);
                labels.Add(l);
                i++;
            }

            panelNoPwdReq.Controls.AddRange(labels.ToArray());
        }

        private void ResizeForm()
        {
            int h = panel1.Location.Y + 50;
            if (!splitBottom.Panel1Collapsed) h += splitBottom.Panel1.Height;
            if (!splitBottom.Panel2Collapsed) h += splitBottom.Panel2.Height;

            this.Height = h;
        }
    }
}

