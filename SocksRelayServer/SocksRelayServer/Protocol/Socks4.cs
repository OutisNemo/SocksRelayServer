namespace SocksRelayServer.Protocol
{
    public static class Socks4
    {
        public static byte Version = 0x04;

        public static byte CommandStreamConnection = 0x01;
        public static byte CommandBindingConnection = 0x02;

        public static byte StatusRequestGranted = 0x5A;
        public static byte StatusRequestFailed = 0x5B;
    }
}
