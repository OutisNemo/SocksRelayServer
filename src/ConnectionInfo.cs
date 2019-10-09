using System;

namespace SocksRelayServer
{
    using System.Net.Sockets;

    internal class ConnectionInfo : IDisposable
    {
        public Socket LocalSocket { get; set; }

        public Socket RemoteSocket { get; set; }

        public void Terminate()
        {
            if (LocalSocket != null && LocalSocket.Connected)
            {
                LocalSocket.Shutdown(SocketShutdown.Both);
                LocalSocket.Close();
            }

            if (RemoteSocket != null && RemoteSocket.Connected)
            {
                RemoteSocket.Shutdown(SocketShutdown.Both);
                RemoteSocket.Close();
            }

            Dispose();
        }

        public void Dispose()
        {
            LocalSocket?.Dispose();
            RemoteSocket?.Dispose();
        }
    }
}
