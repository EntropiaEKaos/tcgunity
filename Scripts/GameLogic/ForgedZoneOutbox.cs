using System;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// Server-side pending zone instructions with explicit acknowledgements.
    /// Never acknowledge until the authoritative zone mutation succeeds.
    /// Persistence and real card movement are the responsibility of the host.
    /// </summary>
    public sealed class ForgedZoneOutbox
    {
        [Serializable]
        public sealed class Item
        {
            public long actionId;
            public string sourceUid;
            public bool countered;
            public bool acknowledged;
        }

        [Serializable]
        public sealed class Snapshot
        {
            public List<Item> items = new List<Item>();
        }

        private readonly Dictionary<long, Item> items = new Dictionary<long, Item>();

        public bool Enqueue(ForgedResolutionJournal.ZoneInstruction instruction)
        {
            if (instruction == null || instruction.actionId <= 0 || string.IsNullOrEmpty(instruction.sourceUid))
                return false;
            if (items.TryGetValue(instruction.actionId, out var existing))
            {
                if (existing.sourceUid != instruction.sourceUid || existing.countered != instruction.countered)
                    throw new InvalidOperationException("Conflicting zone instruction ID");
                return false;
            }
            items.Add(instruction.actionId, new Item {
                actionId = instruction.actionId, sourceUid = instruction.sourceUid,
                countered = instruction.countered
            });
            return true;
        }

        public bool Acknowledge(long actionId)
        {
            if (!items.TryGetValue(actionId, out var item) || item.acknowledged)
                return false;
            item.acknowledged = true;
            return true;
        }

        public List<Item> Pending()
        {
            var result = new List<Item>();
            foreach (var item in items.Values)
                if (!item.acknowledged)
                    result.Add(new Item { actionId = item.actionId, sourceUid = item.sourceUid, countered = item.countered });
            result.Sort((a, b) => a.actionId.CompareTo(b.actionId));
            return result;
        }

        public Snapshot ExportSnapshot()
        {
            var result = new Snapshot();
            foreach (var item in items.Values)
                result.items.Add(new Item {
                    actionId = item.actionId, sourceUid = item.sourceUid,
                    countered = item.countered, acknowledged = item.acknowledged
                });
            result.items.Sort((a, b) => a.actionId.CompareTo(b.actionId));
            return result;
        }

        public static ForgedZoneOutbox FromSnapshot(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.items == null)
                throw new ArgumentException("Invalid outbox snapshot");
            var outbox = new ForgedZoneOutbox();
            foreach (var item in snapshot.items)
            {
                if (item == null || item.actionId <= 0 || string.IsNullOrEmpty(item.sourceUid)
                    || outbox.items.ContainsKey(item.actionId))
                    throw new ArgumentException("Invalid or duplicate zone instruction");
                outbox.items.Add(item.actionId, new Item {
                    actionId = item.actionId, sourceUid = item.sourceUid,
                    countered = item.countered, acknowledged = item.acknowledged
                });
            }
            return outbox;
        }
    }
}
