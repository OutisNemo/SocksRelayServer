using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocksRelayServer;
using SocksSharp;
using SocksSharp.Proxy;

namespace Tests
{
    [TestClass]
    public class SocksRelayServerTests
    {
        private static readonly IPAddress RemoteProxyAddress = IPAddress.Parse("192.168.0.100");
        private static readonly int RemoteProxyPort = 1080;

        [TestInitialize]
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
        public async Task CheckRelayingToIpAddress()
        {
            using (var relay = CreateRelayServer())
            {
                relay.Start();

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 5
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        var response = await httpClient.GetAsync("http://172.217.20.14");
                        var content = await response.Content.ReadAsStringAsync();

                        Assert.IsTrue(content.Contains("google.com"));
                    }
                }
            }
        }

        [TestMethod]
        public async Task CheckRelayingToHostnameResolveLocally()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = false;
                relay.Start();

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 5
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4a>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        var response = await httpClient.GetAsync("https://google.com");
                        var content = await response.Content.ReadAsStringAsync();

                        Assert.IsTrue(content.Contains("google.com"));
                    }
                }
            }
        }

        [TestMethod]
        public async Task CheckRelayingToHostnameResolveRemotely()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = true;
                relay.Start();

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 5
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4a>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        var response = await httpClient.GetAsync("https://google.com");
                        var content = await response.Content.ReadAsStringAsync();

                        Assert.IsTrue(content.Contains("google.com"));
                    }
                }
            }
        }

        [TestMethod]
        public async Task CheckRelayingToNonExistentIpAddress()
        {
            using (var relay = CreateRelayServer())
            {
                relay.Start();

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 5
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4a>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        var response = await httpClient.GetAsync("http://255.255.255.255");
                        var content = await response.Content.ReadAsStringAsync();

                        Assert.IsTrue(content.Contains("google.com"));
                    }
                }
            }
        }

        [TestMethod]
        public async Task CheckRelayingToNonExistentHostnameResolveLocally()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = false;
                relay.Start();

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 5
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4a>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        try
                        {
                            var response = await httpClient.GetAsync("https://nonexists-subdomain.google.com");
                            var content = await response.Content.ReadAsStringAsync();

                            Assert.Fail();
                        }
                        catch (ProxyException e)
                        {
                            Assert.AreEqual("Host unreachable", e.Message);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public async Task CheckRelayingToNonExistentHostnameResolveRemotely()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = true;
                relay.Start();

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 5
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4a>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        try
                        {
                            var response = await httpClient.GetAsync("https://nonexists-subdomain.google.com");
                            var content = await response.Content.ReadAsStringAsync();

                            Assert.Fail();
                        }
                        catch (ProxyException e)
                        {
                            Assert.AreEqual("Host unreachable", e.Message);
                        }
                    }
                }
            }
        }

        private static int GetFreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            return port;
        }

        private static ISocksRelayServer CreateRelayServer()
        {
            return new SocksRelayServer.SocksRelayServer(new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort()), new IPEndPoint(RemoteProxyAddress, RemoteProxyPort));
        }
    }
}
