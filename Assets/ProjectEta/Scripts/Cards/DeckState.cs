using System.Collections.Generic;
using ProjectEta.Pieces;

namespace ProjectEta.Cards
{
    public class DeckState
    {
        private readonly List<PieceDefinition> _ownedCardPool = new List<PieceDefinition>();
        private readonly List<PieceDefinition> _drawPile = new List<PieceDefinition>();
        private readonly List<PieceDefinition> _deadCardPile = new List<PieceDefinition>();

        public IReadOnlyList<PieceDefinition> OwnedCardPool => _ownedCardPool;
        public IReadOnlyList<PieceDefinition> DrawPile => _drawPile;
        public IReadOnlyList<PieceDefinition> DeadCardPile => _deadCardPile;

        public void AddToOwnedPool(PieceDefinition card) => _ownedCardPool.Add(card);
        public void MoveToDeadPile(PieceDefinition card) => _deadCardPile.Add(card);

        public void ReturnDeadPileToOwnedPool()
        {
            _ownedCardPool.AddRange(_deadCardPile);
            _deadCardPile.Clear();
        }
    }
}
