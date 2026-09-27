using System.Threading.Tasks;
using Game;
using Game.Context;
using Game.Mod;
using RailRouteArchipelago.Interception;
using RailRouteArchipelago.Patching;
using Utils;

namespace RailRouteArchipelago
{
    /// <summary>
    /// Entry point. Game.ModController instantiates every exported IGameMod in
    /// mods/RailRouteArchipelago/RailRouteArchipelago.dll and calls OnContextChanged/OnEnable.
    /// </summary>
    public class ArchipelagoMod : AbstractMod
    {
        private IControllers deps;
        private bool subscribed;
        private static bool debugKeyInstalled;

        public override CachedLocalizedString Title => "Archipelago";

        public override CachedLocalizedString Description => "Archipelago multiworld randomizer client.";

        public override async Task OnEnable()
        {
            Log.Info("Enabled.");
            UpgradeInterception.Configure();
            PatchManager.Apply();
            if (PatchManager.Applied && !debugKeyInstalled)
            {
                DebugGrantKey.Install();
                debugKeyInstalled = true;
            }
            await Task.Yield();
        }

        public override async Task OnDisable()
        {
            Unsubscribe();
            PatchManager.Remove();
            await Task.Yield();
        }

        public override async Task OnContextChanged(IControllers dependencies)
        {
            Log.Info("Context changed: " + dependencies.CurrentMode);
            if (dependencies.CurrentMode == GameMode.Play)
            {
                deps = dependencies;
                Subscribe();
            }
            await Task.Yield();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }
            deps.EventManager.BeforeLevelStarted += OnLevelStarted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || deps == null)
            {
                return;
            }
            deps.EventManager.BeforeLevelStarted -= OnLevelStarted;
            subscribed = false;
        }

        private void OnLevelStarted(Game.Level.Level level)
        {
            Log.Info("Level started: " + level.LevelDefinition.Uuid);
            if (PatchManager.Degraded)
            {
                deps.NotificationController.CreateSideNotification()
                    .Text("Archipelago mod is running degraded: patching failed, Archipelago features are unavailable. See Player.log.")
                    .CanBeDismissed()
                    .NotSaved();
            }
        }
    }
}
