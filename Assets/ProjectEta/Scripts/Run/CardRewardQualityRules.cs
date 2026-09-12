using System; // StringComparison 사용

namespace ProjectEta.Run
{
    public static class CardRewardQualityRules
    {
        private const string EliteRewardProfileId = "PrototypeEliteReward"; // Elite 전용 StageDefinition 보상 ID
        private const string MidBossRewardProfileId = "MidBossReward74"; // 74일차 Mid Boss 보상 ID

        public static CardRewardProfile GetProfile(int stage, CardRewardSource source)
        {
            return GetProfile(stage, source, string.Empty); // 기존 호출을 프로필 ID 없는 규칙으로 위임
        }

        public static CardRewardProfile GetProfile(int stage, CardRewardSource source, string rewardProfileId)
        {
            int safeStage = stage < 1 ? 1 : stage; // 최소 Stage 보정

            if (source == CardRewardSource.MidBossVictory || string.Equals(rewardProfileId, MidBossRewardProfileId, StringComparison.Ordinal))
            {
                return safeStage <= 6
                    ? new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.MidBossVictory, safeStage, 20, 50, 30) // 전반 Mid Boss 고급 보상
                    : new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.MidBossVictory, safeStage, 10, 45, 45); // 후반 Mid Boss 3성 강화
            }

            if (source == CardRewardSource.EliteVictory || string.Equals(rewardProfileId, EliteRewardProfileId, StringComparison.Ordinal))
            {
                if (safeStage <= 3)
                {
                    return new CardRewardProfile(CardRewardQuality.Improved, CardRewardSource.EliteVictory, safeStage, 55, 38, 7); // 초반 Elite 우대 보상
                }

                if (safeStage <= 6)
                {
                    return new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.EliteVictory, safeStage, 30, 50, 20); // 중반 Elite 고급 보상
                }

                return new CardRewardProfile(CardRewardQuality.Advanced, CardRewardSource.EliteVictory, safeStage, 15, 50, 35); // 후반 Elite 3성 비중 강화
            }

            if (safeStage <= 3)
            {
                return source == CardRewardSource.RewardNode
                    ? new CardRewardProfile(CardRewardQuality.Improved, source, safeStage, 65, 30, 5) // 초반 Reward 노드 우대
                    : new CardRewardProfile(CardRewardQuality.Basic, source, safeStage, 80, 18, 2); // 초반 일반 전투 보상
            }

            if (safeStage <= 6)
            {
                return source == CardRewardSource.RewardNode
                    ? new CardRewardProfile(CardRewardQuality.Advanced, source, safeStage, 35, 50, 15) // 중반 Reward 노드 우대
                    : new CardRewardProfile(CardRewardQuality.Improved, source, safeStage, 50, 42, 8); // 중반 일반 전투 보상
            }

            return source == CardRewardSource.RewardNode
                ? new CardRewardProfile(CardRewardQuality.Advanced, source, safeStage, 15, 50, 35) // 후반 Reward 노드 우대
                : new CardRewardProfile(CardRewardQuality.Advanced, source, safeStage, 25, 55, 20); // 후반 일반 전투 보상
        }
    }
}
