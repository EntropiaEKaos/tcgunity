# Graveyard recovery — first safe slice

## What this change adds
- `EffectRecoverDiscard`: a ScriptableObject effect for moving **one card from the caster's own discard pile** into hand or deck.
- Explicit server-side guards on zone and owner, even if a client submits an invalid selection.
- Reuses `Player.RemoveCardFromAllGroups`, `Card.Clear`, `AbilityTarget.CardSelector` and `ConditionCardPile`.

## Unity authoring
1. Create **TcgEngine > Effect > RecoverDiscard** and choose Hand or Deck.
2. Create/configure an `AbilityData` with `target = CardSelector` and attach this effect.
3. Set `conditions_target` to include `ConditionCardPile` configured for `Discard`, plus `ConditionOwner` set to the caster's owner. The owner restriction is also enforced by the effect.
4. Add the ability to a test card, enter Play Mode, and verify the selector only offers eligible discarded cards.

## Required validation before merge
- Recovery of own card from discard to hand, and then deck.
- Invalid/no target, enemy target, hand/board target: no zone change.
- One UID exists in only one zone after recovery.
- Online client/host agree on game data after recovery.
- Existing death triggers, discard UI and OnPlay behavior remain unchanged.

## Not included
- Resurrection directly to board (requires validated free slot and special summon semantics).
- Graveyard exiling and graveyard-cast.
- Counter/stack priority rework. `EffectCounter` remains unchanged.
- Unity Play Mode and network testing: must be run in a Unity project with scene/assets; this PR is not certified.

## Why no change to existing EffectSendPile
Existing cards may depend on its behavior; this initial effect is intentionally opt-in.

## Creature revival (second slice)
- `EffectReviveDiscard` selects a **Character** in your own discard pile and brings the **same card UID** to a random empty slot on your side.
- The revived card enters **exhausted**, cleared of temporary damage/status. This is **not** a normal play and does **not** fire OnPlay.
- Use `AbilityTarget.CardSelector` with target conditions `ConditionCardPile=Discard`, `ConditionCardType=Character`, `ConditionOwner=true`.
- If the board is full, there is no zone change. Configure card text accordingly; ability cost/activation may still be spent because the effect returns void.
- Validations required: same UID, occupied field no-op, no OnPlay trigger, invalid target no-op, multiplayer synchronization and OnSummon client animation.
