using System.Collections.Generic; // IReadOnlyList<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // InputSystemUIInputModule 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Meta; // 영구 성장 상태·해금 사용
using ProjectEta.Run; // RunSaveSystem 사용
using ProjectEta.SceneFlow; // SceneFlowController 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(-100)]
    public sealed class MainMenuController : MonoBehaviour
    {
        private Canvas _canvas; // 메인 메뉴 Canvas
        private GameObject _mainRoot; // 메인 버튼 루트
        private GameObject _metaRoot; // 영구 성장 패널 루트
        private GameObject _settingsRoot; // 설정 패널 루트
        private Button _continueButton; // 이어하기 버튼
        private Text _continueStatusText; // 이어하기 상태 문구
        private Text _metaSummaryText; // 영구 성장 요약 문구
        private readonly List<Button> _unlockButtons = new List<Button>(); // 영구 해금 버튼 목록
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private void Start()
        {
            PrepareCamera(); // 기본 MainMenu 카메라 배경 정리
            EnsureEventSystem(); // 버튼 입력 EventSystem 보장
            BuildUI(); // 메인 메뉴 런타임 UI 생성
            RefreshMenuState(); // 세이브·메타 진행 상태 반영
        }

        private void BuildUI()
        {
            if (_canvas != null) return; // 중복 UI 생성 차단

            var canvasObject = new GameObject("MainMenuCanvas_Day54", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 메인 메뉴 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 모드 적용
            _canvas.sortingOrder = 100; // 기본 씬 UI 위 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 개발 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            CreateBackdrop(canvasObject.transform); // 전체 화면 배경 생성
            BuildMainPanel(canvasObject.transform); // 메인 메뉴 패널 생성
            BuildMetaPanel(canvasObject.transform); // 영구 성장 패널 생성
            BuildSettingsPanel(canvasObject.transform); // 설정 골격 패널 생성
        }

        private void CreateBackdrop(Transform parent)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)); // 전체 화면 배경 생성
            backdrop.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(backdrop.GetComponent<RectTransform>(), 0f); // 전체 화면 Stretch 적용
            backdrop.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.035f, 1f); // 어두운 메뉴 배경 적용
        }

        private void BuildMainPanel(Transform parent)
        {
            _mainRoot = new GameObject("MainMenuRoot", typeof(RectTransform), typeof(Image)); // 중앙 메인 메뉴 루트 생성
            _mainRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            SetRect(_mainRoot.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 900f)); // 중앙 패널 위치·크기 적용
            _mainRoot.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.105f, 0.97f); // 메뉴 패널 배경 적용

            Text title = CreateText("Title", _mainRoot.transform, 58, FontStyle.Bold); // 게임 제목 생성
            title.text = "PROJECT η"; // 프로젝트 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 330f), new Vector2(620f, 90f)); // 제목 위치 적용

            Text subtitle = CreateText("Subtitle", _mainRoot.transform, 20, FontStyle.Normal); // 개발 메뉴 설명 생성
            subtitle.text = "체스 기반 카드 배치 · 합성 로그라이트"; // 장르 설명 적용
            SetRect(subtitle.rectTransform, new Vector2(0f, 270f), new Vector2(620f, 48f)); // 설명 위치 적용

            Button newGameButton = CreateButton("NewGame", _mainRoot.transform, "새 게임", new Vector2(0f, 155f)); // 새 게임 버튼 생성
            newGameButton.onClick.AddListener(HandleNewGame); // 새 게임 씬 흐름 연결

            _continueButton = CreateButton("Continue", _mainRoot.transform, "이어하기", new Vector2(0f, 55f)); // 이어하기 버튼 생성
            _continueButton.onClick.AddListener(HandleContinue); // 이어하기 씬 흐름 연결

            Button metaButton = CreateButton("Meta", _mainRoot.transform, "영구 성장", new Vector2(0f, -45f)); // 영구 성장 버튼 생성
            metaButton.onClick.AddListener(ShowMetaPanel); // 영구 성장 패널 열기 연결

            Button settingsButton = CreateButton("Settings", _mainRoot.transform, "설정", new Vector2(0f, -145f)); // 설정 버튼 생성
            settingsButton.onClick.AddListener(ShowSettingsPanel); // 설정 패널 열기 연결

            Button quitButton = CreateButton("Quit", _mainRoot.transform, "게임 종료", new Vector2(0f, -245f)); // 게임 종료 버튼 생성
            quitButton.onClick.AddListener(SceneFlowController.QuitGame); // 빌드 종료 연결

            _continueStatusText = CreateText("ContinueStatus", _mainRoot.transform, 18, FontStyle.Normal); // 세이브 상태 문구 생성
            SetRect(_continueStatusText.rectTransform, new Vector2(0f, -345f), new Vector2(600f, 70f)); // 상태 문구 위치 적용
        }

        private void BuildMetaPanel(Transform parent)
        {
            _metaRoot = new GameObject("MetaRoot", typeof(RectTransform), typeof(Image)); // 영구 성장 패널 루트 생성
            _metaRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            SetRect(_metaRoot.GetComponent<RectTransform>(), Vector2.zero, new Vector2(980f, 900f)); // 중앙 패널 위치·크기 적용
            _metaRoot.GetComponent<Image>().color = new Color(0.09f, 0.07f, 0.045f, 0.99f); // 영구 성장 패널 배경 적용

            Text title = CreateText("MetaTitle", _metaRoot.transform, 42, FontStyle.Bold); // 영구 성장 제목 생성
            title.text = "영구 성장"; // 영구 성장 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 365f), new Vector2(820f, 70f)); // 제목 위치 적용

            _metaSummaryText = CreateText("MetaSummary", _metaRoot.transform, 24, FontStyle.Bold); // 메타 토큰 요약 생성
            SetRect(_metaSummaryText.rectTransform, new Vector2(0f, 285f), new Vector2(820f, 70f)); // 요약 위치 적용

            IReadOnlyList<MetaUnlockDefinition> definitions = MetaUnlockCatalog.All; // 현재 영구 해금 정의 목록 조회

            for (int i = 0; i < definitions.Count; i++)
            {
                int column = i % 2; // 해금 버튼 열 계산
                int row = i / 2; // 해금 버튼 행 계산
                float x = column == 0 ? -225f : 225f; // 좌우 열 위치 계산
                float y = 160f - row * 110f; // 해금 버튼 행 위치 계산
                Button button = CreateButton($"MetaUnlock_{i}", _metaRoot.transform, string.Empty, new Vector2(x, y), new Vector2(410f, 84f)); // 영구 해금 버튼 생성
                MetaUnlockDefinition captured = definitions[i]; // 버튼 콜백용 정의 고정
                button.onClick.AddListener(() => HandleUnlock(captured)); // 영구 해금 실행 연결
                _unlockButtons.Add(button); // 갱신용 버튼 목록 등록
            }

            Button close = CreateButton("MetaClose", _metaRoot.transform, "닫기", new Vector2(0f, -350f), new Vector2(300f, 70f)); // 영구 성장 닫기 버튼 생성
            close.onClick.AddListener(HidePanels); // 메인 메뉴 복귀 연결
            _metaRoot.SetActive(false); // 기본 숨김 상태 적용
        }

        private void BuildSettingsPanel(Transform parent)
        {
            _settingsRoot = new GameObject("SettingsRoot", typeof(RectTransform), typeof(Image)); // 설정 골격 패널 생성
            _settingsRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            SetRect(_settingsRoot.GetComponent<RectTransform>(), Vector2.zero, new Vector2(820f, 620f)); // 설정 패널 위치·크기 적용
            _settingsRoot.GetComponent<Image>().color = new Color(0.065f, 0.07f, 0.085f, 0.99f); // 설정 패널 배경 적용

            Text title = CreateText("SettingsTitle", _settingsRoot.transform, 40, FontStyle.Bold); // 설정 제목 생성
            title.text = "설정"; // 설정 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 210f), new Vector2(650f, 70f)); // 제목 위치 적용

            Text body = CreateText("SettingsBody", _settingsRoot.transform, 24, FontStyle.Normal); // 설정 안내 생성
            body.text = "54일차 프로토타입\n해상도 · 음량 · 그래픽 옵션은 UI/UX 단계에서 확장합니다."; // 설정 골격 안내 적용
            SetRect(body.rectTransform, new Vector2(0f, 40f), new Vector2(680f, 180f)); // 안내 위치 적용

            Button close = CreateButton("SettingsClose", _settingsRoot.transform, "닫기", new Vector2(0f, -205f), new Vector2(300f, 70f)); // 설정 닫기 버튼 생성
            close.onClick.AddListener(HidePanels); // 메인 메뉴 복귀 연결
            _settingsRoot.SetActive(false); // 기본 숨김 상태 적용
        }

        private void HandleNewGame()
        {
            SetMainButtonsInteractable(false); // 씬 전환 중 메뉴 연타 차단

            if (!SceneFlowController.StartNewRun())
            {
                SetMainButtonsInteractable(true); // 전환 실패 시 메뉴 입력 복구
                RefreshMenuState(); // 현재 세이브 상태 재표시
            }
        }

        private void HandleContinue()
        {
            SetMainButtonsInteractable(false); // 씬 전환 중 메뉴 연타 차단

            if (!SceneFlowController.ContinueRun())
            {
                SetMainButtonsInteractable(true); // 이어하기 실패 시 메뉴 입력 복구
                RefreshMenuState(); // 세이브 유효성 재표시
            }
        }

        private void ShowMetaPanel()
        {
            RefreshMetaPanel(); // 최신 메타 진행 상태 반영
            _mainRoot.SetActive(false); // 메인 메뉴 버튼 숨김
            _settingsRoot.SetActive(false); // 설정 패널 숨김
            _metaRoot.SetActive(true); // 영구 성장 패널 표시
        }

        private void ShowSettingsPanel()
        {
            _mainRoot.SetActive(false); // 메인 메뉴 버튼 숨김
            _metaRoot.SetActive(false); // 영구 성장 패널 숨김
            _settingsRoot.SetActive(true); // 설정 패널 표시
        }

        private void HidePanels()
        {
            _metaRoot.SetActive(false); // 영구 성장 패널 숨김
            _settingsRoot.SetActive(false); // 설정 패널 숨김
            _mainRoot.SetActive(true); // 메인 메뉴 버튼 표시
            RefreshMenuState(); // 복귀 시 최신 상태 갱신
        }

        private void HandleUnlock(MetaUnlockDefinition definition)
        {
            MetaProgressState progress = MetaProgressService.Current; // 현재 영구 성장 상태 조회
            if (!MetaUnlockService.TryUnlock(progress, definition)) return; // 비용 부족·이미 해금 상태 차단

            MetaProgressService.Save(); // 해금·토큰 변화를 즉시 디스크 저장
            RefreshMetaPanel(); // 해금 상태·잔액 즉시 갱신
        }

        private void RefreshMenuState()
        {
            bool canContinue = RunSaveSystem.CanContinue; // 현재 안전 세이브 유효성 조회
            if (_continueButton != null) _continueButton.interactable = canContinue && !SceneFlowController.IsTransitioning; // 이어하기 가능 상태 적용

            if (_continueStatusText != null)
            {
                _continueStatusText.text = canContinue
                    ? "안전 지점 저장 데이터가 있습니다."
                    : "이어할 수 있는 런 저장 데이터가 없습니다."; // 이어하기 상태 안내 적용
            }
        }

        private void RefreshMetaPanel()
        {
            if (_metaSummaryText == null) return; // 영구 성장 UI 준비 전 차단

            MetaProgressState progress = MetaProgressService.Current; // 현재 영구 진행 상태 조회
            _metaSummaryText.text = $"메타 토큰 {progress.MetaTokens}  ·  기물 {progress.UnlockedPieceIds.Count}  ·  킹 {progress.UnlockedKingIds.Count}  ·  패시브 {progress.UnlockedPassiveIds.Count}"; // 영구 진행 요약 적용
            IReadOnlyList<MetaUnlockDefinition> definitions = MetaUnlockCatalog.All; // 해금 정의 목록 조회

            for (int i = 0; i < _unlockButtons.Count; i++)
            {
                Button button = _unlockButtons[i]; // 현재 해금 버튼 조회
                if (i >= definitions.Count)
                {
                    button.gameObject.SetActive(false); // 남는 버튼 숨김
                    continue;
                }

                MetaUnlockDefinition definition = definitions[i]; // 현재 해금 정의 조회
                bool unlocked = progress.IsUnlocked(definition.UnlockType, definition.UnlockId); // 영구 해금 여부 조회
                bool canUnlock = MetaUnlockService.CanUnlock(progress, definition); // 비용 충족 여부 조회
                Text label = button.GetComponentInChildren<Text>(true); // 버튼 Label 조회

                if (label != null)
                {
                    label.text = unlocked
                        ? $"{definition.DisplayName}\n해금 완료"
                        : $"{definition.DisplayName}\n{definition.Cost} Token"; // 해금 상태·비용 표시
                }

                button.interactable = canUnlock; // 해금 가능 여부 버튼 적용
                button.gameObject.SetActive(true); // 정의 버튼 표시
            }
        }

        private void SetMainButtonsInteractable(bool interactable)
        {
            if (_mainRoot == null) return; // 메인 메뉴 루트 누락 방어
            Button[] buttons = _mainRoot.GetComponentsInChildren<Button>(true); // 메인 메뉴 버튼 전체 조회

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = interactable; // 씬 전환 입력 상태 일괄 적용
            }

            if (interactable) RefreshMenuState(); // 입력 복구 시 이어하기 유효성 다시 적용
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2? customSize = null)
        {
            Vector2 size = customSize ?? new Vector2(480f, 76f); // 기본 버튼 크기 계산
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 런타임 Button 생성
            buttonObject.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
            image.color = new Color(0.18f, 0.22f, 0.29f, 1f); // 메뉴 버튼 배경 적용
            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 지정

            Text text = CreateText("Label", buttonObject.transform, 24, FontStyle.Bold); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 8f); // 버튼 내부 여백 적용
            return button; // 완성 Button 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 글자 적용
            text.raycastTarget = false; // 버튼 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            new GameObject("EventSystem_Day54", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
        }

        private static void PrepareCamera()
        {
            Camera camera = Camera.main; // MainMenu 기본 카메라 조회
            if (camera == null) return; // 카메라 누락 허용
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 메뉴 배경 적용
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f); // UI 배경과 맞는 어두운 색 적용
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
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
    }
}
