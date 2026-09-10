using UnityEngine; // MonoBehaviour·GameObject·Time 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem; // Keyboard 사용
using UnityEngine.InputSystem.UI; // InputSystemUIInputModule 사용
using UnityEngine.UI; // Canvas·CanvasScaler·Button·Image·Text 사용
using ProjectEta.Board; // BoardInputController·RouteMapBoardController 사용
using ProjectEta.SceneFlow; // MainMenu 전환 사용
using ProjectEta.UI; // 최초 튜토리얼 사용

namespace ProjectEta.Settings
{
    [DefaultExecutionOrder(1400)]
    public sealed class BattleSettingsOverlayController : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.76f); // Pause 전체 배경 색상
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.085f, 0.99f); // Pause 패널 배경 색상
        private static readonly Color ButtonColor = new Color(0.13f, 0.17f, 0.23f, 1f); // Pause 버튼 기본 색상
        private static readonly Color AccentColor = new Color(0.38f, 0.68f, 0.88f, 1f); // Pause 강조 색상
        private static readonly Color SoftTextColor = new Color(0.68f, 0.72f, 0.79f, 1f); // Pause 보조 글자 색상

        private readonly BattlePauseNavigationState _navigation = new BattlePauseNavigationState(); // Pause 화면 내비게이션 상태
        private GameObject _pauseRoot; // Pause 기본 화면 루트
        private GameObject _controlsRoot; // 조작법 화면 루트
        private GameObject _settingsRoot; // 재사용 설정 화면 루트
        private BoardInputController _boardInputController; // 전투 보드 입력
        private RouteMapBoardController _routeMapBoardController; // 경로 지도 입력
        private bool _boardInputWasEnabled; // Pause 열기 전 전투 입력 상태
        private bool _routeMapWasEnabled; // Pause 열기 전 지도 입력 상태
        private float _previousTimeScale = 1f; // Pause 열기 전 TimeScale
        private bool _runtimeSuspended; // Pause 런타임 중단 적용 여부
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsOpen => _navigation.IsOpen; // 외부 Pause 열림 상태 조회
        public BattlePausePanel CurrentPanel => _navigation.CurrentPanel; // 외부 현재 Pause 하위 화면 조회

        private void Start()
        {
            GameSettingsService.EnsureLoaded(); // 직접 Battle 실행 시 설정 로드 보장
            EnsureEventSystem(); // Pause UI 입력 시스템 보장
            BuildOverlay(); // Pause·조작법·설정 화면 생성
            ApplyPanelState(); // 최초 닫힘 상태 적용
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 입력 장치 조회
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return; // ESC 입력 없으면 처리 종료
            if (FirstRunTutorialController.IsAnyTutorialOpen) return; // 튜토리얼 표시 중 Pause 입력 차단

            if (!_navigation.IsOpen)
            {
                OpenPanel(); // 게임 중 ESC Pause 진입
                return; // Pause 진입 처리 종료
            }

            bool wasOpen = _navigation.IsOpen; // 뒤로가기 전 열림 상태 저장
            if (!_navigation.TryBack()) return; // 현재 화면 뒤로가기 처리
            ApplyPanelState(); // 변경 화면 표시 적용

            if (wasOpen && !_navigation.IsOpen)
            {
                ResumeRuntime(); // Pause 최상위 ESC 게임 복귀
            }
        }

        public void OpenPanel()
        {
            if (_navigation.IsOpen || _pauseRoot == null) return; // 중복 열기·UI 준비 전 차단

            SuspendRuntime(); // 게임 시간·입력 중단
            _navigation.OpenPause(); // Pause 기본 화면 상태 진입
            ApplyPanelState(); // Pause 화면 표시
        }

        public void ClosePanel()
        {
            if (!_navigation.IsOpen) return; // 중복 닫기 차단

            _navigation.Close(); // Pause 계층 전체 닫기
            ApplyPanelState(); // 모든 Pause 화면 숨김
            ResumeRuntime(); // 게임 시간·입력 복원
        }

        public void ShowPausePanel()
        {
            if (!_navigation.IsOpen) return; // Pause 외부 직접 화면 진입 차단
            _navigation.OpenPause(); // Pause 기본 화면 복귀
            ApplyPanelState(); // Pause 화면 표시
        }

        public void ShowControlsPanel()
        {
            if (!_navigation.IsOpen) return; // Pause 외부 조작법 진입 차단
            _navigation.ShowControls(); // 조작법 화면 상태 진입
            ApplyPanelState(); // 조작법 화면 표시
        }

        public void ShowSettingsPanel()
        {
            if (!_navigation.IsOpen) return; // Pause 외부 설정 진입 차단
            _navigation.ShowSettings(); // 설정 화면 상태 진입
            ApplyPanelState(); // 설정 화면 표시
        }

        private void HandleReplayTutorial()
        {
            FirstRunTutorialController tutorial = Object.FindFirstObjectByType<FirstRunTutorialController>(); // 현재 튜토리얼 컨트롤러 조회
            ClosePanel(); // Pause 상태 먼저 복원
            if (tutorial != null) tutorial.ShowTutorial(); // 전체 조작 튜토리얼 다시 표시
        }

        private void HandleReturnToMainMenu()
        {
            ClosePanel(); // 씬 전환 전 TimeScale·입력 복원

            if (!SceneFlowController.ReturnToMainMenu())
            {
                OpenPanel(); // 전환 실패 시 Pause 화면 복구
            }
        }

        private void SuspendRuntime()
        {
            if (_runtimeSuspended) return; // 중복 런타임 중단 차단

            _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 현재 전투 입력 조회
            _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 현재 지도 입력 조회
            _boardInputWasEnabled = _boardInputController != null && _boardInputController.enabled; // 전투 입력 기존 상태 저장
            _routeMapWasEnabled = _routeMapBoardController != null && _routeMapBoardController.enabled; // 지도 입력 기존 상태 저장

            if (_boardInputController != null) _boardInputController.enabled = false; // Pause 중 보드 입력 차단
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = false; // Pause 중 지도 노드 입력 차단

            _previousTimeScale = Time.timeScale; // 기존 TimeScale 저장
            Time.timeScale = 0f; // Pause 중 런타임 진행 중단
            _runtimeSuspended = true; // 런타임 중단 상태 기록
        }

        private void ResumeRuntime()
        {
            if (!_runtimeSuspended) return; // 복원할 Pause 상태 없음

            if (_boardInputController != null) _boardInputController.enabled = _boardInputWasEnabled; // 전투 입력 기존 상태 복원
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = _routeMapWasEnabled; // 지도 입력 기존 상태 복원
            Time.timeScale = _previousTimeScale; // 기존 TimeScale 복원
            _runtimeSuspended = false; // 런타임 중단 상태 해제
        }

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("BattlePauseCanvas_Day68", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Pause 공통 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결

            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전체 화면 오버레이 모드 적용
            canvas.sortingOrder = 2100; // 전투 HUD 위 Pause 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 적용

            BuildPauseScreen(canvasObject.transform); // Pause 기본 화면 생성
            BuildControlsScreen(canvasObject.transform); // 조작법 화면 생성
            BuildSettingsScreen(canvasObject.transform); // 기존 설정 패널 생성
            GameSettingsService.ReapplyUiScale(); // 신규 Pause Canvas 저장 UI Scale 적용
        }

        private void BuildPauseScreen(Transform parent)
        {
            _pauseRoot = CreateScreenRoot("PauseRoot_Day68", parent); // Pause 기본 화면 루트 생성
            GameObject panel = CreateImage("PausePanel", _pauseRoot.transform, Vector2.zero, new Vector2(660f, 820f), PanelColor); // Pause 중앙 패널 생성

            Text section = CreateText("Section", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter); // Pause 상단 분류 문구 생성
            section.text = "SYSTEM"; // Pause 상단 분류 문구 적용
            section.color = AccentColor; // 강조 색상 적용
            SetRect(section.rectTransform, new Vector2(0f, 320f), new Vector2(520f, 40f)); // 분류 문구 위치 적용

            Text title = CreateText("Title", panel.transform, 48, FontStyle.Bold, TextAnchor.MiddleCenter); // Pause 제목 생성
            title.text = "일시정지"; // Pause 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 250f), new Vector2(520f, 72f)); // Pause 제목 위치 적용

            Text hint = CreateText("Hint", panel.transform, 17, FontStyle.Normal, TextAnchor.MiddleCenter); // Pause 안내 문구 생성
            hint.text = "ESC로 게임으로 돌아갑니다."; // Pause 안내 적용
            hint.color = SoftTextColor; // 보조 글자 색상 적용
            SetRect(hint.rectTransform, new Vector2(0f, 195f), new Vector2(520f, 42f)); // Pause 안내 위치 적용

            Button resume = CreateButton("Resume", panel.transform, "계속하기", new Vector2(0f, 90f)); // 계속하기 버튼 생성
            resume.onClick.AddListener(ClosePanel); // 게임 복귀 연결

            Button controls = CreateButton("Controls", panel.transform, "조작법", new Vector2(0f, -25f)); // 조작법 버튼 생성
            controls.onClick.AddListener(ShowControlsPanel); // 조작법 화면 연결

            Button settings = CreateButton("Settings", panel.transform, "설정", new Vector2(0f, -140f)); // 설정 버튼 생성
            settings.onClick.AddListener(ShowSettingsPanel); // 설정 화면 연결

            Button mainMenu = CreateButton("MainMenu", panel.transform, "메인 메뉴", new Vector2(0f, -255f)); // 메인 메뉴 버튼 생성
            mainMenu.onClick.AddListener(HandleReturnToMainMenu); // 메인 메뉴 전환 연결

            Text warning = CreateText("Warning", panel.transform, 15, FontStyle.Normal, TextAnchor.MiddleCenter); // 저장 경고 문구 생성
            warning.text = "전투 중 진행 상황은 안전 지점 저장에 포함되지 않습니다."; // 메인 메뉴 복귀 저장 안내 적용
            warning.color = new Color(1f, 0.72f, 0.48f, 0.92f); // 경고 글자 색상 적용
            SetRect(warning.rectTransform, new Vector2(0f, -335f), new Vector2(540f, 44f)); // 저장 경고 위치 적용
        }

        private void BuildControlsScreen(Transform parent)
        {
            _controlsRoot = CreateScreenRoot("ControlsRoot_Day68", parent); // 조작법 화면 루트 생성
            GameObject panel = CreateImage("ControlsPanel", _controlsRoot.transform, Vector2.zero, new Vector2(980f, 860f), PanelColor); // 조작법 중앙 패널 생성

            Text section = CreateText("Section", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 조작법 상단 분류 문구 생성
            section.text = "CONTROLS"; // 조작법 분류 문구 적용
            section.color = AccentColor; // 강조 색상 적용
            SetRect(section.rectTransform, new Vector2(-365f, 350f), new Vector2(740f, 40f)); // 분류 문구 위치 적용

            Text title = CreateText("Title", panel.transform, 42, FontStyle.Bold, TextAnchor.MiddleLeft); // 조작법 제목 생성
            title.text = "조작법"; // 조작법 제목 적용
            SetRect(title.rectTransform, new Vector2(-305f, 295f), new Vector2(860f, 60f)); // 조작법 제목 위치 적용

            Text body = CreateText("Body", panel.transform, 22, FontStyle.Normal, TextAnchor.UpperLeft); // 조작법 본문 생성
            body.text = "[전투]\n좌클릭  · 카드 / 기물 선택\nSpace   · 배치 턴 종료 / 플레이어 행동 완료\nESC     · 일시정지\n\n[Route Map]\n마우스 이동  · 선택 가능한 노드 정보 확인\n좌클릭       · 다음 Stage 선택\n\n[UI]\nESC     · 뒤로가기 / 닫기"; // 실제 현재 입력 기준 조작법 적용
            body.color = Color.white; // 본문 색상 적용
            body.lineSpacing = 1.2f; // 본문 줄 간격 적용
            SetRect(body.rectTransform, new Vector2(0f, 15f), new Vector2(780f, 470f)); // 조작법 본문 위치 적용

            Button replay = CreateButton("ReplayTutorial", panel.transform, "튜토리얼 다시 보기", new Vector2(-205f, -330f), new Vector2(350f, 68f)); // 튜토리얼 다시 보기 버튼 생성
            replay.onClick.AddListener(HandleReplayTutorial); // 튜토리얼 재표시 연결

            Button back = CreateButton("Back", panel.transform, "뒤로", new Vector2(205f, -330f), new Vector2(350f, 68f)); // Pause 복귀 버튼 생성
            back.onClick.AddListener(ShowPausePanel); // Pause 기본 화면 복귀 연결
        }

        private void BuildSettingsScreen(Transform parent)
        {
            _settingsRoot = new GameObject("SettingsRoot_Day68", typeof(RectTransform)); // Pause 설정 화면 루트 생성
            _settingsRoot.transform.SetParent(parent, false); // Pause Canvas 자식 연결
            Stretch(_settingsRoot.GetComponent<RectTransform>()); // 전체 화면 배치

            SettingsPanelController settingsPanel = _settingsRoot.AddComponent<SettingsPanelController>(); // 기존 정식 설정 패널 재사용
            settingsPanel.Initialize(ShowPausePanel); // 취소 버튼 Pause 복귀 연결
        }

        private void ApplyPanelState()
        {
            if (_pauseRoot != null) _pauseRoot.SetActive(_navigation.CurrentPanel == BattlePausePanel.Pause); // Pause 화면 표시 상태 적용
            if (_controlsRoot != null) _controlsRoot.SetActive(_navigation.CurrentPanel == BattlePausePanel.Controls); // 조작법 화면 표시 상태 적용
            if (_settingsRoot != null) _settingsRoot.SetActive(_navigation.CurrentPanel == BattlePausePanel.Settings); // 설정 화면 표시 상태 적용
        }

        private static GameObject CreateScreenRoot(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image)); // 전체 화면 Pause 하위 루트 생성
            root.transform.SetParent(parent, false); // Pause Canvas 자식 연결
            RectTransform rect = root.GetComponent<RectTransform>(); // 전체 화면 RectTransform 조회
            Stretch(rect); // 전체 화면 Stretch 적용
            Image backdrop = root.GetComponent<Image>(); // 전체 화면 배경 조회
            backdrop.color = BackdropColor; // 어두운 배경 적용
            return root; // 완성 화면 루트 반환
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 공통 Image 오브젝트 생성
            imageObject.transform.SetParent(parent, false); // 요청 UI 부모 연결
            SetRect(imageObject.GetComponent<RectTransform>(), position, size); // 위치·크기 적용
            imageObject.GetComponent<Image>().color = color; // 배경 색상 적용
            return imageObject; // 완성 Image 오브젝트 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2? customSize = null)
        {
            Vector2 size = customSize ?? new Vector2(480f, 76f); // 기본 버튼 크기 계산
            GameObject buttonObject = CreateImage(name, parent, position, size, ButtonColor); // 버튼 배경 생성
            Button button = buttonObject.AddComponent<Button>(); // Button 컴포넌트 추가
            button.targetGraphic = buttonObject.GetComponent<Image>(); // 버튼 대상 그래픽 지정

            ColorBlock colors = button.colors; // 버튼 상태 색상 조회
            colors.normalColor = ButtonColor; // 기본 버튼 색상 적용
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.38f, 1f); // Hover 색상 적용
            colors.pressedColor = new Color(0.08f, 0.12f, 0.17f, 1f); // Pressed 색상 적용
            colors.selectedColor = colors.highlightedColor; // 선택 색상 적용
            colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.7f); // 비활성 색상 적용
            colors.fadeDuration = 0.08f; // 상태 전환 시간 적용
            button.colors = colors; // 버튼 상태 색상 저장

            Text text = CreateText("Label", buttonObject.transform, 23, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 8f); // 버튼 내부 여백 적용
            return button; // 완성 Button 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 공통 Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 요청 UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 글자 정렬 적용
            text.color = Color.white; // 기본 흰색 적용
            text.raycastTarget = false; // 상위 버튼 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            new GameObject("EventSystem_Day68_Pause", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 시작 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 끝 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }

        private void OnDestroy()
        {
            ResumeRuntime(); // 플레이 종료·씬 전환 전 TimeScale·입력 복원
        }
    }
}
