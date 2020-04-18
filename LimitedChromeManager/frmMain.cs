using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
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
            "STEP_PROTECT|Protect from closing",
            "STEP_MONITOR|Monitor processes in limited user", // Long
            "STEP_CLEAN|Close all existing process in limited user",
            "STEP_HTTP|Start HTTP token server", //Long - 1 Request-
            "STEP_CHROME|Run limited chrome",
            "STEP_TOKEN|Chrome requested token",
            "STEP_WAIT|Wait for chrome to exit", //Long (on exit from monitor- check if processes > 0)
            "STEP_DONE|Done!",
            "STEP_|",
            "STEP_ERROR|ERROR - check logs"
        };

        public static class Flags
        {
            public static bool USER_CANCEL = false;
        }

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

        public void checkItem(string stepCode)
        {
            int index = -1;
            index = Array.FindIndex(steps,(step) => step.StartsWith(stepCode));
            if (index > -1)
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
            clstProcess.Items.AddRange(steps.Select((step)=>step.Substring(step.IndexOf('|')+1)).ToArray());
            bwProcess.RunWorkerAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Flags.USER_CANCEL = true;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void bwProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Exception ex = e.Error;
                checkItem("STEP_ERROR");
                log("Process Ended with error:\n" + ex?.ToString());
            }
            else
            {
                object result = e.Result;
                log("Process Ended. Cancelled? " + e.Cancelled);
                checkItem("STEP_DONE");
            }
            btnExit.Visible = true;
        }

        public void wait5SecondThread_example()
        {
            log("5 second thread started...");
            Thread.Sleep((int)TimeSpan.FromSeconds(5).TotalMilliseconds);
            log("5 second thread done.");
        }

        public void waitForCancel_example()
        {
            log("cancel thread started...");
            while (!Flags.USER_CANCEL)
            {
                Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }
            log("cancel thread done.");
        }

        public void oneTimeTokenHTTPThread()
        {
            OneTimeHTTPRequest server = new OneTimeHTTPRequest();
            try
            {
                log("Starting HTTP Token server...");
                checkItem("STEP_HTTP");
                server.StartListener(IPAddress.Loopback, 80);
                checkItem("STEP_TOKEN");
                log("Token sucess!!");
            }
            catch (Exception ex)
            {
                log("Error serving token\n" + ex.ToString());
                checkItem("STEP_ERROR");
            }
        }

        private void bwProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            /* Example for running multiple tasks:
             * ============================================
            Thread wait5 = new Thread(wait5SecondThread_example);
            Thread waitCancel = new Thread(waitForCancel_example);
            wait5.Start();
            waitCancel.Start();
            waitCancel.Join();
            wait5.Join();
            */

            Thread http = new Thread(oneTimeTokenHTTPThread);
            http.Start();
            
            
            http.Join();
            log("All Done threads!");
        }

       
    }
}
