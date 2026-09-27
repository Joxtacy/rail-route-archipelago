using System.IO;
using Game;
using RailRouteArchipelago.Core;

namespace RailRouteArchipelago.Interception
{
    /// <summary>Holds the active purchase handler, chosen from the settings file at mod enable time.</summary>
    internal static class UpgradeInterception
    {
        public static IUpgradePurchaseHandler Handler { get; private set; } = new LogOnlyHandler();

        public static bool InterceptEnabled => Handler is SlotPurchaseHandler;

        public static void Configure()
        {
            var modDir = Path.GetDirectoryName(typeof(UpgradeInterception).Assembly.Location) ?? ".";
            var path = Path.Combine(modDir, ModSettings.FileName);
            if (!ModSettings.TryLoad(path, out var settings, out var error))
            {
                Log.Warn(error.TrimEnd('.') + ". Using defaults (intercept mode off).");
            }
            Handler = settings.InterceptUpgradePurchases ? new SlotPurchaseHandler() : new LogOnlyHandler();
            Log.Info("Intercept mode: " + (InterceptEnabled ? "on" : "off") + " (settings: " + path + ")");
        }

        public static string DisplayName(ResearchController.ResearchItem item) =>
            string.IsNullOrEmpty(item.Title) ? item.Id : item.Title;

        public static string Describe(ResearchController.ResearchItem item) => item.Id + " " + Details(item);

        /// <summary>"(Green, tier 1, cost 3/0/0)"</summary>
        public static string Details(ResearchController.ResearchItem item) =>
            "(" + item.Type + ", tier " + item.Tier
            + ", cost " + item.PrimaryPointsNeeded + "/" + item.SecondaryPointsNeeded + "/" + item.TertiaryPointsNeeded + ")";
    }
}
