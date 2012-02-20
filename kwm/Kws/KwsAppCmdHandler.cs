using System;
using System.Collections.Generic;
using System.Text;
using Tbx.Utils;
using kwm.KwmAppControls;

namespace kwm
{
    public class OutlookStartScreenShareOp : KwsCoreOp
    {
        private OAnpType.OanpScreenShareFlags m_flags;
        private UInt32 m_hwnd;

        public OutlookStartScreenShareOp(WorkspaceManager wm, WmOutlookRequest request)
            : base(wm)
        {
            RegisterOutlookRequest(request);
        }

        bool ParseRequest(WmOutlookRequest request)
        {
            bool success = false;
            try
            {
                m_kws = Wm.GetKwsByInternalID(request.Cmd.Elements[0].UInt64);
                m_flags = (OAnpType.OanpScreenShareFlags)request.Cmd.Elements[1].UInt32;
                m_hwnd = request.Cmd.Elements[2].UInt32;
                success = true;
                RegisterToKws(true);
            }
            catch (Exception ex)
            {
                HandleMiscFailure(ex);
            }
            return success;
        }

        public override void HandleMiscFailure(Exception ex)
        {
            UnregisterFromKws(true);
            m_outlookRequest.SendFailure(ex.Message);
        }

        /// <summary>
        /// Start the operation.
        /// </summary>
        public void StartOp()
        {
            if (!ParseRequest(m_outlookRequest))
                return;
            if ((m_flags & OAnpType.OanpScreenShareFlags.Prompt) == 0)
            {
                //Screenshare
                KwsApp app = m_kws.GetApp(KAnpType.KANP_NS_VNC);
                app.ProcessCommand(m_outlookRequest.Cmd);
            }
            else
            {
                HandleMiscFailure(new Exception("Prompting is not supported yet"));
                return;
            }

            AnpMsg res = m_outlookRequest.MakeReply(OAnpType.OANP_RES_OK);
            m_outlookRequest.SendReply(res);

            // We're done.
            UnregisterFromKws(true);
            m_doneFlag = true;
        }
    }
}
