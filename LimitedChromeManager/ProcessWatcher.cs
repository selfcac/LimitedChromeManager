using Socket2Process;
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
        LocalGroupsAndUsers usersInfo = new LocalGroupsAndUsers();
        string Username;
        Action<string> log;
        public TimeSpan sleepInterval = TimeSpan.FromSeconds(2);

        public ProcessWatcher(string username, Action<string> logger)
        {
            this.Username = username;
            this.log = (data) => { logger?.Invoke(data); };
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
            return ProcessUserLoop(null);
        }

        object GetProp<TEnum>(EventArrivedEventArgs e, TEnum prop )
        {
            return e.NewEvent[prop.ToString()];
        }

        void startProcessWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            // TODO: Check if process is son of ok process and path is ok if son of ok and path problem -> problem
            log(string.Format("Process START: [{0}] [{1}] {2}  son of {3} ",
                    usersInfo.getUserName(
                        new System.Security.Principal.SecurityIdentifier((byte[])GetProp(e, WMI.EWin32_Start.Sid), 0).Value
                    ),
                    GetProp(e, WMI.EWin32_Start.ProcessName),
                    GetProp(e, WMI.EWin32_Start.ProcessID),
                    GetProp(e, WMI.EWin32_Start.ParentProcessID)                   
            ));
        }


        void stopProcessWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            log(string.Format("Process STOP: [{0}] [{1}] ID: {2} from {3}",
                    usersInfo.getUserName(
                        new System.Security.Principal.SecurityIdentifier((byte[])GetProp(e, WMI.EWin32_Stop.Sid), 0).Value
                    ),
                    GetProp(e, WMI.EWin32_Stop.ProcessName),
                    GetProp(e, WMI.EWin32_Stop.ProcessID),
                    GetProp(e, WMI.EWin32_Stop.ParentProcessID)
            ));
        }

        void newProcessInstance_EventArrived(object sender, EventArrivedEventArgs e)
        {
            log(string.Format("Process NEW: ID:{0} from {1} Path: {2}",
                    GetProp(e, WMI.EWin32_Process.ProcessId),
                    GetProp(e, WMI.EWin32_Process.ParentProcessId),
                    GetProp(e, WMI.EWin32_Process.ExecutablePath)
                ));
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
            //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/krnlprov/win32-processstarttrace
            ManagementEventWatcher openProcessWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            openProcessWatch.EventArrived
                                += new EventArrivedEventHandler(startProcessWatch_EventArrived);
            //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/krnlprov/win32-processstoptrace
            ManagementEventWatcher closeProcessWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            closeProcessWatch.EventArrived
                                += new EventArrivedEventHandler(startProcessWatch_EventArrived);
        https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process
            ManagementEventWatcher newProcessInstanceWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceCreationEvent where TargetInstance ISA 'Win32_Process'"));
            newProcessInstanceWatch.EventArrived
                                += new EventArrivedEventHandler(newProcessInstance_EventArrived);

            openProcessWatch.Start();
            newProcessInstanceWatch.Start();
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
                    while (!(isCancelled?.Invoke() ?? false) ) // && openedCount > closedCount)
                    {
                        Thread.Sleep((int)sleepInterval.TotalMilliseconds);
                    }

                    // TODO: Cancel + Close all if unkown process

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
            newProcessInstanceWatch.Stop();
            closeProcessWatch.Stop();

            return error;
        }

    }
}
