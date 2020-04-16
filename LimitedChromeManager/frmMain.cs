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
        string[] steps =
        {
            "Protect from closing",
            "Monitor processes in limited user",
            "Close all existing process in limited user",
            "Start HTTP token server",
            "Run limited chrome",
            "Wait for chrome to exit",
            "",
            "ERROR - check logs"
        };

        public void InvokeF(Control control, Action method, object[] methodParams = null)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(method);
            }
            else
            {
                method.Invoke();
            }
        }

        public void checkItem(int index)
        {
            if (index < steps.Length)
            {
                InvokeF(clstProcess, () => { clstProcess.SetItemCheckState(index, CheckState.Checked); });
            }
        }

        public void log(object data)
        {
            string message = string.Format("[{0}] {1}\n", DateTime.Now.ToShortTimeString(), data?.ToString() ?? "<Empty>");
            InvokeF(rtbLog, () => { rtbLog.Text = message + rtbLog.Text; });
        }

        public void setProgress(int percentage)
        {
            if (percentage >= 0 && percentage <= 100)
            {
                InvokeF(pbMain, () =>
                {
                    pbMain.Style = ProgressBarStyle.Continuous;
                    pbMain.Value = percentage;
                });
            }
            else
            {
                InvokeF(pbMain, () =>
                 {
                     pbMain.Style = ProgressBarStyle.Marquee;
                 });
            }
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            log("Started");
            clstProcess.Items.AddRange(steps);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
