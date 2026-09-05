using NUnit.Framework; // EditMode 테스트·Assert 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Battle; // BattleOutcome 사용
using ProjectEta.Run; // 43일차 런·경로 상태 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day43RunFlowTests // 43일차 런 모드·스테이지 경로 구조 회귀 테스트
    {
        [Test] // 새 런 흐름 기본값 검증
        public void NewRun_StartsInBattleMode() // 새 런의 전투 모드 시작 확인
        {
            var runState = new RunState(3); // 테스트 런 생성

            Assert.AreEqual(RunFlowPhase.Battle, runState.CurrentFlowPhase); // 전투 흐름 상태 검증
            Assert.AreEqual(BoardMode.Battle, runState.CurrentBoardMode); // 전투판 모드 검증
            Assert.AreEqual(0, runState.SelectableStageNodes.Count); // 시작 시 경로 선택 없음 검증
        }

        [Test] // 일반 스테이지 승리 후 경로 지도 진입 검증
        public void BattleVictory_EntersMapModeAndKeepsClearedRound() // 승리→지도 상태 전환 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 3; // 일반 스테이지 지정
            runState.StartCurrentRound(); // 전투 시작

            runState.HandleBattleOutcome(BattleOutcome.Victory); // 승리 결과와 런 흐름 처리

            Assert.AreEqual(RoundProgressStatus.Cleared, runState.CurrentRoundStatus); // 라운드 클리어 유지 검증
            Assert.AreEqual(BattleOutcome.Victory, runState.LastBattleOutcome); // 승리 결과 유지 검증
            Assert.AreEqual(RunFlowPhase.Map, runState.CurrentFlowPhase); // 지도 흐름 진입 검증
            Assert.AreEqual(BoardMode.Map, runState.CurrentBoardMode); // 체스판 지도 모드 검증
        }

        [Test] // 승리 후 다음 스테이지 후보 상태 검증
        public void BattleVictory_PreparesThreeSelectablePrototypeNodes() // 다음 깊이 3개 후보 구성 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 3; // 3단계 지정
            runState.StartCurrentRound(); // 전투 시작

            runState.HandleBattleOutcome(BattleOutcome.Victory); // 승리 후 지도 진입

            Assert.AreEqual(3, runState.RouteMap.CurrentDepth); // 현재 완료 깊이 검증
            Assert.AreEqual(new Vector2Int(4, 2), runState.RouteMap.KingMapPosition); // 현재 킹 지도 좌표 검증
            Assert.AreEqual(3, runState.SelectableStageNodes.Count); // 다음 후보 3개 검증

            for (int i = 0; i < runState.SelectableStageNodes.Count; i++) // 후보 노드 순회
            {
                Assert.AreEqual(4, runState.SelectableStageNodes[i].Depth); // 다음 깊이 노드 검증
            }
        }

        [Test] // 패배 시 지도 진입 금지 검증
        public void BattleDefeat_EntersFailedFlowWithoutMap() // 패배→런 실패 흐름 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 4; // 일반 스테이지 지정
            runState.StartCurrentRound(); // 전투 시작

            runState.HandleBattleOutcome(BattleOutcome.Defeat); // 패배 결과와 런 흐름 처리

            Assert.AreEqual(RoundProgressStatus.Failed, runState.CurrentRoundStatus); // 라운드 실패 검증
            Assert.AreEqual(RunFlowPhase.Failed, runState.CurrentFlowPhase); // 런 실패 상태 검증
            Assert.AreEqual(BoardMode.Battle, runState.CurrentBoardMode); // 지도 전환 차단 검증
            Assert.AreEqual(0, runState.SelectableStageNodes.Count); // 경로 후보 미생성 검증
        }

        [Test] // 최종 스테이지 승리 처리 검증
        public void FinalRoundVictory_CompletesRunWithoutMapTransition() // 10단계 승리→런 완료 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = RoundState.FinalRound; // 최종 스테이지 지정
            runState.StartCurrentRound(); // 최종 전투 시작

            runState.HandleBattleOutcome(BattleOutcome.Victory); // 최종 승리 처리

            Assert.AreEqual(RoundProgressStatus.Cleared, runState.CurrentRoundStatus); // 최종 라운드 클리어 검증
            Assert.AreEqual(RunFlowPhase.Completed, runState.CurrentFlowPhase); // 런 완료 상태 검증
            Assert.AreEqual(BoardMode.Battle, runState.CurrentBoardMode); // 지도 전환 없음 검증
            Assert.AreEqual(0, runState.SelectableStageNodes.Count); // 다음 경로 없음 검증
        }

        [Test] // 그래프 연결 기반 선택 후보 검증
        public void RouteMap_SelectableNodesFollowCurrentNodeConnections() // 현재 노드 연결만 선택 가능 확인
        {
            var current = new StageNode("current", new Vector2Int(4, 3), 4, "Resolved"); // 현재 노드 생성
            var left = new StageNode("left", new Vector2Int(3, 4), 5, "PrototypeBattle"); // 왼쪽 후보 생성
            var right = new StageNode("right", new Vector2Int(5, 4), 5, "PrototypeBattle"); // 오른쪽 후보 생성
            var orphan = new StageNode("orphan", new Vector2Int(8, 8), 9, "PrototypeBattle"); // 비연결 노드 생성
            current.SetNextNodeIds(new[] { "left", "right", "left" }); // 중복 포함 연결 지정

            var routeMap = new RouteMapState(); // 지도 상태 생성
            routeMap.Configure(4, current, new[] { left, right, orphan }); // 현재 지도 구성

            Assert.AreEqual(2, routeMap.GetSelectableNodes().Count); // 중복 제거·연결 후보 수 검증
            Assert.AreEqual("left", routeMap.GetSelectableNodes()[0].NodeId); // 왼쪽 후보 순서 검증
            Assert.AreEqual("right", routeMap.GetSelectableNodes()[1].NodeId); // 오른쪽 후보 순서 검증
        }
    }
}
