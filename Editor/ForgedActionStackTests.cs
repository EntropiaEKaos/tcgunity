#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedActionStackTests
{
    [Test]
    public void ResolvesInLifoOrderOnlyAfterBothPlayersPass()
    {
        var stack = new ForgedActionStack(2, 0);
        var first = stack.Push(0, "spell-1", "ability");
        var second = stack.Push(1, "spell-2", "counter");

        Assert.Throws<System.InvalidOperationException>(() => stack.PopTop());
        Assert.IsFalse(stack.Pass(0));
        Assert.IsTrue(stack.Pass(1));
        Assert.IsTrue(stack.ResolutionReady);
        Assert.AreEqual(second.id, stack.PopTop().id);
        Assert.IsFalse(stack.ResolutionReady);
        Assert.AreEqual(first.id, stack.Peek().id);
    }

    [Test]
    public void CounterRequiresPriorityAndOnlyRemovesRequestedId()
    {
        var stack = new ForgedActionStack(2, 0);
        var original = stack.Push(0, "spell", "cast");
        Assert.IsFalse(stack.Counter(0, original.id));
        Assert.IsFalse(stack.Counter(1, 9999));
        Assert.IsTrue(stack.Counter(1, original.id));
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void FourPlayerPriorityWrapsAndRequiresFourPasses()
    {
        var stack = new ForgedActionStack(4, 3);
        stack.Push(3, "spell", "cast");
        Assert.AreEqual(0, stack.PriorityPlayer);
        Assert.IsFalse(stack.Pass(0));
        Assert.IsFalse(stack.Pass(1));
        Assert.IsFalse(stack.Pass(2));
        Assert.IsTrue(stack.Pass(3));
        Assert.IsNotNull(stack.PopTop());
        Assert.AreEqual(3, stack.PriorityPlayer);
    }

    [Test]
    public void InvalidPassCannotAdvancePriority()
    {
        var stack = new ForgedActionStack(2, 0);
        stack.Push(0, "spell", "cast");
        Assert.IsFalse(stack.Pass(0));
        Assert.AreEqual(1, stack.PriorityPlayer);
    }
}
#endif
