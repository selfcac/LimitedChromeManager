using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimitedChromeManager
{
    public partial class frmMain : Form
    {
        public void log(object data)
        {
            string message = string.Format("[{0}] {1}\n", DateTime.Now.ToShortTimeString(), data?.ToString() ?? "<Empty>");
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() =>
                {
                    rtbLog.Text = message + rtbLog.Text; 
                }));
            }
            else
            {
                rtbLog.Text = message + rtbLog.Text;
            }
            
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            log("Started");
        }
    }
}
