using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using kwm.Utils;
using System.Collections;

// This file contains stubs for old data types that are not used anymore, but
// that are required for deserializing old versions.

// ************************
// Classes removed in v3
// ************************
namespace kwm.KwmAppControls
{
    [Serializable]
    public class AppAppSharing : ISerializable
    {
        public AppAppSharing(SerializationInfo info, StreamingContext context) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
    namespace AppFTP
    {
        [Serializable]
        public class AppFTP : ISerializable
        {
            public AppFTP(SerializationInfo info, StreamingContext context) { }

            public void GetObjectData(SerializationInfo info, StreamingContext context) { }
        }

        [Serializable]
        public class AppFTPControl : ISerializable
        {
            public AppFTPControl(SerializationInfo info, StreamingContext context) { }

            public void GetObjectData(SerializationInfo info, StreamingContext context) { }

            [Serializable]
            public class AppFtpControlSettings : ISerializable
            {
                public AppFtpControlSettings(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }
        }

        namespace ShareFileSytem
        {

            [Serializable]
            public class ShareFileSystem : ISerializable
            {
                public ShareFileSystem(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }

            [Serializable]
            public class RootItem : ISerializable
            {
                public RootItem(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }

            [Serializable]
            public class FileItem : ISerializable
            {
                public FileItem(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }

            [Serializable]
            public class DirectoryItem : ISerializable
            {
                public DirectoryItem(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }

            [Serializable]
            public class ServerFtpFile : ISerializable
            {
                public ServerFtpFile(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }

            [Serializable]
            public class ShareFileSystemUtility : ISerializable
            {
                public ShareFileSystemUtility(SerializationInfo info, StreamingContext context) { }

                public void GetObjectData(SerializationInfo info, StreamingContext context) { }

                [Serializable]
                public class FileItemModifiedStatus : ISerializable
                {
                    public FileItemModifiedStatus(SerializationInfo info, StreamingContext context) { }

                    public void GetObjectData(SerializationInfo info, StreamingContext context) { }
                }
            }

            [Serializable]
            public class AppFtpControlSettings : ISerializable
            {
                public AppFtpControlSettings(SerializationInfo info, StreamingContext context) { }
                public void GetObjectData(SerializationInfo info, StreamingContext context) { }
            }
        }
    }
}
