using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Battle; // BattleController·TurnManager 사용
using ProjectEta.Meta; // MetaProgressService 사용
using ProjectEta.Run; // BoardMode·RunState 사용

namespace ProjectEta.King
{
    public sealed class KingSelectionUI : MonoBehaviour
    {
        private BattleController _battleController; // 현재 BattleController
        private Canvas _canvas; // 킹 선택 전용 Canvas
        private RectTransform _panelRect; // 선택·상태 패널
        private Button _defaultButton; // 기본 킹 선택 버튼
        private Button _attackButton; // 공격형 킹 선택 버튼
        private Text _titleText; // 킹 선택 제목
        private Text _defaultButtonText; // 기본 킹 버튼 문구
        private Text _attackButtonText; // 공격형 킹 버튼 문구
        private Text _statusText; // 현재 킹·격노 상태
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 탐색
            RefreshPresentation(); // 첫 배치 선택·런 중 상태 갱신
        }

        private void ResolveBattleController()
        {
            if (_battleController != null) return; // 기존 BattleController 재사용
            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색
        }

        private void RefreshPresentation()
        {
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            if (runState == null) return; // 런 준비 전 UI 생성 지연

            EnsureUI(); // 킹 선택 UI 생성 보장
            KingRunState kingState = KingRunStateService.Get(runState); // 현재 런 킹 상태 조회
            bool canSelect = CanSelectBeforeInitialPlacement(runState); // 첫 킹 배치 전 선택 가능 여부 조회

            if (canSelect)
            {
                ShowSelectionMode(kingState); // 새 런 킹 선택 UI 표시
                return; // 선택 모드 갱신 종료
            }

            ShowStatusMode(kingState); // 전투·지도 진행 중 현재 킹 상태 표시
        }

        private bool CanSelectBeforeInitialPlacement(RunState runState)
        {
            if (runState == null || _battleController == null) return false; // 런·전투 컨트롤러 누락 차단
            if (runState.CurrentRound != RoundState.FirstRound) return false; // 첫 스테이지 외 킹 교체 차단
            if (runState.CurrentBoardMode != BoardMode.Battle) return false; // 지도 모드 킹 교체 차단

            TurnManager turnManager = _battleController.TurnManager; // 현재 턴 매니저 조회
            if (turnManager == null) return false; // 턴 상태 누락 차단
            return turnManager.IsInitialDeployment && !turnManager.IsInitialKingPlaced; // 최초 킹 배치 전만 선택 허용
        }

        private void ShowSelectionMode(KingRunState kingState)
        {
            _panelRect.sizeDelta = new Vector2(430f, 205f); // 선택 모드 패널 크기 적용
            _titleText.gameObject.SetActive(true); // 선택 제목 표시
            _defaultButton.gameObject.SetActive(true); // 기본 킹 버튼 표시
            _attackButton.gameObject.SetActive(true); // 공격형 킹 버튼 표시

            MetaProgressState progress = MetaProgressService.Current; // 48일차 영구 해금 상태 조회
            bool attackUnlocked = KingUnlockRules.CanSelect(KingArchetype.Attack, progress); // 공격형 킹 해금 여부 확인

            _defaultButton.interactable = kingState.Archetype != KingArchetype.Default; // 현재 기본 킹이면 재선택 비활성화
            _attackButton.interactable = attackUnlocked && kingState.Archetype != KingArchetype.Attack; // 해금·현재 선택 상태 기반 공격형 버튼 활성화
            _defaultButtonText.text = kingState.Archetype == KingArchetype.Default ? "기본 킹\n선택됨" : "기본 킹"; // 기본 킹 선택 상태 표시
            _attackButtonText.text = attackUnlocked
                ? kingState.Archetype == KingArchetype.Attack ? "공격형 킹\n선택됨" : "공격형 킹\n처형의 연쇄"
                : "공격형 킹\n잠금"; // 공격형 킹 해금·선택 상태 표시
            _statusText.text = attackUnlocked
                ? "공격형: 직접 처치 시 격노 +1 · 다음 킹 공격에 스택만큼 피해 증가"
                : "공격형 킹은 48일차 영구 해금 king_attack이 필요합니다."; // 선택 상세 안내 표시
        }

        private void ShowStatusMode(KingRunState kingState)
        {
            _panelRect.sizeDelta = new Vector2(430f, 76f); // 상태 모드 패널 축소
            _titleText.gameObject.SetActive(false); // 선택 제목 숨김
            _defaultButton.gameObject.SetActive(false); // 기본 킹 버튼 숨김
            _attackButton.gameObject.SetActive(false); // 공격형 킹 버튼 숨김

            string displayName = KingArchetypeNames.GetDisplayName(kingState.Archetype); // 현재 킹 표시 이름 조회
            _statusText.text = kingState.Archetype == KingArchetype.Attack
                ? $"{displayName}  |  격노 {kingState.RageStacks}/{KingRunState.AttackRageMaxStacks}"
                : displayName; // 런 중 현재 킹·격노 상태 표시
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 Canvas 생성 차단
            EnsureEventSystem(); // 킹 선택 버튼 입력 보장

            var canvasObject = new GameObject("KingSelectionCanvas_Day49", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 킹 선택 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 킹 시스템 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 선택 UI 적용
            _canvas.sortingOrder = 215; // 일반 전투 UI 위 선택 UI 순서 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용

            BuildPanel(canvasObject.transform); // 좌측 상단 선택 패널 생성
        }

        private void BuildPanel(Transform parent)
        {
            var panelObject = new GameObject("KingSelectionPanel", typeof(RectTransform), typeof(Image)); // 선택 패널 생성
            panelObject.transform.SetParent(parent, false); // Canvas 자식 연결
            _panelRect = panelObject.GetComponent<RectTransform>(); // 선택 패널 RectTransform 확보
            _panelRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 앵커 적용
            _panelRect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 앵커 적용
            _panelRect.pivot = new Vector2(0f, 1f); // 좌측 상단 피벗 적용
            _panelRect.anchoredPosition = new Vector2(24f, -24f); // 화면 좌측 상단 여백 적용
            _panelRect.sizeDelta = new Vector2(430f, 205f); // 초기 선택 패널 크기 적용

            Image panelImage = panelObject.GetComponent<Image>(); // 패널 배경 이미지 조회
            panelImage.color = new Color(0.07f, 0.045f, 0.025f, 0.93f); // 테이블 분위기 어두운 배경 적용

            _titleText = CreateText("Title", panelObject.transform, 23, FontStyle.Bold); // 킹 선택 제목 생성
            _titleText.text = "이번 런의 킹 선택"; // 킹 선택 제목 적용
            SetRect(_titleText.rectTransform, new Vector2(215f, -28f), new Vector2(390f, 42f)); // 제목 위치·크기 적용

            _defaultButton = CreateButton("DefaultKing", panelObject.transform, new Vector2(110f, -92f), new Vector2(185f, 70f), out _defaultButtonText); // 기본 킹 버튼 생성
            _attackButton = CreateButton("AttackKing", panelObject.transform, new Vector2(320f, -92f), new Vector2(185f, 70f), out _attackButtonText); // 공격형 킹 버튼 생성
            _defaultButton.onClick.AddListener(() => SelectKing(KingArchetype.Default)); // 기본 킹 선택 연결
            _attackButton.onClick.AddListener(() => SelectKing(KingArchetype.Attack)); // 공격형 킹 선택 연결

            _statusText = CreateText("Status", panelObject.transform, 17, FontStyle.Normal); // 선택 설명·런 상태 텍스트 생성
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 패시브 설명 줄바꿈 허용
            _statusText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 설명 잘라내기
            SetRect(_statusText.rectTransform, new Vector2(215f, -165f), new Vector2(390f, 56f)); // 설명 위치·크기 적용
        }

        private void SelectKing(KingArchetype archetype)
        {
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            if (!CanSelectBeforeInitialPlacement(runState)) return; // 최초 킹 배치 이후 선택 차단

            MetaProgressState progress = MetaProgressService.Current; // 영구 킹 해금 상태 조회
            if (!KingUnlockRules.CanSelect(archetype, progress)) return; // 잠긴 킹 선택 차단

            KingRunState kingState = KingRunStateService.Get(runState); // 현재 런 킹 상태 조회
            kingState.Select(archetype); // 선택한 킹 타입 런 전체 적용
            Debug.Log($"49일차 킹 선택: {KingArchetypeNames.GetDisplayName(archetype)}"); // 킹 선택 결과 출력
            ShowSelectionMode(kingState); // 선택 상태 즉시 갱신
        }

        private Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, out Text label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 킹 선택 버튼 생성
            buttonObject.transform.SetParent(parent, false); // 선택 패널 자식 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image background = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 조회
            background.color = new Color(0.34f, 0.22f, 0.10f, 0.98f); // 목재 카드형 버튼 색상 적용

            Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 조회
            button.targetGraphic = background; // 버튼 대상 그래픽 지정

            label = CreateText("Label", buttonObject.transform, 19, FontStyle.Bold); // 버튼 문구 생성
            Stretch(label.rectTransform, 6f); // 버튼 내부 문구 여백 적용
            return button; // 완성 버튼 반환
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 문구 적용
            text.raycastTarget = false; // 보드 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 기준 앵커 적용
            rect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 기준 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }

        private void OnDestroy()
        {
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
