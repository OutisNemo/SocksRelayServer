namespace SocksRelayServer.Exception
{
    public class Socks5Exception : SocksRelayServerException
    {
        public Socks5Exception(string message)
            : base(message)
        {
        }
    }
}
