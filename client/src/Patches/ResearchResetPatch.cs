using Game;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>
    /// ResearchController.Reset() clears every slot on each level clear/load (and in the editor and two
    /// mini-tutorials). Clearing the effect state here keeps slot and effect state resetting together.
    /// </summary>
    [HarmonyPatch(typeof(ResearchController), nameof(ResearchController.Reset))]
    internal static class ResearchResetPatch
    {
        private static void Postfix()
        {
            EffectState.Clear();
            Log.Info("Effect state reset");
        }
    }
}
