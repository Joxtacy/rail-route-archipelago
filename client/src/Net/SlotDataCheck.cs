using System;
using System.Collections.Generic;
using Game.Context;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Net
{
    /// <summary>
    /// Compares the slot data with the loaded level: the map's level UUID and whether each DLC is owned and
    /// enabled in the start settings. A mismatch is logged and shown, but the client stays connected.
    /// A map mismatch also blocks checks: a save from another map must not check this seed's locations.
    /// </summary>
    internal static class SlotDataCheck
    {
        /// <summary>Returns true when the slot data names another map than the loaded level's.</summary>
        public static bool Run(Dictionary<string, object> slotData)
        {
            if (slotData == null)
            {
                Log.Warn("Slot data missing; the level isn't checked against the seed");
                return false;
            }
            var deps = Ctx.Deps;
            var mismatches = 0;
            var mapMismatch = false;

            var levelMap = deps.LevelController.CurrentLevel?.LevelDefinition?.Uuid;
            if (TryGetString(slotData, "map", out var map) && !string.Equals(map, levelMap, StringComparison.Ordinal))
            {
                Log.Warn("Slot data mismatch: expected map " + map + ", level has " + (levelMap ?? "(none)"));
                mismatches++;
                mapMismatch = true;
            }
            mismatches += CheckBool(slotData, "expect_delays", LevelDlc.ExpectDelaysActive(deps));
            mismatches += CheckBool(slotData, "happy_passengers", LevelDlc.HappyPassengersActive(deps));

            if (mismatches > 0)
            {
                Notify.Side("This level doesn't match the Archipelago seed" + (mapMismatch ? " – checks are not sent" : ""));
            }
            return mapMismatch;
        }

        private static int CheckBool(Dictionary<string, object> slotData, string key, bool actual)
        {
            if (!TryGetString(slotData, key, out var text))
            {
                return 0;
            }
            if (!bool.TryParse(text, out var expected))
            {
                Log.Warn("Slot data " + key + " is not a boolean: " + text);
                return 0;
            }
            if (expected == actual)
            {
                return 0;
            }
            Log.Warn("Slot data mismatch: expected " + key + " " + expected + ", level has " + actual);
            return 1;
        }

        private static bool TryGetString(Dictionary<string, object> slotData, string key, out string value)
        {
            value = null;
            if (!slotData.TryGetValue(key, out var raw) || raw == null)
            {
                Log.Warn("Slot data has no " + key);
                return false;
            }
            value = Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
    }
}
