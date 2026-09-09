using System.Collections.Generic; // 노드별 Host·Renderer·Material 관리
using UnityEngine; // MonoBehaviour·GameObject·Renderer 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Run; // RunState·BoardMode·StageType 사용

namespace ProjectEta.Board
{
    [DefaultExecutionOrder(1220)]
    public sealed class Day66RouteNodeBuildingDecorator : MonoBehaviour
    {
        private const float SyncInterval = 0.10f; // 경로 데이터 재동기화 주기
        private const float PedestalHeight = 0.065f; // 건물 기단 실제 높이
        private const float PedestalWidth = 0.72f; // 한 타일 안 건물 기단 폭
        private const float PedestalYOffset = 0.035f; // 체스판 위 기단 중심 높이

        private readonly Dictionary<string, GameObject> _buildingHosts = new Dictionary<string, GameObject>(); // 노드 ID별 건물 Host
        private readonly Dictionary<string, Renderer> _signalRenderers = new Dictionary<string, Renderer>(); // 노드 ID별 상태 신호 렌더러
        private readonly List<Material> _runtimeMaterials = new List<Material>(); // 기단 런타임 머티리얼 목록
        private BattleController _battleController; // 현재 RunState 제공 전투 컨트롤러
        private BoardView _boardView; // 건물 좌표 기준 보드
        private RunState _runState; // 현재 런 전체 상태
        private GameObject _buildingRoot; // 지도 건물 전용 Root
        private float _nextSyncTime; // 다음 경로 동기화 시간

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 Battle 씬 여부 확인
            {
                return; // 다른 씬 자동 생성 차단
            }

            if (Object.FindFirstObjectByType<Day66RouteNodeBuildingDecorator>() != null) // 기존 관리자 확인
            {
                return; // 중복 관리자 생성 차단
            }

            GameObject host = new GameObject("Day66RouteNodeBuildingDecorator"); // 66일차 지도 건물 관리자 생성
            host.AddComponent<Day66RouteNodeBuildingDecorator>(); // 런타임 건물 관리자 추가
        }

        private void Update()
        {
            ResolveBindings(); // 현재 전투·보드·런 상태 연결

            if (_runState == null || _boardView == null) // 필수 참조 준비 여부 확인
            {
                SetBuildingsVisible(false); // 준비 전 건물 숨김
                return; // 다음 프레임 재시도
            }

            bool visible = Day66MapPresentationRules.ShouldShowRouteBuildings(_runState.CurrentBoardMode, _runState.CurrentFlowPhase); // 현재 화면 건물 표시 여부 판정

            if (!visible) // 지도 건물 숨김 상태 확인
            {
                SetBuildingsVisible(false); // 전투·Shop·Event에서 건물 숨김
                return; // 숨김 상태 경로 재구성 생략
            }

            EnsureBuildingRoot(); // 지도 건물 Root 보장
            SetBuildingsVisible(true); // Map·Reward 지도 배경에서 건물 표시

            if (Time.unscaledTime >= _nextSyncTime) // 경로 재구성 시점 확인
            {
                _nextSyncTime = Time.unscaledTime + SyncInterval; // 다음 동기화 시간 예약
                ReconcileBuildingsFromRoute(); // RouteMap.Nodes 기준 모든 건물 보장
            }

            SyncInteractiveSignals(); // Hover·선택 상태 색상 연동
        }

        private void ResolveBindings()
        {
            if (_battleController == null) // 전투 컨트롤러 캐시 확인
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
            }

            if (_boardView == null) // 보드 뷰 캐시 확인
            {
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 현재 BoardView 탐색
            }

            if (_battleController == null || _battleController.RunState == null) // 런 상태 준비 여부 확인
            {
                return; // 연결 가능한 시점까지 대기
            }

            if (_runState != _battleController.RunState) // RunState 교체 여부 확인
            {
                _runState = _battleController.RunState; // 새 RunState 연결
                ClearBuildings(); // 이전 런 건물 제거
                _nextSyncTime = 0f; // 새 런 즉시 경로 구성
            }
        }

        private void EnsureBuildingRoot()
        {
            if (_buildingRoot != null || _boardView == null) // 기존 Root 또는 보드 누락 확인
            {
                return; // 중복 Root 생성 차단
            }

            Transform existing = _boardView.transform.Find("Day66RouteBuildingsRoot"); // 이전 런타임 Root 잔존 여부 확인

            if (existing != null) // 잔존 Root 발견 여부 확인
            {
                DestroyRuntimeObject(existing.gameObject); // 오래된 건물 Root 제거
            }

            _buildingRoot = new GameObject("Day66RouteBuildingsRoot"); // 새 건물 전용 Root 생성
            _buildingRoot.transform.SetParent(_boardView.transform, false); // BoardView 좌표계 직접 연결
            _buildingRoot.transform.localPosition = Vector3.zero; // 보드 원점 정렬
            _buildingRoot.transform.localRotation = Quaternion.identity; // 보드 방향 정렬
            _buildingRoot.transform.localScale = Vector3.one; // 좌표 왜곡 방지
        }

        private void ReconcileBuildingsFromRoute()
        {
            if (_buildingRoot == null || _runState == null || _boardView == null) // 건물 구성 준비 여부 확인
            {
                return; // 경로 구성 차단
            }

            IReadOnlyList<Day66RouteBuildingSpec> specs = Day66RouteBuildingPlan.Build(_runState.RouteMap); // 실제 RouteMap 전체 노드 건물 계획 생성
            var liveIds = new HashSet<string>(); // 현재 유효 노드 ID 집합 생성

            for (int i = 0; i < specs.Count; i++) // 전체 노드 계획 순회
            {
                Day66RouteBuildingSpec spec = specs[i]; // 현재 노드 건물 계획 조회
                liveIds.Add(spec.NodeId); // 유효 노드 ID 등록
                EnsureBuilding(spec); // 노드 좌표에 상세 건물 보장
            }

            RemoveStaleBuildings(liveIds); // 경로에서 사라진 건물 제거
        }

        private void EnsureBuilding(Day66RouteBuildingSpec spec)
        {
            if (!_buildingHosts.TryGetValue(spec.NodeId, out GameObject host) || host == null) // 기존 건물 Host 조회
            {
                host = CreateBuildingHost(spec); // 해당 노드 새 상세 건물 생성
                _buildingHosts[spec.NodeId] = host; // Host 캐시에 등록
            }

            host.transform.localPosition = BoardView.BoardToLocalPosition(spec.Position, _boardView.TileSize); // 노드의 실제 체스판 좌표에 Host 고정
            host.transform.localRotation = Quaternion.identity; // 보드 정방향 유지
            host.transform.localScale = Vector3.one; // 건물 모델 비율 왜곡 차단
        }

        private GameObject CreateBuildingHost(Day66RouteBuildingSpec spec)
        {
            GameObject host = new GameObject($"Day66BuildingHost_{spec.NodeId}"); // 노드별 독립 건물 Host 생성
            host.transform.SetParent(_buildingRoot.transform, false); // 건물 Root 자식 연결
            host.transform.localPosition = BoardView.BoardToLocalPosition(spec.Position, _boardView.TileSize); // 모델 생성 전에 최종 좌표 고정
            host.transform.localRotation = Quaternion.identity; // 모델 생성 전 방향 고정
            host.transform.localScale = Vector3.one; // Host 자체 스케일 고정

            Renderer signalRenderer = CreatePedestal(host.transform, spec.StageType); // 독립 기단·상태 신호 생성
            _signalRenderers[spec.NodeId] = signalRenderer; // Hover 연동용 신호 렌더러 저장

            Day66RouteNodeBuildingModel model = host.AddComponent<Day66RouteNodeBuildingModel>(); // 상세 다중 파츠 건물 모델 추가
            model.Initialize(signalRenderer, spec.StageType, _boardView.TileSize); // Host 자식에 건물 즉시 구성
            return host; // 완성된 건물 Host 반환
        }

        private Renderer CreatePedestal(Transform parent, StageType stageType)
        {
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // 건물 아래 원형 기단 생성
            pedestal.name = "BuildingPedestal"; // Hierarchy 식별 이름 적용
            pedestal.transform.SetParent(parent, false); // 노드 Host 자식 연결
            pedestal.transform.localPosition = new Vector3(0f, PedestalYOffset, 0f); // 체스판 바로 위 배치
            pedestal.transform.localRotation = Quaternion.identity; // 보드 평면 정렬
            pedestal.transform.localScale = new Vector3(_boardView.TileSize * PedestalWidth, PedestalHeight, _boardView.TileSize * PedestalWidth); // 건물 아래 넓은 기단 적용

            Collider collider = pedestal.GetComponent<Collider>(); // 기본 Cylinder 콜라이더 조회

            if (collider != null) // 콜라이더 존재 확인
            {
                DestroyRuntimeObject(collider); // 기존 노드 클릭 방해 제거
            }

            Renderer renderer = pedestal.GetComponent<Renderer>(); // 기단 렌더러 확보
            Material material = CreateMaterial(GetStageSignalColor(stageType)); // 스테이지별 기단 색상 생성

            if (renderer != null && material != null) // 렌더러·머티리얼 준비 확인
            {
                renderer.sharedMaterial = material; // 기단 머티리얼 적용
            }

            return renderer; // 건물 Tint 신호 렌더러 반환
        }

        private void SyncInteractiveSignals()
        {
            if (_buildingRoot == null || !_buildingRoot.activeSelf) // 건물 표시 상태 확인
            {
                return; // 숨김 중 색상 동기화 생략
            }

            RouteMapNodeView[] views = Object.FindObjectsByType<RouteMapNodeView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); // 기존 선택 가능 노드 View 조회

            for (int i = 0; i < views.Length; i++) // 선택 가능 노드 순회
            {
                RouteMapNodeView view = views[i]; // 현재 노드 View 조회

                if (view == null || string.IsNullOrWhiteSpace(view.NodeId)) // 유효한 노드 ID 확인
                {
                    continue; // 잘못된 View 제외
                }

                if (!_signalRenderers.TryGetValue(view.NodeId, out Renderer signalRenderer) || signalRenderer == null) // 대응 건물 기단 확인
                {
                    continue; // 아직 생성 전 노드 제외
                }

                Renderer sourceRenderer = view.GetComponent<Renderer>(); // 기존 Hover·Selected 상태 렌더러 조회

                if (sourceRenderer == null || sourceRenderer.sharedMaterial == null || signalRenderer.sharedMaterial == null) // 색상 복사 준비 확인
                {
                    continue; // 누락 렌더러 제외
                }

                signalRenderer.sharedMaterial.color = sourceRenderer.material.color; // 기존 노드 상태 색상을 건물 기단에 전달
            }
        }

        private void RemoveStaleBuildings(HashSet<string> liveIds)
        {
            var staleIds = new List<string>(); // 제거 대상 노드 ID 목록 생성

            foreach (KeyValuePair<string, GameObject> pair in _buildingHosts) // 현재 건물 캐시 순회
            {
                if (!liveIds.Contains(pair.Key)) // 현재 RouteMap 포함 여부 확인
                {
                    staleIds.Add(pair.Key); // 제거 대상 등록
                }
            }

            for (int i = 0; i < staleIds.Count; i++) // 제거 대상 순회
            {
                string nodeId = staleIds[i]; // 현재 제거 노드 ID 조회

                if (_buildingHosts.TryGetValue(nodeId, out GameObject host) && host != null) // 실제 건물 Host 확인
                {
                    DestroyRuntimeObject(host); // 노드 건물 전체 제거
                }

                _buildingHosts.Remove(nodeId); // Host 캐시 제거
                _signalRenderers.Remove(nodeId); // 신호 렌더러 캐시 제거
            }
        }

        private void SetBuildingsVisible(bool visible)
        {
            if (_buildingRoot != null && _buildingRoot.activeSelf != visible) // 실제 표시 상태 변경 필요 여부 확인
            {
                _buildingRoot.SetActive(visible); // 건물 Root 표시 상태 적용
            }
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 우선 탐색

            if (shader == null) // URP 셰이더 누락 확인
            {
                shader = Shader.Find("Standard"); // Standard 셰이더 fallback 탐색
            }

            if (shader == null) // 사용 가능한 셰이더 최종 확인
            {
                return null; // Primitive 기본 머티리얼 유지
            }

            Material material = new Material(shader); // 기단 런타임 머티리얼 생성
            material.color = color; // 스테이지 색상 적용
            _runtimeMaterials.Add(material); // 제거 대상 목록 등록
            return material; // 생성 머티리얼 반환
        }

        private static Color GetStageSignalColor(StageType stageType)
        {
            switch (stageType) // 스테이지 타입별 지도 기단 색상 선택
            {
                case StageType.Elite:
                    return new Color(0.92f, 0.38f, 0.16f); // 엘리트 주황색
                case StageType.Reward:
                    return new Color(0.50f, 0.90f, 0.36f); // 보상 녹색
                case StageType.Shop:
                    return new Color(0.28f, 0.58f, 1.00f); // 상점 파란색
                case StageType.Event:
                    return new Color(0.74f, 0.38f, 1.00f); // 이벤트 보라색
                case StageType.MidBoss:
                    return new Color(1.00f, 0.30f, 0.12f); // 중간 보스 붉은 주황색
                case StageType.FinalBoss:
                    return new Color(0.92f, 0.08f, 0.10f); // 최종 보스 진한 붉은색
                default:
                    return new Color(0.22f, 0.88f, 0.82f); // 일반 전투 청록색
            }
        }

        private void ClearBuildings()
        {
            if (_buildingRoot != null) // 기존 건물 Root 확인
            {
                DestroyRuntimeObject(_buildingRoot); // 건물 계층 전체 제거
                _buildingRoot = null; // Root 참조 초기화
            }

            _buildingHosts.Clear(); // Host 캐시 초기화
            _signalRenderers.Clear(); // 신호 렌더러 캐시 초기화

            for (int i = 0; i < _runtimeMaterials.Count; i++) // 생성 기단 머티리얼 순회
            {
                if (_runtimeMaterials[i] != null) // 남은 머티리얼 확인
                {
                    DestroyRuntimeObject(_runtimeMaterials[i]); // 런타임 머티리얼 해제
                }
            }

            _runtimeMaterials.Clear(); // 머티리얼 목록 초기화
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null) // 제거 대상 확인
            {
                return; // 빈 대상 제거 생략
            }

            if (Application.isPlaying) // Play Mode 여부 확인
            {
                Object.Destroy(target); // 프레임 종료 시 안전 제거
            }
            else
            {
                Object.DestroyImmediate(target); // EditMode 즉시 제거
            }
        }

        private void OnDestroy()
        {
            ClearBuildings(); // 건물·기단 머티리얼 정리
        }
    }
}
