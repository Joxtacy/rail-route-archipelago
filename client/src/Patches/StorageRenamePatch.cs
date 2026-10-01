using Game.Level;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>Rename (in the main menu's save list, so gated on intercept mode) moves the save to a new file name; the state file follows it.</summary>
    [HarmonyPatch(typeof(StorageController), nameof(StorageController.Rename))]
    internal static class StorageRenamePatch
    {
        private static void Postfix(StorageController.SaveFile oldSave, StorageController.SaveFile __result)
        {
            if (UpgradeInterception.InterceptEnabled && oldSave != null && __result != null)
            {
                SaveStateStore.Move(oldSave.FileName, __result.FileName);
            }
        }
    }
}
