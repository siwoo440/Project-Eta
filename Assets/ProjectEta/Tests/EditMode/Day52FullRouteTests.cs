using System.Collections.Generic; // List<T> 사용
using System.Text; // StringBuilder 사용
using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Run; // 경로 지도·스테이지 타입 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day52FullRouteTests
    {
        [Test]
        public void FullRoute_SameSeed_ProducesSameGraph()
        {
            const int seed = 520052; // 고정 경로 시드

            IReadOnlyList<StageNode> first = StageRouteGenerator.CreateFullRoute(seed); // 첫 전체 경로 생성
            IReadOnlyList<StageNode> second = StageRouteGenerator.CreateFullRoute(seed); // 같은 시드 전체 경로 재생성

            Assert.AreEqual(BuildSignature(first), BuildSignature(second)); // 같은 시드 그래프 완전 일치 검증
        }

        [Test]
        public void FullRoute_DifferentSeed_ChangesRoutePrototype()
        {
            IReadOnlyList<StageNode> first = StageRouteGenerator.CreateFullRoute(520001); // 첫 시드 전체 경로 생성
            IReadOnlyList<StageNode> second = StageRouteGenerator.CreateFullRoute(520002); // 다른 시드 전체 경로 생성

            Assert.AreNotEqual(BuildSignature(first), BuildSignature(second)); // 다른 시드 경로 변화 검증
        }

        [Test]
        public void FullRoute_HasOneToTenDepthsAndExpectedBranchCounts()
        {
            IReadOnlyList<StageNode> nodes = StageRouteGenerator.CreateFullRoute(520052); // 전체 경로 생성

            Assert.AreEqual(1, CountDepth(nodes, 1)); // 1단계 시작 노드 1개 검증
            Assert.AreEqual(1, CountDepth(nodes, 5)); // 5단계 중간 보스 단일 노드 검증
            Assert.AreEqual(1, CountDepth(nodes, 10)); // 10단계 최종 보스 단일 노드 검증

            for (int depth = 2; depth <= 9; depth++)
            {
                if (depth == 5) continue; // 중간 보스 깊이 일반 분기 검사 제외

                int count = CountDepth(nodes, depth); // 현재 깊이 노드 수 계산
                Assert.GreaterOrEqual(count, 2, $"{depth}단계 분기가 2개 미만입니다."); // 일반 깊이 최소 2개 검증
                Assert.LessOrEqual(count, 3, $"{depth}단계 분기가 3개를 초과합니다."); // 일반 깊이 최대 3개 검증
            }
        }

        [Test]
        public void FullRoute_ForcesMidBossAndFinalBoss()
        {
            IReadOnlyList<StageNode> nodes = StageRouteGenerator.CreateFullRoute(520052); // 전체 경로 생성
            StageNode midBoss = FindSingleDepth(nodes, 5); // 5단계 단일 노드 조회
            StageNode finalBoss = FindSingleDepth(nodes, 10); // 10단계 단일 노드 조회

            Assert.IsTrue(StageDefinitionCatalog.TryParseStageType(midBoss.StageDefinitionId, out StageType midType)); // 중간 보스 타입 파싱 검증
            Assert.AreEqual(StageType.MidBoss, midType); // 5단계 중간 보스 고정 검증
            Assert.IsTrue(StageDefinitionCatalog.TryParseStageType(finalBoss.StageDefinitionId, out StageType finalType)); // 최종 보스 타입 파싱 검증
            Assert.AreEqual(StageType.FinalBoss, finalType); // 10단계 최종 보스 고정 검증
        }

        [Test]
        public void FullRoute_NormalDepthsUseBattleEliteRewardShopEventOnly()
        {
            IReadOnlyList<StageNode> nodes = StageRouteGenerator.CreateFullRoute(520052); // 전체 경로 생성

            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode node = nodes[i]; // 현재 노드 조회
                if (node.Depth == 5 || node.Depth == 10) continue; // 보스 깊이 제외

                Assert.IsTrue(StageDefinitionCatalog.TryParseStageType(node.StageDefinitionId, out StageType stageType)); // 일반 노드 타입 파싱 검증
                Assert.IsTrue(
                    stageType == StageType.Battle ||
                    stageType == StageType.Elite ||
                    stageType == StageType.Reward ||
                    stageType == StageType.Shop ||
                    stageType == StageType.Event); // 75일차 전체 경로 허용 타입 검증
            }
        }

        [Test]
        public void FullRoute_EveryConnectionIsOneKingStepAndNoDeadEndsBeforeFinalBoss()
        {
            IReadOnlyList<StageNode> nodes = StageRouteGenerator.CreateFullRoute(520052); // 전체 경로 생성
            var byId = new Dictionary<string, StageNode>(); // 노드 ID 조회 사전 생성

            for (int i = 0; i < nodes.Count; i++)
            {
                byId[nodes[i].NodeId] = nodes[i]; // 전체 노드 ID 등록
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode source = nodes[i]; // 현재 출발 노드 조회

                if (source.Depth < 10)
                {
                    Assert.Greater(source.NextNodeIds.Count, 0, $"{source.NodeId}가 막다른 길입니다."); // 최종 전 이전 노드 다음 연결 보장
                }

                for (int nextIndex = 0; nextIndex < source.NextNodeIds.Count; nextIndex++)
                {
                    string targetId = source.NextNodeIds[nextIndex]; // 다음 노드 ID 조회
                    Assert.IsTrue(byId.TryGetValue(targetId, out StageNode target), $"연결 대상 {targetId}가 없습니다."); // 연결 대상 존재 검증
                    Assert.AreEqual(source.Depth + 1, target.Depth); // 정확히 다음 깊이 연결 검증
                    Assert.IsTrue(RouteMapState.IsKingStep(source.Position, target.Position), $"{source.NodeId}->{target.NodeId}가 킹 1칸 이동이 아닙니다."); // 8방향 1칸 이동 검증
                }
            }

            for (int depth = 2; depth <= 10; depth++)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    StageNode target = nodes[i]; // 현재 도착 후보 조회
                    if (target.Depth != depth) continue; // 검사 깊이 외 노드 제외
                    Assert.IsTrue(HasIncoming(nodes, target.NodeId), $"{target.NodeId}에 들어오는 경로가 없습니다."); // 모든 노드 도달 가능 검증
                }
            }
        }

        [Test]
        public void RouteMap_FirstVictoryBuildsWholeGraphAndOnlyNextDepthIsSelectable()
        {
            var route = new RouteMapState(520052); // 고정 시드 경로 상태 생성

            route.PreparePrototypeAfterBattle(1); // 1단계 승리 후 지도 준비

            Assert.IsTrue(route.HasCompleteRoute); // 1~10 전체 그래프 생성 검증
            Assert.AreEqual(1, route.CurrentDepth); // 현재 완료 깊이 1 유지 검증
            Assert.AreEqual(1, route.CurrentNode.Depth); // 현재 루트 노드 깊이 검증
            Assert.IsFalse(route.HasSelectedNode); // 다음 스테이지 선택 전 상태 검증
            Assert.Greater(route.Nodes.Count, 10); // 단일 다음 층이 아닌 전체 그래프 보유 검증

            IReadOnlyList<StageNode> selectable = route.GetSelectableNodes(); // 현재 이동 가능한 노드 조회
            Assert.GreaterOrEqual(selectable.Count, 2); // 2단계 최소 2개 분기 검증

            for (int i = 0; i < selectable.Count; i++)
            {
                Assert.AreEqual(2, selectable[i].Depth); // 바로 다음 2단계만 선택 가능 검증
            }
        }

        [Test]
        public void RouteMap_StageCompletionKeepsWholeGraphAndOpensNextDepth()
        {
            var route = new RouteMapState(520052); // 고정 시드 경로 상태 생성
            route.PreparePrototypeAfterBattle(1); // 전체 경로 초기 생성
            int originalNodeCount = route.Nodes.Count; // 전체 그래프 노드 수 저장
            StageNode stage2 = route.GetSelectableNodes()[0]; // 첫 2단계 선택 노드 조회

            Assert.IsTrue(route.TryMoveKingTo(stage2.NodeId)); // 지도 킹 2단계 이동
            Assert.IsTrue(route.HasSelectedNode); // 스테이지 진입 대기 선택 상태 검증

            route.PreparePrototypeAfterBattle(2); // 2단계 완료 후 다음 경로 개방

            Assert.AreEqual(originalNodeCount, route.Nodes.Count); // 전체 그래프 재생성·유실 없음 검증
            Assert.AreEqual(stage2.NodeId, route.CurrentNodeId); // 현재 킹 노드 유지 검증
            Assert.IsFalse(route.HasSelectedNode); // 완료 후 선택 잠금 해제 검증

            IReadOnlyList<StageNode> selectable = route.GetSelectableNodes(); // 3단계 선택 후보 조회
            Assert.Greater(selectable.Count, 0); // 다음 경로 존재 검증

            for (int i = 0; i < selectable.Count; i++)
            {
                Assert.AreEqual(3, selectable[i].Depth); // 정확히 다음 깊이만 개방 검증
            }
        }

        [Test]
        public void RouteMap_SaveRestore_PreservesWholeGraphAndSeed()
        {
            var route = new RouteMapState(520052); // 고정 시드 경로 상태 생성
            route.PreparePrototypeAfterBattle(1); // 전체 경로 생성
            StageNode stage2 = route.GetSelectableNodes()[0]; // 2단계 첫 후보 조회
            Assert.IsTrue(route.TryMoveKingTo(stage2.NodeId)); // 2단계 선택 기록
            route.PreparePrototypeAfterBattle(2); // 2단계 완료 상태 전환
            string beforeSignature = BuildSignature(route.Nodes); // 저장 전 전체 그래프 서명 생성

            RouteMapSaveData data = route.ToSaveData(); // 51일차 저장 DTO 생성
            var restored = new RouteMapState(1); // 다른 임시 시드 복원 대상 생성
            restored.Restore(data); // 저장 경로 전체 복원

            Assert.AreEqual(520052, restored.MapSeed); // 원래 Map Seed 복원 검증
            Assert.IsTrue(restored.HasCompleteRoute); // 복원 후 전체 10단계 그래프 유지 검증
            Assert.AreEqual(stage2.NodeId, restored.CurrentNodeId); // 현재 노드 복원 검증
            Assert.AreEqual(beforeSignature, BuildSignature(restored.Nodes)); // 전체 노드·연결 동일성 검증
        }

        private static int CountDepth(IReadOnlyList<StageNode> nodes, int depth)
        {
            int count = 0; // 깊이 노드 수 초기화

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Depth == depth) count++; // 같은 깊이 노드 누적
            }

            return count; // 깊이별 노드 수 반환
        }

        private static StageNode FindSingleDepth(IReadOnlyList<StageNode> nodes, int depth)
        {
            StageNode result = null; // 단일 깊이 결과 초기화

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Depth != depth) continue; // 다른 깊이 제외
                Assert.IsNull(result, $"{depth}단계에 노드가 2개 이상입니다."); // 단일 노드 조건 검증
                result = nodes[i]; // 현재 깊이 노드 저장
            }

            Assert.IsNotNull(result, $"{depth}단계 노드가 없습니다."); // 필수 깊이 존재 검증
            return result; // 단일 깊이 노드 반환
        }

        private static bool HasIncoming(IReadOnlyList<StageNode> nodes, string targetId)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode source = nodes[i]; // 현재 출발 노드 조회

                for (int nextIndex = 0; nextIndex < source.NextNodeIds.Count; nextIndex++)
                {
                    if (source.NextNodeIds[nextIndex] == targetId) return true; // 도착 ID 연결 발견
                }
            }

            return false; // 들어오는 연결 없음
        }

        private static string BuildSignature(IReadOnlyList<StageNode> nodes)
        {
            var builder = new StringBuilder(); // 그래프 비교 문자열 생성기

            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode node = nodes[i]; // 현재 노드 조회
                builder.Append(node.Depth).Append('|'); // 깊이 기록
                builder.Append(node.NodeId).Append('|'); // 노드 ID 기록
                builder.Append(node.Position.x).Append(',').Append(node.Position.y).Append('|'); // 좌표 기록
                builder.Append(node.StageDefinitionId).Append('|'); // 스테이지 정의 기록

                for (int nextIndex = 0; nextIndex < node.NextNodeIds.Count; nextIndex++)
                {
                    builder.Append(node.NextNodeIds[nextIndex]).Append(','); // 연결 순서 기록
                }

                builder.Append(';'); // 노드 구분자 기록
            }

            return builder.ToString(); // 전체 그래프 서명 반환
        }
    }
}
