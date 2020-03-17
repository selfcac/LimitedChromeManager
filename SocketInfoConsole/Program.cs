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
        static void populate()
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
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id));
            Console.WriteLine(ProcessUserSid.sidFromProcess((uint)myProcess.Id));

            sw.Stop();
            Console.WriteLine("Only fast sid: " + sw.ElapsedMilliseconds + "ms");

            sw.Reset(); sw.Start();
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));
            Console.WriteLine(ProcessUserSid.sidFromProcess(4));

            sw.Stop();
            sidTimeTotal = sw.ElapsedMilliseconds;

            Console.WriteLine($"Results\n======\nTcpTable: {tableTime}ms\nPaths: {processTimeTotal}ms\nSids: {sidTimeTotal}ms");
        }


        static void Main(string[] args)
        {
            populate();

            //var sw = new Stopwatch();
            //sw.Start();
            //var results = TcpTable.GetAllTcpConnections();
            //sw.Stop();
            //Console.WriteLine("Tcp Table took: " + sw.ElapsedMilliseconds + " ms");

            //for (int i = 0; i < results.Length; i++)
            //{
            //    var item = results[i];
            //    Console.WriteLine(
            //        $"PID\t{item.owningPid}\t" +
            //        $"\t{IpAddrUtils.UintToIP(item.localAddr,true)}:{item.LocalPort}" +
            //        $"" +
            //        $"\t{IpAddrUtils.UintToIP(item.remoteAddr,true)}:{item.RemotePort}"
            //    );
            //}

            Console.ReadLine();
        }
    }
}
