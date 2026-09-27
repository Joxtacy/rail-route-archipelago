using Game.Context;

namespace RailRouteArchipelago
{
    /// <summary>In-game side notifications: dismissable and not saved with the game.</summary>
    internal static class Notify
    {
        public static void Side(string text)
        {
            var notifications = Ctx.Deps?.NotificationController;
            if (notifications == null)
            {
                Log.Warn("Notification not shown (no notification controller): " + text);
                return;
            }
            notifications.CreateSideNotification().Text(text).CanBeDismissed().NotSaved();
        }
    }
}
