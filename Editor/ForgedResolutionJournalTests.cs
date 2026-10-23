#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;
public class ForgedResolutionJournalTests
{
    [Test]
    public void SuccessfulCounterProducesTwoUniqueZoneInstructions()
    {
        var journal = new ForgedResolutionJournal();
        var result = new ForgedStackResolutionService.Outcome
        {
            resolvedActionId = 2, resolvedSourceUid = "counter", isCounter = true,
            counterSucceeded = true, counteredActionId = 1, counteredSourceUid = "spell"
        };
        var instructions = journal.Record(result);
        Assert.AreEqual(2, instructions.Count);
        Assert.AreEqual("counter", instructions[0].sourceUid);
        Assert.AreEqual("spell", instructions[1].sourceUid);
        Assert.IsTrue(instructions[1].countered);
        Assert.AreEqual(0, journal.Record(result).Count);
    }

    [Test]
    public void FizzledCounterOnlyProducesItsOwnZoneInstruction()
    {
        var journal = new ForgedResolutionJournal();
        var outcome = new ForgedStackResolutionService.Outcome
        {
            resolvedActionId = 3, resolvedSourceUid = "counter", isCounter = true
        };
        Assert.AreEqual(1, journal.Record(outcome).Count);
        Assert.AreEqual(0, journal.Record(outcome).Count);
    }

    [Test]
    public void PlainPermanentResolutionDoesNotForceItToGraveyard()
    {
        var journal = new ForgedResolutionJournal();
        var outcome = new ForgedStackResolutionService.Outcome
        {
            resolvedActionId = 7, resolvedSourceUid = "creature", isCounter = false
        };
        Assert.AreEqual(0, journal.Record(outcome).Count);
    }
}
#endif
