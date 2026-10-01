namespace RailRouteArchipelago.Core
{
    public enum BindingOutcome
    {
        /// <summary>Unbound with a matching map: bind the level to the slot logged in to.</summary>
        Bind,

        /// <summary>Already bound to the slot logged in to.</summary>
        Matches,

        /// <summary>Bound to another seed or slot, or to an unknown one: send nothing and disconnect.</summary>
        Refuse,

        /// <summary>Unbound, but the slot is for another map: stay unbound.</summary>
        Skip,
    }

    /// <summary>What a successful login does with the loaded level's binding.</summary>
    public readonly struct BindingDecision
    {
        private BindingDecision(BindingOutcome outcome, string reason)
        {
            Outcome = outcome;
            Reason = reason;
        }

        public BindingOutcome Outcome { get; }

        /// <summary>Why a save is refused; null otherwise.</summary>
        public string Reason { get; }

        public static BindingDecision Decide(SaveApState state, SeedBinding loggedIn, bool mapMatches)
        {
            if (state.UnknownBinding)
            {
                return new BindingDecision(BindingOutcome.Refuse, "state file unreadable");
            }
            if (state.Binding != null)
            {
                return state.Binding.Matches(loggedIn)
                    ? new BindingDecision(BindingOutcome.Matches, null)
                    : new BindingDecision(BindingOutcome.Refuse, "bound to " + state.Binding.Seed + "/" + state.Binding.Slot);
            }
            return new BindingDecision(mapMatches ? BindingOutcome.Bind : BindingOutcome.Skip, null);
        }
    }
}
