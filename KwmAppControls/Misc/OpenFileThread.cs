using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using Tbx.Utils;

namespace kwm.Utils
{
    /// <summary>
    /// This class allows for opening a file in a worker thread. It provides
    /// a kind of "Fire and forget" way to do so: failure will be reported from
    /// that worker thread only.
    /// </summary>
    public class OpenFileThread : KwmWorkerThread
    {
        /// <summary>
        /// Full path to the file to be opened.
        /// </summary>
        private String m_path;

        public OpenFileThread(String path)
        {
            m_path = path;
        }

        protected override void Run()
        {
            Exception failure = null;
            bool error = false;

            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.FileName = m_path;
                p.Start();
            }

            catch (Win32Exception ex)
            {
                // If we catch the following Win32 error :
                // 1155 - No application is associated with the specified file for this operation
                // We fallback by showing the the Open as... dialog.
                if (ex.NativeErrorCode == 1155)
                {
                    Syscalls.SHELLEXECUTEINFO info = new Syscalls.SHELLEXECUTEINFO();
                    info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
                    info.lpDirectory = Path.GetDirectoryName(m_path);
                    info.lpFile = Path.GetFileName(m_path);
                    info.nShow = (int)Syscalls.SW.SW_SHOWDEFAULT;
                    info.lpVerb = "openas";
                    info.fMask = Syscalls.SEE_MASK.ASYNCOK;
                    error = !Syscalls.ShellExecuteEx(ref info);

                    if (error)
                        failure = new Exception(Syscalls.GetLastErrorStringMessage());
                }

                // Can't do anything with the problem, bail out.
                else
                    failure = ex;
            }

            if (failure != null)
            {
                Logging.LogException(failure);
                MessageBox.Show("Unable to open " + m_path + Environment.NewLine +
                failure.Message, "Error on file open", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnCompletion()
        {
            // No op.
        }
    }
}
