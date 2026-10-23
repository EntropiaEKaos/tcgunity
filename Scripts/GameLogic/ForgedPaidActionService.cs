using System;

namespace TcgEngine
{
    /// <summary>
    /// Server-only prototype coordinating validated play with mana reservation.
    /// Not wired to legacy GameLogic or the networking layer.
    /// </summary>
    public sealed class ForgedPaidActionService
    {
        private readonly ForgedActionStack stack;
        private readonly ForgedManaLedger mana;
        private readonly Func<int, string, bool> ownsSource;
        private readonly Func<int, string, string, bool> canPlay;
        private bool executing;

        public ForgedPaidActionService(ForgedActionStack stack, ForgedManaLedger mana,
            Func<int, string, bool> ownsSource, Func<int, string, string, bool> canPlay)
        {
            this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
            this.mana = mana ?? throw new ArgumentNullException(nameof(mana));
            this.ownsSource = ownsSource ?? throw new ArgumentNullException(nameof(ownsSource));
            this.canPlay = canPlay ?? throw new ArgumentNullException(nameof(canPlay));
        }

        public long TryPlay(int authenticatedPlayer, int playerId, string sourceUid, string abilityId, int cost)
        {
            if (executing)
                return 0;
            executing = true;
            try
            {
                if (authenticatedPlayer != playerId || cost < 0
                    || stack.ResolutionReady || stack.PriorityPlayer != playerId
                    || string.IsNullOrEmpty(sourceUid) || mana.GetAvailable(playerId) < cost
                    || !ownsSource(playerId, sourceUid)
                    || !canPlay(playerId, sourceUid, abilityId))
                    return 0;

                // Unique next action ID is read before push. Reserve first so
                // insufficient mana cannot create an action or advance priority.
                long nextActionId = stack.ExportSnapshot().nextId;
                if (!mana.Reserve(nextActionId, playerId, cost))
                    return 0;
                try
                {
                    var entry = stack.Push(playerId, sourceUid, abilityId);
                    if (entry.id != nextActionId)
                        throw new InvalidOperationException("Unexpected action ID");
                    return entry.id;
                }
                catch
                {
                    mana.Refund(nextActionId);
                    throw;
                }
            }
            finally
            {
                executing = false;
            }
        }

        // Cost becomes non-refundable once the authoritative host confirms the cast.
        public bool CommitCost(long actionId) { return mana.Commit(actionId); }
        public bool RefundUncommitted(long actionId) { return mana.Refund(actionId); }
    }
}
