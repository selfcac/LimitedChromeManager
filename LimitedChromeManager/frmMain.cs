﻿using System;
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
using Microsoft.Win32;
using System.Reflection;

namespace LimitedChromeManager
{
    public partial class frmMain : Form
    {
        string[] steps =
        {
            "STEP_TOKEN_CHALL|Proxy Token Challenge",
            "STEP_CLEAN|Close all existing process in limited user",
            "STEP_HTTP|Start HTTP token server", //Long - 1 Request-
            "STEP_TEMP_PORT|Send temp port to proxy",
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

        public T InvokeF<T>(Control control, Func<T> method, object[] methodParams = null)
        {
            if (control.InvokeRequired)
            {
                return (T)control.Invoke(method);
            }
            else
            {
               return (T)method.Invoke();
            }
        }

        public void checkItem(string stepCode)
        {
            int index = -1;
            index = Array.FindIndex(steps, (step) => step.StartsWith(stepCode + "|"));
            if (index > -1)
            {
                InvokeF(clstProcess, () => { clstProcess.SetItemCheckState(index, CheckState.Checked); });
            }
        }

        public bool isCheckedItem(string stepCode)
        {
            bool result = false;

            int index = -1;
            index = Array.FindIndex(steps, (step) => step.StartsWith(stepCode + "|"));
            if (index > -1)
            {
                result = InvokeF<bool>(clstProcess, () => { return clstProcess.GetItemChecked(index); });
            }

            return result;
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
            clstProcess.Items.AddRange(steps.Select((step) => step.Substring(step.IndexOf('|') + 1)).ToArray());
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
                log("Limited Chrome Flow Ended. Cancelled? " + e.Cancelled);
                checkItem("STEP_DONE");

                if (Properties.Settings.Default.CloseOnSucess)
                {
                    if (!isCheckedItem("STEP_ERROR") && !isCheckedItem("STEP_TOKEN_ERROR"))
                    {
                        // Done and all was okay so exit:
                        Application.Exit();
                    }
                }
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


        public Func<int> startHTTPThread(out ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask,
            string TokenData, int acceptTimeoutSec, string contentType = "text/plain")
        {
            OneTimeHTTPRequest server = new OneTimeHTTPRequest()
            {
                findInRequest = Properties.Settings.Default.RequestMustContainArray.Split(';'),
                AcceptTimeout = TimeSpan.FromSeconds(acceptTimeoutSec),
                DataToServe = Encoding.ASCII.GetBytes(TokenData),
                DataContentType = contentType
            };

            httpTask = new ThreadTask<OneTimeHTTPRequest.HTTPTaskResult>(
                () => { return server.StartListener(IPAddress.Loopback, 0, () => Flags.USER_CANCEL); }
            );
            httpTask.Start();
            return ()=> server.Port;
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
                            new ProcessWatcher(LimitedChromeManager.Properties.Settings.Default.AllowedClientUsernames)
                                .KillAllUserProcesses(() => false);
                        }
                        else
                        {
                            log("Skipping closing apps due to config");
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
            string body = "", string method = "GET", string accept = "", WebProxy proxy = null)
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

                httpWebRequest.Timeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
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

        const string proxy_host = "public-api.web-filter.local";
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
            int step = 100 / 7;

            log("Getting all EP from proxy");
            string[] req_proxy = Properties.Settings.Default.DebugProxyString.Split(':');
            WebProxy myProxy = null; // For development porpuses
            if (req_proxy.Length == 2 && int.TryParse(req_proxy[1], out _))
            {
                myProxy = new WebProxy(req_proxy[0], int.Parse(req_proxy[1]));
            }
            JsonElement endpoints = JsonSerializer.Deserialize<JsonElement>(
                RequestSync("http://" + proxy_host + "/", accept: "application/json", proxy: myProxy)
            );

            // 0. Token Challenge
            TokenResult token = TokenChallenge(endpoints, myProxy);
            if (string.IsNullOrEmpty(token.token))
            {
                log("Got empty token!");
                checkItem("STEP_TOKEN_ERROR");
            }
            else
            {
                checkItem("STEP_TOKEN_CHALL");
                setProgress(step * 1);


                // 1. Close all apps in LimitedChrome
                if (Properties.Settings.Default.ShouldKillProcessAtStart)
                {
                    ProcessWatcher pw =
                        new ProcessWatcher(Properties.Settings.Default.AllowedClientUsernames);
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
                string token_stringify = JsonSerializer.Serialize(token);

                ThreadTask<OneTimeHTTPRequest.HTTPTaskResult> httpTask = null;
                Func<int> start_port = startHTTPThread(out httpTask, token_stringify,
                    Properties.Settings.Default.RequestTimeoutSec, contentType: "application/json");

                // Let port populate by unjoind thread
                int get_port_retries = 10;
                int actual_port = start_port();
                while (actual_port < 0 && get_port_retries > 0)
                {
                    Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);

                    get_port_retries--;
                    actual_port = start_port();
                }
                if (actual_port < 0)
                    throw new Exception("Can't get server port");

                log("Server started in address http://localhost:" + actual_port + "/");
                setProgress(step * 3);

                log("Sending temp token to proxy...");
                SendTempPort(endpoints,myProxy, actual_port);
                checkItem("STEP_TEMP_PORT");
                setProgress(step * 4);

                // 4. Run chrome limited
                Process p = Process.Start(
                        Properties.Settings.Default.ProcessToRun,
                        Properties.Settings.Default.ProcessToRunArgs
                    );
                log("Open process with Id: " + p.Id + ", Exited? " + p.HasExited);
                checkItem("STEP_CHROME");
                setProgress(step * 5);

                // 5. Wait for HTTP thread to join
                setProgress(-1);
                joinHTTPThread(httpTask, killOnTokenError: Properties.Settings.Default.ShouldKillProcessAtStart);
                setProgress(step * 6);

                log("All Done threads!");
                setProgress(100);
            }
        }

        private void SendTempPort(JsonElement endpoints,  WebProxy myProxy, int actual_port)
        {
            string manager_port_ep = endpoints
                .GetProperty("MANAGER_PORT_SET")
                .GetProperty("ep").GetString();

            RequestSync("http://" + proxy_host + manager_port_ep, proxy: myProxy,
                       method: "POST", body: actual_port.ToString());
        }

        public class TokenResult
        {
            public string token { get; set; }
            public string salt { get; set; }
            public string token_salted  { get; set;}
        }

        private TokenResult TokenChallenge(JsonElement endpoints, WebProxy myProxy)
        {            
            log("Getting mitm proxy ep...");
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

            var startResult = new
            {
                error = start_info.GetProperty("error").GetBoolean(),
                errortext = start_info.GetProperty("errortext").GetString(),
                path = start_info.GetProperty("path").GetString(),
                challenge = start_info.GetProperty("challenge").GetString()
            };

            if (startResult.error || !File.Exists(startResult.path))
            {
                log("Got invalid file path: '" + startResult.path + "',\n error: '" + startResult.errortext + "'");
            }
            else 
            { 
                log("Got token solution path");

                string challenge_solution = File.ReadAllText(startResult.path);

                var token_verify_args = new
                {
                    user_key = mySalt,
                    challenge = startResult.challenge,
                    proof = challenge_solution
                };

                JsonElement verify_info = JsonSerializer.Deserialize<JsonElement>(
                        RequestSync("http://" + proxy_host + verify_token_ep, proxy: myProxy,
                            method: "POST", body: JsonSerializer.Serialize(token_verify_args))
                );

                var verifyResults = new
                {
                    error = verify_info.GetProperty("error").GetBoolean(),
                    errortext = verify_info.GetProperty("errortext").GetString(),
                    token = verify_info.GetProperty("token").GetString(),
                    salt = verify_info.GetProperty("salt").GetString(),
                    token_salted = verify_info.GetProperty("token_salted").GetString()
                };

                if (verifyResults.error)
                {
                    log("Token error: " + verifyResults.errortext);
                }
                else
                {
                    log("Got Token! Length " + verifyResults.token.Length);
                    return new TokenResult()
                        {
                            token = verifyResults.token,
                            salt = verifyResults.salt,
                            token_salted = verifyResults.token_salted
                        };
                    }
            }

            return new TokenResult();
        }

    }
}
