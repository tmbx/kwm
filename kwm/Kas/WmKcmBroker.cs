using System.Collections.Generic;
using System;
using kwm.Utils;
using System.Diagnostics;
using kwm.KwmAppControls;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// This class represents an ANP message delivered to/from a KAS
    /// by the KAS communication manager.
    /// </summary>
    public class KcmAnpMsg
    {
        /// <summary>
        /// The ANP message being delivered.
        /// </summary>
        public AnpMsg Msg;

        /// <summary>
        /// Associated KAS.
        /// </summary>
        public KasIdentifier KasID;

        public KcmAnpMsg(AnpMsg msg, KasIdentifier kasID)
        {
            Msg = msg;
            KasID = kasID;
        }

        /// <summary>
        /// Return true if the message is an ANP reply.
        /// </summary>
        public bool IsReply()
        {
            return ((Msg.Type & KAnpType.ROLE_MASK) == KAnpType.KANP_RES);
        }

        /// <summary>
        /// Return true if the message is an ANP event.
        /// </summary>
        public bool IsEvent()
        {
            return ((Msg.Type & KAnpType.ROLE_MASK) == KAnpType.KANP_EVT);
        }
    }

    /// <summary>
    /// This class represents a control message exchanged between the WM and
    /// the KCM.
    /// </summary>
    public class KcmControlMsg { }

    /// <summary>
    /// This class represents a KAS connect/disconnect request.
    /// </summary>
    public class KcmConnectionRequest : KcmControlMsg
    {
        /// <summary>
        /// ID of the KAS to connect/disconnect.
        /// </summary>
        public KasIdentifier KasID;

        /// <summary>
        /// True if connection is required.
        /// </summary>
        public bool ConnectFlag;

        public KcmConnectionRequest(KasIdentifier kasID, bool connectFlag)
        {
            KasID = kasID;
            ConnectFlag = connectFlag;
        }
    }

    /// <summary>
    /// This class represents a KAS connection notice. Used when
    /// a KAS is now in the connected state.
    /// </summary>
    public class KcmConnectionNotice : KcmControlMsg
    {
        /// <summary>
        /// ID of the KAS that is now connected.
        /// </summary>
        public KasIdentifier KasID;

        /// <summary>
        /// Minor version of the protocol spoken with the KAS.
        /// </summary>
        public UInt32 MinorVersion;

        public KcmConnectionNotice(KasIdentifier kasID, UInt32 minorVersion)
        {
            KasID = kasID;
            MinorVersion = minorVersion;
        }
    }

    /// <summary>
    /// This class represents a KAS disconnection notice. Used when
    /// a KAS is now in the disconnected state.
    /// </summary>
    public class KcmDisconnectionNotice : KcmControlMsg
    {
        /// <summary>
        /// ID of the KAS that is now disconnected.
        /// </summary>
        public KasIdentifier KasID;

        /// <summary>
        /// If the disconnection was caused by an error, this
        /// is the exception describing the error.
        /// </summary>
        public Exception Ex;

        public KcmDisconnectionNotice(KasIdentifier kasID, Exception ex)
        {
            KasID = kasID;
            Ex = ex;
        }
    }

    /// <summary>
    /// This message is posted to the UI thread by the broker to wake-up
    /// the WM.
    /// </summary>
    public class WkbWmWakeUpMsg : UiThreadMsg
    {
        private WmKcmBroker Broker;

        public WkbWmWakeUpMsg(WmKcmBroker broker)
        {
            Broker = broker;
        }

        public override void Run()
        {
            Broker.HandleWmWakeUp(this);
        }
    }

    /// <summary>
    /// This message is posted to the KCM thread by the broker to wake-up
    /// the KCM.
    /// </summary>
    public class WkbKcmWakeUpMsg : WorkerThreadMsg
    {
        private WmKcmBroker Broker;

        public WkbKcmWakeUpMsg(WmKcmBroker broker)
        {
            Broker = broker;
        }

        public override void Run()
        {
            Broker.HandleKcmWakeUp(this);
        }
    }

    /// <summary>
    /// Delegate type called from the OnCompletion() handler of the KCM
    /// thread.
    /// </summary>
    public delegate void KcmCompletionDelegate(bool successFlag, Exception ex);

    /// <summary>
    /// This class manages the interactions between the KAS communication
    /// manager and the workspace manager. It encapsulates most synchronization
    /// and flow control issues.
    /// </summary>
    public class WmKcmBroker
    {
        /// <summary>
        /// Quench if that many messages are lingering in the WM ANP message
        /// queue.
        /// </summary>
        private const UInt32 m_quenchQueueMaxSize = 50;

        /// <summary>
        /// Number of messages to post to the WM between each quench check.
        /// </summary>
        private const UInt32 m_quenchBatchCount = 100;

        /// <summary>
        /// Rate at which messages will be processed, e.g. 1 message per 2
        /// milliseconds.
        /// </summary>
        private const UInt32 m_quenchProcessRate = 5;

        /// <summary>
        /// Reference to the workspace manager.
        /// </summary>
        private WorkspaceManager m_wm = null;

        /// <summary>
        /// Reference to the KAS communication manager.
        /// </summary>
        private KasCommunicationManager m_kcm = null;

        /// <summary>
        /// Mutex protecting the variables that follow.
        /// </summary>
        private Object m_mutex = new Object();

        /// <summary>
        /// Message posted to wake-up the WM.
        /// </summary>
        private WkbWmWakeUpMsg m_wmWakeUpMsg = null;

        /// <summary>
        /// Message posted to wake-up the KCM.
        /// </summary>
        private WkbKcmWakeUpMsg m_kcmWakeUpMsg = null;

        /// <summary>
        /// Array of control messages posted to the WM.
        /// </summary>
        private List<KcmControlMsg> m_ToWmControlMsgArray = new List<KcmControlMsg>();

        /// <summary>
        /// Array of control messages posted to the KCM.
        /// </summary>
        private List<KcmControlMsg> m_ToKcmControlMsgArray = new List<KcmControlMsg>();

        /// <summary>
        /// Array of ANP message posted to the WM.
        /// </summary>
        private List<KcmAnpMsg> m_ToWmAnpMsgArray = new List<KcmAnpMsg>();

        /// <summary>
        /// Array of ANP message posted to the KCM.
        /// </summary>
        private List<KcmAnpMsg> m_ToKcmAnpMsgArray = new List<KcmAnpMsg>();

        /// <summary>
        /// Number of messages that have been processed in the current batch.
        /// </summary>
        private UInt32 m_currentBatchCount = 0;

        /// <summary>
        /// Date at which the current batch has been started.
        /// </summary>
        private DateTime m_currentBatchStartDate = DateTime.MinValue;

        /// <summary>
        /// Substitute for the constructor to allow KCM to be created
        /// appropriately by the WM.
        /// </summary>
        public void Initialize(WorkspaceManager wm, KasCommunicationManager kcm)
        {
            m_wm = wm;
            m_kcm = kcm;
        }

        /// <summary>
        /// Notify the WM that something occurred. Assume mutex is locked.
        /// </summary>
        private void NotifyWm()
        {
            if (m_wm != null && m_wmWakeUpMsg == null)
            {
                m_wmWakeUpMsg = new WkbWmWakeUpMsg(this);
                m_wm.UiBroker.PostToUi(m_wmWakeUpMsg);
            }
        }

        /// <summary>
        /// Notify the KCM that something occurred. Assume mutex is locked.
        /// </summary>
        private void NotifyKcm()
        {
            if (m_kcm != null && m_kcmWakeUpMsg == null)
            {
                m_kcmWakeUpMsg = new WkbKcmWakeUpMsg(this);
                m_kcm.PostToWorker(m_kcmWakeUpMsg);
            }
        }

        /// <summary>
        /// Recompute the quench deadline returned to the KCM.
        /// Assume mutex is locked.
        /// </summary>
        private DateTime RecomputeQuenchDeadline()
        {
            // Too many unprocessed messages.
            if (m_ToWmAnpMsgArray.Count >= m_quenchQueueMaxSize) return DateTime.MaxValue;

            // Batch check count not yet reached.
            if (m_currentBatchCount < m_quenchBatchCount) return DateTime.MinValue;

            // Compute deadline.
            DateTime deadline = m_currentBatchStartDate.AddMilliseconds(m_currentBatchCount * m_quenchProcessRate);
            DateTime now = DateTime.Now;

            // Enough time has passed during the processsing of the batch.
            // Reset the batch statistics.
            if (deadline < now)
            {
                m_currentBatchCount = 0;
                m_currentBatchStartDate = now;
                return DateTime.MinValue;
            }

            // Not enough time has passed to process the batch. Return the
            // deadline.
            return deadline;
        }


        ////////////////////////////////////////////
        // Interface methods for internal events. //
        ////////////////////////////////////////////

        /// <summary>
        /// Internal handler for WkbWmWakeUpMsg.
        /// </summary>
        public void HandleWmWakeUp(WkbWmWakeUpMsg msg)
        {
            lock (m_mutex)
            {
                // Clear the posted message reference.
                Debug.Assert(m_wmWakeUpMsg == msg);
                m_wmWakeUpMsg = null;
            }

            // Notify the WM state machine that we have something for it.
            m_wm.Sm.HandleWmKcmNotification();
        }

        /// <summary>
        /// Internal handler for WkbKcmWakeUpMsg.
        /// </summary>
        public void HandleKcmWakeUp(WkbKcmWakeUpMsg msg)
        {
            lock (m_mutex)
            {
                // Clear the posted message reference.
                Debug.Assert(m_kcmWakeUpMsg == msg);
                m_kcmWakeUpMsg = null;
            }

            // Notify the KCM that we have something for it.
            m_kcm.HandleWmKcmNotification();
        }


        ///////////////////////////////////
        // Interface methods for the WM. //
        ///////////////////////////////////

        /// <summary>
        /// Request a KAS to be connected.
        /// </summary>
        public void RequestKasConnect(KasIdentifier kasID)
        {
            lock (m_mutex)
            {
                // The following sequence of events can happen:
                // - KCM posts disconnection event.
                // - WM posts ANP message.
                // - WM receives disconnection event.
                // - WM posts connection request.
                // - KCM receives connection request and ANP message concurrently,
                //   possibly posting the ANP message incorrectly.
                // To prevent this situation, we ensure that we have no lingering
                // ANP message left for that KAS.
                List<KcmAnpMsg> newList = new List<KcmAnpMsg>();

                foreach (KcmAnpMsg m in m_ToKcmAnpMsgArray)
                {
                    if (m.KasID != kasID) newList.Add(m);
                }

                m_ToKcmAnpMsgArray = newList;

                m_ToKcmControlMsgArray.Add(new KcmConnectionRequest(kasID, true));
                NotifyKcm();
            }
        }

        /// <summary>
        /// Request a KAS to be disconnected.
        /// </summary>
        public void RequestKasDisconnect(KasIdentifier kasID)
        {
            lock (m_mutex)
            {
                m_ToKcmControlMsgArray.Add(new KcmConnectionRequest(kasID, false));
                NotifyKcm();
            }
        }

        /// <summary>
        /// Send an ANP message to a KAS.
        /// </summary>
        public void SendAnpMsgToKcm(KcmAnpMsg m)
        {
            lock (m_mutex)
            {
                m_ToKcmAnpMsgArray.Add(m);
                NotifyKcm();
            }
        }

        /// <summary>
        /// Return the messages posted by the KCM.
        /// </summary>
        public void GetMessagesForWm(out List<KcmControlMsg> controlArray, out List<KcmAnpMsg> anpArray)
        {
            lock (m_mutex)
            {
                // Notify KCM if it was potentially quenched.
                if (m_ToWmAnpMsgArray.Count >= m_quenchQueueMaxSize) NotifyKcm();

                controlArray = m_ToWmControlMsgArray;
                m_ToWmControlMsgArray = new List<KcmControlMsg>();
                anpArray = m_ToWmAnpMsgArray;
                m_ToWmAnpMsgArray = new List<KcmAnpMsg>();
            }
        }


        ////////////////////////////////////
        // Interface methods for the KCM. //
        ////////////////////////////////////

        /// <summary>
        /// Return the messages posted by the KCM and the current quench
        /// deadline.
        /// </summary>
        public void GetMessagesForKcm(out List<KcmControlMsg> controlArray, out List<KcmAnpMsg> anpArray,
                                      out DateTime quenchDeadline)
        {
            lock (m_mutex)
            {
                controlArray = m_ToKcmControlMsgArray;
                m_ToKcmControlMsgArray = new List<KcmControlMsg>();
                anpArray = m_ToKcmAnpMsgArray;
                m_ToKcmAnpMsgArray = new List<KcmAnpMsg>();
                quenchDeadline = RecomputeQuenchDeadline();
            }
        }

        /// <summary>
        /// Send control and ANP messages to the WM, return current quench
        /// deadline.
        /// </summary>
        public void SendMessagesToWm(List<KcmControlMsg> controlArray, List<KcmAnpMsg> anpArray,
                                     out DateTime quenchDeadline)
        {
            lock (m_mutex)
            {
                m_ToWmControlMsgArray.AddRange(controlArray);
                m_ToWmAnpMsgArray.AddRange(anpArray);
                m_currentBatchCount += (UInt32)anpArray.Count;
                quenchDeadline = RecomputeQuenchDeadline();
                NotifyWm();
            }
        }
    }
}