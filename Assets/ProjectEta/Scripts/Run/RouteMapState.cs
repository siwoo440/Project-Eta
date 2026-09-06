using System; // Guid·StringComparison 사용
using System.Collections.Generic; // List<T>·IEnumerable<T>·IReadOnlyList<T> 사용
using UnityEngine; // Vector2Int·Mathf 사용

namespace ProjectEta.Run
{
    public sealed class RouteMapState
    {
        private const int PrototypeCenterX = 4; // 10×10 보드 중앙 기준 X
        private readonly List<StageNode> _nodes = new List<StageNode>(); // 현재 경로 지도 노드 목록
        private readonly List<string> _selectedPathNodeIds = new List<string>(); // 런 동안 실제 선택한 경로 기록
        private readonly List<string> _visitedNodeIds = new List<string>(); // 런 동안 방문한 노드 이력

        public int MapSeed { get; private set; } // 이후 절차적 전체 경로 재생성을 위한 런 시드
        public int CurrentDepth { get; private set; } // 현재 킹이 위치한 지도 깊이
        public string CurrentNodeId { get; private set; } // 현재 킹이 위치한 노드 ID
        public string SelectedNodeId { get; private set; } // 이번 지도 단계에서 선택한 다음 스테이지 노드 ID
        public Vector2Int KingMapPosition { get; private set; } // 지도 모드 킹 좌표
        public IReadOnlyList<StageNode> Nodes => _nodes; // 전체 노드 읽기 전용 공개
        public IReadOnlyList<string> SelectedPathNodeIds => _selectedPathNodeIds; // 지금까지 선택한 경로 읽기 전용 공개
        public IReadOnlyList<string> VisitedNodeIds => _visitedNodeIds; // 지금까지 방문한 노드 읽기 전용 공개
        public StageNode CurrentNode => FindNode(CurrentNodeId); // 현재 노드 조회
        public StageNode SelectedNode => FindNode(SelectedNodeId); // 선택 스테이지 노드 조회
        public bool HasPreparedRoute => _nodes.Count > 0 && CurrentNode != null; // 경로 준비 여부
        public bool HasSelectedNode => !string.IsNullOrWhiteSpace(SelectedNodeId); // 다음 스테이지 선택 완료 여부

        public RouteMapState()
        {
            MapSeed = Guid.NewGuid().GetHashCode(); // 새 런 고유 경로 시드 생성
            Clear(); // 기본값 초기화
        }

        public void Clear()
        {
            _nodes.Clear(); // 노드 목록 제거
            _selectedPathNodeIds.Clear(); // 선택 경로 기록 제거
            _visitedNodeIds.Clear(); // 방문 노드 이력 제거
            CurrentDepth = RoundState.FirstRound; // 기본 깊이 복구
            CurrentNodeId = string.Empty; // 현재 노드 제거
            SelectedNodeId = string.Empty; // 선택 노드 제거
            KingMapPosition = new Vector2Int(PrototypeCenterX, 0); // 기본 킹 좌표 지정
        }

        public void Configure(int currentDepth, StageNode currentNode, IEnumerable<StageNode> otherNodes)
        {
            _nodes.Clear(); // 현재 깊이 노드만 교체하고 과거 선택 경로는 유지
            CurrentNodeId = string.Empty; // 현재 노드 ID 초기화
            SelectedNodeId = string.Empty; // 선택 노드 ID 초기화
            CurrentDepth = Mathf.Clamp(currentDepth, RoundState.FirstRound, RoundState.FinalRound); // 깊이 범위 보정

            if (currentNode == null) return; // 현재 노드 누락 처리

            AddNodeIfUnique(currentNode); // 현재 노드 등록

            if (otherNodes != null)
            {
                foreach (var node in otherNodes)
                {
                    AddNodeIfUnique(node); // 중복 없이 추가 노드 등록
                }
            }

            CurrentNodeId = currentNode.NodeId; // 현재 노드 ID 저장
            KingMapPosition = currentNode.Position; // 킹 지도 좌표 동기화
            currentNode.MarkVisited(); // 현재 노드 방문 처리
            RecordVisitedNode(currentNode.NodeId); // 현재 노드 방문 이력 기록
        }

        public void PreparePrototypeAfterBattle(int clearedDepth)
        {
            int safeDepth = Mathf.Clamp(clearedDepth, RoundState.FirstRound, RoundState.FinalRound - 1); // 최종 스테이지 전 깊이 보정
            Vector2Int currentPosition = ResolveCurrentPositionForDepth(safeDepth); // 이전 선택 경로 X 유지
            var current = new StageNode($"depth_{safeDepth}_resolved", currentPosition, safeDepth, "ResolvedStage"); // 현재 완료 노드 생성
            var nextNodes = StageRouteGenerator.CreateNextNodes(safeDepth, currentPosition); // 다음 분기 생성
            var nextIds = new List<string>(nextNodes.Count); // 그래프 연결 ID 목록 생성

            for (int i = 0; i < nextNodes.Count; i++)
            {
                nextIds.Add(nextNodes[i].NodeId); // 다음 노드 연결 ID 추가
            }

            current.SetNextNodeIds(nextIds); // 현재 노드에서 다음 분기 연결
            Configure(safeDepth, current, nextNodes); // 새 지도 상태 적용
        }

        public IReadOnlyList<StageNode> GetSelectableNodes()
        {
            var result = new List<StageNode>(); // 선택 가능 결과 목록 생성
            var current = CurrentNode; // 현재 노드 조회
            if (current == null || HasSelectedNode) return result; // 현재 노드 누락·이미 선택 완료 차단

            for (int i = 0; i < current.NextNodeIds.Count; i++)
            {
                var node = FindNode(current.NextNodeIds[i]); // 실제 노드 조회
                if (node != null && IsKingStep(CurrentNode.Position, node.Position)) result.Add(node); // 연결·킹 1칸 조건 후보 추가
            }

            return result; // 선택 가능 노드 반환
        }

        public bool CanMoveTo(StageNode targetNode)
        {
            if (targetNode == null || CurrentNode == null || HasSelectedNode) return false; // 대상·현재 노드 누락·이미 선택 완료 차단
            if (!ContainsConnection(CurrentNode, targetNode.NodeId)) return false; // 그래프 연결 없는 노드 차단
            return IsKingStep(KingMapPosition, targetNode.Position); // 킹 인접 이동 규칙 반환
        }

        public bool TryMoveKingTo(string nodeId)
        {
            var targetNode = FindNode(nodeId); // 대상 노드 조회
            if (!CanMoveTo(targetNode)) return false; // 유효하지 않은 이동 차단

            KingMapPosition = targetNode.Position; // 킹 지도 좌표 이동
            CurrentNodeId = targetNode.NodeId; // 현재 노드를 대상 노드로 변경
            SelectedNodeId = targetNode.NodeId; // 선택 스테이지 기록
            CurrentDepth = targetNode.Depth; // 지도 깊이 갱신
            targetNode.MarkVisited(); // 도착 노드 방문 처리
            RecordVisitedNode(targetNode.NodeId); // 도착 노드 방문 이력 기록
            RecordSelectedPath(targetNode.NodeId); // 실제 선택 경로를 런 전체 기록에 추가
            return true; // 이동 성공 반환
        }

        public RouteMapSaveData ToSaveData()
        {
            var data = new RouteMapSaveData
            {
                mapSeed = MapSeed, // 런 경로 시드 기록
                currentDepth = CurrentDepth, // 현재 깊이 기록
                currentNodeId = CurrentNodeId, // 현재 노드 ID 기록
                selectedNodeId = SelectedNodeId, // 선택 노드 ID 기록
                kingX = KingMapPosition.x, // 지도 킹 X 기록
                kingY = KingMapPosition.y // 지도 킹 Y 기록
            };

            for (int i = 0; i < _selectedPathNodeIds.Count; i++)
            {
                data.selectedPathNodeIds.Add(_selectedPathNodeIds[i]); // 과거 선택 경로 기록
            }

            for (int i = 0; i < _visitedNodeIds.Count; i++)
            {
                data.visitedNodeIds.Add(_visitedNodeIds[i]); // 과거 방문 노드 이력 기록
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                StageNode node = _nodes[i]; // 현재 지도 노드 조회
                if (node == null) continue; // 빈 노드 제외

                var nodeData = new RouteNodeSaveData
                {
                    nodeId = node.NodeId, // 노드 ID 기록
                    x = node.Position.x, // 노드 X 기록
                    y = node.Position.y, // 노드 Y 기록
                    depth = node.Depth, // 노드 깊이 기록
                    stageDefinitionId = node.StageDefinitionId, // 스테이지 정의 ID 기록
                    visited = node.Visited // 방문 여부 기록
                };

                for (int nextIndex = 0; nextIndex < node.NextNodeIds.Count; nextIndex++)
                {
                    nodeData.nextNodeIds.Add(node.NextNodeIds[nextIndex]); // 그래프 연결 기록
                }

                data.nodes.Add(nodeData); // 현재 노드 저장 DTO 추가
            }

            return data; // 경로 지도 저장 DTO 반환
        }

        public void Restore(RouteMapSaveData data)
        {
            Clear(); // 기존 지도·선택 경로 제거
            if (data == null) return; // 저장 데이터 누락 시 기본 지도 유지

            MapSeed = data.mapSeed; // 저장 경로 시드 복원
            CurrentDepth = Mathf.Clamp(data.currentDepth <= 0 ? RoundState.FirstRound : data.currentDepth, RoundState.FirstRound, RoundState.FinalRound); // 깊이 복원
            KingMapPosition = new Vector2Int(data.kingX, data.kingY); // 지도 킹 좌표 복원

            if (data.selectedPathNodeIds != null)
            {
                for (int i = 0; i < data.selectedPathNodeIds.Count; i++)
                {
                    RecordSelectedPath(data.selectedPathNodeIds[i]); // 선택 경로 순서 복원
                }
            }

            if (data.visitedNodeIds != null)
            {
                for (int i = 0; i < data.visitedNodeIds.Count; i++)
                {
                    RecordVisitedNode(data.visitedNodeIds[i]); // 방문 노드 이력 복원
                }
            }

            if (data.nodes != null)
            {
                for (int i = 0; i < data.nodes.Count; i++)
                {
                    RouteNodeSaveData nodeData = data.nodes[i]; // 저장 노드 조회
                    if (nodeData == null || string.IsNullOrWhiteSpace(nodeData.nodeId)) continue; // 잘못된 노드 제외

                    var node = new StageNode(
                        nodeData.nodeId,
                        new Vector2Int(nodeData.x, nodeData.y),
                        nodeData.depth,
                        nodeData.stageDefinitionId); // 런타임 노드 재생성

                    node.SetNextNodeIds(nodeData.nextNodeIds); // 그래프 연결 복원
                    if (nodeData.visited) node.MarkVisited(); // 방문 상태 복원
                    AddNodeIfUnique(node); // 지도 노드 등록
                }
            }

            CurrentNodeId = FindNode(data.currentNodeId) != null ? data.currentNodeId : string.Empty; // 유효한 현재 노드 ID만 복원
            SelectedNodeId = FindNode(data.selectedNodeId) != null ? data.selectedNodeId : string.Empty; // 유효한 선택 노드 ID만 복원

            if (string.IsNullOrWhiteSpace(CurrentNodeId) && _nodes.Count > 0)
            {
                CurrentNodeId = _nodes[0].NodeId; // 손상 저장 데이터는 첫 노드로 안전 보정
                KingMapPosition = _nodes[0].Position; // 보정된 현재 노드 좌표 적용
            }
        }

        public StageNode FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null; // 빈 ID 제외

            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i]; // 현재 노드 조회
                if (node == null) continue; // 빈 노드 제외
                if (string.Equals(node.NodeId, nodeId, StringComparison.Ordinal)) return node; // 동일 ID 노드 반환
            }

            return null; // 일치 노드 없음
        }

        public static bool IsKingStep(Vector2Int from, Vector2Int to)
        {
            int deltaX = Mathf.Abs(to.x - from.x); // 가로 이동 거리 계산
            int deltaY = Mathf.Abs(to.y - from.y); // 세로 이동 거리 계산
            if (deltaX == 0 && deltaY == 0) return false; // 제자리 이동 차단
            return deltaX <= 1 && deltaY <= 1; // 직선·대각선 1칸 허용
        }

        private Vector2Int ResolveCurrentPositionForDepth(int depth)
        {
            if (CurrentDepth == depth) return new Vector2Int(KingMapPosition.x, depth - 1); // 직전 선택 위치 X 유지
            return new Vector2Int(PrototypeCenterX, depth - 1); // 최초·외부 라운드 변경 중앙 위치 사용
        }

        private static bool ContainsConnection(StageNode sourceNode, string targetNodeId)
        {
            if (sourceNode == null || string.IsNullOrWhiteSpace(targetNodeId)) return false; // 잘못된 입력 차단

            for (int i = 0; i < sourceNode.NextNodeIds.Count; i++)
            {
                if (string.Equals(sourceNode.NextNodeIds[i], targetNodeId, StringComparison.Ordinal)) return true; // 연결 ID 일치 반환
            }

            return false; // 연결 없음 반환
        }

        private void RecordVisitedNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return; // 빈 방문 ID 제외

            for (int i = 0; i < _visitedNodeIds.Count; i++)
            {
                if (string.Equals(_visitedNodeIds[i], nodeId, StringComparison.Ordinal)) return; // 중복 방문 이력 차단
            }

            _visitedNodeIds.Add(nodeId); // 새 방문 노드 이력 기록
        }

        private void RecordSelectedPath(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return; // 빈 경로 ID 제외

            for (int i = 0; i < _selectedPathNodeIds.Count; i++)
            {
                if (string.Equals(_selectedPathNodeIds[i], nodeId, StringComparison.Ordinal)) return; // 중복 경로 기록 차단
            }

            _selectedPathNodeIds.Add(nodeId); // 새 선택 경로 순서 기록
        }

        private void AddNodeIfUnique(StageNode node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.NodeId)) return; // 잘못된 노드 제외
            if (FindNode(node.NodeId) != null) return; // 동일 ID 중복 제외
            _nodes.Add(node); // 새 노드 등록
        }
    }
}
