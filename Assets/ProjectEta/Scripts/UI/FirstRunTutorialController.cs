using UnityEngine; // MonoBehaviour·GameObject·Time 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // InputSystemUIInputModule 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // BoardInputController·RouteMapBoardController 사용
using ProjectEta.SceneFlow; // Battle 씬 이름 사용
using ProjectEta.Settings; // 최초 튜토리얼 저장 설정 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1500)]
    public sealed class FirstRunTutorialController : MonoBehaviour
    {
        private static readonly string[] PageTitles =
        {
            "기물 배치",
            "이동과 공격",
            "King 보호",
            "Route Map",
            "카드 성장"
        }; // 튜토리얼 페이지 제목 목록

        private static FirstRunTutorialController _instance; // 현재 Battle 튜토리얼 인스턴스
        private GameObject _root; // 튜토리얼 전체 화면 루트
        private Text _pageIndexText; // 현재 페이지 번호 문구
        private Text _titleText; // 현재 페이지 제목
        private Text _bodyText; // 현재 페이지 본문
        private Text _nextLabel; // 다음·시작 버튼 문구
        private Button _previousButton; // 이전 페이지 버튼
        private BattleController _battleController; // 전투 컨트롤러 입력 중단 대상
        private BoardInputController _boardInputController; // 보드 입력 중단 대상
        private RouteMapBoardController _routeMapBoardController; // 지도 입력 중단 대상
        private bool _battleWasEnabled; // 튜토리얼 전 BattleController 상태
        private bool _boardInputWasEnabled; // 튜토리얼 전 보드 입력 상태
        private bool _routeMapWasEnabled; // 튜토리얼 전 지도 입력 상태
        private float _previousTimeScale = 1f; // 튜토리얼 전 TimeScale
        private int _pageIndex; // 현재 튜토리얼 페이지 인덱스
        private bool _isShowing; // 현재 튜토리얼 표시 여부
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public static bool IsAnyTutorialOpen => _instance != null && _instance._isShowing; // Pause 입력 차단용 튜토리얼 표시 상태
        public bool IsShowing => _isShowing; // 외부 현재 튜토리얼 표시 상태
        public int CurrentPageIndex => _pageIndex; // 외부 현재 페이지 인덱스 조회

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null; // Domain Reload 비활성 환경 인스턴스 초기화
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != SceneFlowController.BattleSceneName) return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<FirstRunTutorialController>() != null) return; // 중복 튜토리얼 관리자 차단

            GameObject host = new GameObject("FirstRunTutorialController_Day68"); // 최초 튜토리얼 호스트 생성
            host.AddComponent<FirstRunTutorialController>(); // 튜토리얼 관리자 추가
        }

        private void Awake()
        {
            _instance = this; // 현재 튜토리얼 인스턴스 등록
        }

        private void Start()
        {
            GameSettingsService.EnsureLoaded(); // 최초 튜토리얼 완료 상태 로드 보장
            EnsureEventSystem(); // 튜토리얼 버튼 입력 시스템 보장
            BuildUI(); // 최초 튜토리얼 화면 생성
            GameSettingsService.ReapplyUiScale(); // 신규 Canvas 저장 UI Scale 적용

            if (!GameSettingsService.Current.FirstTutorialCompleted)
            {
                ShowTutorial(); // 최초 플레이에서 자동 튜토리얼 표시
            }
        }

        public void ShowTutorial()
        {
            if (_isShowing || _root == null) return; // 중복 표시·UI 준비 전 차단

            CaptureAndSuspendGameplay(); // 전투 입력·시간 중단
            _pageIndex = 0; // 첫 페이지부터 시작
            _root.SetActive(true); // 튜토리얼 전체 화면 표시
            _isShowing = true; // 튜토리얼 표시 상태 기록
            RefreshPage(); // 첫 페이지 내용 적용
        }

        private void ShowPreviousPage()
        {
            if (!_isShowing || _pageIndex <= 0) return; // 첫 페이지 이전 이동 차단
            _pageIndex--; // 이전 페이지 이동
            RefreshPage(); // 변경 페이지 내용 적용
        }

        private void ShowNextPage()
        {
            if (!_isShowing) return; // 비표시 상태 입력 차단

            if (_pageIndex >= PageTitles.Length - 1)
            {
                CompleteTutorial(); // 마지막 페이지에서 튜토리얼 종료
                return; // 페이지 증가 차단
            }

            _pageIndex++; // 다음 페이지 이동
            RefreshPage(); // 변경 페이지 내용 적용
        }

        private void SkipTutorial()
        {
            if (!_isShowing) return; // 비표시 상태 입력 차단
            CompleteTutorial(); // 건너뛰기도 최초 안내 완료 처리
        }

        private void CompleteTutorial()
        {
            GameSettingsService.MarkFirstTutorialCompleted(); // 최초 튜토리얼 완료 상태 저장
            _root.SetActive(false); // 튜토리얼 화면 숨김
            _isShowing = false; // 튜토리얼 표시 상태 해제
            RestoreGameplay(); // 전투 입력·시간 복원
            string pauseKey = GameInputBindingService.GetDisplayName(GameInputAction.Pause); // 현재 Pause 키 표시 문구 조회
            SystemToastUI.Push("튜토리얼 완료", $"{pauseKey} → 조작법에서 다시 확인할 수 있습니다.", 1.8f); // 현재 조작키 기반 완료 안내 Toast 등록
        }

        private void CaptureAndSuspendGameplay()
        {
            _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 조회
            _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 현재 보드 입력 조회
            _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 현재 지도 입력 조회
            _battleWasEnabled = _battleController != null && _battleController.enabled; // 전투 컨트롤러 기존 상태 저장
            _boardInputWasEnabled = _boardInputController != null && _boardInputController.enabled; // 보드 입력 기존 상태 저장
            _routeMapWasEnabled = _routeMapBoardController != null && _routeMapBoardController.enabled; // 지도 입력 기존 상태 저장

            if (_battleController != null) _battleController.enabled = false; // 튜토리얼 중 행동 완료 전투 입력 차단
            if (_boardInputController != null) _boardInputController.enabled = false; // 튜토리얼 중 보드 입력 차단
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = false; // 튜토리얼 중 지도 입력 차단

            _previousTimeScale = Time.timeScale; // 기존 TimeScale 저장
            Time.timeScale = 0f; // 튜토리얼 중 게임 진행 중단
        }

        private void RestoreGameplay()
        {
            if (_battleController != null) _battleController.enabled = _battleWasEnabled; // 전투 컨트롤러 기존 상태 복원
            if (_boardInputController != null) _boardInputController.enabled = _boardInputWasEnabled; // 보드 입력 기존 상태 복원
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = _routeMapWasEnabled; // 지도 입력 기존 상태 복원
            Time.timeScale = _previousTimeScale; // 기존 TimeScale 복원
        }

        private void BuildUI()
        {
            GameObject canvasObject = new GameObject("FirstRunTutorialCanvas_Day68", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 튜토리얼 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 튜토리얼 호스트 자식 연결

            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 튜토리얼 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전체 화면 오버레이 적용
            canvas.sortingOrder = UiLayerOrder.Tutorial; // 공통 튜토리얼 계층 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 튜토리얼 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 적용

            _root = new GameObject("TutorialRoot", typeof(RectTransform), typeof(Image)); // 튜토리얼 전체 화면 루트 생성
            _root.transform.SetParent(canvasObject.transform, false); // 튜토리얼 Canvas 자식 연결
            Stretch(_root.GetComponent<RectTransform>()); // 전체 화면 입력 차단 영역 적용
            _root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f); // 어두운 튜토리얼 배경 적용

            GameObject panel = CreateImage("TutorialPanel", _root.transform, Vector2.zero, new Vector2(1040f, 700f), new Color(0.055f, 0.065f, 0.085f, 0.99f)); // 튜토리얼 중앙 패널 생성

            _pageIndexText = CreateText("PageIndex", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 페이지 번호 문구 생성
            _pageIndexText.color = new Color(0.38f, 0.68f, 0.88f, 1f); // 페이지 번호 강조 색상 적용
            SetRect(_pageIndexText.rectTransform, new Vector2(-390f, 270f), new Vector2(170f, 38f)); // 페이지 번호 위치 적용

            _titleText = CreateText("Title", panel.transform, 42, FontStyle.Bold, TextAnchor.MiddleLeft); // 튜토리얼 제목 생성
            SetRect(_titleText.rectTransform, new Vector2(0f, 205f), new Vector2(820f, 70f)); // 튜토리얼 제목 위치 적용

            _bodyText = CreateText("Body", panel.transform, 23, FontStyle.Normal, TextAnchor.UpperLeft); // 튜토리얼 본문 생성
            _bodyText.lineSpacing = 1.25f; // 본문 줄 간격 적용
            SetRect(_bodyText.rectTransform, new Vector2(0f, 20f), new Vector2(820f, 270f)); // 튜토리얼 본문 위치 적용

            _previousButton = CreateButton("Previous", panel.transform, "이전", new Vector2(-265f, -245f), new Vector2(230f, 68f)); // 이전 페이지 버튼 생성
            _previousButton.onClick.AddListener(ShowPreviousPage); // 이전 페이지 이동 연결

            Button skipButton = CreateButton("Skip", panel.transform, "건너뛰기", new Vector2(0f, -245f), new Vector2(230f, 68f)); // 건너뛰기 버튼 생성
            skipButton.onClick.AddListener(SkipTutorial); // 튜토리얼 건너뛰기 연결

            Button nextButton = CreateButton("Next", panel.transform, "다음", new Vector2(265f, -245f), new Vector2(230f, 68f)); // 다음 페이지 버튼 생성
            nextButton.onClick.AddListener(ShowNextPage); // 다음 페이지 이동 연결
            _nextLabel = nextButton.GetComponentInChildren<Text>(true); // 다음 버튼 문구 참조 저장

            _root.SetActive(false); // 최초 생성 시 튜토리얼 숨김
        }

        private void RefreshPage()
        {
            if (_pageIndexText == null || _titleText == null || _bodyText == null || _nextLabel == null) return; // UI 준비 전 페이지 갱신 차단

            int pageNumber = _pageIndex + 1; // 사용자 표시 페이지 번호 계산
            _pageIndexText.text = $"{pageNumber} / {PageTitles.Length}"; // 현재 페이지 번호 적용
            _titleText.text = PageTitles[_pageIndex]; // 현재 페이지 제목 적용
            _bodyText.text = GetPageBody(_pageIndex); // 현재 조작키를 반영한 페이지 본문 적용
            _nextLabel.text = _pageIndex == PageTitles.Length - 1 ? "시작" : "다음"; // 마지막 페이지 시작 문구 적용
            if (_previousButton != null) _previousButton.interactable = _pageIndex > 0; // 첫 페이지 이전 버튼 비활성 적용
        }

        private static string GetPageBody(int pageIndex)
        {
            string completeAction = GameInputBindingService.GetDisplayName(GameInputAction.CompleteAction); // 현재 행동 완료 키 문구 조회
            string pause = GameInputBindingService.GetDisplayName(GameInputAction.Pause); // 현재 Pause 키 문구 조회

            switch (pageIndex)
            {
                case 0:
                    return "손패의 카드를 선택해 전투 보드에 기물을 배치합니다.\n초기 배치에서는 King을 아군 영역에 반드시 배치해야 합니다."; // 배치 안내 반환
                case 1:
                    return $"보드의 기물을 선택하면 이동하거나 공격할 수 있는 대상이 표시됩니다.\n행동을 마치면 {completeAction}로 다음 흐름을 진행합니다."; // 현재 행동 완료 키 반영
                case 2:
                    return "King의 HP가 0이 되면 현재 런이 실패합니다.\n기물을 배치하고 경로를 선택할 때 King의 생존을 우선하세요."; // King 보호 안내 반환
                case 3:
                    return "전투에서 승리하면 Route Map에서 다음 Stage를 선택합니다.\n노드의 색상과 바닥 문양으로 Battle, Reward, Shop, Event, Boss를 구분할 수 있습니다."; // Route Map 안내 반환
                default:
                    return $"Reward와 Shop에서 카드를 확보하고 Fusion으로 더 강한 기물을 만들 수 있습니다.\n{pause}의 조작법 메뉴에서 이 안내를 다시 볼 수 있습니다."; // 현재 Pause 키 반영
            }
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 공통 Image 생성
            imageObject.transform.SetParent(parent, false); // 요청 UI 부모 연결
            SetRect(imageObject.GetComponent<RectTransform>(), position, size); // 위치·크기 적용
            imageObject.GetComponent<Image>().color = color; // 배경 색상 적용
            return imageObject; // 완성 Image 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = CreateImage(name, parent, position, size, new Color(0.13f, 0.17f, 0.23f, 1f)); // 버튼 배경 생성
            Button button = buttonObject.AddComponent<Button>(); // Button 컴포넌트 추가
            button.targetGraphic = buttonObject.GetComponent<Image>(); // 버튼 대상 그래픽 지정

            Text text = CreateText("Label", buttonObject.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 8f); // 버튼 내부 여백 적용
            return button; // 완성 Button 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 공통 Text 생성
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
            new GameObject("EventSystem_Day68_Tutorial", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
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
            if (_isShowing) RestoreGameplay(); // 씬 종료 중 튜토리얼 입력·시간 복원
            if (_instance == this) _instance = null; // 현재 정적 인스턴스 참조 정리
        }
    }
}
