namespace SocksRelayServer
{
    using System.Net.Sockets;
    using System.Threading;

    internal class ConnectionInfo
    {
        public Socket LocalSocket { get; set; }

        public Socket RemoteSocket { get; set; }

        public Thread LocalThread { get; set; }

        public Thread RemoteThread { get; set; }
    }
}
