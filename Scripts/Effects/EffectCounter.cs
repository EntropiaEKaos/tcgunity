using UnityEngine;
using TcgEngine;
using TcgEngine.Gameplay;

[CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Counter", order = 10)]
public class EffectCounter : EffectData
{
    public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
    {
        if (target == null)
            return;

        // Descarta a carta counterada (padrão de counterspell em TCGs)
        // DiscardCard() foi confirmado existir no GameLogic.cs (linha ~963)
        logic.DiscardCard(target);
    }
}
