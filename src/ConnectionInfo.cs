using SocksRelayServer.Relay;

namespace SocksRelayServer
{
    internal class ConnectionInfo
    {
        public System.Net.Sockets.Socket LocalSocket { get; set; }

        public System.Net.Sockets.Socket RemoteSocket { get; set; }

        public void Terminate()
        {
            LocalSocket.TryDispose();
            RemoteSocket.TryDispose();
        }
    }
}
