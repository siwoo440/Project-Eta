using System.Linq; // 결과 노드 검색 사용
using NUnit.Framework; // NUnit 테스트 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Board; // Day66RouteBuildingPlan·RouteNodeBuildingStyle 사용
using ProjectEta.Run; // RouteMapState·StageNode·StageType 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day66RouteBuildingPlanTests
    {
        [Test]
        public void Build_IncludesEveryRouteNode_WithoutSceneHierarchyLookup()
        {
            var route = new RouteMapState(66); // 테스트 경로 상태 생성
            var current = new StageNode("current", new Vector2Int(4, 0), 1, StageDefinitionCatalog.CreateDefinitionId(1, StageType.Battle)); // 현재 전투 노드 생성
            var reward = new StageNode("reward", new Vector2Int(3, 1), 2, StageDefinitionCatalog.CreateDefinitionId(2, StageType.Reward)); // 보상 노드 생성
            var shop = new StageNode("shop", new Vector2Int(4, 1), 2, StageDefinitionCatalog.CreateDefinitionId(2, StageType.Shop)); // 상점 노드 생성
            var boss = new StageNode("boss", new Vector2Int(5, 1), 2, StageDefinitionCatalog.CreateDefinitionId(2, StageType.MidBoss)); // 보스 노드 생성
            route.Configure(1, current, new[] { reward, shop, boss }); // Hierarchy 없이 경로 데이터만 구성

            var specs = Day66RouteBuildingPlan.Build(route); // 경로 데이터에서 건물 계획 생성

            Assert.That(specs.Count, Is.EqualTo(4)); // 모든 RouteMap 노드 건물 계획 생성 확인
            Assert.That(specs.Any(spec => spec.NodeId == "reward" && spec.StageType == StageType.Reward), Is.True); // Reward 보물고 분기 확인
            Assert.That(specs.Any(spec => spec.NodeId == "shop" && spec.StageType == StageType.Shop), Is.True); // Shop 건물 분기 확인
            Assert.That(specs.Any(spec => spec.NodeId == "boss" && spec.StageType == StageType.MidBoss), Is.True); // Boss 요새 분기 확인
        }

        [Test]
        public void Build_InvalidStageDefinition_FallsBackToBattleBuilding()
        {
            var route = new RouteMapState(66); // 테스트 경로 상태 생성
            var current = new StageNode("broken", new Vector2Int(4, 0), 1, "invalid_stage_type"); // 잘못된 StageDefinition 노드 생성
            route.Configure(1, current, null); // 단일 노드 경로 구성

            var specs = Day66RouteBuildingPlan.Build(route); // 건물 계획 생성

            Assert.That(specs.Count, Is.EqualTo(1)); // 단일 노드 계획 확인
            Assert.That(specs[0].StageType, Is.EqualTo(StageType.Battle)); // 파싱 실패 일반 전투 fallback 확인
        }

        [Test]
        public void Resolve_RegularBuildings_AreLargeEnoughToReadAsBuildings()
        {
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Battle).Scale, Is.GreaterThanOrEqualTo(1.05f)); // 일반 성채 가시 크기 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Reward).Scale, Is.GreaterThanOrEqualTo(1.05f)); // 보물고 가시 크기 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Shop).Scale, Is.GreaterThanOrEqualTo(1.05f)); // 상점 가시 크기 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Event).Scale, Is.GreaterThanOrEqualTo(1.05f)); // 이벤트 탑 가시 크기 확인
        }
    }
}
