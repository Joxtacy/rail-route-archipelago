using Game;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>
    /// CanResearch(ResearchItem) drives both the upgrade panel's Install button and
    /// UnlockUpgradeCommand.Validate, so blocking here hides the button and rejects the command.
    /// </summary>
    [HarmonyPatch(typeof(ResearchController), nameof(ResearchController.CanResearch), typeof(ResearchController.ResearchItem))]
    internal static class CanResearchPatch
    {
        private static void Postfix(ResearchController.ResearchItem item, ref bool __result)
        {
            if (__result && item != null && UpgradeInterception.Handler.IsBlocked(item))
            {
                __result = false;
            }
        }
    }
}
