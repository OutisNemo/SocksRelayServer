using System;

namespace SocksRelayServer
{
    public class ConnectionException : ApplicationException
    {
        public ConnectionException(string message) : base(message) { }
    }
}