using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimitedChromeManager
{
    public partial class frmMain : Form
    {
        string[] steps =
        {
            "STEP_TOKEN_CHALL|Proxy Token Challenge",
            "STEP_CLEAN|Close all existing process in limited user",
            "STEP_HTTP|Start HTTP token server", //Long - 1 Request-
            "STEP_CHROME|Run limited browser",
            "STEP_TOKEN|Browser ext requested token",
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


        public void startHTTPThread(out ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask, int acceptTimeoutSec)
        {
            OneTimeHTTPRequest server = new OneTimeHTTPRequest()
            {
                findInRequest = Properties.Settings.Default.RequestFindings.Split(';'),
                AcceptTimeout = TimeSpan.FromSeconds(acceptTimeoutSec)
            };

            httpTask = new ThreadTask<OneTimeHTTPRequest.HTTPTaskResult>(
                () => { return server.StartListener(IPAddress.Loopback, 6667, () => Flags.USER_CANCEL); }
            );
            httpTask.Start();
        }

        public void joinHTTPThread(ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask, bool killOnTokenError) { 

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
                        log("Error that might risk the token, error: " +
                            httpTaskResult.description);
                        if (killOnTokenError)
                        {
                            log("closing all processes in user because token error");
                            new ProcessWatcher(LimitedChromeManager.Properties.Settings.Default.LimitedUserName)
                                .KillAllUserProcesses(()=>false);
                        }
                        break;
                }
            }
            else
            {
                log("Error serving token\n" + httpTask.GetError());
                checkItem("STEP_ERROR");
            }
        }

        private string RequestSync(string url, 
            string body = "", string method="GET", string accept="",  WebProxy proxy = null)
        {
            string result = "";
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                if (accept != "")
                    httpWebRequest.Accept = accept;
                httpWebRequest.Method = method;
                if (proxy != null)
                    httpWebRequest.Proxy = proxy;

                if (httpWebRequest.Method.ToLower() == "post")
                {
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(body);
                    }
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                log(ex.ToString());
            }
            return result;
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

            // Steps:
            // =====================================
            int step = 100 / 6;

            // 0. Token Challenge
            TokenChallenge();
            checkItem("STEP_TOKEN_CHALL");
            setProgress(step * 1);


            // 1. Close all apps in LimitedChrome
            if (Properties.Settings.Default.shouldKillProcessAtStart)
            {
                ProcessWatcher pw =
                    new ProcessWatcher(Properties.Settings.Default.LimitedUserName);
                checkItem("STEP_CLEAN");
                log("Closing apps in limited user...");
                int closedProcesses = pw.KillAllUserProcesses(() => Flags.USER_CANCEL);
                log("Closed " + closedProcesses + " apps in limited user");
            }
            else
            {
                log("Skipping closing apps due to config");
            }
            setProgress(step * 2);

            // 3. Start HTTP Token server (Has accept timout)
            log("Starting HTTP Token server...");
            checkItem("STEP_HTTP");
            ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask = null;
            startHTTPThread(out httpTask, Properties.Settings.Default.RequestTimeoutSec);
            setProgress(step * 3);

            // 4. Run chrome limited
            Process p = Process.Start(
                    Properties.Settings.Default.ProcessToRun,
                    Properties.Settings.Default.ProcessToRunArgs
                );
            checkItem("STEP_CHROME");
            setProgress(step * 4);

            // 5. Wait for HTTP thread to join
            setProgress(-1);
            joinHTTPThread(httpTask, killOnTokenError: true);
            setProgress(step * 5);

            log("All Done threads!");
            setProgress(100);
        }

        private void TokenChallenge()
        {
            string[] req_proxy = Properties.Settings.Default.proxyString.Split(':');
            WebProxy myProxy = null; // For development porpuses
            if (req_proxy.Length == 2 && int.TryParse(req_proxy[1], out _))
            {
                myProxy = new WebProxy(req_proxy[0], int.Parse(req_proxy[1]));
            }

            string proxy_host = Properties.Settings.Default.proxyLocalHost;
            log("Getting mitm proxy ep...");
            JsonElement endpoints = JsonSerializer.Deserialize<JsonElement>(
                    RequestSync("http://" + proxy_host + "/", accept: "application/json", proxy: myProxy)
            );
            string start_token_ep = endpoints
                .GetProperty("START_TOKEN_TEST")
                .GetProperty("ep").GetString();
            string verify_token_ep = endpoints
                .GetProperty("VERIFY_TOKEN_TEST")
                .GetProperty("ep").GetString();

            string mySalt = "ChromeManger_" + (new Random()).Next().ToString();
            var token_start_args = new { user_key = mySalt };

            log("Starting token challenge.");
            JsonElement start_info = JsonSerializer.Deserialize<JsonElement>(
                    RequestSync("http://" + proxy_host + start_token_ep, proxy: myProxy,
                        method: "POST", body: JsonSerializer.Serialize(token_start_args))
            );

            string hashed_file_path = start_info.GetProperty("path").GetString();
            string challenge_not_hashed = start_info.GetProperty("challenge").GetString();

            if (!File.Exists(hashed_file_path))
            {
                log("Got invalid file path: '" + hashed_file_path + "'");
            }
            else 
            { 
                log("Got token solution path");

                string challenge_solution = File.ReadAllText(hashed_file_path);

                var token_verify_args = new
                {
                    user_key = mySalt,
                    challenge = challenge_not_hashed,
                    proof = challenge_solution
                };

                JsonElement verify_info = JsonSerializer.Deserialize<JsonElement>(
                        RequestSync("http://" + proxy_host + verify_token_ep, proxy: myProxy,
                            method: "POST", body: JsonSerializer.Serialize(token_verify_args))
                );

                bool verify_hasError = verify_info.GetProperty("error").GetBoolean();
                string verify_errorText = verify_info.GetProperty("errortext").GetString();
                string token = verify_info.GetProperty("token").GetString();

                if (verify_hasError)
                {
                    log("Token error: " + verify_errorText);
                }
                else
                {
                    log("Got Token! Length " + token.Length);
                }
            }
        }

    }
}
