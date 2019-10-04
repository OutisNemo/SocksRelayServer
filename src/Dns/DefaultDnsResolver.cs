﻿namespace SocksRelayServer.Dns
{
    using System.Net;
    using System.Net.Sockets;

    internal class DefaultDnsResolver : IDnsResolver
    {
        public IPAddress TryResolve(string hostname)
        {
            try
            {
                var result = System.Net.Dns.GetHostAddresses(hostname);
                return result.Length < 1 ? null : result[0];
            }
            catch (SocketException)
            {
                return null;
            }
        }
    }
}
