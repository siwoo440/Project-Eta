using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Battle; // BattleOutcome 사용
using ProjectEta.Run; // 53일차 통합 런 흐름 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day53RogueliteLoopTests
    {
        [Test]
        public void PrepareSelectedStage_UsesNodeDepthAsSingleStageNumberSource()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.RouteMap.PreparePrototypeAfterBattle(1); // Stage 1 완료 후 전체 지도 준비
            StageNode target = run.RouteMap.GetSelectableNodes()[0]; // Stage 2 첫 선택 가능 노드 조회
            Assert.IsTrue(run.RouteMap.TryMoveKingTo(target.NodeId)); // 실제 지도 노드 선택
            StageDefinition definition = StageDefinitionCatalog.Resolve(target.StageDefinitionId, target.Depth); // 선택 스테이지 정의 조회

            bool prepared = RunStageFlowService.TrySynchronizeSelectedStage(run, target, definition); // 선택 노드 기준 스테이지 진입 상태 준비

            Assert.IsTrue(prepared); // 진입 준비 성공 검증
            Assert.AreEqual(target.Depth, run.CurrentRound); // StageNode.Depth와 CurrentRound 일치 검증
            Assert.AreEqual(target.Depth, run.RouteMap.CurrentDepth); // RouteMap 깊이와 CurrentRound 일치 검증
        }

        [Test]
        public void PrepareSelectedStage_RejectsNodeThatWasNotSelected()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.RouteMap.PreparePrototypeAfterBattle(1); // 전체 지도 준비
            var selectable = run.RouteMap.GetSelectableNodes(); // Stage 2 선택 후보 조회
            StageNode selected = selectable[0]; // 실제 선택 노드 지정
            StageNode wrong = selectable[1]; // 선택하지 않은 다른 노드 지정
            Assert.IsTrue(run.RouteMap.TryMoveKingTo(selected.NodeId)); // 첫 노드 실제 선택
            StageDefinition wrongDefinition = StageDefinitionCatalog.Resolve(wrong.StageDefinitionId, wrong.Depth); // 잘못된 노드 정의 조회

            bool prepared = RunStageFlowService.TrySynchronizeSelectedStage(run, wrong, wrongDefinition); // 선택하지 않은 노드 진입 시도

            Assert.IsFalse(prepared); // 비선택 노드 진입 차단 검증
            Assert.AreEqual(1, run.CurrentRound); // 잘못된 진입으로 현재 스테이지가 변하지 않음 검증
        }

        [Test]
        public void PrepareSelectedNonBattleStage_EntersMatchingFlowPhase()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.RouteMap.PreparePrototypeAfterBattle(1); // 전체 지도 준비
            StageNode target = FindFirstNonBattle(run); // Stage 2 비전투 노드 조회
            Assert.IsNotNull(target); // 비전투 후보 존재 검증
            Assert.IsTrue(run.RouteMap.TryMoveKingTo(target.NodeId)); // 비전투 노드 선택
            StageDefinition definition = StageDefinitionCatalog.Resolve(target.StageDefinitionId, target.Depth); // 실제 스테이지 정의 조회

            bool prepared = RunStageFlowService.TrySynchronizeSelectedStage(run, target, definition); // 선택 노드 깊이 동기화
            bool entered = RunStageFlowService.EnterNonBattleStage(run, definition); // StageType에 맞는 실제 비전투 Flow 진입

            Assert.IsTrue(prepared); // 선택 노드 동기화 성공 검증
            Assert.IsTrue(entered); // 비전투 진입 성공 검증
            Assert.AreEqual(ToExpectedPhase(definition.StageType), run.CurrentFlowPhase); // StageType별 RunFlowPhase 연결 검증
            Assert.AreEqual(RoundProgressStatus.InProgress, run.CurrentRoundStatus); // 비전투 스테이지 진행 중 상태 검증
        }

        [Test]
        public void CompleteNonBattleStage_ReturnsToSameFullRouteAndOpensNextDepth()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.RouteMap.PreparePrototypeAfterBattle(1); // 전체 지도 생성
            int originalNodeCount = run.RouteMap.Nodes.Count; // 전체 그래프 노드 수 저장
            StageNode target = FindFirstNonBattle(run); // Stage 2 비전투 노드 조회
            Assert.IsTrue(run.RouteMap.TryMoveKingTo(target.NodeId)); // 비전투 노드 이동
            StageDefinition definition = StageDefinitionCatalog.Resolve(target.StageDefinitionId, target.Depth); // 스테이지 정의 조회
            Assert.IsTrue(RunStageFlowService.TrySynchronizeSelectedStage(run, target, definition)); // 선택 노드 깊이 동기화
            Assert.IsTrue(RunStageFlowService.EnterNonBattleStage(run, definition)); // 비전투 흐름 진입

            bool completed = RunStageFlowService.CompleteNonBattleStage(run); // 비전투 스테이지 완료

            Assert.IsTrue(completed); // 완료 처리 성공 검증
            Assert.AreEqual(RunFlowPhase.Map, run.CurrentFlowPhase); // 지도 복귀 검증
            Assert.AreEqual(RoundProgressStatus.Cleared, run.CurrentRoundStatus); // 현재 스테이지 완료 상태 검증
            Assert.AreEqual(originalNodeCount, run.RouteMap.Nodes.Count); // 52일차 전체 그래프 유지 검증
            Assert.IsFalse(run.RouteMap.HasSelectedNode); // 다음 노드 선택 잠금 해제 검증
            Assert.Greater(run.RouteMap.GetSelectableNodes().Count, 0); // 다음 스테이지 경로 존재 검증

            for (int i = 0; i < run.RouteMap.GetSelectableNodes().Count; i++)
            {
                Assert.AreEqual(target.Depth + 1, run.RouteMap.GetSelectableNodes()[i].Depth); // 정확히 다음 깊이만 선택 가능 검증
            }
        }

        [Test]
        public void CompleteBattleVictory_StageFourOpensForcedMidBoss()
        {
            var run = new RunState(3); // 테스트 런 생성
            AdvanceToDepth(run, 4); // Stage 4 전투 상태까지 진행
            run.StartCurrentRound(); // Stage 4 전투 진행 상태 설정

            bool completed = RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory); // Stage 4 전투 승리 처리

            Assert.IsTrue(completed); // 전투 결과 처리 성공 검증
            Assert.AreEqual(RunFlowPhase.Map, run.CurrentFlowPhase); // 승리 후 지도 복귀 검증
            Assert.AreEqual(1, run.RouteMap.GetSelectableNodes().Count); // Stage 5 강제 단일 노드 검증
            StageNode midBoss = run.RouteMap.GetSelectableNodes()[0]; // Stage 5 노드 조회
            StageDefinition definition = StageDefinitionCatalog.Resolve(midBoss.StageDefinitionId, midBoss.Depth); // Stage 5 정의 조회
            Assert.AreEqual(5, midBoss.Depth); // 중간 보스 깊이 검증
            Assert.AreEqual(StageType.MidBoss, definition.StageType); // 중간 보스 타입 검증
        }

        [Test]
        public void CompleteBattleReward_ReturnsToPreparedMapWithoutCompletingStageAgain()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.StartCurrentRound(); // Stage 1 전투 시작
            Assert.IsTrue(RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory)); // 전투 승리로 지도 준비
            int nodeCount = run.RouteMap.Nodes.Count; // 승리 직후 전체 그래프 노드 수 저장
            run.Flow.EnterReward(); // CardRewardController의 전투 승리 보상 진입 재현

            bool completed = RunStageFlowService.CompleteBattleReward(run); // 전투 승리 카드 보상 완료

            Assert.IsTrue(completed); // 전투 보상 완료 성공 검증
            Assert.AreEqual(RunFlowPhase.Map, run.CurrentFlowPhase); // 지도 선택 상태 복귀 검증
            Assert.AreEqual(RoundProgressStatus.Cleared, run.CurrentRoundStatus); // 이미 끝난 전투 스테이지 상태 유지 검증
            Assert.AreEqual(nodeCount, run.RouteMap.Nodes.Count); // 카드 보상으로 전체 그래프를 다시 생성하지 않음 검증
            Assert.IsFalse(run.RouteMap.HasSelectedNode); // 다음 Stage 선택 가능 상태 유지 검증
        }

        [Test]
        public void CompleteBattleDefeat_EndsRunWithoutOpeningMap()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.StartCurrentRound(); // Stage 1 전투 진행 시작

            bool completed = RunStageFlowService.CompleteBattle(run, BattleOutcome.Defeat); // 전투 패배 처리

            Assert.IsTrue(completed); // 패배 결과 처리 성공 검증
            Assert.AreEqual(RunFlowPhase.Failed, run.CurrentFlowPhase); // 런 실패 상태 검증
            Assert.IsTrue(run.Flow.IsRunFinished); // 종료 런 판정 검증
        }

        [Test]
        public void CompleteBattleVictory_FinalBossEndsRun()
        {
            var run = new RunState(3); // 테스트 런 생성
            run.CurrentRound = RoundState.FinalRound; // 최종 스테이지 지정
            run.StartCurrentRound(); // FinalBoss 전투 진행 시작

            bool completed = RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory); // 최종 보스 승리 처리

            Assert.IsTrue(completed); // 최종 승리 결과 처리 성공 검증
            Assert.AreEqual(RunFlowPhase.Completed, run.CurrentFlowPhase); // 런 클리어 상태 검증
            Assert.IsTrue(run.Flow.IsRunFinished); // 종료 런 판정 검증
        }

        [Test]
        public void IntegratedRouteLoop_CanAdvanceFromStageOneToFinalBoss()
        {
            var run = new RunState(3); // 전체 루프 테스트 런 생성
            run.StartCurrentRound(); // Stage 1 전투 시작
            Assert.IsTrue(RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory)); // Stage 1 승리로 전체 지도 진입

            while (!run.Flow.IsRunFinished)
            {
                Assert.AreEqual(RunFlowPhase.Map, run.CurrentFlowPhase); // 각 스테이지 사이 지도 상태 검증
                StageNode target = run.RouteMap.GetSelectableNodes()[0]; // 현재 경로 첫 후보 선택
                Assert.IsTrue(run.RouteMap.TryMoveKingTo(target.NodeId)); // King 지도 이동
                StageDefinition definition = StageDefinitionCatalog.Resolve(target.StageDefinitionId, target.Depth); // 실제 StageDefinition 조회
                Assert.IsTrue(RunStageFlowService.TrySynchronizeSelectedStage(run, target, definition)); // 선택 노드 깊이 동기화
                Assert.AreEqual(target.Depth, run.CurrentRound); // 지도 깊이·런 스테이지 동기화 검증

                if (definition.RequiresBattle)
                {
                    run.StartCurrentRound(); // 전투 스테이지 실제 전투 상태 진입
                    Assert.IsTrue(RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory)); // 프로토타입 전투 승리 처리
                }
                else
                {
                    Assert.IsTrue(RunStageFlowService.EnterNonBattleStage(run, definition)); // 실제 Reward·Shop·Event 흐름 진입
                    Assert.IsTrue(RunStageFlowService.CompleteNonBattleStage(run)); // 비전투 스테이지 완료 처리
                }
            }

            Assert.AreEqual(RoundState.FinalRound, run.CurrentRound); // 최종 깊이 도달 검증
            Assert.AreEqual(RunFlowPhase.Completed, run.CurrentFlowPhase); // Stage 10 최종 보스 이후 런 완료 검증
        }

        private static StageNode FindFirstNonBattle(RunState run)
        {
            var selectable = run.RouteMap.GetSelectableNodes(); // 현재 선택 가능 노드 조회

            for (int i = 0; i < selectable.Count; i++)
            {
                StageNode node = selectable[i]; // 현재 후보 노드 조회
                StageDefinition definition = StageDefinitionCatalog.Resolve(node.StageDefinitionId, node.Depth); // 후보 타입 조회
                if (definition != null && !definition.RequiresBattle) return node; // 첫 비전투 노드 반환
            }

            return null; // 비전투 후보 없음 반환
        }

        private static RunFlowPhase ToExpectedPhase(StageType stageType)
        {
            if (stageType == StageType.Reward) return RunFlowPhase.Reward; // Reward 흐름 매핑
            if (stageType == StageType.Shop) return RunFlowPhase.Shop; // Shop 흐름 매핑
            return RunFlowPhase.Event; // Event 흐름 매핑
        }

        private static void AdvanceToDepth(RunState run, int targetDepth)
        {
            run.RouteMap.PreparePrototypeAfterBattle(1); // 전체 Route Graph 준비

            for (int depth = 2; depth <= targetDepth; depth++)
            {
                StageNode target = run.RouteMap.GetSelectableNodes()[0]; // 현재 다음 깊이 첫 노드 선택
                Assert.AreEqual(depth, target.Depth); // 예상 깊이 검증
                Assert.IsTrue(run.RouteMap.TryMoveKingTo(target.NodeId)); // 지도 King 이동
                run.CurrentRound = depth; // 실제 StageTransition의 깊이 동기화 재현

                if (depth < targetDepth)
                {
                    run.RouteMap.PreparePrototypeAfterBattle(depth); // 중간 깊이 완료 후 다음 선택 개방
                    run.Flow.EnterMap(); // 다음 지도 선택 상태 유지
                }
            }
        }
    }
}
