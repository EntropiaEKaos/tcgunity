using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Recover a card from the caster's discard pile to hand or deck.
    /// Author with AbilityTarget.CardSelector and a ConditionCardPile(Discard)
    /// so the card is selected through the existing server-validated selector.
    /// Never use this for board resurrection: playing onto the board requires
    /// a slot and the normal gameplay validation/trigger pipeline.
    /// </summary>
    [CreateAssetMenu(fileName = "effect_recover_discard",
        menuName = "TcgEngine/Effect/RecoverDiscard", order = 10)]
    public class EffectRecoverDiscard : EffectData
    {
        public enum Destination { Hand, Deck }
        public Destination destination = Destination.Hand;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (logic == null || caster == null || target == null)
                return;

            Game game = logic.GetGameData();
            if (game == null || caster.player_id != target.player_id || !game.IsInDiscard(target))
                return;

            Player player = game.GetPlayer(target.player_id);
            if (player == null || !player.cards_discard.Contains(target))
                return;

            // Change zone exactly once. Preserve UID; reset transient card state
            // using the same convention as EffectSendPile.
            player.RemoveCardFromAllGroups(target);
            if (destination == Destination.Hand)
                player.cards_hand.Add(target);
            else
                player.cards_deck.Add(target);
            target.Clear();
        }
    }
}