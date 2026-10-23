using System;
using System.Security.Cryptography;
using System.Text;

namespace TcgEngine
{
    /// <summary>
    /// Integrity envelope over host-serialized recovery snapshots.
    /// The host owns durable and atomic storage; checksums are not signatures.
    /// </summary>
    public static class ForgedCheckpointIntegrity
    {
        [Serializable]
        public sealed class Envelope
        {
            public int formatVersion = 1;
            public string payload;
            public string sha256;
        }

        public static Envelope Seal(string serializedSnapshot)
        {
            if (string.IsNullOrEmpty(serializedSnapshot))
                throw new ArgumentException("Snapshot payload must not be empty");
            return new Envelope { payload = serializedSnapshot, sha256 = Hash(serializedSnapshot) };
        }

        public static string Verify(Envelope envelope)
        {
            if (envelope == null || envelope.formatVersion != 1
                || string.IsNullOrEmpty(envelope.payload)
                || string.IsNullOrEmpty(envelope.sha256))
                throw new ArgumentException("Invalid checkpoint envelope");
            var expected = Hash(envelope.payload);
            if (!string.Equals(expected, envelope.sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Checkpoint integrity mismatch");
            return envelope.payload;
        }

        private static string Hash(string payload)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
