using Game;
using Game.Context;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Whether a DLC is active in the current level: owned and enabled in the level's start settings.
    /// The same test SystemUpgradesPage.InstantiateUpgrades uses to hide upgrades.
    /// </summary>
    internal static class LevelDlc
    {
        public static bool ExpectDelaysActive(IControllers deps) =>
            deps.ModController.IsDlcOwned(ModController.Dlc.ExpectDelays)
            && deps.LevelController.StartSettings != null
            && deps.LevelController.StartSettings.ExpectDelaysEnabled;

        public static bool HappyPassengersActive(IControllers deps) =>
            deps.ModController.IsDlcOwned(ModController.Dlc.HappyPassengers)
            && deps.LevelController.StartSettings != null
            && deps.LevelController.StartSettings.HappyPassengersEnabled;
    }
}
