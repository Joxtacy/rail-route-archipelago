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
    /// other index asks the server for a full list again. A full list is the replay, applied silently, only
    /// when it came right after the Connected packet: the server sends one there only if the slot has items,
    /// so the first item of an empty slot also arrives at index 0 and must show its popup. In any other
    /// full list (a resync reply) only the items not applied before show a popup.
    /// </summary>
    internal sealed class ItemRouter
    {
        private readonly ApSession owner;
        private readonly ItemSync sync = new ItemSync();

        public ItemRouter(ApSession owner)
        {
            this.owner = owner;
        }

        public void OnReceivedItems(ReceivedItemsPacket packet, bool replay)
        {
            var items = packet.Items ?? new NetworkItem[0];
            var expected = sync.Expected;
            switch (sync.Accept(packet.Index, items.Length))
            {
                case ItemSyncAction.Replace:
                    EffectState.ClearReceived();
                    var silentCount = replay ? items.Length : sync.Known;
                    for (var i = 0; i < items.Length; i++)
                    {
                        Apply(items[i], silent: i < silentCount);
                    }
                    Log.Info("Received item list replaced: " + items.Length + " item(s), " + (items.Length - silentCount) + " new"
                        + (replay ? " (replay on connect)" : ""));
                    if (replay && items.Length > 0)
                    {
                        Notify.Side("Restored " + items.Length + " Archipelago items");
                    }
                    break;
                case ItemSyncAction.Append:
                    foreach (var item in items)
                    {
                        Apply(item, silent: false);
                    }
                    break;
                case ItemSyncAction.Resync:
                    Log.Warn("Received items at index " + packet.Index + ", expected " + expected + ". Requesting a resync.");
                    owner.SendPacket(new SyncPacket(), "Sync");
                    break;
            }
        }

        private void Apply(NetworkItem item, bool silent)
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
            Log.Info("Archipelago item: " + name + " from " + sender + (silent ? " (replay)" : ""));
            UpgradeReceiver.Receive(upgrade, silent, sender);
        }
    }
}
