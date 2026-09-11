using ProjectEta.Battle; // BattleOutcome 사용

namespace ProjectEta.Run
{
    public static class RunStageFlowService
    {
        public static bool TrySynchronizeSelectedStage(RunState runState, StageNode stageNode, StageDefinition definition)
        {
            if (runState == null || stageNode == null || definition == null) return false; // 필수 런·노드·정의 누락 차단
            if (runState.Flow.IsRunFinished) return false; // 종료 런의 추가 스테이지 진입 차단
            if (!runState.RouteMap.HasSelectedNode) return false; // 지도에서 실제 선택되지 않은 진입 차단
            if (!string.Equals(runState.RouteMap.SelectedNodeId, stageNode.NodeId, System.StringComparison.Ordinal)) return false; // 선택 노드 ID 불일치 차단
            if (!string.Equals(runState.RouteMap.CurrentNodeId, stageNode.NodeId, System.StringComparison.Ordinal)) return false; // 지도 King 현재 노드 불일치 차단
            if (runState.RouteMap.CurrentDepth != stageNode.Depth) return false; // RouteMap 깊이와 StageNode 깊이 불일치 차단
            if (stageNode.Depth < RoundState.FirstRound || stageNode.Depth > RoundState.FinalRound) return false; // 1~10 범위 밖 스테이지 차단

            runState.CurrentRound = stageNode.Depth; // StageNode.Depth를 런 스테이지 번호의 단일 기준으로 동기화
            return true; // 선택 스테이지 동기화 성공 반환
        }

        public static bool EnterNonBattleStage(RunState runState, StageDefinition definition)
        {
            if (runState == null || definition == null) return false; // 필수 런·정의 누락 차단
            if (runState.Flow.IsRunFinished || definition.RequiresBattle) return false; // 종료 런·전투형 정의 차단
            if (!runState.RouteMap.HasSelectedNode) return false; // 선택 노드 없는 비전투 진입 차단
            if (runState.CurrentRound != runState.RouteMap.CurrentDepth) return false; // 지도 깊이·현재 스테이지 불일치 차단

            runState.Round.Begin(); // Reward·Shop·Event 진행 중 상태 시작

            if (definition.StageType == StageType.Reward)
            {
                runState.Flow.EnterReward(); // 카드 보상 흐름 진입
                return true; // Reward 진입 성공 반환
            }

            if (definition.StageType == StageType.Shop)
            {
                runState.Flow.EnterShop(); // 상점 흐름 진입
                return true; // Shop 진입 성공 반환
            }

            if (definition.StageType == StageType.Event)
            {
                runState.Flow.EnterEvent(); // 이벤트 흐름 진입
                return true; // Event 진입 성공 반환
            }

            return false; // 지원하지 않는 비전투 타입 차단
        }

        public static bool CompleteNonBattleStage(RunState runState)
        {
            if (runState == null || runState.Flow.IsRunFinished) return false; // 런 누락·이미 종료된 런 차단
            if (!IsNonBattleFlow(runState.CurrentFlowPhase)) return false; // Reward·Shop·Event 외 완료 차단
            if (runState.CurrentRound != runState.RouteMap.CurrentDepth) return false; // 현재 스테이지·지도 깊이 불일치 차단

            runState.Round.Restore(runState.CurrentRound, RoundProgressStatus.Cleared, BattleOutcome.Victory); // 비전투 스테이지를 승리 완료 상태로 기록

            if (runState.CurrentRound >= RoundState.FinalRound)
            {
                if (RunPhaseProgressService.TryAdvanceToNextPhase(runState)) return true; // 1~4페이즈 마지막 깊이는 다음 페이즈로 직접 전환

                runState.Flow.CompleteRun(); // 5페이즈 마지막 깊이만 최종 런 완료 처리
                return true; // 최종 비전투 완료 성공 반환
            }

            runState.RouteMap.PreparePrototypeAfterBattle(runState.CurrentRound); // 전체 Route Graph를 유지하며 현재 스테이지 완료·다음 선택 개방
            runState.Flow.EnterMap(); // 다음 Stage 선택 지도 흐름 복귀
            RunPhaseProgressService.TryNormalizeFirstPhaseRoute(runState); // 첫 스테이지 뒤 기존 단일판 경로를 1페이즈 규칙으로 정규화
            return true; // 비전투 완료 성공 반환
        }

        public static bool CompleteBattle(RunState runState, BattleOutcome outcome)
        {
            if (runState == null || runState.CurrentFlowPhase != RunFlowPhase.Battle) return false; // 런 누락·전투 외 결과 처리 차단
            if (outcome == BattleOutcome.None) return false; // 미결정 전투 결과 차단

            runState.RecordBattleOutcome(outcome); // 전투 결과를 라운드 상태에 먼저 기록

            if (outcome == BattleOutcome.Defeat)
            {
                runState.Flow.FailRun(); // 패배는 즉시 런 실패 상태로 전환
                return true; // 패배 처리 성공 반환
            }

            if (runState.CurrentRound >= RoundState.FinalRound)
            {
                if (RunPhaseProgressService.TryAdvanceToNextPhase(runState)) return true; // 1~4페이즈 마지막 승리는 Completed 없이 다음 페이즈로 직접 전환

                runState.Flow.CompleteRun(); // 5페이즈 마지막 승리만 최종 런 완료 처리
                return true; // 최종 승리 처리 성공 반환
            }

            runState.RouteMap.PreparePrototypeAfterBattle(runState.CurrentRound); // 현재 스테이지 완료 후 다음 깊이 선택 상태 준비
            runState.Flow.EnterMap(); // 동일 체스판을 경로 지도 모드로 전환
            RunPhaseProgressService.TryNormalizeFirstPhaseRoute(runState); // 첫 승리 뒤 기존 단일판 경로를 1페이즈 규칙으로 정규화
            return true; // 일반 전투 승리 처리 성공 반환
        }

        public static bool CompleteBattleReward(RunState runState)
        {
            if (runState == null || runState.Flow.IsRunFinished) return false; // 런 누락·종료 런 차단
            if (runState.CurrentFlowPhase != RunFlowPhase.Reward) return false; // 전투 카드 보상 외 흐름 차단
            if (runState.CurrentRoundStatus != RoundProgressStatus.Cleared) return false; // 아직 완료되지 않은 전투의 보상 복귀 차단
            if (!runState.RouteMap.HasPreparedRoute || runState.RouteMap.HasSelectedNode) return false; // 승리 후 다음 경로 준비 상태 검증

            runState.Flow.EnterMap(); // 이미 완료된 전투를 다시 완료 처리하지 않고 지도만 복귀
            return true; // 전투 승리 카드 보상 완료 성공 반환
        }

        public static bool IsNonBattleFlow(RunFlowPhase phase)
        {
            return phase == RunFlowPhase.Reward || phase == RunFlowPhase.Shop || phase == RunFlowPhase.Event; // 비전투 진행 흐름 판정
        }
    }
}
