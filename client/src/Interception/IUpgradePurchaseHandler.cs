using Game;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// Decides what a player's system-upgrade purchase does. The Archipelago client will provide
    /// a handler that sends a location check; the patches don't need to change for that.
    /// </summary>
    internal interface IUpgradePurchaseHandler
    {
        /// <summary>
        /// Called before the game applies a validated purchase. Return true if the handler took care
        /// of the purchase (the game's own unlock is skipped), false to let the game unlock normally.
        /// </summary>
        bool OnPurchase(ResearchController.ResearchItem item);
    }
}
