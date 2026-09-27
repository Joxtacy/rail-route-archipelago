using Game;
using Game.Context;
using RailRouteArchipelago.Core;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Intercept mode on: a purchase spends its points but records a pending location check instead of
    /// unlocking the upgrade. Stand-in for the future "send location check to Archipelago" handler.
    /// </summary>
    internal sealed class PendingCheckHandler : IUpgradePurchaseHandler
    {
        public PendingChecks Pending { get; } = new PendingChecks();

        public bool OnPurchase(ResearchController.ResearchItem item)
        {
            // Mirrors UnlockUpgradeCommand.Run, minus ResearchController.CompleteResearch.
            var wallet = Ctx.Deps.GameControllers.Wallet;
            wallet.AddResearchPointsPrimary(-item.PrimaryPointsNeeded);
            wallet.AddResearchPointsSecondary(-item.SecondaryPointsNeeded);
            wallet.AddResearchPointsTertiary(-item.TertiaryPointsNeeded);

            Pending.Add(item.Id);
            Log.Info("Check recorded: " + UpgradeInterception.Describe(item) + ". Pending checks: " + Pending.Count);
            Ctx.Deps.NotificationController.CreateSideNotification()
                .Text("Check sent: " + UpgradeInterception.DisplayName(item))
                .CanBeDismissed()
                .NotSaved();
            return true;
        }

        public bool IsBlocked(ResearchController.ResearchItem item) => Pending.Contains(item.Id);
    }
}
