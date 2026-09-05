using NUnit.Framework; // EditMode 테스트·Assert 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Run; // RouteMapState·StageNode 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day44RouteMapTests // 44일차 경로 지도 킹 이동 회귀 테스트
    {
        [Test] // 연결된 인접 노드 이동 가능 검증
        public void CanMoveTo_ConnectedAdjacentNode_ReturnsTrue() // 킹 1칸 연결 이동 허용 확인
        {
            var current = new StageNode("current", new Vector2Int(4, 2), 3, "Resolved"); // 현재 노드 생성
            var next = new StageNode("next", new Vector2Int(5, 3), 4, "PrototypeBattle"); // 다음 노드 생성
            current.SetNextNodeIds(new[] { next.NodeId }); // 현재→다음 연결 지정

            var map = new RouteMapState(); // 경로 지도 상태 생성
            map.Configure(3, current, new[] { next }); // 테스트 경로 구성

            Assert.IsTrue(map.CanMoveTo(next)); // 연결된 대각선 1칸 이동 허용 검증
        }

        [Test] // 연결되지 않은 인접 노드 이동 차단 검증
        public void CanMoveTo_DisconnectedAdjacentNode_ReturnsFalse() // 그래프 연결 없는 이동 차단 확인
        {
            var current = new StageNode("current", new Vector2Int(4, 2), 3, "Resolved"); // 현재 노드 생성
            var next = new StageNode("next", new Vector2Int(5, 3), 4, "PrototypeBattle"); // 인접 노드 생성

            var map = new RouteMapState(); // 경로 지도 상태 생성
            map.Configure(3, current, new[] { next }); // 연결 없는 경로 구성

            Assert.IsFalse(map.CanMoveTo(next)); // 연결 없는 인접 이동 차단 검증
        }

        [Test] // 연결됐지만 멀리 있는 노드 이동 차단 검증
        public void CanMoveTo_ConnectedDistantNode_ReturnsFalse() // 킹 1칸 거리 규칙 확인
        {
            var current = new StageNode("current", new Vector2Int(4, 2), 3, "Resolved"); // 현재 노드 생성
            var distant = new StageNode("distant", new Vector2Int(6, 4), 4, "PrototypeBattle"); // 두 칸 이상 떨어진 노드 생성
            current.SetNextNodeIds(new[] { distant.NodeId }); // 그래프 연결 지정

            var map = new RouteMapState(); // 경로 지도 상태 생성
            map.Configure(3, current, new[] { distant }); // 테스트 경로 구성

            Assert.IsFalse(map.CanMoveTo(distant)); // 그래프 연결만으로 장거리 점프 불가 검증
        }

        [Test] // 실제 선택 이동 상태 갱신 검증
        public void TryMoveKingTo_ValidNode_UpdatesMapSelection() // 킹 위치·현재 노드·선택 노드 갱신 확인
        {
            var current = new StageNode("current", new Vector2Int(4, 2), 3, "Resolved"); // 현재 노드 생성
            var next = new StageNode("next", new Vector2Int(4, 3), 4, "PrototypeBattle"); // 직선 1칸 노드 생성
            current.SetNextNodeIds(new[] { next.NodeId }); // 현재→다음 연결 지정

            var map = new RouteMapState(); // 경로 지도 상태 생성
            map.Configure(3, current, new[] { next }); // 테스트 경로 구성

            bool moved = map.TryMoveKingTo(next.NodeId); // 킹 이동·스테이지 선택 시도

            Assert.IsTrue(moved); // 이동 성공 검증
            Assert.AreEqual(next.Position, map.KingMapPosition); // 킹 지도 좌표 갱신 검증
            Assert.AreEqual(next.NodeId, map.CurrentNodeId); // 현재 노드 갱신 검증
            Assert.AreEqual(next.NodeId, map.SelectedNodeId); // 선택 노드 기록 검증
            Assert.AreEqual(next.Depth, map.CurrentDepth); // 현재 지도 깊이 갱신 검증
            Assert.IsTrue(next.Visited); // 선택 노드 방문 처리 검증
        }

        [Test] // 한 지도 단계에서 중복 선택 차단 검증
        public void TryMoveKingTo_AfterStageSelected_ReturnsFalse() // 첫 선택 후 추가 이동 차단 확인
        {
            var current = new StageNode("current", new Vector2Int(4, 2), 3, "Resolved"); // 현재 노드 생성
            var left = new StageNode("left", new Vector2Int(3, 3), 4, "PrototypeBattle"); // 왼쪽 후보 생성
            var right = new StageNode("right", new Vector2Int(5, 3), 4, "PrototypeBattle"); // 오른쪽 후보 생성
            current.SetNextNodeIds(new[] { left.NodeId, right.NodeId }); // 두 후보 연결 지정

            var map = new RouteMapState(); // 경로 지도 상태 생성
            map.Configure(3, current, new[] { left, right }); // 테스트 경로 구성

            Assert.IsTrue(map.TryMoveKingTo(left.NodeId)); // 첫 스테이지 선택
            Assert.IsFalse(map.TryMoveKingTo(right.NodeId)); // 두 번째 선택 차단 검증
            Assert.AreEqual(left.NodeId, map.SelectedNodeId); // 최초 선택 유지 검증
        }
    }
}
