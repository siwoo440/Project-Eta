using System.Collections.Generic; // 건물 계획 목록 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Run; // RouteMapState·StageType 사용

namespace ProjectEta.Board
{
    public readonly struct Day66RouteBuildingSpec
    {
        public string NodeId { get; } // 연결 노드 ID
        public Vector2Int Position { get; } // 보드 좌표
        public int Depth { get; } // 스테이지 깊이
        public StageType StageType { get; } // 건물 종류 기준 스테이지 타입

        public Day66RouteBuildingSpec(string nodeId, Vector2Int position, int depth, StageType stageType)
        {
            NodeId = nodeId ?? string.Empty; // 안전한 노드 ID 저장
            Position = position; // 보드 좌표 저장
            Depth = depth; // 깊이 저장
            StageType = stageType; // 스테이지 타입 저장
        }
    }

    public static class Day66RouteBuildingPlan
    {
        public static IReadOnlyList<Day66RouteBuildingSpec> Build(RouteMapState route)
        {
            var result = new List<Day66RouteBuildingSpec>(); // 건물 계획 결과 생성

            if (route == null) // 경로 누락 확인
            {
                return result; // 빈 결과 반환
            }

            for (int i = 0; i < route.Nodes.Count; i++) // 전체 RouteMap 노드 순회
            {
                StageNode node = route.Nodes[i]; // 현재 노드 조회

                if (node == null || string.IsNullOrWhiteSpace(node.NodeId)) // 잘못된 노드 확인
                {
                    continue; // 건물 생성 제외
                }

                StageType stageType = StageType.Battle; // 기본 일반 전투 타입 지정

                if (!StageDefinitionCatalog.TryParseStageType(node.StageDefinitionId, out stageType)) // StageDefinition 타입 파싱 확인
                {
                    stageType = StageType.Battle; // 손상 데이터 일반 성채 fallback 적용
                }

                result.Add(new Day66RouteBuildingSpec(node.NodeId, node.Position, node.Depth, stageType)); // Hierarchy와 무관한 건물 계획 추가
            }

            return result; // 전체 건물 계획 반환
        }
    }
}
