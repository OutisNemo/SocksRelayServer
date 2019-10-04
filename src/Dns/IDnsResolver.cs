using System.Threading.Tasks;

namespace SocksRelayServer.Dns
{
    using System.Net;

    public interface IDnsResolver
    {
        Task<IPAddress> TryResolve(string hostname);
    }
}
