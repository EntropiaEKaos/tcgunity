using System;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Opt-in adapter from the FORGED 2 zone outbox to the existing GameLogic.
    /// Invoke only on the authoritative game thread, not on clients.
    /// </summary>
    public sealed class ForgedUnityGraveyardAdapter
    {
        private readonly GameLogic logic;
        private readonly ForgedZoneOutboxProcessor processor;

        public ForgedUnityGraveyardAdapter(GameLogic logic, ForgedZoneOutbox outbox)
        {
            this.logic = logic ?? throw new ArgumentNullException(nameof(logic));
            processor = new ForgedZoneOutboxProcessor(outbox, Apply);
        }

        public int ProcessPending() { return processor.ProcessPending(); }

        private bool Apply(long actionId, string cardUid, bool countered)
        {
            var game = logic.GameData;
            if (game == null || actionId <= 0 || string.IsNullOrEmpty(cardUid))
                return false;
            var card = game.GetCard(cardUid);
            if (card == null)
                return false;
            // Replayed move already applied: acknowledge without retriggering OnDeath.
            if (game.IsInDiscard(card))
                return true;
            // Actual discard and associated notifications remain under GameLogic.
            logic.DiscardCard(card);
            return game.IsInDiscard(card);
        }
    }
}
