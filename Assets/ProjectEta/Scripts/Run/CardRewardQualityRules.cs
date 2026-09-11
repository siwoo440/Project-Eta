namespace ProjectEta.Run
{
    public static class CardRewardQualityRules
    {
        public static CardRewardProfile GetProfile(int stage, CardRewardSource source)
        {
            int safeStage = stage < 1 ? 1 : stage; // 최소 Stage 보정

            if (safeStage <= 3)
            {
                return source == CardRewardSource.RewardNode
                    ? new CardRewardProfile(CardRewardQuality.Improved, source, safeStage, 65, 30, 5) // 초반 Reward 노드 우대
                    : new CardRewardProfile(CardRewardQuality.Basic, source, safeStage, 80, 18, 2); // 초반 전투 보상
            }

            if (safeStage <= 6)
            {
                return source == CardRewardSource.RewardNode
                    ? new CardRewardProfile(CardRewardQuality.Advanced, source, safeStage, 35, 50, 15) // 중반 Reward 노드 우대
                    : new CardRewardProfile(CardRewardQuality.Improved, source, safeStage, 50, 42, 8); // 중반 전투 보상
            }

            return source == CardRewardSource.RewardNode
                ? new CardRewardProfile(CardRewardQuality.Advanced, source, safeStage, 15, 50, 35) // 후반 Reward 노드 우대
                : new CardRewardProfile(CardRewardQuality.Advanced, source, safeStage, 25, 55, 20); // 후반 전투 보상
        }
    }
}
