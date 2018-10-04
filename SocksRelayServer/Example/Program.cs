using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SocksRelayServer;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // Do a quick test
            TestSocks5Client();

            // Start the relay server (from SOCKS v4 to SOCKS v5)
            var server = new Socks4Server(new IPEndPoint(IPAddress.Loopback, 1080), new IPEndPoint(IPAddress.Parse("192.168.0.100"), 1080));
            server.LocalConnect += server_LocalConnect;
            server.RemoteConnect += server_RemoteConnect;

            server.Start();
            Console.ReadKey();
        }

        private static void TestSocks5Client()
        {
            var client = new Socks5Client(new IPEndPoint(IPAddress.Parse("192.168.0.100"), 1080));
            var socket = client.ConnectTo("google.com", 80);
            const string strGet = "GET / HTTP/1.1\r\nHost: google.com\r\nAccept: image/gif, image/jpeg, */*\r\nAccept-Language: en-us\r\nAccept-Encoding: gzip, deflate\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)\r\n\r\n";

            socket.Send(System.Text.Encoding.ASCII.GetBytes(strGet));
            ReadResponse(socket);
        }

        private static void server_RemoteConnect(object sender, IPEndPoint iep)
        {
            Console.WriteLine($"RemoteConnect: {iep.ToString()}");
        }

        private static void server_LocalConnect(object sender, IPEndPoint iep)
        {
            var socks4Server = (Socks4Server)sender;
            Console.WriteLine($"LocalConnect: {iep}");
        }

        private static void ReadResponse(Socket socket)
        {
            var flag = true; // just so we know we are still reading
            var headerString = string.Empty; // to store header information
            var bodyBuff = new byte[0]; // to later hold the body content

            while (flag)
            {
                // read the header byte by byte, until \r\n\r\n
                var buffer = new byte[1];
                socket.Receive(buffer, 0, 1, 0);
                headerString += Encoding.ASCII.GetString(buffer);
                var contentLength = 0; // the body length

                if (!headerString.Contains("\r\n\r\n")) continue;
                var headers = headerString.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var header in headers)
                {
                    var keyValues = header.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (keyValues[0].Trim().ToLowerInvariant().Equals("content-length"))
                    {
                        contentLength = int.Parse(keyValues[1]);
                        break;
                    }
                }

                flag = false;
                // read the body
                bodyBuff = new byte[contentLength];
                socket.Receive(bodyBuff, 0, contentLength, 0);
            }

            Console.WriteLine("Server Response :");
            Console.WriteLine(Encoding.ASCII.GetString(bodyBuff));
        }
    }
}
