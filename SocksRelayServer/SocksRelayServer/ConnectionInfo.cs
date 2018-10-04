using System.Net.Sockets;
using System.Threading;

namespace SocksRelayServer
{
    class ConnectionInfo
    {
        public Socket LocalSocket;
        public Thread LocalThread;
        public Socket RemoteSocket;
        public Thread RemoteThread;
    }
}