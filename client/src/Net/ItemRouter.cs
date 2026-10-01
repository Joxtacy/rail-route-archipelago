using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Game.Context;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Net
{
    /// <summary>
    /// Applies ReceivedItems packets on the main thread, driven by <see cref="ItemSync"/>: a full list
    /// (index 0) replaces every received item, the next increment is applied with unlock popups, and any
    /// other index asks the server for a full list again. Packets wait until the login has decided the
    /// level's binding (<see cref="Begin"/> or <see cref="Drop"/>), since the replay and the login reach the
    /// main thread in no fixed order. In a full list the first items, up to the save's received-item index
    /// (or the items applied before a resync), are restored without side effects. A full list is the replay,
    /// applied without popups, only when it came right after the Connected packet: the server sends one there
    /// only if the slot has items, so the first item of an empty slot also arrives at index 0 and must show
    /// its popup.
    /// </summary>
    internal sealed class ItemRouter
    {
        private readonly ApSession owner;
        private readonly ItemSync sync = new ItemSync();
        private readonly List<(ReceivedItemsPacket Packet, bool Replay)> held = new List<(ReceivedItemsPacket, bool)>();
        private bool decided;
        private bool dropped;

        public ItemRouter(ApSession owner)
        {
            this.owner = owner;
        }

        public void OnReceivedItems(ReceivedItemsPacket packet, bool replay)
        {
            if (dropped)
            {
                return;
            }
            if (!decided)
            {
                held.Add((packet, replay));
                return;
            }
            Route(packet, replay);
        }

        /// <summary>The level isn't refused: seeds the index the save already has, then applies the held packets in order.</summary>
        public void Begin(int receivedIndex)
        {
            sync.Seed(receivedIndex);
            decided = true;
            foreach (var (packet, replay) in held)
            {
                Route(packet, replay);
            }
            held.Clear();
        }

        /// <summary>The save is refused: no item is applied, now or later.</summary>
        public void Drop()
        {
            decided = true;
            dropped = true;
            if (held.Count > 0)
            {
                Log.Info("Dropped " + held.Count + " received-items packet(s) for the refused save");
            }
            held.Clear();
        }

        private void Route(ReceivedItemsPacket packet, bool replay)
        {
            var items = packet.Items ?? new NetworkItem[0];
            var expected = sync.Expected;
            switch (sync.Accept(packet.Index, items.Length))
            {
                case ItemSyncAction.Replace:
                    EffectState.ClearReceived();
                    var known = sync.Known;
                    for (var i = 0; i < items.Length; i++)
                    {
                        Apply(items[i], i < known ? ReceiveMode.Restore : replay ? ReceiveMode.NewSilent : ReceiveMode.Live);
                    }
                    var fresh = items.Length - known;
                    Log.Info("Received item list replaced: " + items.Length + " item(s), " + known + " restored, " + fresh + " new"
                        + (replay ? " (replay on connect)" : ""));
                    if (replay && items.Length > 0)
                    {
                        Notify.Side("Restored " + known + " Archipelago items" + (fresh > 0 ? ", " + fresh + " new" : ""));
                    }
                    break;
                case ItemSyncAction.Append:
                    foreach (var item in items)
                    {
                        Apply(item, ReceiveMode.Live);
                    }
                    break;
                case ItemSyncAction.Resync:
                    Log.Warn("Received items at index " + packet.Index + ", expected " + expected + ". Requesting a resync.");
                    owner.SendPacket(new SyncPacket(), "Sync");
                    return;
            }
            var state = SaveStateStore.Current;
            state.ReceivedIndex = System.Math.Max(state.ReceivedIndex, sync.Expected);
        }

        private void Apply(NetworkItem item, ReceiveMode mode)
        {
            var session = owner.Session;
            var name = session.Items.GetItemName(item.Item, ApSession.Game);
            var sender = session.Players.GetPlayerName(item.Player) ?? ("slot " + item.Player);
            if (string.IsNullOrEmpty(name))
            {
                Log.Warn("Unknown item ID " + item.Item + " from " + sender + ": no name in the datapackage. Ignored.");
                return;
            }
            if (ItemNames.IsFiller(name))
            {
                Log.Info("Filler item ignored: " + name + " from " + sender);
                return;
            }
            if (!ItemNames.TryGet(name, out var id))
            {
                Log.Warn("Unknown item \"" + name + "\" (ID " + item.Item + ") from " + sender + ". Ignored.");
                return;
            }
            var deps = Ctx.Deps;
            if (id == "custom_contracts" && LevelDlc.HappyPassengersActive(deps))
            {
                // The variant the level shows; both count as one item.
                id = "custom_contracts_alt";
            }
            var upgrade = deps.ResearchController.ResearchItems.FirstOrDefault(i => i.Id == id);
            if (upgrade == null)
            {
                Log.Warn("Item \"" + name + "\" from " + sender + " maps to " + id + ", which the level doesn't have. Ignored.");
                return;
            }
            Log.Info("Archipelago item: " + name + " from " + sender
                + (mode == ReceiveMode.Restore ? " (restored)" : mode == ReceiveMode.NewSilent ? " (new in the replay)" : ""));
            UpgradeReceiver.Receive(upgrade, mode, sender);
        }
    }
}
