using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// For resurrection abilities: prevents a target selection (and thus
    /// payment of an activated ability cost) when the caster has no free slot.
    /// Add this to both trigger conditions and card-target conditions.
    /// </summary>
    [CreateAssetMenu(fileName = "condition_graveyard_space",
        menuName = "TcgEngine/Condition/GraveyardBoardSpace", order = 10)]
    public class ConditionGraveyardBoardSpace : ConditionData
    {
        private bool HasSpace(Game game, Card caster)
        {
            if (game == null || caster == null)
                return false;

            Player player = game.GetPlayer(caster.player_id);
            return player != null && player.GetEmptySlots().Count > 0;
        }

        public override bool IsTriggerConditionMet(Game game, AbilityData ability, Card caster)
        {
            return HasSpace(game, caster);
        }

        public override bool IsTargetConditionMet(Game game, AbilityData ability, Card caster, Card target)
        {
            return HasSpace(game, caster);
        }
    }
}
