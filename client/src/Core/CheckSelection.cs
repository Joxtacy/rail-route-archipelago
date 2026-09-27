using System.Collections.Generic;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// Which locations the resend on connect sends: every bought slot except those the game granted
    /// itself, mapped to its location name and de-duplicated (both Custom Contracts variants give one).
    /// </summary>
    public sealed class CheckSelection
    {
        private CheckSelection(List<string> locationNames, List<string> unknownIds)
        {
            LocationNames = locationNames;
            UnknownIds = unknownIds;
        }

        /// <summary>Location names to send, in the order of the bought slots.</summary>
        public IReadOnlyList<string> LocationNames { get; }

        /// <summary>Bought slot Ids with no location name.</summary>
        public IReadOnlyList<string> UnknownIds { get; }

        public static CheckSelection Select(IEnumerable<string> boughtIds, ICollection<string> grantedIds)
        {
            var names = new List<string>();
            var seen = new HashSet<string>();
            var unknown = new List<string>();
            foreach (var id in boughtIds)
            {
                if (id == null || (grantedIds != null && grantedIds.Contains(id)))
                {
                    continue;
                }
                if (!Core.LocationNames.TryGet(id, out var name))
                {
                    unknown.Add(id);
                }
                else if (seen.Add(name))
                {
                    names.Add(name);
                }
            }
            return new CheckSelection(names, unknown);
        }
    }
}
