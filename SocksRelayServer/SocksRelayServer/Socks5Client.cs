using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SocksRelayServer.Exception;

namespace SocksRelayServer
{
    internal class Socks5Client
    {
        private readonly string _socksAddr;
        private readonly int _socksPort;
        private readonly string _destAddr;
        private readonly int _destPort;
        private readonly string _username;
        private readonly string _password;
        private readonly Socket _socket;

        private Socks5Client(string socksAddress, int socksPort, string destAddress, int destPort, string username, string password)
        {
            _socksAddr = socksAddress;
            _socksPort = socksPort;
            _destAddr = destAddress;
            _destPort = destPort;
            _username = username;
            _password = password;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public static Socket Connect(string socksAddress, int socksPort, string destAddress, int destPort, string username, string password)
        {
            var client = new Socks5Client(socksAddress, socksPort, destAddress, destPort, username, password);
            return client.Connect();
        }

        public Socket Connect()
        {
            byte[] buffer;
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                buffer = new byte[4]
                {
                    Protocol.Socks5.Version,
                    2,
                    Protocol.Socks5.AuthenticationNone,
                    Protocol.Socks5.AuthenticationUsernamePassword
                };
            }
            else
            {
                buffer = new byte[3]
                {
                    Protocol.Socks5.Version,
                    1,
                    Protocol.Socks5.AuthenticationNone
                };
            }
            
            _socket.Connect(_socksAddr, _socksPort);
            _socket.Send(buffer);
            _socket.Receive(buffer, 0, 2, SocketFlags.None);

            if (buffer[1] == Protocol.Socks5.AuthenticationNoMethodAccepted)
            {
                _socket.Close();
                throw new Socks5Exception("No authentication method is accepted");
            }

            if (buffer[1] == Protocol.Socks5.AuthenticationUsernamePassword)
            {
                var credentials = new byte[_username.Length + _password.Length + 3];
                credentials[0] = 1;
                credentials[1] = (byte)_username.Length;
                Encoding.ASCII.GetBytes(_username).CopyTo(credentials, 2);
                credentials[_username.Length + 2] = (byte)_password.Length;
                Encoding.ASCII.GetBytes(_password).CopyTo(credentials, _username.Length + 3);

                _socket.Send(credentials, credentials.Length, SocketFlags.None);
                buffer = new byte[2];
                _socket.Receive(buffer, buffer.Length, SocketFlags.None);
                if (buffer[1] != Protocol.Socks5.StatusRequestGranted)
                {
                    throw new Socks5Exception("Invalid username or password");
                }
            }

            var addrType = GetAddressType(_destAddr);
            var address = GetDestAddressBytes(addrType, _destAddr);
            var port = GetDestPortBytes(_destPort);

            buffer = new byte[4 + port.Length + address.Length];
            buffer[0] = Protocol.Socks5.Version;
            buffer[1] = Protocol.Socks5.CommandStreamConnection;
            buffer[2] = 0x00;
            buffer[3] = addrType;

            address.CopyTo(buffer, 4);
            port.CopyTo(buffer, 4 + address.Length);
            _socket.Send(buffer);
            buffer = new byte[255];
            _socket.Receive(buffer, buffer.Length, SocketFlags.None);

            if (buffer[1] != Protocol.Socks5.StatusRequestGranted)
            {
                switch (buffer[1])
                {
                    case Protocol.Socks5.StatusGeneralFailure:
                        throw new Socks5Exception("Upstream connection failed: StatusGeneralFailure");
                    case Protocol.Socks5.StatusConnectionNotAllowed:
                        throw new Socks5Exception("Upstream connection failed: StatusConnectionNotAllowed");
                    case Protocol.Socks5.StatusNetworkUnreachable:
                        throw new Socks5Exception("Upstream connection failed: StatusNetworkUnreachable");
                    case Protocol.Socks5.StatusHostUnreachable:
                        throw new Socks5Exception("Upstream connection failed: StatusHostUnreachable");
                    case Protocol.Socks5.StatusConnectionRefused:
                        throw new Socks5Exception("Upstream connection failed: StatusConnectionRefused");
                    default:
                        throw new Socks5Exception("Upstream connection failed: Unknown error");
                }
                
            }

            return _socket;
        }


        private static byte GetAddressType(string destAddr)
        {
            var result = IPAddress.TryParse(destAddr, out var ipAddr);

            if (!result)
            {
                return Protocol.Socks5.AddressTypeDomain;
            }
                
            switch (ipAddr.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return Protocol.Socks5.AddressTypeIPv4;
                case AddressFamily.InterNetworkV6:
                    return Protocol.Socks5.AddressTypeIPv6;
                default:
                    throw new Socks5Exception("Upstream connection failed: Unknown address type");
            }
        }

        private static byte[] GetDestAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case Protocol.Socks5.AddressTypeIPv4:
                case Protocol.Socks5.AddressTypeIPv6:
                    return IPAddress.Parse(host).GetAddressBytes();
                case Protocol.Socks5.AddressTypeDomain:
                    var bytes = new byte[host.Length + 1];
                    bytes[0] = Convert.ToByte(host.Length);
                    Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);
                    return bytes;
                default:
                    return null;
            }
        }

        private static byte[] GetDestPortBytes(int value)
        {
            var array = new byte[2];
            array[0] = Convert.ToByte(value / 256);
            array[1] = Convert.ToByte(value % 256);
            return array;
        }
    }
}