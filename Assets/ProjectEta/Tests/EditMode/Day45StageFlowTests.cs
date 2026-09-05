using NUnit.Framework; // EditMode 테스트·Assert 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Battle; // TurnManager·BattleOutcome 사용
using ProjectEta.Run; // StageRouteGenerator·StageDefinitionCatalog·RunFlowState 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day45StageFlowTests // 45일차 스테이지 경로·판 전환 상태 회귀 테스트
    {
        [Test] // 일반 깊이의 2~3개 분기 생성 검증
        public void GenerateNextNodes_NormalDepth_CreatesTwoOrThreeBranches() // 일반 스테이지 분기 수 확인
        {
            var nodes = StageRouteGenerator.CreateNextNodes(3, new Vector2Int(4, 2)); // 3단계 완료 후 4단계 후보 생성

            Assert.That(nodes.Count, Is.InRange(2, 3)); // 일반 깊이 분기 수 검증

            for (int i = 0; i < nodes.Count; i++) // 생성 노드 순회
            {
                Assert.AreEqual(4, nodes[i].Depth); // 다음 깊이 고정 검증
                Assert.IsTrue(RouteMapState.IsKingStep(new Vector2Int(4, 2), nodes[i].Position)); // 킹 1칸 이동 가능 좌표 검증
            }
        }

        [Test] // 5단계 중간 보스 강제 검증
        public void GenerateNextNodes_DepthFive_ForcesMidBoss() // 4→5단계 보스 노드 확인
        {
            var nodes = StageRouteGenerator.CreateNextNodes(4, new Vector2Int(4, 3)); // 5단계 후보 생성

            Assert.AreEqual(1, nodes.Count); // 보스 깊이는 단일 강제 노드 검증
            Assert.AreEqual(5, nodes[0].Depth); // 중간 보스 깊이 검증
            Assert.AreEqual(StageType.MidBoss, StageDefinitionCatalog.Resolve(nodes[0].StageDefinitionId, nodes[0].Depth).StageType); // 중간 보스 타입 검증
        }

        [Test] // 10단계 최종 보스 강제 검증
        public void GenerateNextNodes_DepthTen_ForcesFinalBoss() // 9→10단계 보스 노드 확인
        {
            var nodes = StageRouteGenerator.CreateNextNodes(9, new Vector2Int(5, 8)); // 10단계 후보 생성

            Assert.AreEqual(1, nodes.Count); // 최종 보스 단일 노드 검증
            Assert.AreEqual(10, nodes[0].Depth); // 최종 깊이 검증
            Assert.AreEqual(StageType.FinalBoss, StageDefinitionCatalog.Resolve(nodes[0].StageDefinitionId, nodes[0].Depth).StageType); // 최종 보스 타입 검증
        }

        [Test] // 43일차의 3개 후보 회귀 유지 검증
        public void PreparePrototypeAfterBattle_RoundThree_KeepsThreeCandidates() // 기존 Day43 테스트 호환 확인
        {
            var map = new RouteMapState(); // 새 경로 지도 생성

            map.PreparePrototypeAfterBattle(3); // 3단계 완료 처리

            Assert.AreEqual(3, map.GetSelectableNodes().Count); // 4단계는 기존처럼 3개 후보 유지 검증
            Assert.AreEqual(new Vector2Int(4, 2), map.KingMapPosition); // 기존 현재 킹 좌표 유지 검증
        }

        [Test] // 비전투 흐름이 지도 보드를 유지하는지 검증
        public void RunFlow_NonBattleStages_KeepMapBoardMode() // Reward·Shop·Event 표시 모드 확인
        {
            var flow = new RunFlowState(); // 새 런 흐름 생성

            flow.EnterReward(); // 보상 스테이지 진입
            Assert.AreEqual(RunFlowPhase.Reward, flow.Phase); // 보상 흐름 검증
            Assert.AreEqual(BoardMode.Map, flow.BoardMode); // 경로 지도 표시 유지 검증

            flow.EnterShop(); // 상점 스테이지 진입
            Assert.AreEqual(RunFlowPhase.Shop, flow.Phase); // 상점 흐름 검증
            Assert.AreEqual(BoardMode.Map, flow.BoardMode); // 경로 지도 표시 유지 검증

            flow.EnterEvent(); // 이벤트 스테이지 진입
            Assert.AreEqual(RunFlowPhase.Event, flow.Phase); // 이벤트 흐름 검증
            Assert.AreEqual(BoardMode.Map, flow.BoardMode); // 경로 지도 표시 유지 검증
        }

        [Test] // 동일 TurnManager를 다음 전투에 재사용할 수 있는지 검증
        public void TurnManager_ResetForNewBattle_RestoresInitialDeployment() // BattleEnded→새 전투 상태 초기화 확인
        {
            var turnManager = new TurnManager(); // 새 턴 상태 생성
            turnManager.SetPlayerActionTransitionDeferred(true); // 런타임 연출 지연 모드 활성화
            turnManager.EndBattle(BattleOutcome.Victory); // 이전 전투 종료

            turnManager.ResetForNewBattle(); // 다음 스테이지 전투용 초기화

            Assert.AreEqual(TurnState.DeploymentTurn, turnManager.CurrentState); // 시작 배치 턴 복구 검증
            Assert.AreEqual(1, turnManager.TurnNumber); // 턴 번호 초기화 검증
            Assert.IsTrue(turnManager.IsInitialDeployment); // 시작 배치 상태 검증
            Assert.IsFalse(turnManager.IsInitialKingPlaced); // 킹 재배치 필요 상태 검증
            Assert.AreEqual(BattleOutcome.None, turnManager.Outcome); // 이전 승패 결과 제거 검증
            Assert.IsTrue(turnManager.IsPlayerActionTransitionDeferred); // 연출 지연 설정 유지 검증
        }
    }
}
