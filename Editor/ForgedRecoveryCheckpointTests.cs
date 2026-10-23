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
        outbox.Enqueue(new ForgedResolutionJournal.ZoneInstruction {
            actionId = 99, sourceUid = "previous-spell", countered = true
        });
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
}
#endif
