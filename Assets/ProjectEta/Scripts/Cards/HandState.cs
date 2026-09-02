using System.Collections.Generic;
using ProjectEta.Pieces;

namespace ProjectEta.Cards
{
    public class HandState
    {
        public const int MaxHandSize = 10;

        private readonly List<PieceDefinition> _hand = new List<PieceDefinition>();

        public IReadOnlyList<PieceDefinition> Hand => _hand;
        public bool IsFull => _hand.Count >= MaxHandSize;

        public bool TryAddCard(PieceDefinition card)
        {
            if (IsFull) return false;
            _hand.Add(card);
            return true;
        }

        public bool RemoveCard(PieceDefinition card) => _hand.Remove(card);
    }
}
