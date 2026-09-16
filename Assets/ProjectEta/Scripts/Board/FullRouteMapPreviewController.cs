using System.Collections; // 초기화 대기 코루틴 사용
using System.Collections.Generic; // HashSet<T>·List<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Material·Color 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Run; // RouteMapState·StageNode·StageType 사용

namespace ProjectEta.Board
{
    [DefaultExecutionOrder(1010)]
    public sealed class FullRouteMapPreviewController : MonoBehaviour
    {
        private const float FutureNodeHeight = 0.024f; // 미래 노드 원판 높이
        private const float FutureNodeRadius = 0.756f; // 미래 일반 노드 현재 크기 대비 70% 반지름
        private const float BossNodeRadius = 0.966f; // 보스 노드 현재 크기 대비 70% 강조 반지름
        private const float PreviewPathHeight = 0.012f; // 전체 경로선 보드 높이

        private static readonly Color FutureDimColor = new Color(0.13f, 0.15f, 0.17f); // 미래 노드 톤다운 기준색
        private static readonly Color VisitedColor = new Color(0.72f, 0.60f, 0.24f); // 지나온 노드 강조 기준색
        private static readonly Color PreviewPathColor = new Color(0.12f, 0.30f, 0.34f); // 전체 미래 경로선 색상

        private BattleController _battleController; // 현재 RunState 소유 전투 컨트롤러
        private RouteMapBoardController _routeMapBoardController; // 기존 지도 입력·현재 노드 표시 컨트롤러
        private BoardView _boardView; // 10×10 보드 좌표 변환 기준
        private RunState _runState; // 현재 전체 런 상태
        private GameObject _previewRoot; // 52일차 전체 경로 미리보기 루트
        private readonly List<Material> _runtimeMaterials = new List<Material>(); // 런타임 생성 머티리얼 정리 목록
        private IReadOnlyDictionary<string, Vector3> _nodePositions; // 전체 노드·경로 공통 시각 좌표표
        private string _lastPreviewKey = string.Empty; // 현재 지도 진행 상태 비교 키

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<FullRouteMapPreviewController>() != null) return; // 중복 미리보기 관리자 차단

            var host = new GameObject("FullRouteMapPreviewController_Day52"); // 52일차 전체 경로 표시 호스트 생성
            host.AddComponent<FullRouteMapPreviewController>(); // 전체 경로 미리보기 컴포넌트 추가
        }

        private IEnumerator Start()
        {
            const int maxWaitFrames = 240; // 필수 런타임 객체 최대 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임 초기화

            while (waitedFrames < maxWaitFrames)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 탐색
                _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 기존 지도 컨트롤러 탐색
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 실제 10×10 BoardView 탐색

                if (_battleController != null &&
                    _battleController.RunState != null &&
                    _routeMapBoardController != null &&
                    _boardView != null &&
                    _boardView.IsBound)
                {
                    _runState = _battleController.RunState; // 현재 RunState 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임까지 대기
            }

            Debug.LogError("52일차 FullRouteMapPreviewController 초기화 실패: BattleController·RouteMapBoardController·BoardView를 확인하세요."); // 초기화 실패 로그
        }

        private void Update()
        {
            if (_battleController != null && _battleController.RunState != _runState)
            {
                _runState = _battleController.RunState; // 안전 세이브 복원·외부 RunState 교체 대응
                _lastPreviewKey = string.Empty; // 새 런 전체 경로 표시 강제 갱신
                DestroyPreview(); // 이전 런 경로 시각 제거
            }

            if (_runState == null || _routeMapBoardController == null) return; // 런·지도 컨트롤러 준비 전 처리 차단

            if (!_routeMapBoardController.IsMapModeActive)
            {
                if (_previewRoot != null) DestroyPreview(); // 전투 모드 복귀 시 미래 경로 시각 제거
                return;
            }

            if (_runState.CurrentFlowPhase != RunFlowPhase.Map)
            {
                if (_previewRoot != null) DestroyPreview(); // 보상·상점·이벤트 동안 전체 지도 아이콘 잔존 차단
                _lastPreviewKey = string.Empty; // 실제 Map 복귀 시 재생성 허용
                return;
            }

            if (!_runState.RouteMap.HasCompleteRoute) return; // 52일차 전체 그래프 준비 전 미리보기 차단
            if (_runState.RouteMap.HasSelectedNode) return; // 스테이지 진입 이동 중 기존 시각 유지

            string previewKey = CreatePreviewKey(_runState.RouteMap); // 현재 지도 진행 상태 키 생성

            if (_previewRoot != null && string.Equals(previewKey, _lastPreviewKey, System.StringComparison.Ordinal))
            {
                return; // 같은 현재 노드·방문 상태 중복 생성 차단
            }

            BuildPreview(); // 현재 전체 10단계 그래프 기준 미래·과거 노드 표시
            _lastPreviewKey = previewKey; // 정상 생성 상태 키 저장
        }

        private void BuildPreview()
        {
            DestroyPreview(); // 이전 전체 경로 미리보기 제거

            RouteMapState route = _runState.RouteMap; // 현재 전체 경로 상태 조회
            IReadOnlyDictionary<string, Vector3> sharedPositions = _routeMapBoardController.NodePositions; // 현재 지도 컨트롤러 좌표표 조회
            bool sharesAllNodes = sharedPositions != null && sharedPositions.Count == route.Nodes.Count; // 전체 노드 좌표 포함 여부 확인
            _nodePositions = sharesAllNodes ? sharedPositions : RouteMapVisualLayout.Build(route, _boardView.TileSize); // 동일 좌표표 우선 재사용과 손상 상태 대체 생성
            _previewRoot = new GameObject("RouteMapFullGraph_Day52"); // 전체 그래프 시각 루트 생성
            _previewRoot.transform.SetParent(_boardView.transform, false); // 기존 보드 로컬 좌표계 재사용

            IReadOnlyList<StageNode> selectableNodes = route.GetSelectableNodes(); // 기존 컨트롤러가 직접 그릴 클릭 가능 다음 노드 조회
            var selectableIds = new HashSet<string>(); // 중복 표시 제외용 선택 노드 ID 집합 생성

            for (int i = 0; i < selectableNodes.Count; i++)
            {
                if (selectableNodes[i] != null) selectableIds.Add(selectableNodes[i].NodeId); // 클릭 가능 노드 ID 등록
            }

            CreateAllPathLines(route, selectableIds); // 1~10 전체 그래프 연결선 생성

            for (int i = 0; i < route.Nodes.Count; i++)
            {
                StageNode node = route.Nodes[i]; // 현재 전체 그래프 노드 조회
                if (node == null) continue; // 빈 노드 제외
                if (node.NodeId == route.CurrentNodeId) continue; // 현재 노드는 기존 지도 컨트롤러 표시 사용
                if (selectableIds.Contains(node.NodeId)) continue; // 바로 다음 클릭 노드는 기존 지도 컨트롤러 표시 사용
                CreatePreviewNode(node, route.CurrentDepth); // 미래·과거 비클릭 노드 미리보기 생성
            }

            Debug.Log($"52일차 전체 경로 표시: Nodes={route.Nodes.Count} / Depth={route.CurrentDepth} / Seed={route.MapSeed}"); // 전체 경로 표시 결과 로그
        }

        private void CreateAllPathLines(RouteMapState route, HashSet<string> selectableIds)
        {
            for (int i = 0; i < route.Nodes.Count; i++)
            {
                StageNode source = route.Nodes[i]; // 현재 연결 출발 노드 조회
                if (source == null) continue; // 빈 출발 노드 제외

                for (int nextIndex = 0; nextIndex < source.NextNodeIds.Count; nextIndex++)
                {
                    StageNode target = route.FindNode(source.NextNodeIds[nextIndex]); // 연결 대상 노드 조회
                    if (target == null) continue; // 손상 연결 제외

                    bool existingControllerDraws =
                        source.NodeId == route.CurrentNodeId &&
                        selectableIds.Contains(target.NodeId); // 기존 RouteMapBoardController가 현재→선택 후보 선을 그리는지 확인

                    if (existingControllerDraws) continue; // 동일 연결선 중복 표시 차단
                    CreatePreviewPathLine(source, target); // 미래·과거 공통 좌표 연결선 생성
                }
            }
        }

        private void CreatePreviewNode(StageNode node, int currentDepth)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // 비클릭 미래·과거 노드 원판 생성
            marker.name = $"RouteMap_FullNode_{node.NodeId}"; // 노드 ID 기반 계층창 이름 적용
            marker.transform.SetParent(_previewRoot.transform, false); // 전체 경로 루트 자식 배치
            marker.transform.localPosition = GetNodeLocalPosition(node) + new Vector3(0f, FutureNodeHeight * 0.5f, 0f); // 공통 좌표표 기준 미래 노드 배치

            StageType stageType = ResolveStageType(node); // 저장 가능한 StageDefinition ID에서 스테이지 타입 복원
            bool boss = stageType == StageType.MidBoss || stageType == StageType.FinalBoss; // 보스 노드 여부 계산
            float radius = boss ? BossNodeRadius : FutureNodeRadius; // 보스 노드 표시 크기 선택
            marker.transform.localScale = new Vector3(_boardView.TileSize * radius, FutureNodeHeight * 0.5f, _boardView.TileSize * radius); // 원판 크기 적용

            Color stageColor = GetStageColor(stageType); // 스테이지 종류 기본 색상 조회
            Color displayColor = node.Visited || node.Depth < currentDepth
                ? Color.Lerp(stageColor, VisitedColor, 0.58f)
                : Color.Lerp(stageColor, FutureDimColor, 0.42f); // 방문·미래 상태에 따른 표시 톤 적용

            Renderer renderer = marker.GetComponent<Renderer>(); // 미래·과거 노드 렌더러 조회
            renderer.sharedMaterial = CreateMaterial(displayColor); // 비클릭 노드 머티리얼 적용
            RemoveCollider(marker); // 미래·과거 노드 클릭 충돌 제거
            CreatePreviewIcon(node, stageType, renderer); // 미리보기 노드 생성과 동시에 아이콘 구성
        }

        private void CreatePreviewIcon(StageNode node, StageType stageType, Renderer signalRenderer) // 미래·방문 노드 원판 아이콘 직접 생성
        {
            GameObject host = new GameObject($"RouteMap_FullIconHost_{node.NodeId}"); // 마커 비균일 스케일과 분리된 아이콘 호스트 생성
            host.transform.SetParent(_previewRoot.transform, false); // 전체 지도 미리보기 수명에 아이콘 연결
            host.transform.localPosition = GetNodeLocalPosition(node); // 공통 좌표표 기준 미래 아이콘 배치
            host.transform.localRotation = Quaternion.identity; // 보드 정방향 유지
            host.transform.localScale = Vector3.one; // 아이콘 원본 비율 유지

            RouteNodeIconPresenter presenter = host.AddComponent<RouteNodeIconPresenter>(); // StageType별 단일 아이콘 표시기 생성
            presenter.Initialize(signalRenderer, stageType, _boardView.TileSize); // 방문·미래 노드 아이콘 연결
        }

        private void CreatePreviewPathLine(StageNode fromNode, StageNode toNode) // 두 노드를 잇는 전체 지도 경로선 생성
        { // 경로선 생성 시작
            Vector3 from = GetNodeLocalPosition(fromNode); // 출발 노드 공통 시각 위치 계산
            Vector3 to = GetNodeLocalPosition(toNode); // 도착 노드 공통 시각 위치 계산
            Vector3 delta = to - from; // 연결 방향·길이 계산
            float distance = new Vector2(delta.x, delta.z).magnitude; // 평면 연결 거리 계산

            var line = GameObject.CreatePrimitive(PrimitiveType.Cube); // 전체 그래프 경로선 생성
            line.name = "RouteMap_FullPath"; // 전체 그래프 연결선 이름 적용
            line.transform.SetParent(_previewRoot.transform, false); // 전체 경로 루트 자식 배치
            line.transform.localPosition = (from + to) * 0.5f + new Vector3(0f, PreviewPathHeight, 0f); // 두 노드 중앙에 연결선 배치
            line.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0f); // 출발→도착 방향 회전 적용
            line.transform.localScale = new Vector3(_boardView.TileSize * 0.045f, 0.012f, distance); // 기존 선택 가능 경로보다 얇은 선 크기 적용
            line.GetComponent<Renderer>().sharedMaterial = CreateMaterial(PreviewPathColor); // 전체 경로선 공통 머티리얼 적용
            RemoveCollider(line); // 경로선 포인터 충돌 제거
        }

        private static StageType ResolveStageType(StageNode node)
        {
            if (node == null) return StageType.Battle; // 빈 노드 일반 전투 fallback
            return StageDefinitionCatalog.TryParseStageType(node.StageDefinitionId, out StageType stageType)
                ? stageType
                : StageType.Battle; // StageDefinition ID 파싱 실패 일반 전투 fallback
        }

        private static Color GetStageColor(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.Elite: return new Color(0.95f, 0.45f, 0.12f); // 엘리트 주황색
                case StageType.Reward: return new Color(0.35f, 0.82f, 0.35f); // 카드 보상 녹색
                case StageType.Shop: return new Color(0.22f, 0.48f, 0.95f); // 상점 파란색
                case StageType.Event: return new Color(0.65f, 0.34f, 0.92f); // 이벤트 보라색
                case StageType.MidBoss: return new Color(0.95f, 0.25f, 0.12f); // 중간 보스 붉은 주황색
                case StageType.FinalBoss: return new Color(0.78f, 0.05f, 0.08f); // 최종 보스 진한 붉은색
                default: return new Color(0.18f, 0.82f, 0.78f); // 일반 전투 청록색
            }
        }

        private Vector3 GetNodeLocalPosition(Vector2Int cell)
        {
            return RouteMapVisualLayout.GetNodeLocalPosition(cell, _boardView.TileSize); // 현재·미래 노드 공통 사각형 배치 적용
        }

        private Vector3 GetNodeLocalPosition(StageNode node) // 노드 ID 기반 공통 시각 좌표 조회
        { // 좌표 조회 시작
            if (node != null && _nodePositions != null && _nodePositions.TryGetValue(node.NodeId, out Vector3 position)) return position; // 공유 좌표 존재 시 반환
            return node != null ? GetNodeLocalPosition(node.Position) : Vector3.zero; // 손상 지도용 논리 좌표 대체
        } // 좌표 조회 종료

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 우선 탐색
            if (shader == null) shader = Shader.Find("Standard"); // URP 셰이더 누락 시 기본 셰이더 fallback
            var material = new Material(shader); // 전체 경로 전용 런타임 머티리얼 생성
            material.color = color; // 표시 색상 적용
            _runtimeMaterials.Add(material); // 제거 대상 머티리얼 기록
            return material; // 생성 머티리얼 반환
        }

        private static string CreatePreviewKey(RouteMapState route)
        {
            return $"{route.MapSeed}|{route.CurrentNodeId}|{route.CurrentDepth}|{route.VisitedNodeIds.Count}|{route.Nodes.Count}"; // 전체 그래프 진행 상태 비교 키 생성
        }

        private static void RemoveCollider(GameObject target)
        {
            if (target == null) return; // 빈 시각 오브젝트 제외
            Collider collider = target.GetComponent<Collider>(); // 기본 프리미티브 콜라이더 조회
            if (collider != null) Object.Destroy(collider); // 지도 입력 방해 콜라이더 제거
        }

        private void DestroyPreview()
        {
            _nodePositions = null; // 전체 시각 좌표표 초기화
            if (_previewRoot != null)
            {
                Destroy(_previewRoot); // 전체 경로 시각 루트 제거
                _previewRoot = null; // 전체 경로 루트 참조 초기화
            }

            for (int i = 0; i < _runtimeMaterials.Count; i++)
            {
                if (_runtimeMaterials[i] != null) Destroy(_runtimeMaterials[i]); // 런타임 머티리얼 제거
            }

            _runtimeMaterials.Clear(); // 머티리얼 정리 목록 초기화
        }

        private void OnDestroy()
        {
            DestroyPreview(); // 씬 종료·컴포넌트 제거 시 전체 경로 시각 자원 정리
        }
    }
}
