using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// This class contains static methods to manipulate paths and obtain
    /// file information.
    /// </summary>
    public class KfsPath
    {
        /// <summary>
        /// This method converts every backslash in the path to slash. 
        /// If the slashTerminated param is true, the path is appended a trailing
        /// delimiter if necessary, otherwise it is removed if necessary.
        /// </summary>
        /// <param name="pathToConvert">The path to convert.</param>
        /// <param name="slashTerminated">True if a trailing delimiter must be appended.</param>
        public static String GetUnixFilePath(String pathToConvert, bool slashTerminated)
        {
            String tempPath = pathToConvert.Replace("\\", "/");
            if (tempPath.Length > 0)
            {
                if (slashTerminated)
                {
                    if (tempPath[tempPath.Length - 1] != '/')
                    {
                        tempPath = tempPath + "/";
                    }
                }
                else
                {
                    if (tempPath[tempPath.Length - 1] == '/')
                    {
                        tempPath = tempPath.Substring(0, tempPath.Length - 1);
                    }
                }
            }
            return tempPath;
        }

        /// <summary>
        /// This method converts every slash in the path to backslashes. 
        /// If the backslashTerminated param is true, the path is appended a trailing
        /// delimiter if necessary, otherwise it is removed if necessary.
        /// </summary>
        /// <param name="pathToConvert">The path to convert.</param>
        /// <param name="backslashTerminated">True if a trailing delimiter must be appended.</param>
        public static String GetWindowsFilePath(String pathToConvert, bool backslashTerminated)
        {
            String tempPath = pathToConvert.Replace("/", "\\");
            if (tempPath.Length > 0)
            {
                if (backslashTerminated)
                {
                    if (tempPath[tempPath.Length - 1] != '\\')
                    {
                        tempPath = tempPath + "\\";
                    }
                }
                else
                {
                    if (tempPath[tempPath.Length - 1] == '\\')
                    {
                        tempPath = tempPath.Substring(0, tempPath.Length - 1);
                    }
                }
            }
            return tempPath;
        }

        /// <summary>
        /// Test if fileName contains invalid characters for a Windows file name. 
        /// </summary>
        public static bool IsValidFileName(String fileName)
        {
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 ||
                fileName.Length == 0 ||
                fileName.StartsWith(" "))
            {
                return false;
            }

            try
            {
                Encoding latinEuropeanEncoding = Encoding.GetEncoding("iso-8859-1", EncoderExceptionFallback.ExceptionFallback, DecoderExceptionFallback.ExceptionFallback);
                Encoding uniCode = Encoding.Unicode;
                Encoding.Convert(uniCode, latinEuropeanEncoding, uniCode.GetBytes(fileName));
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Return a list with each portion of the path. 
        /// Example : a\b\c\allo.txt
        /// The list returned is:
        /// |a|b|c|allo.txt|
        /// 
        /// The function doesn't care whether the path is a UNIX or a Windows path. It
        /// splits portions at slashses and backslashes. The function does not work on
        /// absolute paths.
        /// </summary>
        public static String[] SplitRelativePath(String relativePath)
        {
            return relativePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Return the directory portion of the path specified. The directory
        /// portion will have a trailing delimiter if it is non-empty.
        /// </summary>
        public static String DirName(String path)
        {
            if (path == "") return "";
            int LastIndex = path.Length - 1;
            for (; LastIndex > 0 && !IsDelim(path[LastIndex]); LastIndex--) { }
            if (!IsDelim(path[LastIndex])) return "";
            return path.Substring(0, LastIndex + 1);
        }

        /// <summary>
        /// Return the file portion of the path specified.
        /// </summary>
        public static String BaseName(String path)
        {
            if (path == "") return "";
            int LastIndex = path.Length - 1;
            for (; LastIndex > 0 && !IsDelim(path[LastIndex]); LastIndex--) { }
            if (!IsDelim(path[LastIndex])) return path;
            return path.Substring(LastIndex + 1, path.Length - LastIndex - 1);
        }

        /// <summary>
        /// Add a trailing slash to the path specified if the path is non-empty
        /// and it does not already end with a delimiter.
        /// </summary>
        public static String AddTrailingSlash(String path)
        {
            return AddTrailingSlash(path, false);
        }

        /// <summary>
        /// Add a trailing slash to the path specified if the path does not already 
        /// end with a delimiter, or if the path is empty and slashIfEmpty is set to true.
        /// </summary>
        public static String AddTrailingSlash(String path, bool slashIfEmpty)
        {
            if (path == "")
            {
                if (slashIfEmpty)
                    return "/";
                else
                    return "";
            }

            if (IsDelim(path[path.Length - 1])) return path;
            return path + "/";
        }

        /// <summary>
        /// Add a trailing backslash to the path specified if the path is non-empty
        /// and it does not already end with a delimiter.
        /// </summary>
        public static String AddTrailingBackslash(String path)
        {
            if (path == "") return "";
            if (IsDelim(path[path.Length - 1])) return path;
            return path + @"\";
        }

        /// <summary>
        /// Remove the trailing delimiter from the string specified, if there
        /// is one.
        /// </summary>
        public static String StripTrailingDelim(String path)
        {
            if (path == "") return "";
            if (IsDelim(path[path.Length - 1])) return path.Substring(0, path.Length - 1);
            return path;
        }

        /// <summary>
        /// Return true if the character specified is a slash or a backslash.
        /// </summary>
        public static bool IsDelim(Char c)
        {
            return (c == '/' || c == '\\');
        }

        /// <summary>
        /// Return of the file specified. The file must exist.
        /// </summary>
        public static UInt64 GetFileSize(String path)
        {
            return (UInt64)(new FileInfo(path)).Length;
        }
    }

    /// <summary>
    /// Contains a method to compute the hash of a file.
    /// </summary>
    public class KfsHash
    {
        /// <summary>
        /// Compute the hash and the size of the file specified.
        /// </summary>
        public static void GetHashAndSize(String path, out byte[] hash, out UInt64 size)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
                hash = md5Hasher.ComputeHash(fs);
                size = (UInt64)fs.Length;
            }

            finally
            {
                if (fs != null) fs.Close();
            }
        }

        /// <summary>
        /// Same as above, for an open file.
        /// </summary>
        public static void GetHashAndSize(Stream s, out byte[] hash, out UInt64 size)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            hash = md5Hasher.ComputeHash(s);
            size = (UInt64)s.Length;
        }
    }
}