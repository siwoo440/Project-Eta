using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Cards // 카드 관련 타입을 모아두는 네임스페이스
{
    public class HandState // 손패에 든 카드를 관리하는 클래스
    {
        public const int MaxHandSize = 10; // 손패 최대 장수

        private readonly List<PieceDefinition> _hand = new List<PieceDefinition>(); // 현재 손패에 든 카드 목록

        public IReadOnlyList<PieceDefinition> Hand => _hand; // 외부에서 읽는 손패 목록
        public bool IsFull => _hand.Count >= MaxHandSize; // 손패가 가득 찼는지 여부

        public bool TryAddCard(PieceDefinition card) // 손패에 카드를 추가 시도하는 메서드
        {
            if (IsFull) return false; // 손패가 가득 찼으면 추가 실패
            _hand.Add(card); // 손패에 카드 추가
            return true; // 추가 성공
        }

        public bool RemoveCard(PieceDefinition card) => _hand.Remove(card); // 손패에서 카드 제거(성공 여부 반환)
    }
}
