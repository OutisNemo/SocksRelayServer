using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OutisNemo.SocksRelayServer
{
    public delegate void ConnectEventHandler(object sender, IPEndPoint endPoint);

    public class Socks4Server
    {
        private readonly int _transferUnitSize;
        private readonly IPEndPoint _localEndPoint;
        private readonly Socks5Client _client;
        private readonly List<ConnectionInfo> _connections = new List<ConnectionInfo>();
        private Socket _serverSocket;
        private Thread _acceptThread;

        public event ConnectEventHandler LocalConnect;
        public event ConnectEventHandler RemoteConnect;

        public Socks4Server(IPEndPoint localEndPoint, IPEndPoint remoteProxyEndPoint, string username, string password, int transferUnitSize = 4096)
        {
            _localEndPoint = localEndPoint;
            _transferUnitSize = transferUnitSize;
            _client = new Socks5Client(remoteProxyEndPoint, username, password);
        }

        public Socks4Server(IPEndPoint localEndPoint, IPEndPoint remoteProxyEndPoint, int transferUnitSize = 4096) : 
            this(localEndPoint, remoteProxyEndPoint, string.Empty, string.Empty, transferUnitSize)
        {
            
        }

        public void Start()
        {
            SetupServerSocket();

            _acceptThread = new Thread(AcceptConnections) {IsBackground = true};
            _acceptThread.Start();
        }

        private void SetupServerSocket()
        {
            // Create the socket, bind it, and start listening
            _serverSocket = new Socket(_localEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(_localEndPoint);
            _serverSocket.Listen((int)SocketOptionName.MaxConnections);
        }

        private void AcceptConnections()
        {
            while (true)
            {
                // Accept a connection
                var connection = new ConnectionInfo();
                var socket = _serverSocket.Accept();

                connection.LocalSocket = socket;
                connection.RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Create the thread for the receives.
                connection.LocalThread = new Thread(ProcessLocalConnection) {IsBackground = true};
                connection.LocalThread.Start(connection);

                LocalConnect?.Invoke(this, (IPEndPoint)socket.RemoteEndPoint);

                // Store the socket
                lock (_connections)
                {
                    _connections.Add(connection);
                }
            }
        }

        private void ProcessLocalConnection(object state)
        {
            var connection = (ConnectionInfo)state;
            var buffer = new byte[_transferUnitSize];

            try
            {
                // we are setting up the socks!
                var bytesRead = connection.LocalSocket.Receive(buffer);
                Debug.WriteLine("LocalSocket.Receive {0}", bytesRead);

                if (bytesRead < 1)
                {
                    return;
                }

                if (buffer[0] == 0x04 && buffer[1] == 0x01)
                {
                    IPEndPoint destinationEndPoint;
                    var remotePort = (ushort) (buffer[2] << 8 | buffer[3]);
                    var ipAddressBuffer = new byte[4];
                    Buffer.BlockCopy(buffer, 4, ipAddressBuffer, 0, 4);

                    if (ipAddressBuffer[0] == 0 && ipAddressBuffer[1] == 0 && ipAddressBuffer[2] == 0 && ipAddressBuffer[3] > 0)
                    {
                        // SOCKS v4a
                        var hostBuffer = new byte[256];
                        Buffer.BlockCopy(buffer, 9, hostBuffer, 0, 100);

                        // Resolve hostname
                        var hostname = Encoding.ASCII.GetString(hostBuffer).TrimEnd('\0');
                        var destinationIp = Dns.GetHostAddresses(hostname).FirstOrDefault();
                        if (destinationIp == null)
                        {
                            throw new ConnectionException($"Cannot resolve destination hostname: {hostname}");
                        }

                        destinationEndPoint = new IPEndPoint(destinationIp, remotePort);
                    }
                    else
                    {
                        // SOCKS v4
                        destinationEndPoint = new IPEndPoint(new IPAddress(ipAddressBuffer), remotePort);
                    }

                    connection.RemoteSocket = _client.ConnectTo(destinationEndPoint);
                    if (connection.RemoteSocket.Connected)
                    {
                        RemoteConnect?.Invoke(this, destinationEndPoint);

                        var socksResponse = new byte[] {
                            0x00,
                            0x5a,
                            buffer[2], buffer[3], // port
                            buffer[4], buffer[5], buffer[6], buffer[7] // IP
                        };
                        connection.LocalSocket.Send(socksResponse);

                        // Create the thread for the receives.
                        connection.RemoteThread = new Thread(ProcessRemoteConnection) {IsBackground = true};
                        connection.RemoteThread.Start(connection);
                    } 
                    else 
                    {
                        Debug.WriteLine("Connection failed.");
                        var socksResponse = new byte[] {
                            0x00, 
                            0x5b,
                            buffer[2], buffer[3], // port
                            buffer[4], buffer[5], buffer[6], buffer[7] // IP
                        };
                        connection.LocalSocket.Send(socksResponse);

                        return;
                    }
                }

                // start receiving actual data
                while (true)
                {
                    bytesRead = connection.LocalSocket.Receive(buffer);
                    Debug.WriteLine("LocalSocket.Receive {0}", bytesRead);

                    if (bytesRead == 0) {
                        Debug.WriteLine("Local connection closed!");
                        break;
                    }

                    connection.RemoteSocket.Send(buffer, bytesRead, SocketFlags.None);
                    Debug.WriteLine("RemoteSocket.Send {0}", bytesRead);
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Socket exception: " + ex.SocketErrorCode);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            finally
            {
                Debug.WriteLine("ProcessLocalConnection Cleaning up...");

                connection.LocalSocket.Close();

                lock (_connections)
                {
                    _connections.Remove(connection);
                }
            }
        }

        private void ProcessRemoteConnection(object state)
        {
            var connection = (ConnectionInfo)state;
            var buffer = new byte[_transferUnitSize];

            try
            {
                // start receiving actual data
                while (true)
                {
                    var bytesRead = connection.RemoteSocket.Receive(buffer);
                    Debug.WriteLine("RemoteSocket.Receive {0}", bytesRead);

                    if (bytesRead == 0)
                    {
                        Debug.WriteLine("Remote connection closed!");
                        break;
                    }

                    connection.LocalSocket.Send(buffer, bytesRead, SocketFlags.None);
                    Debug.WriteLine("RemoteSocket.Send {0}", bytesRead);
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Socket exception: " + ex.SocketErrorCode);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            finally
            {
                Debug.WriteLine("ProcessRemoteConnection Cleaning up...");

                connection.RemoteSocket.Close();

                lock (_connections) _connections.Remove(connection);
            }
        }
    }
}
