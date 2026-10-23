#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;
public class ForgedZoneOutboxProcessorTests
{
    private static ForgedResolutionJournal.ZoneInstruction Instruction(long id)
    {
        return new ForgedResolutionJournal.ZoneInstruction { actionId = id, sourceUid = "spell" };
    }

    [Test]
    public void SuccessAcknowledgesOnce()
    {
        var outbox = new ForgedZoneOutbox();
        outbox.Enqueue(Instruction(1));
        int calls = 0;
        var processor = new ForgedZoneOutboxProcessor(outbox,
            (id, uid, countered) => { calls++; return true; });
        Assert.AreEqual(1, processor.ProcessPending());
        Assert.AreEqual(0, processor.ProcessPending());
        Assert.AreEqual(1, calls);
    }

    [Test]
    public void FailedMoveRemainsPendingForRetry()
    {
        var outbox = new ForgedZoneOutbox();
        outbox.Enqueue(Instruction(1));
        int attempts = 0;
        var processor = new ForgedZoneOutboxProcessor(outbox,
            (id, uid, countered) => ++attempts > 1);
        Assert.AreEqual(0, processor.ProcessPending());
        Assert.AreEqual(1, outbox.Pending().Count);
        Assert.AreEqual(1, processor.ProcessPending());
        Assert.AreEqual(0, outbox.Pending().Count);
    }

    [Test]
    public void ExceptionDoesNotAcknowledgeAndAllowsRetry()
    {
        var outbox = new ForgedZoneOutbox();
        outbox.Enqueue(Instruction(1));
        var bad = new ForgedZoneOutboxProcessor(outbox,
            (id, uid, countered) => throw new System.InvalidOperationException("Host failure"));
        Assert.Throws<System.InvalidOperationException>(() => bad.ProcessPending());
        Assert.AreEqual(1, outbox.Pending().Count);
        var good = new ForgedZoneOutboxProcessor(outbox, (id, uid, countered) => true);
        Assert.AreEqual(1, good.ProcessPending());
    }
}
#endif
