using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Common;
using Pantheon.Common.IO;
using Pantheon.Common.TCP;
using Xunit;

namespace Pantheon.Tests.Common
{
    public class NetworkManagerTests
    {
        static INetworkManager networkManager = null;
        static TcpNetworkManagerServer server = null;
        static INetworkManager client = null;
        string host = "127.0.0.1";
        int port = 9091;

        NetStream testData = null;

        public NetworkManagerTests()
        {
            PantheonInitializer.InitializeAll();
        }

        [Fact]
        public void FactoryShouldCreateNetworkManager()
        {
            Assert.DoesNotThrow(() => networkManager = NetworkManagerFactory.CreateNetworkManager(NetworkManagerFeatures.TcpServer, port));

            Assert.NotNull(networkManager);

            Assert.Equal(networkManager.Port, port);

            Assert.IsAssignableFrom(typeof(TcpNetworkManagerServer), networkManager);

            server = (TcpNetworkManagerServer)networkManager;
        }

        [Fact]
        public void ServerShouldStart()
        {
            Assert.NotNull(networkManager);

            Assert.DoesNotThrow(() => networkManager.Connect());

            Assert.True(networkManager.IsConnected);
        }

        [Fact]
        public void FactoryCreatesClient()
        {
            client = NetworkManagerFactory.CreateNetworkManager(NetworkManagerFeatures.TcpClient, host, port);

            Assert.NotNull(client);

            Assert.Equal(client.Host, host);
            Assert.Equal(client.Port, port);

            Assert.IsAssignableFrom(typeof(TcpNetworkManager), client);
        }

        [Fact]
        public void ClientShouldConnect()
        {
            bool clientConnected = false;
            server.OnConnected += (sender, e) =>
            {
                Assert.NotNull(e.NetworkManager);

                clientConnected = true;
            };
            Assert.DoesNotThrow(() => client.Connect());

            Assert.True(client.IsConnected);

            Assert.True(clientConnected);
        }

        [Fact]
        public void ClientSendsData()
        {
            testData = new NetStream();
            testData.Write(true);
            testData.Write((byte)128);
            testData.Write(new byte[] { 1, 2, 3 });
            testData.Write(DateTime.Now);
            testData.Write(1.2d);
            testData.Write(2.1f);
            testData.Write(int.MaxValue);
            testData.Write(long.MaxValue);

            Assert.DoesNotThrow(() => client.WriteMessage(testData));
        }

        [Fact]
        public void ServerShouldReceiveData()
        {
            var data = server.ReadMessage();

            Assert.NotNull(data);
            Assert.NotEmpty(data.Data);
            Assert.Equal(testData.Length, data.Length);

            Assert.True(Enumerable.SequenceEqual(testData.Data, data.Data));
        }

        [Fact]
        public void ServerShouldSendReply()
        {
            Assert.DoesNotThrow(() => server.WriteMessage(testData));
        }

        [Fact]
        public void ClientShouldReceiveReply()
        {
            var data = client.ReadMessage();


            Assert.NotNull(data);
            Assert.NotEmpty(data.Data);
            Assert.Equal(testData.Length, data.Length);

            Assert.True(Enumerable.SequenceEqual(testData.Data, data.Data));
        }
    }
}
