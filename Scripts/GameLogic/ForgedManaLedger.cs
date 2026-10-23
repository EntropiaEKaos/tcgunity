using System;
using System.Collections.Generic;

namespace TcgEngine
{
    /// <summary>
    /// Server-owned mana reservation ledger for pending actions.
    /// Reservations are keyed by unique action ID. This model is NOT yet wired
    /// into Player.mana or the Unity networking transaction boundary.
    /// </summary>
    public sealed class ForgedManaLedger
    {
        private readonly Dictionary<int, int> available = new Dictionary<int, int>();
        private readonly Dictionary<long, Reservation> reservations = new Dictionary<long, Reservation>();

        private sealed class Reservation
        {
            public int playerId;
            public int cost;
            public bool committed;
        }

        public void SetAvailable(int playerId, int mana)
        {
            if (playerId < 0 || mana < 0)
                throw new ArgumentOutOfRangeException();
            foreach (var reservation in reservations.Values)
                if (reservation.playerId == playerId)
                    throw new InvalidOperationException("Cannot reset mana with outstanding reservations");
            available[playerId] = mana;
        }

        public int GetAvailable(int playerId)
        {
            return available.TryGetValue(playerId, out var mana) ? mana : 0;
        }

        public bool Reserve(long actionId, int playerId, int cost)
        {
            if (actionId <= 0 || playerId < 0 || cost < 0 || reservations.ContainsKey(actionId))
                return false;
            int mana = GetAvailable(playerId);
            if (mana < cost)
                return false;
            available[playerId] = mana - cost;
            reservations.Add(actionId, new Reservation { playerId = playerId, cost = cost });
            return true;
        }

        public bool Commit(long actionId)
        {
            if (!reservations.TryGetValue(actionId, out var reservation) || reservation.committed)
                return false;
            reservation.committed = true;
            return true;
        }

        public bool Refund(long actionId)
        {
            if (!reservations.TryGetValue(actionId, out var reservation) || reservation.committed)
                return false;
            checked { available[reservation.playerId] = GetAvailable(reservation.playerId) + reservation.cost; }
            reservations.Remove(actionId);
            return true;
        }

        public bool FinalizeCommitted(long actionId)
        {
            if (!reservations.TryGetValue(actionId, out var reservation) || !reservation.committed)
                return false;
            reservations.Remove(actionId);
            return true;
        }
    }
}
