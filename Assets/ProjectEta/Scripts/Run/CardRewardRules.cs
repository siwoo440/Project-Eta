using System.Collections.Generic; // IReadOnlyList<T> 사용
using ProjectEta.Cards; // DeckState 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run // 카드 보상 규칙 네임스페이스
{
    public static class CardRewardRules // 카드 보상 획득 가능 여부와 중복 보유 상한 규칙
    {
        public const int PrototypeOwnedCopyLimit = 3; // 동일 PieceId 보상 획득 임시 상한

        public static bool CanOffer(PieceDefinition definition, IReadOnlyList<PieceDefinition> ownedCards) // 일반 보상 후보 포함 가능 여부 판정
        {
            if (!RunContentPoolRules.CanUseAsReward(definition)) return false; // 공통 Reward Pool 정책 위반 카드 제외
            return CountOwnedCopies(ownedCards, definition.PieceId) < PrototypeOwnedCopyLimit; // 동일 카드 보유 상한 미만만 허용
        }

        public static bool TryAddOwnedCard(DeckState deckState, PieceDefinition definition) // 선택한 보상 카드를 DeckState 공개 API를 통해 OwnedCardPool에 추가
        {
            if (deckState == null || definition == null || string.IsNullOrWhiteSpace(definition.PieceId)) return false; // 잘못된 입력 차단
            if (CountOwnedCopies(deckState.OwnedCardPool, definition.PieceId) >= PrototypeOwnedCopyLimit) return false; // 동일 카드 보유 상한 초과 차단
            deckState.AddToOwnedPool(definition); // 읽기 전용 노출을 우회하지 않고 DeckState가 보유 풀을 직접 변경
            return true; // 획득 성공 반환
        }

        public static int CountOwnedCopies(IReadOnlyList<PieceDefinition> ownedCards, string pieceId) // 동일 PieceId 현재 보유 장수 계산
        {
            if (ownedCards == null || string.IsNullOrWhiteSpace(pieceId)) return 0; // 빈 입력 기본값 반환
            int count = 0; // 동일 카드 장수 초기화

            for (int i = 0; i < ownedCards.Count; i++)
            {
                PieceDefinition definition = ownedCards[i]; // 현재 보유 카드 조회
                if (definition != null && definition.PieceId == pieceId) count++; // 동일 ID 장수 증가
            }

            return count; // 최종 보유 장수 반환
        }
    }
}
