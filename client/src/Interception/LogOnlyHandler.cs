using Game;

namespace RailRouteArchipelago.Interception
{
    /// <summary>Intercept mode off: purchases behave as in the unmodded game and are only logged.</summary>
    internal sealed class LogOnlyHandler : IUpgradePurchaseHandler
    {
        public bool OnPurchase(ResearchController.ResearchItem item)
        {
            Log.Info("Upgrade purchased: " + UpgradeInterception.Describe(item));
            return false;
        }

        public bool IsBlocked(ResearchController.ResearchItem item) => false;
    }
}
