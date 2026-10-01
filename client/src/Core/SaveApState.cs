using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RailRouteArchipelago.Core
{
    /// <summary>The goal as far as one save is concerned.</summary>
    public enum GoalStatus
    {
        None,
        Pending,
        Sent,

        /// <summary>The level already had the Endless-complete star when it was bound. Never sent.</summary>
        Ineligible,
    }

    /// <summary>The seed and slot a save was played with.</summary>
    public sealed class SeedBinding
    {
        public SeedBinding(string seed, int team, int slot, string slotName)
        {
            Seed = seed;
            Team = team;
            Slot = slot;
            SlotName = slotName;
        }

        public string Seed { get; }

        public int Team { get; }

        public int Slot { get; }

        /// <summary>For the log only: the slot number identifies the slot within a seed.</summary>
        public string SlotName { get; }

        public bool Matches(SeedBinding other) =>
            other != null && string.Equals(Seed, other.Seed, StringComparison.Ordinal) && Team == other.Team && Slot == other.Slot;

        /// <summary>"&lt;seed&gt;/&lt;team&gt;/&lt;slot&gt;"</summary>
        public string Key => Seed + "/" + Team + "/" + Slot;

        public override string ToString() => Seed + ", team " + Team + ", slot " + SlotName + " (" + Slot + ")";
    }

    /// <summary>
    /// A save's Archipelago state, stored next to the save as &lt;save file name&gt;.ap.json: the seed it's bound
    /// to, the checks sent, the goal and how many of the slot's received items have taken effect in the level.
    /// </summary>
    public sealed class SaveApState
    {
        public const int CurrentVersion = 1;

        public const string FileSuffix = ".ap.json";

        public SeedBinding Binding { get; set; }

        /// <summary>The save's state file couldn't be read, so the save counts as bound to an unknown seed.</summary>
        public bool UnknownBinding { get; set; }

        public SortedSet<string> SentChecks { get; } = new SortedSet<string>(StringComparer.Ordinal);

        public GoalStatus Goal { get; set; }

        /// <summary>How many of the slot's received items, in the server's order, have taken effect in the level.</summary>
        public int ReceivedIndex { get; set; }

        public static SaveApState UnreadableFile() => new SaveApState { UnknownBinding = true };

        public string Serialize()
        {
            var dto = new Dto
            {
                Version = CurrentVersion,
                Binding = Binding == null ? null : new BindingDto
                {
                    Seed = Binding.Seed,
                    Team = Binding.Team,
                    Slot = Binding.Slot,
                    SlotName = Binding.SlotName,
                },
                UnknownBinding = UnknownBinding,
                SentChecks = new List<string>(SentChecks),
                Goal = GoalName(Goal),
                ReceivedIndex = ReceivedIndex,
            };
            return JsonConvert.SerializeObject(dto, Formatting.Indented);
        }

        /// <summary>
        /// Parses a state file. Invalid JSON, an unknown version, a binding without a seed, an unknown goal
        /// or a negative index is an error.
        /// </summary>
        public static bool TryParse(string text, out SaveApState state, out string error)
        {
            state = null;
            Dto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<Dto>(text ?? "");
            }
            catch (JsonException e)
            {
                error = "invalid JSON: " + e.Message;
                return false;
            }
            if (dto == null)
            {
                error = "empty file";
                return false;
            }
            if (dto.Version != CurrentVersion)
            {
                error = "unknown version " + dto.Version;
                return false;
            }
            if (dto.Binding != null && string.IsNullOrEmpty(dto.Binding.Seed))
            {
                error = "the binding has no seed";
                return false;
            }
            if (!TryParseGoal(dto.Goal, out var goal))
            {
                error = "unknown goal " + (dto.Goal ?? "(missing)");
                return false;
            }
            if (dto.ReceivedIndex < 0)
            {
                error = "negative received index " + dto.ReceivedIndex;
                return false;
            }
            state = new SaveApState
            {
                Binding = dto.Binding == null ? null : new SeedBinding(dto.Binding.Seed, dto.Binding.Team, dto.Binding.Slot, dto.Binding.SlotName),
                UnknownBinding = dto.UnknownBinding,
                Goal = goal,
                ReceivedIndex = dto.ReceivedIndex,
            };
            foreach (var name in dto.SentChecks ?? new List<string>())
            {
                if (!string.IsNullOrEmpty(name))
                {
                    state.SentChecks.Add(name);
                }
            }
            error = null;
            return true;
        }

        /// <summary>Log text: the binding, then the counts.</summary>
        public string Describe() =>
            (UnknownBinding ? "unknown binding (unreadable state file)" : Binding == null ? "unbound" : "bound to " + Binding)
            + ", " + SentChecks.Count + " sent check(s), goal " + GoalName(Goal) + ", received index " + ReceivedIndex;

        public static string GoalName(GoalStatus goal)
        {
            switch (goal)
            {
                case GoalStatus.None: return "none";
                case GoalStatus.Pending: return "pending";
                case GoalStatus.Sent: return "sent";
                case GoalStatus.Ineligible: return "ineligible";
                default: return goal.ToString();
            }
        }

        private static bool TryParseGoal(string text, out GoalStatus goal)
        {
            foreach (GoalStatus value in Enum.GetValues(typeof(GoalStatus)))
            {
                if (GoalName(value) == text)
                {
                    goal = value;
                    return true;
                }
            }
            goal = GoalStatus.None;
            return false;
        }

        private sealed class Dto
        {
            [JsonProperty("version")]
            public int Version { get; set; }

            [JsonProperty("binding")]
            public BindingDto Binding { get; set; }

            [JsonProperty("unknownBinding")]
            public bool UnknownBinding { get; set; }

            [JsonProperty("sentChecks")]
            public List<string> SentChecks { get; set; }

            [JsonProperty("goal")]
            public string Goal { get; set; }

            [JsonProperty("receivedIndex")]
            public int ReceivedIndex { get; set; }
        }

        private sealed class BindingDto
        {
            [JsonProperty("seed")]
            public string Seed { get; set; }

            [JsonProperty("team")]
            public int Team { get; set; }

            [JsonProperty("slot")]
            public int Slot { get; set; }

            [JsonProperty("slotName")]
            public string SlotName { get; set; }
        }
    }
}
