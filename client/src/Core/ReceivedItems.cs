using System.Collections.Generic;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// How many copies of each item have been received, keyed by item key (the game's Research name,
    /// with both Custom Contracts variants under one key). Progressive items count one per copy.
    /// </summary>
    public sealed class ReceivedItems
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        /// <summary>Records one copy of <paramref name="key"/> and returns the new count.</summary>
        public int Receive(string key)
        {
            counts.TryGetValue(key, out var count);
            counts[key] = ++count;
            return count;
        }

        public int Count(string key) => counts.TryGetValue(key, out var count) ? count : 0;

        public void Clear() => counts.Clear();
    }
}
