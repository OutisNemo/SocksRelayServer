using System;

namespace OutisNemo.SocksRelayServer
{
    public class ConnectionException : ApplicationException
    {
        public ConnectionException(string message) : base(message) { }
    }
}