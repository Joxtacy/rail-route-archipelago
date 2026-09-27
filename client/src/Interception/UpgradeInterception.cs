using System.IO;
using Game;
using Game.Context;
using RailRouteArchipelago.Core;

namespace RailRouteArchipelago.Interception
{
    /// <summary>Holds the active purchase handler, chosen from the settings file at mod enable time.</summary>
    internal static class UpgradeInterception
    {
        public static IUpgradePurchaseHandler Handler { get; private set; } = new LogOnlyHandler();

        public static bool InterceptEnabled => Handler is PendingCheckHandler;

        public static void Configure()
        {
            var modDir = Path.GetDirectoryName(typeof(UpgradeInterception).Assembly.Location) ?? ".";
            var path = Path.Combine(modDir, ModSettings.FileName);
            if (!ModSettings.TryLoad(path, out var settings, out var error))
            {
                Log.Warn(error.TrimEnd('.') + ". Using defaults (intercept mode off).");
            }
            Handler = settings.InterceptUpgradePurchases ? new PendingCheckHandler() : new LogOnlyHandler();
            Log.Info("Intercept mode: " + (InterceptEnabled ? "on" : "off") + " (settings: " + path + ")");
        }

        /// <summary>
        /// Grants the most recently intercepted upgrade the way an Archipelago item receipt will:
        /// through ResearchController.CompleteResearch, which is never intercepted.
        /// </summary>
        public static void GrantMostRecentPending()
        {
            if (!(Handler is PendingCheckHandler pendingHandler))
            {
                return;
            }
            var id = pendingHandler.Pending.MostRecent;
            if (id == null)
            {
                Log.Info("Debug grant: no pending checks.");
                return;
            }
            var item = FindItem(id);
            pendingHandler.Pending.Remove(id);
            if (item == null)
            {
                Log.Warn("Debug grant: upgrade " + id + " not found in this level.");
                return;
            }
            Ctx.Deps.ResearchController.CompleteResearch(item.Research, ignoreLockedState: true);
            Log.Info("Debug grant: " + Describe(item) + ", researched=" + item.Researched);
        }

        private static ResearchController.ResearchItem FindItem(string id)
        {
            foreach (var item in Ctx.Deps.ResearchController.ResearchItems)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }
            return null;
        }

        public static string DisplayName(ResearchController.ResearchItem item) =>
            string.IsNullOrEmpty(item.Title) ? item.Id : item.Title;

        public static string Describe(ResearchController.ResearchItem item) =>
            item.Id + " (" + item.Type + ", tier " + item.Tier
            + ", cost " + item.PrimaryPointsNeeded + "/" + item.SecondaryPointsNeeded + "/" + item.TertiaryPointsNeeded + ")";
    }
}
