using System; // Serializable·StringComparison 사용
using System.Collections.Generic; // List<T>·IEnumerable<T>·IReadOnlyList<T> 사용
using UnityEngine; // SerializeField·Vector2Int 사용

namespace ProjectEta.Run // 런 경로 노드 네임스페이스
{
    [Serializable] // Unity 직렬화 대상 지정
    public sealed class StageNode // 체스판 경로 지도 위 한 스테이지 노드
    {
        [SerializeField] private string _nodeId; // 런 안에서 노드를 구분하는 ID
        [SerializeField] private Vector2Int _position; // 10×10 체스판 좌표
        [SerializeField] private int _depth; // 1~10 스테이지 깊이
        [SerializeField] private string _stageDefinitionId; // 연결할 StageDefinition ID
        [SerializeField] private List<string> _nextNodeIds = new List<string>(); // 다음 이동 가능 노드 ID 목록
        [SerializeField] private bool _visited; // 방문 완료 여부

        public string NodeId => _nodeId; // 노드 ID 공개
        public Vector2Int Position => _position; // 체스판 좌표 공개
        public int Depth => _depth; // 스테이지 깊이 공개
        public string StageDefinitionId => _stageDefinitionId; // StageDefinition ID 공개
        public IReadOnlyList<string> NextNodeIds => _nextNodeIds; // 다음 노드 목록 공개
        public bool Visited => _visited; // 방문 여부 공개

        public StageNode(string nodeId, Vector2Int position, int depth, string stageDefinitionId) // 런타임 노드 생성
        {
            _nodeId = nodeId ?? string.Empty; // null ID 방지
            _position = position; // 체스판 좌표 저장
            _depth = depth < RoundState.FirstRound ? RoundState.FirstRound : depth; // 최소 깊이 보정
            _stageDefinitionId = stageDefinitionId ?? string.Empty; // null 정의 ID 방지
        }

        public void SetNextNodeIds(IEnumerable<string> nodeIds) // 다음 이동 가능 노드 목록 교체
        {
            _nextNodeIds.Clear(); // 기존 연결 제거
            if (nodeIds == null) return; // 빈 입력 처리

            foreach (var nodeId in nodeIds) // 입력 연결 순회
            {
                if (string.IsNullOrWhiteSpace(nodeId)) continue; // 빈 연결 제외
                if (ContainsNextNode(nodeId)) continue; // 중복 연결 제외
                _nextNodeIds.Add(nodeId); // 새 연결 추가
            }
        }

        public void MarkVisited() // 현재 노드 방문 처리
        {
            _visited = true; // 방문 상태 기록
        }

        private bool ContainsNextNode(string nodeId) // 중복 연결 검사
        {
            for (int i = 0; i < _nextNodeIds.Count; i++) // 기존 연결 순회
            {
                if (string.Equals(_nextNodeIds[i], nodeId, StringComparison.Ordinal)) return true; // 동일 ID 확인
            }

            return false; // 동일 연결 없음
        }
    }
}
