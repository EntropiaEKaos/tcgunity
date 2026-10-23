using System;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// Produces exactly-once zone instructions for resolved stack actions.
    /// The live GameLogic host must apply these instructions authoritatively.
    /// This does not move Unity cards or mutate players.
    /// </summary>
    public sealed class ForgedResolutionJournal
    {
        public sealed class ZoneInstruction
        {
            public long actionId;
            public string sourceUid;
            public bool countered;
        }

        [Serializable]
        public sealed class Snapshot
        {
            public List<long> handledActionIds = new List<long>();
        }

        private readonly HashSet<long> handled = new HashSet<long>();

        // Persist on the authoritative host before acknowledging a completed transition.
        public Snapshot ExportSnapshot()
        {
            var snapshot = new Snapshot();
            snapshot.handledActionIds.AddRange(handled);
            snapshot.handledActionIds.Sort();
            return snapshot;
        }

        public static ForgedResolutionJournal FromSnapshot(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.handledActionIds == null)
                throw new ArgumentException("Invalid journal snapshot");
            var journal = new ForgedResolutionJournal();
            foreach (long id in snapshot.handledActionIds)
                if (id <= 0 || !journal.handled.Add(id))
                    throw new ArgumentException("Duplicate or invalid action ID");
            return journal;
        }

        public List<ZoneInstruction> Record(ForgedStackResolutionService.Outcome outcome)
        {
            var instructions = new List<ZoneInstruction>();
            if (outcome == null || outcome.resolvedActionId <= 0
                || string.IsNullOrEmpty(outcome.resolvedSourceUid))
                return instructions;
            if (!handled.Add(outcome.resolvedActionId))
                return instructions;

            // Counters are spells; the host should move their source to discard.
            if (outcome.isCounter)
                instructions.Add(new ZoneInstruction
                {
                    actionId = outcome.resolvedActionId,
                    sourceUid = outcome.resolvedSourceUid,
                    countered = false
                });
            if (outcome.counterSucceeded && outcome.counteredActionId > 0
                && !string.IsNullOrEmpty(outcome.counteredSourceUid)
                && handled.Add(outcome.counteredActionId))
                instructions.Add(new ZoneInstruction
                {
                    actionId = outcome.counteredActionId,
                    sourceUid = outcome.counteredSourceUid,
                    countered = true
                });
            return instructions;
        }
    }
}
