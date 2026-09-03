using System; // Random을 사용해 드로우 덱을 섞기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Cards // 카드 관련 타입을 모아두는 네임스페이스
{
    public class DeckState // 보유 카드 풀·드로우 더미·죽은 카드 더미를 관리하는 클래스
    {
        private readonly List<PieceDefinition> _ownedCardPool = new List<PieceDefinition>(); // 보유 중인 전체 카드 풀
        private readonly List<PieceDefinition> _drawPile = new List<PieceDefinition>(); // 현재 라운드에서 뽑을 카드 더미
        private readonly List<PieceDefinition> _deadCardPile = new List<PieceDefinition>(); // 사망으로 현재 라운드에서 사용할 수 없는 카드 더미

        public IReadOnlyList<PieceDefinition> OwnedCardPool => _ownedCardPool; // 외부에서 읽는 보유 카드 풀
        public IReadOnlyList<PieceDefinition> DrawPile => _drawPile; // 외부에서 읽는 드로우 더미
        public IReadOnlyList<PieceDefinition> DeadCardPile => _deadCardPile; // 외부에서 읽는 죽은 카드 더미

        public void AddToOwnedPool(PieceDefinition card) // 보유 카드 풀에 카드를 추가하는 메서드
        {
            if (card == null) return; // null 카드는 상태에 넣지 않고 종료
            _ownedCardPool.Add(card); // 보유 카드 풀에 카드 추가
        }

        public void AddToDrawPile(PieceDefinition card) // 저장 복원 등에서 드로우 더미를 직접 복원하기 위한 메서드
        {
            if (card == null) return; // null 카드는 상태에 넣지 않고 종료
            _drawPile.Add(card); // 현재 순서를 유지하며 드로우 더미 끝에 카드 추가
        }

        public void MoveToDeadPile(PieceDefinition card) // 죽은 카드 더미에 카드를 추가하는 메서드
        {
            if (card == null) return; // null 카드는 상태에 넣지 않고 종료
            _deadCardPile.Add(card); // 죽은 카드 더미에 카드 추가
        }

        public void RebuildDrawPileFromOwnedPool(Random random = null) // 보유 카드 풀 전체를 복사한 뒤 셔플해 새 드로우 더미를 만드는 메서드
        {
            _drawPile.Clear(); // 이전 라운드의 드로우 순서를 모두 비움
            _drawPile.AddRange(_ownedCardPool); // 현재 보유 카드 전체를 새 드로우 더미에 복사

            var shuffleRandom = random ?? new Random(); // 테스트에서는 고정 시드를, 실제 플레이에서는 새 난수를 사용
            for (int i = _drawPile.Count - 1; i > 0; i--) // Fisher-Yates 방식으로 뒤에서 앞으로 순회하며
            {
                int swapIndex = shuffleRandom.Next(i + 1); // 0부터 현재 인덱스까지 교환할 위치를 하나 선택
                var temporary = _drawPile[i]; // 현재 카드를 임시 보관
                _drawPile[i] = _drawPile[swapIndex]; // 선택된 카드를 현재 위치로 이동
                _drawPile[swapIndex] = temporary; // 임시 보관한 카드를 선택 위치로 이동
            }
        }

        public bool TryDraw(out PieceDefinition card) // 드로우 더미 맨 위 카드 한 장을 꺼내는 메서드
        {
            if (_drawPile.Count == 0) // 드로우 더미가 비어 있으면
            {
                card = null; // 반환 카드가 없음을 명시
                return false; // 드로우 실패 반환
            }

            int topIndex = _drawPile.Count - 1; // 리스트 마지막 요소를 드로우 더미의 맨 위로 사용
            card = _drawPile[topIndex]; // 맨 위 카드 참조를 반환 값에 저장
            _drawPile.RemoveAt(topIndex); // 실제 드로우가 성공했으므로 더미에서 카드 제거
            return true; // 드로우 성공 반환
        }

        public bool TryDrawToHand(HandState hand) // 카드 유실 없이 드로우 더미에서 손패로 한 장 이동하는 메서드
        {
            if (hand == null || hand.IsFull || _drawPile.Count == 0) // 손패가 없거나 가득 찼거나 덱이 비었으면
            {
                return false; // 카드 상태를 바꾸지 않고 실패 반환
            }

            int topIndex = _drawPile.Count - 1; // 실제로 뽑을 맨 위 카드 인덱스 계산
            var card = _drawPile[topIndex]; // 아직 제거하지 않고 카드 참조만 확인
            if (!hand.TryAddCard(card)) // 예상치 못한 이유로 손패 추가에 실패하면
            {
                return false; // 드로우 더미를 건드리지 않아 카드 유실을 방지
            }

            _drawPile.RemoveAt(topIndex); // 손패 추가에 성공한 뒤에만 드로우 더미에서 카드 제거
            return true; // 덱→손패 이동 성공 반환
        }

        public void ReturnDeadPileToOwnedPool() // 죽은 카드 더미를 보유 카드 풀로 되돌리는 기존 메서드
        {
            _ownedCardPool.AddRange(_deadCardPile); // 기존 규칙을 유지해 죽은 카드 더미의 카드를 보유 풀에 합침
            _deadCardPile.Clear(); // 죽은 카드 더미 비우기
        }
    }
}
