using System; // Random을 사용해 드로우 덱을 섞기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Cards
{
    public class DeckState
    {
        private readonly List<PieceDefinition> _ownedCardPool = new List<PieceDefinition>(); // 보유 중인 전체 카드 풀
        private readonly List<PieceDefinition> _drawPile = new List<PieceDefinition>(); // 현재 라운드에서 뽑을 카드 더미
        private readonly List<PieceDefinition> _deadCardPile = new List<PieceDefinition>(); // 사망으로 현재 라운드에서 사용할 수 없는 카드 더미

        public IReadOnlyList<PieceDefinition> OwnedCardPool => _ownedCardPool; // 외부에서 읽는 보유 카드 풀
        public IReadOnlyList<PieceDefinition> DrawPile => _drawPile; // 외부에서 읽는 드로우 더미
        public IReadOnlyList<PieceDefinition> DeadCardPile => _deadCardPile; // 외부에서 읽는 죽은 카드 더미

        public void AddToOwnedPool(PieceDefinition card)
        {
            if (card == null) return; // null 카드는 상태에 넣지 않고 종료
            _ownedCardPool.Add(card); // 보유 카드 풀에 카드 추가
        }

        public bool RemoveFromOwnedPool(PieceDefinition card)
        {
            if (card == null) return false; // null 카드는 처리하지 않음
            return _ownedCardPool.Remove(card); // 동일 카드가 여러 장이면 1장만 제거하고 성공 여부 반환
        }

        public void AddToDrawPile(PieceDefinition card)
        {
            if (card == null) return; // null 카드는 상태에 넣지 않고 종료
            _drawPile.Add(card); // 현재 순서를 유지하며 드로우 더미 끝에 카드 추가
        }

        public void AddDrawCardToBottom(PieceDefinition card)
        {
            if (card == null) return; // null 카드는 드로우 더미에 넣지 않음
            _drawPile.Insert(0, card); // TryDraw가 사용하는 마지막 인덱스 반대편을 덱 맨 아래로 사용
        }

        public IReadOnlyList<PieceDefinition> PeekTopCards(int count)
        {
            var result = new List<PieceDefinition>(); // 덱을 변경하지 않는 후보 목록 생성
            if (count <= 0) return result; // 잘못된 요청 수량은 빈 목록 반환

            int safeCount = Math.Min(count, _drawPile.Count); // 현재 덱 장수 안에서 확인 수량 제한

            for (int i = 0; i < safeCount; i++)
            {
                int index = _drawPile.Count - 1 - i; // 마지막 인덱스부터 실제 드로우 순서 계산
                result.Add(_drawPile[index]); // 맨 위부터 아래 방향으로 후보 추가
            }

            return result; // 덱 상태를 바꾸지 않은 후보 목록 반환
        }

        public void MoveToDeadPile(PieceDefinition card)
        {
            if (card == null) return; // null 카드는 상태에 넣지 않고 종료
            _ownedCardPool.Remove(card); // 보유 풀에 같은 카드가 있으면 정확히 1장 제거
            _deadCardPile.Add(card); // 죽은 카드 더미에 실제 한 장 추가
        }

        public void RebuildDrawPileFromOwnedPool(Random random = null)
        {
            _drawPile.Clear(); // 이전 라운드 드로우 순서 제거
            _drawPile.AddRange(_ownedCardPool); // 현재 보유 카드 전체 복사

            var shuffleRandom = random ?? new Random(); // 고정 시드 또는 새 난수 사용

            for (int i = _drawPile.Count - 1; i > 0; i--)
            {
                int swapIndex = shuffleRandom.Next(i + 1); // 현재 인덱스 범위에서 교환 위치 선택
                var temporary = _drawPile[i]; // 현재 카드 임시 저장
                _drawPile[i] = _drawPile[swapIndex]; // 선택 카드 현재 위치 이동
                _drawPile[swapIndex] = temporary; // 임시 카드 교환 위치 이동
            }
        }

        public bool TryDraw(out PieceDefinition card)
        {
            if (_drawPile.Count == 0)
            {
                card = null; // 반환 카드 없음 처리
                return false; // 드로우 실패 반환
            }

            int topIndex = _drawPile.Count - 1; // 리스트 마지막을 덱 맨 위로 사용
            card = _drawPile[topIndex]; // 맨 위 카드 반환
            _drawPile.RemoveAt(topIndex); // 실제 드로우 카드 제거
            return true; // 드로우 성공 반환
        }

        public bool TryDrawToHand(HandState hand)
        {
            if (hand == null || hand.IsFull || _drawPile.Count == 0) return false; // 손패·덱 상태 검증

            int topIndex = _drawPile.Count - 1; // 맨 위 카드 인덱스 계산
            var card = _drawPile[topIndex]; // 제거 전 카드 참조 확인

            if (!hand.TryAddCard(card)) return false; // 손패 추가 실패 시 덱 유지

            _drawPile.RemoveAt(topIndex); // 손패 추가 성공 뒤 덱에서 제거
            return true; // 덱→손패 이동 성공 반환
        }

        public bool TryMoveSpecificToHand(PieceDefinition card, HandState hand)
        {
            if (card == null || hand == null || hand.IsFull) return false; // 필수 정보·손패 상한 검증

            int cardIndex = _drawPile.LastIndexOf(card); // 드로우 더미의 실제 카드 위치 탐색
            if (cardIndex < 0) return false; // 해당 카드가 없으면 실패
            if (!hand.TryAddCard(card)) return false; // 손패 추가 실패 시 덱 유지

            _drawPile.RemoveAt(cardIndex); // 손패 추가 성공 뒤 해당 카드 제거
            return true; // 특정 카드 이동 성공 반환
        }

        public void ReturnDeadPileToOwnedPool()
        {
            _ownedCardPool.AddRange(_deadCardPile); // 죽은 카드 소유 풀 복귀
            _deadCardPile.Clear(); // 죽은 카드 더미 비움
        }

        public bool DiscardToBottom(PieceDefinition card, HandState hand)
        {
            if (card == null || hand == null) return false; // 필수 정보 누락 실패
            if (!hand.RemoveCard(card)) return false; // 실제 손패 카드 제거 실패 처리

            AddDrawCardToBottom(card); // 손패 카드를 드로우 덱 맨 아래로 이동
            return true; // 정리 성공 반환
        }
    }
}
