using Game;
using Game.Context;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Net;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Intercept mode on: in split mode a purchase spends its points and marks the slot as bought
    /// (Researched), recording a location check instead of granting the upgrade's effect. Repeat
    /// purchases are blocked by the game's own Validate, and bought slots persist with the save.
    /// Outside split mode (tutorials, story, other level types) the game unlocks normally.
    /// </summary>
    internal sealed class SlotPurchaseHandler : IUpgradePurchaseHandler
    {
        public bool OnPurchase(ResearchController.ResearchItem item)
        {
            if (!SplitFlags.Active)
            {
                Log.Info("Upgrade purchased: " + UpgradeInterception.Describe(item));
                return false;
            }
            var deps = Ctx.Deps;
            // Mirrors UnlockUpgradeCommand.Run, minus ResearchController.CompleteResearch.
            var wallet = deps.GameControllers.Wallet;
            wallet.AddResearchPointsPrimary(-item.PrimaryPointsNeeded);
            wallet.AddResearchPointsSecondary(-item.SecondaryPointsNeeded);
            wallet.AddResearchPointsTertiary(-item.TertiaryPointsNeeded);
            item.Researched = true;

            string checkName;
            if (LocationNames.TryGet(item.Id, out var locationName))
            {
                checkName = locationName;
                Log.Info("Check recorded: " + item.Id + " → " + locationName + " " + UpgradeInterception.Details(item));
                // Does nothing while offline: the resend on the next connect covers it.
                CheckSender.Send(locationName);
            }
            else
            {
                checkName = UpgradeInterception.DisplayName(item);
                Log.Warn("Check recorded: " + item.Id + " has no location name " + UpgradeInterception.Details(item) + ". Slot bought anyway.");
            }
            Notify.Side("Check sent: " + checkName);
            // Refreshes the panel, slot buttons, tier indicators and child slots as after a normal purchase.
            EffectState.RaiseResearchCompleted(deps.EventManager, item);
            return true;
        }
    }
}
