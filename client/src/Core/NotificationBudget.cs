using System;
using System.Collections.Generic;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// Keeps the mod's notifications oldest first and decides which to remove when a new one takes the
    /// count over the limit. Only routine (non-sticky) entries are ever evicted, oldest first, so sticky
    /// ones can take the count over the limit. Entries for which <c>isGone</c> answers true (dismissed,
    /// expired, cleared by the game) are pruned and no longer count.
    /// </summary>
    public sealed class NotificationBudget<T>
    {
        private readonly List<(T Item, bool Sticky)> entries = new List<(T, bool)>();
        private readonly Func<T, bool> isGone;

        public NotificationBudget(Func<T, bool> isGone)
        {
            this.isGone = isGone ?? throw new ArgumentNullException(nameof(isGone));
        }

        public int Count => entries.Count;

        /// <summary>
        /// Adds <paramref name="item"/> and returns the entries to evict, oldest first. A
        /// <paramref name="limit"/> of 0 or less evicts nothing.
        /// </summary>
        public List<T> Add(T item, bool sticky, int limit)
        {
            entries.RemoveAll(e => isGone(e.Item));
            entries.Add((item, sticky));

            var evicted = new List<T>();
            if (limit <= 0)
            {
                return evicted;
            }
            while (entries.Count > limit)
            {
                var oldest = entries.FindIndex(e => !e.Sticky);
                if (oldest < 0)
                {
                    break;
                }
                evicted.Add(entries[oldest].Item);
                entries.RemoveAt(oldest);
            }
            return evicted;
        }

        public void Clear() => entries.Clear();
    }
}
