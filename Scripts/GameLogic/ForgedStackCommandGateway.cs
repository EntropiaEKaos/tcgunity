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
        private readonly Func<int, string, string, bool> canPlay;
        private readonly ForgedPaidActionService paidActions;
        private readonly Func<int, string, string, int> manaCost;
        private bool executing;

        public ForgedStackCommandGateway(ForgedActionStack stack, Func<int, string, bool> ownsSource,
            Func<int, string, long, bool> canCounter = null,
            Func<int, string, string, bool> canPlay = null,
            ForgedPaidActionService paidActions = null,
            Func<int, string, string, int> manaCost = null)
        {
            this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
            this.ownsSource = ownsSource ?? throw new ArgumentNullException(nameof(ownsSource));
            this.canCounter = canCounter;
            this.canPlay = canPlay;
            this.paidActions = paidActions;
            this.manaCost = manaCost;
        }

        public Result Execute(int authenticatedPlayerId, Command command)
        {
            if (executing)
                return Reject("Nested command execution denied");
            executing = true;
            try
            {
                return ExecuteCore(authenticatedPlayerId, command);
            }
            finally
            {
                executing = false;
            }
        }

        private Result ExecuteCore(int authenticatedPlayerId, Command command)
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
                // Fail closed until host supplies a single paid casting path.
                if (paidActions == null || manaCost == null)
                    return Reject("Paid action service not configured");
                if (stack.ResolutionReady || stack.PriorityPlayer != command.playerId)
                    return Reject("Priority denied");
                if (string.IsNullOrEmpty(command.sourceUid)
                    || !ownsSource(command.playerId, command.sourceUid)
                    || canPlay == null
                    || !canPlay(command.playerId, command.sourceUid, command.abilityId))
                    return Reject("Play source or rules denied");
                int cost = manaCost(command.playerId, command.sourceUid, command.abilityId);
                long id = paidActions.TryPlay(authenticatedPlayerId, command.playerId,
                    command.sourceUid, command.abilityId, cost);
                return id > 0 ? new Result { accepted = true, actionId = id }
                    : Reject("Mana or cast rules denied");
            }

            if (command.kind == CommandKind.Counter)
            {
                if (paidActions == null || manaCost == null || canCounter == null)
                    return Reject("Paid counter service not configured");
                if (stack.ResolutionReady || stack.PriorityPlayer != command.playerId
                    || !stack.HasPendingAction(command.targetActionId))
                    return Reject("No pending target or priority denied");
                if (string.IsNullOrEmpty(command.sourceUid)
                    || !ownsSource(command.playerId, command.sourceUid))
                    return Reject("Counter source denied");
                int cost = manaCost(command.playerId, command.sourceUid, command.abilityId);
                long id = paidActions.TryCounter(authenticatedPlayerId, command.playerId,
                    command.sourceUid, command.abilityId, command.targetActionId, cost, canCounter);
                return id > 0 ? new Result { accepted = true, actionId = id }
                    : Reject("Mana or counter rules denied");
            }
            return Reject("Unknown command");
        }

        private static Result Reject(string reason)
        {
            return new Result { accepted = false, reason = reason };
        }
    }
}
