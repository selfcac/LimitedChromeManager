using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimitedChromeManager
{
    public partial class frmMain : Form
    {
        string[] steps =
        {
            "Protect from closing",
            "Monitor processes in limited user", // Long
            "Close all existing process in limited user",
            "Start HTTP token server", //Long - 1 Request-
            "Run limited chrome",
            "Wait for chrome to exit", //Long (on exit from monitor- check if processes > 0)
            "Done!",
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
       
        public void log(object data)
        {
            string message = string.Format("[{0}] {1}\n", DateTime.Now.ToLongTimeString(), data?.ToString() ?? "<Empty>");
            InvokeF(rtbLog, () => { rtbLog.Text = message + rtbLog.Text; });
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            log("Started");
            clstProcess.Items.AddRange(steps);
            bwProcess.RunWorkerAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void bwProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            setProgress(0);
            Thread.Sleep(1000);
            setProgress(50);
            Thread.Sleep(1000);
            setProgress(100);
            Thread.Sleep(1000);
            setProgress(-1);
            Thread.Sleep(1000);

            throw new Exception("Example exception");

            e.Result = 0;
        }

        private void bwProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null )
            {
                Exception ex = e.Error;
                log("Process Ended with error:\n" + ex?.ToString());
            }
            else if (e.Cancelled)
            {
                log("Process Cancelled by User");
            }
            else 
            {
                Object result = e.Result;
                log("Process Ended without any Errors");
            }
        }

        
    }
}
