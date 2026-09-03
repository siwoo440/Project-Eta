using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Cards // 카드 관련 타입을 모아두는 네임스페이스
{
    public class DeckState // 보유 카드 풀·드로우 더미·죽은 카드 더미를 관리하는 클래스
    {
        private readonly List<PieceDefinition> _ownedCardPool = new List<PieceDefinition>(); // 보유 중인 전체 카드 풀
        private readonly List<PieceDefinition> _drawPile = new List<PieceDefinition>(); // 드로우 대상이 되는 더미
        private readonly List<PieceDefinition> _deadCardPile = new List<PieceDefinition>(); // 사망으로 봉인된 카드 더미

        public IReadOnlyList<PieceDefinition> OwnedCardPool => _ownedCardPool; // 외부에서 읽는 보유 카드 풀
        public IReadOnlyList<PieceDefinition> DrawPile => _drawPile; // 외부에서 읽는 드로우 더미
        public IReadOnlyList<PieceDefinition> DeadCardPile => _deadCardPile; // 외부에서 읽는 죽은 카드 더미

        public void AddToOwnedPool(PieceDefinition card) => _ownedCardPool.Add(card); // 보유 카드 풀에 카드 추가
        public void MoveToDeadPile(PieceDefinition card) => _deadCardPile.Add(card); // 죽은 카드 더미로 카드 이동

        public void ReturnDeadPileToOwnedPool() // 죽은 카드 더미를 보유 카드 풀로 되돌리는 메서드
        {
            _ownedCardPool.AddRange(_deadCardPile); // 죽은 카드 더미의 카드를 보유 풀에 합침
            _deadCardPile.Clear(); // 죽은 카드 더미 비우기
        }
    }
}
