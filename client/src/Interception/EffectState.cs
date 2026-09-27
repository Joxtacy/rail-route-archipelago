using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using HarmonyLib;
using RailRouteArchipelago.Core;
using Research = Game.ResearchController.Research;
using ResearchItem = Game.ResearchController.ResearchItem;

namespace RailRouteArchipelago.Interception
{
    /// <summary>
    /// What the game's effect queries answer in split mode: received item counts plus the upgrades the
    /// game itself granted (level configuration, "unlock all", tutorial buttons). Slot flags play no part.
    /// Lives for one level session; <see cref="Clear"/> runs with ResearchController.Reset().
    /// Maps are keyed by (int)Research so hot-path lookups never box the enum.
    /// </summary>
    internal static class EffectState
    {
        public static readonly ReceivedItems Received = new ReceivedItems();

        private static readonly HashSet<string> grantedIds = new HashSet<string>();

        private static readonly Dictionary<int, string> itemKeys = Enum.GetValues(typeof(Research)).Cast<Research>()
            .ToDictionary(r => (int)r, r => r == Research.CustomContractsAlt ? Research.CustomContracts.ToString() : r.ToString());

        private static readonly AccessTools.FieldRef<ResearchController, Dictionary<Research, int>> initialValueField =
            AccessTools.FieldRefAccess<ResearchController, Dictionary<Research, int>>("InitialValue");

        // Built once from the controller's item list (see EnsureIndexed).
        private static List<ResearchItem> indexedItems;
        private static Dictionary<int, string[]> binaryGrantIds;
        private static Dictionary<int, ResearchItem[]> chains;
        private static ResearchItem[] automationItems;
        private static Dictionary<int, int> initialValues;

        /// <summary>Set while the mod raises ResearchCompleted itself, so the grant observer ignores it.</summary>
        public static bool RaisingOwnEvent { get; private set; }

        public static string ItemKey(Research research) => itemKeys[(int)research];

        public static void RecordGrant(string id) => grantedIds.Add(id);

        public static void Clear()
        {
            Received.Clear();
            grantedIds.Clear();
        }

        /// <summary>HasResearched: binary upgrades only, like the game (levelled ones answer false).</summary>
        public static bool HasResearched(ResearchController controller, Research research)
        {
            EnsureIndexed(controller);
            if (!binaryGrantIds.TryGetValue((int)research, out var ids))
            {
                return false;
            }
            if (Received.Count(itemKeys[(int)research]) > 0)
            {
                return true;
            }
            foreach (var id in ids)
            {
                if (grantedIds.Contains(id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ResearchedValue: the higher of the n-th chain level (n = received copies, clamped to the chain)
        /// and the highest granted level, else the game's initial value. False for a non-levelled
        /// research, so the caller lets the game answer (it throws, as unmodded).
        /// </summary>
        public static bool TryResearchedValue(ResearchController controller, Research research, out int value)
        {
            EnsureIndexed(controller);
            value = 0;
            if (!chains.TryGetValue((int)research, out var chain))
            {
                return false;
            }
            var found = false;
            var level = Math.Min(Received.Count(itemKeys[(int)research]), chain.Length);
            if (level > 0)
            {
                value = chain[level - 1].Value.Value;
                found = true;
            }
            foreach (var item in chain)
            {
                if (grantedIds.Contains(item.Id) && (!found || item.Value.Value > value))
                {
                    value = item.Value.Value;
                    found = true;
                }
            }
            if (!found)
            {
                value = initialValues[(int)research];
            }
            return true;
        }

        public static bool IsAutomationEnabled(ResearchController controller)
        {
            EnsureIndexed(controller);
            foreach (var item in automationItems)
            {
                if (Received.Count(itemKeys[(int)item.Research]) > 0 || grantedIds.Contains(item.Id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The item a receive of <paramref name="selected"/> takes effect as: the item itself for a binary
        /// upgrade, or the next level of its chain (clamped to the last) for a levelled one.
        /// </summary>
        public static ResearchItem ResolveEffectItem(ResearchController controller, ResearchItem selected)
        {
            EnsureIndexed(controller);
            if (!selected.Value.HasValue || !chains.TryGetValue((int)selected.Research, out var chain))
            {
                return selected;
            }
            var next = Math.Min(Received.Count(ItemKey(selected.Research)), chain.Length - 1);
            return chain[next];
        }

        /// <summary>Raises ResearchCompleted for a UI refresh without it being recorded as a game grant.</summary>
        public static void RaiseResearchCompleted(IEventManager eventManager, ResearchItem item)
        {
            RaisingOwnEvent = true;
            try
            {
                eventManager.TriggerResearchCompleted(item);
            }
            finally
            {
                RaisingOwnEvent = false;
            }
        }

        private static void EnsureIndexed(ResearchController controller)
        {
            var items = controller.ResearchItems;
            if (ReferenceEquals(items, indexedItems))
            {
                return;
            }
            // Same split as ResearchController.Awake: Value-less items are binary, the rest form chains by Value.
            var binary = items.Where(i => !i.Value.HasValue).ToList();
            binaryGrantIds = binary.ToDictionary(i => (int)i.Research, i => binary
                .Where(other => itemKeys[(int)other.Research] == itemKeys[(int)i.Research])
                .Select(other => other.Id).ToArray());
            chains = items.Where(i => i.Value.HasValue).OrderBy(i => i.Value.Value)
                .GroupBy(i => (int)i.Research).ToDictionary(g => g.Key, g => g.ToArray());
            automationItems = items.Where(i => i.EnablesAutomation).ToArray();
            initialValues = initialValueField(controller).ToDictionary(e => (int)e.Key, e => e.Value);
            indexedItems = items;
            Log.Info("Effect state indexed: " + binaryGrantIds.Count + " binary, " + chains.Count + " levelled, "
                + automationItems.Length + " automation upgrades.");
        }
    }
}
