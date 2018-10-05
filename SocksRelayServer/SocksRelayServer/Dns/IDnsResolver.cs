using System.Net;

namespace SocksRelayServer.Dns
{
    public interface IDnsResolver
    {
        IPAddress TryResolve(string hostname);
    }
}
