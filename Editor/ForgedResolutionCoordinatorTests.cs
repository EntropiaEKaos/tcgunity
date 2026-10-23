#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;
public class ForgedResolutionCoordinatorTests
{
    [Test]
    public void CounterResolutionQueuesTwoZoneMovesAndFinalizesCosts()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        mana.SetAvailable(1, 5);
        var paid = new ForgedPaidActionService(stack, mana,
            (player, card) => true, (player, card, ability) => true);
        long spell = paid.TryPlay(0, 0, "spell", "cast", 2);
        long counter = paid.TryCounter(1, 1, "counter", "deny", spell, 3,
            (player, card, id) => true);
        stack.Pass(0);
        stack.Pass(1);
        var outbox = new ForgedZoneOutbox();
        var coordinator = new ForgedResolutionCoordinator(
            new ForgedStackResolutionService(stack, entry => true),
            new ForgedResolutionJournal(), outbox, paid);
        var outcome = coordinator.ResolveOne();
        Assert.IsTrue(outcome.counterSucceeded);
        Assert.AreEqual(2, outbox.Pending().Count);
        Assert.AreEqual(3, mana.GetAvailable(0));
        Assert.AreEqual(2, mana.GetAvailable(1));
        Assert.IsFalse(paid.RefundUncommitted(spell));
        Assert.IsFalse(paid.RefundUncommitted(counter));
    }

    [Test]
    public void NormalActionDoesNotGenerateCounterGraveyardMoves()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 3);
        var paid = new ForgedPaidActionService(stack, mana,
            (player, card) => true, (player, card, ability) => true);
        long spell = paid.TryPlay(0, 0, "creature", "cast", 1);
        stack.Pass(1);
        stack.Pass(0);
        var outbox = new ForgedZoneOutbox();
        var coordinator = new ForgedResolutionCoordinator(
            new ForgedStackResolutionService(stack, entry => true),
            new ForgedResolutionJournal(), outbox, paid);
        Assert.IsFalse(coordinator.ResolveOne().isCounter);
        Assert.AreEqual(0, outbox.Pending().Count);
        Assert.IsFalse(paid.RefundUncommitted(spell));
    }
}
#endif
