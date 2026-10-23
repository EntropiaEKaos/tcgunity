using System;

namespace TcgEngine
{
    /// <summary>
    /// Executes pending zone instructions via an authoritative host callback.
    /// The host must make each move idempotent by action ID and persist changes
    /// before reporting success. This class does not itself mutate Unity zones.
    /// </summary>
    public sealed class ForgedZoneOutboxProcessor
    {
        private readonly ForgedZoneOutbox outbox;
        private readonly Func<long, string, bool, bool> applyMove;
        private bool processing;

        public ForgedZoneOutboxProcessor(ForgedZoneOutbox outbox,
            Func<long, string, bool, bool> applyMove)
        {
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.applyMove = applyMove ?? throw new ArgumentNullException(nameof(applyMove));
        }

        public int ProcessPending()
        {
            if (processing)
                throw new InvalidOperationException("Reentrant zone processing denied");
            processing = true;
            int acknowledged = 0;
            try
            {
                foreach (var item in outbox.Pending())
                {
                    // Failed callback remains pending for a future retry.
                    if (applyMove(item.actionId, item.sourceUid, item.countered)
                        && outbox.Acknowledge(item.actionId))
                        acknowledged++;
                }
                return acknowledged;
            }
            finally
            {
                processing = false;
            }
        }
    }
}
