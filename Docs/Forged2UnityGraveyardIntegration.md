# FORGED 2: opt-in Unity graveyard bridge

`ForgedUnityGraveyardAdapter` is the first bridge from FORGED 2 pending zone instructions to existing `TcgEngine.Gameplay.GameLogic`.

- Construct it using the authoritative `GameLogic` instance and an existing `ForgedZoneOutbox` after the host has durably captured the outbox instructions.
- Call `ProcessPending()` on the authoritative Unity/game thread, never from a client UI or a network receive thread.
- The bridge resolves each source UID via `GameData.GetCard`, calls existing `GameLogic.DiscardCard`, verifies `IsInDiscard`, and only then acknowledges the instruction.
- Already-discarded cards are accepted as idempotent replays without invoking OnDeath twice. Unknown UID and exceptions remain pending for retry.
- No production wiring, transport, persistent transaction or atomic recovery implemented; if a card moves and the server crashes before persisting acknowledgement, the next host must reconstruct outbox and authoritative game state together.
- This adapter is **not included in the .NET 8 NUnit project** because it references Unity `GameLogic`; a Unity Editor compile/integration run is required before merge.
- Before integration, confirm that only valid countered spells and spell-like responses produce zone instructions, since the legacy `DiscardCard` triggers standard card-death abilities when applicable.
