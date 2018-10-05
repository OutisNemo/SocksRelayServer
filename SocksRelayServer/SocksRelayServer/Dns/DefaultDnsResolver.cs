using System.Net;

namespace SocksRelayServer.Dns
{
    internal class DefaultDnsResolver : IDnsResolver
    {
        public IPAddress TryResolve(string hostname)
        {
            var result = System.Net.Dns.GetHostAddresses(hostname);
            return result.Length < 1 ? null : result[0];
        }
    }
}
