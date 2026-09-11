using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용
using UnityEngine; // Mathf 사용
using ProjectEta.Pieces; // PieceDefinition·PieceCategory·PieceMovementType 사용

namespace ProjectEta.Run
{
    public static class ShopService
    {
        public static IReadOnlyList<PieceDefinition> GetManageableCards(RunState runState)
        {
            var result = new List<PieceDefinition>(); // 제거·강화 가능 카드 목록 생성
            if (runState == null || runState.Deck == null) return result; // 런·덱 누락 방어

            for (int i = 0; i < runState.Deck.OwnedCardPool.Count; i++)
            {
                PieceDefinition card = runState.Deck.OwnedCardPool[i]; // 현재 보유 카드 조회
                if (!IsManageableCard(card)) continue; // 조작 불가 카드 제외
                result.Add(card); // 조작 가능 카드 등록
            }

            return result; // 전체 조작 가능 카드 반환
        }

        public static bool TryPurchaseCard(RunState runState, RunEconomyState economy, ShopOffer offer, out StageChoiceResult result)
        {
            result = null; // 기본 실패 결과 초기화
            if (runState == null || economy == null || offer == null || offer.Card == null) return false; // 필수 상태 누락 차단
            if (offer.IsPurchased) return false; // 구매 완료 상품 재구매 차단
            if (!CardRewardRules.CanOffer(offer.Card, runState.Deck.OwnedCardPool)) return false; // 획득 규칙 위반 상품 차단
            if (!economy.TrySpend(offer.Price)) return false; // Gold 부족 구매 차단

            if (!CardRewardRules.TryAddOwnedCard(runState.Deck, offer.Card))
            {
                economy.Add(offer.Price); // 카드 추가 실패 Gold 환불
                return false; // 구매 실패 반환
            }

            offer.MarkPurchased(); // 상품 구매 완료 처리
            result = new StageChoiceResult(StageChoiceEffectType.CardAdded, $"{offer.Card.DisplayName} 구매", -offer.Price, 0, offer.Card); // 구매 결과 생성
            return true; // 구매 성공 반환
        }

        public static bool TryRemoveCard(RunState runState, RunEconomyState economy, PieceDefinition card, int phase, out StageChoiceResult result)
        {
            result = null; // 기본 실패 결과 초기화
            if (runState == null || economy == null || !IsManageableCard(card)) return false; // 필수 상태·카드 검증
            int price = ShopPriceRules.GetCardRemovePrice(phase); // 현재 제거 비용 계산
            if (!economy.TrySpend(price)) return false; // Gold 부족 제거 차단

            if (!runState.Deck.RemoveFromOwnedPool(card))
            {
                economy.Add(price); // 제거 실패 Gold 환불
                return false; // 제거 실패 반환
            }

            result = new StageChoiceResult(StageChoiceEffectType.CardRemoved, $"{card.DisplayName} 제거", -price, 0, card); // 제거 결과 생성
            return true; // 제거 성공 반환
        }

        public static bool TryHealKing(RunState runState, RunEconomyState economy, int phase, int maxHp, out StageChoiceResult result)
        {
            result = null; // 기본 실패 결과 초기화
            if (runState == null || economy == null || maxHp <= 0) return false; // 필수 상태·최대 HP 검증
            if (runState.KingHp >= maxHp) return false; // 최대 HP 회복 차단
            int price = ShopPriceRules.GetHealPrice(phase); // 현재 회복 비용 계산
            if (!economy.TrySpend(price)) return false; // Gold 부족 회복 차단

            int before = runState.KingHp; // 회복 전 HP 저장
            runState.KingHp = Mathf.Min(maxHp, runState.KingHp + ShopPriceRules.HealAmount); // 킹 HP 회복 적용
            result = new StageChoiceResult(StageChoiceEffectType.KingHpChanged, "킹 HP 회복", -price, runState.KingHp - before, null); // 회복 결과 생성
            return true; // 회복 성공 반환
        }

        public static bool TryUpgradeCard(RunState runState, RunEconomyState economy, PieceDefinition card, int phase, int stage, out StageChoiceResult result)
        {
            result = null; // 기본 실패 결과 초기화
            if (runState == null || economy == null || !IsManageableCard(card)) return false; // 필수 상태·카드 검증
            int price = ShopPriceRules.GetUpgradePrice(phase, stage); // 현재 강화 비용 계산
            if (!economy.TrySpend(price)) return false; // Gold 부족 강화 차단

            if (!RuntimeCardUpgradeService.TryUpgradeOwnedCard(runState.Deck, card, out PieceDefinition upgraded))
            {
                economy.Add(price); // 강화 실패 Gold 환불
                return false; // 강화 실패 반환
            }

            result = new StageChoiceResult(StageChoiceEffectType.CardUpgraded, $"{card.DisplayName} → {upgraded.DisplayName}", -price, 0, upgraded); // 강화 결과 생성
            return true; // 강화 성공 반환
        }

        public static bool IsManageableCard(PieceDefinition card)
        {
            if (card == null) return false; // 빈 카드 차단
            if (card.MovementType == PieceMovementType.King) return false; // King 조작 차단
            if (card.Category == PieceCategory.Monster || card.Category == PieceCategory.Boss) return false; // 적·보스 조작 차단
            return true; // 일반 플레이어 카드 허용
        }
    }
}
