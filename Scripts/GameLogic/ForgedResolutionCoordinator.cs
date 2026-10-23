using System;

namespace TcgEngine
{
    /// <summary>
    /// Coordinates one priority-approved resolution into a replayable outbox.
    /// Host must persist stack/journal/outbox together before dispatching moves.
    /// This orchestrator does not apply Unity zones or persist authoritative state.
    /// </summary>
    public sealed class ForgedResolutionCoordinator
    {
        private readonly ForgedStackResolutionService resolver;
        private readonly ForgedResolutionJournal journal;
        private readonly ForgedZoneOutbox outbox;
        private readonly ForgedPaidActionService paid;

        public ForgedResolutionCoordinator(ForgedStackResolutionService resolver,
            ForgedResolutionJournal journal, ForgedZoneOutbox outbox,
            ForgedPaidActionService paid)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.paid = paid ?? throw new ArgumentNullException(nameof(paid));
        }

        public ForgedStackResolutionService.Outcome ResolveOne()
        {
            var outcome = resolver.ResolveReady();
            if (outcome == null)
                return null;
            foreach (var instruction in journal.Record(outcome))
                outbox.Enqueue(instruction);
            // Casting costs are spent when action is accepted, not when the
            // target successfully resolves. A countered spell is also paid.
            paid.CommitCost(outcome.resolvedActionId);
            if (outcome.counterSucceeded)
                paid.CommitCost(outcome.counteredActionId);
            return outcome;
        }
    }
}
