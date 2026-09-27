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
    /// false-to-true move within the level; a star restored from a save never counts. <see cref="GoalState"/>
    /// decides, this class executes. Re-attaches when a context brings a different EventManager.
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

        /// <summary>After a login: sends a goal pending for this map, or points at Shift+F10 for a star from a save.</summary>
        public static void OnLogin(ApSession session)
        {
            var map = CurrentMap;
            var granted = EndlessCompleteGranted(Ctx.Deps.ResearchController);
            var decision = GoalState.OnLogin(map, !session.ChecksBlocked, session.GoalSupported, granted);
            if (decision.Send)
            {
                Send(session, map, " (pending since offline)");
                return;
            }
            if (decision.Refusal != GoalRefusal.NotPending && decision.Refusal != GoalRefusal.AlreadySent)
            {
                Log.Warn("Pending goal not sent: " + decision.Reason);
            }
            if (granted && !GoalState.Sent && !GoalState.IsPendingFor(map))
            {
                const string hint = "Endless-complete star found in the save; the goal isn't sent automatically. Press Shift+F10 to send it.";
                Log.Info(hint);
                Notify.Side(hint);
            }
        }

        /// <summary>Hands the goal status to the socket. A failure makes the goal pending again for the map.</summary>
        public static void Send(ApSession session, string map, string how)
        {
            GoalState.MarkSent();
            session.SendPacket(new StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal }, "the goal", message =>
            {
                GoalState.MarkFailed(map);
                Log.Error("Sending the goal failed: " + message + ". It's sent after the next login on this map.");
            });
            Log.Info("Goal sent: Endless complete on " + map + how);
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

            var map = CurrentMap;
            var session = ArchipelagoPump.Session;
            var connected = session != null && session.IsConnected;
            var decision = GoalState.OnReachedLive(map, connected, connected && !session.ChecksBlocked,
                connected && session.GoalSupported);
            if (decision.Send)
            {
                Send(session, map, "");
            }
            else
            {
                Log.Warn("Goal reached, not sent: " + decision.Reason);
            }
        }
    }
}
