using System.Threading.Tasks;
using Game;
using Game.Context;
using Game.Mod;
using UnityEngine;
using Utils;

namespace RailRouteArchipelago
{
    /// <summary>
    /// Entry point. Game.ModController instantiates every exported IGameMod in
    /// mods/RailRouteArchipelago/RailRouteArchipelago.dll and calls OnContextChanged/OnEnable.
    /// </summary>
    public class ArchipelagoMod : AbstractMod
    {
        private const string LogPrefix = "[Archipelago] ";

        private IControllers deps;
        private bool subscribed;

        public override CachedLocalizedString Title => "Archipelago";

        public override CachedLocalizedString Description => "Archipelago multiworld randomizer client.";

        public override async Task OnEnable()
        {
            Debug.Log(LogPrefix + "Enabled. Game version " + Application.version + ", platform " + Application.platform);
            await Task.Yield();
        }

        public override async Task OnDisable()
        {
            Unsubscribe();
            await Task.Yield();
        }

        public override async Task OnContextChanged(IControllers dependencies)
        {
            Debug.Log(LogPrefix + "Context changed: " + dependencies.CurrentMode);
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
            Debug.Log(LogPrefix + "Level started: " + level.LevelDefinition.Uuid);
            deps.NotificationController.CreateSideNotification()
                .Text("Hello from Archipelago!")
                .CanBeDismissed()
                .NotSaved();
        }
    }
}
