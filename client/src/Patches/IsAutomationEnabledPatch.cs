using Game;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>
    /// In split mode, automation is on once an EnablesAutomation upgrade is received or granted. The game
    /// also sets the flag when loading bought automation slots, so the getter is overridden, not the setter.
    /// </summary>
    [HarmonyPatch(typeof(ResearchController), nameof(ResearchController.IsAutomationEnabled), MethodType.Getter)]
    internal static class IsAutomationEnabledPatch
    {
        private static void Postfix(ResearchController __instance, ref bool __result)
        {
            if (SplitFlags.Active)
            {
                __result = EffectState.IsAutomationEnabled(__instance);
            }
        }
    }
}
