using System;
using System.Collections.Generic;
using System.Text;
using Wizard.UI;
using kwm.KwmAppControls;
using System.Diagnostics;
using Tbx.Utils;

/* There are several important operations that may involve a workspace: create,
 * join, invite, etc. Although many of these operations are similar, it is
 * not possible to rely only inheritance to avoid code duplication, since some
 * code is shared between several inheritance branches. Therefore, we rely on
 * both inheritance and composition to factor out the code.
 * 
 * The consequence is that the code is quite convoluted but also very compact. 
 */

namespace kwm
{
    /// <summary>
    /// Result of the workspace core operation.
    /// </summary>
    public enum KwmCoreKwsOpRes
    {
        /// <summary>
        /// Not initialized.
        /// </summary>
        None,

        /// <summary>
        /// The operation failed because the user's KPS configuration 
        /// in the registry is incomplete.
        /// </summary>
        InvalidCfg,

        /// <summary>
        /// The user has no KCD or does not have the power to create a 
        /// workspace.
        /// </summary>
        NoPower,

        /// <summary>
        /// A miscellaneous error has occurred.
        /// </summary>
        MiscError,

        /// <summary>
        /// The operation has completed successfully.
        /// </summary>
        Success
    }

    /// <summary>
    /// UI step completed in the core workspace operation.
    /// </summary>
    public enum KwmCoreKwsUiStep
    {
        /// <summary>
        /// No step completed.
        /// </summary>
        None,

        /// <summary>
        /// We got the global workspace information.
        /// </summary>
        Create,

        /// <summary>
        /// We got the address of the users to invite.
        /// </summary>
        Invite,

        /// <summary>
        /// We got the password of the users.
        /// </summary>
        Pwd
    }

    /// <summary>
    /// Logical step to perform in the core workspace operation.
    /// </summary>
    public enum KwmCoreKwsOpStep
    {
        /// <summary>
        /// Uninitialized.
        /// </summary>
        None,

        /// <summary>
        /// Create and show the wizard.
        /// </summary>
        WizardEntry,

        /// <summary>
        /// Perform the initial get ticket ticket query to obtain the user's
        /// powers.
        /// </summary>
        InitialPowerQuery,

        /// <summary>
        /// Perform one of the UI step above. Be aware that the wizard may
        /// choose to return to a step already completed.
        /// </summary>
        UIStep,

        /// <summary>
        /// Lookup the recipients.
        /// </summary>
        LookupRec,

        /// <summary>
        /// Create the workspace object.
        /// </summary>
        CreateKwsObject,

        /// <summary>
        /// Connect to the KAS and get the login ticket.
        /// </summary>
        KasConnectAndGetTicket,

        /// <summary>
        /// Create the workspace on the KAS.
        /// </summary>
        KasCreate,

        /// <summary>
        /// Login on the KAS.
        /// </summary>
        KasLogin,

        /// <summary>
        /// Perform the invitation on the KAS.
        /// </summary>
        KasInvite,

        /// <summary>
        /// Get a SKURL from the KAS. 
        /// </summary>
        KasSkurl,

        /// <summary>
        /// Set a password to a workspace user.
        /// </summary>
        SetUserPwd,

        /// <summary>
        /// Final step of the operation. Show the wizard if required.
        /// </summary>
        End
    }

    /// <summary>
    /// Core operation (e.g. invite users) being processed in a workspace.
    /// </summary>
    public class KwsCoreOp
    {
        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        public WorkspaceManager Wm = null;

        /// <summary>
        /// Result of the operation.
        /// </summary>
        public KwmCoreKwsOpRes OpRes = KwmCoreKwsOpRes.None;

        /// <summary>
        /// String describing the miscellaneous error, if any.
        /// </summary>
        public String ErrorString = "";

        /// <summary>
        /// Workspace associated to the core operation, if any.
        /// </summary>
        public Workspace m_kws = null;

        /// <summary>
        /// Outlook request associated to the operation, if any.
        /// </summary>
        public WmOutlookRequest m_outlookRequest = null;

        /// <summary>
        /// KMOD query associated to the operation, if any.
        /// </summary>
        public KmodQuery m_kmodQuery = null;

        /// <summary>
        /// Wizard associated to the current operation, if any.
        /// </summary>
        public WizardSheet m_wizard = null;

        /// <summary>
        /// UI step completed in the operation.
        /// </summary>
        public KwmCoreKwsUiStep m_uiStep = KwmCoreKwsUiStep.None;

        /// <summary>
        /// Current logical step to perform in the operation.
        /// </summary>
        public KwmCoreKwsOpStep m_opStep = KwmCoreKwsOpStep.None;

        /// <summary>
        /// True if the core operation has completed one way or another.
        /// </summary>
        public bool m_doneFlag = false;

        /// <summary>
        /// True if the wizard has been reentered.
        /// </summary>
        public bool wizEntryFlag = false;

        public KwsCoreOp(WorkspaceManager wm)
        {
            Wm = wm;
        }

        /// <summary>
        /// Set the outlook request reference and listen for its failure.
        /// </summary>
        public virtual void RegisterOutlookRequest(WmOutlookRequest request)
        {
            m_outlookRequest = request;
            m_outlookRequest.OnOutlookFailure += HandleMiscFailure;
        }

        /// <summary>
        /// Check if it is possible to register to the workspace. If not, this
        /// method returns false and calls HandleMiscFailure().
        /// </summary>
        public virtual bool CheckRegisterToKws()
        {
            if (m_kws == null || m_kws.CoreOp != null)
            {
                HandleMiscFailure(new Exception("another operation is already in progress, please try again later"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start listening to the events fired by the workspace state machine.
        /// Set the flag to true to assign the operation to the workspace.
        /// </summary>
        public virtual void RegisterToKws(bool setCoreOpFlag)
        {
            Debug.Assert(m_kws != null);
            Debug.Assert(!setCoreOpFlag || m_kws.CoreOp == null);
            if (setCoreOpFlag) m_kws.CoreOp = this;
            m_kws.OnKwsSmNotif += HandleImmediateNotification;
            m_kws.OnKwsStatusChanged += HandleDeferredNotification;
        }

        /// <summary>
        /// Stop listening to the events fired by the workspace state machine.
        /// The workspace reference is not cleared. Do not set the flag if unregistering
        /// a management operation such as KwsSetPwd.
        /// </summary>
        public virtual void UnregisterFromKws(bool unsetCoreOpFlag)
        {
            if (m_kws == null) return;
            Debug.Assert(!unsetCoreOpFlag || m_kws.CoreOp == this);
            if (unsetCoreOpFlag) m_kws.CoreOp = null;

            m_kws.OnKwsSmNotif -= HandleImmediateNotification;
            m_kws.OnKwsStatusChanged -= HandleDeferredNotification;
        }

        /// <summary>
        /// Unregister from the workspace, delete it and clear its reference.
        /// </summary>
        public virtual void UnregisterAndDeleteKws()
        {
            if (m_kws != null)
            {
                UnregisterFromKws(true);
                m_kws.Sm.RequestTaskSwitch(KwsTask.Delete);
                ClearKws();
            }
        }

        /// <summary>
        /// Clear the workspace reference, if any.
        /// </summary>
        public virtual void ClearKws()
        {
            if (m_kws != null) m_kws = null;
        }

        /// <summary>
        /// Handle an immediate notification received from the workspace state 
        /// machine.
        /// </summary>
        public virtual void HandleImmediateNotification(Object sender, KwsSmNotifEventArgs evt)
        {
            KwsSmNotif type = evt.Type;
            if (type == KwsSmNotif.Connected) HandleKasConnected();
            else if (type == KwsSmNotif.Disconnecting) HandleKasDisconnecting(evt.Ex);
            else if (type == KwsSmNotif.Login) HandleKwsLoginSuccess();
            else if (type == KwsSmNotif.Logout) HandleKwsLogOut(evt.Ex);
            else if (type == KwsSmNotif.TaskSwitch) HandleTaskSwitch(evt.Task);
            else if (type == KwsSmNotif.AppFailure) HandleAppFailure(evt.Ex);
  
        }

        /// <summary>
        /// Handle a deferred notification received from the workspace state 
        /// machine.
        /// </summary>
        public virtual void HandleDeferredNotification(Object sender, EventArgs evt)
        {
        }

        /// <summary>
        /// Called when the KAS has connected.
        /// </summary>
        public virtual void HandleKasConnected()
        {
        }

        /// <summary>
        /// Called when the KAS is disconnecting or has disconnected. 'ex' is
        /// non-null if the connection was lost because an error occurred.
        /// </summary>
        public virtual void HandleKasDisconnecting(Exception ex)
        {
            if (ex != null) HandleMiscFailure(ex);
        }

        /// <summary>
        /// Called when the workspace becomes logged in.
        /// </summary>
        public virtual void HandleKwsLoginSuccess()
        {
        }

        /// <summary>
        /// Called when the workspace has logged out normally or when the login
        /// fails. 'ex' is non-null if the login has failed.
        /// </summary>
        public virtual void HandleKwsLogOut(Exception ex)
        {
            if (ex != null) HandleMiscFailure(ex);
        }

        /// <summary>
        /// Called when the current workspace task is switching.
        /// </summary>
        public virtual void HandleTaskSwitch(KwsTask task)
        {
            // Cancel the operation if we switch to an unexpected task.
            if (task != KwsTask.Spawn && task != KwsTask.WorkOnline)
                HandleMiscFailure(new Exception("interrupted operation"));
        }

        /// <summary>
        /// Called when an application error has occurred.
        /// </summary>
        public virtual void HandleAppFailure(Exception ex)
        {
            HandleMiscFailure(ex);
        }

        /// <summary>
        /// Cancel when the connection with Outlook is lost.
        /// </summary>
        public virtual void HandleOutlookFailure(Exception ex)
        {
            HandleMiscFailure(ex);
        }

        /// <summary>
        /// Called when a serious error has been detected.
        /// </summary>
        public virtual void HandleMiscFailure(Exception ex)
        {
        }

        /// <summary>
        /// Cancel and clear the current KMOD query, if any.
        /// </summary>
        public virtual void ClearKmodQuery()
        {
            if (m_kmodQuery != null)
            {
                m_kmodQuery.Cancel();
                m_kmodQuery = null;
            }
        }

        /// <summary>
        /// Close and clear the wizard, if any.
        /// </summary>
        public virtual void ClearWizard()
        {
            if (m_wizard != null)
            {
                m_wizard.Close();
                m_wizard = null;
            }
        }

        /// <summary>
        /// Return false if the operation was cancelled. Assert that the
        /// specified step matches the current operation step.
        /// </summary>
        public bool CheckCtx(KwmCoreKwsOpStep step)
        {
            if (m_doneFlag) return false;
            Debug.Assert(m_opStep == step);
            return true;
        }
    }
}

