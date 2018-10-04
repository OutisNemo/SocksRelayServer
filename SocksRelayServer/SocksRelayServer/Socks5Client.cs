using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocksRelayServer
{
    public class Socks5Client
    {
        private readonly IPEndPoint _proxyEndPoint;
        private readonly string _username;
        private readonly string _password;

        public Socks5Client(IPEndPoint proxyEndPoint, string username, string password)
        {
            _proxyEndPoint = proxyEndPoint;
            _username = username;
            _password = password;
        }

        public Socks5Client(string proxyAddress, ushort proxyPort, string username, string password)
        {
            var proxyIp = Dns.GetHostAddresses(proxyAddress).FirstOrDefault();
            if (proxyIp == null)
            {
                throw new ConnectionException("Cannot resolve proxy address");
            }

            _proxyEndPoint = new IPEndPoint(proxyIp, proxyPort);
            _username = username;
            _password = password;
        }

        public Socks5Client(IPEndPoint proxyEndPoint) : this(proxyEndPoint, string.Empty, string.Empty)
        {
            
        }

        public Socks5Client(string proxyAddress, ushort proxyPort) : this(proxyAddress, proxyPort, string.Empty, string.Empty)
        {

        }

        public Socket ConnectTo(string destinationHost, ushort destinationPort)
        {
            var socket = InitializeSocket();

            var request = new byte[257];
            var response = new byte[257];
            ushort nIndex = 0;

            request[nIndex++] = 0x05; // version 5.
            request[nIndex++] = 0x01; // command = connect.
            request[nIndex++] = 0x00; // Reserve = must be 0x00

            var rawBytes = Encoding.Default.GetBytes(destinationHost);
            request[nIndex++] = 0x03; // Address is full-qualified domain name.
            request[nIndex++] = Convert.ToByte(destinationHost.Length); // length of address.
            rawBytes.CopyTo(request, nIndex);
            nIndex += (ushort)rawBytes.Length;

            // using big-edian byte order
            var portBytes = BitConverter.GetBytes(destinationPort);
            for (var i = portBytes.Length - 1; i >= 0; i--)
            {
                request[nIndex++] = portBytes[i];
            }

            // send connect request.
            socket.Send(request, nIndex, SocketFlags.None);
            socket.Receive(response); // Get variable length response...
            if (response[1] != 0x00)
            {
                throw new ConnectionException("Unknown error during SOCKS handshake");
            }

            // Success
            return socket;
        }

        public Socket ConnectTo(IPEndPoint destinationEndPoint)
        {
            var socket = InitializeSocket();

            var request = new byte[257];
            var response = new byte[257];
            byte[] rawBytes;
            ushort nIndex = 0;

            request[nIndex++] = 0x05; // version 5.
            request[nIndex++] = 0x01; // command = connect.
            request[nIndex++] = 0x00; // Reserve = must be 0x00

            switch (destinationEndPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    request[nIndex++] = 0x01;
                    rawBytes = destinationEndPoint.Address.GetAddressBytes();
                    rawBytes.CopyTo(request, nIndex);
                    nIndex += (ushort)rawBytes.Length;
                    break;
                case AddressFamily.InterNetworkV6:
                    request[nIndex++] = 0x04;
                    rawBytes = destinationEndPoint.Address.GetAddressBytes();
                    rawBytes.CopyTo(request, nIndex);
                    nIndex += (ushort)rawBytes.Length;
                    break;
                default:
                    throw new ConnectionException("Unknown AddressFamily of destination endpoint");
            }

            // using big-edian byte order
            var portBytes = BitConverter.GetBytes((ushort) destinationEndPoint.Port);
            for (var i = portBytes.Length - 1; i >= 0; i--)
            {
                request[nIndex++] = portBytes[i];
            }
                
            // send connect request.
            socket.Send(request, nIndex, SocketFlags.None);
            socket.Receive(response); // Get variable length response...
            if (response[1] != 0x00)
            {
                throw new ConnectionException("Unknown error during SOCKS handshake");
            }
                
            // Success
            return socket;
        }

        private Socket InitializeSocket()
        {
            var request = new byte[257];
            var response = new byte[257];

            // open a TCP connection to SOCKS5 server
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_proxyEndPoint);

            ushort nIndex = 0;
            request[nIndex++] = 0x05; // Version 5.

            if (!string.IsNullOrEmpty(_username))
            {
                request[nIndex++] = 0x02; // 2 authentication methods
                request[nIndex++] = 0x02; // USERNAME/PASSWORD
                request[nIndex++] = 0x00; // NO AUTHENTICATION REQUIRED
            }
            else
            {
                request[nIndex++] = 0x01; // 1 authentication method
                request[nIndex++] = 0x00; // NO AUTHENTICATION REQUIRED
            }

            socket.Send(request, nIndex, SocketFlags.None);

            var nGot = socket.Receive(response, 2, SocketFlags.None);
            if (nGot != 2)
            {
                throw new ConnectionException("Invalid response received from proxy server");
            }

            if (response[0] != 0x05)
            {
                socket.Close();
                throw new ConnectionException("Invalid response received from proxy server, maybe its not a SOCKS5 proxy?");
            }

            if (response[1] == 0xFF)
            {
                // No authentication method was accepted close the socket.
                socket.Close();
                throw new ConnectionException("None of the authentication method was accepted by proxy server");
            }

            if (response[1] == 0x02)
            {
                if (string.IsNullOrEmpty(_username) || _password == null)
                {
                    throw new ConnectionException("Server requires authentication, but no credentials were provided");
                }

                //Username/Password Authentication protocol
                nIndex = 0;
                request[nIndex++] = 0x01; // Version 5.

                // add user name
                request[nIndex++] = (byte) _username.Length;
                var rawBytes = Encoding.Default.GetBytes(_username);
                rawBytes.CopyTo(request, nIndex);
                nIndex += (ushort) rawBytes.Length;

                // add password
                request[nIndex++] = (byte) _password.Length;
                rawBytes = Encoding.Default.GetBytes(_password);
                rawBytes.CopyTo(request, nIndex);
                nIndex += (ushort) rawBytes.Length;

                // Send the Username/Password request
                socket.Send(request, nIndex, SocketFlags.None);

                // Receive 2 byte response...
                nGot = socket.Receive(response, 2, SocketFlags.None);
                if (nGot != 2)
                {
                    throw new ConnectionException("Invalid response received from proxy server");
                }

                if (response[1] != 0x00)
                {
                    throw new ConnectionException("Invalid username or password");
                }
            }

            // This version only supports connect command. 
            // UDP and Bind are not supported.

            return socket;
        }
    }
}