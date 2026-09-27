using System.Linq;
using Game;
using Game.Context;
using Game.Hud;
using Game.Hud.Notification;
using Game.Level;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Receiving an upgrade item in split mode: counts it and applies the upgrade's side effects the way
    /// ResearchController.CompleteResearch does, without ever writing any slot's Researched flag.
    /// The Archipelago client calls <see cref="Receive"/> with items resolved from AP item names; F9 calls it too.
    /// </summary>
    internal static class UpgradeReceiver
    {
        /// <summary>
        /// Receives the item for <paramref name="upgrade"/>: that upgrade, or for a levelled one a copy of
        /// its progressive item (the next level of its chain). <paramref name="silent"/> skips the unlock
        /// popup only (for the replay on connect); side effects and the UI refresh still happen. When a non-silent
        /// receive can't show the popup (the system upgrades menu is open), a side notification names the upgrade
        /// and <paramref name="sender"/> instead.
        /// </summary>
        public static void Receive(ResearchController.ResearchItem upgrade, bool silent = false, string sender = null)
        {
            if (!SplitFlags.Active)
            {
                Log.Warn("Item receive ignored outside split mode: " + upgrade.Id);
                return;
            }
            var deps = Ctx.Deps;
            var controller = (ResearchController)deps.ResearchController;
            var effectItem = EffectState.ResolveEffectItem(controller, upgrade);
            var count = EffectState.Received.Receive(EffectState.ItemKey(effectItem.Research));
            ApplySideEffects(deps, effectItem.Research);

            // Mirrors the tail of CompleteResearch.
            if (!silent)
            {
                if (deps.CurrentMode == GameMode.Play && deps.GameController.Loaded && !deps.MenuController.SystemUpgradesShown)
                {
                    ModalDialogUiController.CreateCustom<UnlockPopup>()?.Show(effectItem.Title, null, UnlockPopup.UnlockableType.SystemUpgrade, effectItem.IconCode);
                }
                else
                {
                    Notify.Side("Received " + UpgradeInterception.DisplayName(effectItem) + (sender == null ? "" : " from " + sender));
                }
            }
            EffectState.RaiseResearchCompleted(deps.EventManager, effectItem);
            Log.Info("Item received: " + effectItem.Id + " (" + UpgradeInterception.DisplayName(effectItem) + "), count " + count);
        }

        /// <summary>Mirrors CompleteResearch's side-effect switch (re-check after game updates, see FINDINGS.md).</summary>
        private static void ApplySideEffects(IControllers deps, ResearchController.Research research)
        {
            if (deps.GameControllers == null || deps.LevelController.CurrentLevel == null
                || deps.LevelController.CurrentLevel.LevelDefinition.ScoringModel == ScoringModel.Score)
            {
                return;
            }
            switch (research)
            {
                case ResearchController.Research.AutomationAutoAccept:
                    foreach (var station in deps.StationRepository.GetStations().Where(s => !s.IsWaypoint))
                    {
                        station.AutoAccept = true;
                    }
                    break;
                case ResearchController.Research.AutomationAutoReverse:
                    foreach (var station in deps.StationRepository.GetStations().Where(s => !s.IsWaypoint))
                    {
                        station.AutoReverse = true;
                    }
                    break;
                case ResearchController.Research.TrainAlerts:
                    deps.InterfaceController.SetBrakingTrainAlertPreference(enabled: true, forceUpdate: true);
                    deps.InterfaceController.SetStoppedTrainAlertPreference(enabled: true, forceUpdate: true);
                    deps.InterfaceController.SetArrivedTrainAlertPreference(enabled: true, forceUpdate: true);
                    break;
                case ResearchController.Research.ManualSignalRoutePreview:
                    deps.InterfaceController.SetManualSignalRoutePreview(enabled: true, forceUpdate: true);
                    break;
                case ResearchController.Research.SignallingSafety:
                    deps.InterfaceController.SetSignallingSafety(enabled: true, forceUpdate: true);
                    break;
            }
        }
    }
}
