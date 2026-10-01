using Game;
using Game.Context;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Interception;
using UnityEngine;

namespace RailRouteArchipelago.Net
{
    /// <summary>
    /// The main-thread side of the Archipelago connection. Each frame it runs the queued network events,
    /// then connects once per loaded Endless level (split mode and GameController.Loaded) and disconnects
    /// when that level is left, reloaded or split mode ends. Polling covers every load path (new game,
    /// saved map, restart), since every one of them sets Loaded false and then true.
    /// </summary>
    internal sealed class ArchipelagoPump : MonoBehaviour
    {
        private static readonly MainThreadQueue Queue = new MainThreadQueue();
        private static bool installed;

        private Game.Level.Level handledLevel;

        /// <summary>The current level's session, or null when offline.</summary>
        public static ApSession Session { get; private set; }

        public static void Install()
        {
            if (installed)
            {
                return;
            }
            var go = new GameObject("RailRouteArchipelago.ArchipelagoPump");
            DontDestroyOnLoad(go);
            go.AddComponent<ArchipelagoPump>();
            installed = true;
        }

        private void Update()
        {
            Queue.Drain(e => Log.Error("Archipelago event failed: " + e));

            var deps = Ctx.Deps;
            var level = deps?.LevelController?.CurrentLevel;
            if (!SplitFlags.Active || deps.GameController == null || !deps.GameController.Loaded || level == null)
            {
                EndLevel();
                return;
            }
            if (!ReferenceEquals(level, handledLevel))
            {
                EndLevel();
                StartLevel(deps, level);
            }
        }

        private void OnDestroy() => EndLevel();

        private void StartLevel(IControllers deps, Game.Level.Level level)
        {
            handledLevel = level;
            LevelDiagnostics.Log(deps);
            SaveStateStore.LoadFor(level, deps.GameController.LoadedSave);
            GoalWatcher.OnLevelLoaded(level);
            var settings = UpgradeInterception.Settings;
            if (!settings.HasConnection)
            {
                Log.Info("Archipelago offline: no server/slot configured");
                return;
            }
            if (!ServerAddress.TryParse(settings.Server, out var address))
            {
                Log.Warn("Archipelago offline: invalid server address \"" + settings.Server.Trim() + "\"");
                return;
            }
            Session = new ApSession(Queue, address, settings.Slot, settings.PasswordOrNull);
            Session.Start();
        }

        private void EndLevel()
        {
            handledLevel = null;
            if (Session == null)
            {
                return;
            }
            Session.Disconnect();
            Session = null;
        }
    }
}
