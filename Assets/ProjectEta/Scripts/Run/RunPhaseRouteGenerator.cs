using System.Collections.Generic; // Dictionary<T>·List<T>·IReadOnlyList<T> 사용

namespace ProjectEta.Run
{
    public static class RunPhaseRouteGenerator
    {
        public static IReadOnlyList<StageNode> CreateFullRoute(int mapSeed, int phase)
        {
            int safePhase = ClampPhase(phase); // 1~5 페이즈 범위 보정
            int phaseSeed = CreatePhaseSeed(mapSeed, safePhase); // 페이즈별 독립 경로 Seed 계산
            IReadOnlyList<StageNode> source = StageRouteGenerator.CreateFullRoute(phaseSeed); // 기존 1~10 경로 규칙 재사용
            var result = new List<StageNode>(source.Count); // 페이즈 전용 노드 결과 생성
            var idMap = new Dictionary<string, string>(); // 기존 ID→페이즈 ID 변환표 생성

            for (int i = 0; i < source.Count; i++)
            {
                StageNode sourceNode = source[i]; // 기존 생성 노드 조회
                if (sourceNode == null) continue; // 빈 노드 제외

                string nodeId = CreatePhaseNodeId(sourceNode.NodeId, safePhase); // 페이즈 충돌 없는 노드 ID 생성
                string definitionId = ResolveStageDefinitionId(sourceNode, safePhase); // 페이즈 마지막 보스 정의 보정
                var phaseNode = new StageNode(nodeId, sourceNode.Position, sourceNode.Depth, definitionId); // 페이즈 전용 노드 복제
                result.Add(phaseNode); // 결과 노드 등록
                idMap[sourceNode.NodeId] = nodeId; // 연결 변환용 ID 기록
            }

            int resultIndex = 0; // 결과 노드 연결 인덱스 초기화

            for (int i = 0; i < source.Count; i++)
            {
                StageNode sourceNode = source[i]; // 기존 연결 원본 조회
                if (sourceNode == null) continue; // 빈 노드 제외
                StageNode phaseNode = result[resultIndex++]; // 대응 페이즈 노드 조회
                var nextIds = new List<string>(sourceNode.NextNodeIds.Count); // 변환된 다음 노드 ID 목록 생성

                for (int nextIndex = 0; nextIndex < sourceNode.NextNodeIds.Count; nextIndex++)
                {
                    string sourceNextId = sourceNode.NextNodeIds[nextIndex]; // 기존 다음 노드 ID 조회
                    if (idMap.TryGetValue(sourceNextId, out string phaseNextId)) nextIds.Add(phaseNextId); // 페이즈 ID로 연결 변환
                }

                phaseNode.SetNextNodeIds(nextIds); // 변환된 전체 그래프 연결 적용
            }

            return result; // 새 페이즈 1~10 전체 경로 반환
        }

        private static string ResolveStageDefinitionId(StageNode sourceNode, int phase)
        {
            if (sourceNode.Depth == RoundState.FinalRound && phase < RunPhaseProgressService.TotalPhases)
            {
                return StageDefinitionCatalog.CreateDefinitionId(sourceNode.Depth, StageType.MidBoss); // 1~4페이즈 마지막 노드를 중간 보스로 처리
            }

            return sourceNode.StageDefinitionId; // 5페이즈 FinalBoss·기타 노드 기존 정의 유지
        }

        private static string CreatePhaseNodeId(string sourceNodeId, int phase)
        {
            if (phase <= RunPhaseProgressService.FirstPhase) return sourceNodeId; // 1페이즈 기존 저장 ID 호환 유지
            return $"phase_{phase}_{sourceNodeId}"; // 2~5페이즈 노드 ID 충돌 방지
        }

        private static int CreatePhaseSeed(int mapSeed, int phase)
        {
            if (phase <= RunPhaseProgressService.FirstPhase) return mapSeed; // 1페이즈 기존 경로 Seed 유지

            unchecked
            {
                int phaseSalt = (phase - 1) * 486187739; // 페이즈별 결정론적 Salt 생성
                return mapSeed ^ phaseSalt ^ 1597463007; // 같은 런에서도 다른 페이즈 경로 생성
            }
        }

        private static int ClampPhase(int phase)
        {
            if (phase < RunPhaseProgressService.FirstPhase) return RunPhaseProgressService.FirstPhase; // 최소 페이즈 보정
            if (phase > RunPhaseProgressService.TotalPhases) return RunPhaseProgressService.TotalPhases; // 최대 페이즈 보정
            return phase; // 정상 페이즈 반환
        }
    }
}
