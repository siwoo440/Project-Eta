using NUnit.Framework; // NUnit 테스트 사용
using ProjectEta.Run; // Route·Stage·Reward 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day75StageContentIntegrationTests
    {
        [Test]
        public void FullRoute_AlwaysContainsLateEliteNode()
        {
            var route = StageRouteGenerator.CreateFullRoute(20260912); // 고정 Seed 전체 경로 생성
            bool foundElite = false; // Elite 발견 상태 초기화

            for (int i = 0; i < route.Count; i++)
            {
                StageNode node = route[i]; // 현재 경로 노드 조회
                if (node == null || node.Depth != 8) continue; // 8단계 외 노드 제외
                if (!StageDefinitionCatalog.TryParseStageType(node.StageDefinitionId, out StageType stageType)) continue; // 타입 복원 실패 제외

                if (stageType == StageType.Elite)
                {
                    foundElite = true; // 후반 Elite 노드 발견 기록
                    break; // 추가 탐색 종료
                }
            }

            Assert.IsTrue(foundElite, "전체 Route의 8단계에는 최소 1개의 Elite 노드가 있어야 합니다."); // Elite 실제 경로 노출 검증
        }

        [Test]
        public void FullRoute_SameSeedProducesSameNodeLayout()
        {
            var left = StageRouteGenerator.CreateFullRoute(750075); // 첫 경로 생성
            var right = StageRouteGenerator.CreateFullRoute(750075); // 같은 Seed 두 번째 경로 생성

            Assert.AreEqual(left.Count, right.Count); // 노드 수 동일 검증

            for (int i = 0; i < left.Count; i++)
            {
                Assert.AreEqual(left[i].NodeId, right[i].NodeId); // 노드 ID 재현성 검증
                Assert.AreEqual(left[i].StageDefinitionId, right[i].StageDefinitionId); // Stage 타입 재현성 검증
                Assert.AreEqual(left[i].Position, right[i].Position); // 배치 좌표 재현성 검증
            }
        }

        [Test]
        public void EliteReward_IsStrongerThanNormalBattleReward()
        {
            CardRewardProfile battle = CardRewardQualityRules.GetProfile(
                3,
                CardRewardSource.BattleVictory,
                "PrototypeBattleReward"); // 초반 일반 전투 보상 생성

            CardRewardProfile elite = CardRewardQualityRules.GetProfile(
                3,
                CardRewardSource.EliteVictory,
                "PrototypeEliteReward"); // 초반 Elite 보상 생성

            Assert.Greater(elite.TwoStarWeight + elite.ThreeStarWeight, battle.TwoStarWeight + battle.ThreeStarWeight); // Elite 고등급 가중치 우위 검증
            Assert.GreaterOrEqual(elite.ThreeStarWeight, battle.ThreeStarWeight); // Elite 3성 가중치 열화 방지
        }

        [Test]
        public void MidBossReward_UsesDedicatedAdvancedProfile()
        {
            CardRewardProfile elite = CardRewardQualityRules.GetProfile(
                5,
                CardRewardSource.EliteVictory,
                "PrototypeEliteReward"); // 같은 구간 Elite 기준 보상 생성

            CardRewardProfile midBoss = CardRewardQualityRules.GetProfile(
                5,
                CardRewardSource.MidBossVictory,
                "MidBossReward74"); // 74일차 Mid Boss 전용 보상 생성

            Assert.AreEqual(CardRewardQuality.Advanced, midBoss.Quality); // Mid Boss 고급 보상 검증
            Assert.GreaterOrEqual(midBoss.ThreeStarWeight, elite.ThreeStarWeight); // Mid Boss 3성 가중치가 Elite 이상인지 검증
        }

        [Test]
        public void StageCatalog_ExposesDedicatedEliteAndBossRewardProfileIds()
        {
            StageDefinition elite = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(8, StageType.Elite),
                8); // Elite StageDefinition 생성

            StageDefinition midBoss = StageDefinitionCatalog.Resolve(
                StageDefinitionCatalog.CreateDefinitionId(10, StageType.MidBoss),
                10); // Mid Boss StageDefinition 생성

            Assert.AreEqual("PrototypeEliteReward", elite.RewardProfileId); // Elite 프로필 ID 검증
            Assert.AreEqual("MidBossReward74", midBoss.RewardProfileId); // Mid Boss 프로필 ID 검증
        }
    }
}
