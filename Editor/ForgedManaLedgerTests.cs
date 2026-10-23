#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedManaLedgerTests
{
    [Test]
    public void RejectedOrRepeatedReservationCannotChargeTwice()
    {
        var ledger = new ForgedManaLedger();
        ledger.SetAvailable(0, 5);
        Assert.IsTrue(ledger.Reserve(1, 0, 3));
        Assert.IsFalse(ledger.Reserve(1, 0, 3));
        Assert.IsFalse(ledger.Reserve(2, 0, 3));
        Assert.AreEqual(2, ledger.GetAvailable(0));
    }

    [Test]
    public void RefundIsIdempotentAndRestoresMana()
    {
        var ledger = new ForgedManaLedger();
        ledger.SetAvailable(0, 5);
        Assert.IsTrue(ledger.Reserve(1, 0, 4));
        Assert.IsTrue(ledger.Refund(1));
        Assert.IsFalse(ledger.Refund(1));
        Assert.AreEqual(5, ledger.GetAvailable(0));
    }

    [Test]
    public void CommittedCostCannotBeRefundedOrRepeated()
    {
        var ledger = new ForgedManaLedger();
        ledger.SetAvailable(0, 6);
        Assert.IsTrue(ledger.Reserve(7, 0, 4));
        Assert.IsTrue(ledger.Commit(7));
        Assert.IsFalse(ledger.Commit(7));
        Assert.IsFalse(ledger.Refund(7));
        Assert.IsTrue(ledger.FinalizeCommitted(7));
        Assert.AreEqual(2, ledger.GetAvailable(0));
    }

    [Test]
    public void RejectedNegativeCostDoesNotCreditMana()
    {
        var ledger = new ForgedManaLedger();
        ledger.SetAvailable(0, 2);
        Assert.IsFalse(ledger.Reserve(1, 0, -10));
        Assert.IsFalse(ledger.Reserve(0, 0, 1));
        Assert.AreEqual(2, ledger.GetAvailable(0));
    }
}
#endif
