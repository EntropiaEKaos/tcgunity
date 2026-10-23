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
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => player == 0 && uid == "mine");
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
        var gateway = new ForgedStackCommandGateway(stack, (player, uid) => player == 0 && uid == "mine");
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
        var gateway = new ForgedStackCommandGateway(stack,
            (player, uid) => player == 1 && uid == "counter-card",
            (player, uid, actionId) => actionId == spell.id);
        Assert.IsFalse(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "stolen", spell.id)).accepted);
        Assert.AreEqual(1, stack.Count);
        Assert.IsTrue(gateway.Execute(1, Cmd(ForgedStackCommandGateway.CommandKind.Counter, 1, "counter-card", spell.id)).accepted);
        Assert.AreEqual(0, stack.Count);
    }

}
#endif
