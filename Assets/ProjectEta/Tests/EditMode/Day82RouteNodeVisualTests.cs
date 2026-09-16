using System.Collections.Generic; // 경로 노드·좌표 목록 사용
using System.Reflection; // 비공개 지도 좌표 계산 호출
using NUnit.Framework; // NUnit 검증 기능
using UnityEngine; // GameObject·Vector2Int·Vector3 사용
using ProjectEta.Board; // 지도 노드 표시 컴포넌트 사용
using ProjectEta.Run; // StageType 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 시작
    public sealed class Day82RouteNodeVisualTests // 82일차 경로 노드 표시 회귀 테스트
    { // 테스트 클래스 시작
        [Test] // 단일 아이콘 표면 검증
        public void RouteNodeModel_UsesSingleTexturedSurface() // 기존 입체 모델 제거 검증
        { // 테스트 시작
            GameObject host = new GameObject("RouteNodeIconTestHost"); // 테스트 호스트 생성
            RouteNodeIconPresenter presenter = host.AddComponent<RouteNodeIconPresenter>(); // 현재 노드 아이콘 표시기 연결

            try // 테스트 자원 정리 보장
            { // 보호 구간 시작
                presenter.Initialize(null, StageType.Shop, 1f); // 상점 노드 표시 초기화

                Assert.That(presenter.VisualRoot, Is.Not.Null); // 표시 루트 생성 확인
                Assert.That(presenter.PartCount, Is.EqualTo(1)); // 단일 아이콘 표면 확인
                Renderer iconRenderer = presenter.VisualRoot.GetComponentInChildren<Renderer>(); // 아이콘 렌더러 조회
                Assert.That(iconRenderer, Is.Not.Null); // 아이콘 렌더러 존재 확인
                Assert.That(iconRenderer.sharedMaterial.mainTexture, Is.Not.Null); // 아이콘 텍스처 연결 확인
                Assert.That(iconRenderer.sharedMaterial.mainTexture.name, Is.EqualTo("RouteShopIcon")); // 상점 아이콘 선택 확인
            } // 보호 구간 종료
            finally // 테스트 자원 정리
            { // 정리 구간 시작
                Object.DestroyImmediate(host); // 테스트 호스트 제거
            } // 정리 구간 종료
        } // 테스트 종료

        [TestCase(StageType.Battle, 0.798f)] // 일반 노드 현재 크기 대비 70% 사례
        [TestCase(StageType.FinalBoss, 0.966f)] // 보스 노드 현재 크기 대비 70% 사례
        public void RouteNodeIcon_UsesReducedVisualSize(StageType stageType, float expectedSize) // 노드 아이콘 30% 축소 검증
        { // 테스트 시작
            GameObject host = new GameObject("RouteNodeSizeTestHost"); // 테스트 호스트 생성
            RouteNodeIconPresenter presenter = host.AddComponent<RouteNodeIconPresenter>(); // 노드 아이콘 표시기 연결

            try // 테스트 자원 정리 보장
            { // 보호 구간 시작
                presenter.Initialize(null, stageType, 1f); // 지정 스테이지 노드 표시 초기화

                Transform iconSurface = presenter.VisualRoot.transform.Find("RouteNodeIconSurface"); // 실제 아이콘 표면 조회
                Assert.That(iconSurface, Is.Not.Null); // 아이콘 표면 존재 확인
                Assert.That(iconSurface.localScale.x, Is.EqualTo(expectedSize).Within(0.001f)); // 축소 가로 크기 확인
                Assert.That(iconSurface.localScale.y, Is.EqualTo(expectedSize).Within(0.001f)); // 축소 세로 크기 확인
            } // 보호 구간 종료
            finally // 테스트 자원 정리
            { // 정리 구간 시작
                Object.DestroyImmediate(host); // 테스트 호스트 제거
            } // 정리 구간 종료
        } // 테스트 종료

        [TestCase(StageType.Battle, "RouteBattleIcon")] // 일반 전투 아이콘 사례
        [TestCase(StageType.Elite, "RouteEliteIcon")] // 엘리트 아이콘 사례
        [TestCase(StageType.Reward, "RouteRewardIcon")] // 보상 아이콘 사례
        [TestCase(StageType.Shop, "RouteShopIcon")] // 상점 아이콘 사례
        [TestCase(StageType.Event, "RouteEventIcon")] // 이벤트 아이콘 사례
        [TestCase(StageType.MidBoss, "RouteBossIcon")] // 중간 보스 아이콘 사례
        [TestCase(StageType.FinalBoss, "RouteBossIcon")] // 최종 보스 아이콘 사례
        public void RouteNodeModel_StageTypeSelectsExpectedTexture(StageType stageType, string expectedTextureName) // 스테이지별 텍스처 연결 검증
        { // 테스트 시작
            GameObject host = new GameObject("RouteNodeTextureTestHost"); // 테스트 호스트 생성
            RouteNodeIconPresenter presenter = host.AddComponent<RouteNodeIconPresenter>(); // 현재 노드 아이콘 표시기 연결

            try // 테스트 자원 정리 보장
            { // 보호 구간 시작
                presenter.Initialize(null, stageType, 1f); // 지정 스테이지 노드 표시 초기화

                Renderer iconRenderer = presenter.VisualRoot.GetComponentInChildren<Renderer>(); // 표시 렌더러 조회
                Assert.That(iconRenderer.sharedMaterial.mainTexture, Is.Not.Null); // 텍스처 연결 확인
                Assert.That(iconRenderer.sharedMaterial.mainTexture.name, Is.EqualTo(expectedTextureName)); // 종류별 텍스처 확인
            } // 보호 구간 종료
            finally // 테스트 자원 정리
            { // 정리 구간 시작
                Object.DestroyImmediate(host); // 테스트 호스트 제거
            } // 정리 구간 종료
        } // 테스트 종료

        [Test] // 가로 간격 검증
        public void NodeLayout_AdjacentColumnsUseOnePointEightSpacing() // 가로 발판 간격 확대 검증
        { // 테스트 시작
            using (RouteMapControllerFixture fixture = new RouteMapControllerFixture()) // 지도 컨트롤러 준비
            { // 테스트 범위 시작
                Vector3 left = fixture.GetNodePosition(new Vector2Int(3, 2)); // 왼쪽 노드 위치 조회
                Vector3 right = fixture.GetNodePosition(new Vector2Int(4, 2)); // 오른쪽 노드 위치 조회

                Assert.That(Mathf.Abs(right.x - left.x), Is.EqualTo(1.8f).Within(0.001f)); // 정확한 1.8배 가로 간격 확인
            } // 테스트 범위 종료
        } // 테스트 종료

        [Test] // 수평 정렬 완화 검증
        public void NodeLayout_SameDepthNodesUseDifferentForwardOffsets() // 같은 깊이 노드 엇갈림 검증
        { // 테스트 시작
            using (RouteMapControllerFixture fixture = new RouteMapControllerFixture()) // 지도 컨트롤러 준비
            { // 테스트 범위 시작
                Vector3 left = fixture.GetNodePosition(new Vector2Int(3, 2)); // 왼쪽 노드 위치 조회
                Vector3 right = fixture.GetNodePosition(new Vector2Int(4, 2)); // 오른쪽 노드 위치 조회

                Assert.That(Mathf.Abs(right.z - left.z), Is.GreaterThanOrEqualTo(0.1f)); // 가로줄 이탈 간격 확인
            } // 테스트 범위 종료
        } // 테스트 종료

        [Test] // 확대 노드 경로 간격 검증
        public void NodeLayout_AdjacentDepthsLeaveRoomForSixTimesNodes() // 총 6배 노드 겹침 방지 검증
        { // 테스트 시작
            using (RouteMapControllerFixture fixture = new RouteMapControllerFixture()) // 지도 컨트롤러 준비
            { // 테스트 범위 시작
                Vector3 first = fixture.GetNodePosition(new Vector2Int(4, 1)); // 두 번째 깊이 노드 위치 조회
                Vector3 second = fixture.GetNodePosition(new Vector2Int(4, 2)); // 세 번째 깊이 노드 위치 조회

                Assert.That(Vector3.Distance(first, second), Is.GreaterThanOrEqualTo(1.14f)); // 확대 아이콘 이상의 경로 간격 확인
            } // 테스트 범위 종료
        } // 테스트 종료

        [Test] // 시작·최종 보스 위치 검증
        public void NodeLayout_StartAndFinalBossUseOppositeBoardEnds() // 양끝 중앙선 배치 검증
        { // 테스트 시작
            using (RouteMapControllerFixture fixture = new RouteMapControllerFixture()) // 지도 컨트롤러 준비
            { // 테스트 범위 시작
                Vector3 first = fixture.GetNodePosition(new Vector2Int(4, 0)); // 첫 깊이 위치 조회
                Vector3 final = fixture.GetNodePosition(new Vector2Int(4, 9)); // 마지막 깊이 위치 조회

                Assert.That(first.x, Is.EqualTo(0f).Within(0.001f)); // 시작 노드 중앙 X축 확인
                Assert.That(final.x, Is.EqualTo(first.x).Within(0.001f)); // 최종 보스 일직선 X축 확인
                Assert.That(first.z, Is.EqualTo(-4f).Within(0.001f)); // 시작 노드 아래 끝 위치 확인
                Assert.That(final.z, Is.EqualTo(4f).Within(0.001f)); // 최종 보스 위 끝 위치 확인
            } // 테스트 범위 종료
        } // 테스트 종료

        [TestCase(82, 0)] // 시작 노드 현재 위치 사례
        [TestCase(82, 7)] // 전반 분기 현재 위치 사례
        [TestCase(82, 9)] // 중간 보스 현재 위치 사례
        [TestCase(82, 11)] // 후반 중앙 분기 현재 위치 사례
        [TestCase(82, 16)] // 최종 전 분기 현재 위치 사례
        [TestCase(82, 20)] // 최종 보스 현재 위치 사례
        [TestCase(20260916, 0)] // 날짜 기반 회귀 Seed 사례
        public void NodeLayout_FullRouteUsesSharedNonOverlappingPositions(int mapSeed, int currentNodeIndex) // 전체 경로 노드 겹침 방지 검증
        { // 테스트 시작
            IReadOnlyList<StageNode> nodes = StageRouteGenerator.CreateFullRoute(mapSeed); // 전체 경로 노드 생성
            var otherNodes = new List<StageNode>(); // 현재 노드 외 목록 생성

            for (int i = 0; i < nodes.Count; i++) // 전체 노드 순회
            { // 순회 시작
                if (i == currentNodeIndex) continue; // 현재 노드 중복 등록 제외
                otherNodes.Add(nodes[i]); // 지도 상태 구성 노드 추가
            } // 순회 종료

            var route = new RouteMapState(mapSeed); // 검증용 지도 상태 생성
            StageNode currentNode = nodes[currentNodeIndex]; // 현재 진행 노드 선택
            route.Configure(currentNode.Depth, currentNode, otherNodes); // 현재 진행 위치별 전체 경로 상태 구성
            IReadOnlyDictionary<string, Vector3> positions = RouteMapVisualLayout.Build(route, 1f); // 전체 노드 공통 좌표표 생성

            Assert.That(positions.Count, Is.EqualTo(nodes.Count)); // 모든 노드 좌표 생성 확인

            for (int firstIndex = 0; firstIndex < nodes.Count; firstIndex++) // 첫 번째 노드 순회
            { // 첫 순회 시작
                for (int secondIndex = firstIndex + 1; secondIndex < nodes.Count; secondIndex++) // 두 번째 노드 순회
                { // 둘째 순회 시작
                    StageNode first = nodes[firstIndex]; // 첫 번째 노드 조회
                    StageNode second = nodes[secondIndex]; // 두 번째 노드 조회
                    float minimumDistance = ResolveRadius(first, route.CurrentNodeId) + ResolveRadius(second, route.CurrentNodeId) + 0.12f; // 두 원판 반지름과 여백 합산
                    float actualDistance = Vector3.Distance(positions[first.NodeId], positions[second.NodeId]); // 실제 노드 중심 거리 계산
                    Assert.That(actualDistance, Is.GreaterThanOrEqualTo(minimumDistance - 0.001f), $"겹침 노드: {first.NodeId} / {second.NodeId}"); // 모든 노드 겹침 없음 확인
                } // 둘째 순회 종료
            } // 첫 순회 종료

            Assert.That(positions[nodes[0].NodeId], Is.EqualTo(new Vector3(0f, 0f, -4f))); // 시작 노드 아래 중앙 확인
            Assert.That(positions[nodes[nodes.Count - 1].NodeId], Is.EqualTo(new Vector3(0f, 0f, 4f))); // 최종 보스 위 중앙 확인
        } // 테스트 종료

        private static float ResolveRadius(StageNode node, string currentNodeId) // 노드별 실제 표시 반지름 계산
        { // 반지름 계산 시작
            if (node.NodeId == currentNodeId) return 0.63f; // 현재 노드 축소 원판 반지름
            if (node.Depth == RoundState.FinalRound || node.Depth == 5) return 0.483f; // 보스 축소 원판 반지름
            return 0.462f; // 선택 가능 일반 노드 축소 원판 반지름
        } // 반지름 계산 종료

        private sealed class RouteMapControllerFixture : System.IDisposable // 지도 좌표 테스트 자원
        { // Fixture 시작
            private readonly GameObject _boardHost; // 보드 호스트
            private readonly GameObject _controllerHost; // 컨트롤러 호스트
            private readonly RouteMapBoardController _controller; // 테스트 대상 컨트롤러
            private readonly MethodInfo _positionMethod; // 좌표 계산 메서드

            public RouteMapControllerFixture() // 테스트 자원 생성자
            { // 생성자 시작
                _boardHost = new GameObject("RouteLayoutBoardTestHost"); // 보드 호스트 생성
                BoardView boardView = _boardHost.AddComponent<BoardView>(); // 보드 뷰 생성
                _controllerHost = new GameObject("RouteLayoutControllerTestHost"); // 컨트롤러 호스트 생성
                _controller = _controllerHost.AddComponent<RouteMapBoardController>(); // 지도 컨트롤러 생성
                FieldInfo boardField = typeof(RouteMapBoardController).GetField("_boardView", BindingFlags.Instance | BindingFlags.NonPublic); // 보드 참조 필드 조회
                boardField.SetValue(_controller, boardView); // 보드 참조 주입
                _positionMethod = typeof(RouteMapBoardController).GetMethod("GetNodeLocalPosition", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Vector2Int) }, null); // 구버전 좌표 계산 오버로드 조회
            } // 생성자 종료

            public Vector3 GetNodePosition(Vector2Int cell) // 테스트 좌표 조회
            { // 메서드 시작
                return (Vector3)_positionMethod.Invoke(_controller, new object[] { cell }); // 실제 좌표 계산 호출
            } // 메서드 종료

            public void Dispose() // 테스트 자원 해제
            { // 해제 시작
                Object.DestroyImmediate(_controllerHost); // 컨트롤러 호스트 제거
                Object.DestroyImmediate(_boardHost); // 보드 호스트 제거
            } // 해제 종료
        } // Fixture 종료
    } // 테스트 클래스 종료
} // 네임스페이스 종료
