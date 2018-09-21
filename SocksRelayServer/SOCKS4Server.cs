using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocksRelayServer
{
    class ConnectionInfo
    {
        public Socket LocalSocket;
        public Thread LocalThread;
        public Socket RemoteSocket;
        public Thread RemoteThread;
    }

    public delegate void ConnectEventHandler(object sender, IPEndPoint iep);
    public delegate void ConnectionLogHandler(object sender, int code, string message);

    class Socks4Server
    {
        private Socket _serverSocket;
        private readonly int _port;
        private readonly int _transferUnitSize;
        private Thread _acceptThread;
        private List<ConnectionInfo> _connections =
            new List<ConnectionInfo>();

        public event ConnectEventHandler LocalConnect;
        public event ConnectEventHandler RemoteConnect;

        public Socks4Server(int port, int transferUnitSize) 
        { 
            _port = port;
            _transferUnitSize = transferUnitSize;
        }

        public void Start()
        {
            SetupServerSocket();

            _acceptThread = new Thread(AcceptConnections);
            _acceptThread.IsBackground = true;
            _acceptThread.Start();
        }

        private void SetupServerSocket()
        {
            IPEndPoint myEndpoint = new IPEndPoint(IPAddress.Loopback, 
                _port);

            // Create the socket, bind it, and start listening
            _serverSocket = new Socket(myEndpoint.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(myEndpoint);
            _serverSocket.Listen((int)SocketOptionName.MaxConnections);
        }

        private void AcceptConnections()
        {
            while (true)
            {
                // Accept a connection
                ConnectionInfo connection = new ConnectionInfo();

                Socket socket = _serverSocket.Accept();

                connection.LocalSocket = socket;
                connection.RemoteSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Create the thread for the receives.
                connection.LocalThread = new Thread(ProcessLocalConnection);
                connection.LocalThread.IsBackground = true;
                connection.LocalThread.Start(connection);

                LocalConnect?.Invoke(this, (IPEndPoint)socket.RemoteEndPoint);

                // Store the socket
                lock (_connections) _connections.Add(connection);
            }
        }

        private IPEndPoint _remoteEndPoint;
        private void ProcessLocalConnection(object state)
        {
            ConnectionInfo connection = (ConnectionInfo)state;
            int bytesRead = 0;

            byte[] buffer = new byte[_transferUnitSize];
            try
            {
                // we are setting up the socks!
                bytesRead = connection.LocalSocket.Receive(buffer);

                Console.WriteLine("ProcessLocalConnection::Receive bytesRead={0}", bytesRead);
                DumpBytes(buffer, bytesRead);

                if (bytesRead > 0)
                {
                    if (buffer[0] == 0x04 && buffer[1] == 0x01)
                    {
                        int remotePort = buffer[2] << 8 | buffer[3];
                        byte[] ipAddressBuffer = new byte[4];
                        Buffer.BlockCopy(buffer, 4, ipAddressBuffer, 0, 4);
                        
                        IPEndPoint remoteEndPoint = new IPEndPoint(new IPAddress(ipAddressBuffer), remotePort);
                        _remoteEndPoint = remoteEndPoint;

                        connection.RemoteSocket = SocksProxy.ConnectToSocks5Proxy(
                            "127.0.0.1", 9951, _remoteEndPoint.Address.ToString(),
                            (ushort) remotePort, string.Empty, string.Empty);

                        if (connection.RemoteSocket.Connected)
                        {
                            Console.WriteLine("Connected to remote: {0}", _remoteEndPoint);

                            RemoteConnect?.Invoke(this, remoteEndPoint);

                            byte[] socksResponse = new byte[] {
                                0x00, 0x5a,
                                buffer[2], buffer[3], // port
                                buffer[4], buffer[5], buffer[6], buffer[7] // IP
                            };
                            connection.LocalSocket.Send(socksResponse);

                            // Create the thread for the receives.
                            connection.RemoteThread = new Thread(ProcessRemoteConnection);
                            connection.RemoteThread.IsBackground = true;
                            connection.RemoteThread.Start(connection);
                        } 
                        else 
                        {
                            Console.WriteLine("Connection failed.");
                            byte[] socksResponse = new byte[] {
                                0x00, 
                                0x5b,
                                buffer[2], buffer[3], // port
                                buffer[4], buffer[5], buffer[6], buffer[7] // IP
                            };
                            connection.LocalSocket.Send(socksResponse);
                            return;

                        }
                    }
                }
                else if (bytesRead == 0) return;

                // start receiving actual data
                while (true)
                {
                    bytesRead = connection.LocalSocket.Receive(buffer);
                    Console.WriteLine("LocalSocket.Receive {0}", bytesRead);
                    Console.WriteLine(Encoding.Unicode.GetString(buffer));
                    DumpBytes(buffer, bytesRead);
                    if (bytesRead == 0) {
                        Console.WriteLine("Local connection closed!");
                        break;
                    } else {
                        connection.RemoteSocket.Send(buffer, bytesRead, SocketFlags.None);
                        Console.WriteLine("RemoteSocket.Send {0}", bytesRead);
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception: " + exc);
            }
            finally
            {
                Console.WriteLine("ProcessLocalConnection Cleaning up...");
                connection.LocalSocket.Close();
                //connection.RemoteSocket.Close();
                lock (_connections) _connections.Remove(connection);
            }
        }

        private void ProcessRemoteConnection(object state)
        {
            ConnectionInfo connection = (ConnectionInfo)state;
            int bytesRead = 0;

            byte[] buffer = new byte[_transferUnitSize];
            try
            {
                // start receiving actual data
                while (true)
                {
                    bytesRead = connection.RemoteSocket.Receive(buffer);
                    Console.WriteLine("RemoteSocket.Receive {0}", bytesRead);
                    if (bytesRead == 0) {
                        Console.WriteLine("Remote connection closed!");
                        break;
                    } else {
                        connection.LocalSocket.Send(buffer, bytesRead, SocketFlags.None);
                        Console.WriteLine("RemoteSocket.Send {0}", bytesRead);
                    }
                }
            }
            catch (SocketException exc)
            {
                Console.WriteLine("Socket exception: " + exc.SocketErrorCode);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception: " + exc);
            }
            finally
            {
                Console.WriteLine("ProcessRemoteConnection Cleaning up...");
                //connection.LocalSocket.Close();
                connection.RemoteSocket.Close();
                lock (_connections) 
                    _connections.Remove(connection);
            }
        }

        private void DumpBytes(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++) Console.Write("{0:X2} ", buffer[i]);
            Console.Write("\n");
        }
    }

}
