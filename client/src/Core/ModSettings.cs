using System;
using System.IO;
using Newtonsoft.Json;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// Optional settings file next to the mod DLL. Anything missing or invalid falls back to defaults
    /// (intercept mode off) so a broken file never changes gameplay.
    /// </summary>
    public sealed class ModSettings
    {
        public const string FileName = "RailRouteArchipelago.settings.json";

        [JsonProperty("interceptUpgradePurchases")]
        public bool InterceptUpgradePurchases { get; set; }

        /// <summary>
        /// Loads settings from <paramref name="path"/>. Returns false (with defaults and an error message)
        /// only when the file exists but cannot be read or parsed; a missing file is not an error.
        /// </summary>
        public static bool TryLoad(string path, out ModSettings settings, out string error)
        {
            settings = new ModSettings();
            error = null;
            if (!File.Exists(path))
            {
                return true;
            }
            try
            {
                settings = JsonConvert.DeserializeObject<ModSettings>(File.ReadAllText(path)) ?? new ModSettings();
                return true;
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                settings = new ModSettings();
                error = "Could not read " + path + ": " + e.Message;
                return false;
            }
        }
    }
}
