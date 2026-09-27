using System.Threading.Tasks;
using Game;
using Game.Context;
using Game.Mod;
using RailRouteArchipelago.Interception;
using RailRouteArchipelago.Net;
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
            // Before anything can load an Archipelago.MultiClient.Net type.
            NewtonsoftBinding.Register();
            Log.Info("Enabled.");
            NewtonsoftBinding.LogSpike();
            UpgradeInterception.Configure();
            PatchManager.Apply();
            if (PatchManager.Applied && !debugKeyInstalled)
            {
                DebugReceiveKey.Install();
                debugKeyInstalled = true;
            }
            if (PatchManager.Applied)
            {
                ArchipelagoPump.Install();
                // Subscribed once here so the first level's grants are seen; OnContextChanged re-attaches
                // only if a new context brings a different EventManager.
                GameGrantObserver.Attach(Ctx.Deps?.EventManager);
            }
            await Task.Yield();
        }

        public override async Task OnDisable()
        {
            Unsubscribe();
            GameGrantObserver.Detach();
            PatchManager.Remove();
            await Task.Yield();
        }

        public override async Task OnContextChanged(IControllers dependencies)
        {
            Log.Info("Context changed: " + dependencies.CurrentMode);
            if (PatchManager.Applied)
            {
                GameGrantObserver.Attach(dependencies.EventManager);
            }
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
