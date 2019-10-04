namespace SocksRelayServer.Protocol
{
    internal static class Socks5
    {
        public const byte Version = 0x05;

        public const byte CommandStreamConnection = 0x01;
        public const byte CommandBindingConnection = 0x02;
        public const byte CommandUdpConnection = 0x03;

        public const byte AuthenticationUsernamePassword = 0x02;
        public const byte AuthenticationNone = 0x00;
        public const byte AuthenticationNoMethodAccepted = 0xFF;

        public const byte AddressTypeIPv4 = 0x01;
        public const byte AddressTypeIPv6 = 0x04;
        public const byte AddressTypeDomain = 0x03;

        public const byte StatusRequestGranted = 0x00;
        public const byte StatusGeneralFailure = 0x01;
        public const byte StatusConnectionNotAllowed = 0x02;
        public const byte StatusNetworkUnreachable = 0x03;
        public const byte StatusHostUnreachable = 0x04;
        public const byte StatusConnectionRefused = 0x05;
    }
}
