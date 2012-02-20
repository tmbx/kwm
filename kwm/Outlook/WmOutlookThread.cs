using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Windows.Forms;
using kwm.Utils;
using Microsoft.Win32;
using System.Security.Cryptography;
using Tbx.Utils;
using System.IO;
using System.Diagnostics;

namespace kwm
{
    /// <summary>
    /// Status of the session with Outlook.
    /// </summary>
    public enum OutlookSessionStatus
    {
        /// <summary>
        /// The Outlook thread is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The broker is waiting for the Outlook thread to stop.
        /// </summary>
        Stopping,

        /// <summary>
        /// The Outlook thread is listening for a connection from Outlook.
        /// </summary>
        Listening,

        /// <summary>
        /// The session is open.
        /// </summary>
        Open
    }

    /// <summary>
    /// Type of a message sent to the Outlook broker by the Outlook thread.
    /// </summary>
    public enum OutlookBrokerMsgType
    {
        /// <summary>
        /// Connected to Outlook.
        /// </summary>
        Connected,

        /// <summary>
        /// Disconnected from Outlook.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Received a message from Outlook.
        /// </summary>
        ReceivedMsg
    }

    /// <summary>
    /// This class represents a message sent to the Outlook thread by the Outlook
    /// broker.
    /// </summary>
    public class WmOutlookThreadMsg : WorkerThreadMsg
    {
        /// <summary>
        /// Reference to the Outlook worker thread.
        /// </summary>
        private WmOutlookThread m_thread;

        /// <summary>
        /// Session ID associated to this message.
        /// </summary>
        public UInt64 SessionID;

        /// <summary>
        /// ANP message to send.
        /// </summary>
        public AnpMsg Msg;

        public WmOutlookThreadMsg(WmOutlookThread thread, UInt64 sessionID, AnpMsg msg)
        {
            m_thread = thread;
            SessionID = sessionID;
            Msg = msg;
        }

        public override void Run()
        {
            m_thread.HandleBrokerMsg(this);
        }
    }

    /// <summary>
    /// This class represents a message sent to the Outlook broker by the Outlook 
    /// thread.
    /// </summary>
    public class OutlookUiThreadMsg : UiThreadMsg
    {
        /// <summary>
        /// Reference to the Outlook broker.
        /// </summary>
        private WmOutlookBroker m_broker;

        /// <summary>
        /// Message type.
        /// </summary>
        public OutlookBrokerMsgType Type;

        /// <summary>
        /// Associated session ID, if any.
        /// </summary>
        public UInt64 SessionID;

        /// <summary>
        /// Associated ANP message, if any.
        /// </summary>
        public AnpMsg Msg;

        /// <summary>
        /// Associated exception, if any.
        /// </summary>
        public Exception Ex;

        public OutlookUiThreadMsg(WmOutlookBroker broker, OutlookBrokerMsgType type)
        {
            m_broker = broker;
            Type = type;
        }

        public override void Run()
        {
            m_broker.HandleThreadMsg(this);
        }
    }

    /// <summary>
    /// The Outlook thread handles the low-level session with Outlook.
    /// </summary>
    public class WmOutlookThread : KwmWorkerThread
    {
        /// <summary>
        /// Reference to the Outlook broker.
        /// </summary>
        private WmOutlookBroker m_broker;

        /// <summary>
        /// True if we're ready to exchange messages with Outlook.
        /// </summary>
        private bool m_chattingFlag = false;

        /// <summary>
        /// Current session ID.
        /// </summary>
        private UInt64 m_sessionID = 0;

        /// <summary>
        /// Authentication data written in the information file.
        /// </summary>
        private byte[] m_secret = null;

        /// <summary>
        /// Listening socket.
        /// </summary>
        private Socket m_listenSock = null;

        /// <summary>
        /// Outlook socket.
        /// </summary>
        private Socket m_outlookSock = null;

        /// <summary>
        /// Queue of ANP messages to send to Outlook.
        /// </summary>
        private Queue<AnpMsg> m_msgQueue = new Queue<AnpMsg>();

        protected override void Run()
        {
            try
            {
                StartListening();

                while (true)
                {
                    ConnectToOutlook();
                    ChatWithOutlook();
                }
            }

            finally
            {
                CloseListenSock();
                CloseOutlookSock();
            }
        }

        protected override void OnCompletion()
        {
            if (Status == WorkerStatus.Success) FailException = new Exception("unexpected thread termination");
            m_broker.OnThreadCompletion(FailException);
        }

        public WmOutlookThread(WmOutlookBroker broker)
        {
            m_broker = broker;
        }

        /// <summary>
        /// Handle a message received from the Outlook broker.
        /// </summary>
        public void HandleBrokerMsg(WmOutlookThreadMsg msg)
        {
            if (!m_chattingFlag || msg.SessionID != m_sessionID) return;
            m_msgQueue.Enqueue(msg.Msg);
        }

        /// <summary>
        /// Start listening for Outlook connections.
        /// </summary>
        private void StartListening()
        {
            // Start to listen.
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            m_listenSock = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_listenSock.Bind(endPoint);
            m_listenSock.Listen(1);
            int port = ((IPEndPoint)m_listenSock.LocalEndPoint).Port;

            // Generate the authentication data.
            m_secret = new byte[16];
            (new RNGCryptoServiceProvider()).GetBytes(m_secret);

            // Write the information file.
            FileStream stream = null;

            try
            {
                // Format:
                // - port as string.
                // - secret as hexadecimal string.
                String destPath = OAnp.GetKppMsoKwmInfoPath();
                
                // Make sure the target directory exists.
                String dataPath = OAnp.GetKppMsoKwmDataPath();
                if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);

                String tmpPath = destPath + ".tmp";
                stream = File.Open(tmpPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                StreamWriter w = new StreamWriter(stream);
                w.WriteLine(port.ToString());
                w.WriteLine(Base.HexStr(m_secret));
                w.Close();

                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(tmpPath, destPath);
            }

            finally
            {
                if (stream != null) stream.Close();
            }
        }

        /// <summary>
        /// Block until the connection to Outlook is established.
        /// </summary>
        private void ConnectToOutlook()
        {
            Debug.Assert(!m_chattingFlag);

            Logging.Log("ConnectToOutlook() called.");
            while (!m_chattingFlag)
            {
                Logging.Log("In while loop.");
                // Close the Outlook socket if we have opened it inconclusively.
                CloseOutlookSock();

                try
                {
                    // Accept a connection.
                    SelectSockets select = new SelectSockets();
                    select.AddRead(m_listenSock);
                    Block(select);
                    if (!select.InRead(m_listenSock)) continue;
                    m_outlookSock = m_listenSock.Accept();
                    m_outlookSock.Blocking = false;

                    Logging.Log("About to read MSO auth data.");
                    // Read the authentication data. Timeout after 2 seconds.
                    DateTime startTime = DateTime.Now;
                    byte[] authSockData = new byte[m_secret.Length];
                    int nbRead = 0;
                    while (nbRead != m_secret.Length)
                    {
                        Logging.Log("In Read while loop");
                        select = new SelectSockets();
                        select.AddRead(m_outlookSock);
                        SetSelectTimeout(startTime, 2000, select, "no authentication data received");
                        Logging.Log("About to block");
                        Block(select);
                        Logging.Log("Out of block");
                        int r = Base.SockRead(m_outlookSock, authSockData, nbRead, m_secret.Length - nbRead);
                        if (r > 0) nbRead += r;
                        Logging.Log("End Read while loop");
                    }

                    if (!Base.ByteArrayEqual(m_secret, authSockData))
                        throw new Exception("invalid authentication data received");

                    Logging.Log("About to tell the broker we are connected.");
                    // Inform the broker that we're connected.
                    OutlookUiThreadMsg msg = new OutlookUiThreadMsg(m_broker, OutlookBrokerMsgType.Connected);
                    msg.SessionID = ++m_sessionID;
                    PostToUI(msg);

                    // We're now chatting.
                    m_chattingFlag = true;
                }

                catch (Exception ex)
                {
                    if (ex is WorkerCancellationException) throw;
                    Logging.Log(2, ex.Message);
                }
            }
        }

        /// <summary>
        /// Exchange messages with Outlook.
        /// </summary>
        private void ChatWithOutlook()
        {
            Debug.Assert(m_msgQueue.Count == 0);
            Debug.Assert(m_chattingFlag);

            try
            {
                AnpTransport transport = new AnpTransport(m_outlookSock);

                while (true)
                {
                    if (!transport.isReceiving) transport.beginRecv();
                    if (!transport.isSending && m_msgQueue.Count != 0) transport.sendMsg(m_msgQueue.Dequeue());
                    SelectSockets select = new SelectSockets();
                    select.AddRead(m_outlookSock);
                    if (transport.isSending) select.AddWrite(m_outlookSock);
                    Block(select);
                    transport.doXfer();

                    if (transport.doneReceiving)
                    {
                        OutlookUiThreadMsg msg = new OutlookUiThreadMsg(m_broker, OutlookBrokerMsgType.ReceivedMsg);
                        msg.Msg = transport.getRecv();
                        PostToUI(msg);
                    }
                }
            }

            catch (Exception ex)
            {
                m_chattingFlag = false;
                CloseOutlookSock();
                m_msgQueue.Clear();
                if (ex is WorkerCancellationException) throw;
                OutlookUiThreadMsg msg = new OutlookUiThreadMsg(m_broker, OutlookBrokerMsgType.Disconnected);
                msg.Ex = ex;
                PostToUI(msg);
            }
        }

        /// <summary>
        /// Helper method for ConnectToOutlook().
        /// </summary>
        private void SetSelectTimeout(DateTime startTime, int msec, SelectSockets select, String msg)
        {
            int remaining = msec - (int)(DateTime.Now - startTime).TotalMilliseconds;
            if (remaining < 0) throw new Exception(msg);
            select.Timeout = remaining * 1000;
        }

        /// <summary>
        /// Close the listening socket if it is open.
        /// </summary>
        private void CloseListenSock()
        {
            if (m_listenSock != null)
            {
                m_listenSock.Close();
                m_listenSock = null;
            }
        }

        /// <summary>
        /// Close the Outlook socket if it is open.
        /// </summary>
        private void CloseOutlookSock()
        {
            Logging.Log("CloseOutlookSock");
            if (m_outlookSock != null)
            {
                m_outlookSock.Close();
                m_outlookSock = null;
            }
        }
    }
}