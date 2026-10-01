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
        PredatesBinding,
        ForeignSave,
    }

    /// <summary>What to do with the goal: send it now, or refuse with a reason for the log, plus the save's goal state to keep.</summary>
    public readonly struct GoalDecision
    {
        private GoalDecision(GoalRefusal refusal, GoalStatus state)
        {
            Refusal = refusal;
            State = state;
        }

        public GoalRefusal Refusal { get; }

        /// <summary>The save's goal state after the decision. A send leaves it as it was until <see cref="GoalState.MarkSent"/>.</summary>
        public GoalStatus State { get; }

        public bool Send => Refusal == GoalRefusal.None;

        public string Reason => GoalState.ReasonText(Refusal);

        public static GoalDecision SendNow(GoalStatus state) => new GoalDecision(GoalRefusal.None, state);

        public static GoalDecision Refuse(GoalRefusal refusal, GoalStatus state) => new GoalDecision(refusal, state);
    }

    /// <summary>
    /// The goal decisions over one save's <see cref="GoalStatus"/>. A star the level had when it was bound is
    /// ineligible for good. A star earned live is sent, or stays pending in the save until a login on a save
    /// of that level bound to the seed. Once sent, it is never sent again from that save.
    /// </summary>
    public static class GoalState
    {
        public const string SupportedGoal = "endless_complete";

        public static bool IsSupportedGoal(string goal) => goal == SupportedGoal;

        /// <summary>The level binds to a seed: a star it already has predates the binding.</summary>
        public static GoalStatus OnBind(GoalStatus state, bool granted) => granted ? GoalStatus.Ineligible : state;

        /// <summary>After a login on a level that isn't refused: sends a star the save has and hasn't sent.</summary>
        public static GoalDecision OnLogin(GoalStatus state, bool bound, bool mapMatches, bool goalSupported, bool granted)
        {
            if (state == GoalStatus.Sent)
            {
                return GoalDecision.Refuse(GoalRefusal.AlreadySent, state);
            }
            if (state == GoalStatus.Ineligible)
            {
                return GoalDecision.Refuse(GoalRefusal.PredatesBinding, state);
            }
            if (!granted)
            {
                return GoalDecision.Refuse(GoalRefusal.LevelNotGranted, state);
            }
            return Check(state, bound, mapMatches, goalSupported);
        }

        /// <summary>
        /// The star was awarded during play. Sends when connected to a matching slot the save is bound to;
        /// while offline the goal becomes pending in the save.
        /// </summary>
        public static GoalDecision OnReachedLive(GoalStatus state, bool connected, bool bound, bool mapMatches, bool goalSupported)
        {
            if (state == GoalStatus.Sent)
            {
                return GoalDecision.Refuse(GoalRefusal.AlreadySent, state);
            }
            if (state == GoalStatus.Ineligible)
            {
                return GoalDecision.Refuse(GoalRefusal.PredatesBinding, state);
            }
            if (!connected)
            {
                return GoalDecision.Refuse(GoalRefusal.NotConnected, GoalStatus.Pending);
            }
            return Check(state, bound, mapMatches, goalSupported);
        }

        public static GoalStatus MarkSent() => GoalStatus.Sent;

        /// <summary>Sending failed: the goal is pending again.</summary>
        public static GoalStatus MarkFailed() => GoalStatus.Pending;

        public static string ReasonText(GoalRefusal refusal)
        {
            switch (refusal)
            {
                case GoalRefusal.None: return "sent";
                case GoalRefusal.NotConnected: return "not connected";
                case GoalRefusal.MapMismatch: return "the level's map doesn't match the seed";
                case GoalRefusal.UnsupportedGoal: return "the slot's goal isn't " + SupportedGoal;
                case GoalRefusal.LevelNotGranted: return "the level hasn't earned the Endless-complete star";
                case GoalRefusal.AlreadySent: return "the goal was already sent from this save";
                case GoalRefusal.PredatesBinding: return "the star predates the binding";
                case GoalRefusal.ForeignSave: return "the save is bound to another seed";
                default: return refusal.ToString();
            }
        }

        private static GoalDecision Check(GoalStatus state, bool bound, bool mapMatches, bool goalSupported)
        {
            if (!mapMatches)
            {
                return GoalDecision.Refuse(GoalRefusal.MapMismatch, state);
            }
            if (!goalSupported)
            {
                return GoalDecision.Refuse(GoalRefusal.UnsupportedGoal, state);
            }
            if (!bound)
            {
                return GoalDecision.Refuse(GoalRefusal.ForeignSave, state);
            }
            return GoalDecision.SendNow(state);
        }
    }
}
