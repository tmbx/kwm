using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace kwm
{
    public class UnitTestException : Exception
    {
        public UnitTestException(String msg) : base(msg) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : System.Attribute { }

    public partial class UnitTest : Form
    {
        public UnitTest()
        {
            InitializeComponent();
        }

        public void Assert(bool b)
        {
            if (!b)
            {
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame frame = stackTrace.GetFrames()[1];  // get method calls (frames)
                throw new UnitTestException("Assertion Failed: " + frame.ToString());
            }
        }

        private MethodInfo[] GetUnitTestList()
        {
            MethodInfo[] methods = this.GetType().GetMethods();
            List<MethodInfo> Tests = new List<MethodInfo>();
            foreach (MethodInfo m in methods)
            {
                object[] attribs = m.GetCustomAttributes(false);
                bool isATest = false;
                foreach (object attr in attribs)
                {
                    if (attr as TestAttribute != null)
                    {
                        isATest = true;
                        break;
                    }
                }

                if (isATest)
                {
                    Tests.Add(m);
                }
            }
            return Tests.ToArray();
        }

        private void RunBtn_Click(object caller, EventArgs ev)
        {
            ResultView.Items.Clear();
            ResultView.Refresh();
            MethodInfo[] utList = GetUnitTestList();
            foreach (MethodInfo m in utList)
            {
                try
                {
                    m.Invoke(this, null);
                    ResultView.Items.Add(new ListViewItem(m.Name + ": success"));
                }
                catch (TargetInvocationException _e)
                {
                    Exception e = _e.InnerException;
                    if (e.GetType() == typeof(UnitTestException))
                    {
                        ResultView.Items.Add(new ListViewItem(m.Name + ": " + e.Message));
                    }
                    else
                    {
                        ResultView.Items.Add(new ListViewItem(m.Name + " : " + "Exception : " + e.ToString()));
                    }
                }
            }
        }
    }
}