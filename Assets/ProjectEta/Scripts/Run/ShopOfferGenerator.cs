using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public static class ShopOfferGenerator
    {
        public static IReadOnlyList<ShopOffer> Generate(
            IReadOnlyList<PieceDefinition> sourcePool,
            IReadOnlyList<PieceDefinition> ownedCards,
            int candidateCount,
            int mapSeed,
            int phase,
            int stage,
            string nodeId)
        {
            int safeCount = candidateCount < 0 ? 0 : candidateCount; // 요청 상품 수 보정
            int safePhase = NormalizePhase(phase); // 페이즈 범위 보정
            int safeStage = NormalizeStage(stage); // 스테이지 범위 보정
            int ownedCount = ownedCards != null ? ownedCards.Count : 0; // 보유 카드 수 계산
            int seed = CreateSeed(mapSeed, safePhase, safeStage, nodeId, ownedCount); // 상점 고유 시드 생성
            CardRewardProfile profile = CreateShopProfile(safePhase, safeStage); // 상점 전용 등급 프로필 생성
            IReadOnlyList<PieceDefinition> cards = CardRewardGenerator.Generate(sourcePool, ownedCards, safeCount, seed, profile); // 기존 해금·중복 규칙 기반 상품 카드 생성
            var offers = new List<ShopOffer>(cards.Count); // 최종 상품 목록 생성

            for (int i = 0; i < cards.Count; i++)
            {
                PieceDefinition card = cards[i]; // 현재 상품 카드 조회
                if (card == null) continue; // 빈 상품 제외
                int price = ShopPriceRules.GetCardPurchasePrice(card, safePhase, safeStage); // 카드별 가격 계산
                offers.Add(new ShopOffer(card, price)); // 가격 포함 상품 등록
            }

            return offers; // 최종 상품 목록 반환
        }

        public static int CreateSeed(int mapSeed, int phase, int stage, string nodeId, int ownedCount)
        {
            unchecked
            {
                int seed = 17; // 안정 해시 시작값
                seed = seed * 31 + mapSeed; // 지도 시드 반영
                seed = seed * 31 + phase; // 현재 페이즈 반영
                seed = seed * 31 + stage; // 현재 스테이지 반영
                seed = seed * 31 + ownedCount; // 보유 카드 수 반영

                if (!string.IsNullOrEmpty(nodeId))
                {
                    for (int i = 0; i < nodeId.Length; i++)
                    {
                        seed = seed * 31 + nodeId[i]; // 노드 ID 안정 해시 반영
                    }
                }

                return seed; // 최종 재현 가능 시드 반환
            }
        }

        private static CardRewardProfile CreateShopProfile(int phase, int stage)
        {
            int progress = (phase - 1) * RoundState.FinalRound + stage; // 전체 런 진행도 계산

            if (progress <= 10)
            {
                return new CardRewardProfile(CardRewardQuality.Basic, CardRewardSource.RewardNode, stage, 70, 25, 5); // 1페이즈 저등급 중심 상품
            }

            if (progress <= 20)
            {
                return new CardRewardProfile(CardRewardQuality.Improved, CardRewardSource.RewardNode, stage, 55, 35, 10); // 2페이즈 2성 비중 증가
            }

            if (progress <= 30)
            {
                return new CardRewardProfile(CardRewardQuality.Improved, CardRewardSource.RewardNode, stage, 40, 45, 15); // 3페이즈 균형 상품
            }

            if (progress <= 40)
            {
                return new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.RewardNode, stage, 30, 45, 25); // 4페이즈 고등급 비중 증가
            }

            return new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.RewardNode, stage, 20, 45, 35); // 5페이즈 3성 비중 확대
        }

        private static int NormalizePhase(int phase)
        {
            if (phase < RunPhaseProgressService.FirstPhase) return RunPhaseProgressService.FirstPhase; // 최소 페이즈 보정
            if (phase > RunPhaseProgressService.TotalPhases) return RunPhaseProgressService.TotalPhases; // 최대 페이즈 보정
            return phase; // 정상 페이즈 반환
        }

        private static int NormalizeStage(int stage)
        {
            if (stage < RoundState.FirstRound) return RoundState.FirstRound; // 최소 스테이지 보정
            if (stage > RoundState.FinalRound) return RoundState.FinalRound; // 최대 스테이지 보정
            return stage; // 정상 스테이지 반환
        }
    }
}
