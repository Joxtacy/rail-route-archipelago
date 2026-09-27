using System.Linq;
using Game.Context;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Logged at each Endless level load in intercept mode: every upgrade's slot state, the level's storage
    /// and the runtime tier and star thresholds. Answers "does this map override the upgrade set, tiers or
    /// DLC source?" from Player.log, and shows what changed after a game update.
    /// </summary>
    internal static class LevelDiagnostics
    {
        public static void Log(IControllers deps)
        {
            var level = deps.LevelController.CurrentLevel;
            var definition = level?.LevelDefinition;
            RailRouteArchipelago.Log.Info("Level loaded: " + definition?.Uuid + ", storage " + definition?.Storage
                + ", Expect Delays " + (LevelDlc.ExpectDelaysActive(deps) ? "on" : "off")
                + ", Happy Passengers " + (LevelDlc.HappyPassengersActive(deps) ? "on" : "off"));

            var research = deps.ResearchController;
            foreach (var item in research.ResearchItems)
            {
                RailRouteArchipelago.Log.Info("Upgrade " + item.Id + ": researched " + item.Researched + ", locked " + item.Locked
                    + (item.Locked ? " (" + item.LockedReason + ")" : "")
                    + ", game grant " + EffectState.GrantedIds.Contains(item.Id));
            }

            var tiers = research.UpgradeTiersConfigurations;
            RailRouteArchipelago.Log.Info("Tier thresholds: " + (tiers == null ? "(none)" : string.Join(", ", tiers.Values
                .Select(t => t.ResearchType + " " + t.Tier1UnlockThreshold + "/" + t.Tier2UnlockThreshold + "/" + t.Tier3UnlockThreshold))));

            var rewards = research.ThroughputRewards;
            RailRouteArchipelago.Log.Info("Throughput rewards: " + (rewards == null ? "(none)" : string.Join(", ", rewards
                .Select(r => r.RequiredThroughputType + " " + r.RequiredThroughput + " → "
                    + (r.RewardedPointsType.HasValue ? r.RewardedPointsAmount + " " + r.RewardedPointsType + " points" : "")
                    + (r.RewardedStarType.HasValue ? r.RewardedStarType + " star" : "")
                    + (r.Granted ? " (granted)" : "")))));
        }
    }
}
