using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용
using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Battle; // BattleOutcome 사용
using ProjectEta.Run; // 5페이즈 RouteMap 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day71FivePhaseRouteTests
    {
        [Test]
        public void Phase2Route_노드ID에Phase가포함된다()
        {
            IReadOnlyList<StageNode> nodes = RunPhaseRouteGenerator.CreateFullRoute(12345, 2); // 2페이즈 전체 경로 생성

            Assert.Greater(nodes.Count, 0); // 실제 경로 생성 확인

            for (int i = 0; i < nodes.Count; i++)
            {
                Assert.That(nodes[i].NodeId, Does.StartWith("phase_2_")); // 2페이즈 노드 고유 ID 검증
            }
        }

        [Test]
        public void Phase1부터4까지_마지막노드는MidBoss다()
        {
            for (int phase = 1; phase < RunPhaseProgressService.TotalPhases; phase++)
            {
                IReadOnlyList<StageNode> nodes = RunPhaseRouteGenerator.CreateFullRoute(22091, phase); // 현재 페이즈 경로 생성
                StageNode lastNode = FindDepth(nodes, RoundState.FinalRound); // 10번째 깊이 노드 조회

                Assert.IsNotNull(lastNode); // 마지막 노드 존재 확인
                Assert.IsTrue(StageDefinitionCatalog.TryParseStageType(lastNode.StageDefinitionId, out StageType stageType)); // StageType 복원 확인
                Assert.AreEqual(StageType.MidBoss, stageType); // 1~4페이즈 중간 보스 처리 검증
            }
        }

        [Test]
        public void Phase5_마지막노드는FinalBoss다()
        {
            IReadOnlyList<StageNode> nodes = RunPhaseRouteGenerator.CreateFullRoute(22091, RunPhaseProgressService.TotalPhases); // 5페이즈 경로 생성
            StageNode lastNode = FindDepth(nodes, RoundState.FinalRound); // 10번째 깊이 노드 조회

            Assert.IsNotNull(lastNode); // 마지막 노드 존재 확인
            Assert.IsTrue(StageDefinitionCatalog.TryParseStageType(lastNode.StageDefinitionId, out StageType stageType)); // StageType 복원 확인
            Assert.AreEqual(StageType.FinalBoss, stageType); // 최종 페이즈 최종 보스 유지 검증
        }

        [Test]
        public void FirstBattle완료_RunStageFlow경로에서1페이즈MidBoss로정규화한다()
        {
            var runState = new RunState(3); // 첫 전투 완료 테스트 런 생성

            bool completed = RunStageFlowService.CompleteBattle(runState, BattleOutcome.Victory); // 실제 전투 완료 서비스 경로 실행
            StageNode lastNode = FindDepth(runState.RouteMap.Nodes, RoundState.FinalRound); // 정규화된 마지막 노드 조회

            Assert.IsTrue(completed); // 실제 전투 완료 처리 성공 확인
            Assert.AreEqual(RunFlowPhase.Map, runState.CurrentFlowPhase); // 첫 승리 후 지도 상태 확인
            Assert.IsNotNull(lastNode); // 마지막 노드 존재 확인
            Assert.IsTrue(StageDefinitionCatalog.TryParseStageType(lastNode.StageDefinitionId, out StageType stageType)); // 마지막 타입 복원 확인
            Assert.AreEqual(StageType.MidBoss, stageType); // 1페이즈가 최종 보스로 표시되지 않는지 확인
        }

        [Test]
        public void Phase1Stage10승리_RunStageFlow경로에서Phase2시작점으로직접전환한다()
        {
            var runState = new RunState(3); // 실제 전투 완료 경로 테스트 런 생성
            ConfigurePhaseAtFinalStage(runState, 1); // 1페이즈 마지막 스테이지 상태 구성

            bool completed = RunStageFlowService.CompleteBattle(runState, BattleOutcome.Victory); // 실제 Stage 10 승리 처리 실행

            Assert.IsTrue(completed); // 전투 완료 처리 성공 확인
            Assert.AreEqual(2, RunPhaseProgressService.GetCurrentPhase(runState)); // 2페이즈 진입 확인
            Assert.AreEqual(RoundState.FirstRound, runState.CurrentRound); // 런 깊이 시작점 복귀 확인
            Assert.AreEqual(RoundState.FirstRound, runState.RouteMap.CurrentDepth); // 지도 깊이 시작점 복귀 확인
            Assert.AreEqual(RunFlowPhase.Map, runState.CurrentFlowPhase); // Completed를 거치지 않는 최종 상태 확인
            Assert.IsTrue(runState.RouteMap.HasCompleteRoute); // 새 페이즈 전체 경로 생성 확인
            Assert.IsFalse(runState.RouteMap.HasSelectedNode); // 새 페이즈 선택 잠금 해제 확인
        }

        [Test]
        public void Phase5Stage10승리_RunStageFlow경로에서최종Completed를유지한다()
        {
            var runState = new RunState(3); // 최종 페이즈 테스트 런 생성
            ConfigurePhaseAtFinalStage(runState, RunPhaseProgressService.TotalPhases); // 5페이즈 마지막 스테이지 상태 구성

            bool completed = RunStageFlowService.CompleteBattle(runState, BattleOutcome.Victory); // 실제 최종 보스 승리 처리 실행

            Assert.IsTrue(completed); // 최종 전투 완료 처리 성공 확인
            Assert.AreEqual(RunPhaseProgressService.TotalPhases, RunPhaseProgressService.GetCurrentPhase(runState)); // 5페이즈 유지 확인
            Assert.AreEqual(RoundState.FinalRound, runState.CurrentRound); // 최종 스테이지 번호 유지 확인
            Assert.AreEqual(RunFlowPhase.Completed, runState.CurrentFlowPhase); // 최종 런 완료 상태 확인
        }

        [Test]
        public void Phase1Stage10비전투완료_다음Phase시작점으로직접전환한다()
        {
            var runState = new RunState(3); // 비전투 마지막 깊이 안전 처리 테스트 런 생성
            ConfigurePhaseAtFinalStage(runState, 1); // 1페이즈 마지막 깊이 상태 구성
            runState.Flow.EnterEvent(); // 비전투 스테이지 진행 상태 재현

            bool completed = RunStageFlowService.CompleteNonBattleStage(runState); // 비전투 마지막 깊이 완료 처리 실행

            Assert.IsTrue(completed); // 비전투 완료 처리 성공 확인
            Assert.AreEqual(2, RunPhaseProgressService.GetCurrentPhase(runState)); // 다음 페이즈 진입 확인
            Assert.AreEqual(RunFlowPhase.Map, runState.CurrentFlowPhase); // 새 페이즈 지도 복귀 확인
            Assert.AreEqual(RoundState.FirstRound, runState.CurrentRound); // 새 페이즈 시작 깊이 확인
            Assert.IsFalse(runState.RouteMap.HasSelectedNode); // 새 페이즈 선택 잠금 해제 확인
        }

        [Test]
        public void LegacyNodeId_Phase1로복원된다()
        {
            var runState = new RunState(3); // 구버전 호환 테스트 런 생성

            Assert.AreEqual(1, RunPhaseProgressService.GetCurrentPhase(runState)); // Phase 정보 없는 기존 런을 1페이즈로 처리
        }

        private static void ConfigurePhaseAtFinalStage(RunState runState, int phase)
        {
            IReadOnlyList<StageNode> nodes = RunPhaseRouteGenerator.CreateFullRoute(runState.RouteMap.MapSeed, phase); // 요청 페이즈 전체 경로 생성
            StageNode finalNode = FindDepth(nodes, RoundState.FinalRound); // 요청 페이즈 마지막 노드 조회
            var otherNodes = new List<StageNode>(); // 마지막 노드를 제외한 지도 노드 목록 생성

            Assert.IsNotNull(finalNode); // 테스트 전제인 마지막 노드 존재 확인

            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode node = nodes[i]; // 현재 경로 노드 조회
                if (node == null || object.ReferenceEquals(node, finalNode)) continue; // 빈 노드·현재 노드 제외
                otherNodes.Add(node); // 나머지 지도 노드 등록
            }

            runState.RouteMap.Configure(RoundState.FinalRound, finalNode, otherNodes); // 킹을 해당 페이즈 마지막 노드에 배치
            runState.CurrentRound = RoundState.FinalRound; // 현재 스테이지 번호를 마지막 깊이로 동기화
            runState.Flow.EnterBattle(); // 실제 전투 종료 직전 상태로 전환
        }

        private static StageNode FindDepth(IReadOnlyList<StageNode> nodes, int depth)
        {
            if (nodes == null) return null; // 빈 경로 방어

            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode node = nodes[i]; // 현재 노드 조회
                if (node != null && node.Depth == depth) return node; // 요청 깊이 첫 노드 반환
            }

            return null; // 일치 깊이 없음
        }
    }
}
