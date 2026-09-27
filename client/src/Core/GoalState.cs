namespace RailRouteArchipelago.Core
{
    /// <summary>Why a goal isn't sent. <see cref="None"/> means it is.</summary>
    public enum GoalRefusal
    {
        None,
        NotConnected,
        MapMismatch,
        UnsupportedGoal,
        LevelNotGranted,
        AlreadySent,
        NotPending,
    }

    /// <summary>What to do with the goal: send it now, or refuse with a reason for the log.</summary>
    public readonly struct GoalDecision
    {
        private GoalDecision(GoalRefusal refusal) => Refusal = refusal;

        public static GoalDecision SendNow => new GoalDecision(GoalRefusal.None);

        public GoalRefusal Refusal { get; }

        public bool Send => Refusal == GoalRefusal.None;

        public string Reason => GoalState.ReasonText(Refusal);

        public static GoalDecision Refuse(GoalRefusal refusal) => new GoalDecision(refusal);
    }

    /// <summary>
    /// Whether the Archipelago goal is sent, for the game run. A goal reached live while offline stays
    /// pending for its map until the next login on that map (or until the game quits); once sent, it is
    /// never sent again. A star found only in a save isn't "reached live": only the manual entry point
    /// sends that. Never persisted.
    /// </summary>
    public static class GoalState
    {
        public const string SupportedGoal = "endless_complete";

        /// <summary>The map UUID a goal is pending for, or null.</summary>
        public static string PendingMap { get; private set; }

        public static bool Sent { get; private set; }

        public static bool IsSupportedGoal(string goal) => goal == SupportedGoal;

        public static bool IsPendingFor(string map) => PendingMap != null && PendingMap == map;

        /// <summary>
        /// The star was awarded during play. Sends when connected to a matching slot with a supported
        /// goal; while offline the goal stays pending for <paramref name="map"/>.
        /// </summary>
        public static GoalDecision OnReachedLive(string map, bool connected, bool mapMatches, bool goalSupported)
        {
            if (Sent)
            {
                return GoalDecision.Refuse(GoalRefusal.AlreadySent);
            }
            if (!connected)
            {
                PendingMap = map;
                return GoalDecision.Refuse(GoalRefusal.NotConnected);
            }
            return Check(mapMatches, goalSupported, levelGranted: true);
        }

        /// <summary>After a login: sends only a goal pending for this map, and leaves another map's alone.</summary>
        public static GoalDecision OnLogin(string map, bool mapMatches, bool goalSupported, bool levelGranted)
        {
            if (Sent)
            {
                return GoalDecision.Refuse(GoalRefusal.AlreadySent);
            }
            if (!IsPendingFor(map))
            {
                return GoalDecision.Refuse(GoalRefusal.NotPending);
            }
            return Check(mapMatches, goalSupported, levelGranted);
        }

        /// <summary>The player asked to send the goal. Skips only the "reached live" guard.</summary>
        public static GoalDecision OnManual(string map, bool connected, bool mapMatches, bool goalSupported, bool levelGranted)
        {
            if (Sent)
            {
                return GoalDecision.Refuse(GoalRefusal.AlreadySent);
            }
            if (!connected)
            {
                return GoalDecision.Refuse(GoalRefusal.NotConnected);
            }
            return Check(mapMatches, goalSupported, levelGranted);
        }

        public static void MarkSent()
        {
            Sent = true;
            PendingMap = null;
        }

        /// <summary>Sending failed: the goal is pending again for <paramref name="map"/>.</summary>
        public static void MarkFailed(string map)
        {
            Sent = false;
            PendingMap = map;
        }

        public static void Reset()
        {
            Sent = false;
            PendingMap = null;
        }

        public static string ReasonText(GoalRefusal refusal)
        {
            switch (refusal)
            {
                case GoalRefusal.None: return "sent";
                case GoalRefusal.NotConnected: return "not connected";
                case GoalRefusal.MapMismatch: return "the level's map doesn't match the seed";
                case GoalRefusal.UnsupportedGoal: return "the slot's goal isn't " + SupportedGoal;
                case GoalRefusal.LevelNotGranted: return "the level hasn't earned the Endless-complete star";
                case GoalRefusal.AlreadySent: return "the goal was already sent in this game run";
                case GoalRefusal.NotPending: return "no goal is pending for this map";
                default: return refusal.ToString();
            }
        }

        private static GoalDecision Check(bool mapMatches, bool goalSupported, bool levelGranted)
        {
            if (!mapMatches)
            {
                return GoalDecision.Refuse(GoalRefusal.MapMismatch);
            }
            if (!goalSupported)
            {
                return GoalDecision.Refuse(GoalRefusal.UnsupportedGoal);
            }
            if (!levelGranted)
            {
                return GoalDecision.Refuse(GoalRefusal.LevelNotGranted);
            }
            return GoalDecision.SendNow;
        }
    }
}
