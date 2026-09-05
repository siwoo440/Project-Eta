using System.Collections; // 초기화·킹 이동 코루틴 사용
using System.Collections.Generic; // List<T>·Dictionary<T>·HashSet<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Material·Color·Physics 사용
using UnityEngine.EventSystems; // UI 위 클릭 차단
using UnityEngine.InputSystem; // 새 Input System 마우스 입력 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Pieces; // PieceView 사용
using ProjectEta.Run; // BoardMode·StageNode 사용
using ProjectEta.UI; // 전투 카드·합성 개발 UI 숨김 사용

namespace ProjectEta.Board // 보드 경로 지도 런타임 네임스페이스
{
    [DefaultExecutionOrder(1000)] // 전투 종료·런 흐름 갱신 이후 지도 표시
    public sealed class RouteMapBoardController : MonoBehaviour // 동일 10×10 체스판의 전투→경로 지도 시각화·입력 전환
    {
        private const float NodeMarkerHeight = 0.08f; // 스테이지 노드 마커 높이
        private const float NodeMarkerRadius = 0.31f; // 스테이지 노드 마커 반지름
        private const float MapKingHeightOffset = 0.08f; // 지도 킹 바닥 높이
        private const float KingMoveDuration = 0.28f; // 지도 킹 1칸 이동 시간

        private static readonly Color NodeColor = new Color(0.18f, 0.82f, 0.78f); // 이동 가능 노드 기본 색
        private static readonly Color NodeHoverColor = new Color(0.72f, 1f, 0.96f); // 노드 마우스 오버 색
        private static readonly Color NodeSelectedColor = new Color(1f, 0.73f, 0.18f); // 선택 노드 금색
        private static readonly Color NodeDimmedColor = new Color(0.2f, 0.24f, 0.25f); // 비선택 후보 흐림 색
        private static readonly Color CurrentNodeColor = new Color(0.8f, 0.72f, 0.3f); // 현재 위치 마커 색
        private static readonly Color PathColor = new Color(0.18f, 0.46f, 0.5f); // 연결 경로 색
        private static readonly Color MapKingColor = new Color(0.95f, 0.82f, 0.3f); // 지도 킹 색

        private BattleController _battleController; // 현재 전투 컨트롤러
        private BoardView _boardView; // 기존 10×10 체스판 뷰
        private BoardInputController _boardInputController; // 기존 전투용 보드 입력
        private RunState _runState; // 현재 런 상태
        private GameObject _mapRoot; // 지도 전용 시각 오브젝트 루트
        private Transform _mapKingTransform; // 지도 전용 킹 루트
        private RouteMapNodeView _hoveredNodeView; // 현재 마우스 오버 노드
        private readonly Dictionary<string, RouteMapNodeView> _nodeViews = new Dictionary<string, RouteMapNodeView>(); // 노드 ID별 표시 객체
        private readonly List<PieceView> _hiddenPieceViews = new List<PieceView>(); // 지도 모드에서 숨긴 전투 기물
        private readonly HashSet<GameObject> _hiddenUiRoots = new HashSet<GameObject>(); // 지도 모드에서 숨긴 전투 UI 루트
        private readonly List<Material> _runtimeMaterials = new List<Material>(); // 런타임 생성 머티리얼 목록
        private bool _boardInputWasEnabled; // 지도 전환 전 전투 입력 활성 상태
        private bool _mapModeActive; // 현재 지도 표시 활성 여부
        private bool _kingMoving; // 지도 킹 이동 애니메이션 진행 여부
        private BoardState _battleBoardAtMapEntry; // 지도 진입 당시 이전 전투 BoardState 참조

        public bool IsMapModeActive => _mapModeActive; // 외부 스테이지 전환기가 지도 표시 여부 확인
        public event System.Action<StageNode> StageNodeSelected; // 킹 이동 연출 완료 뒤 실제 StageDefinition 진입 요청 이벤트

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬·Inspector 수정 없이 44일차 컨트롤러 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<RouteMapBoardController>() != null) return; // 중복 생성 차단

            var host = new GameObject("RouteMapBoardController_Day44"); // 지도 컨트롤러 호스트 생성
            host.AddComponent<RouteMapBoardController>(); // 지도 시각화·입력 컴포넌트 추가
        }

        private IEnumerator Start() // 기존 전투 보드 준비 후 런 상태 연결
        {
            const int maxWaitFrames = 240; // 최대 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 필수 객체 생성 대기
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 탐색
                _boardView = Object.FindFirstObjectByType<BoardView>(); // BoardView 탐색
                _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 기존 보드 입력 탐색

                if (_battleController != null && _battleController.RunState != null && _boardView != null && _boardView.IsBound) // 필수 상태 준비 확인
                {
                    _runState = _battleController.RunState; // 런 상태 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("44일차 RouteMapBoardController 초기화 실패: BattleController·RunState·BoardView 연결을 확인하세요."); // 초기화 실패 기록
        }

        private void Update() // 런 BoardMode 변화 감지·지도 입력 처리
        {
            if (_runState == null) return; // 런 상태 준비 전 처리 차단

            if (_runState.CurrentBoardMode == BoardMode.Map && !_mapModeActive) EnterMapMode(); // 전투 승리 후 지도 모드 진입
            else if (_runState.CurrentBoardMode == BoardMode.Battle && _mapModeActive) ExitMapMode(); // 이후 전투 복귀 대응

            if (_mapModeActive) HandleMapInput(); // 지도 모드 전용 클릭 처리
        }

        private void EnterMapMode() // 기존 체스판을 경로 지도 표시 상태로 전환
        {
            if (_runState == null || _boardView == null || !_runState.RouteMap.HasPreparedRoute) return; // 지도 데이터·보드 준비 확인

            _boardView.ClearHighlight(); // 기존 단일 선택 강조 제거
            _boardView.ClearMoveCandidates(); // 기존 이동·공격 강조 제거

            _battleBoardAtMapEntry = _runState.Board; // 지도 진입 당시 이전 전투 보드 참조 저장
            _boardInputWasEnabled = _boardInputController != null && _boardInputController.enabled; // 기존 전투 입력 상태 저장
            if (_boardInputController != null) _boardInputController.enabled = false; // 지도 중 기존 전투 입력 차단

            HideBattlePieces(); // 기존 전투 기물 화면에서 숨김
            HideBattleUi(); // 손패·덱·합성·결과 개발 UI 숨김
            BuildMapVisuals(); // 현재 노드·경로·다음 후보·지도 킹 표시
            _mapModeActive = true; // 지도 모드 활성 기록

            Debug.Log($"44일차 경로 지도 표시: Depth={_runState.RouteMap.CurrentDepth} / King={_runState.RouteMap.KingMapPosition} / Selectable={_runState.RouteMap.GetSelectableNodes().Count}"); // 지도 진입 상태 기록
        }

        private void ExitMapMode() // 지도 표시를 제거하고 기존 전투 표시 상태로 복귀
        {
            StopAllCoroutines(); // 진행 중 지도 킹 이동 정지
            _kingMoving = false; // 이동 상태 초기화
            SetHoveredNode(null); // 마우스 오버 표시 해제
            DestroyMapVisuals(); // 지도 전용 시각 오브젝트 제거

            if (_battleBoardAtMapEntry != null && _runState != null && !ReferenceEquals(_battleBoardAtMapEntry, _runState.Board)) DestroyHiddenBattlePieces(); // 새 BattleState로 교체됐다면 이전 기물 뷰 제거
            else RestoreBattlePieces(); // 같은 전투 보드라면 기존 기물 표시 복원

            RestoreBattleUi(); // 기존 전투 UI 표시 복원

            if (_boardInputController != null) _boardInputController.enabled = _boardInputWasEnabled; // 기존 보드 입력 상태 복원

            _battleBoardAtMapEntry = null; // 이전 전투 보드 참조 정리
            _mapModeActive = false; // 지도 모드 비활성 기록
        }

        private void BuildMapVisuals() // RouteMapState를 실제 10×10 보드 위에 표시
        {
            DestroyMapVisuals(); // 이전 지도 표시 제거

            _mapRoot = new GameObject("RouteMapVisuals_Day44"); // 지도 시각 루트 생성
            _mapRoot.transform.SetParent(_boardView.transform, false); // 기존 보드 로컬 좌표계 재사용

            var currentNode = _runState.RouteMap.CurrentNode; // 현재 킹 위치 노드 조회
            if (currentNode != null) CreateCurrentNodeMarker(currentNode.Position); // 현재 위치 마커 생성

            var selectableNodes = _runState.RouteMap.GetSelectableNodes(); // 이동 가능한 다음 스테이지 후보 조회
            for (int i = 0; i < selectableNodes.Count; i++) // 후보 노드 순회
            {
                var node = selectableNodes[i]; // 현재 후보 노드 조회
                CreatePathLine(_runState.RouteMap.KingMapPosition, node.Position); // 현재 위치→후보 연결선 생성
                CreateSelectableNodeMarker(node); // 클릭 가능한 후보 마커 생성
            }

            CreateMapKing(_runState.RouteMap.KingMapPosition); // 지도 전용 킹 표시
        }

        private void CreateCurrentNodeMarker(Vector2Int cell) // 현재 킹 위치 바닥 마커 생성
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // 원형 현재 위치 마커 생성
            marker.name = "RouteMap_CurrentNode"; // 계층창 이름 지정
            marker.transform.SetParent(_mapRoot.transform, false); // 지도 루트 자식 배치
            marker.transform.localPosition = BoardView.BoardToLocalPosition(cell, _boardView.TileSize) + new Vector3(0f, 0.025f, 0f); // 현재 보드 좌표에 배치
            marker.transform.localScale = new Vector3(_boardView.TileSize * 0.29f, 0.018f, _boardView.TileSize * 0.29f); // 낮은 원판 형태 적용

            var renderer = marker.GetComponent<Renderer>(); // 현재 위치 렌더러 확보
            renderer.sharedMaterial = CreateMaterial(CurrentNodeColor); // 현재 위치 색상 적용

            RemoveCollider(marker); // 현재 위치 마커 클릭 충돌 제거
        }

        private void CreateSelectableNodeMarker(StageNode node) // 다음 스테이지 클릭 마커 생성
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // 원형 스테이지 노드 생성
            marker.name = $"RouteMap_Node_{node.NodeId}"; // 노드 ID 기반 이름 지정
            marker.transform.SetParent(_mapRoot.transform, false); // 지도 루트 자식 배치
            marker.transform.localPosition = BoardView.BoardToLocalPosition(node.Position, _boardView.TileSize) + new Vector3(0f, NodeMarkerHeight * 0.5f, 0f); // 노드 보드 좌표 배치
            marker.transform.localScale = new Vector3(_boardView.TileSize * NodeMarkerRadius, NodeMarkerHeight * 0.5f, _boardView.TileSize * NodeMarkerRadius); // 클릭 가능한 원판 크기 적용

            StageDefinition stageDefinition = StageDefinitionCatalog.Resolve(node.StageDefinitionId, node.Depth); // 노드의 실제 스테이지 타입 조회
            Color stageColor = GetStageColor(stageDefinition != null ? stageDefinition.StageType : StageType.Battle); // 타입별 기본 노드 색상 선택
            var renderer = marker.GetComponent<Renderer>(); // 노드 렌더러 확보
            renderer.sharedMaterial = CreateMaterial(stageColor); // 스테이지 타입별 선택 가능 색상 적용

            var nodeView = marker.AddComponent<RouteMapNodeView>(); // 노드 식별·상태 표시 컴포넌트 추가
            nodeView.Initialize(node.NodeId, renderer, stageColor, NodeHoverColor, NodeSelectedColor, NodeDimmedColor); // 노드 ID·타입 색상 연결
            _nodeViews[node.NodeId] = nodeView; // 노드 ID별 표시 객체 등록
        }

        private void CreatePathLine(Vector2Int fromCell, Vector2Int toCell) // 현재 위치와 다음 노드를 잇는 지도 경로선 생성
        {
            Vector3 from = BoardView.BoardToLocalPosition(fromCell, _boardView.TileSize); // 시작 보드 로컬 위치 계산
            Vector3 to = BoardView.BoardToLocalPosition(toCell, _boardView.TileSize); // 도착 보드 로컬 위치 계산
            Vector3 delta = to - from; // 연결 방향·거리 계산
            float distance = new Vector2(delta.x, delta.z).magnitude; // 평면 거리 계산

            var line = GameObject.CreatePrimitive(PrimitiveType.Cube); // 경로선 큐브 생성
            line.name = "RouteMap_Path"; // 계층창 이름 지정
            line.transform.SetParent(_mapRoot.transform, false); // 지도 루트 자식 배치
            line.transform.localPosition = (from + to) * 0.5f + new Vector3(0f, 0.018f, 0f); // 두 노드 중간 위치 배치
            line.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0f); // 경로 방향 회전 적용
            line.transform.localScale = new Vector3(_boardView.TileSize * 0.08f, 0.018f, distance); // 얇은 연결선 크기 적용

            var renderer = line.GetComponent<Renderer>(); // 경로 렌더러 확보
            renderer.sharedMaterial = CreateMaterial(PathColor); // 경로 색상 적용

            RemoveCollider(line); // 경로선 클릭 충돌 제거
        }

        private void CreateMapKing(Vector2Int cell) // 전투 PieceRuntimeState와 분리된 지도 전용 킹 생성
        {
            var kingRoot = new GameObject("RouteMap_King"); // 지도 킹 루트 생성
            kingRoot.transform.SetParent(_mapRoot.transform, false); // 지도 루트 자식 배치
            kingRoot.transform.localPosition = BoardView.BoardToLocalPosition(cell, _boardView.TileSize) + new Vector3(0f, MapKingHeightOffset, 0f); // 현재 지도 좌표 배치
            _mapKingTransform = kingRoot.transform; // 이동 애니메이션용 참조 저장

            var material = CreateMaterial(MapKingColor); // 지도 킹 공통 머티리얼 생성
            float scale = Mathf.Max(0.65f, _boardView.TileSize); // 타일 크기 기반 모델 스케일 계산

            CreateKingPart(kingRoot.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f) * scale, new Vector3(0.34f, 0.04f, 0.34f) * scale, material); // 킹 받침 생성
            CreateKingPart(kingRoot.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f) * scale, new Vector3(0.18f, 0.30f, 0.18f) * scale, material); // 킹 몸통 생성
            CreateKingPart(kingRoot.transform, PrimitiveType.Sphere, new Vector3(0f, 0.80f, 0f) * scale, new Vector3(0.22f, 0.22f, 0.22f) * scale, material); // 킹 머리 생성
            CreateKingPart(kingRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.01f, 0f) * scale, new Vector3(0.055f, 0.19f, 0.055f) * scale, material); // 십자가 세로 생성
            CreateKingPart(kingRoot.transform, PrimitiveType.Cube, new Vector3(0f, 1.01f, 0f) * scale, new Vector3(0.17f, 0.055f, 0.055f) * scale, material); // 십자가 가로 생성
        }

        private void HandleMapInput() // MapMode 전용 노드 마우스 선택 처리
        {
            if (_runState == null || _runState.RouteMap.HasSelectedNode || _kingMoving) // 선택 완료·이동 중 입력 차단
            {
                SetHoveredNode(null); // 오버 표시 해제
                return; // 추가 입력 종료
            }

            if (Mouse.current == null) return; // 마우스 장치 누락 방어

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) // UI 위 포인터 확인
            {
                SetHoveredNode(null); // 지도 마커 오버 표시 해제
                return; // UI 클릭과 지도 클릭 중복 차단
            }

            var nodeView = RaycastNodeView(); // 현재 포인터 아래 지도 노드 조회
            SetHoveredNode(nodeView); // 마우스 오버 색상 갱신

            if (nodeView != null && Mouse.current.leftButton.wasPressedThisFrame) TrySelectNode(nodeView.NodeId); // 좌클릭 스테이지 선택
        }

        private RouteMapNodeView RaycastNodeView() // 마우스 위치에서 지도 노드 마커 검색
        {
            Camera targetCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(); // 메인 카메라 우선 확보
            if (targetCamera == null) return null; // 카메라 누락 처리

            Vector2 pointerPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            Ray ray = targetCamera.ScreenPointToRay(pointerPosition); // 화면 좌표→월드 레이 생성
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f); // 보드·마커 전체 충돌 검색

            for (int i = 0; i < hits.Length; i++) // 충돌 결과 순회
            {
                var nodeView = hits[i].collider.GetComponent<RouteMapNodeView>(); // 노드 마커 컴포넌트 조회
                if (nodeView != null) return nodeView; // 지도 노드 우선 반환
            }

            return null; // 노드 마커 없음 반환
        }

        private void TrySelectNode(string nodeId) // 클릭한 노드로 지도 킹 이동 시도
        {
            var targetNode = _runState.RouteMap.FindNode(nodeId); // StageNode 데이터 조회
            if (!_runState.RouteMap.CanMoveTo(targetNode)) return; // 연결·킹 1칸 규칙 위반 차단

            Vector3 startLocalPosition = _mapKingTransform.localPosition; // 현재 지도 킹 위치 저장
            Vector3 targetLocalPosition = BoardView.BoardToLocalPosition(targetNode.Position, _boardView.TileSize) + new Vector3(0f, MapKingHeightOffset, 0f); // 목표 노드 킹 위치 계산

            if (!_runState.RouteMap.TryMoveKingTo(nodeId)) return; // 상태 데이터 이동·선택 반영

            ApplyNodeSelectionVisuals(nodeId); // 선택 노드 강조·나머지 후보 흐림 적용
            SetHoveredNode(null); // 오버 상태 정리
            StartCoroutine(AnimateKingMove(startLocalPosition, targetLocalPosition, targetNode)); // 지도 킹 1칸 이동 연출 시작
        }

        private IEnumerator AnimateKingMove(Vector3 start, Vector3 target, StageNode targetNode) // 지도 킹 부드러운 1칸 이동
        {
            _kingMoving = true; // 이동 중 입력 차단
            float elapsed = 0f; // 경과 시간 초기화

            while (elapsed < KingMoveDuration) // 지정 시간 동안 위치 보간
            {
                elapsed += Time.unscaledDeltaTime; // 게임 시간 배율과 무관한 이동 시간 누적
                float t = Mathf.Clamp01(elapsed / KingMoveDuration); // 0~1 이동 비율 계산
                float smoothT = t * t * (3f - 2f * t); // SmoothStep 보간값 계산
                if (_mapKingTransform != null) _mapKingTransform.localPosition = Vector3.Lerp(start, target, smoothT); // 킹 위치 보간 적용
                yield return null; // 다음 프레임 대기
            }

            if (_mapKingTransform != null) _mapKingTransform.localPosition = target; // 최종 좌표 오차 제거
            _kingMoving = false; // 이동 상태 종료

            Debug.Log($"45일차 다음 스테이지 선택 완료: Node={targetNode.NodeId} / Depth={targetNode.Depth} / Position={targetNode.Position} / StageDefinition={targetNode.StageDefinitionId}"); // 실제 스테이지 진입 직전 선택 결과 기록
            StageNodeSelected?.Invoke(targetNode); // 킹 이동이 끝난 뒤 StageDefinition 기반 판 전환 요청
        }

        public void RefreshMapVisuals() // 비전투 스테이지 완료 후 같은 MapMode에서 다음 분기 표시 갱신
        {
            if (!_mapModeActive || _runState == null || _runState.CurrentBoardMode != BoardMode.Map) return; // 지도 상태가 아니면 갱신하지 않음
            BuildMapVisuals(); // 갱신된 RouteMapState 기준 노드·경로·킹 다시 생성
            Debug.Log($"45일차 경로 지도 갱신: Depth={_runState.RouteMap.CurrentDepth} / Selectable={_runState.RouteMap.GetSelectableNodes().Count}"); // 갱신 결과 기록
        }

        private void ApplyNodeSelectionVisuals(string selectedNodeId) // 선택한 다음 스테이지를 판 위에 고정 표시
        {
            foreach (var pair in _nodeViews) // 모든 후보 노드 표시 순회
            {
                bool selected = pair.Key == selectedNodeId; // 선택 노드 여부 계산
                pair.Value.SetSelectionState(selected, !selected); // 선택=금색·나머지=흐림 적용
            }
        }

        private void SetHoveredNode(RouteMapNodeView nodeView) // 현재 마우스 오버 노드 교체
        {
            if (_hoveredNodeView == nodeView) return; // 동일 노드면 변경 없음
            if (_hoveredNodeView != null) _hoveredNodeView.SetHovered(false); // 이전 노드 오버 해제
            _hoveredNodeView = nodeView; // 새 오버 노드 저장
            if (_hoveredNodeView != null) _hoveredNodeView.SetHovered(true); // 새 노드 오버 적용
        }

        private void HideBattlePieces() // 전투 PieceView를 삭제하지 않고 화면에서 임시 숨김
        {
            _hiddenPieceViews.Clear(); // 이전 숨김 목록 초기화
            var pieceViews = Object.FindObjectsByType<PieceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); // 현재 활성 전투 기물 전체 조회

            for (int i = 0; i < pieceViews.Length; i++) // 전투 기물 순회
            {
                var pieceView = pieceViews[i]; // 현재 PieceView 조회
                if (pieceView == null || !pieceView.gameObject.activeSelf) continue; // 빈·이미 비활성 기물 제외
                _hiddenPieceViews.Add(pieceView); // 복원 대상 기록
                pieceView.gameObject.SetActive(false); // 지도 모드 동안 전투 기물 숨김
            }
        }

        private void RestoreBattlePieces() // 지도 진입 전 활성 전투 기물 표시 복원
        {
            for (int i = 0; i < _hiddenPieceViews.Count; i++) // 숨긴 기물 순회
            {
                var pieceView = _hiddenPieceViews[i]; // 현재 복원 대상 조회
                if (pieceView != null) pieceView.gameObject.SetActive(true); // 남아 있는 기물 표시 복원
            }

            _hiddenPieceViews.Clear(); // 복원 목록 초기화
        }

        private void DestroyHiddenBattlePieces() // 새 BattleState 진입 시 이전 전투 PieceView를 복원하지 않고 제거
        {
            for (int i = 0; i < _hiddenPieceViews.Count; i++) // 이전 전투 기물 뷰 순회
            {
                var pieceView = _hiddenPieceViews[i]; // 현재 제거 대상 조회
                if (pieceView != null) Destroy(pieceView.gameObject); // 새 보드에 이전 기물이 다시 나타나지 않도록 제거
            }

            _hiddenPieceViews.Clear(); // 제거 목록 초기화
        }

        private void HideBattleUi() // 지도 선택에 불필요한 기존 전투 UI 숨김
        {
            _hiddenUiRoots.Clear(); // 이전 UI 복원 목록 초기화
            HideUiRoot(Object.FindFirstObjectByType<HandUI>()); // 손패 UI 숨김
            HideUiRoot(Object.FindFirstObjectByType<DeckPanelUI>()); // 덱 UI 숨김
            HideUiRoot(Object.FindFirstObjectByType<FusionPanelUI>()); // 합성 UI 숨김
            HideUiRoot(Object.FindFirstObjectByType<DebugBattleResultButtons>()); // 승리·패배 개발 버튼 숨김
            HideUiRoot(Object.FindFirstObjectByType<DebugCombatSpeedButtons>()); // 전투 배속 개발 버튼 숨김
        }

        private void HideUiRoot(Component component) // UI 컴포넌트의 Canvas만 숨기고 시스템 호스트는 활성 상태로 유지
        {
            if (component == null || !component.gameObject.activeInHierarchy) return; // 빈·비활성 컴포넌트 제외

            var canvases = component.GetComponentsInChildren<Canvas>(true); // BattleController 같은 공용 호스트 아래 실제 Canvas 조회

            for (int i = 0; i < canvases.Length; i++) // 조회된 Canvas 순회
            {
                var canvas = canvases[i]; // 현재 Canvas 조회
                if (canvas == null || !canvas.gameObject.activeSelf) continue; // 빈·이미 숨겨진 Canvas 제외
                if (_hiddenUiRoots.Add(canvas.gameObject)) canvas.gameObject.SetActive(false); // Canvas만 숨기고 BattleController 호스트는 유지
            }
        }

        private void RestoreBattleUi() // 지도 진입 전 활성 UI 표시 복원
        {
            foreach (var uiRoot in _hiddenUiRoots) // 숨긴 UI 루트 순회
            {
                if (uiRoot != null) uiRoot.SetActive(true); // 남아 있는 UI 복원
            }

            _hiddenUiRoots.Clear(); // 복원 목록 초기화
        }

        private static Color GetStageColor(StageType stageType) // 지도에서 스테이지 종류를 즉시 구분할 기본 색상 선택
        {
            switch (stageType) // 스테이지 타입별 색상 분기
            {
                case StageType.Elite: return new Color(0.95f, 0.45f, 0.12f); // 엘리트 주황색
                case StageType.Reward: return new Color(0.35f, 0.82f, 0.35f); // 보상 녹색
                case StageType.Shop: return new Color(0.22f, 0.48f, 0.95f); // 상점 파란색
                case StageType.Event: return new Color(0.65f, 0.34f, 0.92f); // 이벤트 보라색
                case StageType.MidBoss: return new Color(0.95f, 0.25f, 0.12f); // 중간 보스 붉은 주황색
                case StageType.FinalBoss: return new Color(0.78f, 0.05f, 0.08f); // 최종 보스 진한 붉은색
                default: return NodeColor; // 일반 전투 기존 청록색
            }
        }

        private Material CreateMaterial(Color color) // 지도 전용 단색 URP 머티리얼 생성
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 검색
            if (shader == null) shader = Shader.Find("Standard"); // URP 검색 실패 시 기본 셰이더 대체
            var material = new Material(shader); // 런타임 머티리얼 생성
            material.color = color; // 기본 색상 지정
            _runtimeMaterials.Add(material); // 종료 시 정리 대상 기록
            return material; // 생성 머티리얼 반환
        }

        private void CreateKingPart(Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material) // 지도 킹 프리미티브 파츠 생성
        {
            var part = GameObject.CreatePrimitive(primitiveType); // 지정 프리미티브 생성
            part.name = $"MapKing_{primitiveType}"; // 파츠 계층창 이름 지정
            part.transform.SetParent(parent, false); // 킹 루트 자식 배치
            part.transform.localPosition = localPosition; // 파츠 로컬 위치 적용
            part.transform.localScale = localScale; // 파츠 크기 적용
            part.GetComponent<Renderer>().sharedMaterial = material; // 공통 킹 머티리얼 적용
            RemoveCollider(part); // 지도 노드 클릭 방해 콜라이더 제거
        }

        private static void RemoveCollider(GameObject target) // 장식용 프리미티브 콜라이더 제거
        {
            var collider = target.GetComponent<Collider>(); // 기본 프리미티브 콜라이더 조회
            if (collider != null) Object.Destroy(collider); // 런타임 클릭 방해 요소 제거
        }

        private void DestroyMapVisuals() // 지도 전용 시각·머티리얼 정리
        {
            _nodeViews.Clear(); // 노드 표시 사전 초기화
            _mapKingTransform = null; // 지도 킹 참조 초기화

            if (_mapRoot != null) // 지도 루트 존재 확인
            {
                Destroy(_mapRoot); // 지도 전용 오브젝트 전체 제거
                _mapRoot = null; // 루트 참조 초기화
            }

            for (int i = 0; i < _runtimeMaterials.Count; i++) // 생성 머티리얼 순회
            {
                if (_runtimeMaterials[i] != null) Destroy(_runtimeMaterials[i]); // 런타임 머티리얼 제거
            }

            _runtimeMaterials.Clear(); // 머티리얼 목록 초기화
        }

        private void OnDestroy() // 씬 종료 시 지도 전용 자원 정리
        {
            SetHoveredNode(null); // 오버 상태 정리
            DestroyMapVisuals(); // 지도 시각·머티리얼 정리
        }
    }
}
