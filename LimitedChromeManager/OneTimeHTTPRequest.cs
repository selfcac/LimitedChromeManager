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
        public TimeSpan AcceptTimeout = TimeSpan.FromSeconds(10);
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
Content-Type: {0}
Content-Length: {1}
Connection: Closed";
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

                ns.Close();
                client.Close();
                server.Stop();
            }
            
        }
    }
}
