using System;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// Transport-neutral bundle for an authoritative FORGED 2 recovery checkpoint.
    /// The host must persist the entire bundle atomically with its game state.
    /// </summary>
    public static class ForgedRecoveryCheckpoint
    {
        [Serializable]
        public sealed class Snapshot
        {
            public int version = 1;
            public ForgedActionStack.Snapshot stack;
            public ForgedManaLedger.Snapshot mana;
            public ForgedResolutionJournal.Snapshot journal;
            public ForgedZoneOutbox.Snapshot outbox;
        }

        public sealed class Restored
        {
            public ForgedActionStack stack;
            public ForgedManaLedger mana;
            public ForgedResolutionJournal journal;
            public ForgedZoneOutbox outbox;
        }

        public static Snapshot Capture(ForgedActionStack stack, ForgedManaLedger mana,
            ForgedResolutionJournal journal, ForgedZoneOutbox outbox)
        {
            if (stack == null || mana == null || journal == null || outbox == null)
                throw new ArgumentNullException("Missing checkpoint component");
            return new Snapshot {
                stack = stack.ExportSnapshot(),
                mana = mana.ExportSnapshot(),
                journal = journal.ExportSnapshot(),
                outbox = outbox.ExportSnapshot()
            };
        }

        public static Restored Restore(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.version != 1)
                throw new ArgumentException("Unsupported recovery checkpoint version");
            // Validate each component and cross-component references before returning.
            var restored = new Restored {
                stack = ForgedActionStack.FromSnapshot(snapshot.stack),
                mana = ForgedManaLedger.FromSnapshot(snapshot.mana),
                journal = ForgedResolutionJournal.FromSnapshot(snapshot.journal),
                outbox = ForgedZoneOutbox.FromSnapshot(snapshot.outbox)
            };
            var reserved = new HashSet<long>();
            foreach (var item in snapshot.mana.reservations)
            {
                if (!reserved.Add(item.actionId))
                    throw new ArgumentException("Duplicate mana reservation");
                // Older resolved actions can retain committed cost records until finalized.
                // Uncommitted reservations must still have their action pending.
                if (!item.committed && !restored.stack.HasPendingAction(item.actionId))
                    throw new ArgumentException("Uncommitted mana reservation has no pending action");
            }
            var journalIds = new HashSet<long>(snapshot.journal.handledActionIds);
            foreach (var instruction in snapshot.outbox.items)
            {
                if (!journalIds.Contains(instruction.actionId))
                    throw new ArgumentException("Zone outbox action missing from resolution journal");
            }
            return restored;
        }
    }
}
