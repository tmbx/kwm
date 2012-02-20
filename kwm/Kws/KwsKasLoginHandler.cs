using System;
using System.Collections.Generic;
using System.Text;
using kwm.KwmAppControls;
using System.Diagnostics;
using kwm.Utils;
using System.Windows.Forms;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// Type of workspace login being performed.
    /// </summary>
    public enum KwsLoginType
    {
        /// <summary>
        /// Only the cached login step can be performed.
        /// </summary>
        Cached,

        /// <summary>
        /// Both the cached and the ticket login steps can be performed.
        /// </summary>
        NoPwdPrompt,

        /// <summary>
        /// All the login steps may be performed.
        /// </summary>
        All
    }

    /// <summary>
    /// Workspace login step being performed.
    /// </summary>
    public enum KwsLoginStep
    {
        /// <summary>
        /// No steps yet.
        /// </summary>
        None,

        /// <summary>
        /// Login using the cached workspace credentials.
        /// </summary>
        Cached,

        /// <summary>
        /// Login using a ticket obtained from the KPS.
        /// </summary>
        Ticket,

        /// <summary>
        /// Login using a password obtained by prompting the user.
        /// </summary>
        Pwd
    }

    /// <summary>
    /// Outcome of the last login attempt of a workspace.
    /// </summary>
    public enum KwsLoginResult
    {
        /// <summary>
        /// No attempt has been made yet.
        /// </summary>
        None,

        /// <summary>
        /// The credentials have been accepted by the KCD.
        /// </summary>
        Accepted,

        /// <summary>
        /// The security credentials were refused.
        /// </summary>
        BadSecurityCreds,

        /// <summary>
        /// Special case of BadSecurityCreds: the user must provide a password.
        /// </summary>
        PwdRequired,

        /// <summary>
        /// The workspace ID is invalid.
        /// </summary>
        BadKwsID,

        /// <summary>
        /// The email ID is invalid or it has been purged from the database.
        /// </summary>
        BadEmailID,

        /// <summary>
        /// The workspace has been deleted.
        /// </summary>
        DeletedKws,

        /// <summary>
        /// The user account has been locked.
        /// </summary>
        AccountLocked,

        /// <summary>
        /// The credentials are accepted but the login failed since the 
        /// information about the last event received is invalid, probably
        /// because the server has lost some events. All events must be 
        /// refetched from the server.
        /// </summary>
        OOS,

        /// <summary>
        /// No ticket could be obtained from the KPS when attempting to login
        /// using a ticket.
        /// </summary>
        CannotGetTicket,

        /// <summary>
        /// A miscellaneous KCD error occurred.
        /// </summary>
        MiscKcdError
    }

    /// <summary>
    /// Information contained in a KANP_RES_KWS_CONNECT_KWS reply.
    /// </summary>
    public class KwsConnectRes
    {
        public UInt32 Code;
        public String ErrMsg;
        public UInt32 UserID;
        public String EmailID;
        public UInt64 LoginLatestEventID;
        public bool SecureFlag;
        public bool PwdOnKcdFlag;
        public String KwmoAddress;

        public KwsConnectRes(AnpMsg res)
        {
            Code = res.Elements[0].UInt32;
            ErrMsg = res.Elements[1].String;
            UserID = res.Elements[2].UInt32;
            EmailID = res.Elements[3].String;
            LoginLatestEventID = res.Elements[4].UInt64;
            SecureFlag = (res.Elements[5].UInt32 > 0);
            PwdOnKcdFlag = (res.Elements[6].UInt32 > 0);
            KwmoAddress = res.Elements[7].String;
        }
    }

    /// <summary>
    /// Workspace KAS login handler. 
    /// </summary>
    [Serializable]
    public class KwsKasLoginHandler
    {
        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        [NonSerialized]
        private WorkspaceManager m_wm;

        /// <summary>
        /// Reference to the workspace.
        /// </summary>
        [NonSerialized]
        private Workspace m_kws;

        /// <summary>
        /// Outcome of the last login attempt of a workspace.
        /// </summary>
        public KwsLoginResult LoginResult = KwsLoginResult.None;

        /// <summary>
        /// String describing the login result.
        /// </summary>
        public String LoginResultString = "";

        /// <summary>
        /// String describing why the ticket was refused by the server.
        /// </summary>
        public String TicketRefusalString = "";

        /// <summary>
        /// True if the KCD has told us that the user has a password.
        /// </summary>
        [NonSerialized]
        public bool PwdOnKcdFlag = false;

        /// <summary>
        /// Current login step being performed.
        /// </summary>
        [NonSerialized]
        private KwsLoginStep m_currentStep = KwsLoginStep.None;

        /// <summary>
        /// Login type.
        /// </summary>
        [NonSerialized]
        private KwsLoginType m_loginType = KwsLoginType.All;

        /// <summary>
        /// Ticket query currently under way, if any.
        /// </summary>
        [NonSerialized]
        private WmLoginTicketQuery m_ticketQuery = null;

        /// <summary>
        /// Pending request for the user password, if any.
        /// </summary>
        [NonSerialized]
        private LoginHandlerPwdPromptGer m_pwdRequest = null;

        /// <summary>
        /// Set the reference to the WM and workspace.
        /// </summary>
        public void SetRef(WorkspaceManager wm, Workspace kws)
        {
            m_wm = wm;
            m_kws = kws;
        }

        /// <summary>
        /// Cancel and clear the ticket query and the password request, if
        /// required and set the current login step to None.
        /// </summary>
        public void ClearAllQueries()
        {
            m_currentStep = KwsLoginStep.None;
            ClearTicketQuery();
            ClearPwdRequest();
        }

        /// <summary>
        /// Set the login type. This method shouldn't be called outside the
        /// state machine.
        /// </summary>
        public void SetLoginType(KwsLoginType type)
        {
            m_loginType = type;
        }

        /// <summary>
        /// Called by the workspace state machine to log in the workspace.
        /// </summary>
        public void PerformLogin()
        {
            Debug.Assert(m_currentStep == KwsLoginStep.None);

            // We perform the cached step if explicitly required, if the 
            // workspace is open, if we have a ticket and a password, or if we
            // cannot login on the KPS. The latter condition is necessary since
            // we have to try to login once to determine whether a password is
            // available on the KCD. Otherwise, we perform the ticket step
            // directly.
            KwsCredentials creds = m_kws.CoreData.Credentials;
            bool cachedFlag = (m_loginType == KwsLoginType.Cached ||
                               !creds.SecureFlag ||
                               creds.Ticket != null ||
                               creds.Pwd != "");

            // Optimization: it's preferable to access the registry as little 
            // as possible.
            WmWinRegistry registry = null;
            if (!cachedFlag)
            {
                registry = WmWinRegistry.Spawn();
                cachedFlag = !registry.CanLoginOnKps();
            }

            if (cachedFlag) HandleCachedLoginStep();
            else HandleTicketLoginStep(registry);
        }

        /// <summary>
        /// Called by the workspace state machine to log out of the workspace
        /// normally.
        /// </summary>
        public void PerformLogout()
        {
            // Send the logout command.
            AnpMsg msg = m_kws.NewKAnpCmd(KAnpType.KANP_CMD_KWS_DISCONNECT_KWS);
            m_kws.PostNonAppKasQuery(msg, null, HandleDisconnectKwsReply, true);
        }

        // Cancel and clear the ticket query, if required.
        private void ClearTicketQuery()
        {
            if (m_ticketQuery != null)
            {
                m_ticketQuery.Cancel();
                m_ticketQuery = null;
            }
        }

        // Cancel and clear the password request, if required.
        private void ClearPwdRequest()
        {
            if (m_pwdRequest != null)
            {
                m_pwdRequest.Cancel();
                m_pwdRequest = null;
            }
        }

        /// <summary>
        /// Handle a login failure. The login result code and string are set,
        /// the workspace is set dirty and the state machine is notified.
        /// </summary>
        private void HandleLoginFailure(KwsLoginResult res, String resString)
        {
            LoginResult = res;
            LoginResultString = resString;
            m_kws.SetDirty();
            m_kws.Sm.HandleLoginFailure();
        }

        /// <summary>
        /// Send the cached credentials to the KAS.
        /// </summary>
        private void HandleCachedLoginStep()
        {
            m_currentStep = KwsLoginStep.Cached;
            SendLoginCommand();
        }

        /// <summary>
        /// Obtain a login ticket and login with it.
        /// </summary>
        private void HandleTicketLoginStep(WmWinRegistry registry)
        {
            m_currentStep = KwsLoginStep.Ticket;
            m_ticketQuery = new WmLoginTicketQuery();
            m_ticketQuery.Submit(m_wm.KmodBroker, registry, HandleTicketLoginResult);
        }

        /// <summary>
        /// Prompt the user for a password and login with it. 'failedFlag' is
        /// true if the last password provided is wrong.
        /// </summary>
        private void HandlePwdLoginStep(bool failedFlag)
        {
            m_currentStep = KwsLoginStep.Pwd;
            m_pwdRequest = new LoginHandlerPwdPromptGer(this, m_kws.CoreData.Credentials.KwsName, m_kws.CoreData.Credentials.Pwd, failedFlag);
            m_kws.PostGuiExecRequest(m_pwdRequest);
        }

        /// <summary>
        /// Handle the results of the ticket query.
        /// </summary>
        private void HandleTicketLoginResult(WmLoginTicketQuery query)
        {
            // Clear the query.
            Debug.Assert(m_ticketQuery == query);
            m_ticketQuery = null;

            // Update the registry, if required.
            query.UpdateRegistry(WmWinRegistry.Spawn());

            // The query failed.
            if (query.Res == WmLoginTicketQueryRes.InvalidCfg ||
                query.Res == WmLoginTicketQueryRes.MiscError)
            {
                HandleLoginFailure(KwsLoginResult.CannotGetTicket, "cannot obtain ticket: " + query.OutDesc);
            }

            // The query succeeded.
            else
            {
                // Update the credentials.
                m_kws.CoreData.Credentials.Ticket = query.Ticket.BinaryTicket;
                m_kws.SetDirty();

                // Send the login command.
                SendLoginCommand();
            }
        }

        /// <summary>
        /// Handle the result of the workspace user password prompt.
        /// </summary>
        public void HandlePwdPromptResult(LoginHandlerPwdPromptGer request, DialogResult res, String pwd)
        {
            Debug.Assert(m_pwdRequest == request);
            m_pwdRequest = null;

            // No password was provided.
            if (res != DialogResult.OK)
            {
                HandleLoginFailure(KwsLoginResult.PwdRequired, "password required");
            }

            // A password was provided.
            else
            {
                // Update the password.
                m_kws.CoreData.Credentials.Pwd = pwd;
                m_kws.SetDirty();

                // Send the login command.
                SendLoginCommand();
            }
        }

        /// <summary>
        /// Send a login command to the KAS.
        /// </summary>
        private void SendLoginCommand()
        {
            AnpMsg msg = m_kws.NewKAnpCmd(KAnpType.KANP_CMD_KWS_CONNECT_KWS);

            // Add the last event information.
            AnpMsg lastEvent = m_kws.GetLastEventInDb();

            if (lastEvent == null)
            {
                msg.AddUInt64(0);
                msg.AddUInt64(0);
            }

            else
            {
                msg.AddUInt64(lastEvent.ID);
                msg.AddUInt64(lastEvent.Elements[1].UInt64);
            }

            // Add the credential information.
            msg.AddUInt32(m_kws.CoreData.Credentials.UserID);
            msg.AddString(m_kws.CoreData.Credentials.UserName);
            msg.AddString(m_kws.CoreData.Credentials.UserEmailAddress);
            msg.AddString(m_kws.CoreData.Credentials.EmailID);

            // Send a ticket only if we're at the cached or ticket steps.
            byte[] ticket = null;
            if (m_currentStep != KwsLoginStep.Pwd) ticket = m_kws.CoreData.Credentials.Ticket;
            msg.AddBin(ticket);

            // Send a password only if we're at the cached or password steps.
            String pwd = "";
            if (m_currentStep != KwsLoginStep.Ticket) pwd = m_kws.CoreData.Credentials.Pwd;
            msg.AddString(pwd);

            // Post the login query.
            m_kws.PostNonAppKasQuery(msg, null, HandleConnectKwsReply, true);
        }

        /// <summary>
        /// Called when the login reply is received.
        /// </summary>
        private void HandleConnectKwsReply(KasQuery query)
        {
            Logging.Log("Got login reply, kws " + m_kws.InternalID + ", status " + m_kws.MainStatus);
            
            Debug.Assert(m_kws.LoginStatus == KwsLoginStatus.LoggingIn);

            // This is the standard login reply.
            if (query.Res.Type == KAnpType.KANP_RES_KWS_CONNECT_KWS)
            {
                // Get the provided information.
                KwsConnectRes r = new KwsConnectRes(query.Res);
                Logging.Log(m_currentStep + " login step: " + r.ErrMsg);

                // Dispatch.
                if (r.Code == KAnpType.KANP_KWS_LOGIN_OK) HandleConnectKwsSuccess(r);
                else if (r.Code == KAnpType.KANP_KWS_LOGIN_BAD_PWD_OR_TICKET) HandleBadPwdOrTicket(r);
                else if (r.Code == KAnpType.KANP_KWS_LOGIN_OOS) HandleLoginFailure(KwsLoginResult.OOS, r.ErrMsg);
                else if (r.Code == KAnpType.KANP_KWS_LOGIN_BAD_KWS_ID) HandleLoginFailure(KwsLoginResult.BadKwsID, r.ErrMsg);
                else if (r.Code == KAnpType.KANP_KWS_LOGIN_BAD_EMAIL_ID) HandleLoginFailure(KwsLoginResult.BadEmailID, r.ErrMsg);
                else if (r.Code == KAnpType.KANP_KWS_LOGIN_DELETED_KWS) HandleLoginFailure(KwsLoginResult.DeletedKws, r.ErrMsg);
                else if (r.Code == KAnpType.KANP_KWS_LOGIN_ACCOUNT_LOCKED) HandleLoginFailure(KwsLoginResult.AccountLocked, r.ErrMsg);
                else HandleLoginFailure(KwsLoginResult.MiscKcdError, r.ErrMsg);
            }

            // This is an unexpected reply.
            else
            {
                HandleLoginFailure(KwsLoginResult.MiscKcdError, Misc.HandleUnexpectedKAnpReply("login", query.Res).Message);
            }
        }

        /// <summary>
        /// Called on successful login.
        /// </summary>
        private void HandleConnectKwsSuccess(KwsConnectRes r)
        {
            // Update our credentials and login information if needed.
            if (LoginResult != KwsLoginResult.Accepted ||
                m_kws.CoreData.Credentials.UserID != r.UserID ||
                m_kws.CoreData.Credentials.EmailID != r.EmailID ||
                m_kws.CoreData.Credentials.SecureFlag != r.SecureFlag ||
                m_kws.CoreData.Credentials.KwmoAddress != r.KwmoAddress)
            {
                LoginResult = KwsLoginResult.Accepted;
                LoginResultString = "login successful";
                m_kws.CoreData.Credentials.UserID = r.UserID;
                m_kws.CoreData.Credentials.EmailID = r.EmailID;
                m_kws.CoreData.Credentials.SecureFlag = r.SecureFlag;
                m_kws.CoreData.Credentials.KwmoAddress = r.KwmoAddress;
                m_kws.SetDirty();
            }

            // Remember the latest event ID available on the KAS.
            m_kws.KAnpState.LoginLatestEventId = r.LoginLatestEventID;

            // Tell the state machine.
            m_kws.Sm.HandleLoginSuccess();
        }

        /// <summary>
        /// Called when the login fails with KANP_KWS_LOGIN_BAD_PWD_OR_TICKET.
        /// </summary>
        private void HandleBadPwdOrTicket(KwsConnectRes r)
        {
            // Remember that the workspace is secure and if a password is available.
            m_kws.CoreData.Credentials.SecureFlag = r.SecureFlag;
            PwdOnKcdFlag = r.PwdOnKcdFlag;
            m_kws.SetDirty();

            // The cached step has failed.
            if (m_currentStep == KwsLoginStep.Cached)
            {
                // Clear the ticket refusal string.
                TicketRefusalString = "";
                m_kws.SetDirty();

                // Only the cached step was allowed. We're done.
                if (m_loginType == KwsLoginType.Cached)
                {
                    HandleLoginFailure(KwsLoginResult.BadSecurityCreds, "security credentials refused");
                    return;
                }

                // We can perform the ticket step.
                WmWinRegistry registry = WmWinRegistry.Spawn();
                if (registry.CanLoginOnKps())
                {
                    HandleTicketLoginStep(registry);
                    return;
                }
            }

            // The ticket step has failed.
            else if (m_currentStep == KwsLoginStep.Ticket)
            {
                // Set the ticket refusal string.
                TicketRefusalString = r.ErrMsg;
                m_kws.SetDirty();
            }

            // There is no password on the KCD.
            if (!PwdOnKcdFlag)
            {
                HandleLoginFailure(KwsLoginResult.BadSecurityCreds, "a password must be assigned to you");
                return;
            }

            // We're not allowed to prompt.
            if (m_loginType == KwsLoginType.NoPwdPrompt)
            {
                HandleLoginFailure(KwsLoginResult.PwdRequired, "password required");
                return;
            }

            // Prompt for the password.
            HandlePwdLoginStep(m_currentStep == KwsLoginStep.Pwd);
        }

        private void HandleDisconnectKwsReply(KasQuery ctx)
        {
            // We're now logged out. Ignore failures from the server,
            // this can happen legitimately if we send a logout command
            // before we get the result of the login command.
            m_kws.Sm.HandleNormalLogout();
        }
    }

    /// <summary>
    /// This request is posted to prompt for the user's password.
    /// </summary>
    public class LoginHandlerPwdPromptGer : GuiExecRequest
    {
        private KwsKasLoginHandler m_handler;
        private String m_kwsName;
        private String m_kwsPwd;

        /// <summary>
        /// Set to true if the last password was invalid.
        /// </summary>
        private bool m_failFlag;

        public LoginHandlerPwdPromptGer(KwsKasLoginHandler handler, String kwsName, String kwsPwd, bool failedFlag)
        {
            m_handler = handler;
            m_kwsName = kwsName;
            m_kwsPwd = kwsPwd;
            m_failFlag = failedFlag;
        }

        public override void Run()
        {
            DialogResult res = frmPwdPrompt.ShowPrompt(m_kwsName, m_failFlag, ref m_kwsPwd);
            if (CancelFlag) return;
            m_handler.HandlePwdPromptResult(this, res, m_kwsPwd);
        }
    }
}
