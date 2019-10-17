using System.Net.Sockets;

namespace SocksRelayServer.Relay
{
    internal static class Helpers
    {
        public static void TryDispose(this Socket socket)
        {
            if (socket is null)
            {
                return;
            }

            if (socket.Connected)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Send);
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                socket.Close();
            }
            catch
            {
                // ignored
            }
        }

        public static void TryDispose(this SocketAsyncEventArgs saea)
        {
            if (saea is null)
            {
                return;
            }

            try
            {
                saea.UserToken = null;
                saea.AcceptSocket = null;

                saea.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
