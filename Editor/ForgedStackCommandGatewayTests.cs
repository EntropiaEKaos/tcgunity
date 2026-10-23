#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedStackCommandGatewayTests
{
    private static ForgedStackCommandGateway.Command Cmd(
        ForgedStackCommandGateway.CommandKind kind, int player, string source = null, long target = 0)
    {
        return new ForgedStackCommandGateway.Command
        {
            kind = kind, playerId = player, sourceUid = source,
            abilityId = "cast", targetActionId = target
        };
    }

    [Test]
    public void RejectsImpersonationAndForeignSource()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => player == 0 && uid == "mine",
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => player == 0 && uid == "mine", null,
            (player, uid, ability) => true, paid, (player, uid, ability) => 2);
        Assert.IsFalse(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "mine")).accepted);
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "theirs")).accepted);
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void RejectsInvalidPriorityPassAndClientResolution()
    {
        var stack = new ForgedActionStack(2, 0);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true);
        Assert.IsFalse(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Pass, 1)).accepted);
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Resolve, 0)).accepted);
        Assert.AreEqual(0, stack.PriorityPlayer);
    }

    [Test]
    public void AcceptsOwnedActionAndPassesInOrder()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => player == 0 && uid == "mine",
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => player == 0 && uid == "mine", null,
            (player, uid, ability) => true, paid, (player, uid, ability) => 2);
        var added = gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "mine"));
        Assert.IsTrue(added.accepted);
        Assert.Greater(added.actionId, 0);
        var first = gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Pass, 1));
        Assert.IsTrue(first.accepted);
        Assert.IsFalse(first.resolutionReady);
        var second = gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Pass, 0));
        Assert.IsTrue(second.accepted);
        Assert.IsTrue(second.resolutionReady);
        Assert.AreEqual(added.actionId, stack.PopTop().id);
    }
    [Test]
    public void CounterFailsClosedWithoutServerRuleValidator()
    {
        var stack = new ForgedActionStack(2, 0);
        stack.Push(0, "spell", "play");
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true);
        var result = gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter-card", 1));
        Assert.IsFalse(result.accepted);
        Assert.AreEqual(1, stack.Count);
    }

    [Test]
    public void CounterRequiresOwnedCardAndExplicitServerApproval()
    {
        var stack = new ForgedActionStack(2, 0);
        var spell = stack.Push(0, "spell", "play");
        var mana = new ForgedManaLedger();
        mana.SetAvailable(1, 5);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => player == 1 && uid == "counter-card",
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack,
            (player, uid) => player == 1 && uid == "counter-card",
            (player, uid, actionId) => actionId == spell.id, null, paid,
            (player, uid, ability) => 2);
        Assert.IsFalse(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "stolen", spell.id)).accepted);
        Assert.AreEqual(1, stack.Count);
        Assert.IsTrue(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter-card", spell.id)).accepted);
        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(spell.id, stack.ExportSnapshot().entries[0].id);
        Assert.IsTrue(stack.Peek().IsCounter);
        Assert.AreEqual(3, mana.GetAvailable(1));
    }

    [Test]
    public void InvalidCounterDoesNotInvokeRuleHookOrChangeStack()
    {
        var stack = new ForgedActionStack(2, 0);
        var spell = stack.Push(0, "original", "cast");
        int validations = 0;
        var gateway = new ForgedStackCommandGateway(stack,
            (player, uid) => true,
            (player, uid, id) => { validations++; return true; });
        Assert.IsFalse(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter", spell.id + 999)).accepted);
        Assert.AreEqual(0, validations);
        Assert.AreEqual(spell.id, stack.Peek().id);
        Assert.AreEqual(1, stack.PriorityPlayer);
    }

    [Test]
    public void OutOfPriorityCounterDoesNotInvokeRuleHook()
    {
        var stack = new ForgedActionStack(2, 0);
        var spell = stack.Push(0, "original", "cast");
        int validations = 0;
        var gateway = new ForgedStackCommandGateway(stack,
            (player, uid) => true,
            (player, uid, id) => { validations++; return true; });
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 0, "counter", spell.id)).accepted);
        Assert.AreEqual(0, validations);
        Assert.AreEqual(1, stack.Count);
    }

    [Test]
    public void RuleCallbackCannotSubmitNestedCommand()
    {
        var stack = new ForgedActionStack(2, 0);
        var spell = stack.Push(0, "original", "cast");
        ForgedStackCommandGateway gateway = null;
        bool nestedAccepted = true;
        var mana = new ForgedManaLedger();
        mana.SetAvailable(1, 5);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => true,
            (player, uid, ability) => true);
        gateway = new ForgedStackCommandGateway(stack,
            (player, uid) => true,
            (player, uid, id) =>
            {
                nestedAccepted = gateway.Execute(1,
                    Cmd(ForgedStackCommandGateway.CommandKind.Pass, 1)).accepted;
                return true;
            }, null, paid, (player, uid, ability) => 2);
        var counter = gateway.Execute(1,
            Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter", spell.id));
        Assert.IsTrue(counter.accepted);
        Assert.IsFalse(nestedAccepted);
        Assert.AreEqual(2, stack.Count);
        Assert.AreEqual(0, stack.PriorityPlayer);
    }

    [Test]
    public void RuleCallbackExceptionDoesNotLeaveGatewayLocked()
    {
        var stack = new ForgedActionStack(2, 0);
        stack.Push(0, "original", "cast");
        var mana = new ForgedManaLedger();
        mana.SetAvailable(1, 5);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => true,
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack,
            (player, uid) => true,
            (player, uid, id) => throw new System.InvalidOperationException("Rule failed"),
            null, paid, (player, uid, ability) => 2);
        Assert.Throws<System.InvalidOperationException>(() =>
            gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter", 1)));
        Assert.AreEqual(1, stack.Count);
        Assert.IsTrue(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Pass, 1)).accepted);
    }

    [Test]
    public void PlayFailsClosedWhenLegalityValidatorMissing()
    {
        var stack = new ForgedActionStack(2, 0);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true);
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "my-card")).accepted);
        Assert.AreEqual(0, stack.Count);
        Assert.AreEqual(0, stack.PriorityPlayer);
    }

    [Test]
    public void MissingPaidServiceRejectsBeforeRuleValidation()
    {
        var stack = new ForgedActionStack(2, 0);
        int validations = 0;
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true, null,
            (player, uid, ability) => { validations++; return false; });
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "my-card")).accepted);
        Assert.AreEqual(0, validations);
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void ConfiguredPaidServiceRejectsPlayWhenRulesDeny()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 5);
        int validations = 0;
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => true,
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true, null,
            (player, uid, ability) => { validations++; return false; },
            paid, (player, uid, ability) => 2);
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "card")).accepted);
        Assert.AreEqual(1, validations);
        Assert.AreEqual(5, mana.GetAvailable(0));
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void GatewayRejectsFreePlayWithoutPaidService()
    {
        var stack = new ForgedActionStack(2, 0);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true, null,
            (player, uid, ability) => true);
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "card")).accepted);
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void GatewayPaidCastChargesManaAndCreatesOneAction()
    {
        var stack = new ForgedActionStack(2, 0);
        var mana = new ForgedManaLedger();
        mana.SetAvailable(0, 4);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => true,
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true, null,
            (player, uid, ability) => true, paid, (player, uid, ability) => 3);
        var result = gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "card"));
        Assert.IsTrue(result.accepted);
        Assert.AreEqual(1, mana.GetAvailable(0));
        Assert.AreEqual(1, stack.Count);
        Assert.IsFalse(gateway.Execute(0, Cmd(ForgedStackCommandGateway.CommandKind.Play, 0, "card")).accepted);
        Assert.AreEqual(1, mana.GetAvailable(0));
    }

    [Test]
    public void CounterWithInsufficientManaLeavesOriginalPending()
    {
        var stack = new ForgedActionStack(2, 0);
        var original = stack.Push(0, "original", "cast");
        var mana = new ForgedManaLedger();
        mana.SetAvailable(1, 1);
        var paid = new ForgedPaidActionService(stack, mana, (player, uid) => true,
            (player, uid, ability) => true);
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => true,
            (player, uid, id) => true, null, paid, (player, uid, ability) => 2);
        Assert.IsFalse(gateway.Execute(1,
            Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter", original.id)).accepted);
        Assert.AreEqual(1, mana.GetAvailable(1));
        Assert.AreEqual(1, stack.Count);
        Assert.AreEqual(original.id, stack.Peek().id);
        Assert.AreEqual(1, stack.PriorityPlayer);
    }

}
#endif
