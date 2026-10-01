using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class GoalStateTests
    {
        [Fact]
        public void Bind_WithTheStar_Ineligible()
        {
            Assert.Equal(GoalStatus.Ineligible, GoalState.OnBind(GoalStatus.None, granted: true));
        }

        [Theory]
        [InlineData(GoalStatus.None)]
        [InlineData(GoalStatus.Pending)]
        public void Bind_WithoutTheStar_Unchanged(GoalStatus state)
        {
            Assert.Equal(state, GoalState.OnBind(state, granted: false));
        }

        [Theory]
        [InlineData(GoalStatus.None)]
        [InlineData(GoalStatus.Pending)]
        public void Login_BoundGranted_NotSent_Sends(GoalStatus state)
        {
            var decision = GoalState.OnLogin(state, bound: true, mapMatches: true, goalSupported: true, granted: true);

            Assert.True(decision.Send);
            Assert.Equal(state, decision.State);
        }

        [Fact]
        public void Login_Sent_Refuses()
        {
            var decision = GoalState.OnLogin(GoalStatus.Sent, bound: true, mapMatches: true, goalSupported: true, granted: true);

            Assert.Equal(GoalRefusal.AlreadySent, decision.Refusal);
            Assert.Equal(GoalStatus.Sent, decision.State);
        }

        [Fact]
        public void Login_Ineligible_RefusesWithPredatesReason()
        {
            var decision = GoalState.OnLogin(GoalStatus.Ineligible, bound: true, mapMatches: true, goalSupported: true, granted: true);

            Assert.Equal(GoalRefusal.PredatesBinding, decision.Refusal);
            Assert.Equal("the star predates the binding", decision.Reason);
            Assert.Equal(GoalStatus.Ineligible, decision.State);
        }

        [Fact]
        public void Login_NotGranted_Refuses()
        {
            var decision = GoalState.OnLogin(GoalStatus.None, bound: true, mapMatches: true, goalSupported: true, granted: false);

            Assert.Equal(GoalRefusal.LevelNotGranted, decision.Refusal);
        }

        [Theory]
        [InlineData(false, true, true, GoalRefusal.MapMismatch)]
        [InlineData(true, false, true, GoalRefusal.UnsupportedGoal)]
        [InlineData(true, true, false, GoalRefusal.ForeignSave)]
        public void Login_EachFailedCondition_RefusesWithItsReason(bool mapMatches, bool goalSupported, bool bound, GoalRefusal expected)
        {
            var decision = GoalState.OnLogin(GoalStatus.Pending, bound, mapMatches, goalSupported, granted: true);

            Assert.Equal(expected, decision.Refusal);
            Assert.Equal(GoalStatus.Pending, decision.State);
        }

        [Fact]
        public void Live_ConnectedAndBound_Sends()
        {
            var decision = GoalState.OnReachedLive(GoalStatus.None, connected: true, bound: true, mapMatches: true, goalSupported: true);

            Assert.True(decision.Send);
        }

        [Fact]
        public void Live_Offline_Pending()
        {
            var decision = GoalState.OnReachedLive(GoalStatus.None, connected: false, bound: false, mapMatches: false, goalSupported: false);

            Assert.Equal(GoalRefusal.NotConnected, decision.Refusal);
            Assert.Equal(GoalStatus.Pending, decision.State);
        }

        [Fact]
        public void Live_MapMismatch_Refuses_NotPending()
        {
            var decision = GoalState.OnReachedLive(GoalStatus.None, connected: true, bound: false, mapMatches: false, goalSupported: true);

            Assert.Equal(GoalRefusal.MapMismatch, decision.Refusal);
            Assert.Equal(GoalStatus.None, decision.State);
        }

        [Theory]
        [InlineData(GoalStatus.Sent, GoalRefusal.AlreadySent)]
        [InlineData(GoalStatus.Ineligible, GoalRefusal.PredatesBinding)]
        public void Live_SentOrIneligible_RefusesEvenOffline(GoalStatus state, GoalRefusal expected)
        {
            var decision = GoalState.OnReachedLive(state, connected: false, bound: true, mapMatches: true, goalSupported: true);

            Assert.Equal(expected, decision.Refusal);
            Assert.Equal(state, decision.State);
        }

        [Fact]
        public void PendingThenBindWithTheStar_Ineligible()
        {
            var live = GoalState.OnReachedLive(GoalStatus.None, connected: false, bound: false, mapMatches: true, goalSupported: true);

            var bound = GoalState.OnBind(live.State, granted: true);

            Assert.Equal(GoalStatus.Ineligible, bound);
            Assert.False(GoalState.OnLogin(bound, bound: true, mapMatches: true, goalSupported: true, granted: true).Send);
        }

        [Fact]
        public void SentThenFailed_Pending_SendsOnLogin()
        {
            var failed = GoalState.MarkFailed();

            Assert.Equal(GoalStatus.Sent, GoalState.MarkSent());
            Assert.Equal(GoalStatus.Pending, failed);
            Assert.True(GoalState.OnLogin(failed, bound: true, mapMatches: true, goalSupported: true, granted: true).Send);
        }

        [Fact]
        public void ReasonTexts()
        {
            Assert.Equal("not connected", GoalState.ReasonText(GoalRefusal.NotConnected));
            Assert.Equal("the level's map doesn't match the seed", GoalState.ReasonText(GoalRefusal.MapMismatch));
            Assert.Equal("the level hasn't earned the Endless-complete star", GoalState.ReasonText(GoalRefusal.LevelNotGranted));
            Assert.Contains("endless_complete", GoalState.ReasonText(GoalRefusal.UnsupportedGoal));
            Assert.Equal("the goal was already sent from this save", GoalState.ReasonText(GoalRefusal.AlreadySent));
            Assert.Equal("the star predates the binding", GoalState.ReasonText(GoalRefusal.PredatesBinding));
            Assert.Equal("the save is bound to another seed", GoalState.ReasonText(GoalRefusal.ForeignSave));
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
