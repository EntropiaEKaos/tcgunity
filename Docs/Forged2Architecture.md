# FORGED 2 — Unity TCG Foundation

FORGED 2 is the working name for evolving tcgunity into a next-generation FORGED client and rules engine. This branch is isolated from the existing FORGED game. It does not rename or deploy the existing production project.

## Design pillars
1. Authoritative gameplay: only the host/server may accept commands and mutate the canonical action stack.
2. Priority and responses: actions have unique IDs; pass priority around living players and resolve top-of-stack after all eligible players pass.
3. LIFO: counters must target a pending action ID, not just discard a card on the battlefield.
4. Zone integrity: a card UID exists in exactly one zone; graveyard recursion, exile and resurrection are independently validated.
5. Presentation separation: Unity board/FX render state; never decide combat or legality.

## Development order
- Phase A (this branch): pure C# stack state machine foundation with action IDs, priority, pass logic and counter targeting; not wired into the legacy resolver yet.
- Phase B: authoritative command/serialization protocol and game-logic integration.
- Phase C: Unity reaction interface, readable graveyard and counter effects.
- Phase D: Unity Play Mode, multiplayer, AI and visual regression certification.

## Critical transition rules
- Never replace ResolveQueue FIFO blindly: existing Secrets and abilities depend on its scheduling.
- Do not send a card to discard as a substitute for cancelling a pending action.
- Never allow two zone memberships or reset card identity by creating a duplicate.
- Keep all work in PRs; no main merge before manual Unity validation.

## Current state
A pure domain model is included below; full integration is not yet implemented. See `Scripts/GameLogic/ForgedActionStack.cs`.

## Priority state-machine hardening
- `ResolutionReady` is set only after all players pass in order; `PopTop()` rejects premature resolution.
- A new push or cancellation resets accumulated passes. Pushing/countering/passing during the resolution-ready window is rejected.
- After a LIFO pop, priority returns to the resolved action's controller. The authoritative server will need to define priority rules for each phase before integration.
- Tests added at `Editor/ForgedActionStackTests.cs` for LIFO, ID-specific cancellation, 4-player wraparound and invalid passes.
- Tests are authored, not executed. The project does not currently expose a configured test pipeline; Unity Editor Test Runner verification remains necessary.

## Snapshot protocol foundation
- `ExportSnapshot()` produces a detached serializable DTO for state transfer or replay.
- `FromSnapshot()` validates players, priority, pass state, unique ordered action IDs and next-ID monotonicity before restoring.
- This is not network integration or anti-cheat by itself: only the authoritative server should produce trusted snapshots, and protocol versioning plus Unity transport messages remain to be built.
- Two additional editor test cases cover round-trip restore and rejection of corrupted IDs.
