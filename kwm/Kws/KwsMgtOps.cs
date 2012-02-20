using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Tbx.Utils;
using kwm.KwmAppControls;
using System.Windows.Forms;
using kwm.Utils;

namespace kwm
{
    /// <summary>
    /// This class is used to set any user property.
    /// </summary>
    public class KwsSetUserProperty : KwsCoreOp
    {
        private UInt32 m_targetUserId;
        private String m_stringParam;
        private UInt32 m_flagParam;

        /// <summary>
        /// Set password query.
        /// </summary>
        private WmKasQuery m_query;

        /// <summary>
        /// Form asking the user to wait.
        /// </summary>
        private frmPleaseWait m_frmPleaseWait = new frmPleaseWait();
        
        public KwsSetUserProperty(WorkspaceManager wm, Workspace kws, UInt32 userId, String newPwd)
            : base(wm)
        {
            m_kws = kws;
            m_targetUserId = userId;
            m_stringParam = newPwd;
        }

        public override void HandleMiscFailure(Exception ex)
        {
            Cancel(false, ex.Message);
        }

        /// <summary>
        /// Cancel the operation.
        /// </summary>
        public void Cancel(bool userCancelFlag, String reason)
        {
            if (m_doneFlag) return;
            if (!userCancelFlag) ReportFailureToUser(reason);
            CleanUpOnDone();
        }

        public void PerformQuery()
        {
            // Start listening to the workspace status events.
            RegisterToKws(false);

            m_opStep = KwmCoreKwsOpStep.SetUserPwd;
            AnpMsg msg = m_kws.NewKAnpCmd(KAnpType.KANP_CMD_MGT_SET_USER_PWD);
            msg.AddUInt32(m_targetUserId);
            msg.AddString(m_stringParam);
            m_query = m_kws.PostNonAppKasQuery(msg, null, HandleSetUserPwdResponse, true);

            if (m_frmPleaseWait.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                Cancel(true, "");
        }

        private void HandleSetUserPwdResponse(KasQuery ctx)
        {
            if (!CheckCtx(KwmCoreKwsOpStep.SetUserPwd)) return;
            Debug.Assert(m_query == ctx);
            m_query = null;

            // FIXME if version >= 4 check against KANP_RES_KWS_PROP_CHANGE
            if (ctx.Res.Type == KAnpType.KANP_RES_OK)
            {
                m_kws.PostGuiExecRequest(new SetPwdGer(MessageBoxIcon.Information,
                                                       "The new password has been set."));
                
                // We have changed our own password. Save it.
                if (m_kws.CoreData.Credentials.UserID == m_targetUserId)
                {
                    m_kws.CoreData.Credentials.Pwd = m_stringParam;
                    m_kws.SetDirty();
                }
            }

            else
            {
                try
                {
                    String msg = "Unable to set the new password: " + ctx.Res.Elements[1].String;
                    ReportFailureToUser(msg);
                }

                catch (Exception ex)
                {
                    HandleMiscFailure(ex);
                    return;
                }
            }

            CleanUpOnDone();
        }

        /// <summary>
        /// Report the failure specified to the user.
        /// </summary>
        private void ReportFailureToUser(String error)
        {
            m_kws.PostGuiExecRequest(new SetPwdGer(MessageBoxIcon.Error, error));
        }

        /// <summary>
        /// Clean up the operation when it has completed.
        /// </summary>
        private void CleanUpOnDone()
        {
            if (m_doneFlag) return;
            m_doneFlag = true;

            UnregisterFromKws(false);

            if (m_query != null)
            {
                m_query.Cancel();
                m_query = null;
            }

            if (m_frmPleaseWait != null)
            {
                m_frmPleaseWait.Close();
                m_frmPleaseWait.Dispose();
                m_frmPleaseWait = null;
            }
        }

    }

    /// <summary>
    /// Ger required to let the user know the result of its SetUserPwd
    /// operation. Since we receive the result from a callback called by
    /// the workspace's state machine, it is imperative to use a Ger and not
    /// prompt directly.
    /// </summary>
    public class SetPwdGer : GuiExecRequest
    {
        MessageBoxIcon m_icon;
        String m_msg;

        public SetPwdGer(MessageBoxIcon icon, String message)
        {
            m_icon = icon;
            m_msg = message;
        }

        public override void Run()
        {
            Misc.KwmTellUser(m_msg, m_icon);
        }
    }
}
