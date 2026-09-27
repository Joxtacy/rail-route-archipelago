using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class ReceivedItemsTests
    {
        [Fact]
        public void Unknown_IsZero()
        {
            Assert.Equal(0, new ReceivedItems().Count("Autoblock"));
        }

        [Fact]
        public void RepeatedReceives_Accumulate()
        {
            var items = new ReceivedItems();

            Assert.Equal(1, items.Receive("TrackSpeed"));
            Assert.Equal(2, items.Receive("TrackSpeed"));
            Assert.Equal(3, items.Receive("TrackSpeed"));
            Assert.Equal(3, items.Count("TrackSpeed"));
        }

        [Fact]
        public void Keys_AreIndependent()
        {
            var items = new ReceivedItems();

            items.Receive("TrackSpeed");
            items.Receive("TrackSpeed");
            items.Receive("Autoblock");

            Assert.Equal(2, items.Count("TrackSpeed"));
            Assert.Equal(1, items.Count("Autoblock"));
            Assert.Equal(0, items.Count("StationCount"));
        }

        [Fact]
        public void Clear_ResetsEverything()
        {
            var items = new ReceivedItems();
            items.Receive("TrackSpeed");
            items.Receive("Autoblock");

            items.Clear();

            Assert.Equal(0, items.Count("TrackSpeed"));
            Assert.Equal(0, items.Count("Autoblock"));
            Assert.Equal(1, items.Receive("Autoblock"));
        }
    }
}
