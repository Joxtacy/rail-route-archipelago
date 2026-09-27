using HarmonyLib;
using Multiplayer.Commands.Game;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>
    /// UnlockUpgradeCommand.Run is the only path a player purchase takes (the upgrade panel's install
    /// button sends this command). Tutorials, story and grants call ResearchController.CompleteResearch directly.
    /// </summary>
    [HarmonyPatch(typeof(UnlockUpgradeCommand), nameof(UnlockUpgradeCommand.Run))]
    internal static class UnlockUpgradeCommandPatch
    {
        private static bool Prefix(UnlockUpgradeCommand __instance)
        {
            var handled = UpgradeInterception.Handler.OnPurchase(__instance.ResearchItem);
            return !handled;
        }
    }
}
