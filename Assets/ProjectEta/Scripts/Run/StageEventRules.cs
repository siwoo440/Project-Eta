namespace ProjectEta.Run
{
    public static class StageEventRules
    {
        public const int EventCardChoiceCount = 3; // 이벤트 카드 후보 수 임시값
        public const int TravelingMerchantCardPrice = 35; // 떠돌이 상인 카드 가격 임시값
        public const int GoldCacheCurrency = 40; // 보물 상자 Gold 보상 임시값
        public const int ShrineHealPrice = 25; // 제단 회복 가격 임시값
        public const int HealAmount = 1; // 이벤트 회복량
        public const int RiskRewardCurrency = RunEconomyRules.RiskRewardCurrency; // 위험 계약 Gold 보상 재사용

        public static int GetEventWeight(StageEventDefinition definition, int phase)
        {
            if (definition == null || !definition.IsAvailableInPhase(phase)) return 0; // 등장 불가 이벤트 제외

            switch (definition.EventType)
            {
                case StageEventType.Rest:
                    return phase >= 4 ? definition.BaseWeight - 4 : definition.BaseWeight; // 후반 무료 회복 비중 감소
                case StageEventType.RiskReward:
                    return definition.BaseWeight + (phase - 1) * 2; // 후반 위험 이벤트 비중 증가
                case StageEventType.Training:
                    return definition.BaseWeight + (phase - 3) * 2; // 후반 훈련 이벤트 비중 증가
                default:
                    return definition.BaseWeight; // 기본 가중치 유지
            }
        }

        public static CardRewardProfile GetCardRewardProfile(int phase, int stage)
        {
            int safePhase = NormalizePhase(phase); // Phase 범위 보정
            int safeStage = NormalizeStage(stage); // Stage 범위 보정
            int progress = (safePhase - 1) * RoundState.FinalRound + safeStage; // 전체 런 진행도 계산

            if (progress <= 10)
            {
                return new CardRewardProfile(CardRewardQuality.Basic, CardRewardSource.RewardNode, safeStage, 70, 25, 5); // 초반 이벤트 카드 품질
            }

            if (progress <= 20)
            {
                return new CardRewardProfile(CardRewardQuality.Improved, CardRewardSource.RewardNode, safeStage, 55, 35, 10); // 2페이즈 이벤트 카드 품질
            }

            if (progress <= 30)
            {
                return new CardRewardProfile(CardRewardQuality.Improved, CardRewardSource.RewardNode, safeStage, 40, 45, 15); // 3페이즈 이벤트 카드 품질
            }

            if (progress <= 40)
            {
                return new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.RewardNode, safeStage, 30, 45, 25); // 4페이즈 이벤트 카드 품질
            }

            return new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.RewardNode, safeStage, 20, 45, 35); // 5페이즈 이벤트 카드 품질
        }

        private static int NormalizePhase(int phase)
        {
            if (phase < RunPhaseProgressService.FirstPhase) return RunPhaseProgressService.FirstPhase; // 최소 Phase 보정
            if (phase > RunPhaseProgressService.TotalPhases) return RunPhaseProgressService.TotalPhases; // 최대 Phase 보정
            return phase; // 정상 Phase 반환
        }

        private static int NormalizeStage(int stage)
        {
            if (stage < RoundState.FirstRound) return RoundState.FirstRound; // 최소 Stage 보정
            if (stage > RoundState.FinalRound) return RoundState.FinalRound; // 최대 Stage 보정
            return stage; // 정상 Stage 반환
        }
    }
}
