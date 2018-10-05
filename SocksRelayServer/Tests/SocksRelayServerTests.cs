using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class SocksRelayServer
    {
        private static readonly IPAddress RemoteProxyAddress = IPAddress.Parse("192.168.0.100");
        private static readonly int RemoteProxyPort = 1080;

        [TestMethod]
        public void IsRemoteProxyListening()
        {
            using (var client = new TcpClient())
            {
                try
                {
                    client.Connect(RemoteProxyAddress, RemoteProxyPort);
                }
                catch (SocketException)
                {
                    Assert.Fail("Remote proxy server is not running, check configured IP and port");
                }

                client.Close();
            }
        }

        [TestMethod]
        public void CheckRelayingToIpAddress()
        {
            
        }

        [TestMethod]
        public void CheckRelayingToHostname()
        {

        }

        [TestMethod]
        public void CheckRelayingToNonExistentIpAddress()
        {

        }

        [TestMethod]
        public void CheckRelayingToNonExistentHostname()
        {

        }
    }
}
