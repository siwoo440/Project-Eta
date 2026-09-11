using System; // StringComparison·Int32 사용
using System.Collections.Generic; // List<T>·IReadOnlyList<T> 사용
using ProjectEta.Battle; // BattleOutcome 사용

namespace ProjectEta.Run
{
    public static class RunPhaseProgressService
    {
        public const int FirstPhase = 1; // 첫 RouteMap 페이즈
        public const int TotalPhases = 5; // 전체 RouteMap 페이즈 수
        private const string PhaseNodePrefix = "phase_"; // 2~5페이즈 저장 가능한 노드 ID 접두사

        public static int GetCurrentPhase(RunState runState)
        {
            if (runState == null || runState.RouteMap == null) return FirstPhase; // 런 누락 시 1페이즈 fallback

            if (TryParsePhase(runState.RouteMap.CurrentNodeId, out int currentPhase)) return currentPhase; // 현재 킹 노드에서 페이즈 복원
            if (TryParsePhase(runState.RouteMap.SelectedNodeId, out int selectedPhase)) return selectedPhase; // 선택 중 노드에서 페이즈 복원
            return FirstPhase; // 구버전·1페이즈 무접두 ID 호환
        }

        public static bool TryNormalizeFirstPhaseRoute(RunState runState)
        {
            if (!ShouldNormalizeFirstPhaseRoute(runState)) return false; // 정규화 대상이 아닌 지도 제외
            return RebuildAtPhaseStart(runState, FirstPhase, false); // 첫 페이즈 전체 경로를 MidBoss 규칙으로 재구성
        }

        public static bool TryAdvanceToNextPhase(RunState runState)
        {
            if (runState == null) return false; // 런 상태 누락 차단
            if (runState.CurrentRound < RoundState.FinalRound) return false; // 현재 페이즈 마지막 깊이 이전 전환 차단
            if (runState.CurrentRoundStatus != RoundProgressStatus.Cleared) return false; // 완료되지 않은 마지막 스테이지 전환 차단
            if (runState.LastBattleOutcome != BattleOutcome.Victory) return false; // 승리 외 결과의 페이즈 전환 차단

            StageNode currentNode = runState.RouteMap.CurrentNode; // 현재 마지막 지도 노드 조회
            if (currentNode == null) return false; // 지도 노드 없는 기존 FinalBoss 직접 진행은 최종 완료 유지
            if (!StageDefinitionCatalog.TryParseStageType(currentNode.StageDefinitionId, out StageType stageType)) return false; // 현재 스테이지 타입 복원 실패 차단
            if (stageType != StageType.MidBoss) return false; // 중간 보스 외 FinalBoss·기타 스테이지의 다음 페이즈 전환 차단

            int currentPhase = GetCurrentPhase(runState); // 현재 RouteMap 페이즈 조회
            if (currentPhase >= TotalPhases) return false; // 5페이즈 이후 추가 페이즈 생성 차단

            return RebuildAtPhaseStart(runState, currentPhase + 1, true); // 다음 페이즈 전체 경로 생성·시작점 복귀
        }

        public static bool HandleBattleCompleted(RunState runState, BattleOutcome outcome)
        {
            if (runState == null || outcome != BattleOutcome.Victory) return false; // 승리 외 기존 호출 호환 차단
            if (TryNormalizeFirstPhaseRoute(runState)) return true; // 기존 첫 전투 경로 정규화 호출 호환
            return TryAdvanceToNextPhase(runState); // 기존 마지막 스테이지 전환 호출 호환
        }

        public static bool TryParsePhase(string nodeId, out int phase)
        {
            phase = FirstPhase; // 구버전 기본 페이즈 지정
            if (string.IsNullOrWhiteSpace(nodeId)) return false; // 빈 노드 ID 제외
            if (!nodeId.StartsWith(PhaseNodePrefix, StringComparison.Ordinal)) return false; // 페이즈 접두사 없는 1페이즈·구버전 처리

            int numberStart = PhaseNodePrefix.Length; // 숫자 토큰 시작 위치 계산
            int separatorIndex = nodeId.IndexOf('_', numberStart); // 페이즈 숫자 뒤 구분자 검색
            if (separatorIndex <= numberStart) return false; // 잘못된 접두 형식 차단

            string phaseToken = nodeId.Substring(numberStart, separatorIndex - numberStart); // 페이즈 숫자 토큰 추출
            if (!int.TryParse(phaseToken, out int parsed)) return false; // 숫자 변환 실패 차단
            if (parsed < FirstPhase || parsed > TotalPhases) return false; // 1~5 범위 밖 값 차단

            phase = parsed; // 정상 페이즈 반환
            return true; // 파싱 성공 반환
        }

        private static bool ShouldNormalizeFirstPhaseRoute(RunState runState)
        {
            if (runState == null) return false; // 런 상태 누락 차단
            if (runState.CurrentFlowPhase != RunFlowPhase.Map) return false; // 첫 전투 후 지도 상태만 정규화
            if (runState.CurrentRound != RoundState.FirstRound) return false; // 1단계 전투 완료 직후만 정규화
            if (!runState.RouteMap.HasCompleteRoute) return false; // 기존 전체 그래프 생성 전 차단
            if (TryParsePhase(runState.RouteMap.CurrentNodeId, out _)) return false; // 2~5페이즈 경로 중복 정규화 차단

            StageNode finalNode = FindFirstDepth(runState.RouteMap.Nodes, RoundState.FinalRound); // 현재 10번째 노드 조회
            if (finalNode == null) return false; // 손상 경로 차단
            if (!StageDefinitionCatalog.TryParseStageType(finalNode.StageDefinitionId, out StageType stageType)) return true; // 정의 손상 경로 정규화 허용
            return stageType == StageType.FinalBoss; // 기존 단일판 FinalBoss 그래프만 1페이즈 규칙으로 교체
        }

        private static bool RebuildAtPhaseStart(RunState runState, int phase, bool resetRound)
        {
            IReadOnlyList<StageNode> phaseNodes = RunPhaseRouteGenerator.CreateFullRoute(runState.RouteMap.MapSeed, phase); // 새 페이즈 전체 그래프 생성
            StageNode root = FindFirstDepth(phaseNodes, RoundState.FirstRound); // 새 페이즈 시작점 조회
            if (root == null) return false; // 시작점 누락 시 기존 상태 유지

            var otherNodes = new List<StageNode>(phaseNodes.Count - 1); // 시작점을 제외한 전체 노드 목록 생성

            for (int i = 0; i < phaseNodes.Count; i++)
            {
                StageNode node = phaseNodes[i]; // 현재 페이즈 노드 조회
                if (node == null || object.ReferenceEquals(node, root)) continue; // 시작점·빈 노드 제외
                otherNodes.Add(node); // RouteMap 구성용 나머지 노드 등록
            }

            runState.RouteMap.Configure(RoundState.FirstRound, root, otherNodes); // 킹을 첫 지점으로 되돌리고 새 그래프 적용

            if (resetRound)
            {
                runState.Round.Restore(RoundState.FirstRound, RoundProgressStatus.Cleared, BattleOutcome.Victory); // 새 페이즈 시작점을 완료 체크포인트로 동기화
                runState.Flow.EnterMap(); // 새 노드 선택 가능한 지도 흐름 재개
            }

            return true; // 페이즈 그래프 교체 성공 반환
        }

        private static StageNode FindFirstDepth(IReadOnlyList<StageNode> nodes, int depth)
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
