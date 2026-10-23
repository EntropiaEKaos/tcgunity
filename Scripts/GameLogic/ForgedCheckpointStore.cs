using System;

namespace TcgEngine
{
    /// <summary>
    /// Host-independent checkpoint save/load contract. The host must provide
    /// atomic durable storage of a complete envelope; this type does not.
    /// </summary>
    public sealed class ForgedCheckpointStore
    {
        public interface IStorage
        {
            void Write(ForgedCheckpointIntegrity.Envelope envelope);
            ForgedCheckpointIntegrity.Envelope Read();
        }

        private readonly IStorage storage;

        public ForgedCheckpointStore(IStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Save(string serializedSnapshot)
        {
            storage.Write(ForgedCheckpointIntegrity.Seal(serializedSnapshot));
        }

        public string LoadVerified()
        {
            return ForgedCheckpointIntegrity.Verify(storage.Read());
        }
    }
}
