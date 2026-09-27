using Game;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>In split mode, binary upgrade effects come from received items and game grants, not slot flags.</summary>
    [HarmonyPatch(typeof(ResearchController), nameof(ResearchController.HasResearched), typeof(ResearchController.Research))]
    internal static class HasResearchedPatch
    {
        private static bool Prefix(ResearchController __instance, ResearchController.Research research, ref bool __result)
        {
            if (!SplitFlags.Active)
            {
                return true;
            }
            __result = EffectState.HasResearched(__instance, research);
            return false;
        }
    }
}
