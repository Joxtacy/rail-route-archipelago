using Game.Level;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>
    /// Every save delete, including pruned autosaves, goes through DeleteSave; the state file goes with it.
    /// Gated on intercept mode, not split mode: the main menu's save list deletes saves too.
    /// </summary>
    [HarmonyPatch(typeof(StorageController), nameof(StorageController.DeleteSave))]
    internal static class StorageDeleteSavePatch
    {
        private static void Postfix(StorageController.SaveFile saveFile)
        {
            if (UpgradeInterception.InterceptEnabled && saveFile != null)
            {
                SaveStateStore.Delete(saveFile.FileName);
            }
        }
    }
}
