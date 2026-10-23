#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedCheckpointIntegrityTests
{
    [Test]
    public void SealedCheckpointReturnsOriginalPayload()
    {
        var envelope = ForgedCheckpointIntegrity.Seal("{\\"version\\":1}");
        Assert.AreEqual("{\\"version\\":1}", ForgedCheckpointIntegrity.Verify(envelope));
        Assert.AreEqual(64, envelope.sha256.Length);
    }

    [Test]
    public void PayloadTamperingIsRejected()
    {
        var envelope = ForgedCheckpointIntegrity.Seal("snapshot-v1");
        envelope.payload = "snapshot-v2";
        Assert.Throws<System.InvalidOperationException>(() => ForgedCheckpointIntegrity.Verify(envelope));
    }

    [Test]
    public void UnsupportedEnvelopeVersionIsRejected()
    {
        var envelope = ForgedCheckpointIntegrity.Seal("snapshot");
        envelope.formatVersion = 2;
        Assert.Throws<System.ArgumentException>(() => ForgedCheckpointIntegrity.Verify(envelope));
    }
}
#endif
