using kwm.KwmAppControls.AppKfs;
using System.Diagnostics;
using System;

namespace kwm.Utils
{
    public class TestSuite
    {
        public static void TestPath()
        {
            Debug.Assert(KfsPath.GetUnixFilePath("", true) == "");
            Debug.Assert(KfsPath.GetUnixFilePath("", false) == "");
            Debug.Assert(KfsPath.GetUnixFilePath("a", true) == "a/");
            Debug.Assert(KfsPath.GetUnixFilePath("a/", true) == "a/");
            Debug.Assert(KfsPath.GetUnixFilePath("a", false) == "a");
            Debug.Assert(KfsPath.GetUnixFilePath("a/", false) == "a");
            Debug.Assert(KfsPath.GetUnixFilePath("a\\b", true) == "a/b/");

            String[] SA = KfsPath.SplitRelativePath("a/b/");
            Debug.Assert(SA.Length == 2);
            Debug.Assert(SA[0] == "a");
            Debug.Assert(SA[1] == "b");

            Debug.Assert(KfsPath.DirName("") == "");
            Debug.Assert(KfsPath.DirName("a") == "");
            Debug.Assert(KfsPath.DirName("a/") == "a/");
            Debug.Assert(KfsPath.DirName("a/b") == "a/");

            Debug.Assert(KfsPath.BaseName("") == "");
            Debug.Assert(KfsPath.BaseName("a") == "a");
            Debug.Assert(KfsPath.BaseName("a/") == "");
            Debug.Assert(KfsPath.BaseName("a/b") == "b");

            Debug.Assert(KfsPath.StripTrailingDelim("") == "");
            Debug.Assert(KfsPath.StripTrailingDelim("/") == "");
            Debug.Assert(KfsPath.StripTrailingDelim("a") == "a");
            Debug.Assert(KfsPath.StripTrailingDelim("a/") == "a");
        }
    }
}