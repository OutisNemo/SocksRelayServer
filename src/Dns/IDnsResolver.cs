using System.Net;
using System.Threading.Tasks;

namespace SocksRelayServer.Dns
{
    public interface IDnsResolver
    {
        Task<IPAddress> TryResolve(string hostname);
    }
}
