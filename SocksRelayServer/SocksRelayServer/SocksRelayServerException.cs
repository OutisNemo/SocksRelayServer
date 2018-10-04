using System;

namespace SocksRelayServer
{
    public class SocksRelayServerException : Exception
    {
        public SocksRelayServerException(string message) : base(message) { }
    }
}