using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class PendingChecksTests
    {
        [Fact]
        public void Empty_HasNoMostRecent()
        {
            var checks = new PendingChecks();

            Assert.Equal(0, checks.Count);
            Assert.Null(checks.MostRecent);
            Assert.False(checks.Contains("autoblock"));
        }

        [Fact]
        public void Add_RecordsOnce()
        {
            var checks = new PendingChecks();

            Assert.True(checks.Add("autoblock"));
            Assert.False(checks.Add("autoblock"));
            Assert.Equal(1, checks.Count);
            Assert.True(checks.Contains("autoblock"));
        }

        [Fact]
        public void LevelledIds_AreDistinct()
        {
            var checks = new PendingChecks();

            checks.Add("track_speed1");

            Assert.True(checks.Contains("track_speed1"));
            Assert.False(checks.Contains("track_speed2"));
        }

        [Fact]
        public void MostRecent_FollowsInsertionOrderAndRemoval()
        {
            var checks = new PendingChecks();
            checks.Add("autoblock");
            checks.Add("track_speed1");
            checks.Add("waypoint");

            Assert.Equal("waypoint", checks.MostRecent);
            Assert.True(checks.Remove("waypoint"));
            Assert.Equal("track_speed1", checks.MostRecent);
            Assert.True(checks.Remove("autoblock"));
            Assert.Equal("track_speed1", checks.MostRecent);
            Assert.True(checks.Remove("track_speed1"));
            Assert.Null(checks.MostRecent);
        }

        [Fact]
        public void Remove_Unknown_ReturnsFalse()
        {
            Assert.False(new PendingChecks().Remove("autoblock"));
        }

        [Fact]
        public void Duplicate_DoesNotChangeMostRecent()
        {
            var checks = new PendingChecks();
            checks.Add("autoblock");
            checks.Add("waypoint");

            checks.Add("autoblock");

            Assert.Equal("waypoint", checks.MostRecent);
        }
    }
}
