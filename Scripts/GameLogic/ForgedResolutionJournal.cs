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

        private readonly HashSet<long> handled = new HashSet<long>();

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
