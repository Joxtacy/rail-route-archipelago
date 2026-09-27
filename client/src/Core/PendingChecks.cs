using System.Collections.Generic;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// In-memory set of upgrade ids recorded as pending location checks, in the order they were recorded.
    /// Keyed by ResearchItem.Id (unique per level of a levelled upgrade, e.g. "track_speed1").
    /// </summary>
    public sealed class PendingChecks
    {
        private readonly List<string> order = new List<string>();
        private readonly HashSet<string> ids = new HashSet<string>();

        public int Count => order.Count;

        /// <summary>Records <paramref name="id"/>; returns false if it was already pending.</summary>
        public bool Add(string id)
        {
            if (!ids.Add(id))
            {
                return false;
            }
            order.Add(id);
            return true;
        }

        public bool Contains(string id) => ids.Contains(id);

        public bool Remove(string id)
        {
            if (!ids.Remove(id))
            {
                return false;
            }
            order.Remove(id);
            return true;
        }

        /// <summary>The most recently recorded id that is still pending, or null.</summary>
        public string MostRecent => order.Count == 0 ? null : order[order.Count - 1];
    }
}
