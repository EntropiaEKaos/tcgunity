using System;

namespace TcgEngine
{
    /// <summary>
    /// Server-only resolver for one ready stack entry. Reports zone transitions
    /// as intent for the authoritative GameLogic host to apply.
    /// Does not mutate GameLogic card zones or network state by itself.
    /// </summary>
    public sealed class ForgedStackResolutionService
    {
        public sealed class Outcome
        {
            public long resolvedActionId;
            public string resolvedSourceUid;
            public bool isCounter;
            public bool counterSucceeded;
            public long counteredActionId;
            public string counteredSourceUid;
        }

        private readonly ForgedActionStack stack;
        private readonly Func<ForgedActionStack.Entry, bool> canResolveCounter;

        public ForgedStackResolutionService(ForgedActionStack stack,
            Func<ForgedActionStack.Entry, bool> canResolveCounter)
        {
            this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
            this.canResolveCounter = canResolveCounter ?? throw new ArgumentNullException(nameof(canResolveCounter));
        }

        public Outcome ResolveReady()
        {
            if (!stack.ResolutionReady)
                throw new InvalidOperationException("Priority round incomplete");
            var top = stack.Peek();
            var target = top != null && top.IsCounter ? stack.FindPendingAction(top.counterTargetId) : null;
            var allowed = top != null && top.IsCounter && target != null && canResolveCounter(top);
            var resolved = stack.PopTop();
            if (resolved == null)
                return null;

            var outcome = new Outcome
            {
                resolvedActionId = resolved.id,
                resolvedSourceUid = resolved.sourceUid,
                isCounter = resolved.IsCounter
            };
            if (resolved.IsCounter && allowed && stack.ApplyResolvedCounter(resolved))
            {
                outcome.counterSucceeded = true;
                outcome.counteredActionId = target.id;
                outcome.counteredSourceUid = target.sourceUid;
            }
            return outcome;
        }
    }
}
