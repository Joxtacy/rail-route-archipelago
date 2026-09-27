using Game;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Records upgrades the game unlocks itself (level configuration, "unlock all", tutorial buttons) by
    /// listening to ResearchCompleted, so CompleteResearch stays unpatched. Events the mod raises itself
    /// are skipped. Re-attaches when a context brings a different EventManager.
    /// </summary>
    internal static class GameGrantObserver
    {
        private static IEventManager attached;

        public static void Attach(IEventManager eventManager)
        {
            if (ReferenceEquals(eventManager, attached))
            {
                return;
            }
            Detach();
            if (eventManager == null)
            {
                return;
            }
            eventManager.ResearchCompleted += OnResearchCompleted;
            attached = eventManager;
        }

        public static void Detach()
        {
            if (attached == null)
            {
                return;
            }
            attached.ResearchCompleted -= OnResearchCompleted;
            attached = null;
        }

        private static void OnResearchCompleted(ResearchController.ResearchItem item)
        {
            if (EffectState.RaisingOwnEvent || item == null || !UpgradeInterception.InterceptEnabled)
            {
                return;
            }
            EffectState.RecordGrant(item.Id);
            Log.Info("Game grant: " + item.Id);
        }
    }
}
