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
        [Serializable]
        public sealed class BalanceSnapshot
        {
            public int playerId;
            public int availableMana;
        }

        [Serializable]
        public sealed class ReservationSnapshot
        {
            public long actionId;
            public int playerId;
            public int cost;
            public bool committed;
        }

        [Serializable]
        public sealed class Snapshot
        {
            public List<BalanceSnapshot> balances = new List<BalanceSnapshot>();
            public List<ReservationSnapshot> reservations = new List<ReservationSnapshot>();
        }

        public Snapshot ExportSnapshot()
        {
            var result = new Snapshot();
            foreach (var balance in available)
                result.balances.Add(new BalanceSnapshot { playerId = balance.Key, availableMana = balance.Value });
            foreach (var entry in reservations)
                result.reservations.Add(new ReservationSnapshot {
                    actionId = entry.Key, playerId = entry.Value.playerId,
                    cost = entry.Value.cost, committed = entry.Value.committed
                });
            result.balances.Sort((a, b) => a.playerId.CompareTo(b.playerId));
            result.reservations.Sort((a, b) => a.actionId.CompareTo(b.actionId));
            return result;
        }

        public static ForgedManaLedger FromSnapshot(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.balances == null || snapshot.reservations == null)
                throw new ArgumentException("Invalid mana ledger snapshot");
            var ledger = new ForgedManaLedger();
            foreach (var balance in snapshot.balances)
            {
                if (balance == null || balance.playerId < 0 || balance.availableMana < 0
                    || ledger.available.ContainsKey(balance.playerId))
                    throw new ArgumentException("Invalid or repeated mana balance");
                ledger.available.Add(balance.playerId, balance.availableMana);
            }
            foreach (var entry in snapshot.reservations)
            {
                if (entry == null || entry.actionId <= 0 || entry.playerId < 0 || entry.cost < 0
                    || !ledger.available.ContainsKey(entry.playerId)
                    || ledger.reservations.ContainsKey(entry.actionId))
                    throw new ArgumentException("Invalid or repeated mana reservation");
                ledger.reservations.Add(entry.actionId, new Reservation {
                    playerId = entry.playerId, cost = entry.cost, committed = entry.committed
                });
            }
            return ledger;
        }

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
