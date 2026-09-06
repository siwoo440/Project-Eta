using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용
using ProjectEta.Cards; // DeckState·HandState 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.King
{
    public static class StrategyKingAbility
    {
        public const int CandidateCount = 3; // 전술적 준비 확인 카드 수

        public static IReadOnlyList<PieceDefinition> BuildCandidates(KingRunState kingState, DeckState deck, HandState hand)
        {
            if (kingState == null || kingState.Archetype != KingArchetype.Strategy) return new List<PieceDefinition>(); // 전략형 킹 외 후보 없음
            if (deck == null || hand == null || hand.IsFull) return new List<PieceDefinition>(); // 덱·손패 누락 또는 손패 Full 시 발동 차단
            return deck.PeekTopCards(CandidateCount); // 덱 위 최대 3장을 드로우 순서대로 확인
        }

        public static bool TryChoose(
            KingRunState kingState,
            DeckState deck,
            HandState hand,
            IReadOnlyList<PieceDefinition> candidates,
            int selectedIndex,
            out PieceDefinition selected)
        {
            selected = null; // 기본 선택 결과 초기화
            if (kingState == null || kingState.Archetype != KingArchetype.Strategy) return false; // 전략형 킹 외 선택 차단
            if (!kingState.StrategyPreparationPending) return false; // 실제 전술적 준비 진행 중이 아니면 차단
            if (deck == null || hand == null || hand.IsFull) return false; // 덱·손패 누락 또는 손패 Full 차단
            if (candidates == null || candidates.Count == 0) return false; // 후보 없음 차단
            if (selectedIndex < 0 || selectedIndex >= candidates.Count) return false; // 후보 인덱스 범위 검증
            if (deck.DrawPile.Count < candidates.Count) return false; // 후보 생성 뒤 덱 장수 변경 감지

            for (int i = 0; i < candidates.Count; i++)
            {
                int deckIndex = deck.DrawPile.Count - 1 - i; // 현재 덱의 후보 위치 계산
                if (!ReferenceEquals(deck.DrawPile[deckIndex], candidates[i])) return false; // 후보 표시 뒤 덱 순서 변경 시 선택 거부
            }

            var drawn = new List<PieceDefinition>(candidates.Count); // 상단 후보를 실제 덱에서 임시 분리

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!deck.TryDraw(out PieceDefinition card))
                {
                    RestoreDrawnCards(deck, drawn); // 예상치 못한 드로우 실패 시 원래 순서 복구
                    return false; // 선택 실패 반환
                }

                drawn.Add(card); // 실제 위에서부터 뽑힌 후보 저장
            }

            selected = drawn[selectedIndex]; // 플레이어가 선택한 후보 확정

            if (!hand.TryAddCard(selected))
            {
                RestoreDrawnCards(deck, drawn); // 손패 추가 실패 시 후보 전체 원래 순서 복구
                selected = null; // 선택 결과 제거
                return false; // 선택 실패 반환
            }

            for (int i = drawn.Count - 1; i >= 0; i--)
            {
                if (i == selectedIndex) continue; // 선택 카드는 손패에 남김
                deck.AddDrawCardToBottom(drawn[i]); // 선택하지 않은 후보를 덱 맨 아래로 이동
            }

            kingState.CompleteStrategyPreparation(); // 이번 배치 턴 전술적 준비 완료
            return true; // 카드 선택 성공 반환
        }

        private static void RestoreDrawnCards(DeckState deck, List<PieceDefinition> drawn)
        {
            for (int i = drawn.Count - 1; i >= 0; i--)
            {
                deck.AddToDrawPile(drawn[i]); // 마지막에 뽑은 카드부터 되돌려 원래 맨 위 순서 복구
            }
        }
    }
}
