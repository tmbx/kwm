using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using kwm.Utils;
using System.Windows.Forms;
using Tbx.Utils;

namespace kwm
{
    class CFileLogger : ILogger
    {
        private String m_strPath;
        private String m_strFilename;
        private StreamWriter m_writer;

        public CFileLogger(String _path, String _filename)
        {
            Directory.CreateDirectory(_path);

            // Flush logs older than 5 days.
            try
            {
                string[] files = Directory.GetFiles(_path);
                foreach (string path in files)
                {
                    try
                    {
                        if (File.GetLastAccessTime(path).AddDays(5) < DateTime.Now)
                            File.Delete(path);
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }

            m_strPath = _path;
            m_strFilename = _filename;
            m_writer = File.AppendText(m_strPath + "\\" + m_strFilename);
        }

        /// <summary>
        /// Close the writer stream and unregister from the logging class.
        /// </summary>
        public void CloseAndUnregister()
        {
            lock (Logging.Mutex)
            {
                Logging.UnregisterLogHandler(this);
                if (m_writer != null)
                {
                    m_writer.Close();
                    m_writer = null;
                }
            }
        }

        public void HandleOnLogEvent(Object sender, LogEventArgs args)
        {
            if (m_writer == null) return;

            String logText = args.Timestamp.ToString("") + " | " + args.Severity + " | " +
                             args.Caller + "::line " + args.Line + " | " + args.Message +
                             Environment.NewLine;
            m_writer.Write(logText);
            
            // Bad for perfomance but necessary in case of a crash.
            m_writer.Flush();
        }
    }
}
