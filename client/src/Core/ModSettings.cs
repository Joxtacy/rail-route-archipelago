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
        public const int DefaultNotificationSeconds = 10;
        public const int DefaultNotificationLimit = 5;

        [JsonProperty("interceptUpgradePurchases")]
        public bool InterceptUpgradePurchases { get; set; }

        /// <summary>Archipelago server address; see <see cref="ServerAddress"/> for the accepted forms.</summary>
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("slot")]
        public string Slot { get; set; }

        /// <summary>Optional room password. Only ever passed to the login, never logged.</summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// Test only: lowers the Endless-complete star's threshold on a level that hasn't earned it yet.
        /// The game saves the lowered threshold with the level, so use it on throwaway saves.
        /// </summary>
        [JsonProperty("debugEndlessCompleteThreshold")]
        public int? DebugEndlessCompleteThreshold { get; set; }

        /// <summary>Seconds before a routine notification expires; 0 turns expiry off. See <see cref="NotificationSecondsOrDefault"/>.</summary>
        [JsonProperty("notificationSeconds")]
        public int? NotificationSeconds { get; set; }

        /// <summary>Most mod notifications on screen; 0 turns the limit off. See <see cref="NotificationLimitOrDefault"/>.</summary>
        [JsonProperty("notificationLimit")]
        public int? NotificationLimit { get; set; }

        /// <summary>The expiry in seconds, with a missing or negative value meaning the default. 0 means no expiry.</summary>
        public int NotificationSecondsOrDefault => OrDefault(NotificationSeconds, DefaultNotificationSeconds);

        /// <summary>The notification limit, with a missing or negative value meaning the default. 0 means no limit.</summary>
        public int NotificationLimitOrDefault => OrDefault(NotificationLimit, DefaultNotificationLimit);

        /// <summary>Both a server and a slot are set (the address may still be invalid).</summary>
        public bool HasConnection => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Slot);

        /// <summary>The password, or null when it is missing or empty.</summary>
        public string PasswordOrNull => string.IsNullOrEmpty(Password) ? null : Password;

        /// <summary>Log text for the settings. Says whether a password is set, never what it is.</summary>
        public override string ToString() =>
            "intercept " + (InterceptUpgradePurchases ? "on" : "off")
            + ", server " + (string.IsNullOrWhiteSpace(Server) ? "none" : Server.Trim())
            + ", slot " + (string.IsNullOrWhiteSpace(Slot) ? "none" : Slot)
            + ", password " + (PasswordOrNull == null ? "none" : "set")
            + ", notifications " + (NotificationSecondsOrDefault == 0 ? "off" : NotificationSecondsOrDefault + "s")
            + ", limit " + (NotificationLimitOrDefault == 0 ? "off" : NotificationLimitOrDefault.ToString())
            + (DebugEndlessCompleteThreshold.HasValue ? ", debug endless-complete threshold " + DebugEndlessCompleteThreshold.Value : "");

        private static int OrDefault(int? value, int fallback) => value.HasValue && value.Value >= 0 ? value.Value : fallback;

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
