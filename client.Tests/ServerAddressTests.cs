using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class ServerAddressTests
    {
        [Theory]
        [InlineData("localhost:38281", "localhost", 38281)]
        [InlineData("archipelago.gg:54321", "archipelago.gg", 54321)]
        [InlineData("127.0.0.1:1", "127.0.0.1", 1)]
        [InlineData("  localhost:40000  ", "localhost", 40000)]
        public void HostAndPort(string text, string host, int port)
        {
            Assert.True(ServerAddress.TryParse(text, out var address));
            Assert.Equal(host, address.Host);
            Assert.Equal(port, address.Port);
            Assert.False(address.IsUri);
            Assert.Null(address.Scheme);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("archipelago.gg")]
        public void BareHost_DefaultPort(string text)
        {
            Assert.True(ServerAddress.TryParse(text, out var address));
            Assert.Equal(text, address.Host);
            Assert.Equal(ServerAddress.DefaultPort, address.Port);
            Assert.Equal(38281, address.Port);
            Assert.False(address.IsUri);
        }

        [Theory]
        [InlineData("ws://localhost:38281", "ws", "localhost", 38281)]
        [InlineData("wss://archipelago.gg:54321", "wss", "archipelago.gg", 54321)]
        [InlineData("wss://archipelago.gg:443/", "wss", "archipelago.gg", 443)]
        [InlineData("WSS://archipelago.gg:54321", "wss", "archipelago.gg", 54321)]
        [InlineData("ws://localhost", "ws", "localhost", 38281)]
        public void Uri(string text, string scheme, string host, int port)
        {
            Assert.True(ServerAddress.TryParse(text, out var address));
            Assert.True(address.IsUri);
            Assert.Equal(scheme, address.Scheme);
            Assert.Equal(host, address.Host);
            Assert.Equal(port, address.Port);
            Assert.Equal(scheme, address.ToUri().Scheme);
            Assert.Equal(host, address.ToUri().Host);
            Assert.Equal(port, address.ToUri().Port);
        }

        [Fact]
        public void ToString_ShowsHostAndPort()
        {
            ServerAddress.TryParse("localhost", out var plain);
            ServerAddress.TryParse("wss://archipelago.gg:54321", out var uri);

            Assert.Equal("localhost:38281", plain.ToString());
            Assert.Equal("wss://archipelago.gg:54321", uri.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("localhost:")]
        [InlineData("localhost:abc")]
        [InlineData("localhost:0")]
        [InlineData("localhost:65536")]
        [InlineData("localhost:-1")]
        [InlineData("localhost: 38281")]
        [InlineData(":38281")]
        [InlineData("http://localhost:38281")]
        [InlineData("https://archipelago.gg")]
        [InlineData("ws://")]
        [InlineData("ws://localhost:38281/room")]
        [InlineData("ws://user@localhost:38281")]
        [InlineData("local host")]
        [InlineData("localhost/path")]
        public void Invalid_Rejected(string text)
        {
            Assert.False(ServerAddress.TryParse(text, out var address));
            Assert.Null(address);
        }
    }
}
