using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using kwm.Utils;

namespace kwm
{
    /// <summary>
    /// Base form for all the application's UI forms. Used to unify the 
    /// look and feel of the application such as setting the top-left icon.
    /// </summary>
    public partial class frmKBaseForm : Form
    {
        public frmKBaseForm()
        {
            InitializeComponent();
            Icon = Properties.Resources.TeamboxIcon;
        }
    }
}