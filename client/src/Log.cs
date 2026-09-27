using UnityEngine;

namespace RailRouteArchipelago
{
    /// <summary>Game-log output with a common prefix so our lines are easy to grep in Player.log.</summary>
    internal static class Log
    {
        private const string Prefix = "[Archipelago] ";

        public static void Info(string message) => Debug.Log(Prefix + message);

        public static void Warn(string message) => Debug.LogWarning(Prefix + message);

        public static void Error(string message) => Debug.LogError(Prefix + message);
    }
}
