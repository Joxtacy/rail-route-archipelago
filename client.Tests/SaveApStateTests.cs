using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class SaveApStateTests
    {
        private static readonly SeedBinding SeedA = new SeedBinding("seed-a", 0, 1, "Player1");

        [Fact]
        public void RoundTrip_KeepsEveryField()
        {
            var state = new SaveApState
            {
                Binding = SeedA,
                UnknownBinding = true,
                Goal = GoalStatus.Pending,
                ReceivedIndex = 7,
            };
            state.SentChecks.Add("Upgrade Slot 2");
            state.SentChecks.Add("Upgrade Slot 1");

            Assert.True(SaveApState.TryParse(state.Serialize(), out var parsed, out var error), error);

            Assert.True(SeedA.Matches(parsed.Binding));
            Assert.Equal("Player1", parsed.Binding.SlotName);
            Assert.True(parsed.UnknownBinding);
            Assert.Equal(new[] { "Upgrade Slot 1", "Upgrade Slot 2" }, parsed.SentChecks);
            Assert.Equal(GoalStatus.Pending, parsed.Goal);
            Assert.Equal(7, parsed.ReceivedIndex);
        }

        [Theory]
        [InlineData(GoalStatus.None, "none")]
        [InlineData(GoalStatus.Pending, "pending")]
        [InlineData(GoalStatus.Sent, "sent")]
        [InlineData(GoalStatus.Ineligible, "ineligible")]
        public void Goal_IsALowercaseString(GoalStatus goal, string json)
        {
            var text = new SaveApState { Goal = goal }.Serialize();

            Assert.Contains("\"goal\": \"" + json + "\"", text);
            Assert.True(SaveApState.TryParse(text, out var parsed, out _));
            Assert.Equal(goal, parsed.Goal);
        }

        [Fact]
        public void Fresh_UnboundEmptyNoneZero()
        {
            var state = new SaveApState();

            Assert.Null(state.Binding);
            Assert.False(state.UnknownBinding);
            Assert.Empty(state.SentChecks);
            Assert.Equal(GoalStatus.None, state.Goal);
            Assert.Equal(0, state.ReceivedIndex);
        }

        [Fact]
        public void Unbound_RoundTripsAsUnbound()
        {
            Assert.True(SaveApState.TryParse(new SaveApState().Serialize(), out var parsed, out _));

            Assert.Null(parsed.Binding);
        }

        [Theory]
        [InlineData("{\"version\": 1, \"goal\": ")]
        [InlineData("not json")]
        [InlineData("")]
        [InlineData("[]")]
        public void InvalidJson_Error(string text)
        {
            Assert.False(SaveApState.TryParse(text, out var state, out var error));
            Assert.Null(state);
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void UnknownVersion_Error(int version)
        {
            var text = "{\"version\": " + version + ", \"goal\": \"none\", \"receivedIndex\": 0}";

            Assert.False(SaveApState.TryParse(text, out _, out var error));
            Assert.Contains("version", error);
        }

        [Fact]
        public void NegativeIndex_Error()
        {
            Assert.False(SaveApState.TryParse("{\"version\": 1, \"goal\": \"none\", \"receivedIndex\": -1}", out _, out var error));
            Assert.Contains("index", error);
        }

        [Theory]
        [InlineData("\"done\"")]
        [InlineData("\"Sent\"")]
        [InlineData("2")]
        [InlineData("null")]
        public void UnknownGoal_Error(string goal)
        {
            Assert.False(SaveApState.TryParse("{\"version\": 1, \"goal\": " + goal + ", \"receivedIndex\": 0}", out _, out var error));
            Assert.Contains("goal", error);
        }

        [Fact]
        public void BindingWithoutSeed_Error()
        {
            Assert.False(SaveApState.TryParse("{\"version\": 1, \"binding\": {\"team\": 0, \"slot\": 1}, \"goal\": \"none\"}", out _, out _));
        }

        [Fact]
        public void Matches_SameSeedTeamSlot_OtherSlotName()
        {
            Assert.True(SeedA.Matches(new SeedBinding("seed-a", 0, 1, "Renamed")));
        }

        [Theory]
        [InlineData("seed-b", 0, 1)]
        [InlineData("seed-a", 1, 1)]
        [InlineData("seed-a", 0, 2)]
        public void Matches_OtherSeedTeamOrSlot_False(string seed, int team, int slot)
        {
            Assert.False(SeedA.Matches(new SeedBinding(seed, team, slot, "Player1")));
        }

        [Fact]
        public void Matches_Null_False()
        {
            Assert.False(SeedA.Matches(null));
        }

        [Fact]
        public void Describe_NamesBindingAndCounts()
        {
            var state = new SaveApState { Binding = SeedA, Goal = GoalStatus.Sent, ReceivedIndex = 3 };
            state.SentChecks.Add("Upgrade Slot 1");

            var text = state.Describe();

            Assert.Contains("seed-a", text);
            Assert.Contains("Player1", text);
            Assert.Contains("1 sent check", text);
            Assert.Contains("goal sent", text);
            Assert.Contains("received index 3", text);
            Assert.Contains("unbound", new SaveApState().Describe());
            Assert.Contains("unknown", SaveApState.UnreadableFile().Describe());
        }
    }

    public class BindingDecisionTests
    {
        private static readonly SeedBinding SeedA = new SeedBinding("seed-a", 0, 1, "Player1");

        [Fact]
        public void Unbound_MapMatches_Binds()
        {
            Assert.Equal(BindingOutcome.Bind, BindingDecision.Decide(new SaveApState(), SeedA, mapMatches: true).Outcome);
        }

        [Fact]
        public void Unbound_MapMismatch_Skips()
        {
            Assert.Equal(BindingOutcome.Skip, BindingDecision.Decide(new SaveApState(), SeedA, mapMatches: false).Outcome);
        }

        [Fact]
        public void BoundToSameSlot_OtherSlotName_Matches()
        {
            var state = new SaveApState { Binding = new SeedBinding("seed-a", 0, 1, "Old name") };

            Assert.Equal(BindingOutcome.Matches, BindingDecision.Decide(state, SeedA, mapMatches: true).Outcome);
        }

        [Fact]
        public void BoundToAnotherSeed_Refuses_NamesTheBinding()
        {
            var state = new SaveApState { Binding = new SeedBinding("seed-b", 0, 1, "Player1") };

            var decision = BindingDecision.Decide(state, SeedA, mapMatches: true);

            Assert.Equal(BindingOutcome.Refuse, decision.Outcome);
            Assert.Equal("bound to seed-b/1", decision.Reason);
        }

        [Fact]
        public void BoundToAnotherSlot_Refuses()
        {
            var state = new SaveApState { Binding = new SeedBinding("seed-a", 0, 2, "Player2") };

            Assert.Equal(BindingOutcome.Refuse, BindingDecision.Decide(state, SeedA, mapMatches: true).Outcome);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnknownBinding_Refuses_EvenWithMapMismatch(bool mapMatches)
        {
            var decision = BindingDecision.Decide(SaveApState.UnreadableFile(), SeedA, mapMatches);

            Assert.Equal(BindingOutcome.Refuse, decision.Outcome);
            Assert.Equal("state file unreadable", decision.Reason);
        }
    }
}
