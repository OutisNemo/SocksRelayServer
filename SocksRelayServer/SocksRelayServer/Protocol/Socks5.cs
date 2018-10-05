namespace SocksRelayServer.Protocol
{
    internal static class Socks5
    {
        public static byte Version = 0x05;

        public static byte CommandStreamConnection = 0x01;
        public static byte CommandBindingConnection = 0x02;
        public static byte CommandUdpConnection = 0x03;

        public static byte AddressTypeIPv4 = 0x01;
        public static byte AddressTypeIPv6 = 0x04;
        public static byte AddressTypeDomain = 0x03;

        public static byte StatusRequestGranted = 0x00;
        public static byte StatusGeneralFailure = 0x01;
        public static byte StatusConnectionNotAllowed = 0x02;
        public static byte StatusNetworkUnreachable = 0x03;
        public static byte StatusHostUnreachable = 0x04;
        public static byte StatusConnectionRefused = 0x05;
    }
}
