namespace RailRouteArchipelago.Core
{
    public enum ItemSyncAction
    {
        /// <summary>Index 0: the full list. Replace every received item with the packet's items.</summary>
        Replace,

        /// <summary>The expected index: new items to add.</summary>
        Append,

        /// <summary>Any other index: ignore the packet and ask the server for the full list again.</summary>
        Resync,
    }

    /// <summary>
    /// Index bookkeeping for ReceivedItems packets, so the received-item state always equals the server's
    /// list for the connection: a full list replaces it, the next increment extends it, and anything
    /// else (a gap, a duplicate, an old packet) asks for a resync, which the server answers with a full list.
    /// </summary>
    public sealed class ItemSync
    {
        /// <summary>The index the next increment must have: the number of items applied so far.</summary>
        public int Expected { get; private set; }

        /// <summary>
        /// After a <see cref="ItemSyncAction.Replace"/>: how many of the list's first items were already applied
        /// before it (the previous <see cref="Expected"/>, capped at the list's length). The rest are new.
        /// </summary>
        public int Known { get; private set; }

        public ItemSyncAction Accept(int index, int count)
        {
            if (index == 0)
            {
                Known = System.Math.Min(Expected, count);
                Expected = count;
                return ItemSyncAction.Replace;
            }
            if (index == Expected)
            {
                Expected += count;
                return ItemSyncAction.Append;
            }
            return ItemSyncAction.Resync;
        }

        public void Reset()
        {
            Expected = 0;
            Known = 0;
        }
    }
}
