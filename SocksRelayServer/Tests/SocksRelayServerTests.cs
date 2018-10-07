using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
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
        public async Task CheckIfTrafficIsNotMalformed()
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

                await DoTestRequest<Socks4a>(settings, "http://httpbin.org/get");
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
                    ConnectTimeout = 30
                };

                await DoTestRequest<Socks4>(settings, "http://172.217.18.78/");
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
                    ConnectTimeout = 30
                };

                await DoTestRequest<Socks4a>(settings, "http://google.com/");
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
                    ConnectTimeout = 30
                };

                await DoTestRequest<Socks4a>(settings, "https://google.com/");
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

                var settings = new ProxySettings()
                {
                    Host = relay.LocalEndPoint.Address.ToString(),
                    Port = relay.LocalEndPoint.Port,
                    ConnectTimeout = 30
                };

                await DoTestRequest<Socks4a>(settings, "https://google.com/");
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
            var relay = new SocksRelayServer.SocksRelayServer(new IPEndPoint(IPAddress.Loopback, GetFreeTcpPort()), new IPEndPoint(RemoteProxyAddress, RemoteProxyPort));
            relay.OnLogMessage += (sender, s) => Console.WriteLine(s);

            return relay;
        }

        private static async Task DoTestRequest<T>(ProxySettings settings, string url) where T : IProxy
        {
            string responseContentWithProxy;
            using (var proxyClientHandler = new ProxyClientHandler<T>(settings))
            {
                using (var httpClient = new HttpClient(proxyClientHandler))
                {
                    var response = await httpClient.SendAsync(GenerateRequestMessageForTestRequest(url));
                    responseContentWithProxy = await response.Content.ReadAsStringAsync();
                }
            }

            string responseContentWithoutProxy;
            using (var handler = new HttpClientHandler())
            {
                handler.AllowAutoRedirect = false;

                using (var httpClient = new HttpClient(handler))
                {
                    var response = await httpClient.SendAsync(GenerateRequestMessageForTestRequest(url));
                    responseContentWithoutProxy = await response.Content.ReadAsStringAsync();
                }
            }

            Assert.AreEqual(responseContentWithoutProxy, responseContentWithProxy);
        }

        private static HttpRequestMessage GenerateRequestMessageForTestRequest(string url)
        {
            var requestMessage = new HttpRequestMessage
            {
                Version = HttpVersion.Version10,
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
                
            };

            requestMessage.Headers.TryAddWithoutValidation(
                "User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36"
            );

            return requestMessage;
        }
    }
}
