using System.Collections.Generic;
using System;
using Iesi.Collections.Generic;
using System.Diagnostics;
using kwm.KwmAppControls;
using System.Runtime.Serialization;
using System.Xml;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    /// <summary>
    /// Status of the connection with the KAS.
    /// </summary>
    public enum KasConnStatus
    {
        /// <summary>
        /// The workspace is not connected.
        /// </summary>
        Disconnected,

        /// <summary>
        /// A request has been sent to disconnect the workspace.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// The workspace is connected.
        /// </summary>
        Connected,

        /// <summary>
        /// A request has been sent to connect the workspace.
        /// </summary>
        Connecting
    }

    /// <summary>
    /// This class provides an identifier for a KAS server. The identifier
    /// consists of the host name and the port of the KAS server. Since this
    /// object is shared between threads without locking, it is immutable.
    /// </summary>
    [Serializable]
    public class KasIdentifier : IComparable
    {
        private String m_host;
        private UInt16 m_port;

        public String Host { get { return m_host; } }
        public UInt16 Port { get { return m_port; } }

        public KasIdentifier(String host, UInt16 port)
        {
            m_host = host;
            m_port = port;
        }

        public int CompareTo(Object obj)
        {
            KasIdentifier kas = (KasIdentifier)obj;

            int r = kas.Host.CompareTo(Host);
            if (r != 0) return r;

            return kas.Port.CompareTo(Port);
        }

        /// <summary>
        /// Export the object in an Xml format to the specified XmlDocument.
        /// </summary>
        public void ExportToXml(XmlDocument doc, XmlElement parent)
        {
            XmlElement kasID = doc.CreateElement("KasID");
            kasID.SetAttribute("version", "1");
            parent.AppendChild(kasID);
            Misc.CreateXmlElement(doc, kasID, "Host", m_host);
            Misc.CreateXmlElement(doc, kasID, "Port", m_port.ToString());
        }

        /// <summary>
        /// Convert the Xml representation of a KasIdentifier object to its
        /// KasIdentifier object equivalent.
        /// </summary>
        public static KasIdentifier FromXml(XmlElement elem)
        {
            UInt32 version = UInt32.Parse(elem.GetAttribute("version"));
            if (version != 1) throw new Exception("Unsupported KasIdentifier version '" + version + "'.");
            String host = Misc.GetXmlChildValue(elem, "Host", "");
            UInt16 port = UInt16.Parse(Misc.GetXmlChildValue(elem, "Port", "0"));
            KasIdentifier kasID = new KasIdentifier(host, port);
            return kasID;
        }
    }

    /// <summary>
    /// This class binds a KasQuery to a KAS and a workspace, if required.
    /// </summary>
    public class WmKasQuery : KasQuery
    {
        /// <summary>
        /// KAS associated to the query.
        /// </summary>
        public WmKas Kas;

        /// <summary>
        /// Workspace associated to the query, if any.
        /// </summary>
        public Workspace Kws;

        /// <summary>
        /// Application associated to the query, if any.
        /// </summary>
        public KwsApp App;

        /// <summary>
        /// If the context is bound to a workspace and this flag is true,
        /// the context will be cleared if the workspace is logged out.
        /// </summary>
        public bool ClearOnLogoutFlag;

        public WmKasQuery(AnpMsg cmd, Object[] metaData, KasQueryDelegate callback,
                          WmKas kas, Workspace kws, KwsApp app, bool clearOnLogoutFlag)
            : base(cmd, metaData, callback)
        {
            Kas = kas;
            Kws = kws;
            App = app;
            ClearOnLogoutFlag = clearOnLogoutFlag;
        }

        public override void Cancel()
        {
            RemoveFromQueryMap();
        }

        /// <summary>
        /// Remove the query from the KAS query map, if needed.
        /// </summary>
        public void RemoveFromQueryMap()
        {
            if (Kas.QueryMap.ContainsKey(MsgID)) Kas.QueryMap.Remove(MsgID);
        }
    }

    /// <summary>
    /// This class represents a KAS used by the workspace manager.
    /// </summary>
    [Serializable]
    public class WmKas : ISerializable
    {
        /// <summary>
        /// Number of seconds that must elapse before trying to reconnect a KAS
        /// that disconnected due to an error.
        /// </summary>
        private const UInt32 ReconnectDelay = 60;

        /// <summary>
        /// ReconnectDelay scaling factor in the exponential backoff algorithm.
        /// </summary>
        private const UInt32 BackoffFactor = 4;

        /// <summary>
        /// Backoff limit to the exponential backoff algorithm.
        /// </summary>
        private const UInt32 MaxNbBackoff = 5;

        /// <summary>
        /// Identifier of the KAS.
        /// </summary>
        public KasIdentifier KasID;

        /// <summary>
        /// Tree of workspaces using this KAS indexed by internal
        /// workspace ID.
        /// </summary>
        [NonSerialized]
        public SortedDictionary<UInt64, Workspace> KwsTree;

        /// <summary>
        /// Tree of workspaces that want to be connected indexed
        /// by internal workspace ID.
        /// </summary>
        [NonSerialized]
        public SortedDictionary<UInt64, Workspace> KwsConnectTree;

        /// <summary>
        /// Tree mapping message IDs to KAS queries.
        /// </summary>
        [NonSerialized]
        public SortedDictionary<UInt64, WmKasQuery> QueryMap;

        /// <summary>
        /// Connection status.
        /// </summary>
        [NonSerialized]
        public KasConnStatus ConnStatus;

        /// <summary>
        /// If the KAS was disconnected because an error occurred, this
        /// is the exception corresponding to the error.
        /// </summary>
        [NonSerialized]
        public Exception ErrorEx;

        /// <summary>
        /// Date at which the error occurred.
        /// </summary>
        [NonSerialized]
        public DateTime ErrorDate;

        /// <summary>
        /// Number of consecutive connection attempts that failed. This is
        /// used for exponential backoff.
        /// </summary>
        [NonSerialized]
        public UInt32 FailedConnectCount;

        /// <summary>
        /// Minor version of the protocol spoken with the KAS.
        /// </summary>
        [NonSerialized]
        public UInt32 MinorVersion;

        /// <summary>
        /// Non-deserializing constructor.
        /// </summary>
        public WmKas(KasIdentifier kasID)
        {
            KasID = kasID;
            Initialize();
        }

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        public WmKas(SerializationInfo info, StreamingContext context)
        {
            Initialize();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
        
        /// <summary>
        /// Initialization code common to both the deserialized and
        /// non-deserialized cases.
        /// </summary>
        public void Initialize()
        {
            KwsTree = new SortedDictionary<UInt64, Workspace>();
            KwsConnectTree = new SortedDictionary<UInt64, Workspace>();
            QueryMap = new SortedDictionary<UInt64, WmKasQuery>();
            ConnStatus = KasConnStatus.Disconnected;
            ErrorEx = null;
            ErrorDate = DateTime.MinValue;
            FailedConnectCount = 0;
            MinorVersion = 0;
        }

        /// <summary>
        /// Return the workspace having the external ID specified, if any.
        /// </summary>
        public Workspace GetWorkspaceByExternalID(UInt64 externalKwsId)
        {
            // This code could eventually be optimized if there are too many
            // workspaces.
            foreach (Workspace kws in KwsTree.Values)
                if (kws.CoreData.Credentials.ExternalID == externalKwsId) return kws;
            return null;
        }

        /// <summary>
        /// Cancel the KAS queries related to the workspace specified. If 
        /// logoutFlag is true, only the queries that have the 
        /// ClearOnLogoutFlag set are removed.
        /// </summary>
        public void CancelKwsKasQuery(Workspace kws, bool logoutFlag)
        {
            List<WmKasQuery> list = new List<WmKasQuery>();
            foreach (WmKasQuery query in QueryMap.Values)
                if (query.Kws == kws && (!logoutFlag || query.ClearOnLogoutFlag)) list.Add(query);
            foreach (WmKasQuery query in list) query.Cancel();
        }

        /// <summary>
        /// Clear the current error status of the KAS, if any.
        /// </summary>
        public void ClearError(bool clearFailedConnectFlag)
        {
            ErrorEx = null;
            ErrorDate = DateTime.MinValue;
            if (clearFailedConnectFlag) FailedConnectCount = 0;
        }

        /// <summary>
        /// Set the current error status and increase the number of failed
        /// connection attempts if requested.
        /// </summary>
        public void SetError(Exception ex, DateTime date, bool connectFailureFlag)
        {
            ErrorEx = ex;
            ErrorDate = date;
            if (connectFailureFlag) FailedConnectCount++;
        }

        /// <summary>
        /// Return the reconnection deadline, which is based on the current 
        /// error state and the failed connection count.
        /// </summary>
        public DateTime GetReconnectDeadline()
        {
            if (ErrorEx == null) return DateTime.MinValue;

            // If a troublesome KAS constantly fails, the failed connection
            // count will remain 0. In that case, we consider the failed 
            // connection count to be 1. By design the number of backoffs is 
            // one less than the failed connection count.
            UInt32 nbBackoff = FailedConnectCount;
            if (nbBackoff > 0) nbBackoff--;
            nbBackoff = Math.Min(nbBackoff, MaxNbBackoff);
            return ErrorDate.AddSeconds(ReconnectDelay * Math.Pow(BackoffFactor, nbBackoff)); 
        }
    }
}
