using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
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
            "STEP_CLEAN|Close all existing process in limited user",
            "STEP_HTTP|Start HTTP token server", //Long - 1 Request-
            "STEP_CHROME|Run limited chrome",
            "STEP_TOKEN|Chrome requested token",
            "STEP_DONE|Done!",
            "STEP_|",
            "STEP_TOKEN_ERROR|Token error - close all process",
            "STEP_ERROR|ERROR (General)"
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
            File.AppendAllText("log.txt", message);
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

        public int waitForCancel_example()
        {
            int timeSlept = 0;
            log("cancel thread started...");
            while (!Flags.USER_CANCEL)
            {
                Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                timeSlept++;
            }
            log("cancel thread done.");
            return timeSlept;
        }


        public void startHTTPThread(out ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask)
        {
            log("Starting HTTP Token server...");
            checkItem("STEP_HTTP");

            OneTimeHTTPRequest server = new OneTimeHTTPRequest()
            {
                findInRequest = Properties.Settings.Default.RequestFindings.Split(';')
            };

            httpTask = new ThreadTask<OneTimeHTTPRequest.HTTPTaskResult>(
                () => { return server.StartListener(IPAddress.Loopback, 6667, () => Flags.USER_CANCEL); }
            );
            httpTask.Start();
        }

        public void joinHTTPThread(ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask) { 

            if (httpTask.Join())
            {
                OneTimeHTTPRequest.HTTPTaskResult httpTaskResult = httpTask.Result();
                switch (httpTaskResult.StatusCode)
                {
                    case OneTimeHTTPRequest.HTTPResultEnum.SUCCESS:
                        checkItem("STEP_TOKEN");
                        log("Token sucess!!");
                        break;
                    case OneTimeHTTPRequest.HTTPResultEnum.NOTOKEN_ERROR:
                        log("Error serving token\n" + httpTaskResult.description);
                        checkItem("STEP_ERROR");
                        break;
                    case OneTimeHTTPRequest.HTTPResultEnum.TOKEN_AUTH_ERROR:
                        checkItem("STEP_TOKEN_ERROR");
                        log("Error that might risk the token, closing all processes in user, error: " +
                            httpTaskResult.description);
                        new ProcessWatcher(LimitedChromeManager.Properties.Settings.Default.LimitedUserName)
                            .KillAllUserProcesses();
                        break;
                }
            }
            else
            {
                log("Error serving token\n" + httpTask.GetError());
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

            //Thread http = new Thread(oneTimeTokenHTTPThread);
            //http.Start();
            //http.Join();

            //ThreadTask<int> waitCancel = new ThreadTask<int>(waitForCancel_example);
            //waitCancel.Start();

            //if (waitCancel.Join())
            //{
            //    log("Waited " + waitCancel.Result() + " Times");
            //}

            ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask;
            startHTTPThread(out httpTask);
            //joinHTTPThread(httpTask);
            httpTask.Join();

            log("All Done threads!");
        }

       
    }
}
