using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Tbx.Utils;
using kwm.KwmAppControls;
using System.Diagnostics;

namespace kwm
{
    public partial class frmUserProperties : frmKBaseForm
    {
        private WmUiBroker m_uiBroker;

        public frmUserProperties()
        {
            InitializeComponent();
        }

        public frmUserProperties(WmUiBroker broker) : this()
        {
            m_uiBroker = broker;
            InitUI();
        }

        private void InitUI()
        {
            KwsUser targetUser = m_uiBroker.GetSelectedUser();
            Debug.Assert(targetUser != null); 
            
            FillFields(targetUser);
            SetFieldStatus(targetUser);
        }

        private void FillFields(KwsUser targetUser)
        {
            if (targetUser.UserName == "")
            {
                lblUserName.Text = targetUser.EmailAddress;
            }
            else
            {
                lblUserName.Text = targetUser.UiSimpleName;
                lblUserEmail.Text = targetUser.EmailAddress;
            }

            txtUserName.Text = targetUser.HasAdminName() ? targetUser.AdminName : targetUser.UserName;

            // FIXME adapt for new roles.
            cboRole.SelectedIndex = targetUser.Power == 0 ? 2 : 0;

            // FIXME adapt disabled account.
        }

        private void SetFieldStatus(KwsUser targetUser)
        {
            String deniedExpl = "";
            txtUserName.Enabled = m_uiBroker.CanPerformUserAction(UserAction.SetName, m_uiBroker.Browser.SelectedKws, targetUser, ref deniedExpl);
            if (txtUserName.Enabled) txtUserName.Tag = "You can change this user's display name here.";
            else txtUserName.Tag = deniedExpl;
            cboRole.Enabled = m_uiBroker.CanPerformUserAction(UserAction.ChangeRole, m_uiBroker.Browser.SelectedKws, targetUser, ref deniedExpl);
            chkDisabledAccount.Enabled = m_uiBroker.CanPerformUserAction(UserAction.ChangeDisabledAccountFlag, m_uiBroker.Browser.SelectedKws, targetUser, ref deniedExpl);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            String msg = "A disabled account prevents that particular user from logging on to the " +
                         Base.GetKwsString() + ". " + Environment.NewLine + Environment.NewLine +
                         "This can be used to suspend the access instead of removing it permanently " +
                         "by removing the user from the " + Base.GetKwsString() + ".";

            Help.ShowPopup(sender as Control, msg, new Point(Cursor.Position.X, Cursor.Position.Y + 20));
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Help.ShowPopup(sender as Control, "More details on role and their restrictions, see the User Guide available under the Help menu.", new Point(Cursor.Position.X, Cursor.Position.Y + 20));
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            Help.ShowPopup(sender as Control, txtUserName.Tag as String, new Point(Cursor.Position.X, Cursor.Position.Y + 20));
        }
    }
}
