using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SocksRelayServer.Dns;
using SocksRelayServer.Exception;

namespace SocksRelayServer
{
    public class SocksRelayServer : ISocksRelayServer
    {
        private readonly List<ConnectionInfo> _connections;
        private Socket _serverSocket;
        private Thread _acceptThread;
        private bool _serverStarted;

        public event EventHandler<IPEndPoint> OnLocalConnect;
        public event EventHandler<IPEndPoint> OnRemoteConnect;
        public event EventHandler<string> OnLogMessage;

        public IDnsResolver DnsResolver { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int BufferSize { get; set; }
        public bool ResolveHostnamesRemotely { get; set; }
        public IPEndPoint LocalEndPoint { get; }
        public IPEndPoint RemotEndPoint { get; }
        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }

        public SocksRelayServer(IPEndPoint localEndPoint, IPEndPoint remoteProxyEndPoint)
        {
            if (Equals(localEndPoint, remoteProxyEndPoint))
            {
                throw new SocksRelayServerException("LocalEndPoint and RemoteEndPoint cannot be the same");
            }

            BufferSize = 4096;
            LocalEndPoint = localEndPoint;
            RemotEndPoint = remoteProxyEndPoint;
            SendTimeout = 0;
            ReceiveTimeout = 0;
            
            ResolveHostnamesRemotely = false;
            DnsResolver = new DefaultDnsResolver();

            _connections = new List<ConnectionInfo>();
        }

        public void Start()
        {
            if (!ResolveHostnamesRemotely && DnsResolver == null)
            {
                throw new SocksRelayServerException("DnsResolver property cannot be null when using Local DNS resolution");
            }

            SetupServerSocket();

            _serverStarted = true;
            _acceptThread = new Thread(AcceptConnections) {IsBackground = true};
            _acceptThread.Start();
        }

        public void Stop()
        {
            _serverStarted = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private void SetupServerSocket()
        {
            // Create the socket, bind it, and start listening
            _serverSocket = new Socket(LocalEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(LocalEndPoint);
            _serverSocket.Listen((int)SocketOptionName.MaxConnections);
        }

        private void AcceptConnections()
        {
            while (_serverStarted)
            {
                // Accept a connection
                var connection = new ConnectionInfo();

                var socket = _serverSocket.Accept();
                socket.ReceiveTimeout = ReceiveTimeout;
                socket.SendTimeout = SendTimeout;

                connection.LocalSocket = socket;
                connection.RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = SendTimeout,
                    ReceiveTimeout = ReceiveTimeout
                };

                // Create the thread for the receive
                connection.LocalThread = new Thread(ProcessLocalConnection)
                {
                    IsBackground = true
                };

                connection.LocalThread.Start(connection);
                OnLocalConnect?.Invoke(this, (IPEndPoint)socket.RemoteEndPoint);

                // Store the socket
                lock (_connections)
                {
                    _connections.Add(connection);
                }
            }

            _serverSocket.Close();
        }

        private void ProcessLocalConnection(object state)
        {
            var connection = (ConnectionInfo)state;
            var buffer = new byte[BufferSize];
            int bytesRead;

            try
            {
                bytesRead = connection.LocalSocket.Receive(buffer);
                if (bytesRead < 1 || buffer[0] != Protocol.Socks4.Version)
                {
                    connection.LocalSocket.Close();
                    return;
                }

                OnLogMessage?.Invoke(this, $"LocalSocket.Receive {bytesRead}");
            }
            catch (SocketException ex)
            {
                OnLogMessage?.Invoke(this, $"Caught SocketException in ProcessLocalConnection with error code {ex.SocketErrorCode.ToString()}");
            }

            try
            {
                switch (buffer[1])
                {
                    case Protocol.Socks4.CommandStreamConnection:
                        {
                            var portBuffer = new[] { buffer[2], buffer[3] };
                            var port = (ushort)(portBuffer[0] << 8 | portBuffer[1]);

                            var ipBuffer = new[] { buffer[4], buffer[5], buffer[6], buffer[7] };
                            var ip = new IPAddress(ipBuffer);

                            var destinationEndPoint = new IPEndPoint(ip, port);
                            if (IsSocks4AProtocol(ipBuffer))
                            {
                                var hostBuffer = new byte[256];
                                Buffer.BlockCopy(buffer, 9, hostBuffer, 0, 100);

                                // Resolve hostname, fallback to remote proxy dns resolution
                                var hostname = Encoding.ASCII.GetString(hostBuffer).TrimEnd((char)0);
                                var destinationIp = ResolveHostnamesRemotely ? null : DnsResolver.TryResolve(hostname);

                                connection.RemoteSocket = Socks5Client.Connect(
                                    RemotEndPoint.Address.ToString(),
                                    RemotEndPoint.Port, destinationIp == null ? hostname : destinationIp.ToString(),
                                    port,
                                    Username,
                                    Password,
                                    SendTimeout,
                                    ReceiveTimeout
                                );

                                OnRemoteConnect?.Invoke(this, destinationEndPoint);
                            }
                            else
                            {
                                destinationEndPoint = new IPEndPoint(new IPAddress(ipBuffer), port);
                                connection.RemoteSocket = Socks5Client.Connect(
                                    RemotEndPoint.Address.ToString(),
                                    RemotEndPoint.Port,
                                    destinationEndPoint.Address.ToString(),
                                    port,
                                    Username,
                                    Password,
                                    SendTimeout,
                                    ReceiveTimeout
                                );

                                OnRemoteConnect?.Invoke(this, destinationEndPoint);
                            }

                            if (connection.RemoteSocket.Connected)
                            {
                                SendSocks4Reply(connection.LocalSocket, Protocol.Socks4.StatusRequestGranted, ipBuffer, portBuffer);

                                // Create the thread for the receives.
                                connection.RemoteThread = new Thread(ProcessRemoteConnection) { IsBackground = true };
                                connection.RemoteThread.Start(connection);
                            }
                            else
                            {
                                OnLogMessage?.Invoke(this, "RemoteSocket connection failed");
                                SendSocks4Reply(connection.LocalSocket, Protocol.Socks4.StatusRequestFailed, ipBuffer, portBuffer);
                                connection.LocalSocket.Close();
                            }

                            break;
                        }
                    case Protocol.Socks4.CommandBindingConnection:
                        {
                            var portBuffer = new[] { buffer[2], buffer[3] };
                            var ipBuffer = new[] { buffer[4], buffer[5], buffer[6], buffer[7] };

                            // TCP/IP port binding not supported
                            SendSocks4Reply(connection.LocalSocket, Protocol.Socks4.StatusRequestFailed, ipBuffer, portBuffer);
                            connection.LocalSocket.Close();
                            break;
                        }

                    default:
                        OnLogMessage?.Invoke(this, "Unknown protocol on LocalSocket");
                        connection.LocalSocket.Close();
                        break;
                }

                // start receiving actual data if the socket still open
                while (true)
                {
                    if (!connection.LocalSocket.Connected || !connection.RemoteSocket.Connected)
                    {
                        break;
                    }

                    bytesRead = connection.LocalSocket.Receive(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    if (connection.RemoteSocket.Connected)
                    {
                        connection.RemoteSocket.Send(buffer, bytesRead, SocketFlags.None);
                        OnLogMessage?.Invoke(this, $"Forwarded {bytesRead} bytes from LocalSocket to RemoteSocket");
                    }
                }
            }
            catch (SocketException ex)
            {
                OnLogMessage?.Invoke(this, $"Caught SocketException in ProcessLocalConnection with error code {ex.SocketErrorCode.ToString()}");
            }
            catch (Socks5Exception ex)
            {
                var portBuffer = new[] { buffer[2], buffer[3] };
                var ipBuffer = new[] { buffer[4], buffer[5], buffer[6], buffer[7] };

                SendSocks4Reply(connection.LocalSocket, Protocol.Socks4.StatusRequestFailed, ipBuffer, portBuffer);
                connection.LocalSocket.Close();

                OnLogMessage?.Invoke(this, $"Caught Socks5Exception in ProcessLocalConnection with message {ex.Message}");
            }

            if (connection.LocalSocket.Connected)
            {
                OnLogMessage?.Invoke(this, "Closing LocalSocket");
                connection.LocalSocket.Close();
            }

            lock (_connections)
            {
                _connections.Remove(connection);
            }
        }

        private void ProcessRemoteConnection(object state)
        {
            var connection = (ConnectionInfo)state;
            var buffer = new byte[BufferSize];

            try
            {
                // start receiving actual data
                while (true)
                {
                    if (!connection.LocalSocket.Connected || !connection.RemoteSocket.Connected)
                    {
                        break;
                    }

                    var bytesRead = connection.RemoteSocket.Receive(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    if (connection.LocalSocket.Connected)
                    {
                        connection.LocalSocket.Send(buffer, bytesRead, SocketFlags.None);
                        OnLogMessage?.Invoke(this, $"Forwarded {bytesRead} bytes from RemoteSocket to LocalSocket");
                    }
                }
            }
            catch (SocketException ex)
            {
                OnLogMessage?.Invoke(this, $"Caught SocketException in ProcessRemoteConnection with error code {ex.SocketErrorCode.ToString()}");
            }

            if (connection.RemoteSocket.Connected)
            {
                OnLogMessage?.Invoke(this, "Closing RemoteSocket");
                connection.RemoteSocket.Close();
            }
        }

        private static void SendSocks4Reply(Socket socket, byte statusCode, IReadOnlyList<byte> ipAddress, IReadOnlyList<byte> portNumber)
        {
            var response = new byte[] {
                0x00,
                statusCode,
                portNumber[0], portNumber[1],
                ipAddress[0], ipAddress[1], ipAddress[2], ipAddress[3]
            };

            socket.Send(response);
        }

        private static bool IsSocks4AProtocol(IReadOnlyList<byte> ip)
        {
            return ip[0] == 0 && ip[1] == 0 && ip[2] == 0 && ip[3] > 0;
        }
    }
}
