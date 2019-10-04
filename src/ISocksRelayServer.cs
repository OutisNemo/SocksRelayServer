using System;
using System.Net;
using SocksRelayServer.Dns;

namespace SocksRelayServer
{
    public interface ISocksRelayServer : IDisposable
    {
        event EventHandler<DnsEndPoint> OnLocalConnect;

        event EventHandler<DnsEndPoint> OnRemoteConnect;

        event EventHandler<string> OnLogMessage;

        /// <summary>
        /// Set this to handle custom DNS resolutions. The default DNS reoslution method uses the built-in method of the OS.
        /// </summary>
        IDnsResolver DnsResolver { get; set; }

        /// <summary>
        /// The username used for authenticate against the upstream proxy.
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// The password used for authenticate against the upstream proxy.
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Set the buffer size used for communicating in both ways. The default is set to 8096.
        /// </summary>
        int BufferSize { get; set; }

        /// <summary>
        /// Set this to true if you want to pass the unresolved hostnames to the upstream proxy. In this case the DnsResolver property is ignored.
        /// </summary>
        bool ResolveHostnamesRemotely { get; set; }

        /// <summary>
        /// The local endpoint where this server is listening.
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// The remote endpoint where this server is relaying the requests.
        /// </summary>
        IPEndPoint RemotEndPoint { get; }

        /// <summary>
        /// The sockets time-out value, in milliseconds. If you set the property with a value between 1 and 499, the value will be changed to 500.
        /// The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.
        /// </summary>
        int SendTimeout { get; set; }

        /// <summary>
        /// The sockets time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also
        /// indicates an infinite time-out period.
        /// </summary>
        int ReceiveTimeout { get; set; }

        void Start();

        void Stop();
    }
}
