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
        //https://qa.social.msdn.microsoft.com/Forums/vstudio/en-US/5da98f49-cf3d-4eef-a302-feb87d9342ab/how-can-i-detect-when-a-prog-opened-with-shell-is-closed-quotexplorerexequot?forum=vbgeneral

        LocalGroupsAndUsers usersInfo = new LocalGroupsAndUsers();
        string Username;
        public TimeSpan sleepInterval = TimeSpan.FromSeconds(2);

        public ProcessWatcher(string username)
        {
            this.Username = username;
        }

        int ProcessUserLoop(Action<Process> mainLoop, Func<bool> isCanceled)
        {
            int processWithoutErrors = 0;
            foreach (Process p in Process.GetProcesses())
            {
                if (isCanceled?.Invoke() ?? false)
                {
                    processWithoutErrors = -1;
                    break;
                }
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

        public int KillAllUserProcesses(Func<bool> isCanceled)
        {
            return ProcessUserLoop(
                (p) => { if (!p.HasExited) {p.Kill(); } },
                isCanceled
            );
        }

    }
}
