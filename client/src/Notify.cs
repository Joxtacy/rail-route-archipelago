using Game.Context;
using Game.Hud.Notification;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago
{
    /// <summary>
    /// In-game side notifications: dismissable and not saved with the game. Routine ones (<see cref="Side"/>)
    /// expire after <c>notificationSeconds</c>; sticky ones (<see cref="Sticky"/>) stay until dismissed.
    /// At most <c>notificationLimit</c> are shown, evicting the oldest routine ones first.
    /// </summary>
    internal static class Notify
    {
        // Only Destroyed means gone (dismissed, expired or cleared by the game). The game sets ToDestroy as soon as a
        // delayed dismiss starts counting down, so an expiring notification has it for its whole time on screen.
        private static readonly NotificationBudget<Notification> Budget =
            new NotificationBudget<Notification>(n => n.Destroyed);

        /// <summary>A routine notification that expires.</summary>
        public static void Side(string text) => Show(text, sticky: false);

        /// <summary>A warning or the goal: never expires and is never evicted by the limit.</summary>
        public static void Sticky(string text) => Show(text, sticky: true);

        private static void Show(string text, bool sticky)
        {
            var notifications = Ctx.Deps?.NotificationController;
            if (notifications == null)
            {
                Log.Warn("Notification not shown (no notification controller): " + text);
                return;
            }
            var settings = UpgradeInterception.Settings;
            var notification = notifications.CreateSideNotification().Text(text).CanBeDismissed().NotSaved();
            var seconds = settings.NotificationSecondsOrDefault;
            if (!sticky && seconds > 0)
            {
                // The game's own delayed dismiss: the timer starts once the item is on screen and runs in real
                // seconds. Dismiss would also swap in DoneStatusText, but the mod never sets one.
                notification.Dismiss(seconds, playSuccessSound: false);
            }
            foreach (var evicted in Budget.Add(notification, sticky, settings.NotificationLimitOrDefault))
            {
                // Its NotificationItem clears itself next frame; a still-queued one is skipped by the side panel.
                evicted.Destroyed = true;
            }
        }
    }
}
