using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Reanimate one of your original creatures from discard into a random
    /// free slot on your side of the board, exhausted. No normal OnPlay triggers.
    /// Configure ability target as CardSelector with CardPile=Discard and
    /// CardType=Character conditions; server-side validation also occurs here.
    /// </summary>
    [CreateAssetMenu(fileName = "effect_revive_discard",
        menuName = "TcgEngine/Effect/ReviveDiscard", order = 10)]
    public class EffectReviveDiscard : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (logic != null)
                logic.ReviveDiscardCard(caster, target);
        }
    }
}
