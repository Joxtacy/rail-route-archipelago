using System;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class CheckSelectionTests
    {
        [Fact]
        public void BoughtSlots_MapToLocationNames()
        {
            var selection = CheckSelection.Select(new[] { "autoblock", "track_speed1", "tunnels" }, Array.Empty<string>());

            Assert.Equal(new[] { "Autoblocks", "Basic Tracks", "Tunnels" }, selection.LocationNames);
            Assert.Empty(selection.UnknownIds);
        }

        [Fact]
        public void GrantedSlots_Excluded()
        {
            var selection = CheckSelection.Select(new[] { "autoblock", "waypoint", "tunnels" }, new[] { "waypoint" });

            Assert.Equal(new[] { "Autoblocks", "Tunnels" }, selection.LocationNames);
        }

        [Fact]
        public void CustomContractsVariants_GiveOneName()
        {
            var selection = CheckSelection.Select(new[] { "custom_contracts", "custom_contracts_alt" }, Array.Empty<string>());

            Assert.Equal(new[] { "Custom Contracts" }, selection.LocationNames);
        }

        [Fact]
        public void UnknownIds_Reported_OthersStillSelected()
        {
            var selection = CheckSelection.Select(new[] { "not_an_upgrade", "autoblock" }, Array.Empty<string>());

            Assert.Equal(new[] { "Autoblocks" }, selection.LocationNames);
            Assert.Equal(new[] { "not_an_upgrade" }, selection.UnknownIds);
        }

        [Fact]
        public void NoSlots_Empty()
        {
            var selection = CheckSelection.Select(Array.Empty<string>(), null);

            Assert.Empty(selection.LocationNames);
            Assert.Empty(selection.UnknownIds);
        }
    }
}
