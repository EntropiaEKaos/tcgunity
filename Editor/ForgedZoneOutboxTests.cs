#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedZoneOutboxTests
{
    private static ForgedResolutionJournal.ZoneInstruction Instruction(long id, string uid = "spell")
    {
        return new ForgedResolutionJournal.ZoneInstruction { actionId = id, sourceUid = uid };
    }

    [Test]
    public void PendingRemainsUntilHostAcknowledges()
    {
        var box = new ForgedZoneOutbox();
        Assert.IsTrue(box.Enqueue(Instruction(3)));
        Assert.AreEqual(1, box.Pending().Count);
        Assert.IsTrue(box.Acknowledge(3));
        Assert.IsFalse(box.Acknowledge(3));
        Assert.AreEqual(0, box.Pending().Count);
    }

    [Test]
    public void ReplayAfterRestoreRetainsUnacknowledgedInstruction()
    {
        var box = new ForgedZoneOutbox();
        box.Enqueue(Instruction(1));
        box.Enqueue(Instruction(2, "counter"));
        box.Acknowledge(1);
        var copy = ForgedZoneOutbox.FromSnapshot(box.ExportSnapshot());
        Assert.AreEqual(1, copy.Pending().Count);
        Assert.AreEqual(2, copy.Pending()[0].actionId);
        Assert.IsFalse(copy.Enqueue(Instruction(2, "counter")));
    }

    [Test]
    public void ConflictingActionIdCannotReplacePendingInstruction()
    {
        var box = new ForgedZoneOutbox();
        box.Enqueue(Instruction(1, "first"));
        Assert.Throws<System.InvalidOperationException>(() => box.Enqueue(Instruction(1, "other")));
        Assert.AreEqual("first", box.Pending()[0].sourceUid);
    }

    [Test]
    public void InvalidSnapshotFailsClosed()
    {
        var snapshot = new ForgedZoneOutbox.Snapshot();
        snapshot.items.Add(new ForgedZoneOutbox.Item { actionId = 1, sourceUid = "a" });
        snapshot.items.Add(new ForgedZoneOutbox.Item { actionId = 1, sourceUid = "b" });
        Assert.Throws<System.ArgumentException>(() => ForgedZoneOutbox.FromSnapshot(snapshot));
    }
}
#endif
