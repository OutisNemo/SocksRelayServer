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
    public class TestHelpers
    {
        public static int GetFreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            return port;
        }

        public static async Task DoTestRequest<T>(IPEndPoint relayEndPoint, string url) where T : IProxy
        {
            var settings = new ProxySettings()
            {
                Host = relayEndPoint.Address.ToString(),
                Port = relayEndPoint.Port,
                ConnectTimeout = 30
            };

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

        public static HttpRequestMessage GenerateRequestMessageForTestRequest(string url)
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