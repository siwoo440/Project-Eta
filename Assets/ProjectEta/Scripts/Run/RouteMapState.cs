using System; // StringComparison 사용
using System.Collections.Generic; // List<T>·IEnumerable<T>·IReadOnlyList<T> 사용
using UnityEngine; // Vector2Int·Mathf 사용

namespace ProjectEta.Run // 런 경로 지도 상태 네임스페이스
{
    public sealed class RouteMapState // 동일 10×10 체스판을 경로 지도로 사용할 런타임 상태
    {
        private const int PrototypeCenterX = 4; // 10×10 보드 중앙 기준 X
        private readonly List<StageNode> _nodes = new List<StageNode>(); // 현재 경로 지도 노드 목록

        public int CurrentDepth { get; private set; } // 현재 완료한 스테이지 깊이
        public string CurrentNodeId { get; private set; } // 현재 킹이 위치한 노드 ID
        public Vector2Int KingMapPosition { get; private set; } // 지도 모드 킹 좌표
        public IReadOnlyList<StageNode> Nodes => _nodes; // 전체 노드 읽기 전용 공개
        public StageNode CurrentNode => FindNode(CurrentNodeId); // 현재 노드 조회
        public bool HasPreparedRoute => _nodes.Count > 0 && CurrentNode != null; // 경로 준비 여부

        public RouteMapState() // 새 경로 지도 상태 생성
        {
            Clear(); // 기본값 초기화
        }

        public void Clear() // 현재 경로 지도 상태 비우기
        {
            _nodes.Clear(); // 노드 목록 제거
            CurrentDepth = RoundState.FirstRound; // 기본 깊이 복구
            CurrentNodeId = string.Empty; // 현재 노드 제거
            KingMapPosition = new Vector2Int(PrototypeCenterX, 0); // 기본 킹 좌표 지정
        }

        public void Configure(int currentDepth, StageNode currentNode, IEnumerable<StageNode> otherNodes) // 외부 경로 데이터로 상태 구성
        {
            Clear(); // 기존 경로 제거
            CurrentDepth = Mathf.Clamp(currentDepth, RoundState.FirstRound, RoundState.FinalRound); // 깊이 범위 보정

            if (currentNode == null) return; // 현재 노드 누락 처리

            AddNodeIfUnique(currentNode); // 현재 노드 등록

            if (otherNodes != null) // 추가 노드 목록 확인
            {
                foreach (var node in otherNodes) // 추가 노드 순회
                {
                    AddNodeIfUnique(node); // 중복 없이 등록
                }
            }

            CurrentNodeId = currentNode.NodeId; // 현재 노드 ID 저장
            KingMapPosition = currentNode.Position; // 킹 지도 좌표 동기화
            currentNode.MarkVisited(); // 현재 노드 방문 처리
        }

        public void PreparePrototypeAfterBattle(int clearedDepth) // 43일차 상태 검증용 다음 스테이지 후보 준비
        {
            int safeDepth = Mathf.Clamp(clearedDepth, RoundState.FirstRound, RoundState.FinalRound - 1); // 최종 스테이지 전 깊이 보정
            int nextDepth = safeDepth + 1; // 다음 선택 스테이지 깊이 계산
            int currentY = safeDepth - 1; // 완료 스테이지 지도 Y 계산
            int nextY = nextDepth - 1; // 다음 스테이지 지도 Y 계산

            var current = new StageNode($"depth_{safeDepth}_resolved", new Vector2Int(PrototypeCenterX, currentY), safeDepth, "ResolvedStage"); // 현재 완료 노드 생성
            var left = new StageNode($"depth_{nextDepth}_left", new Vector2Int(PrototypeCenterX - 1, nextY), nextDepth, "PrototypeBattle"); // 왼쪽 후보 생성
            var center = new StageNode($"depth_{nextDepth}_center", new Vector2Int(PrototypeCenterX, nextY), nextDepth, "PrototypeBattle"); // 중앙 후보 생성
            var right = new StageNode($"depth_{nextDepth}_right", new Vector2Int(PrototypeCenterX + 1, nextY), nextDepth, "PrototypeBattle"); // 오른쪽 후보 생성

            current.SetNextNodeIds(new[] { left.NodeId, center.NodeId, right.NodeId }); // 현재 노드에서 다음 3개 노드 연결
            Configure(safeDepth, current, new[] { left, center, right }); // 프로토타입 경로 상태 적용
        }

        public IReadOnlyList<StageNode> GetSelectableNodes() // 현재 위치에서 선택 가능한 다음 스테이지 반환
        {
            var result = new List<StageNode>(); // 선택 가능 결과 목록 생성
            var current = CurrentNode; // 현재 노드 조회
            if (current == null) return result; // 현재 노드 없으면 빈 목록 반환

            for (int i = 0; i < current.NextNodeIds.Count; i++) // 연결된 다음 노드 ID 순회
            {
                var node = FindNode(current.NextNodeIds[i]); // 실제 노드 조회
                if (node != null) result.Add(node); // 존재 노드만 후보 추가
            }

            return result; // 선택 가능 노드 반환
        }

        public StageNode FindNode(string nodeId) // 노드 ID로 경로 노드 조회
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null; // 빈 ID 제외

            for (int i = 0; i < _nodes.Count; i++) // 전체 노드 순회
            {
                var node = _nodes[i]; // 현재 노드 조회
                if (node == null) continue; // 빈 노드 제외
                if (string.Equals(node.NodeId, nodeId, StringComparison.Ordinal)) return node; // 동일 ID 노드 반환
            }

            return null; // 일치 노드 없음
        }

        private void AddNodeIfUnique(StageNode node) // 중복 없는 노드 등록
        {
            if (node == null || string.IsNullOrWhiteSpace(node.NodeId)) return; // 잘못된 노드 제외
            if (FindNode(node.NodeId) != null) return; // 동일 ID 중복 제외
            _nodes.Add(node); // 새 노드 등록
        }
    }
}
