using System; // Random 사용
using System.Collections.Generic; // List<T> 사용
using ProjectEta.Pieces; // PieceDefinition 사용

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

        public event Action<PieceDefinition> CardAcquired;

        public void AddToOwnedPool(PieceDefinition card)
        {
            if (!CanEnterCardPools(card)) return;
            _ownedCardPool.Add(card);
        }

        public void AddAcquiredCard(PieceDefinition card)
        {
            if (!CanEnterCardPools(card)) return;
            _ownedCardPool.Add(card);
            CardAcquired?.Invoke(card);
        }

        public bool RemoveFromOwnedPool(PieceDefinition card)
        {
            if (card == null) return false;
            return _ownedCardPool.Remove(card);
        }

        public void AddToDrawPile(PieceDefinition card)
        {
            if (!CanEnterCardPools(card)) return;
            _drawPile.Add(card);
        }

        public void AddDrawCardToBottom(PieceDefinition card)
        {
            if (!CanEnterCardPools(card)) return;
            _drawPile.Insert(0, card);
        }

        public IReadOnlyList<PieceDefinition> PeekTopCards(int count)
        {
            var result = new List<PieceDefinition>();
            if (count <= 0) return result;

            int safeCount = Math.Min(count, _drawPile.Count);

            for (int i = 0; i < safeCount; i++)
            {
                int index = _drawPile.Count - 1 - i;
                result.Add(_drawPile[index]);
            }

            return result;
        }

        public void MoveToDeadPile(PieceDefinition card)
        {
            if (!CanEnterCardPools(card)) return; // 91일차: 임시 소환물 사망은 DeadCardPile에 들어가지 않음
            _ownedCardPool.Remove(card);
            _deadCardPile.Add(card);
        }

        public void RebuildDrawPileFromOwnedPool(Random random = null)
        {
            _drawPile.Clear();
            _drawPile.AddRange(_ownedCardPool);

            var shuffleRandom = random ?? new Random();

            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int swapIndex = shuffleRandom.Next(i + 1);
                PieceDefinition temporary = _drawPile[i];
                _drawPile[i] = _drawPile[swapIndex];
                _drawPile[swapIndex] = temporary;
            }
        }

        public bool TryDraw(out PieceDefinition card)
        {
            if (_drawPile.Count == 0)
            {
                card = null;
                return false;
            }

            int topIndex = _drawPile.Count - 1;
            card = _drawPile[topIndex];
            _drawPile.RemoveAt(topIndex);
            return true;
        }

        public bool TryDrawToHand(HandState hand)
        {
            if (hand == null || hand.IsFull || _drawPile.Count == 0) return false;

            int topIndex = _drawPile.Count - 1;
            PieceDefinition card = _drawPile[topIndex];

            if (!hand.TryAddCard(card)) return false;

            _drawPile.RemoveAt(topIndex);
            return true;
        }

        public bool TryMoveSpecificToHand(PieceDefinition card, HandState hand)
        {
            if (card == null || hand == null || hand.IsFull) return false;

            int cardIndex = _drawPile.LastIndexOf(card);
            if (cardIndex < 0) return false;
            if (!hand.TryAddCard(card)) return false;

            _drawPile.RemoveAt(cardIndex);
            return true;
        }

        public void ReturnDeadPileToOwnedPool()
        {
            _ownedCardPool.AddRange(_deadCardPile);
            _deadCardPile.Clear();
        }

        public bool DiscardToBottom(PieceDefinition card, HandState hand)
        {
            if (card == null || hand == null) return false;
            if (!hand.RemoveCard(card)) return false;

            AddDrawCardToBottom(card);
            return true;
        }

        private static bool CanEnterCardPools(PieceDefinition card)
        {
            return card != null && !card.IsRuntimeTemporarySummonDefinition;
        }
    }
}
