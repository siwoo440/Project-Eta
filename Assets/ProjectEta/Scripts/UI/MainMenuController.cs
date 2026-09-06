using System.Collections.Generic; // IReadOnlyList<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem; // Keyboard 입력 사용
using UnityEngine.InputSystem.UI; // InputSystemUIInputModule 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Meta; // 영구 성장 상태·해금 사용
using ProjectEta.Run; // RunSaveSystem·RunContinueInfo 사용
using ProjectEta.SceneFlow; // SceneFlowController 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(-100)]
    public sealed class MainMenuController : MonoBehaviour
    {
        private readonly MainMenuNavigationState _navigation = new MainMenuNavigationState(); // 메뉴 화면 내비게이션 상태
        private readonly List<Button> _unlockButtons = new List<Button>(); // 영구 해금 버튼 목록
        private Canvas _canvas; // 메인 메뉴 Canvas
        private GameObject _mainRoot; // 메인 화면 루트
        private GameObject _metaRoot; // 영구 성장 패널 루트
        private GameObject _settingsRoot; // 설정 패널 루트
        private GameObject _modalRoot; // 확인 팝업 오버레이 루트
        private Button _continueButton; // 이어하기 버튼
        private Text _continueStatusText; // 이어하기 상태 카드 문구
        private Text _metaSummaryText; // 영구 성장 요약 문구
        private Text _modalTitleText; // 확인 팝업 제목
        private Text _modalBodyText; // 확인 팝업 본문
        private Text _modalConfirmLabel; // 확인 팝업 확정 버튼 문구
        private Button _modalConfirmButton; // 확인 팝업 확정 버튼
        private Button _modalCancelButton; // 확인 팝업 취소 버튼
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private static readonly Color BackdropColor = new Color(0.012f, 0.016f, 0.024f, 1f); // 전체 배경 색상
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.085f, 0.98f); // 기본 패널 색상
        private static readonly Color PanelSecondaryColor = new Color(0.035f, 0.043f, 0.058f, 0.98f); // 보조 패널 색상
        private static readonly Color AccentColor = new Color(0.38f, 0.68f, 0.88f, 1f); // 핵심 강조 색상
        private static readonly Color SoftTextColor = new Color(0.68f, 0.72f, 0.79f, 1f); // 보조 텍스트 색상
        private static readonly Color DangerColor = new Color(0.68f, 0.22f, 0.24f, 1f); // 위험 동작 색상

        private void Start()
        {
            PrepareCamera(); // MainMenu 카메라 배경 정리
            EnsureEventSystem(); // 버튼 입력 EventSystem 보장
            BuildUI(); // 55일차 정식 MainMenu UI 생성
            RefreshMenuState(); // 이어하기·메타 상태 반영
            ApplyPanelState(); // 최초 메인 화면 상태 적용
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 입력 장치 조회
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return; // ESC 입력이 없으면 처리 종료
            if (!_navigation.TryBack()) return; // 서브 패널·팝업 상태에서만 뒤로가기 처리

            ApplyPanelState(); // 메인 메뉴 화면 복귀 적용
            RefreshMenuState(); // 복귀 시 이어하기 상태 갱신
        }

        private void BuildUI()
        {
            if (_canvas != null) return; // 중복 UI 생성 차단

            var canvasObject = new GameObject("MainMenuCanvas_Day55", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 정식 MainMenu Canvas 생성
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
            BuildMainPanel(canvasObject.transform); // 메인 메뉴 화면 생성
            BuildMetaPanel(canvasObject.transform); // 영구 성장 패널 생성
            BuildSettingsPanel(canvasObject.transform); // 설정 진입 패널 생성
            BuildModal(canvasObject.transform); // 공통 확인 팝업 생성
        }

        private void CreateBackdrop(Transform parent)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)); // 전체 화면 배경 생성
            backdrop.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(backdrop.GetComponent<RectTransform>(), 0f); // 전체 화면 Stretch 적용
            backdrop.GetComponent<Image>().color = BackdropColor; // 정식 어두운 배경 색상 적용

            var glow = new GameObject("BackdropGlow", typeof(RectTransform), typeof(Image)); // 좌측 브랜드 영역 광원 띠 생성
            glow.transform.SetParent(parent, false); // Canvas 자식 연결
            SetRect(glow.GetComponent<RectTransform>(), new Vector2(-620f, 0f), new Vector2(760f, 1080f)); // 좌측 배경 띠 위치 적용
            glow.GetComponent<Image>().color = new Color(0.07f, 0.16f, 0.23f, 0.28f); // 은은한 청색 강조 적용

            var divider = new GameObject("BackdropDivider", typeof(RectTransform), typeof(Image)); // 화면 중심 구분선 생성
            divider.transform.SetParent(parent, false); // Canvas 자식 연결
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(2f, 860f)); // 중앙 세로 구분선 위치 적용
            divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f); // 낮은 대비 구분선 적용
        }

        private void BuildMainPanel(Transform parent)
        {
            _mainRoot = new GameObject("MainMenuRoot", typeof(RectTransform)); // 메인 메뉴 전체 루트 생성
            _mainRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(_mainRoot.GetComponent<RectTransform>(), 0f); // 전체 화면 기준 배치

            GameObject brandPanel = CreatePanel("BrandPanel", _mainRoot.transform, new Vector2(-430f, 0f), new Vector2(760f, 820f), PanelSecondaryColor); // 좌측 브랜드 패널 생성
            GameObject menuPanel = CreatePanel("MenuPanel", _mainRoot.transform, new Vector2(430f, 0f), new Vector2(620f, 820f), PanelColor); // 우측 메뉴 패널 생성

            Text eyebrow = CreateText("Eyebrow", brandPanel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 브랜드 상단 분류 문구 생성
            eyebrow.text = "TURN-BASED ROGUELITE STRATEGY"; // 장르 키워드 적용
            eyebrow.color = AccentColor; // 강조 색상 적용
            SetRect(eyebrow.rectTransform, new Vector2(0f, 300f), new Vector2(610f, 40f)); // 분류 문구 위치 적용

            Text title = CreateText("Title", brandPanel.transform, 64, FontStyle.Bold, TextAnchor.MiddleLeft); // 게임 제목 생성
            title.text = "PROJECT η"; // 프로젝트 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 220f), new Vector2(610f, 100f)); // 제목 위치 적용

            Text subtitle = CreateText("Subtitle", brandPanel.transform, 24, FontStyle.Normal, TextAnchor.UpperLeft); // 게임 설명 생성
            subtitle.text = "카드를 기물로 배치하고\n체스판 위에서 경로와 전투를 이어가는\n턴제 로그라이트 전략 게임"; // 현재 프로젝트 장르 설명 적용
            subtitle.color = SoftTextColor; // 보조 텍스트 색상 적용
            subtitle.lineSpacing = 1.25f; // 문단 가독성 간격 적용
            SetRect(subtitle.rectTransform, new Vector2(0f, 65f), new Vector2(610f, 170f)); // 게임 설명 위치 적용

            _continueStatusText = CreateText("ContinueStatus", brandPanel.transform, 21, FontStyle.Normal, TextAnchor.UpperLeft); // 이어하기 상태 카드 문구 생성
            _continueStatusText.lineSpacing = 1.2f; // 상태 카드 줄 간격 적용
            SetRect(_continueStatusText.rectTransform, new Vector2(0f, -170f), new Vector2(610f, 190f)); // 상태 카드 문구 위치 적용

            Text footer = CreateText("Footer", brandPanel.transform, 17, FontStyle.Normal, TextAnchor.LowerLeft); // 하단 안내 문구 생성
            footer.text = "ESC  뒤로가기"; // 공통 뒤로가기 안내 적용
            footer.color = new Color(1f, 1f, 1f, 0.42f); // 낮은 대비 안내 색상 적용
            SetRect(footer.rectTransform, new Vector2(0f, -350f), new Vector2(610f, 40f)); // 하단 안내 위치 적용

            Text menuTitle = CreateText("MenuTitle", menuPanel.transform, 26, FontStyle.Bold, TextAnchor.MiddleLeft); // 메뉴 제목 생성
            menuTitle.text = "MAIN MENU"; // 메뉴 제목 적용
            menuTitle.color = SoftTextColor; // 보조 강조 색상 적용
            SetRect(menuTitle.rectTransform, new Vector2(0f, 320f), new Vector2(480f, 50f)); // 메뉴 제목 위치 적용

            Button newGameButton = CreateButton("NewGame", menuPanel.transform, "새 게임", new Vector2(0f, 205f)); // 새 게임 버튼 생성
            newGameButton.onClick.AddListener(HandleNewGame); // 새 게임 확인·전환 흐름 연결

            _continueButton = CreateButton("Continue", menuPanel.transform, "이어하기", new Vector2(0f, 100f)); // 이어하기 버튼 생성
            _continueButton.onClick.AddListener(HandleContinue); // 이어하기 씬 흐름 연결

            Button metaButton = CreateButton("Meta", menuPanel.transform, "영구 성장", new Vector2(0f, -5f)); // 영구 성장 버튼 생성
            metaButton.onClick.AddListener(ShowMetaPanel); // 영구 성장 패널 열기 연결

            Button settingsButton = CreateButton("Settings", menuPanel.transform, "설정", new Vector2(0f, -110f)); // 설정 버튼 생성
            settingsButton.onClick.AddListener(ShowSettingsPanel); // 설정 패널 열기 연결

            Button quitButton = CreateButton("Quit", menuPanel.transform, "게임 종료", new Vector2(0f, -215f), null, true); // 게임 종료 버튼 생성
            quitButton.onClick.AddListener(HandleQuit); // 종료 확인 팝업 연결

            Text version = CreateText("Version", menuPanel.transform, 16, FontStyle.Normal, TextAnchor.MiddleRight); // 개발 버전 문구 생성
            version.text = "MAIN MENU · DAY 55"; // 현재 UI 개발 일차 표시
            version.color = new Color(1f, 1f, 1f, 0.34f); // 낮은 대비 버전 표시 적용
            SetRect(version.rectTransform, new Vector2(0f, -345f), new Vector2(480f, 36f)); // 버전 문구 위치 적용
        }

        private void BuildMetaPanel(Transform parent)
        {
            _metaRoot = new GameObject("MetaRoot", typeof(RectTransform)); // 영구 성장 전체 루트 생성
            _metaRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(_metaRoot.GetComponent<RectTransform>(), 0f); // 전체 화면 배치

            GameObject panel = CreatePanel("MetaPanel", _metaRoot.transform, Vector2.zero, new Vector2(1080f, 900f), PanelColor); // 영구 성장 중앙 패널 생성
            Text section = CreateText("MetaSection", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 영구 성장 분류 문구 생성
            section.text = "PERMANENT PROGRESSION"; // 분류 문구 적용
            section.color = AccentColor; // 강조 색상 적용
            SetRect(section.rectTransform, new Vector2(0f, 370f), new Vector2(900f, 40f)); // 분류 문구 위치 적용

            Text title = CreateText("MetaTitle", panel.transform, 46, FontStyle.Bold, TextAnchor.MiddleLeft); // 영구 성장 제목 생성
            title.text = "영구 성장"; // 영구 성장 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 315f), new Vector2(900f, 70f)); // 제목 위치 적용

            _metaSummaryText = CreateText("MetaSummary", panel.transform, 22, FontStyle.Bold, TextAnchor.MiddleLeft); // 메타 토큰 요약 생성
            SetRect(_metaSummaryText.rectTransform, new Vector2(0f, 250f), new Vector2(900f, 50f)); // 요약 위치 적용

            IReadOnlyList<MetaUnlockDefinition> definitions = MetaUnlockCatalog.All; // 현재 영구 해금 정의 목록 조회
            for (int i = 0; i < definitions.Count; i++)
            {
                int column = i % 2; // 해금 버튼 열 계산
                int row = i / 2; // 해금 버튼 행 계산
                float x = column == 0 ? -235f : 235f; // 좌우 열 위치 계산
                float y = 120f - row * 105f; // 해금 버튼 행 위치 계산
                Button button = CreateButton($"MetaUnlock_{i}", panel.transform, string.Empty, new Vector2(x, y), new Vector2(430f, 78f)); // 영구 해금 버튼 생성
                MetaUnlockDefinition captured = definitions[i]; // 버튼 콜백용 정의 고정
                button.onClick.AddListener(() => HandleUnlock(captured)); // 영구 해금 실행 연결
                _unlockButtons.Add(button); // 갱신용 버튼 목록 등록
            }

            Button close = CreateButton("MetaClose", panel.transform, "뒤로", new Vector2(0f, -365f), new Vector2(260f, 64f)); // 영구 성장 뒤로 버튼 생성
            close.onClick.AddListener(HandleBack); // 메인 메뉴 복귀 연결
            _metaRoot.SetActive(false); // 기본 숨김 상태 적용
        }

        private void BuildSettingsPanel(Transform parent)
        {
            _settingsRoot = new GameObject("SettingsRoot", typeof(RectTransform)); // 설정 전체 루트 생성
            _settingsRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(_settingsRoot.GetComponent<RectTransform>(), 0f); // 전체 화면 배치

            GameObject panel = CreatePanel("SettingsPanel", _settingsRoot.transform, Vector2.zero, new Vector2(900f, 640f), PanelColor); // 설정 중앙 패널 생성
            Text section = CreateText("SettingsSection", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 설정 분류 문구 생성
            section.text = "OPTIONS"; // 설정 분류 문구 적용
            section.color = AccentColor; // 강조 색상 적용
            SetRect(section.rectTransform, new Vector2(0f, 230f), new Vector2(720f, 40f)); // 분류 문구 위치 적용

            Text title = CreateText("SettingsTitle", panel.transform, 44, FontStyle.Bold, TextAnchor.MiddleLeft); // 설정 제목 생성
            title.text = "설정"; // 설정 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 170f), new Vector2(720f, 70f)); // 제목 위치 적용

            Text body = CreateText("SettingsBody", panel.transform, 23, FontStyle.Normal, TextAnchor.UpperLeft); // 설정 안내 생성
            body.text = "56일차 설정 시스템에서 다음 항목을 연결합니다.\n\n· 해상도\n· 전체화면 / 창모드\n· UI Scale\n· 적용 / 취소 / 초기화\n· 설정 저장 / 복원"; // 다음 일차 설정 범위 안내 적용
            body.color = SoftTextColor; // 보조 텍스트 색상 적용
            body.lineSpacing = 1.25f; // 설정 안내 줄 간격 적용
            SetRect(body.rectTransform, new Vector2(0f, -5f), new Vector2(720f, 280f)); // 설정 안내 위치 적용

            Button close = CreateButton("SettingsClose", panel.transform, "뒤로", new Vector2(0f, -245f), new Vector2(260f, 64f)); // 설정 뒤로 버튼 생성
            close.onClick.AddListener(HandleBack); // 메인 메뉴 복귀 연결
            _settingsRoot.SetActive(false); // 기본 숨김 상태 적용
        }

        private void BuildModal(Transform parent)
        {
            _modalRoot = new GameObject("ModalRoot", typeof(RectTransform), typeof(Image)); // 공통 확인 오버레이 생성
            _modalRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(_modalRoot.GetComponent<RectTransform>(), 0f); // 전체 화면 Stretch 적용
            _modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f); // 배경 차단 오버레이 적용

            GameObject panel = CreatePanel("ModalPanel", _modalRoot.transform, Vector2.zero, new Vector2(720f, 420f), new Color(0.07f, 0.08f, 0.105f, 1f)); // 확인 팝업 패널 생성
            _modalTitleText = CreateText("ModalTitle", panel.transform, 34, FontStyle.Bold, TextAnchor.MiddleLeft); // 팝업 제목 생성
            SetRect(_modalTitleText.rectTransform, new Vector2(0f, 125f), new Vector2(580f, 60f)); // 팝업 제목 위치 적용

            _modalBodyText = CreateText("ModalBody", panel.transform, 21, FontStyle.Normal, TextAnchor.UpperLeft); // 팝업 본문 생성
            _modalBodyText.color = SoftTextColor; // 팝업 본문 보조 색상 적용
            _modalBodyText.lineSpacing = 1.2f; // 팝업 본문 줄 간격 적용
            SetRect(_modalBodyText.rectTransform, new Vector2(0f, 20f), new Vector2(580f, 150f)); // 팝업 본문 위치 적용

            _modalCancelButton = CreateButton("ModalCancel", panel.transform, "취소", new Vector2(-155f, -125f), new Vector2(250f, 66f)); // 팝업 취소 버튼 생성
            _modalCancelButton.onClick.AddListener(HandleBack); // 팝업 취소 연결

            _modalConfirmButton = CreateButton("ModalConfirm", panel.transform, string.Empty, new Vector2(155f, -125f), new Vector2(250f, 66f), true); // 팝업 확정 버튼 생성
            _modalConfirmLabel = _modalConfirmButton.GetComponentInChildren<Text>(true); // 팝업 확정 문구 참조 저장
            _modalRoot.SetActive(false); // 기본 팝업 숨김 적용
        }

        private void HandleNewGame()
        {
            bool startImmediately = _navigation.RequestNewGame(RunSaveSystem.CanContinue); // 현재 이어하기 데이터 기준 새 게임 분기
            if (startImmediately)
            {
                BeginNewRun(); // 기존 진행이 없으면 즉시 새 게임 시작
                return; // 확인 팝업 처리 생략
            }

            ConfigureNewGameModal(); // 기존 진행 삭제 확인 팝업 구성
            ApplyPanelState(); // 새 게임 확인 팝업 표시
        }

        private void HandleContinue()
        {
            SetAllButtonsInteractable(false); // 씬 전환 중 모든 메뉴 입력 차단

            if (SceneFlowController.ContinueRun()) return; // 이어하기 씬 전환 성공 시 현재 메뉴 종료 대기

            _navigation.ShowMain(); // 이어하기 실패 시 메인 화면 복구
            SetAllButtonsInteractable(true); // 메뉴 입력 복구
            ApplyPanelState(); // 메인 화면 상태 재적용
            RefreshMenuState(); // 저장 유효성 재표시
        }

        private void HandleQuit()
        {
            _navigation.RequestQuit(); // 종료 확인 상태 진입
            ConfigureQuitModal(); // 종료 확인 팝업 구성
            ApplyPanelState(); // 종료 확인 팝업 표시
        }

        private void BeginNewRun()
        {
            SetAllButtonsInteractable(false); // 씬 전환 중 모든 메뉴 입력 차단

            if (SceneFlowController.StartNewRun()) return; // 새 게임 씬 전환 성공 시 현재 메뉴 종료 대기

            _navigation.ShowMain(); // 새 게임 실패 시 메인 화면 복구
            SetAllButtonsInteractable(true); // 메뉴 입력 복구
            ApplyPanelState(); // 메인 화면 상태 재적용
            RefreshMenuState(); // 저장 상태 재표시
        }

        private void ShowMetaPanel()
        {
            _navigation.ShowMeta(); // 영구 성장 패널 상태 진입
            RefreshMetaPanel(); // 최신 메타 진행 상태 반영
            ApplyPanelState(); // 영구 성장 패널 표시
        }

        private void ShowSettingsPanel()
        {
            _navigation.ShowSettings(); // 설정 패널 상태 진입
            ApplyPanelState(); // 설정 패널 표시
        }

        private void HandleBack()
        {
            if (!_navigation.TryBack()) return; // 메인 화면에서 중복 뒤로가기 차단

            ApplyPanelState(); // 메인 화면 복귀 적용
            RefreshMenuState(); // 복귀 시 이어하기 상태 갱신
        }

        private void ConfigureNewGameModal()
        {
            _modalTitleText.text = "새 게임을 시작하시겠습니까?"; // 새 게임 확인 제목 적용
            _modalBodyText.text = "이어할 수 있는 런이 있습니다.\n새 게임을 시작하면 현재 Run Save가 삭제됩니다.\n영구 성장 데이터는 유지됩니다."; // 저장 삭제 범위 안내 적용
            _modalConfirmLabel.text = "새 게임 시작"; // 새 게임 확정 문구 적용
            _modalConfirmButton.onClick.RemoveAllListeners(); // 이전 팝업 확정 동작 제거
            _modalConfirmButton.onClick.AddListener(BeginNewRun); // 새 게임 시작 동작 연결
        }

        private void ConfigureQuitModal()
        {
            _modalTitleText.text = "게임을 종료하시겠습니까?"; // 게임 종료 확인 제목 적용
            _modalBodyText.text = "현재 저장된 안전 지점 데이터는 유지됩니다.\n저장되지 않은 진행 상황은 복원되지 않을 수 있습니다."; // 종료 영향 안내 적용
            _modalConfirmLabel.text = "게임 종료"; // 게임 종료 확정 문구 적용
            _modalConfirmButton.onClick.RemoveAllListeners(); // 이전 팝업 확정 동작 제거
            _modalConfirmButton.onClick.AddListener(SceneFlowController.QuitGame); // 실제 게임 종료 동작 연결
        }

        private void ApplyPanelState()
        {
            MainMenuPanel panel = _navigation.CurrentPanel; // 현재 메뉴 화면 상태 조회
            bool modalVisible = panel == MainMenuPanel.NewGameConfirm || panel == MainMenuPanel.QuitConfirm; // 현재 확인 팝업 표시 여부 계산
            if (_mainRoot != null) _mainRoot.SetActive(panel == MainMenuPanel.Main || modalVisible); // 메인 화면·팝업 배경 표시 상태 적용
            if (_metaRoot != null) _metaRoot.SetActive(panel == MainMenuPanel.Meta); // 영구 성장 패널 표시 상태 적용
            if (_settingsRoot != null) _settingsRoot.SetActive(panel == MainMenuPanel.Settings); // 설정 패널 표시 상태 적용
            if (_modalRoot != null) _modalRoot.SetActive(modalVisible); // 확인 팝업 표시 상태 적용
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
            bool canContinue = RunSaveSystem.TryGetContinueInfo(out RunContinueInfo info); // 현재 안전 저장 요약 조회
            if (_continueButton != null) _continueButton.interactable = canContinue && !SceneFlowController.IsTransitioning; // 이어하기 가능 상태 적용
            if (_continueStatusText == null) return; // 상태 카드 생성 전 처리 차단

            if (!canContinue)
            {
                _continueStatusText.text = "새로운 런을 시작할 수 있습니다.\n\n이어하기 가능한 안전 저장 데이터가 없습니다."; // 신규 런 중심 상태 안내 적용
                _continueStatusText.color = SoftTextColor; // 비활성 상태 카드 색상 적용
                return; // 이어하기 요약 처리 종료
            }

            string phaseLabel = GetFlowLabel(info.FlowPhase); // 진행 흐름 한글 표시 생성
            _continueStatusText.text = $"이어하기 가능\n\nSTAGE {info.Stage} · {phaseLabel}\nKING HP {info.KingHp} · GOLD {info.Gold}\n방문 경로 {info.VisitedNodeCount}"; // 실제 저장 데이터 진행 요약 표시
            _continueStatusText.color = Color.white; // 이어하기 가능 상태 강조 적용
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
                    continue; // 다음 버튼 처리
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

        private void SetAllButtonsInteractable(bool interactable)
        {
            if (_canvas == null) return; // Canvas 누락 방어
            Button[] buttons = _canvas.GetComponentsInChildren<Button>(true); // 모든 MainMenu 버튼 조회

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = interactable; // 씬 전환 입력 상태 일괄 적용
            }

            if (interactable) RefreshMenuState(); // 입력 복구 시 이어하기 상태 다시 적용
        }

        private static string GetFlowLabel(RunFlowPhase phase)
        {
            switch (phase)
            {
                case RunFlowPhase.Map:
                    return "경로 지도"; // Map 한글 표시 반환
                case RunFlowPhase.Reward:
                    return "보상"; // Reward 한글 표시 반환
                case RunFlowPhase.Shop:
                    return "상점"; // Shop 한글 표시 반환
                case RunFlowPhase.Event:
                    return "이벤트"; // Event 한글 표시 반환
                default:
                    return phase.ToString(); // 예외 흐름 원본 문자열 반환
            }
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 공통 패널 GameObject 생성
            panel.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(panel.GetComponent<RectTransform>(), position, size); // 패널 위치·크기 적용
            panel.GetComponent<Image>().color = color; // 패널 배경 색상 적용
            return panel; // 완성 패널 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2? customSize = null, bool danger = false)
        {
            Vector2 size = customSize ?? new Vector2(480f, 78f); // 기본 버튼 크기 계산
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 런타임 Button 생성
            buttonObject.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
            Color baseColor = danger ? DangerColor : new Color(0.13f, 0.17f, 0.23f, 1f); // 기본·위험 버튼 색상 선택
            image.color = baseColor; // 버튼 기본 배경 적용

            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 지정
            ColorBlock colors = button.colors; // 버튼 상태 색상 구조 조회
            colors.normalColor = baseColor; // 기본 상태 색상 적용
            colors.highlightedColor = danger ? new Color(0.78f, 0.29f, 0.31f, 1f) : new Color(0.18f, 0.28f, 0.38f, 1f); // Hover 색상 적용
            colors.pressedColor = danger ? new Color(0.48f, 0.15f, 0.17f, 1f) : new Color(0.08f, 0.12f, 0.17f, 1f); // Pressed 색상 적용
            colors.selectedColor = colors.highlightedColor; // 선택 상태 색상 적용
            colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.7f); // Disabled 색상 적용
            colors.colorMultiplier = 1f; // 상태 색상 배수 유지
            colors.fadeDuration = 0.08f; // 버튼 상태 전환 시간 적용
            button.colors = colors; // 완성 버튼 상태 색상 적용

            Text text = CreateText("Label", buttonObject.transform, 24, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 8f); // 버튼 내부 여백 적용
            return button; // 완성 Button 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 요청 정렬 적용
            text.color = Color.white; // 기본 흰색 글자 적용
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
            new GameObject("EventSystem_Day55", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
        }

        private static void PrepareCamera()
        {
            Camera camera = Camera.main; // MainMenu 기본 카메라 조회
            if (camera == null) return; // 카메라 누락 허용
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 메뉴 배경 적용
            camera.backgroundColor = BackdropColor; // MainMenu UI 배경과 동일 색상 적용
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
