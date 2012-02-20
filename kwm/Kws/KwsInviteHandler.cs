using System;
using System.Diagnostics;
using kwm.KwmAppControls;
using System.Collections.Generic;
using System.Runtime.Serialization;
using kwm.Utils;
using System.IO;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// This class contains code common to operations where users are invited.
    /// </summary>
    public class KwsInviteOpHelper
    {
        /// <summary>
        /// Reference to the operation.
        /// </summary>
        public KwsCoreOp Op;

        /// <summary>
        /// Reference to the credentials.
        /// </summary>
        public KwsCredentials Creds;

        /// <summary>
        /// Users to invite.
        /// </summary>
        public KwsInviteOpParams InviteParams = new KwsInviteOpParams();

        /// <summary>
        /// Return a reference to the create workspace wizard form.
        /// </summary>
        public frmCreateKwsWizard Wiz
        {
            get { return (frmCreateKwsWizard)Op.m_wizard; }
        }

        public KwsInviteOpHelper(KwsCoreOp op, KwsCredentials creds)
        {
            Op = op;
            Creds = creds;
        }

        /// <summary>
        /// Lookup the recipient addresses to determine if they are members.
        /// Return true if a lookup is being performed.
        /// </summary>
        public bool LookupRecAddr(KmodQueryDelegate callback, bool showWizardFlag)
        {
            // Clear the cached recipient security information.
            foreach (KwsInviteOpUser u in InviteParams.UserArray)
            {
                u.KeyID = 0;
                u.OrgName = "";
                u.Pwd = "";
            }

            // If the workspace is open, if there are no recipients to 
            // invite or if all recipients are already invited, skip the lookup.
            if (!Creds.SecureFlag || InviteParams.NotAlreadyInvitedUserArray.Count == 0) 
                return false;

            // Perform the key lookup.
            Op.m_opStep = KwmCoreKwsOpStep.LookupRec;

            // Fill out the server information.
            K3p.K3pSetServerInfo ssi = new K3p.K3pSetServerInfo();
            WmK3pServerInfo.RegToServerInfo(WmWinRegistry.Spawn(), ssi.Info);

            // Fill out the recipient array with email addresses that are not
            // already invited to the workspace.
            List<String> addrList = new List<String>();
            foreach (KwsInviteOpUser u in InviteParams.NotAlreadyInvitedUserArray)
                addrList.Add(u.EmailAddress);
                
            K3p.kpp_lookup_rec_addr lra = new K3p.kpp_lookup_rec_addr();
            lra.AddrArray = addrList.ToArray();

            // Send the query.
            Op.m_kmodQuery = new KmodQuery();
            Op.m_kmodQuery.Submit(Op.Wm.KmodBroker, new K3pCmd[] { ssi, lra }, callback);

            // Tell the wizard to wait.
            if (showWizardFlag) Wiz.ShowPleaseWait("Looking up who needs a password");

            return true;
        }

        /// <summary>
        /// Handle the results of the lookup query.
        /// </summary>
        public void HandleLookupRecAddrResult(KmodQuery query)
        {
            Debug.Assert(Op.m_kmodQuery == query);
            Op.m_kmodQuery = null;

            try
            {
                K3p.kmo_lookup_rec_addr res = query.OutMsg as K3p.kmo_lookup_rec_addr;

                // We got valid results.
                if (res != null)
                {
                    if (res.RecArray.Length != (UInt32)InviteParams.NotAlreadyInvitedUserArray.Count)
                        throw new Exception("invalid number of users in lookup");

                    for (int i = 0; i < InviteParams.NotAlreadyInvitedUserArray.Count; i++)
                    {
                        KwsInviteOpUser u = InviteParams.NotAlreadyInvitedUserArray[i];
                        K3p.kmo_lookup_rec_addr_rec a = res.RecArray[i];
                        u.KeyID = (a.KeyID == "") ? 0 : Convert.ToUInt64(a.KeyID);
                        u.OrgName = a.OrgName;

                        Logging.Log("user " + u.EmailAddress + ": " + u.KeyID + " (" + (u.OrgName == "" ? "<none>" : u.OrgName) + ")");
                    }
                }

                else throw new Exception(query.OutDesc);
            }

            // Ignore the failed query.
            catch (Exception ex)
            {
                Logging.Log(2, "The query to lookup the recipient addresses failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Return true if the user must be prompted for passwords.
        /// </summary>
        public bool MustPromptForPwd()
        {
            if (!Creds.SecureFlag) return false;
            foreach (KwsInviteOpUser u in InviteParams.NotAlreadyInvitedUserArray) if (u.KeyID == 0) return true;
            return false;
        }

        /// <summary>
        /// Process the invitations on the KAS.
        /// </summary>
        public void PerformKasInvite(KasQueryDelegate callback)
        {
            // Send an invitation command to the KAS.
            Op.m_opStep = KwmCoreKwsOpStep.KasInvite;

            AnpMsg msg = Op.m_kws.NewKAnpCmd(KAnpType.KANP_CMD_KWS_INVITE_KWS);
            msg.AddString(InviteParams.Message);
            msg.AddUInt32((UInt32)InviteParams.UserArray.Count);

            foreach (KwsInviteOpUser u in InviteParams.UserArray)
            {
                msg.AddString(u.UserName);
                msg.AddString(u.EmailAddress);
                msg.AddUInt64(u.KeyID);
                msg.AddString(u.OrgName);
                msg.AddString(u.Pwd);
                msg.AddUInt32(InviteParams.KcdSendInvitationEmailFlag ? (UInt32)1 : 0);
            }

            Op.m_kws.PostNonAppKasQuery(msg, null, callback, true);
        }

        /// <summary>
        /// Called when the invite to workspace command reply is received.
        /// Return true if the invitation was successful.
        /// </summary>
        public bool HandleKasInviteResult(KasQuery query)
        {
            try
            {
                AnpMsg res = query.Res;

                // Handle failures.
                if (res.Type != KAnpType.KANP_RES_KWS_INVITE_KWS)
                    throw Misc.HandleUnexpectedKAnpReply("invite to " + Base.GetKwsString(), res);

                // Update the invitation information.
                InviteParams.WLEU = res.Elements[0].String;

                if (res.Elements[1].UInt32 != (UInt32)InviteParams.UserArray.Count)
                    throw new Exception("invalid number of users invited");

                int i = 2;
                foreach (KwsInviteOpUser u in InviteParams.UserArray)
                {
                    u.EmailID = res.Elements[i++].String;
                    u.Url = res.Elements[i++].String;
                    u.Error = res.Elements[i++].String;
                }

                return true;
            }

            catch (Exception ex)
            {
                Op.HandleMiscFailure(ex);
                return false;
            }
        }

        /// <summary>
        /// Fill the content of InviteParams from the content of the Outlook 
        /// Create or Invite command specified.
        /// </summary>
        public void FillInviteParamsFromOutlookCmd(AnpMsg cmd, int index)
        {
            InviteParams.UserArray.Clear();
            UInt32 nbUser = cmd.Elements[index++].UInt32;
            for (UInt32 i = 0; i < nbUser; i++)
            {
                KwsInviteOpUser iu = new KwsInviteOpUser();
                InviteParams.UserArray.Add(iu);
                iu.UserName = cmd.Elements[index++].String;
                iu.EmailAddress = cmd.Elements[index++].String;
                iu.KeyID = cmd.Elements[index++].UInt64;
                iu.OrgName = cmd.Elements[index++].String;
                iu.Pwd = cmd.Elements[index++].String;
            }
        }

        /// <summary>
        /// Fill the content of InviteParams from the content of the Outlook 
        /// LookupRecAddr command specified.
        /// </summary>
        public void FillInviteParamsFromOutlookLookupRecCmd(AnpMsg cmd)
        {
            InviteParams.UserArray.Clear();
            UInt32 nbUser = cmd.Elements[0].UInt32;
            for (int i = 1; i <= nbUser; i++)
            {
                KwsInviteOpUser iu = new KwsInviteOpUser();
                InviteParams.UserArray.Add(iu);
                iu.EmailAddress = cmd.Elements[i].String;
            }
        }

        /// <summary>
        /// Fill the content of the Outlook result specified from the content
        /// of InviteParams.
        /// </summary>
        public void FillOutlookResultWithInviteParms(AnpMsg res)
        {
            res.AddString(InviteParams.WLEU);
            res.AddUInt32((UInt32)InviteParams.UserArray.Count);
            foreach (KwsInviteOpUser iu in InviteParams.UserArray)
            {
                res.AddString(iu.EmailAddress);
                res.AddString(iu.Url);
            }
        }
    }

    /// <summary>
    /// Base class for the KwmInviteOp and OutlookInviteOp.
    /// </summary>
    public class BaseInviteOp : KwsCoreOp
    {
        /// <summary>
        /// Reference to the invite helper.
        /// </summary>
        protected KwsInviteOpHelper m_ih;
        
        /// <summary>
        /// Users to invite.
        /// </summary>
        public KwsInviteOpParams InviteParams
        {
            get { return m_ih.InviteParams; }
            set { m_ih.InviteParams = value; }
        }

        public BaseInviteOp(WorkspaceManager wm, Workspace kws)
            : base(wm)
        {
            m_kws = kws;
            m_ih = new KwsInviteOpHelper(this, m_kws.CoreData.Credentials);
        }
    }

    /// <summary>
    /// Invite users workspace operation of the KWM.
    /// </summary>
    public class KwmInviteOp : BaseInviteOp
    {
        /// <summary>
        /// Text entered by the user to pre-populate the wizard, if any.
        /// </summary>
        public String Invitees = "";

        /// <summary>
        /// When set to true, the first page of the wizard prompting for a 
        /// personalized message is skipped.
        /// </summary>
        public bool SkipFirstPage;
        
        public KwmInviteOp(WorkspaceManager wm, Workspace kws, String invitees, bool skipFirstPageFlag)
            : base(wm, kws)
        {
            Invitees = invitees;
            SkipFirstPage = skipFirstPageFlag;
        }

        public override void HandleMiscFailure(Exception ex)
        {
            CancelOp(false, ex.Message);
        }

        /// <summary>
        /// Start the operation. This method blocks.
        /// </summary>
        public KwmCoreKwsOpRes StartOp()
        {
            Debug.Assert(m_opStep == KwmCoreKwsOpStep.None);
            m_opStep = KwmCoreKwsOpStep.WizardEntry;
            m_wizard = new frmCreateKwsWizard(this, Wm.KmodBroker);
            m_ih.Wiz.ShowAndReenter();
            return OpRes;
        }

        /// <summary>
        /// Cancel the operation in progress. The wizard end page is shown
        /// unless cancelWizardFlag is true, in which case the wizard is 
        /// cancelled as well. This can be called from any context.
        /// </summary>
        public void CancelOp(bool cancelWizardFlag, String reason)
        {
            if (m_doneFlag) return;

            if (!wizEntryFlag) cancelWizardFlag = true;
            OpRes = KwmCoreKwsOpRes.MiscError;
            ErrorString = reason;
            UnregisterFromKws(true);
            if (!cancelWizardFlag) ShowWizardEnd();

            m_doneFlag = true;
        }

        /// <summary>
        /// Called by the wizard when it has displayed itself.
        /// </summary>
        public void WizEntry()
        {
            if (!CheckCtx(KwmCoreKwsOpStep.WizardEntry)) return;
            wizEntryFlag = true;
            m_opStep = KwmCoreKwsOpStep.UIStep;
            m_uiStep = KwmCoreKwsUiStep.Create;
            if (!CheckRegisterToKws()) return;
            RegisterToKws(true);
            m_ih.Wiz.ShowInvitePage();
        }

        /// <summary>
        /// Called when the wizard has obtained the invited users information.
        /// </summary>
        public void WizInvitePageNext()
        {
            if (!CheckCtx(KwmCoreKwsOpStep.UIStep)) return;
            m_uiStep = KwmCoreKwsUiStep.Invite;
            if (!m_ih.LookupRecAddr(HandleLookupRecAddrResult, true)) PromptForPwd();
        }

        /// <summary>
        /// Called when the wizard has obtained the passwords we needed.
        /// </summary>
        public void WizPasswordPageNext()
        {
            if (!CheckCtx(KwmCoreKwsOpStep.UIStep)) return;
            m_uiStep = KwmCoreKwsUiStep.Pwd;
            PerformKasInvite();
        }

        /// <summary>
        /// Handle the results of the lookup query.
        /// </summary>
        private void HandleLookupRecAddrResult(KmodQuery query)
        {
            if (!CheckCtx(KwmCoreKwsOpStep.LookupRec)) return;
            m_ih.HandleLookupRecAddrResult(query);
            PromptForPwd();
        }

        /// <summary>
        /// Prompt the user for the passwords if required.
        /// </summary>
        private void PromptForPwd()
        {
            if (m_ih.MustPromptForPwd())
            {
                m_opStep = KwmCoreKwsOpStep.UIStep;
                m_ih.Wiz.ShowPasswordPage();
            }

            else
            {
                m_uiStep = KwmCoreKwsUiStep.Pwd;
                PerformKasInvite();
            }
        }

        /// <summary>
        /// Process the invitations on the KAS.
        /// </summary>
        private void PerformKasInvite()
        {
            if (InviteParams.UserArray.Count == 0) CompleteOp();
            else m_ih.PerformKasInvite(HandleKasInviteResult);
        }

        /// <summary>
        /// Called when the invite to workspace command reply is received.
        /// </summary>
        private void HandleKasInviteResult(KasQuery query)
        {
            if (!CheckCtx(KwmCoreKwsOpStep.KasInvite)) return;
            if (m_ih.HandleKasInviteResult(query)) CompleteOp();
        }

        /// <summary>
        /// Called when the operation has been completed.
        /// </summary>
        private void CompleteOp()
        {
            OpRes = KwmCoreKwsOpRes.Success;

            // Handle the case where no recipient has been successfully invited
            // if required.
            if (m_ih.InviteParams.UserArray.Count > 0)
            {
                bool failFlag = true;
                String err = "";

                foreach (KwsInviteOpUser u in m_ih.InviteParams.UserArray)
                {
                    if (u.Error != "")
                    {
                        err = u.Error;
                    }

                    else
                    {
                        failFlag = false;
                        break;
                    }
                }

                if (failFlag)
                {
                    ErrorString = err;
                    OpRes = KwmCoreKwsOpRes.MiscError;
                }
            }

            UnregisterFromKws(true);
            ShowWizardEnd();
            m_doneFlag = true;
        }

        /// <summary>
        /// Ask the wizard to display the result page.
        /// </summary>
        private void ShowWizardEnd()
        {
            Debug.Assert(OpRes != KwmCoreKwsOpRes.None);
            m_opStep = KwmCoreKwsOpStep.End;
            m_ih.Wiz.ShowResults();
        }
    }

    /// <summary>
    /// Outlook invitation operation.
    /// </summary>
    public class OutlookInviteOp : BaseInviteOp
    {
        public OutlookInviteOp(WorkspaceManager wm, Workspace kws, WmOutlookRequest request)
            : base(wm, kws)
        {
            RegisterOutlookRequest(request);
        }

        public override void HandleMiscFailure(Exception ex)
        {
            if (m_doneFlag) return;
            m_doneFlag = true;
            UnregisterFromKws(true);
            m_outlookRequest.SendFailure(ex.Message);
        }

        /// <summary>
        /// Start the operation.
        /// </summary>
        public void StartOp()
        {
            Debug.Assert(m_opStep == KwmCoreKwsOpStep.None);
            if (!CheckRegisterToKws()) return;
            RegisterToKws(true);
            if (ParseOutlookRequest()) PerformKasInvite();
        }

        /// <summary>
        /// Parse the request from Outlook. Return true on success.
        /// </summary>
        private bool ParseOutlookRequest()
        {
            try
            {
                m_ih.FillInviteParamsFromOutlookCmd(m_outlookRequest.Cmd, 1);
                InviteParams.FlagInvitedUsers(m_kws.CoreData.UserInfo);
                InviteParams.UserArray = InviteParams.NotAlreadyInvitedUserArray;
                return true;
            }

            catch (Exception ex)
            {
                HandleMiscFailure(ex);
                return false;
            }
        }

        /// <summary>
        /// Process the invitations on the KAS.
        /// </summary>
        private void PerformKasInvite()
        {
            m_ih.PerformKasInvite(HandleKasInviteResult);
        }

        /// <summary>
        /// Called when the invite to workspace command reply is received.
        /// </summary>
        private void HandleKasInviteResult(KasQuery query)
        {
            if (!CheckCtx(KwmCoreKwsOpStep.KasInvite)) return;
            if (m_ih.HandleKasInviteResult(query)) CompleteOp();
        }

        /// <summary>
        /// Called when the operation has been completed.
        /// </summary>
        private void CompleteOp()
        {
            // Send the reply to Outlook.
            AnpMsg res = m_outlookRequest.MakeReply(OAnpType.OANP_RES_INVITE_TO_KWS);
            m_ih.FillOutlookResultWithInviteParms(res);
            m_outlookRequest.SendReply(res);

            // We're done.
            UnregisterFromKws(true);
            m_doneFlag = true;
        }
    }

    /// <summary>
    /// Outlook lookup recipient address operation. This is not bound to a 
    /// workspace but we pretend it is for convenience.
    /// </summary>
    public class OutlookLookupRecAddrOp : KwsCoreOp
    {
        /// <summary>
        /// Reference to the invite helper.
        /// </summary>
        protected KwsInviteOpHelper m_ih;

        /// <summary>
        /// Users to invite.
        /// </summary>
        public KwsInviteOpParams InviteParams
        {
            get { return m_ih.InviteParams; }
            set { m_ih.InviteParams = value; }
        }

        public OutlookLookupRecAddrOp(WorkspaceManager wm, WmOutlookRequest request)
            : base(wm)
        {
            KwsCredentials creds = new KwsCredentials();
            creds.SecureFlag = true;
            m_ih = new KwsInviteOpHelper(this, creds);
            m_ih.FillInviteParamsFromOutlookLookupRecCmd(request.Cmd);
            RegisterOutlookRequest(request);
        }

        public override void HandleMiscFailure(Exception ex)
        {
            if (m_doneFlag) return;
            m_doneFlag = true;
            ClearKmodQuery();
            m_outlookRequest.SendFailure(ex.Message);
        }

        /// <summary>
        /// Start the operation.
        /// </summary>
        public void StartOp()
        {
            if (!m_ih.LookupRecAddr(HandleLookupRecAddrResult, false)) CompleteOp();
        }

        /// <summary>
        /// Handle the results of the lookup query.
        /// </summary>
        private void HandleLookupRecAddrResult(KmodQuery query)
        {
            if (!CheckCtx(KwmCoreKwsOpStep.LookupRec)) return;
            m_ih.HandleLookupRecAddrResult(query);
            CompleteOp();
        }

        /// <summary>
        /// Called when the operation has completed successfully.
        /// </summary>
        private void CompleteOp()
        {
            // Send the reply.
            AnpMsg res = m_outlookRequest.MakeReply(OAnpType.OANP_RES_LOOKUP_REC_ADDR);
            res.AddUInt32((UInt32)InviteParams.UserArray.Count);
            foreach (KwsInviteOpUser iu in InviteParams.UserArray)
            {
                res.AddString(iu.EmailAddress);
                res.AddUInt64(iu.KeyID);
                res.AddString(iu.OrgName);
            }
            m_outlookRequest.SendReply(res);

            // We're done.
            m_doneFlag = true;
        }
    }
}