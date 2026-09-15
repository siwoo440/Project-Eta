using System.Collections.Generic; // IReadOnlyList<T> 사용
using ProjectEta.Cards; // DeckState 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run // 카드 보상 규칙 네임스페이스
{
    public static class CardRewardRules // 카드 보상 획득 가능 여부와 중복 보유 상한 규칙
    {
        public const int PrototypeOwnedCopyLimit = CardOwnershipRules.DefaultOwnedLimit; // 기존 호출 호환용 기본 보유 상한

        public static bool CanOffer(PieceDefinition definition, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards = null) // 일반 보상 후보 포함 가능 여부 판정
        {
            if (!RunContentPoolRules.CanUseAsReward(definition)) return false; // 공통 Reward Pool 정책 위반 카드 제외
            int ownedLimit = CardOwnershipRules.GetOwnedLimit(definition.Grade); // 카드 등급별 보유 상한 조회
            int ownedCopies = CountOwnedCopies(ownedCards, definition.PieceId) + CountOwnedCopies(deadCards, definition.PieceId); // 정상·사망 동일 카드 보유 수 계산
            return ownedCopies < ownedLimit; // 동일 카드 보유 상한 미만만 허용
        }

        public static bool TryAddOwnedCard(DeckState deckState, PieceDefinition definition) // 선택한 보상 카드를 DeckState 공개 API를 통해 OwnedCardPool에 추가
        {
            if (deckState == null || definition == null || string.IsNullOrWhiteSpace(definition.PieceId)) return false; // 잘못된 입력 차단
            int ownedLimit = CardOwnershipRules.GetOwnedLimit(definition.Grade); // 카드 등급별 보유 상한 조회
            int ownedCopies = CountOwnedCopies(deckState.OwnedCardPool, definition.PieceId) + CountOwnedCopies(deckState.DeadCardPile, definition.PieceId); // 정상·사망 동일 카드 보유 수 계산
            if (ownedCopies >= ownedLimit) return false; // 동일 카드 보유 상한 초과 차단
            deckState.AddAcquiredCard(definition); // 외부 카드 획득과 완료 이벤트를 함께 등록
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
