using Socket2Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LimitedChromeManager
{
    public class OneTimeHTTPRequest
    {
        public TimeSpan AcceptTimeout = TimeSpan.FromMinutes(2);
        // Including read write times:
        public TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(5);
        public TimeSpan TotalResponseTimeout = TimeSpan.FromSeconds(5);
        public TimeSpan SleepInterval = TimeSpan.FromMilliseconds(200);

        public byte[] DataToServe = Encoding.ASCII.GetBytes("Sample Text");
        public string DataContentType = "text/plain";

        public byte[] RequestBuffer = new byte[1024 * 500]; // 500KB default

        public void StartListener(IPAddress ip, int port)
        {
            TcpListener server = new TcpListener(ip,port);
            server.Start();

            // Timeout for accepting client -> Just check if pending (https://stackoverflow.com/a/3315200)
            int acceptTimePassedMS = 0;
            while (!server.Pending() && acceptTimePassedMS < AcceptTimeout.TotalMilliseconds)
            {
                int timeToSleepMS = (int)SleepInterval.TotalMilliseconds;
                Thread.Sleep(timeToSleepMS);
                acceptTimePassedMS += timeToSleepMS;
            }

            if (acceptTimePassedMS >= AcceptTimeout.TotalMilliseconds)
            {
                throw new Exception("Accept socket timeout");
            }
            else
            {
                TcpClient client = server.AcceptTcpClient();
                client.ReceiveTimeout = (int)TotalRequestTimeout.TotalMilliseconds;
                client.SendTimeout = (int)TotalResponseTimeout.TotalMilliseconds;

                NetworkStream ns = client.GetStream();
                if (!ns.CanTimeout)
                    throw new Exception("Networkstream can't timeout!");
                ns.ReadTimeout = client.ReceiveTimeout;
                ns.WriteTimeout = client.SendTimeout;

                int bytesRecieved = 0;
                Stopwatch recieveTimer = new Stopwatch();
                recieveTimer.Start();
                while (recieveTimer.ElapsedMilliseconds < ns.ReadTimeout && ns.DataAvailable)
                {
                    bytesRecieved += ns.Read(RequestBuffer, bytesRecieved, 1024);
                }
                recieveTimer.Stop();

                string requestData = Encoding.ASCII.GetString(RequestBuffer,0, bytesRecieved);
                if (!requestData.EndsWith("\r\n\r\n"))
                {
                    throw new Exception("Error getting a valid HTTP request");
                }
                else
                {
                    string ResponseText = @"HTTP/1.1 200 OK
Access-Control-Allow-Origin: *
Content-Type: {0}
Content-Length: {1}
Connection: Closed";

                    int pid = pidFromConnection(client);
                    if (pid < 0)
                    {
                        throw new Exception("Can't find token req PID owner");
                    }

                    LocalGroupsAndUsers users = new LocalGroupsAndUsers();
                    string processPath = ProcessPath.GetProcessPath((uint)pid);
                    string userSid = ProcessUserSid.sidFromProcess((uint)pid, (s) => { });
                    string userName = users.getUserName(userSid);
                    if (processPath == "" || userName == "" || userSid == "")
                    {
                        throw new Exception("Error getting process information");
                    }

                    if (
                        (
                            Properties.Settings.Default.AllowedUserSids.ToLower().Contains(userSid.ToLower()) 
                            || 
                            Properties.Settings.Default.AllowedUserNames.ToLower().Contains(userName.ToLower())
                        )
                        &&
                            Properties.Settings.Default.AllowedProcessesPaths.ToLower().Contains(processPath.ToLower())
                        )
                    {
                        ResponseText += "\r\n\r\n" + Encoding.ASCII.GetString(DataToServe);
                        ResponseText = ResponseText.Replace("{0}", DataContentType).Replace("{1}", DataToServe.Length.ToString());
                        byte[] responseBuffer = Encoding.ASCII.GetBytes(ResponseText);

                        Stopwatch sendTimer = new Stopwatch();
                        sendTimer.Start();
                        ns.Write(responseBuffer, 0, responseBuffer.Length);
                        sendTimer.Stop();

                        if (sendTimer.ElapsedMilliseconds >= ns.WriteTimeout)
                        {
                            throw new Exception("Error sending response, got timeout.");
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format(
                            "Token acced denied problem!\nProcess: {0}\nUserSid: {1}\nUserName: {2}",
                                processPath, userSid, userName
                            ));
                    }

                    
                }

                ns.Close();
                client.Close();
                server.Stop();
            }
            
        }


        public static bool IsLocalIpAddress(string host)
        {
            https://www.csharp-examples.net/local-ip/?ref=driverlayer.com/web
            try
            { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static int pidFromConnection(TcpClient client)
        {
            int result = -1;
            try
            {
                IPEndPoint server_ep = (IPEndPoint)client.Client.LocalEndPoint;
                IPEndPoint client_ep = (IPEndPoint)client.Client.RemoteEndPoint;
                if (IsLocalIpAddress(client_ep.Address.ToString())) {
                    var table = TcpTable.GetAllTcpConnections();
                    var searchResults = table.Where((conn) =>
                        conn.localAddr == conn.remoteAddr && // Process connection is local
                        conn.RemotePort == server_ep.Port && // Process connected to use
                        conn.LocalPort == client_ep.Port // Same assinged temp port when accepted
                        );
                    if (searchResults.Count() == 1)
                    {
                        var connection = searchResults.First();
                        result = connection.owningPid;
                    }                                        
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }
    }
}
