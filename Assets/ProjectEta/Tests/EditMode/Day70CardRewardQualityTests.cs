using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Pieces; // PieceGrade 사용
using ProjectEta.Run; // Reward 품질 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day70CardRewardQualityTests
    {
        [Test]
        public void EarlyBattleReward_1성가중치가가장높다()
        {
            CardRewardProfile profile = CardRewardQualityRules.GetProfile(2, CardRewardSource.BattleVictory); // 초반 전투 보상 조회

            Assert.AreEqual(CardRewardQuality.Basic, profile.Quality); // 기본 품질 검증
            Assert.Greater(profile.OneStarWeight, profile.TwoStarWeight); // 1성 중심 구성 검증
            Assert.Greater(profile.TwoStarWeight, profile.ThreeStarWeight); // 3성 희소성 검증
        }

        [Test]
        public void LateBattleReward_초반보다3성가중치가높다()
        {
            CardRewardProfile early = CardRewardQualityRules.GetProfile(2, CardRewardSource.BattleVictory); // 초반 프로필 조회
            CardRewardProfile late = CardRewardQualityRules.GetProfile(8, CardRewardSource.BattleVictory); // 후반 프로필 조회

            Assert.Greater(late.ThreeStarWeight, early.ThreeStarWeight); // Stage 진행에 따른 3성 비중 증가 검증
            Assert.Less(late.OneStarWeight, early.OneStarWeight); // Stage 진행에 따른 1성 비중 감소 검증
        }

        [Test]
        public void RewardNode_같은Stage전투보상보다3성가중치가높다()
        {
            CardRewardProfile battle = CardRewardQualityRules.GetProfile(5, CardRewardSource.BattleVictory); // 중반 전투 보상 조회
            CardRewardProfile rewardNode = CardRewardQualityRules.GetProfile(5, CardRewardSource.RewardNode); // 중반 Reward 노드 조회

            Assert.Greater(rewardNode.ThreeStarWeight, battle.ThreeStarWeight); // Reward 노드 품질 우대 검증
            Assert.Less(rewardNode.OneStarWeight, battle.OneStarWeight); // Reward 노드 저등급 비중 감소 검증
        }

        [Test]
        public void GeneralReward_4성5성가중치는0이다()
        {
            CardRewardProfile profile = CardRewardQualityRules.GetProfile(8, CardRewardSource.RewardNode); // 후반 Reward 프로필 조회

            Assert.AreEqual(0, profile.GetGradeWeight(PieceGrade.FourStar)); // 4성 일반 Reward 제외 검증
            Assert.AreEqual(0, profile.GetGradeWeight(PieceGrade.FiveStar)); // 5성 일반 Reward 제외 검증
        }

        [Test]
        public void InvalidStage_Stage1로보정된다()
        {
            CardRewardProfile profile = CardRewardQualityRules.GetProfile(0, CardRewardSource.BattleVictory); // 잘못된 Stage 입력

            Assert.AreEqual(1, profile.Stage); // 최소 Stage 보정 검증
        }
    }
}
