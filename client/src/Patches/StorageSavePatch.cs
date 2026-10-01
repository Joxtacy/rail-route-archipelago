using Game;
using Game.Level;
using HarmonyLib;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Patches
{
    /// <summary>
    /// Every game save goes through StorageController.Save, called in the same main-thread step that captured
    /// the SavedGame. Writing the level's Archipelago state here snapshots both at the same moment.
    /// </summary>
    [HarmonyPatch(typeof(StorageController), nameof(StorageController.Save), typeof(string), typeof(SavedGame))]
    internal static class StorageSavePatch
    {
        private static void Prefix(string saveName)
        {
            if (SplitFlags.Active)
            {
                SaveStateStore.WriteFor(saveName);
            }
        }
    }
}
