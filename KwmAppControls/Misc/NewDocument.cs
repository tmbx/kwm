using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections;
using System.IO;
using Tbx.Utils;

namespace kwm.Utils
{
    /// <summary>
    /// Represent a document type that can appear in the New context menu.
    /// </summary>
    public class NewDocument
    {
        /// <summary>
        /// Human-readable document type description.
        /// </summary>
        public String DisplayName;

        /// <summary>
        /// Type extension.
        /// </summary>
        public String Extension;

        /// <summary>
        /// Type Program ID.
        /// </summary>
        public String ProgID;

        /// <summary>
        /// Type default icon.
        /// </summary>
        public Icon TypeIcon;

        /// <summary>
        /// Action to take when creating a new document. Valid verbs:
        /// 
        /// Command:    Executes an application. This is a REG_SZ value 
        ///             specifying the path of the application to be executed. 
        ///             For example, you could set it to launch a wizard.
        /// Data:	    Creates a file containing specified data. Data is a 
        ///             REG_BINARY value with the file's data. Data is ignored 
        ///             if either NullFile or FileName is specified.
        /// FileName:	Creates a file that is a copy of a specified file. 
        ///             FileName is a REG_SZ value, set to the fully qualified 
        ///             path of the file to be copied.
        /// NullFile:	Creates an empty file. NullFile is not assigned a value. 
        ///             If NullFile is specified, the Data and FileName values 
        ///             are ignored.
        /// </summary>
        public Hashtable Verbs = new Hashtable();

        /// <summary>
        /// No manual instantiation
        /// </summary>
        private NewDocument() { }

        public override string ToString()
        {
            return DisplayName + " (" + Extension + ")";
        }

        /// <summary>
        /// Try to create a new NewDocument object. Returns null if impossible.
        /// </summary>
        /// <param name="rootKey">ShellNew key's parent.</param>
        /// <param name="extension">Type extension/param>
        /// <param name="progID">Type ProgID.</param>
        /// <returns></returns>
        public static NewDocument GetNewDoc(RegistryKey ParentKey, String Extension, String ProgID)
        {
            Debug.Assert(Extension.StartsWith("."));
            Debug.Assert(ProgID != "");

            RegistryKey ShellNewKey = null;
            RegistryKey ProgIDKey = null;

            NewDocument retValue = new NewDocument();
            retValue.Extension = Extension;
            retValue.ProgID = ProgID;

            try
            {
                // Get the verbs list for this key. There can be multiple ones. If more
                // than one verb is present, use them with the right priority order.
                ShellNewKey = ParentKey.OpenSubKey("ShellNew");

                foreach (String strShellNewContent in ShellNewKey.GetValueNames())
                {
                    Verb v = null;

                    if (strShellNewContent == "Command" ||
                        strShellNewContent == "FileName")
                    {
                        Debug.Assert(ShellNewKey.GetValueKind(strShellNewContent) == RegistryValueKind.String ||
                            ShellNewKey.GetValueKind(strShellNewContent) == RegistryValueKind.ExpandString);
                        String value = (String)ShellNewKey.GetValue(strShellNewContent);
                        if (value != null && value != "")
                            v = new Verb(VerbToVerbType(strShellNewContent), value);
                    }
                    else if (strShellNewContent == "Data")
                    {
                        byte[] value = null;
                        if (ShellNewKey.GetValueKind(strShellNewContent) == RegistryValueKind.Binary)
                        {
                            value = (byte[])ShellNewKey.GetValue(strShellNewContent);
                        }
                        else if (ShellNewKey.GetValueKind(strShellNewContent) == RegistryValueKind.String)
                        {
                            // It has been observed that some ShellNew key contained the binary
                            // data in a registry key of type "String". This was unexpected, the
                            // workaround consists of converting the String into a byte array.
                            string str = (string)ShellNewKey.GetValue(strShellNewContent);
                            value = (new UnicodeEncoding()).GetBytes(str);
                        }
                        else
                        {
                            Logging.Log(2, "Data of unknown type: The registry key data is of type " + ShellNewKey.GetValueKind(strShellNewContent).ToString() + "which is not supported");
                            continue;
                        }

                        if (value != null)
                            v = new Verb(VerbType.Data, value);
                    }
                    else if (strShellNewContent == "NullFile")
                    {
                        v = new Verb(VerbType.Nullfile, "");
                    }
                    else
                    {
                        Logging.Log(2, "Unknown Verb '" + strShellNewContent + "'");
                        continue;
                    }

                    if (v != null)
                        retValue.Verbs.Add(v.VerbType, v);
                }

                // If no verb is present, abort.
                if (retValue.Verbs.Count < 1)
                    return null;

                // Get the display name located in the ProgID key's (Default) value.
                // Example: HKEY_CLASSES_ROOT\Word.Document.8
                ProgIDKey = Registry.ClassesRoot.OpenSubKey(ProgID);

                // If no DisplayName is present, abort.
                retValue.DisplayName = (String)ProgIDKey.GetValue("");
                if (retValue.DisplayName == null || retValue.DisplayName == "")
                    return null;

                retValue.TypeIcon = ExtractIcon.GetIcon(Extension, true);
            }
            catch (Exception ex)
            {
                Logging.Log(2, "An error occured creating the NewDocument structure: " + ex.Message);
                Logging.LogException(ex);
                return null;
            }
            finally
            {
                if (ShellNewKey != null)
                    ShellNewKey.Close();

                if (ProgIDKey != null)
                    ProgIDKey.Close();
            }

            return retValue;
        }

        /// <summary>
        /// Execute the object's Verb action to the specified destination.
        /// </summary>
        /// <param name="destination">Target directory, slash terminated.</param>
        public void DoVerbAction(String destination, String param)
        {
            if (Verbs.ContainsKey(VerbType.Command))
            {
                string error = "";
                if (Misc.OpenFile(((Verb)Verbs[VerbType.Command]).VerbAction as String + " " + destination + " " + param, ref error))
                    throw new Exception(error);
            }
            else if (Verbs.ContainsKey(VerbType.Nullfile))
            {
                using (StreamWriter sw = File.CreateText(destination + param)) {}
            }
            else if (Verbs.ContainsKey(VerbType.FileName))
            {
                string action = (Verbs[VerbType.FileName] as Verb).VerbAction as String;
                string sourceFile = "";

                if (File.Exists(action))
                    sourceFile = action;
                else if (File.Exists(Environment.GetEnvironmentVariable("Userprofile") + @"\Templates\" + action))
                    sourceFile = Environment.GetEnvironmentVariable("Userprofile") + @"\Templates\" + action;
                else if (File.Exists(Environment.GetEnvironmentVariable("Allusersprofile") + @"\Templates\" + action))
                    sourceFile = Environment.GetEnvironmentVariable("Allusersprofile") + @"\Templates\" + action;
                else if (File.Exists(Environment.GetEnvironmentVariable("Systemroot") + @"\ShellNew\" + action))
                    sourceFile = Environment.GetEnvironmentVariable("Systemroot") + @"\ShellNew\" + action;
                
                if (sourceFile == "")
                    throw new Exception(action + " not found.");

                Misc.CopyFile(sourceFile, destination + param, true, true, false, true, false);
            }
            else if (Verbs.ContainsKey(VerbType.Data))
            {
                byte[] data = ((Verb)Verbs[VerbType.Data]).VerbAction as byte[];

                if (data == null)
                    throw new Exception("No data present.");

                using(BinaryWriter binWriter = new BinaryWriter(File.Open(destination + param, FileMode.Create)))
                {
                    binWriter.Write(data);
                }

            }
            else
                Debug.Assert(false, "No verb in NewDocument object.");
        }

        /// <summary>
        /// Returns the VerbType associated to a String representation of a verb.
        /// </summary>
        /// <param name="Verb"></param>
        /// <returns></returns>
        public static VerbType VerbToVerbType(String Verb)
        {
            if (Verb == "Command")
                return VerbType.Command;
            else if (Verb == "Data")
                return VerbType.Data;
            else if (Verb == "FileName")
                return VerbType.FileName;
            else if (Verb == "NullFile")
                return VerbType.Nullfile;
            else
                throw new ArgumentException("Invalid verb " + Verb);
        }
    }

    public class Verb
    {
        public VerbType VerbType;
        public object VerbAction;

        public Verb(VerbType Type, object Action)
        {
            VerbType = Type;
            VerbAction = Action;
        }
    }

    /// <summary>
    /// Valid Verb types.
    /// </summary>
    public enum VerbType
    {
        Command,
        Data,
        FileName,
        Nullfile
    }
}
