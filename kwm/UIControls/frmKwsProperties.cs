using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Tbx.Utils;
using System.Diagnostics;
using kwm.Utils;

namespace kwm
{
    public partial class frmKwsProperties : frmKBaseForm
    {

        WmUiBroker m_uiBroker;

        public frmKwsProperties()
        {
            InitializeComponent();
        }

        public frmKwsProperties(WmUiBroker broker) : this()
        {
            m_uiBroker = broker;
            InitUI();
        }

        private void InitUI()
        {
            Workspace kws = m_uiBroker.Browser.SelectedKws;
            Debug.Assert(kws != null);

            txtCreator.Text = kws.CoreData.UserInfo.Creator.UiFullName;
            // FIXME show Freemium user if that is the case.
            lblOrganization.Text = kws.CoreData.UserInfo.Creator.OrgName;
            lblCreationDate.Text = Base.KDateToDateTime(kws.CoreData.UserInfo.Creator.InvitationDate).ToString();
            txtServer.Text = kws.CoreData.Credentials.KasID.Host;
            txtKwsName.Text = kws.CoreData.Credentials.KwsName;
            this.Text = kws.CoreData.Credentials.KwsName + " properties";
            lblKwsName.Text = kws.CoreData.Credentials.KwsName;
            if (kws.CoreData.Credentials.PublicFlag)
                rdoPublic.Checked = true;
            else if (kws.CoreData.Credentials.SecureFlag)
                rdoSecure.Checked = true;
            else
                rdoStandard.Checked = true;
            
            // FIXME set moderated, locked and preserve deleted checkboxes status
            String ignored = "";
            txtKwsName.Enabled = m_uiBroker.CanPerformKwsAction(KwsAction.Rename, kws, ref ignored);
            grpKwsType.Enabled = m_uiBroker.CanPerformKwsAction(KwsAction.ChangeKwsType, kws, ref ignored);
            chkModerated.Enabled = m_uiBroker.CanPerformKwsAction(KwsAction.ChangeModerationFlag, kws, ref ignored);
            chkLocked.Enabled = m_uiBroker.CanPerformKwsAction(KwsAction.ChangeLockFlag, kws, ref ignored);
            grpKwsStatus.Enabled = chkModerated.Enabled || chkLocked.Enabled;

            chkPreserve.Enabled = m_uiBroker.CanPerformKwsAction(KwsAction.ChangePreserveDeletedFlag, kws, ref ignored);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            String msg = "A moderated " + Base.GetKwsString() +
                         " restricts the right to modify the " + Base.GetKwsString() +
                         " to Administrators only. For example, no files can be added" +
                         " or removed, no screen sharing session can be started, etc." +
                         Environment.NewLine +
                         "A locked " + Base.GetKwsString() + " goes one step further and extends the restrictions" +
                         " even to Administrators. Only the Server Administrator can lock or unlock a " + Base.GetKwsString() + ".";
            Misc.ShowHelpTooltip(msg, sender as Control);
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            String msg = "By default, when a file is deleted from a " + Base.GetKwsString() +
                         ", it is permanently removed. You may override this setting and preserve " +
                         "all deleted files until this setting is changed." + Environment.NewLine + Environment.NewLine +
                         "Keep in mind that the deleted files will still count in your " + Base.GetKwsString() + " storage quota.";
            Misc.ShowHelpTooltip(msg, sender as Control);
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            String msg = "There exists three different " + Base.GetKwsString() + " types." +
                         Environment.NewLine +
                         "Public: " + Base.GetPublicKwsDescription() +
                         Environment.NewLine +
                         "Standard: " + Base.GetStdKwsDescription() +
                         Environment.NewLine +
                         "Secure: " + Base.GetSecureKwsDescription();
            Misc.ShowHelpTooltip(msg, sender as Control);
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            String msg = "Shows which organization authenticated the " + Base.GetKwsString() + " creator. Freemium users are not authenticated by anyone and should not be trusted.";
            Misc.ShowHelpTooltip(msg, sender as Control);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            /* Check what changed
             * Send the appropriate commands and wait for all the events to come in. 
             * Give feedback to the user in real time?
             * On success, close the window.
             * 
             * */
        }
    }
}