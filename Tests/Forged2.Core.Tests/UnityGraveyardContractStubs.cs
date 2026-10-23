using System;
using System.Collections.Generic;

namespace TcgEngine
{
    public class Card { public string uid; }
    public class Game
    {
        private readonly Dictionary<string, Card> cards = new Dictionary<string, Card>();
        private readonly HashSet<string> discarded = new HashSet<string>();
        public void Add(Card card) { cards.Add(card.uid, card); }
        public Card GetCard(string uid) { return cards.TryGetValue(uid, out var card) ? card : null; }
        public bool IsInDiscard(Card card) { return discarded.Contains(card.uid); }
        public void MarkDiscard(Card card) { discarded.Add(card.uid); }
    }
}

namespace TcgEngine.Gameplay
{
    public class GameLogic
    {
        public TcgEngine.Game GameData { get; set; }
        public int discardCalls;
        public bool failBeforeDiscard;
        public void DiscardCard(TcgEngine.Card card)
        {
            discardCalls++;
            if (failBeforeDiscard) throw new InvalidOperationException("Simulated host failure");
            GameData.MarkDiscard(card);
        }
    }
}
