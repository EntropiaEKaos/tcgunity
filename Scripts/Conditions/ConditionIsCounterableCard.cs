using UnityEngine;
using TcgEngine;

/// <summary>
/// Verifica se a carta é de um tipo counterável (Character/Artifact).
/// 
/// Uso duplo confirmado contra AbilityData.cs real:
/// 1) IsTriggerConditionMet() - decide se o Secret DISPARA (popup aparece)
/// 2) IsTargetConditionMet() - decide se o card PODE SER SELECIONADO
/// 
/// Sem o primeiro método, o popup aparecia pra Spells/Equipment (UX ruim).
/// </summary>

[CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/IsCounterableCard", order = 10)]
public class ConditionIsCounterableCard : ConditionData
{
    public bool allow_character = true;
    public bool allow_artifact = true;

    /// <summary>
    /// Decide se o Secret DISPARA quando a carta é jogada.
    /// Retorna false pra Spells/Equipment, true pra Creatures/Artifacts.
    /// Sem isso, popup aparecia pra tudo, mas Counter falhava silenciosamente em Spell.
    /// </summary>
    public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
    {
        // A carta que disparou o Secret (OnPlayOther trigger)
        Card trigger_card = data.GetCard(data.ability_triggerer);
        
        if (trigger_card == null)
        {
            Debug.LogWarning("[Counter] IsTriggerConditionMet: ability_triggerer is null");
            return false;
        }

        CardData tdata = trigger_card.CardData;
        if (tdata == null)
        {
            Debug.LogWarning("[Counter] IsTriggerConditionMet: trigger_card.CardData is null");
            return false;
        }

        // Só deixa o popup aparecer pra Character e Artifact
        bool is_countereable = (allow_character && tdata.type == CardType.Character)
                            || (allow_artifact && tdata.type == CardType.Artifact);

        if (is_countereable)
            Debug.Log($"[Counter] Popup abrindo para: {tdata.title} (tipo={tdata.type})");
        else
            Debug.Log($"[Counter] Secret NÃO disparou para: {tdata.title} (tipo={tdata.type}) - não é counterável");

        return is_countereable;
    }

    /// <summary>
    /// Decide se o card PODE SER SELECIONADO quando o popup está aberto.
    /// Camada extra de validação (redundante se IsTriggerConditionMet está correto, mas safe).
    /// </summary>
    public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
    {
        if (target == null)
            return false;

        CardData tdata = target.CardData;
        if (tdata == null)
            return false;

        bool is_valid = (allow_character && tdata.type == CardType.Character)
                     || (allow_artifact && tdata.type == CardType.Artifact);

        if (!is_valid)
            Debug.LogWarning($"[Counter] Tentativa de countera card inválido: {tdata.title}");

        return is_valid;
    }
}