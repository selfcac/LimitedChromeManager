using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LimitedChromeManager
{
    public class ProcessWatcher
    {
        string Username;
        public TimeSpan sleepInterval = TimeSpan.FromSeconds(2);

        public ProcessWatcher(string username)
        {
            this.Username = username;
        }

        int ProcessUserLoop(Action<Process> mainLoop)
        {
            int processWithoutErrors = 0;
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    Socket2Process.LocalGroupsAndUsers users = new Socket2Process.LocalGroupsAndUsers();
                    string pSID = Socket2Process.ProcessUserSid.sidFromProcess((uint)p.Id, (error) => { });
                    string pUserName = users.getUserName(pSID);

                    if (pSID != "" && pUserName != "" && pUserName.ToLower() == Username.ToLower())
                    {
                        mainLoop?.Invoke(p);
                        processWithoutErrors++;
                    }
                }
                catch (Exception ex)
                {
                    // If a process is already closed we might get errors here
                }
            }
            return processWithoutErrors;
        }

        public int KillAllUserProcesses()
        {
            return ProcessUserLoop( (p) => { if (!p.HasExited) {p.Kill(); } });
        }

        public int processCount()
        {
            return ProcessUserLoop( (p) => { });
        }

        void startProcessWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("Process started: {0}"
                              , e.NewEvent.Properties["ProcessName"].Value);
        }


        void stopProcessWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("Process started: {0}"
                              , e.NewEvent.Properties["ProcessName"].Value);
        }


        /*
         * Steps:
         * 1) start watch to catch case where a process is constantly opening and closing to avoid listing detection
         * 2) Kill all currently open process under user
         * 3) Check no process is left open + number of opened is 0 (even if they closed) [ against same avoiding listing detection ]
         * 4) When all process are closed (closed==opened) finish
        */
        int openedCount = 0;
        int closedCount = 0;
        public string StartWatchUntilAllClose(string[] allowedPaths, Func<bool> isCancelled)
        {
            string error = "";

            // Start watch for start stop
            ManagementEventWatcher openProcessWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            openProcessWatch.EventArrived
                                += new EventArrivedEventHandler(startProcessWatch_EventArrived);
            ManagementEventWatcher closeProcessWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            closeProcessWatch.EventArrived
                                += new EventArrivedEventHandler(startProcessWatch_EventArrived);

            openProcessWatch.Start();
            closeProcessWatch.Start();

            try
            {
                // Kill all
                KillAllUserProcesses();

                // Verify 0 process + 0 opens
                int openProcesses = processCount();
                if (openProcesses > 0 || openedCount > 0)
                {
                    error = "Couldn't clean user, some process stil Open(ed)!";

                }
                else
                {
                    // Clear killed count to 0
                    closedCount = 0;

                    // Sleep untill : closed = opened + Enable cancelling
                    while (openedCount > closedCount && !(isCancelled?.Invoke() ?? false))
                    {
                        Thread.Sleep((int)sleepInterval.TotalMilliseconds);
                    }

                    if ((isCancelled?.Invoke() ?? false))
                    {
                        error = "Cancelled by user";
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.ToString();
            }

            openProcessWatch.Stop();
            closeProcessWatch.Stop();

            return error;
        }

    }
}
