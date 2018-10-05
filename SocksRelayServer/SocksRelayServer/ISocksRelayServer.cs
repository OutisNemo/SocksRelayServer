using System;
using System.Net;
using SocksRelayServer.Dns;

namespace SocksRelayServer
{
    public interface ISocksRelayServer : IDisposable
    {
        event EventHandler<IPEndPoint> OnLocalConnect;
        event EventHandler<IPEndPoint> OnRemoteConnect;
        event EventHandler<string> OnLogMessage;

        IDnsResolver DnsResolver { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        int BufferSize { get; set; }
        bool ResolveHostnamesRemotely { get; set; }
        IPEndPoint LocalEndPoint { get; }
        IPEndPoint RemotEndPoint { get; }

        void Start();
        void Stop();
    }
}
