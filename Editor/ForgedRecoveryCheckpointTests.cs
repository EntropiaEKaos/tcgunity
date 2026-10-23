#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedRecoveryCheckpointTests
{
    [Test]
    public void RoundTripPreservesPendingStackManaAndOutbox()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        var entry = stack.Push(0, "spell", "cast");
        Assert.IsTrue(mana.Reserve(entry.id, 0, 2));
        var journal = new ForgedResolutionJournal();
        var outbox = new ForgedZoneOutbox();
        foreach (var instruction in journal.Record(new ForgedStackResolutionService.Outcome {
            resolvedActionId = 99, resolvedSourceUid = "previous-spell", isCounter = true
        }))
            outbox.Enqueue(instruction);
        var copy = ForgedRecoveryCheckpoint.Restore(
            ForgedRecoveryCheckpoint.Capture(stack, mana, journal, outbox));
        Assert.AreEqual(entry.id, copy.stack.Peek().id);
        Assert.AreEqual(3, copy.mana.GetAvailable(0));
        Assert.AreEqual(1, copy.outbox.Pending().Count);
    }

    [Test]
    public void BadVersionOrBadComponentRejectsWholeCheckpoint()
    {
        var snap = ForgedRecoveryCheckpoint.Capture(new ForgedActionStack(2, 0),
            new ForgedManaLedger(), new ForgedResolutionJournal(), new ForgedZoneOutbox());
        snap.version = 2;
        Assert.Throws<System.ArgumentException>(() => ForgedRecoveryCheckpoint.Restore(snap));
        snap.version = 1;
        snap.mana = null;
        Assert.Throws<System.ArgumentException>(() => ForgedRecoveryCheckpoint.Restore(snap));
    }
    [Test]
    public void MissingManaReservationActionRejectsCheckpoint()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        mana.Reserve(42, 0, 2);
        var snapshot = ForgedRecoveryCheckpoint.Capture(stack, mana,
            new ForgedResolutionJournal(), new ForgedZoneOutbox());
        Assert.Throws<System.ArgumentException>(() => ForgedRecoveryCheckpoint.Restore(snapshot));
    }

    [Test]
    public void OutboxWithoutJournalEntryRejectsCheckpoint()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        var outbox = new ForgedZoneOutbox();
        outbox.Enqueue(new ForgedResolutionJournal.ZoneInstruction {
            actionId = 42, sourceUid = "spell", countered = true
        });
        var snapshot = ForgedRecoveryCheckpoint.Capture(stack, mana,
            new ForgedResolutionJournal(), outbox);
        Assert.Throws<System.ArgumentException>(() => ForgedRecoveryCheckpoint.Restore(snapshot));
    }
    [Test]
    public void ProcessedActionStillInStackRejectsCheckpoint()
    {
        var stack = new ForgedActionStack(2, 0);
        var action = stack.Push(0, "spell", "cast");
        var journal = new ForgedResolutionJournal();
        journal.Record(new ForgedStackResolutionService.Outcome {
            resolvedActionId = action.id, resolvedSourceUid = "spell", isCounter = true
        });
        var snapshot = ForgedRecoveryCheckpoint.Capture(stack, new ForgedManaLedger(),
            journal, new ForgedZoneOutbox());
        Assert.Throws<System.ArgumentException>(() => ForgedRecoveryCheckpoint.Restore(snapshot));
        Assert.AreEqual(action.id, stack.Peek().id);
    }

    [Test]
    public void CompletedJournalActionWithoutPendingStackRestores()
    {
        var journal = new ForgedResolutionJournal();
        journal.Record(new ForgedStackResolutionService.Outcome {
            resolvedActionId = 7, resolvedSourceUid = "counter", isCounter = true
        });
        var snapshot = ForgedRecoveryCheckpoint.Capture(new ForgedActionStack(2, 0),
            new ForgedManaLedger(), journal, new ForgedZoneOutbox());
        Assert.IsNotNull(ForgedRecoveryCheckpoint.Restore(snapshot));
    }

}
#endif
