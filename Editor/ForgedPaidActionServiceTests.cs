#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedPaidActionServiceTests
{
    [Test]
    public void InsufficientManaDoesNotPushOrAdvancePriority()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 2);
        var service = new ForgedPaidActionService(stack, mana, (player, card) => true,
            (player, card, ability) => true);
        Assert.AreEqual(0, service.TryPlay(0, 0, "spell", "cast", 3));
        Assert.AreEqual(0, stack.Count);
        Assert.AreEqual(0, stack.PriorityPlayer);
        Assert.AreEqual(2, mana.GetAvailable(0));
    }

    [Test]
    public void ValidPlayReservesExactlyOnceBeforePriorityPass()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        var service = new ForgedPaidActionService(stack, mana, (player, card) => true,
            (player, card, ability) => true);
        var id = service.TryPlay(0, 0, "spell", "cast", 3);
        Assert.Greater(id, 0);
        Assert.AreEqual(2, mana.GetAvailable(0));
        Assert.AreEqual(1, stack.Count);
        Assert.AreEqual(1, stack.PriorityPlayer);
        Assert.AreEqual(0, service.TryPlay(0, 0, "spell", "cast", 3));
        Assert.AreEqual(2, mana.GetAvailable(0));
    }

    [Test]
    public void RejectedOwnershipAndRulesNeverReserveMana()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        var service = new ForgedPaidActionService(stack, mana, (player, card) => false,
            (player, card, ability) => true);
        Assert.AreEqual(0, service.TryPlay(0, 0, "spell", "cast", 3));
        Assert.AreEqual(5, mana.GetAvailable(0));
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void FailedLegalityCallbackDoesNotLeaveServiceLocked()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        var service = new ForgedPaidActionService(stack, mana, (player, card) => true,
            (player, card, ability) => { if (ability == "bad") throw new System.InvalidOperationException(); return true; });
        Assert.Throws<System.InvalidOperationException>(() => service.TryPlay(0, 0, "spell", "bad", 3));
        Assert.AreEqual(5, mana.GetAvailable(0));
        Assert.Greater(service.TryPlay(0, 0, "spell", "cast", 3), 0);
    }
}
#endif
