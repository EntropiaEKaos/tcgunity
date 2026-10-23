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
        private readonly Func<int, string, long, bool> canCounter;

        public ForgedStackCommandGateway(ForgedActionStack stack, Func<int, string, bool> ownsSource,
            Func<int, string, long, bool> canCounter = null)
        {
            this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
            this.ownsSource = ownsSource ?? throw new ArgumentNullException(nameof(ownsSource));
            this.canCounter = canCounter;
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
                if (stack.ResolutionReady || stack.PriorityPlayer != command.playerId)
                    return Reject("Priority denied");
                bool done = stack.Pass(command.playerId);
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
                // Never accept a free counter solely on knowledge of an action ID.
                // The host supplies a rule/cost validator. No validator means deny.
                if (string.IsNullOrEmpty(command.sourceUid)
                    || !ownsSource(command.playerId, command.sourceUid)
                    || canCounter == null
                    || !canCounter(command.playerId, command.sourceUid, command.targetActionId))
                    return Reject("Counter source or rules denied");
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
