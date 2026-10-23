using System;

namespace TcgEngine
{
    /// <summary>
    /// Server-side command boundary for FORGED 2 pending action stack.
    /// This is a transport-independent adapter; do NOT instantiate on clients as authority.
    /// Authorization of the caller's player ID is provided by the hosting network layer.
    /// </summary>
    public sealed class ForgedStackCommandGateway
    {
        public enum CommandKind { Play, Counter, Pass, Resolve }
        public sealed class Command
        {
            public CommandKind kind;
            public int playerId;
            public string sourceUid;
            public string abilityId;
            public long targetActionId;
        }

        public sealed class Result
        {
            public bool accepted;
            public bool resolutionReady;
            public long actionId;
            public string reason;
        }

        private readonly ForgedActionStack stack;
        private readonly Func<int, string, bool> ownsSource;

        public ForgedStackCommandGateway(ForgedActionStack stack, Func<int, string, bool> ownsSource)
        {
            this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
            this.ownsSource = ownsSource ?? throw new ArgumentNullException(nameof(ownsSource));
        }

        public Result Execute(int authenticatedPlayerId, Command command)
        {
            if (command == null || authenticatedPlayerId != command.playerId)
                return Reject("Unauthenticated player or missing command");

            // Action resolution is server-internal, never accepted from clients.
            if (command.kind == CommandKind.Resolve)
                return Reject("Resolution is server-only");

            if (command.kind == CommandKind.Pass)
            {
                bool done = stack.Pass(command.playerId);
                // Distinguish an accepted intermediate pass from a rejected pass.
                if (!done && stack.PriorityPlayer != (command.playerId + 1) % stack.ExportSnapshot().playerCount)
                    return Reject("Priority denied");
                return new Result { accepted = true, resolutionReady = done };
            }

            if (command.kind == CommandKind.Play)
            {
                if (string.IsNullOrEmpty(command.sourceUid)
                    || !ownsSource(command.playerId, command.sourceUid))
                    return Reject("Source not controlled by player");
                try
                {
                    var entry = stack.Push(command.playerId, command.sourceUid, command.abilityId);
                    return new Result { accepted = true, actionId = entry.id };
                }
                catch (InvalidOperationException)
                {
                    return Reject("Priority denied");
                }
            }

            if (command.kind == CommandKind.Counter)
            {
                if (stack.Counter(command.playerId, command.targetActionId))
                    return new Result { accepted = true };
                return Reject("Invalid counter target or priority");
            }
            return Reject("Unknown command");
        }

        private static Result Reject(string reason)
        {
            return new Result { accepted = false, reason = reason };
        }
    }
}
