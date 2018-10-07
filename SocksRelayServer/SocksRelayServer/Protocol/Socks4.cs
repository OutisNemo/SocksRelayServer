namespace SocksRelayServer.Protocol
{
    internal static class Socks4
    {
        public const byte Version = 0x04;

        public const byte CommandStreamConnection = 0x01;
        public const byte CommandBindingConnection = 0x02;

        public const byte StatusRequestGranted = 0x5A;
        public const byte StatusRequestFailed = 0x5B;
    }
}
