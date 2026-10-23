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
    [Test]
    public void LedgerRestorePreservesReservedManaAndCommitState()
    {
        var ledger = new ForgedManaLedger();
        ledger.SetAvailable(0, 8);
        Assert.IsTrue(ledger.Reserve(3, 0, 4));
        Assert.IsTrue(ledger.Commit(3));
        var restored = ForgedManaLedger.FromSnapshot(ledger.ExportSnapshot());
        Assert.AreEqual(4, restored.GetAvailable(0));
        Assert.IsFalse(restored.Refund(3));
        Assert.IsTrue(restored.FinalizeCommitted(3));
    }

    [Test]
    public void LedgerRestoreRejectsDuplicateActionReservations()
    {
        var ledger = new ForgedManaLedger();
        ledger.SetAvailable(0, 8);
        ledger.Reserve(3, 0, 2);
        var snapshot = ledger.ExportSnapshot();
        snapshot.reservations.Add(new ForgedManaLedger.ReservationSnapshot
        { actionId = 3, playerId = 0, cost = 2 });
        Assert.Throws<System.ArgumentException>(() => ForgedManaLedger.FromSnapshot(snapshot));
    }

}
#endif
