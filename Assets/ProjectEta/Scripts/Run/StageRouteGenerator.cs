using System.Collections.Generic; // List<T>·HashSet<T> 사용
using UnityEngine; // Vector2Int·Mathf 사용
using ProjectEta.Board; // BoardState 크기 상수 사용

namespace ProjectEta.Run // 경로 지도 생성 네임스페이스
{
    public static class StageRouteGenerator // 45일차 현재 깊이에서 다음 한 층의 StageNode를 생성하는 프로토타입 생성기
    {
        private static readonly StageType[] OptionalStageTypes = // 일반 전투 외 분기용 스테이지 타입 순환표
        {
            StageType.Elite, // 강화 전투 후보
            StageType.Reward, // 카드 보상 후보
            StageType.Shop, // 상점 후보
            StageType.Event // 이벤트 후보
        };

        public static IReadOnlyList<StageNode> CreateNextNodes(int clearedDepth, Vector2Int currentPosition) // 현재 완료 깊이·킹 위치 기준 다음 노드 생성
        {
            int safeDepth = Mathf.Clamp(clearedDepth, RoundState.FirstRound, RoundState.FinalRound - 1); // 1~9 완료 깊이 보정
            int nextDepth = safeDepth + 1; // 다음 스테이지 깊이 계산
            int nextY = nextDepth - 1; // 깊이를 10×10 보드 Y 좌표로 변환

            if (nextDepth == 5) return CreateForcedBossNode(nextDepth, currentPosition.x, nextY, StageType.MidBoss); // 5단계 중간 보스 강제
            if (nextDepth == 10) return CreateForcedBossNode(nextDepth, currentPosition.x, nextY, StageType.FinalBoss); // 10단계 최종 보스 강제

            int branchCount = nextDepth % 2 == 0 ? 3 : 2; // 짝수 깊이 3분기·홀수 깊이 2분기로 2~3개 규칙 검증
            var xPositions = BuildAdjacentXPositions(currentPosition.x, branchCount); // 현재 킹에서 1칸 안쪽의 고유 X 좌표 생성
            var nodes = new List<StageNode>(xPositions.Count); // 다음 깊이 노드 목록 생성

            for (int i = 0; i < xPositions.Count; i++) // 분기 위치 순회
            {
                StageType stageType = i == 0 ? StageType.Battle : OptionalStageTypes[(nextDepth + i - 1) % OptionalStageTypes.Length]; // 최소 1개 일반 전투와 나머지 변화 노드 구성
                string definitionId = StageDefinitionCatalog.CreateDefinitionId(nextDepth, stageType); // 타입 기반 StageDefinition ID 생성
                string nodeId = $"depth_{nextDepth}_{i}_{stageType.ToString().ToLowerInvariant()}"; // 지도에서 고유한 노드 ID 생성
                nodes.Add(new StageNode(nodeId, new Vector2Int(xPositions[i], nextY), nextDepth, definitionId)); // 실제 StageNode 추가
            }

            return nodes; // 완성된 다음 깊이 분기 반환
        }

        private static IReadOnlyList<StageNode> CreateForcedBossNode(int depth, int currentX, int y, StageType stageType) // 보스 깊이 단일 강제 노드 생성
        {
            int safeX = Mathf.Clamp(currentX, 0, BoardState.Width - 1); // 현재 킹 X를 보드 범위로 보정
            string definitionId = StageDefinitionCatalog.CreateDefinitionId(depth, stageType); // 보스 StageDefinition ID 생성
            string nodeId = $"depth_{depth}_boss_{stageType.ToString().ToLowerInvariant()}"; // 보스 노드 ID 생성
            return new[] { new StageNode(nodeId, new Vector2Int(safeX, y), depth, definitionId) }; // 바로 앞 1칸에 강제 보스 노드 반환
        }

        private static List<int> BuildAdjacentXPositions(int currentX, int branchCount) // 현재 킹 기준 좌·중·우 1칸에서 고유 분기 X 생성
        {
            int safeCurrentX = Mathf.Clamp(currentX, 0, BoardState.Width - 1); // 현재 X 보드 범위 보정
            int[] preferredOffsets = branchCount >= 3 ? new[] { -1, 0, 1 } : new[] { -1, 1 }; // 분기 수에 따른 기본 오프셋 구성
            var result = new List<int>(branchCount); // 결과 X 목록 생성
            var used = new HashSet<int>(); // 경계 보정 중 중복 X 방지

            for (int i = 0; i < preferredOffsets.Length; i++) // 기본 오프셋 순회
            {
                int x = Mathf.Clamp(safeCurrentX + preferredOffsets[i], 0, BoardState.Width - 1); // 보드 경계 안으로 보정
                if (used.Add(x)) result.Add(x); // 새로운 X만 분기 목록에 추가
            }

            for (int offset = -1; result.Count < branchCount && offset <= 1; offset++) // 경계에서 중복된 경우 인접 칸으로 보충
            {
                int x = Mathf.Clamp(safeCurrentX + offset, 0, BoardState.Width - 1); // 보충 후보 X 보정
                if (used.Add(x)) result.Add(x); // 미사용 인접 X 추가
            }

            return result; // 최대 2~3개의 고유 인접 X 반환
        }
    }
}
