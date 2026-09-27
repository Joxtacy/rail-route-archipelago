using Game;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>In split mode, levelled upgrade values come from received copies and game grants, not slot flags.</summary>
    [HarmonyPatch(typeof(ResearchController), nameof(ResearchController.ResearchedValue), typeof(ResearchController.Research))]
    internal static class ResearchedValuePatch
    {
        private static bool Prefix(ResearchController __instance, ResearchController.Research research, ref int __result)
        {
            if (!SplitFlags.Active || !EffectState.TryResearchedValue(__instance, research, out var value))
            {
                return true;
            }
            __result = value;
            return false;
        }
    }
}
