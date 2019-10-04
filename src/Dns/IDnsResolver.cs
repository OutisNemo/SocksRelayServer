namespace SocksRelayServer.Dns
{
    using System.Net;

    public interface IDnsResolver
    {
        IPAddress TryResolve(string hostname);
    }
}
