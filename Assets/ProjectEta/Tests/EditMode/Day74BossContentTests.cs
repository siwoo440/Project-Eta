using NUnit.Framework; // NUnit 테스트 사용
using UnityEngine; // Resources 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // StageDefinition·StageType 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day74BossContentTests
    {
        [Test]
        public void MidBossAndFinalBoss_UseDifferentRoundDefinitions()
        {
            StageDefinition midBoss = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(10, StageType.MidBoss),
                10); // 중간 보스 스테이지 생성

            StageDefinition finalBoss = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(10, StageType.FinalBoss),
                10); // 최종 보스 스테이지 생성

            Assert.NotNull(midBoss); // 중간 보스 정의 존재 검증
            Assert.NotNull(finalBoss); // 최종 보스 정의 존재 검증
            Assert.NotNull(midBoss.RoundDefinition); // 중간 보스 Round 존재 검증
            Assert.NotNull(finalBoss.RoundDefinition); // 최종 보스 Round 존재 검증
            Assert.AreNotSame(midBoss.RoundDefinition, finalBoss.RoundDefinition); // 서로 다른 Round 에셋 사용 검증
            Assert.AreEqual("MidBossRound74", midBoss.RoundDefinition.name); // 중간 보스 Round 이름 검증
            Assert.AreEqual("FinalBossRound74", finalBoss.RoundDefinition.name); // 최종 보스 Round 이름 검증
        }

        [Test]
        public void MidBossAndFinalBoss_UseDifferentRewardProfiles()
        {
            StageDefinition midBoss = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(9, StageType.MidBoss),
                9); // 중간 보스 스테이지 생성

            StageDefinition finalBoss = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(10, StageType.FinalBoss),
                10); // 최종 보스 스테이지 생성

            Assert.AreEqual("MidBossReward74", midBoss.RewardProfileId); // 중간 보스 보상 프로필 검증
            Assert.AreEqual("FinalBossReward74", finalBoss.RewardProfileId); // 최종 보스 보상 프로필 검증
            Assert.AreNotEqual(midBoss.RewardProfileId, finalBoss.RewardProfileId); // 보상 프로필 분리 검증
        }

        [Test]
        public void BossRounds_HaveDistinctBossResources()
        {
            StageDefinition midStage = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(8, StageType.MidBoss),
                8); // 중간 보스 스테이지 생성

            StageDefinition finalStage = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(10, StageType.FinalBoss),
                10); // 최종 보스 스테이지 생성

            Assert.IsTrue(midStage.RoundDefinition.HasBossConfiguration); // 중간 보스 구성 존재 검증
            Assert.IsTrue(finalStage.RoundDefinition.HasBossConfiguration); // 최종 보스 구성 존재 검증
            Assert.AreEqual("MidBoss74", midStage.RoundDefinition.BossResourceName); // 중간 보스 리소스 검증
            Assert.AreEqual("FinalBoss74", finalStage.RoundDefinition.BossResourceName); // 최종 보스 리소스 검증
            Assert.AreNotEqual(midStage.RoundDefinition.BossResourceName, finalStage.RoundDefinition.BossResourceName); // 보스 리소스 분리 검증
        }

        [Test]
        public void FinalBoss_IsStrongerThanMidBoss()
        {
            PieceDefinition midBoss = Resources.Load<PieceDefinition>("MidBoss74"); // 중간 보스 데이터 로드
            PieceDefinition finalBoss = Resources.Load<PieceDefinition>("FinalBoss74"); // 최종 보스 데이터 로드

            Assert.NotNull(midBoss); // 중간 보스 에셋 존재 검증
            Assert.NotNull(finalBoss); // 최종 보스 에셋 존재 검증
            Assert.AreNotEqual(midBoss.PieceId, finalBoss.PieceId); // 서로 다른 보스 ID 검증
            Assert.Greater(finalBoss.BaseHp, midBoss.BaseHp); // 최종 보스 체력 우위 검증
            Assert.GreaterOrEqual(finalBoss.BaseAtk, midBoss.BaseAtk); // 최종 보스 공격력 우위 검증
            Assert.AreEqual(new Vector2Int(2, 2), midBoss.OccupancySize); // 중간 보스 2x2 검증
            Assert.AreEqual(new Vector2Int(2, 2), finalBoss.OccupancySize); // 최종 보스 2x2 검증
        }

        [Test]
        public void FinalBossRound_HasAtLeastAsMuchSupportPressureAsMidBoss()
        {
            StageDefinition midStage = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(7, StageType.MidBoss),
                7); // 중간 보스 스테이지 생성

            StageDefinition finalStage = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(10, StageType.FinalBoss),
                10); // 최종 보스 스테이지 생성

            Assert.GreaterOrEqual(
                finalStage.RoundDefinition.InitialEnemies.Count,
                midStage.RoundDefinition.InitialEnemies.Count); // 최종 보스 시작 지원 적 수 검증

            Assert.GreaterOrEqual(
                finalStage.RoundDefinition.Reinforcements.Count,
                midStage.RoundDefinition.Reinforcements.Count); // 최종 보스 증원 압박 검증
        }
    }
}
