using System.Net;
using DNS.Client;
using SocksRelayServer.Dns;

namespace Tests
{
    class CustomDnsResolver : IDnsResolver
    {
        public IPAddress TryResolve(string hostname)
        {
            // Bind to a Domain Name Server
            var client = new DnsClient("8.8.8.8");

            // Returns a list of IPs
            var ips = client.Lookup(hostname).Result;

            return ips.Count > 0 ? ips[0] : null;
        }
    }
}
