using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Socket2Process;

namespace SocketInfoConsole
{
    class Program
    {
        static void TcpIpPerformance()
        {
            long tableTime, processTimeTotal, sidTimeTotal;
            
            var sw = new Stopwatch();
            sw.Start();

            var tcpTable = TcpTable.GetAllTcpConnections();
            sw.Stop();
            tableTime = sw.ElapsedMilliseconds;

            var itemCount = tcpTable.Length;
            //=====================================================
            var processPaths = new string[tcpTable.Length];
            sw.Reset();
            sw.Start();
            for (int i = 0; i < tcpTable.Length; i++)
            {
                if (tcpTable[i].owningPid != 0)
                {
                    processPaths[i] = ProcessPath.GetProcessPath((uint)tcpTable[i].owningPid);
                }
                else
                {
                    processPaths[i] = "SystemIdle";
                }
            }
            sw.Stop();
            processTimeTotal = sw.ElapsedMilliseconds;

            //=====================================================
            var processSid = new string[tcpTable.Length];
            var myProcess = Process.GetCurrentProcess();
            sw.Reset();sw.Start();
            //for (int i = 0; i < tcpTable.Length; i++)
            //{
            //    if (processPaths[i] != "")
            //    {
            //        processSid[i] = ProcessUser.sidFromProcess((uint)tcpTable[i].owningPid);
            //    }
            //}
            Action<string> loggerSample = new Action<string>((text) => Console.WriteLine(text));

            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id,loggerSample));

            sw.Stop();
            Console.WriteLine("Only fast sid: " + sw.ElapsedMilliseconds + "ms");

            sw.Reset(); sw.Start();
            Console.WriteLine(ProcessUserSid.sidFromProcess(4,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4,loggerSample));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4, loggerSample));

            sw.Stop();
            sidTimeTotal = sw.ElapsedMilliseconds;

            Console.WriteLine($"Results\n======\nTcpTable: {tableTime}ms\nPaths: {processTimeTotal}ms\nSids: {sidTimeTotal}ms");
        }

        static void UserPerformace()
        {
            LocalGroupsAndUsers.PrintInfo(new Action<string>((text) => { Console.WriteLine(text); }));
            LocalGroupsAndUsers users = new LocalGroupsAndUsers();
            string[] sids = {"S-1-5-32-5442313" };
            string[] names = { "Users1" , "Users2" , "Users" };

            var sw = new Stopwatch();
            sw.Start();
            if (users.getUserName("S-1-5-21-865263210-2397608334-156313846-1001") == "Yoni" &&
                    users.isUserInGroups("S-1-5-21-865263210-2397608334-156313846-1002",sids,names ))
            {
                Console.WriteLine("Allowed");
            }
            sw.Stop();
            Console.WriteLine($"User check took {sw.ElapsedMilliseconds}ms");
        }


        static void Main(string[] args)
        {
            TcpIpPerformance();
            UserPerformace();
           
            Console.ReadLine();
        }
    }
}
