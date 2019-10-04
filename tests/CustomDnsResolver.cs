using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DNS.Client;
using SocksRelayServer.Dns;

namespace SocksRelayServerTests
{
    public class CustomDnsResolver : IDnsResolver
    {
        public async Task<IPAddress> TryResolve(string hostname)
        {
            if (IPAddress.TryParse(hostname, out var address))
            {
                return address;
            }

            IList<IPAddress> ips = null;
            var client = new DnsClient("1.1.1.1");

            try
            {
                ips = await client.Lookup(hostname);
            }
            catch (ResponseException)
            {
                // ignore
            }

            return ips?.Count > 0 ? ips[0] : null;
        }
    }
}
