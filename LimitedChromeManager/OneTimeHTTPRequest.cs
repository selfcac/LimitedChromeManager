using Socket2Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LimitedChromeManager
{
    public class OneTimeHTTPRequest
    {
        //  Class types:
        // =================================================

        public enum HTTPResultEnum
        {
            NOTOKEN_ERROR, // Error in protocols, networks etc
            TOKEN_AUTH_ERROR, // Token requirement not met
            SUCCESS
        }

        public class HTTPTaskResult : ResultObject<SimpleWrapper<bool>, HTTPResultEnum> // Only for short lines
        {
            public static new HTTPTaskResult 
                Fail(HTTPResultEnum statusCode, string desc, SimpleWrapper<bool> resultObj = default, Exception error = null)
            {
                HTTPTaskResult result = new HTTPTaskResult()
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    description = desc,
                    Error = error
                };
                return result;
            }

            public static new HTTPTaskResult
                Success(HTTPResultEnum statusCode, string desc, SimpleWrapper<bool> resultObj = default, Exception error = null)
            {
                HTTPTaskResult result = new HTTPTaskResult()
                {
                    IsSuccess = true,
                    StatusCode = statusCode,
                    description = desc,
                    Result = resultObj
                };
                return result;
            }
        }

        private const string HTTPHeadersEnd = "\r\n\r\n";

        //  Class config:
        // =================================================

        public TimeSpan AcceptTimeout = TimeSpan.FromMinutes(2);
        // Including read write times:
        public TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(5);
        public TimeSpan TotalResponseTimeout = TimeSpan.FromSeconds(5);
        public TimeSpan SleepInterval = TimeSpan.FromMilliseconds(200);

        public string[] findInRequest = { };
        public byte[] DataToServe = Encoding.ASCII.GetBytes("Sample Text");
        public string DataContentType = "text/plain";

        public int BufferSize = 1024 * 500; // 500KB default
        

        //  Class Functions:
        // =================================================


        public HTTPTaskResult StartListener(IPAddress ip, int port, Func<bool> isCancelled)
        {
            HTTPTaskResult result
                = HTTPTaskResult.Fail(HTTPResultEnum.NOTOKEN_ERROR, "init");

            TcpListener tcpServer = new TcpListener(ip, port);
            tcpServer.Start();

            // Timeout for accepting client -> Just check if pending (https://stackoverflow.com/a/3315200)
            int acceptTimePassedMS = 0;
            acceptTimePassedMS = WaitForTcpClient(isCancelled, tcpServer, acceptTimePassedMS);

            if (acceptTimePassedMS >= AcceptTimeout.TotalMilliseconds || isCancelled())
            {
                result = HTTPTaskResult
                    .Fail(HTTPResultEnum.NOTOKEN_ERROR, 
                        "Accept socket timeout", resultObj: isCancelled());
            }
            else
            {
                TcpClient client = tcpServer.AcceptTcpClient();
                client.ReceiveTimeout = (int)TotalRequestTimeout.TotalMilliseconds;
                client.SendTimeout = (int)TotalResponseTimeout.TotalMilliseconds;

                NetworkStream ns = client.GetStream();
                try
                {
                    if (!ns.CanTimeout)
                        result = HTTPTaskResult
                            .Fail(HTTPResultEnum.NOTOKEN_ERROR, 
                                "Networkstream can't timeout!", resultObj: isCancelled());
                    else
                    {
                        ns.ReadTimeout = client.ReceiveTimeout;
                        ns.WriteTimeout = client.SendTimeout;

                        int bytesRecieved = 0;
                        byte[] RequestBuffer = new byte[BufferSize];
                        string requestData = RecieveHTTPRequest(isCancelled, ns, ref bytesRecieved, RequestBuffer);

                        if (!requestData.EndsWith(HTTPHeadersEnd))
                        {
                            result = HTTPTaskResult
                                        .Fail(HTTPResultEnum.NOTOKEN_ERROR,
                                            "Error getting a valid HTTP request", resultObj: isCancelled());
                        }
                        else
                        {
                            bool validReq = true;
                            foreach (string item in findInRequest)
                            {
                                if (!requestData.Contains(item.ToLower()))
                                {
                                    validReq = false;
                                    break;
                                }
                            }

                            if (!validReq)
                            {
                                // Because we also check Agent header here, so from this point it is all security
                                result = HTTPTaskResult
                                    .Fail(HTTPResultEnum.TOKEN_AUTH_ERROR, 
                                        "Didn't find all required text in request:\n" + requestData, resultObj: isCancelled());
                            }
                            else
                            {
                                int pid = pidFromConnection(client);
                                if (pid < 0)
                                {
                                    result = HTTPTaskResult
                                        .Fail(HTTPResultEnum.TOKEN_AUTH_ERROR,
                                            "Can't find token req PID owner", resultObj: isCancelled());
                                }
                                else
                                {

                                    LocalGroupsAndUsers users = new LocalGroupsAndUsers();
                                    string processPath = ProcessPath.GetProcessPath((uint)pid);
                                    string userSid = ProcessUserSid.sidFromProcess((uint)pid, (s) => { });
                                    string userName = users.getUserName(userSid);

                                    if (processPath == "" || userName == "" || userSid == "")
                                    {
                                        result = HTTPTaskResult
                                            .Fail(HTTPResultEnum.TOKEN_AUTH_ERROR,
                                                "Error getting process owner information", resultObj: isCancelled());
                                    }
                                    else
                                    {

                                        Properties.Settings config = Properties.Settings.Default;
                                        bool isCallerAllowed =
                                            config.AllowedClientUsernames.ToLower().Contains(userName.ToLower())
                                            && config.AllowedClientPaths.ToLower().Contains(processPath.ToLower())
                                            && !isCancelled();

                                        if (!isCallerAllowed)
                                        {
                                            result = HTTPTaskResult
                                               .Fail(HTTPResultEnum.TOKEN_AUTH_ERROR, 
                                                   string.Format(
                                                        "Token acces denied problem!\nProcess: {0}\nUserSid: {1}\nUserName: {2}",
                                                            processPath, userSid, userName
                                                    ),
                                                   resultObj: isCancelled());
                                        }
                                        else
                                        {
                                            byte[] responseBuffer = CreateTokenHTTPResponse(DataToServe);

                                            Stopwatch sendTimer = new Stopwatch();
                                            sendTimer.Start();
                                            ns.Write(responseBuffer, 0, responseBuffer.Length);
                                            sendTimer.Stop();

                                            if (sendTimer.ElapsedMilliseconds >= ns.WriteTimeout)
                                            {
                                                // Because maybe he got the token and stopped responding (reading) => TOKEN_AUTH_ERROR
                                                result = HTTPTaskResult
                                                    .Fail(HTTPResultEnum.TOKEN_AUTH_ERROR,
                                                        "Error sending response, got timeout.", resultObj: isCancelled());
                                            }
                                            else
                                            {
                                                result = HTTPTaskResult.Success(HTTPResultEnum.SUCCESS, "Token sent!");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = HTTPTaskResult
                                .Fail(HTTPResultEnum.TOKEN_AUTH_ERROR,
                                    "Error in token process", resultObj: isCancelled(), error: ex);
                }
                finally
                {
                    ns.Close();
                    client.Close();
                    tcpServer.Stop();
                }
            }
            return result;
        }

        private byte[] CreateTokenHTTPResponse(byte[] DataToServe)
        {
            string HTTPResponseTemplate = string.Join("\r\n", new[]
                                                        {
                                                "HTTP/1.1 200 OK",
                                                "Access-Control-Allow-Origin: *",
                                                "Content-Type: {0}",
                                                "Content-Length: {1}",
                                                "Connection: Closed"
                                            });
            HTTPResponseTemplate += HTTPHeadersEnd + Encoding.ASCII.GetString(DataToServe);
            HTTPResponseTemplate = HTTPResponseTemplate
                .Replace("{0}", DataContentType)
                .Replace("{1}", DataToServe.Length.ToString());
            byte[] responseBuffer = Encoding.ASCII.GetBytes(HTTPResponseTemplate);
            return responseBuffer;
        }

        private static string RecieveHTTPRequest(Func<bool> isCancelled, NetworkStream ns, ref int bytesRecieved, byte[] RequestBuffer)
        {
            Stopwatch recieveTimer = new Stopwatch();
            recieveTimer.Start();
            while (recieveTimer.ElapsedMilliseconds < ns.ReadTimeout && ns.DataAvailable && !isCancelled())
            {
                bytesRecieved += ns.Read(RequestBuffer, bytesRecieved, 1024);
            }
            recieveTimer.Stop();

            string requestData = Encoding.ASCII.GetString(RequestBuffer, 0, bytesRecieved).ToLower();
            return requestData;
        }

        private int WaitForTcpClient(Func<bool> isCancelled, TcpListener server, int acceptTimePassedMS)
        {
            while (!server.Pending() && acceptTimePassedMS < AcceptTimeout.TotalMilliseconds && !isCancelled())
            {
                int timeToSleepMS = (int)SleepInterval.TotalMilliseconds;
                Thread.Sleep(timeToSleepMS);
                acceptTimePassedMS += timeToSleepMS;
            }

            return acceptTimePassedMS;
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
