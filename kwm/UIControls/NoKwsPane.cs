using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using kwm.Utils;
using Tbx.Utils;

namespace kwm
{
    public partial class NoKwsPane : UserControl
    {
        /// <summary>
        /// Reference to the UI broker.
        /// </summary>
        private WmUiBroker m_uiBroker;

        /// <summary>
        /// Sets the UI Broker. Called by frmMain in Initialize().
        /// </summary>
        public WmUiBroker UiBroker
        {
            set { m_uiBroker = value; }
        }

        public NoKwsPane()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                Debug.Assert(m_uiBroker != null);

                m_uiBroker.RequestCreateKws();
            }
            catch (Exception ex)
            {
                Base.HandleException(ex);
            }
        }
    }
}
