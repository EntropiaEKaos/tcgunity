#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;
public class ForgedStackResolutionServiceTests
{
    [Test]
    public void SuccessfulCounterReportsBothSourcesForHostZones()
    {
        var stack = new ForgedActionStack(2, 0);
        var original = stack.Push(0, "spell", "cast");
        var counter = stack.PushCounter(1, "counter", "deny", original.id);
        stack.Pass(0); stack.Pass(1);
        var resolver = new ForgedStackResolutionService(stack, entry => true);
        var result = resolver.ResolveReady();
        Assert.AreEqual(counter.id, result.resolvedActionId);
        Assert.IsTrue(result.counterSucceeded);
        Assert.AreEqual("spell", result.counteredSourceUid);
        Assert.AreEqual("counter", result.resolvedSourceUid);
        Assert.AreEqual(0, stack.Count);
    }

    [Test]
    public void CounteredCounterLeavesOriginalPending()
    {
        var stack = new ForgedActionStack(2, 0);
        var original = stack.Push(0, "spell", "cast");
        var counter = stack.PushCounter(1, "counter", "deny", original.id);
        stack.PushCounter(0, "counter-counter", "protect", counter.id);
        stack.Pass(1); stack.Pass(0);
        var result = new ForgedStackResolutionService(stack, entry => true).ResolveReady();
        Assert.IsTrue(result.counterSucceeded);
        Assert.AreEqual("counter", result.counteredSourceUid);
        Assert.IsTrue(stack.HasPendingAction(original.id));
        Assert.IsFalse(stack.HasPendingAction(counter.id));
    }

    [Test]
    public void InvalidatedCounterStillResolvesItsSourceOnly()
    {
        var stack = new ForgedActionStack(2, 0);
        var original = stack.Push(0, "spell", "cast");
        stack.PushCounter(1, "counter", "deny", original.id);
        stack.Counter(0, original.id);
        stack.Pass(1); stack.Pass(0);
        var result = new ForgedStackResolutionService(stack, entry => true).ResolveReady();
        Assert.IsFalse(result.counterSucceeded);
        Assert.AreEqual("counter", result.resolvedSourceUid);
        Assert.IsNull(result.counteredSourceUid);
    }
}
#endif
