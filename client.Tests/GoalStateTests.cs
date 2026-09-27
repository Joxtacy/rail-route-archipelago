using System;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public sealed class GoalStateTests : IDisposable
    {
        private const string Haarlem = "haarlem-uuid";
        private const string Prague = "prague-uuid";

        public GoalStateTests() => GoalState.Reset();

        public void Dispose() => GoalState.Reset();

        [Fact]
        public void LiveWhileConnected_Sends()
        {
            var decision = GoalState.OnReachedLive(Haarlem, connected: true, mapMatches: true, goalSupported: true);

            Assert.True(decision.Send);
        }

        [Fact]
        public void LiveWhileOffline_Pending_ThenLoginOnSameMapSends()
        {
            var live = GoalState.OnReachedLive(Haarlem, connected: false, mapMatches: true, goalSupported: true);

            Assert.False(live.Send);
            Assert.Equal(GoalRefusal.NotConnected, live.Refusal);
            Assert.True(GoalState.IsPendingFor(Haarlem));

            var login = GoalState.OnLogin(Haarlem, mapMatches: true, goalSupported: true, levelGranted: true);

            Assert.True(login.Send);
        }

        [Fact]
        public void LoginOnAnotherMap_Refuses_StillPending()
        {
            GoalState.OnReachedLive(Haarlem, connected: false, mapMatches: true, goalSupported: true);

            var login = GoalState.OnLogin(Prague, mapMatches: true, goalSupported: true, levelGranted: true);

            Assert.False(login.Send);
            Assert.Equal(GoalRefusal.NotPending, login.Refusal);
            Assert.True(GoalState.IsPendingFor(Haarlem));
        }

        [Fact]
        public void LoginWithLevelNotGranted_Refuses()
        {
            GoalState.OnReachedLive(Haarlem, connected: false, mapMatches: true, goalSupported: true);

            var login = GoalState.OnLogin(Haarlem, mapMatches: true, goalSupported: true, levelGranted: false);

            Assert.Equal(GoalRefusal.LevelNotGranted, login.Refusal);
        }

        [Fact]
        public void LoginWithNothingPending_Refuses()
        {
            var login = GoalState.OnLogin(Haarlem, mapMatches: true, goalSupported: true, levelGranted: true);

            Assert.Equal(GoalRefusal.NotPending, login.Refusal);
        }

        [Fact]
        public void LiveWithMapMismatch_Refuses_NotPending()
        {
            var live = GoalState.OnReachedLive(Prague, connected: true, mapMatches: false, goalSupported: true);

            Assert.Equal(GoalRefusal.MapMismatch, live.Refusal);
            Assert.Null(GoalState.PendingMap);
        }

        [Fact]
        public void LiveWithUnsupportedGoal_Refuses()
        {
            var live = GoalState.OnReachedLive(Haarlem, connected: true, mapMatches: true, goalSupported: false);

            Assert.Equal(GoalRefusal.UnsupportedGoal, live.Refusal);
        }

        [Fact]
        public void AfterSent_EveryEntryPointRefusesAlreadySent()
        {
            GoalState.MarkSent();

            Assert.Equal(GoalRefusal.AlreadySent,
                GoalState.OnReachedLive(Haarlem, connected: true, mapMatches: true, goalSupported: true).Refusal);
            Assert.Equal(GoalRefusal.AlreadySent,
                GoalState.OnLogin(Haarlem, mapMatches: true, goalSupported: true, levelGranted: true).Refusal);
            Assert.Equal(GoalRefusal.AlreadySent,
                GoalState.OnManual(Haarlem, connected: true, mapMatches: true, goalSupported: true, levelGranted: true).Refusal);
        }

        [Fact]
        public void MarkSent_ClearsPending()
        {
            GoalState.OnReachedLive(Haarlem, connected: false, mapMatches: true, goalSupported: true);

            GoalState.MarkSent();

            Assert.True(GoalState.Sent);
            Assert.Null(GoalState.PendingMap);
        }

        [Fact]
        public void Failed_PendingAgain()
        {
            GoalState.MarkSent();

            GoalState.MarkFailed(Haarlem);

            Assert.False(GoalState.Sent);
            Assert.True(GoalState.IsPendingFor(Haarlem));
            Assert.True(GoalState.OnLogin(Haarlem, mapMatches: true, goalSupported: true, levelGranted: true).Send);
        }

        [Fact]
        public void Manual_AllConditions_Sends()
        {
            var decision = GoalState.OnManual(Haarlem, connected: true, mapMatches: true, goalSupported: true, levelGranted: true);

            Assert.True(decision.Send);
        }

        [Theory]
        [InlineData(false, true, true, true, GoalRefusal.NotConnected)]
        [InlineData(true, false, true, true, GoalRefusal.MapMismatch)]
        [InlineData(true, true, false, true, GoalRefusal.UnsupportedGoal)]
        [InlineData(true, true, true, false, GoalRefusal.LevelNotGranted)]
        public void Manual_EachFailedCondition_RefusesWithItsReason(bool connected, bool mapMatches, bool goalSupported,
            bool levelGranted, GoalRefusal expected)
        {
            var decision = GoalState.OnManual(Haarlem, connected, mapMatches, goalSupported, levelGranted);

            Assert.False(decision.Send);
            Assert.Equal(expected, decision.Refusal);
            Assert.Null(GoalState.PendingMap);
        }

        [Fact]
        public void ReasonTexts()
        {
            Assert.Equal("not connected", GoalState.ReasonText(GoalRefusal.NotConnected));
            Assert.Equal("the level's map doesn't match the seed", GoalState.ReasonText(GoalRefusal.MapMismatch));
            Assert.Equal("the level hasn't earned the Endless-complete star", GoalState.ReasonText(GoalRefusal.LevelNotGranted));
            Assert.Contains("endless_complete", GoalState.ReasonText(GoalRefusal.UnsupportedGoal));
            Assert.Contains("already sent", GoalState.ReasonText(GoalRefusal.AlreadySent));
        }

        [Theory]
        [InlineData("endless_complete", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("all_stars", false)]
        public void IsSupportedGoal(string goal, bool expected)
        {
            Assert.Equal(expected, GoalState.IsSupportedGoal(goal));
        }
    }
}
