using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace kwm
{
    public partial class frmInfoForm : Form
    {
        public frmInfoForm()
        {
            InitializeComponent();
        }

        public frmInfoForm(String title, String content)
            : this()
        {
            lblTitle.Text = title;
            lblContent.Text = content;
        }
    }
}
