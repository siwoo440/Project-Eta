using System; // Guid·StringComparison 사용
using System.Collections.Generic; // List<T>·IEnumerable<T>·IReadOnlyList<T> 사용
using UnityEngine; // Vector2Int·Mathf 사용

namespace ProjectEta.Run
{
    public sealed class RouteMapState
    {
        private const int PrototypeCenterX = 4; // 저장 손상 fallback용 기본 X
        private readonly List<StageNode> _nodes = new List<StageNode>(); // 52일차 1~10 전체 경로 노드 목록
        private readonly List<string> _selectedPathNodeIds = new List<string>(); // 런 동안 실제 선택한 경로 기록
        private readonly List<string> _visitedNodeIds = new List<string>(); // 런 동안 방문한 노드 이력

        public int MapSeed { get; private set; } // 전체 10단계 경로 결정 Seed
        public int CurrentDepth { get; private set; } // 현재 킹이 위치한 지도 깊이
        public string CurrentNodeId { get; private set; } // 현재 킹이 위치한 노드 ID
        public string SelectedNodeId { get; private set; } // 현재 진입 중인 선택 스테이지 노드 ID
        public Vector2Int KingMapPosition { get; private set; } // 지도 모드 킹 좌표
        public IReadOnlyList<StageNode> Nodes => _nodes; // 전체 1~10 경로 노드 읽기 전용 공개
        public IReadOnlyList<string> SelectedPathNodeIds => _selectedPathNodeIds; // 지금까지 선택한 경로 읽기 전용 공개
        public IReadOnlyList<string> VisitedNodeIds => _visitedNodeIds; // 지금까지 방문한 노드 읽기 전용 공개
        public StageNode CurrentNode => FindNode(CurrentNodeId); // 현재 노드 조회
        public StageNode SelectedNode => FindNode(SelectedNodeId); // 선택 스테이지 노드 조회
        public bool HasPreparedRoute => _nodes.Count > 0 && CurrentNode != null; // 경로 준비 여부
        public bool HasSelectedNode => !string.IsNullOrWhiteSpace(SelectedNodeId); // 스테이지 진입 선택 완료 여부
        public bool HasCompleteRoute => HasEveryDepth(); // 1~10 전체 그래프 생성 여부

        public RouteMapState()
            : this(CreateRuntimeSeed())
        {
        }

        public RouteMapState(int mapSeed)
        {
            MapSeed = mapSeed; // 외부 또는 런타임 MapSeed 저장
            Clear(); // 지도 진행 상태 기본값 초기화
        }

        public void Clear()
        {
            _nodes.Clear(); // 전체 노드 목록 제거
            _selectedPathNodeIds.Clear(); // 선택 경로 기록 제거
            _visitedNodeIds.Clear(); // 방문 노드 이력 제거
            CurrentDepth = RoundState.FirstRound; // 기본 깊이 복구
            CurrentNodeId = string.Empty; // 현재 노드 제거
            SelectedNodeId = string.Empty; // 선택 노드 제거
            KingMapPosition = new Vector2Int(PrototypeCenterX, 0); // 기본 킹 좌표 지정
        }

        public void Configure(int currentDepth, StageNode currentNode, IEnumerable<StageNode> otherNodes)
        {
            _nodes.Clear(); // 테스트·구버전 호환용 직접 구성 노드 교체
            CurrentNodeId = string.Empty; // 현재 노드 ID 초기화
            SelectedNodeId = string.Empty; // 선택 노드 ID 초기화
            CurrentDepth = Mathf.Clamp(currentDepth, RoundState.FirstRound, RoundState.FinalRound); // 깊이 범위 보정

            if (currentNode == null) return; // 현재 노드 누락 처리

            AddNodeIfUnique(currentNode); // 현재 노드 등록

            if (otherNodes != null)
            {
                foreach (StageNode node in otherNodes)
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
            int safeDepth = Mathf.Clamp(clearedDepth, RoundState.FirstRound, RoundState.FinalRound - 1); // 다음 선택이 가능한 1~9 완료 깊이 보정

            if (!HasCompleteRoute)
            {
                RebuildCompleteRoute(safeDepth); // 최초 지도 진입 또는 구버전 부분 지도에서 1~10 전체 그래프 생성
            }

            StageNode anchor = ResolveAnchorNode(safeDepth); // 완료한 스테이지와 같은 깊이의 현재 킹 노드 확인
            if (anchor == null) return; // 경로 손상 시 추가 진행 변경 차단

            CurrentDepth = safeDepth; // 완료 깊이를 현재 지도 깊이로 유지
            CurrentNodeId = anchor.NodeId; // 완료 스테이지 노드를 현재 지도 위치로 확정
            KingMapPosition = anchor.Position; // 지도 킹 좌표를 현재 노드와 동기화
            anchor.MarkVisited(); // 완료 스테이지 방문 표시
            RecordVisitedNode(anchor.NodeId); // 런 전체 방문 이력 기록
            SelectedNodeId = string.Empty; // 스테이지 완료 후 다음 깊이 선택 잠금 해제
        }

        public IReadOnlyList<StageNode> GetSelectableNodes()
        {
            var result = new List<StageNode>(); // 선택 가능 결과 목록 생성
            StageNode current = CurrentNode; // 현재 노드 조회
            if (current == null || HasSelectedNode) return result; // 현재 노드 누락·이미 선택 완료 차단

            for (int i = 0; i < current.NextNodeIds.Count; i++)
            {
                StageNode node = FindNode(current.NextNodeIds[i]); // 실제 다음 노드 조회
                if (node != null && IsKingStep(CurrentNode.Position, node.Position)) result.Add(node); // 연결·킹 1칸 조건 후보 추가
            }

            return result; // 바로 다음 깊이 선택 가능 노드 반환
        }

        public bool CanMoveTo(StageNode targetNode)
        {
            if (targetNode == null || CurrentNode == null || HasSelectedNode) return false; // 대상·현재 노드 누락·이미 선택 완료 차단
            if (!ContainsConnection(CurrentNode, targetNode.NodeId)) return false; // 전체 그래프 연결 없는 노드 차단
            return IsKingStep(KingMapPosition, targetNode.Position); // 킹 인접 이동 규칙 반환
        }

        public bool TryMoveKingTo(string nodeId)
        {
            StageNode targetNode = FindNode(nodeId); // 대상 노드 조회
            if (!CanMoveTo(targetNode)) return false; // 유효하지 않은 이동 차단

            KingMapPosition = targetNode.Position; // 킹 지도 좌표 이동
            CurrentNodeId = targetNode.NodeId; // 현재 노드를 대상 노드로 변경
            SelectedNodeId = targetNode.NodeId; // 실제 진입할 스테이지 기록
            CurrentDepth = targetNode.Depth; // 지도 깊이 갱신
            targetNode.MarkVisited(); // 도착 노드 방문 처리
            RecordVisitedNode(targetNode.NodeId); // 도착 노드 방문 이력 기록
            RecordSelectedPath(targetNode.NodeId); // 실제 선택 경로 런 전체 기록
            return true; // 이동 성공 반환
        }

        public RouteMapSaveData ToSaveData()
        {
            var data = new RouteMapSaveData
            {
                mapSeed = MapSeed, // 전체 경로 Seed 기록
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
                StageNode node = _nodes[i]; // 현재 전체 경로 노드 조회
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
                    nodeData.nextNodeIds.Add(node.NextNodeIds[nextIndex]); // 전체 그래프 다음 연결 기록
                }

                data.nodes.Add(nodeData); // 노드 저장 DTO 추가
            }

            return data; // 전체 경로 포함 저장 DTO 반환
        }

        public void Restore(RouteMapSaveData data)
        {
            Clear(); // 기존 지도·선택 경로 제거
            if (data == null) return; // 저장 데이터 누락 시 기본 지도 유지

            MapSeed = data.mapSeed; // 저장 경로 Seed 복원
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

                    node.SetNextNodeIds(nodeData.nextNodeIds); // 전체 그래프 연결 복원
                    if (nodeData.visited) node.MarkVisited(); // 방문 상태 복원
                    AddNodeIfUnique(node); // 지도 노드 등록
                }
            }

            CurrentNodeId = FindNode(data.currentNodeId) != null ? data.currentNodeId : string.Empty; // 유효한 현재 노드 ID만 복원
            SelectedNodeId = FindNode(data.selectedNodeId) != null ? data.selectedNodeId : string.Empty; // 유효한 선택 노드 ID만 복원

            if (string.IsNullOrWhiteSpace(CurrentNodeId) && _nodes.Count > 0)
            {
                StageNode fallback = FindClosestNodeAtDepth(CurrentDepth, data.kingX, null); // 저장 깊이·좌표 기반 현재 노드 fallback 탐색

                if (fallback != null)
                {
                    CurrentNodeId = fallback.NodeId; // fallback 현재 노드 적용
                    KingMapPosition = fallback.Position; // fallback 킹 좌표 적용
                }
            }
        }

        public StageNode FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null; // 빈 ID 제외

            for (int i = 0; i < _nodes.Count; i++)
            {
                StageNode node = _nodes[i]; // 현재 노드 조회
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

        private void RebuildCompleteRoute(int anchorDepth)
        {
            string previousCurrentNodeId = CurrentNodeId; // 부분 지도 현재 노드 ID 보존
            string previousStageDefinitionId = CurrentNode != null ? CurrentNode.StageDefinitionId : string.Empty; // 부분 지도 현재 StageDefinition ID 보존
            int previousX = KingMapPosition.x; // 부분 지도 현재 X 위치 보존
            IReadOnlyList<StageNode> fullRoute = StageRouteGenerator.CreateFullRoute(MapSeed); // 같은 MapSeed로 1~10 전체 그래프 생성

            _nodes.Clear(); // 부분 지도 노드 제거

            for (int i = 0; i < fullRoute.Count; i++)
            {
                AddNodeIfUnique(fullRoute[i]); // 전체 그래프 노드 등록
            }

            RestoreVisitedFlagsFromHistory(); // 기존 방문 이력이 새 전체 그래프와 일치하면 시각 상태 복원

            StageNode anchor = FindNode(previousCurrentNodeId); // 동일 노드 ID 우선 탐색

            if (anchor == null || anchor.Depth != anchorDepth)
            {
                anchor = FindClosestNodeAtDepth(anchorDepth, previousX, previousStageDefinitionId); // 구버전 부분 지도는 깊이·좌표·정의 기준으로 근접 노드 매핑
            }

            if (anchor == null)
            {
                anchor = FindClosestNodeAtDepth(anchorDepth, previousX, null); // 정의 매칭 실패 시 같은 깊이 근접 노드 선택
            }

            if (anchor != null)
            {
                CurrentNodeId = anchor.NodeId; // 전체 그래프 현재 노드 적용
                CurrentDepth = anchor.Depth; // 현재 깊이 동기화
                KingMapPosition = anchor.Position; // 킹 좌표 동기화
                anchor.MarkVisited(); // 현재 완료 노드 방문 처리
                RecordVisitedNode(anchor.NodeId); // 방문 이력 기록
            }

            SelectedNodeId = string.Empty; // 전체 그래프 생성 시 이전 부분 지도 선택 잠금 제거
        }

        private StageNode ResolveAnchorNode(int depth)
        {
            StageNode current = CurrentNode; // 현재 노드 조회
            if (current != null && current.Depth == depth) return current; // 현재 노드가 완료 깊이면 그대로 사용

            StageNode selected = SelectedNode; // 선택 스테이지 노드 조회
            if (selected != null && selected.Depth == depth) return selected; // 선택 노드가 완료 깊이면 현재 위치로 사용

            return FindClosestNodeAtDepth(depth, KingMapPosition.x, null); // 손상 상태는 현재 X와 가장 가까운 같은 깊이 노드로 보정
        }

        private StageNode FindClosestNodeAtDepth(int depth, int preferredX, string preferredStageDefinitionId)
        {
            StageNode best = null; // 가장 가까운 같은 깊이 노드 초기화
            int bestDistance = int.MaxValue; // 최소 X 거리 초기화

            for (int i = 0; i < _nodes.Count; i++)
            {
                StageNode node = _nodes[i]; // 현재 후보 노드 조회
                if (node == null || node.Depth != depth) continue; // 다른 깊이 제외

                if (!string.IsNullOrWhiteSpace(preferredStageDefinitionId) &&
                    string.Equals(node.StageDefinitionId, preferredStageDefinitionId, StringComparison.Ordinal))
                {
                    return node; // 동일 StageDefinition이 있으면 최우선 복원
                }

                int distance = Mathf.Abs(node.Position.x - preferredX); // 현재 X와 후보 X 거리 계산
                if (distance >= bestDistance) continue; // 더 멀거나 같은 후보 제외
                bestDistance = distance; // 최소 거리 갱신
                best = node; // 근접 후보 갱신
            }

            return best; // 근접 같은 깊이 노드 반환
        }

        private void RestoreVisitedFlagsFromHistory()
        {
            for (int i = 0; i < _visitedNodeIds.Count; i++)
            {
                StageNode node = FindNode(_visitedNodeIds[i]); // 방문 이력과 동일 ID 노드 조회
                if (node != null) node.MarkVisited(); // 전체 그래프 동일 노드 방문 표시 복원
            }
        }

        private bool HasEveryDepth()
        {
            if (_nodes.Count == 0) return false; // 빈 지도 전체 경로 아님

            for (int depth = RoundState.FirstRound; depth <= RoundState.FinalRound; depth++)
            {
                bool found = false; // 현재 깊이 존재 여부 초기화

                for (int i = 0; i < _nodes.Count; i++)
                {
                    StageNode node = _nodes[i]; // 현재 노드 조회
                    if (node != null && node.Depth == depth)
                    {
                        found = true; // 현재 깊이 노드 발견
                        break; // 해당 깊이 추가 탐색 종료
                    }
                }

                if (!found) return false; // 하나라도 빠진 깊이가 있으면 전체 경로 아님
            }

            return true; // 1~10 모든 깊이 노드 존재
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

        private static int CreateRuntimeSeed()
        {
            return Guid.NewGuid().GetHashCode(); // 새 런마다 다른 MapSeed 생성
        }
    }
}
