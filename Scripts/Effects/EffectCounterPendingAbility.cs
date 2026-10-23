using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Opt-in ability counter prototype. Cancels only a pending queued ability
    /// belonging to the selected card. Does not remove the card or undo effects.
    /// Full priority windows and spell stack are not yet implemented.
    /// </summary>
    [CreateAssetMenu(fileName = "effect_counter_pending",
        menuName = "TcgEngine/Effect/CounterPendingAbility", order = 10)]
    public class EffectCounterPendingAbility : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (logic == null || caster == null || target == null || caster.uid == target.uid)
                return;
            logic.CounterPendingAbility(target);
        }
    }
}
