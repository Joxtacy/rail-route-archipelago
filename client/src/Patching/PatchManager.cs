using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace RailRouteArchipelago.Patching
{
    /// <summary>
    /// Owns the mod's Harmony instance. Patches are applied at most once per process and
    /// all-or-nothing: if any patch fails, every patch of ours is removed and the mod is degraded.
    /// </summary>
    internal static class PatchManager
    {
        public const string HarmonyId = "railroute.archipelago";

        private static Harmony harmony;

        public static bool Applied { get; private set; }

        public static bool Degraded { get; private set; }

        public static string PlatformDescription =>
            "game " + Application.version
            + ", platform " + Application.platform
            + ", os '" + SystemInfo.operatingSystem + "'"
            + ", arch " + RuntimeInformation.ProcessArchitecture
            + ", Harmony " + typeof(Harmony).Assembly.GetName().Version;

        public static void Apply()
        {
            if (Applied || Degraded)
            {
                return;
            }
            Log.Info("Runtime: " + PlatformDescription);
            harmony = new Harmony(HarmonyId);
            // Patch class by class (instead of PatchAll) so a failure names the game method it targets.
            var failures = 0;
            foreach (var patchType in AccessTools.GetTypesFromAssembly(typeof(PatchManager).Assembly))
            {
                var attributes = HarmonyMethodExtensions.GetFromType(patchType);
                if (attributes == null || attributes.Count == 0)
                {
                    continue;
                }
                var target = HarmonyMethod.Merge(attributes);
                var targetName = target.declaringType?.FullName + "." + target.methodName;
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    Log.Info("Patched " + targetName + ": OK");
                }
                catch (Exception e)
                {
                    failures++;
                    Log.Error("Patch " + patchType.Name + " failed for " + targetName + ": " + (e.InnerException?.Message ?? e.Message) + "\n" + e);
                }
            }

            if (failures == 0)
            {
                Applied = true;
                return;
            }
            Degraded = true;
            Log.Error("Patching failed (" + failures + " patch(es)) on " + PlatformDescription + ". All patches removed; Archipelago features disabled.");
            try
            {
                harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception unpatchError)
            {
                Log.Error("Unpatching after failure also failed: " + unpatchError);
            }
        }

        public static void Remove()
        {
            if (!Applied)
            {
                return;
            }
            harmony.UnpatchAll(HarmonyId);
            Applied = false;
            Log.Info("Patches removed.");
        }
    }
}
