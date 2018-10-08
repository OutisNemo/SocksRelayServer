using System;
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
        private const int RemoteProxyPort = 1080;

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
        public async Task CheckIfTrafficIsNotMalformed()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = false;
                relay.Start();

                await TestHelpers.DoTestRequest<Socks4a>(relay.LocalEndPoint, "http://httpbin.org/get");
            }
        }

        [TestMethod]
        public async Task CheckRelayingToIpAddress()
        {
            using (var relay = CreateRelayServer())
            {
                relay.Start();

                await TestHelpers.DoTestRequest<Socks4>(relay.LocalEndPoint, "http://172.217.18.78/");
            }
        }

        

        [TestMethod]
        public async Task CheckRelayingToHostnameResolveLocally()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = false;
                relay.Start();

                await TestHelpers.DoTestRequest<Socks4a>(relay.LocalEndPoint, "http://google.com/");
            }
        }

        [TestMethod]
        public async Task CheckRelayingToHostnameResolveRemotely()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = true;
                relay.Start();

                await TestHelpers.DoTestRequest<Socks4a>(relay.LocalEndPoint, "https://google.com/");
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
                    ConnectTimeout = 30
                };

                using (var proxyClientHandler = new ProxyClientHandler<Socks4a>(settings))
                {
                    using (var httpClient = new HttpClient(proxyClientHandler))
                    {
                        try
                        {
                            var response = await httpClient.GetAsync("http://0.1.2.3");
                            var content = await response.Content.ReadAsStringAsync();

                            Assert.Fail();
                        }
                        catch (ProxyException e)
                        {
                            Assert.AreEqual("Request rejected or failed", e.Message);
                        }
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
                    ConnectTimeout = 30
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
                            Assert.AreEqual("Request rejected or failed", e.Message);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public async Task CheckRelayingToHostnameUsingCustomResolver()
        {
            using (var relay = CreateRelayServer())
            {
                relay.ResolveHostnamesRemotely = false;
                relay.DnsResolver = new CustomDnsResolver();
                relay.Start();

                await TestHelpers.DoTestRequest<Socks4a>(relay.LocalEndPoint, "https://google.com/");
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
                    ConnectTimeout = 30
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
                            Assert.AreEqual("Request rejected or failed", e.Message);
                        }
                    }
                }
            }
        }

        private static ISocksRelayServer CreateRelayServer()
        {
            var relay = new SocksRelayServer.SocksRelayServer(new IPEndPoint(IPAddress.Loopback, TestHelpers.GetFreeTcpPort()), new IPEndPoint(RemoteProxyAddress, RemoteProxyPort));
            relay.OnLogMessage += (sender, s) => Console.WriteLine(s);

            return relay;
        }
    }
}
