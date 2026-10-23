using System;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// FORGED 2: deterministic, opt-in priority and LIFO action state.
    /// Domain model only: not connected to the legacy ResolveQueue.
    /// Server must validate actions before calling Push/Counter/Pass.
    /// </summary>
    public sealed class ForgedActionStack
    {
        public sealed class Entry
        {
            public readonly long id;
            public readonly int controllerId;
            public readonly string sourceUid;
            public readonly string abilityId;

            internal Entry(long id, int controllerId, string sourceUid, string abilityId)
            {
                this.id = id;
                this.controllerId = controllerId;
                this.sourceUid = sourceUid;
                this.abilityId = abilityId;
            }
        }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly int playerCount;
        private long nextId = 1;
        private int consecutivePasses;
        private bool resolutionReady;
        public bool ResolutionReady { get { return resolutionReady; } }
        public int PriorityPlayer { get; private set; }
        public int Count { get { return entries.Count; } }

        public ForgedActionStack(int playerCount, int startingPlayer)
        {
            if (playerCount < 2 || startingPlayer < 0 || startingPlayer >= playerCount)
                throw new ArgumentOutOfRangeException("Invalid player count or starting player");
            this.playerCount = playerCount;
            PriorityPlayer = startingPlayer;
        }

        [Serializable]
        public sealed class Snapshot
        {
            public int playerCount;
            public int priorityPlayer;
            public int consecutivePasses;
            public bool resolutionReady;
            public long nextId;
            public List<EntrySnapshot> entries = new List<EntrySnapshot>();
        }

        [Serializable]
        public sealed class EntrySnapshot
        {
            public long id;
            public int controllerId;
            public string sourceUid;
            public string abilityId;
        }

        /// <summary>Returns an independent state copy for authoritative sync or replay.</summary>
        public Snapshot ExportSnapshot()
        {
            var snapshot = new Snapshot
            {
                playerCount = playerCount,
                priorityPlayer = PriorityPlayer,
                consecutivePasses = consecutivePasses,
                resolutionReady = resolutionReady,
                nextId = nextId
            };
            foreach (Entry entry in entries)
                snapshot.entries.Add(new EntrySnapshot
                {
                    id = entry.id,
                    controllerId = entry.controllerId,
                    sourceUid = entry.sourceUid,
                    abilityId = entry.abilityId
                });
            return snapshot;
        }

        /// <summary>Restore a validated snapshot; never trust client-supplied snapshots.</summary>
        public static ForgedActionStack FromSnapshot(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.playerCount < 2
                || snapshot.priorityPlayer < 0 || snapshot.priorityPlayer >= snapshot.playerCount
                || snapshot.consecutivePasses < 0 || snapshot.consecutivePasses >= snapshot.playerCount
                || (snapshot.resolutionReady && snapshot.consecutivePasses != 0)
                || snapshot.nextId < 1 || snapshot.entries == null)
                throw new ArgumentException("Invalid action stack snapshot");

            var restored = new ForgedActionStack(snapshot.playerCount, snapshot.priorityPlayer);
            long priorId = 0;
            foreach (EntrySnapshot entry in snapshot.entries)
            {
                if (entry == null || entry.id <= priorId || entry.id >= snapshot.nextId
                    || entry.controllerId < 0 || entry.controllerId >= snapshot.playerCount
                    || string.IsNullOrEmpty(entry.sourceUid))
                    throw new ArgumentException("Invalid pending entry");
                restored.entries.Add(new Entry(entry.id, entry.controllerId, entry.sourceUid, entry.abilityId));
                priorId = entry.id;
            }
            restored.nextId = snapshot.nextId;
            restored.consecutivePasses = snapshot.consecutivePasses;
            restored.resolutionReady = snapshot.resolutionReady;
            return restored;
        }

        // Read-only validation before calling rule or payment hooks.
        public bool HasPendingAction(long actionId)
        {
            if (actionId <= 0)
                return false;
            foreach (Entry entry in entries)
                if (entry.id == actionId)
                    return true;
            return false;
        }

        public Entry Peek()
        {
            return entries.Count == 0 ? null : entries[entries.Count - 1];
        }

        public Entry Push(int controllerId, string sourceUid, string abilityId)
        {
            if (resolutionReady || controllerId != PriorityPlayer || string.IsNullOrEmpty(sourceUid))
                throw new InvalidOperationException("Not your priority or missing source");
            Entry entry = new Entry(nextId++, controllerId, sourceUid, abilityId);
            entries.Add(entry);
            consecutivePasses = 0;
            resolutionReady = false;
            PriorityPlayer = (controllerId + 1) % playerCount;
            return entry;
        }

        public bool Counter(int controllerId, long pendingId)
        {
            if (resolutionReady || controllerId != PriorityPlayer || pendingId <= 0)
                return false;
            // Explicit IDs avoid cancelling the wrong spell; counters may target
            // any pending action. Removing it preserves other pending actions.
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].id != pendingId)
                    continue;
                entries.RemoveAt(i);
                consecutivePasses = 0;
                PriorityPlayer = (controllerId + 1) % playerCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// True means all players passed and the caller should resolve PopTop().
        /// For an empty stack this signals an ended priority round.
        /// </summary>
        public bool Pass(int playerId)
        {
            if (resolutionReady || playerId != PriorityPlayer)
                return false;
            consecutivePasses++;
            PriorityPlayer = (PriorityPlayer + 1) % playerCount;
            if (consecutivePasses < playerCount)
                return false;
            consecutivePasses = 0;
            resolutionReady = true;
            return true;
        }

        public Entry PopTop()
        {
            if (!resolutionReady)
                throw new InvalidOperationException("All players must pass before resolution");
            resolutionReady = false;
            if (entries.Count == 0)
                return null;
            Entry top = entries[entries.Count - 1];
            entries.RemoveAt(entries.Count - 1);
            PriorityPlayer = top.controllerId;
            return top;
        }
    }
}
