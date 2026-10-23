#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using TcgEngine;

public class ForgedCheckpointStoreTests
{
    private sealed class MemoryStorage : ForgedCheckpointStore.IStorage
    {
        public ForgedCheckpointIntegrity.Envelope value;
        public void Write(ForgedCheckpointIntegrity.Envelope envelope) { value = envelope; }
        public ForgedCheckpointIntegrity.Envelope Read() { return value; }
    }

    [Test]
    public void SaveLoadRoundTripVerifiesIntegrity()
    {
        var storage = new MemoryStorage();
        var store = new ForgedCheckpointStore(storage);
        store.Save("valid serialized checkpoint");
        Assert.AreEqual("valid serialized checkpoint", store.LoadVerified());
    }

    [Test]
    public void CorruptStorageIsRejectedOnLoad()
    {
        var storage = new MemoryStorage();
        var store = new ForgedCheckpointStore(storage);
        store.Save("before");
        storage.value.payload = "after";
        Assert.Throws<System.InvalidOperationException>(() => store.LoadVerified());
    }
}
#endif
