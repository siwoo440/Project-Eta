using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // UI 위 포인터 확인
using UnityEngine.InputSystem; // Mouse 입력 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using UnityEngine.UI; // Canvas·Image·Text 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // RouteMapNodeView·BoardView 사용
using ProjectEta.Run; // RunState·StageDefinition 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1240)]
    public sealed class Day66RouteMapUI : MonoBehaviour
    {
        private BattleController _battleController; // 현재 런 상태 제공 전투 컨트롤러
        private BoardView _boardView; // 전체 경로 시각 루트 조회용 보드
        private RunState _runState; // 현재 런 상태
        private Canvas _canvas; // RouteMap 전용 Screen Space Canvas
        private GameObject _headerRoot; // 지도 상단 HUD 루트
        private Text _titleText; // 지도 제목
        private Text _progressText; // 현재 깊이·안내 문구
        private Text _legendText; // StageType 범례
        private GameObject _hoverRoot; // 노드 Hover 정보 패널
        private Text _hoverTitleText; // Hover 노드 제목
        private Text _hoverBodyText; // Hover 노드 상세 설명
        private string _lastHoverNodeId = string.Empty; // 이전 Hover 노드 ID
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<Day66RouteMapUI>() != null) return; // 중복 생성 차단

            GameObject host = new GameObject("Day66RouteMapUI"); // 66일차 RouteMap UI 호스트 생성
            host.AddComponent<Day66RouteMapUI>(); // 정식 지도 HUD 추가
        }

        private void Awake()
        {
            EnsureUI(); // 지도 HUD 선생성
        }

        private void Update()
        {
            ResolveBindings(); // BattleController·RunState 연결 보장
            if (_runState == null) return; // 런 준비 전 처리 차단

            bool mapSelectionActive = _runState.CurrentFlowPhase == RunFlowPhase.Map && _runState.RouteMap.HasPreparedRoute; // 실제 경로 선택 상태 확인
            ApplyFullGraphVisibility(mapSelectionActive); // Shop·Event·Reward 중 미래 지도 겹침 차단

            if (!mapSelectionActive)
            {
                HideMapUI(); // 경로 선택 외 HUD 숨김
                return;
            }

            ShowMapUI(); // RouteMap HUD 표시
            RefreshHeader(); // 현재 깊이 정보 갱신
            RefreshHoveredNode(); // 마우스 노드 상세 정보 갱신
        }

        private void ResolveBindings()
        {
            if (_battleController == null) _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 지연 탐색
            if (_boardView == null) _boardView = Object.FindFirstObjectByType<BoardView>(); // BoardView 지연 탐색
            if (_battleController == null || _battleController.RunState == null) return; // 런 준비 전 종료
            _runState = _battleController.RunState; // 현재 RunState 연결
        }

        private void RefreshHeader()
        {
            int currentPhase = RunPhaseProgressService.GetCurrentPhase(_runState); // 현재 1~5 RouteMap 페이즈 조회
            _titleText.text = "ROUTE MAP"; // 지도 화면 제목 표시
            _progressText.text = $"PHASE {currentPhase}/{RunPhaseProgressService.TotalPhases}    ·    STAGE {_runState.RouteMap.CurrentDepth}/{RoundState.FinalRound}    ·    다음 스테이지를 선택하세요"; // 페이즈·깊이·선택 안내 표시
            _legendText.text = "BATTLE 성채    ELITE 요새    REWARD 보물고    SHOP 상점    EVENT 마법탑    BOSS 성채"; // 건물 범례 표시
        }

        private void RefreshHoveredNode()
        {
            StageNode hoveredNode = ResolveHoveredNode(); // 현재 포인터 아래 선택 가능 노드 조회
            string nodeId = hoveredNode != null ? hoveredNode.NodeId : string.Empty; // Hover 노드 ID 변환
            if (string.Equals(nodeId, _lastHoverNodeId, System.StringComparison.Ordinal)) return; // 동일 Hover 정보 중복 갱신 차단
            _lastHoverNodeId = nodeId; // 현재 Hover 노드 저장

            if (hoveredNode == null)
            {
                _hoverRoot.SetActive(false); // 노드 없음 상세 패널 숨김
                return;
            }

            StageDefinition definition = StageDefinitionCatalog.Resolve(hoveredNode.StageDefinitionId, hoveredNode.Depth); // 실제 StageDefinition 조회
            StageType stageType = definition != null ? definition.StageType : StageType.Battle; // StageType fallback 적용
            string displayName = definition != null ? definition.DisplayName : $"{hoveredNode.Depth}단계"; // 표시 이름 조회
            _hoverTitleText.text = $"{GetStageLabel(stageType)}  ·  {displayName}"; // StageType·이름 표시
            _hoverBodyText.text = $"Depth {hoveredNode.Depth}\n{GetStageDescription(stageType)}\n\n상태: 선택 가능"; // 노드 설명·상태 표시
            _hoverRoot.SetActive(true); // Hover 상세 패널 표시
        }

        private StageNode ResolveHoveredNode()
        {
            if (Mouse.current == null || _runState == null) return null; // 마우스·런 상태 누락 방어
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null; // 다른 UI 위 포인터 지도 Hover 차단

            Camera targetCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(); // 지도 Raycast 카메라 조회
            if (targetCamera == null) return null; // 카메라 누락 방어

            Vector2 pointerPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 조회
            Ray ray = targetCamera.ScreenPointToRay(pointerPosition); // 화면 좌표 월드 Ray 변환
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f); // 지도 노드 충돌 전체 조회

            for (int i = 0; i < hits.Length; i++)
            {
                RouteMapNodeView nodeView = hits[i].collider.GetComponent<RouteMapNodeView>(); // 선택 가능 노드 View 조회
                if (nodeView == null) continue; // 일반 보드 충돌 제외
                StageNode node = _runState.RouteMap.FindNode(nodeView.NodeId); // 실제 StageNode 조회
                if (node != null) return node; // 첫 선택 가능 노드 반환
            }

            return null; // Hover 노드 없음 반환
        }

        private void ApplyFullGraphVisibility(bool mapSelectionActive)
        {
            if (_boardView == null) return; // 보드 누락 방어
            Transform previewRoot = _boardView.transform.Find("RouteMapFullGraph_Day52"); // 전체 미래 경로 루트 조회
            if (previewRoot == null) return; // 미리보기 생성 전 종료
            if (previewRoot.gameObject.activeSelf == mapSelectionActive) return; // 동일 활성 상태 중복 처리 차단
            previewRoot.gameObject.SetActive(mapSelectionActive); // 경로 선택 중에만 미래·방문 건물 표시
        }

        private void ShowMapUI()
        {
            EnsureUI(); // UI 생성 보장
            if (!_headerRoot.activeSelf) _headerRoot.SetActive(true); // 상단 지도 HUD 표시
        }

        private void HideMapUI()
        {
            if (_headerRoot != null) _headerRoot.SetActive(false); // 지도 HUD 숨김
            if (_hoverRoot != null) _hoverRoot.SetActive(false); // Hover 상세 패널 숨김
            _lastHoverNodeId = string.Empty; // 이전 Hover 상태 초기화
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 Canvas 생성 차단

            GameObject canvasObject = new GameObject("Day66RouteMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 지도 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // UI 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 HUD 적용
            _canvas.sortingOrder = 215; // Reward·StageActivity 아래 지도 HUD 순서 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildHeader(canvasObject.transform); // 상단 지도 HUD 생성
            BuildHoverPanel(canvasObject.transform); // 우측 Hover 정보 패널 생성
            HideMapUI(); // 기본 숨김 상태 지정
        }

        private void BuildHeader(Transform parent)
        {
            _headerRoot = new GameObject("RouteMapHeader", typeof(RectTransform), typeof(Image)); // 지도 상단 HUD 루트 생성
            _headerRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = _headerRoot.GetComponent<RectTransform>(); // HUD RectTransform 조회
            SetRect(rootRect, new Vector2(0f, 456f), new Vector2(1320f, 118f)); // 화면 상단 중앙 배치
            Image background = _headerRoot.GetComponent<Image>(); // HUD 배경 조회
            background.color = new Color(0.025f, 0.030f, 0.043f, 0.93f); // 어두운 지도 HUD 배경 적용
            background.raycastTarget = false; // 지도 노드 포인터 간섭 제거

            _titleText = CreateText("MapTitle", _headerRoot.transform, 30, FontStyle.Bold); // ROUTE MAP 제목 생성
            _titleText.alignment = TextAnchor.MiddleLeft; // 제목 좌측 정렬
            SetRect(_titleText.rectTransform, new Vector2(-455f, 22f), new Vector2(320f, 46f)); // 제목 위치 적용

            _progressText = CreateText("MapProgress", _headerRoot.transform, 21, FontStyle.Bold); // 깊이·안내 문구 생성
            _progressText.alignment = TextAnchor.MiddleRight; // 진행 정보 우측 정렬
            SetRect(_progressText.rectTransform, new Vector2(260f, 22f), new Vector2(820f, 46f)); // 진행 정보 위치 적용

            _legendText = CreateText("MapLegend", _headerRoot.transform, 16, FontStyle.Normal); // 건물 범례 생성
            _legendText.alignment = TextAnchor.MiddleCenter; // 범례 중앙 정렬
            _legendText.color = new Color(0.78f, 0.82f, 0.90f, 1f); // 보조 텍스트 색상 적용
            SetRect(_legendText.rectTransform, new Vector2(0f, -30f), new Vector2(1220f, 38f)); // 범례 위치 적용
        }

        private void BuildHoverPanel(Transform parent)
        {
            _hoverRoot = new GameObject("RouteMapHoverPanel", typeof(RectTransform), typeof(Image)); // Hover 상세 패널 생성
            _hoverRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = _hoverRoot.GetComponent<RectTransform>(); // 상세 패널 RectTransform 조회
            SetRect(rootRect, new Vector2(690f, 80f), new Vector2(430f, 260f)); // 화면 우측 배치
            Image background = _hoverRoot.GetComponent<Image>(); // 상세 패널 배경 조회
            background.color = new Color(0.030f, 0.036f, 0.052f, 0.95f); // 지도 정보 배경 적용
            background.raycastTarget = false; // 노드 Hover Raycast 간섭 제거

            _hoverTitleText = CreateText("HoverTitle", _hoverRoot.transform, 23, FontStyle.Bold); // Hover 제목 생성
            _hoverTitleText.alignment = TextAnchor.MiddleLeft; // 제목 좌측 정렬
            SetRect(_hoverTitleText.rectTransform, new Vector2(0f, 82f), new Vector2(380f, 58f)); // 제목 위치 적용

            _hoverBodyText = CreateText("HoverBody", _hoverRoot.transform, 18, FontStyle.Normal); // Hover 상세 본문 생성
            _hoverBodyText.alignment = TextAnchor.UpperLeft; // 본문 좌상단 정렬
            _hoverBodyText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 설명 줄바꿈 허용
            _hoverBodyText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 설명 잘라내기
            _hoverBodyText.color = new Color(0.88f, 0.90f, 0.94f, 1f); // 상세 문구 색상 적용
            SetRect(_hoverBodyText.rectTransform, new Vector2(0f, -42f), new Vector2(380f, 160f)); // 본문 위치 적용
            _hoverRoot.SetActive(false); // 기본 상세 패널 숨김
        }

        private static string GetStageLabel(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.Elite: return "ELITE"; // 엘리트 라벨 반환
                case StageType.Reward: return "REWARD"; // 보상 라벨 반환
                case StageType.Shop: return "SHOP"; // 상점 라벨 반환
                case StageType.Event: return "EVENT"; // 이벤트 라벨 반환
                case StageType.MidBoss: return "MID BOSS"; // 중간 보스 라벨 반환
                case StageType.FinalBoss: return "FINAL BOSS"; // 최종 보스 라벨 반환
                default: return "BATTLE"; // 일반 전투 라벨 반환
            }
        }

        private static string GetStageDescription(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.Elite: return "강화된 적과 싸우는 고위험 전투 스테이지입니다."; // 엘리트 설명 반환
                case StageType.Reward: return "카드 후보를 비교하고 한 장을 획득하는 보상 스테이지입니다."; // 보상 설명 반환
                case StageType.Shop: return "Gold로 카드 구매·제거·회복·강화를 진행하는 상점입니다."; // 상점 설명 반환
                case StageType.Event: return "선택에 따라 보상이나 위험이 발생하는 이벤트 스테이지입니다."; // 이벤트 설명 반환
                case StageType.MidBoss: return "일반 전투보다 강력한 중간 보스가 기다리는 요새입니다."; // 중간 보스 설명 반환
                case StageType.FinalBoss: return "현재 런의 마지막 전투가 진행되는 최종 성채입니다."; // 최종 보스 설명 반환
                default: return "일반 적과 전투를 진행하는 기본 전투 스테이지입니다."; // 일반 전투 설명 반환
            }
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 굵기 적용
            text.alignment = TextAnchor.MiddleCenter; // 기본 중앙 정렬
            text.color = Color.white; // 기본 흰색 글자 적용
            text.raycastTarget = false; // 지도 포인터 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 한글 시스템 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            return _runtimeFont; // 최종 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }
    }
}
