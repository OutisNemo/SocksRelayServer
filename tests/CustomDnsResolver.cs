using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DNS.Client;
using SocksRelayServer.Dns;

namespace SocksRelayServerTests
{
    public class CustomDnsResolver : IDnsResolver
    {
        private readonly DnsClient _client;

        public CustomDnsResolver()
        {
            _client = new DnsClient("1.1.1.1");
        }

        public async Task<IPAddress> TryResolve(string hostname)
        {
            IList<IPAddress> ips = null;

            if (IPAddress.TryParse(hostname, out var address))
            {
                return address;
            }

            try
            {
                ips = await _client.Lookup(hostname);
            }
            catch
            {
                // ignore
            }

            return ips?.Count > 0 ? ips[0] : null;
        }
    }
}
