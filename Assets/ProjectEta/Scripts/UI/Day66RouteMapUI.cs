using UnityEngine; // Unity 기본 형식 사용
using UnityEngine.EventSystems; // UI 포인터 판정 사용
using UnityEngine.InputSystem; // 마우스 입력 사용
using UnityEngine.SceneManagement; // Battle 씬 감시 사용
using UnityEngine.UI; // 런타임 UI 사용
using ProjectEta.Battle; // 전투 상태 사용
using ProjectEta.Board; // 지도 보드 사용
using ProjectEta.Run; // 런 경로 상태 사용

namespace ProjectEta.UI // 프로젝트 UI 네임스페이스
{ // 네임스페이스 범위 시작
    [DefaultExecutionOrder(1240)] // 지도 HUD 실행 순서
    public sealed class Day66RouteMapUI : MonoBehaviour // 우측 경로 지도 패널
    { // 클래스 범위 시작
        private static readonly Vector2 TopRightAnchor = Vector2.one; // 우상단 공통 앵커
        private static readonly Vector2 PanelPosition = new Vector2(-18f, -18f); // 화면 가장자리 여백
        private static readonly Vector2 PanelSize = new Vector2(336f, 820f); // 세로 패널 크기
        private static readonly Color PanelBorderColor = new Color(0.62f, 0.45f, 0.20f, 0.98f); // 황동 테두리 색상
        private static readonly Color PanelBodyColor = new Color(0.020f, 0.032f, 0.042f, 0.95f); // 짙은 청록 배경색
        private static readonly Color RowColor = new Color(0.055f, 0.072f, 0.082f, 0.92f); // 범례 행 배경색
        private static readonly Color PrimaryTextColor = new Color(1f, 0.94f, 0.82f, 1f); // 주요 글자 색상
        private static readonly Color SecondaryTextColor = new Color(0.76f, 0.82f, 0.84f, 1f); // 보조 글자 색상
        private BattleController _battleController; // 현재 런 상태 제공자
        private BoardView _boardView; // 경로 보드 표시 대상
        private RunState _runState; // 현재 런 상태
        private Canvas _canvas; // 지도 전용 Canvas
        private GameObject _sidePanelRoot; // 우측 패널 루트
        private Text _titleText; // 지도 제목
        private Text _phaseText; // 현재 페이즈
        private Text _stageText; // 현재 스테이지
        private Text _instructionText; // 선택 안내
        private GameObject _hoverRoot; // 노드 상세 영역
        private Text _hoverTitleText; // 노드 상세 제목
        private Text _hoverBodyText; // 노드 상세 본문
        private string _lastHoverNodeId = string.Empty; // 이전 Hover 노드 ID
        private static Font _runtimeFont; // 한글 폰트 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 씬 감시 사전 등록
        private static void RegisterSceneLoadedCallback() // 씬 로드 콜백 등록
        { // 메서드 범위 시작
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 콜백 제거
            SceneManager.sceneLoaded += HandleSceneLoaded; // 새 콜백 등록
        } // 메서드 범위 종료

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) // Battle 씬 UI 생성
        { // 메서드 범위 시작
            if (scene.name != "Battle") // Battle 씬 여부 확인
            { // 조건 범위 시작
                return; // 다른 씬 생성 차단
            } // 조건 범위 종료

            if (Object.FindFirstObjectByType<Day66RouteMapUI>() != null) // 기존 UI 확인
            { // 조건 범위 시작
                return; // 중복 생성 차단
            } // 조건 범위 종료

            GameObject host = new GameObject("Day66RouteMapUI"); // 지도 UI 호스트 생성
            host.AddComponent<Day66RouteMapUI>(); // 우측 지도 UI 연결
        } // 메서드 범위 종료

        private void Awake() // 컴포넌트 초기화
        { // 메서드 범위 시작
            EnsureUI(); // 지도 패널 선생성
        } // 메서드 범위 종료

        private void Update() // 지도 상태 갱신
        { // 메서드 범위 시작
            ResolveBindings(); // 런 참조 연결

            if (_runState == null) // 런 준비 여부 확인
            { // 조건 범위 시작
                return; // 준비 전 갱신 차단
            } // 조건 범위 종료

            bool mapSelectionActive = _runState.CurrentFlowPhase == RunFlowPhase.Map && _runState.RouteMap.HasPreparedRoute; // 지도 선택 상태 계산
            ApplyFullGraphVisibility(mapSelectionActive); // 전체 경로 표시 전환

            if (!mapSelectionActive) // 지도 선택 외 상태 확인
            { // 조건 범위 시작
                HideMapUI(); // 지도 UI 숨김
                return; // 현재 갱신 종료
            } // 조건 범위 종료

            ShowMapUI(); // 지도 UI 표시
            RefreshHeader(); // 진행 정보 갱신
            RefreshHoveredNode(); // 노드 정보 갱신
        } // 메서드 범위 종료

        private void ResolveBindings() // 런타임 참조 연결
        { // 메서드 범위 시작
            if (_battleController == null) // 전투 컨트롤러 확인
            { // 조건 범위 시작
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 전투 컨트롤러 탐색
            } // 조건 범위 종료

            if (_boardView == null) // 보드 뷰 확인
            { // 조건 범위 시작
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 보드 뷰 탐색
            } // 조건 범위 종료

            if (_battleController == null || _battleController.RunState == null) // 런 상태 존재 확인
            { // 조건 범위 시작
                return; // 준비 전 연결 차단
            } // 조건 범위 종료

            _runState = _battleController.RunState; // 현재 런 상태 저장
        } // 메서드 범위 종료

        private void RefreshHeader() // 진행 정보 갱신
        { // 메서드 범위 시작
            int currentPhase = RunPhaseProgressService.GetCurrentPhase(_runState); // 현재 페이즈 조회
            _titleText.text = "ROUTE MAP"; // 지도 제목 적용
            _phaseText.text = $"PHASE  {currentPhase} / {RunPhaseProgressService.TotalPhases}"; // 페이즈 문구 적용
            _stageText.text = $"STAGE  {_runState.RouteMap.CurrentDepth} / {RoundState.FinalRound}"; // 스테이지 문구 적용
            _instructionText.text = "다음 스테이지를 선택하세요"; // 선택 안내 적용
        } // 메서드 범위 종료

        private void RefreshHoveredNode() // Hover 노드 상세 갱신
        { // 메서드 범위 시작
            StageNode hoveredNode = ResolveHoveredNode(); // 현재 Hover 노드 조회
            string nodeId = hoveredNode != null ? hoveredNode.NodeId : string.Empty; // 노드 ID 변환

            if (string.Equals(nodeId, _lastHoverNodeId, System.StringComparison.Ordinal)) // 동일 노드 확인
            { // 조건 범위 시작
                return; // 중복 갱신 차단
            } // 조건 범위 종료

            _lastHoverNodeId = nodeId; // 현재 노드 ID 저장

            if (hoveredNode == null) // Hover 노드 존재 확인
            { // 조건 범위 시작
                _hoverRoot.SetActive(false); // 상세 영역 숨김
                return; // 현재 갱신 종료
            } // 조건 범위 종료

            StageDefinition definition = StageDefinitionCatalog.Resolve(hoveredNode.StageDefinitionId, hoveredNode.Depth); // 스테이지 정의 조회
            StageType stageType = definition != null ? definition.StageType : StageType.Battle; // 스테이지 종류 보정
            string displayName = definition != null ? definition.DisplayName : $"{hoveredNode.Depth}단계"; // 표시 이름 보정
            _hoverTitleText.text = $"{GetStageLabel(stageType)}  ·  {displayName}"; // 상세 제목 적용
            _hoverBodyText.text = $"Depth {hoveredNode.Depth}\n{GetStageDescription(stageType)}\n\n상태: 선택 가능"; // 상세 본문 적용
            _hoverRoot.SetActive(true); // 상세 영역 표시
        } // 메서드 범위 종료

        private StageNode ResolveHoveredNode() // 화면 포인터 노드 조회
        { // 메서드 범위 시작
            if (Mouse.current == null || _runState == null) // 입력과 런 상태 확인
            { // 조건 범위 시작
                return null; // 조회 불가 반환
            } // 조건 범위 종료

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) // UI 위 포인터 확인
            { // 조건 범위 시작
                return null; // UI 뒤 노드 조회 차단
            } // 조건 범위 종료

            Camera targetCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>(); // 지도 카메라 조회

            if (targetCamera == null) // 카메라 존재 확인
            { // 조건 범위 시작
                return null; // 조회 불가 반환
            } // 조건 범위 종료

            Vector2 pointerPosition = Mouse.current.position.ReadValue(); // 화면 포인터 좌표 조회
            Ray ray = targetCamera.ScreenPointToRay(pointerPosition); // 화면 좌표 Ray 변환
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f); // 지도 충돌 전체 조회

            for (int index = 0; index < hits.Length; index++) // 충돌 대상 순회
            { // 반복 범위 시작
                RouteMapNodeView nodeView = hits[index].collider.GetComponent<RouteMapNodeView>(); // 노드 뷰 조회

                if (nodeView == null) // 노드 뷰 여부 확인
                { // 조건 범위 시작
                    continue; // 일반 충돌 제외
                } // 조건 범위 종료

                StageNode node = _runState.RouteMap.FindNode(nodeView.NodeId); // 실제 노드 조회

                if (node != null) // 실제 노드 존재 확인
                { // 조건 범위 시작
                    return node; // 첫 노드 반환
                } // 조건 범위 종료
            } // 반복 범위 종료

            return null; // Hover 노드 없음 반환
        } // 메서드 범위 종료

        private void ApplyFullGraphVisibility(bool mapSelectionActive) // 전체 경로 표시 제어
        { // 메서드 범위 시작
            if (_boardView == null) // 보드 뷰 존재 확인
            { // 조건 범위 시작
                return; // 처리 불가 종료
            } // 조건 범위 종료

            Transform previewRoot = _boardView.transform.Find("RouteMapFullGraph_Day52"); // 전체 경로 루트 조회

            if (previewRoot == null || previewRoot.gameObject.activeSelf == mapSelectionActive) // 경로 루트와 현재 상태 확인
            { // 조건 범위 시작
                return; // 불필요한 전환 차단
            } // 조건 범위 종료

            previewRoot.gameObject.SetActive(mapSelectionActive); // 전체 경로 표시 적용
        } // 메서드 범위 종료

        private void ShowMapUI() // 지도 UI 표시
        { // 메서드 범위 시작
            EnsureUI(); // UI 생성 보장

            if (!_sidePanelRoot.activeSelf) // 패널 활성 상태 확인
            { // 조건 범위 시작
                _sidePanelRoot.SetActive(true); // 우측 패널 표시
            } // 조건 범위 종료
        } // 메서드 범위 종료

        private void HideMapUI() // 지도 UI 숨김
        { // 메서드 범위 시작
            if (_sidePanelRoot != null) // 패널 존재 확인
            { // 조건 범위 시작
                _sidePanelRoot.SetActive(false); // 우측 패널 숨김
            } // 조건 범위 종료

            if (_hoverRoot != null) // 상세 영역 존재 확인
            { // 조건 범위 시작
                _hoverRoot.SetActive(false); // 상세 영역 숨김
            } // 조건 범위 종료

            _lastHoverNodeId = string.Empty; // Hover 상태 초기화
        } // 메서드 범위 종료

        private void EnsureUI() // 지도 UI 생성 보장
        { // 메서드 범위 시작
            if (_canvas != null) // 기존 Canvas 확인
            { // 조건 범위 시작
                return; // 중복 생성 차단
            } // 조건 범위 종료

            GameObject canvasObject = new GameObject("Day66RouteMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 지도 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 모드 적용
            _canvas.sortingOrder = 215; // 지도 UI 정렬 순서 적용
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 화면 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로세로 균형 적용
            BuildSidePanel(canvasObject.transform); // 우측 패널 생성
            HideMapUI(); // 초기 숨김 적용
        } // 메서드 범위 종료

        private void BuildSidePanel(Transform parent) // 우측 세로 패널 생성
        { // 메서드 범위 시작
            _sidePanelRoot = new GameObject("RouteMapSidePanel", typeof(RectTransform), typeof(Image)); // 패널 루트 생성
            _sidePanelRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = _sidePanelRoot.GetComponent<RectTransform>(); // 패널 RectTransform 조회
            rootRect.anchorMin = TopRightAnchor; // 우상단 최소 앵커 적용
            rootRect.anchorMax = TopRightAnchor; // 우상단 최대 앵커 적용
            rootRect.pivot = TopRightAnchor; // 우상단 피벗 적용
            rootRect.anchoredPosition = PanelPosition; // 화면 여백 적용
            rootRect.sizeDelta = PanelSize; // 패널 크기 적용
            Image border = _sidePanelRoot.GetComponent<Image>(); // 패널 테두리 이미지 조회
            border.color = PanelBorderColor; // 황동 테두리 색상 적용
            border.raycastTarget = true; // 패널 뒤 지도 클릭 차단
            Image body = CreateImage("RouteMapPanelBody", _sidePanelRoot.transform, PanelBodyColor); // 내부 배경 생성
            StretchRect(body.rectTransform, new Vector2(3f, 3f), new Vector2(-3f, -3f)); // 얇은 테두리 여백 적용
            _titleText = CreateText("MapTitle", _sidePanelRoot.transform, 30, FontStyle.Bold, PrimaryTextColor); // 제목 생성
            _titleText.alignment = TextAnchor.MiddleLeft; // 제목 좌측 정렬
            SetCenteredRect(_titleText.rectTransform, new Vector2(0f, 360f), new Vector2(286f, 48f)); // 제목 위치 적용
            _phaseText = CreateText("MapPhase", _sidePanelRoot.transform, 20, FontStyle.Bold, PrimaryTextColor); // 페이즈 문구 생성
            _phaseText.alignment = TextAnchor.MiddleLeft; // 페이즈 좌측 정렬
            SetCenteredRect(_phaseText.rectTransform, new Vector2(0f, 310f), new Vector2(286f, 34f)); // 페이즈 위치 적용
            _stageText = CreateText("MapStage", _sidePanelRoot.transform, 20, FontStyle.Bold, PrimaryTextColor); // 스테이지 문구 생성
            _stageText.alignment = TextAnchor.MiddleLeft; // 스테이지 좌측 정렬
            SetCenteredRect(_stageText.rectTransform, new Vector2(0f, 272f), new Vector2(286f, 34f)); // 스테이지 위치 적용
            _instructionText = CreateText("MapInstruction", _sidePanelRoot.transform, 17, FontStyle.Normal, SecondaryTextColor); // 선택 안내 생성
            _instructionText.alignment = TextAnchor.MiddleLeft; // 안내 좌측 정렬
            SetCenteredRect(_instructionText.rectTransform, new Vector2(0f, 226f), new Vector2(286f, 42f)); // 안내 위치 적용
            CreateDivider("HeaderDivider", _sidePanelRoot.transform, 199f); // 헤더 구분선 생성
            Text legendTitle = CreateText("LegendTitle", _sidePanelRoot.transform, 16, FontStyle.Bold, SecondaryTextColor); // 범례 제목 생성
            legendTitle.text = "지역 종류"; // 범례 제목 적용
            legendTitle.alignment = TextAnchor.MiddleLeft; // 범례 제목 좌측 정렬
            SetCenteredRect(legendTitle.rectTransform, new Vector2(0f, 174f), new Vector2(286f, 28f)); // 범례 제목 위치 적용
            CreateLegendRow("Battle", "전투", "UI/RouteMap/RouteBattleIcon", 132f); // 일반 전투 행 생성
            CreateLegendRow("Elite", "엘리트", "UI/RouteMap/RouteEliteIcon", 82f); // 엘리트 행 생성
            CreateLegendRow("Reward", "보상", "UI/RouteMap/RouteRewardIcon", 32f); // 보상 행 생성
            CreateLegendRow("Shop", "상점", "UI/RouteMap/RouteShopIcon", -18f); // 상점 행 생성
            CreateLegendRow("Event", "이벤트", "UI/RouteMap/RouteEventIcon", -68f); // 이벤트 행 생성
            CreateLegendRow("Boss", "보스", "UI/RouteMap/RouteBossIcon", -118f); // 보스 행 생성
            CreateDivider("HoverDivider", _sidePanelRoot.transform, -154f); // 상세 영역 구분선 생성
            BuildHoverPanel(_sidePanelRoot.transform); // 패널 내부 상세 영역 생성
        } // 메서드 범위 종료

        private void CreateLegendRow(string id, string label, string resourcePath, float y) // 아이콘 범례 행 생성
        { // 메서드 범위 시작
            GameObject row = new GameObject($"RouteLegendRow_{id}", typeof(RectTransform), typeof(Image)); // 범례 행 루트 생성
            row.transform.SetParent(_sidePanelRoot.transform, false); // 패널 자식 연결
            RectTransform rowRect = row.GetComponent<RectTransform>(); // 행 RectTransform 조회
            SetCenteredRect(rowRect, new Vector2(0f, y), new Vector2(286f, 44f)); // 행 위치 적용
            Image rowBackground = row.GetComponent<Image>(); // 행 배경 조회
            rowBackground.color = RowColor; // 행 배경색 적용
            rowBackground.raycastTarget = false; // 지도 입력 간섭 제거
            Image icon = CreateImage("Icon", row.transform, Color.white); // 아이콘 이미지 생성
            SetCenteredRect(icon.rectTransform, new Vector2(-113f, 0f), new Vector2(38f, 38f)); // 아이콘 위치 적용
            icon.sprite = LoadIconSprite(resourcePath); // 생성 아이콘 연결
            icon.preserveAspect = true; // 아이콘 비율 유지
            icon.enabled = icon.sprite != null; // 누락 아이콘 숨김
            Text labelText = CreateText("Label", row.transform, 18, FontStyle.Bold, PrimaryTextColor); // 한글 라벨 생성
            labelText.text = label; // 한글 라벨 적용
            labelText.alignment = TextAnchor.MiddleLeft; // 라벨 좌측 정렬
            SetCenteredRect(labelText.rectTransform, new Vector2(30f, 0f), new Vector2(210f, 40f)); // 라벨 위치 적용
        } // 메서드 범위 종료

        private void BuildHoverPanel(Transform parent) // 패널 내부 Hover 영역 생성
        { // 메서드 범위 시작
            _hoverRoot = new GameObject("RouteMapHoverPanel", typeof(RectTransform), typeof(Image)); // 상세 영역 루트 생성
            _hoverRoot.transform.SetParent(parent, false); // 패널 자식 연결
            RectTransform rootRect = _hoverRoot.GetComponent<RectTransform>(); // 상세 영역 RectTransform 조회
            SetCenteredRect(rootRect, new Vector2(0f, -284f), new Vector2(286f, 228f)); // 패널 하단 배치
            Image background = _hoverRoot.GetComponent<Image>(); // 상세 배경 조회
            background.color = new Color(0.035f, 0.050f, 0.060f, 0.96f); // 상세 배경색 적용
            background.raycastTarget = false; // 지도 입력 간섭 제거
            _hoverTitleText = CreateText("HoverTitle", _hoverRoot.transform, 19, FontStyle.Bold, PrimaryTextColor); // 상세 제목 생성
            _hoverTitleText.alignment = TextAnchor.MiddleLeft; // 상세 제목 좌측 정렬
            SetCenteredRect(_hoverTitleText.rectTransform, new Vector2(0f, 82f), new Vector2(254f, 48f)); // 상세 제목 위치 적용
            _hoverBodyText = CreateText("HoverBody", _hoverRoot.transform, 15, FontStyle.Normal, SecondaryTextColor); // 상세 본문 생성
            _hoverBodyText.alignment = TextAnchor.UpperLeft; // 상세 본문 좌상단 정렬
            _hoverBodyText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 본문 줄바꿈 적용
            _hoverBodyText.verticalOverflow = VerticalWrapMode.Truncate; // 넘친 본문 잘라내기
            SetCenteredRect(_hoverBodyText.rectTransform, new Vector2(0f, -36f), new Vector2(254f, 166f)); // 상세 본문 위치 적용
            _hoverRoot.SetActive(false); // 초기 상세 영역 숨김
        } // 메서드 범위 종료

        private static void CreateDivider(string name, Transform parent, float y) // 패널 구분선 생성
        { // 메서드 범위 시작
            Image divider = CreateImage(name, parent, new Color(0.62f, 0.45f, 0.20f, 0.72f)); // 황동 구분선 생성
            SetCenteredRect(divider.rectTransform, new Vector2(0f, y), new Vector2(286f, 2f)); // 구분선 위치 적용
        } // 메서드 범위 종료

        private static Sprite LoadIconSprite(string resourcePath) // Resources 아이콘 Sprite 생성
        { // 메서드 범위 시작
            Texture2D texture = Resources.Load<Texture2D>(resourcePath); // PNG 텍스처 로드

            if (texture == null) // 텍스처 존재 확인
            { // 조건 범위 시작
                return null; // 누락 아이콘 반환
            } // 조건 범위 종료

            Rect textureRect = new Rect(0f, 0f, texture.width, texture.height); // 전체 텍스처 영역 생성
            Sprite sprite = Sprite.Create(texture, textureRect, new Vector2(0.5f, 0.5f), 100f); // 런타임 Sprite 생성
            sprite.name = $"{texture.name}_RuntimeSprite"; // 디버그 이름 적용
            return sprite; // 생성 Sprite 반환
        } // 메서드 범위 종료

        private static string GetStageLabel(StageType stageType) // 스테이지 영문 라벨 반환
        { // 메서드 범위 시작
            switch (stageType) // 스테이지 종류 분기
            { // 분기 범위 시작
                case StageType.Elite: // 엘리트 종류
                    return "ELITE"; // 엘리트 라벨 반환
                case StageType.Reward: // 보상 종류
                    return "REWARD"; // 보상 라벨 반환
                case StageType.Shop: // 상점 종류
                    return "SHOP"; // 상점 라벨 반환
                case StageType.Event: // 이벤트 종류
                    return "EVENT"; // 이벤트 라벨 반환
                case StageType.MidBoss: // 중간 보스 종류
                    return "MID BOSS"; // 중간 보스 라벨 반환
                case StageType.FinalBoss: // 최종 보스 종류
                    return "FINAL BOSS"; // 최종 보스 라벨 반환
                default: // 일반 전투 종류
                    return "BATTLE"; // 일반 전투 라벨 반환
            } // 분기 범위 종료
        } // 메서드 범위 종료

        private static string GetStageDescription(StageType stageType) // 스테이지 설명 반환
        { // 메서드 범위 시작
            switch (stageType) // 스테이지 종류 분기
            { // 분기 범위 시작
                case StageType.Elite: // 엘리트 종류
                    return "강화된 적과 싸우는 고위험 전투 스테이지입니다."; // 엘리트 설명 반환
                case StageType.Reward: // 보상 종류
                    return "카드 후보를 비교하고 한 장을 획득하는 보상 스테이지입니다."; // 보상 설명 반환
                case StageType.Shop: // 상점 종류
                    return "Gold로 카드 구매·제거·회복·강화를 진행하는 상점입니다."; // 상점 설명 반환
                case StageType.Event: // 이벤트 종류
                    return "선택에 따라 보상이나 위험이 발생하는 이벤트 스테이지입니다."; // 이벤트 설명 반환
                case StageType.MidBoss: // 중간 보스 종류
                    return "일반 전투보다 강력한 중간 보스가 기다리는 요새입니다."; // 중간 보스 설명 반환
                case StageType.FinalBoss: // 최종 보스 종류
                    return "현재 런의 마지막 전투가 진행되는 최종 성채입니다."; // 최종 보스 설명 반환
                default: // 일반 전투 종류
                    return "일반 적과 전투를 진행하는 기본 전투 스테이지입니다."; // 일반 전투 설명 반환
            } // 분기 범위 종료
        } // 메서드 범위 종료

        private static Image CreateImage(string name, Transform parent, Color color) // 런타임 Image 생성
        { // 메서드 범위 시작
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 이미지 오브젝트 생성
            imageObject.transform.SetParent(parent, false); // UI 부모 연결
            Image image = imageObject.GetComponent<Image>(); // Image 컴포넌트 조회
            image.color = color; // 지정 색상 적용
            image.raycastTarget = false; // 기본 입력 간섭 제거
            return image; // 완성 Image 반환
        } // 메서드 범위 종료

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, Color color) // 런타임 Text 생성
        { // 메서드 범위 시작
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 텍스트 오브젝트 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 굵기 적용
            text.alignment = TextAnchor.MiddleCenter; // 기본 중앙 정렬 적용
            text.color = color; // 지정 글자색 적용
            text.raycastTarget = false; // 지도 입력 간섭 제거
            return text; // 완성 Text 반환
        } // 메서드 범위 종료

        private static Font GetRuntimeFont() // 한글 런타임 폰트 반환
        { // 메서드 범위 시작
            if (_runtimeFont != null) // 캐시 폰트 확인
            { // 조건 범위 시작
                return _runtimeFont; // 기존 폰트 반환
            } // 조건 범위 종료

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 폰트 생성

            if (_runtimeFont == null) // 시스템 폰트 실패 확인
            { // 조건 범위 시작
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 사용
            } // 조건 범위 종료

            return _runtimeFont; // 최종 폰트 반환
        } // 메서드 범위 종료

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size) // 중앙 기준 Rect 배치
        { // 메서드 범위 시작
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 최소 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 최대 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // 지정 위치 적용
            rect.sizeDelta = size; // 지정 크기 적용
        } // 메서드 범위 종료

        private static void StretchRect(RectTransform rect, Vector2 minimumOffset, Vector2 maximumOffset) // 부모 전체 Rect 배치
        { // 메서드 범위 시작
            rect.anchorMin = Vector2.zero; // 전체 최소 앵커 적용
            rect.anchorMax = Vector2.one; // 전체 최대 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.offsetMin = minimumOffset; // 좌하단 내부 여백 적용
            rect.offsetMax = maximumOffset; // 우상단 내부 여백 적용
        } // 메서드 범위 종료
    } // 클래스 범위 종료
} // 네임스페이스 범위 종료
