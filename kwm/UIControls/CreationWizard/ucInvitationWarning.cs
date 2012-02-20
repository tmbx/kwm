using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace kwm.UIControls.CreationWizard
{
    public partial class ucInvitationWarning : UserControl
    {
        /// <summary>
        /// Warning message to display.
        /// </summary>
        public String Message
        {
            get { return rtbFailedInvites.Text; }
            set {rtbFailedInvites.Text = value;}
        }

        public ucInvitationWarning()
        {
            InitializeComponent();
        }
    }
}
