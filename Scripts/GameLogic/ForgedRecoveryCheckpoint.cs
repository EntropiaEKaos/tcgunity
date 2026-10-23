using System;

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
            // All component validation completes before any state is returned.
            return new Restored {
                stack = ForgedActionStack.FromSnapshot(snapshot.stack),
                mana = ForgedManaLedger.FromSnapshot(snapshot.mana),
                journal = ForgedResolutionJournal.FromSnapshot(snapshot.journal),
                outbox = ForgedZoneOutbox.FromSnapshot(snapshot.outbox)
            };
        }
    }
}
