using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Game;
using Game.Context;
using Game.Level;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Net;
using ResearchType = Game.ResearchController.ResearchType;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Reports the Archipelago goal when the level's Endless-complete star is awarded live. StarAwarded
    /// doesn't name the star, so the handler reads the endless-complete reward and counts only its
    /// false-to-true move within the level. After a login it sends a star the save has, when the save's goal
    /// state allows it. <see cref="GoalState"/> decides over <see cref="SaveStateStore.Current"/>, this class
    /// executes. Re-attaches when a context brings a different EventManager.
    /// </summary>
    internal static class GoalWatcher
    {
        private static IEventManager attached;

        private static Game.Level.Level baselineLevel;
        private static bool baselineGranted;

        public static void Attach(IEventManager eventManager)
        {
            if (ReferenceEquals(eventManager, attached))
            {
                return;
            }
            Detach();
            if (eventManager == null)
            {
                return;
            }
            eventManager.StarAwarded += OnStarAwarded;
            attached = eventManager;
        }

        public static void Detach()
        {
            if (attached == null)
            {
                return;
            }
            attached.StarAwarded -= OnStarAwarded;
            attached = null;
        }

        /// <summary>The game's own predicate (GetCurrentStars): Green, no star type, no points. Missing means not granted.</summary>
        public static bool EndlessCompleteGranted(IResearchController research) =>
            research?.ThroughputRewards?.FirstOrDefault(IsEndlessComplete)?.Granted ?? false;

        private static bool IsEndlessComplete(ResearchController.ThroughputReward reward) =>
            reward.RequiredThroughputType == ResearchType.Green && !reward.RewardedStarType.HasValue && !reward.RewardedPointsAmount.HasValue;

        /// <summary>Records whether the loaded level (with its save restored) already has the star.</summary>
        public static void OnLevelLoaded(Game.Level.Level level)
        {
            var research = Ctx.Deps.ResearchController;
            baselineLevel = level;
            baselineGranted = EndlessCompleteGranted(research);
            var threshold = UpgradeInterception.Settings.DebugEndlessCompleteThreshold;
            if (threshold > 0 && !baselineGranted && research?.ThroughputRewards != null
                && research.ThroughputRewards.Any(IsEndlessComplete))
            {
                // Replaces the reward with a new, ungranted one, hence only when it isn't granted.
                research.ModifyThroughputStarReward(threshold.Value);
                Log.Info("Debug: endless-complete threshold set to " + threshold.Value);
            }
        }

        /// <summary>After a login that wasn't refused: sends the star the save has and hasn't sent.</summary>
        public static void OnLogin(ApSession session)
        {
            var state = SaveStateStore.Current;
            var granted = EndlessCompleteGranted(Ctx.Deps.ResearchController);
            var decision = GoalState.OnLogin(state.Goal, session.Bound, !session.ChecksBlocked, session.GoalSupported, granted);
            if (decision.Send)
            {
                Send(session, state, " (from the save)");
                return;
            }
            if (granted && decision.Refusal != GoalRefusal.AlreadySent)
            {
                Log.Warn("Goal not sent: " + decision.Reason);
            }
        }

        /// <summary>Hands the goal status to the socket and marks the save's goal sent. A failure makes it pending again.</summary>
        private static void Send(ApSession session, SaveApState state, string how)
        {
            state.Goal = GoalState.MarkSent();
            session.SendPacket(new StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal }, "the goal", message =>
            {
                state.Goal = GoalState.MarkFailed();
                Log.Error("Sending the goal failed: " + message + ". It's sent after the next login.");
            });
            Log.Info("Goal sent: Endless complete on " + CurrentMap + how);
            Notify.Side("Archipelago goal complete");
        }

        public static string CurrentMap => Ctx.Deps?.LevelController?.CurrentLevel?.LevelDefinition?.Uuid;

        private static void OnStarAwarded(LevelDefinition definition)
        {
            if (!SplitFlags.Active)
            {
                return;
            }
            var level = Ctx.Deps.LevelController.CurrentLevel;
            if (!ReferenceEquals(level, baselineLevel))
            {
                Log.Warn("Star awarded before the level's goal baseline was recorded; not counted as the goal");
                return;
            }
            var granted = EndlessCompleteGranted(Ctx.Deps.ResearchController);
            var reachedLive = granted && !baselineGranted;
            baselineGranted = granted;
            if (!reachedLive)
            {
                return;
            }

            var state = SaveStateStore.Current;
            var session = ArchipelagoPump.Session;
            var connected = session != null && session.IsConnected;
            var decision = GoalState.OnReachedLive(state.Goal, connected, connected && session.Bound,
                connected && !session.ChecksBlocked, connected && session.GoalSupported);
            if (decision.Send)
            {
                Send(session, state, "");
            }
            else
            {
                state.Goal = decision.State;
                Log.Warn("Goal reached, not sent: " + decision.Reason);
            }
        }
    }
}
