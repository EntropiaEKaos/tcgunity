#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;
using TcgEngine.Gameplay;
public class ForgedUnityGraveyardAdapterContractTests
{
    private static ForgedZoneOutbox BuildOutbox()
    {
        var box = new ForgedZoneOutbox();
        box.Enqueue(new ForgedResolutionJournal.ZoneInstruction { actionId = 1, sourceUid = "spell", countered = true });
        return box;
    }

    [Test]
    public void AdapterCallsHostDiscardThenAcknowledges()
    {
        var game = new Game();
        game.Add(new Card { uid = "spell" });
        var logic = new GameLogic { GameData = game };
        var box = BuildOutbox();
        var bridge = new ForgedUnityGraveyardAdapter(logic, box);
        Assert.AreEqual(1, bridge.ProcessPending());
        Assert.AreEqual(1, logic.discardCalls);
        Assert.AreEqual(0, box.Pending().Count);
        Assert.AreEqual(0, bridge.ProcessPending());
    }

    [Test]
    public void MissingCardRemainsPending()
    {
        var logic = new GameLogic { GameData = new Game() };
        var box = BuildOutbox();
        Assert.AreEqual(0, new ForgedUnityGraveyardAdapter(logic, box).ProcessPending());
        Assert.AreEqual(1, box.Pending().Count);
    }

    [Test]
    public void HostExceptionLeavesInstructionPending()
    {
        var game = new Game();
        game.Add(new Card { uid = "spell" });
        var logic = new GameLogic { GameData = game, failBeforeDiscard = true };
        var box = BuildOutbox();
        var bridge = new ForgedUnityGraveyardAdapter(logic, box);
        Assert.Throws<System.InvalidOperationException>(() => bridge.ProcessPending());
        Assert.AreEqual(1, box.Pending().Count);
        logic.failBeforeDiscard = false;
        Assert.AreEqual(1, bridge.ProcessPending());
    }

    [Test]
    public void AlreadyDiscardedCardDoesNotTriggerSecondDiscard()
    {
        var game = new Game();
        var card = new Card { uid = "spell" };
        game.Add(card);
        game.MarkDiscard(card);
        var logic = new GameLogic { GameData = game };
        Assert.AreEqual(1, new ForgedUnityGraveyardAdapter(logic, BuildOutbox()).ProcessPending());
        Assert.AreEqual(0, logic.discardCalls);
    }
}
#endif
