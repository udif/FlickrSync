using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlickrSync.Auxiliary
{
    public partial class AuthBox : Form
    {
        public AuthBox()
        {
            InitializeComponent();
        }

        public string GetCode()
        {
            return this.code.Text;
        }
    }
}
