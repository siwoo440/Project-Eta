using System; // Action·Array 사용
using System.Collections.Generic; // List<T>·Dictionary<T> 사용
using UnityEngine; // MonoBehaviour·Screen·Color·Vector2 사용
using UnityEngine.UI; // Button·Image·Slider·Text 사용

namespace ProjectEta.Settings
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.085f, 0.99f); // 설정 패널 배경 색상
        private static readonly Color RowColor = new Color(0.035f, 0.043f, 0.058f, 0.96f); // 설정 항목 배경 색상
        private static readonly Color NavColor = new Color(0.04f, 0.05f, 0.07f, 0.98f); // 카테고리 영역 배경 색상
        private static readonly Color AccentColor = new Color(0.38f, 0.68f, 0.88f, 1f); // 설정 강조 색상
        private static readonly Color SoftTextColor = new Color(0.68f, 0.72f, 0.79f, 1f); // 보조 텍스트 색상
        private static SettingsCategory _lastImplementedCategory = SettingsCategory.Display; // 마지막 선택 구현 카테고리

        private readonly Dictionary<SettingsCategory, Button> _categoryButtons = new Dictionary<SettingsCategory, Button>(); // 카테고리 버튼 목록
        private Action _closeRequested; // 외부 패널 닫기 요청 콜백
        private GameSettingsEditState _state; // 현재 설정 편집 상태
        private SettingsResolutionOption[] _resolutions = Array.Empty<SettingsResolutionOption>(); // 선택 가능한 해상도 목록
        private SettingsCategory _currentCategory = SettingsCategory.Display; // 현재 표시 카테고리
        private int _resolutionIndex; // 현재 해상도 선택 인덱스
        private int _screenModeIndex; // 현재 화면 모드 선택 인덱스
        private GameObject _displayContentRoot; // 디스플레이 설정 내용 루트
        private GameObject _soundContentRoot; // 사운드 설정 내용 루트
        private Text _categoryTitleText; // 현재 카테고리 제목
        private Text _categoryHintText; // 현재 카테고리 안내
        private Text _resolutionValueText; // 해상도 선택값 문구
        private Text _screenModeValueText; // 화면 모드 선택값 문구
        private Text _uiScaleValueText; // UI Scale 값 문구
        private Text _masterValueText; // Master 볼륨 값 문구
        private Text _bgmValueText; // BGM 볼륨 값 문구
        private Text _sfxValueText; // SFX 볼륨 값 문구
        private Text _statusText; // 적용 상태 문구
        private Slider _uiScaleSlider; // UI Scale Slider
        private Slider _masterSlider; // Master Volume Slider
        private Slider _bgmSlider; // BGM Volume Slider
        private Slider _sfxSlider; // SFX Volume Slider
        private Button _resetButton; // 현재 카테고리 초기화 버튼
        private Button _applyButton; // 설정 적용 버튼
        private bool _initialized; // 설정 패널 초기화 완료 여부
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public SettingsCategory CurrentCategory => _currentCategory; // 현재 설정 카테고리 조회

        public void Initialize(Action closeRequested)
        {
            if (_initialized) return; // 중복 설정 UI 생성 차단

            _closeRequested = closeRequested; // 외부 닫기 콜백 저장
            GameSettingsService.EnsureLoaded(); // 저장 설정 로드 보장
            BuildResolutionCatalog(); // 현재 시스템 해상도 목록 생성
            BuildUI(); // 57일차 카테고리형 설정 패널 생성
            _initialized = true; // 패널 초기화 완료 기록

            if (gameObject.activeInHierarchy) BeginSession(); // 활성 상태 초기화 시 즉시 편집 세션 시작
        }

        private void OnEnable()
        {
            if (!_initialized) return; // Initialize 전 활성 이벤트 차단
            BeginSession(); // 패널 열릴 때 현재 저장값 기준 편집 시작
        }

        private void OnDisable()
        {
            if (!_initialized || _state == null) return; // 초기화 전 비활성 처리 차단
            _state.Cancel(); // 닫힐 때 적용되지 않은 편집값 폐기
            GameSettingsData current = GameSettingsService.Current; // 마지막 적용 설정 조회
            GameSettingsService.PreviewUiScale(current.UiScale); // UI Scale 미리보기 원상복구
            GameSettingsService.PreviewAudio(current.MasterVolume, current.BgmVolume, current.SfxVolume); // 오디오 미리보기 원상복구
        }

        private void BeginSession()
        {
            _state = new GameSettingsEditState(GameSettingsService.Current); // 현재 적용 설정 기반 편집 상태 생성
            _resolutionIndex = FindResolutionIndex(_state.Editing.ResolutionWidth, _state.Editing.ResolutionHeight); // 저장 해상도 선택 위치 계산
            _screenModeIndex = _state.Editing.ScreenMode == (int)FullScreenMode.Windowed ? 1 : 0; // 저장 화면 모드 선택 위치 계산
            _currentCategory = SettingsCategoryCatalog.IsImplemented(_lastImplementedCategory) ? _lastImplementedCategory : SettingsCategory.Display; // 마지막 구현 카테고리 복원
            RefreshControls(); // 편집 상태 UI 반영
            ShowCategory(_currentCategory); // 현재 카테고리 내용 표시
            _statusText.text = "현재 설정"; // 초기 상태 문구 적용
        }

        private void BuildResolutionCatalog()
        {
            Resolution[] systemResolutions = Screen.resolutions; // 현재 모니터 지원 해상도 조회
            var candidates = new List<SettingsResolutionOption>(); // 해상도 변환 후보 목록

            for (int i = 0; i < systemResolutions.Length; i++)
            {
                candidates.Add(new SettingsResolutionOption(systemResolutions[i].width, systemResolutions[i].height)); // RefreshRate 무시 크기 후보 등록
            }

            if (candidates.Count == 0)
            {
                candidates.Add(new SettingsResolutionOption(Screen.width, Screen.height)); // 지원 목록 없음 시 현재 화면 크기 fallback
            }

            GameSettingsData current = GameSettingsService.Current; // 저장 설정 조회
            candidates.Add(new SettingsResolutionOption(current.ResolutionWidth, current.ResolutionHeight)); // 저장 해상도 목록 포함 보장
            _resolutions = GameSettingsResolutionCatalog.DeduplicateAndSort(candidates); // 중복 제거·높은 해상도 우선 정렬
        }

        private void BuildUI()
        {
            GameObject backdrop = CreateImage("SettingsBackdrop_Day57", transform, Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.72f)); // 전체 화면 입력 차단 배경 생성
            Stretch(backdrop.GetComponent<RectTransform>(), 0f); // 전체 화면 Stretch 적용

            GameObject panel = CreateImage("SettingsPanel_Day57", transform, Vector2.zero, new Vector2(1240f, 920f), PanelColor); // 카테고리형 중앙 설정 패널 생성

            Text section = CreateText("Section", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 설정 상단 분류 문구 생성
            section.text = "OPTIONS"; // 설정 상단 분류 문구 적용
            section.color = AccentColor; // 강조 색상 적용
            SetRect(section.rectTransform, new Vector2(-505f, 390f), new Vector2(180f, 36f)); // 상단 분류 문구 배치

            Text title = CreateText("Title", panel.transform, 44, FontStyle.Bold, TextAnchor.MiddleLeft); // 설정 제목 생성
            title.text = "설정"; // 설정 제목 적용
            SetRect(title.rectTransform, new Vector2(-455f, 340f), new Vector2(280f, 64f)); // 설정 제목 배치

            GameObject navPanel = CreateImage("CategoryNavigation", panel.transform, new Vector2(-445f, -25f), new Vector2(270f, 660f), NavColor); // 좌측 카테고리 내비게이션 생성
            BuildCategoryNavigation(navPanel.transform); // 카테고리 버튼 목록 생성

            GameObject contentPanel = CreateImage("SettingsContent", panel.transform, new Vector2(145f, -25f), new Vector2(850f, 660f), new Color(0.03f, 0.037f, 0.052f, 0.98f)); // 우측 설정 내용 영역 생성

            _categoryTitleText = CreateText("CategoryTitle", contentPanel.transform, 34, FontStyle.Bold, TextAnchor.MiddleLeft); // 카테고리 제목 생성
            SetRect(_categoryTitleText.rectTransform, new Vector2(0f, 265f), new Vector2(720f, 52f)); // 카테고리 제목 배치

            _categoryHintText = CreateText("CategoryHint", contentPanel.transform, 16, FontStyle.Normal, TextAnchor.MiddleLeft); // 카테고리 안내 생성
            _categoryHintText.color = SoftTextColor; // 카테고리 안내 보조 색상 적용
            SetRect(_categoryHintText.rectTransform, new Vector2(0f, 220f), new Vector2(720f, 44f)); // 카테고리 안내 배치

            BuildDisplayContent(contentPanel.transform); // 디스플레이 설정 내용 생성
            BuildSoundContent(contentPanel.transform); // 사운드 설정 내용 생성

            _statusText = CreateText("Status", panel.transform, 18, FontStyle.Normal, TextAnchor.MiddleLeft); // 전체 변경 상태 문구 생성
            _statusText.color = SoftTextColor; // 상태 문구 보조 색상 적용
            SetRect(_statusText.rectTransform, new Vector2(150f, -382f), new Vector2(720f, 36f)); // 상태 문구 배치

            _resetButton = CreateButton("Reset", panel.transform, "현재 영역 초기화", new Vector2(-265f, -390f), new Vector2(250f, 68f)); // 현재 카테고리 초기화 버튼 생성
            _resetButton.onClick.AddListener(HandleResetCurrentCategory); // 현재 카테고리 기본값 편집 연결

            Button cancelButton = CreateButton("Cancel", panel.transform, "취소", new Vector2(25f, -390f), new Vector2(250f, 68f)); // 전체 편집 취소 버튼 생성
            cancelButton.onClick.AddListener(HandleCancel); // 전체 편집 취소·닫기 연결

            _applyButton = CreateButton("Apply", panel.transform, "적용", new Vector2(315f, -390f), new Vector2(250f, 68f)); // 전체 설정 적용 버튼 생성
            _applyButton.onClick.AddListener(HandleApply); // 런타임 적용·저장 연결
        }

        private void BuildCategoryNavigation(Transform parent)
        {
            SettingsCategory[] categories =
            {
                SettingsCategory.Display, // 디스플레이 카테고리
                SettingsCategory.Sound, // 사운드 카테고리
                SettingsCategory.Controls, // 향후 조작 카테고리
                SettingsCategory.Gameplay, // 향후 게임플레이 카테고리
                SettingsCategory.Accessibility // 향후 접근성 카테고리
            };

            for (int i = 0; i < categories.Length; i++)
            {
                SettingsCategory category = categories[i]; // 현재 카테고리 조회
                bool implemented = SettingsCategoryCatalog.IsImplemented(category); // 실제 구현 여부 조회
                string label = SettingsCategoryCatalog.GetDisplayName(category); // 카테고리 표시 문구 조회
                if (!implemented) label += "\n준비 중"; // 향후 카테고리 상태 문구 추가

                Button button = CreateButton($"Category_{category}", parent, label, new Vector2(0f, 245f - i * 105f), new Vector2(220f, 78f)); // 카테고리 버튼 생성
                button.interactable = implemented; // 구현된 카테고리만 입력 허용

                if (implemented)
                {
                    SettingsCategory captured = category; // 카테고리 콜백 값 고정
                    button.onClick.AddListener(() => ShowCategory(captured)); // 카테고리 전환 연결
                }

                _categoryButtons[category] = button; // 카테고리 버튼 참조 저장
            }
        }

        private void BuildDisplayContent(Transform parent)
        {
            _displayContentRoot = new GameObject("DisplaySettingsRoot_Day57", typeof(RectTransform)); // 디스플레이 내용 루트 생성
            _displayContentRoot.transform.SetParent(parent, false); // 설정 내용 영역 자식 연결
            SetRect(_displayContentRoot.GetComponent<RectTransform>(), new Vector2(0f, -25f), new Vector2(760f, 430f)); // 디스플레이 내용 영역 배치

            BuildResolutionRow(_displayContentRoot.transform, new Vector2(0f, 125f)); // 해상도 설정 행 생성
            BuildScreenModeRow(_displayContentRoot.transform, new Vector2(0f, 15f)); // 화면 모드 설정 행 생성
            BuildUiScaleRow(_displayContentRoot.transform, new Vector2(0f, -95f)); // UI Scale 설정 행 생성
        }

        private void BuildSoundContent(Transform parent)
        {
            _soundContentRoot = new GameObject("SoundSettingsRoot_Day57", typeof(RectTransform)); // 사운드 내용 루트 생성
            _soundContentRoot.transform.SetParent(parent, false); // 설정 내용 영역 자식 연결
            SetRect(_soundContentRoot.GetComponent<RectTransform>(), new Vector2(0f, -25f), new Vector2(760f, 430f)); // 사운드 내용 영역 배치

            BuildAudioRow(_soundContentRoot.transform, "MasterVolumeRow", "Master", new Vector2(0f, 125f), out _masterSlider, out _masterValueText, HandleMasterVolumeChanged); // Master 설정 행 생성
            BuildAudioRow(_soundContentRoot.transform, "BgmVolumeRow", "BGM", new Vector2(0f, 15f), out _bgmSlider, out _bgmValueText, HandleBgmVolumeChanged); // BGM 설정 행 생성
            BuildAudioRow(_soundContentRoot.transform, "SfxVolumeRow", "SFX", new Vector2(0f, -95f), out _sfxSlider, out _sfxValueText, HandleSfxVolumeChanged); // SFX 설정 행 생성
        }

        private void BuildResolutionRow(Transform parent, Vector2 position)
        {
            GameObject row = CreateImage("ResolutionRow", parent, position, new Vector2(720f, 88f), RowColor); // 해상도 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // 해상도 제목 생성
            label.text = "해상도"; // 해상도 제목 적용
            SetRect(label.rectTransform, new Vector2(-235f, 0f), new Vector2(200f, 50f)); // 해상도 제목 배치

            Button previous = CreateButton("Previous", row.transform, "‹", new Vector2(55f, 0f), new Vector2(60f, 54f)); // 이전 해상도 버튼 생성
            previous.onClick.AddListener(() => CycleResolution(-1)); // 이전 해상도 순환 연결

            _resolutionValueText = CreateText("Value", row.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter); // 해상도 값 문구 생성
            SetRect(_resolutionValueText.rectTransform, new Vector2(205f, 0f), new Vector2(220f, 54f)); // 해상도 값 배치

            Button next = CreateButton("Next", row.transform, "›", new Vector2(355f, 0f), new Vector2(60f, 54f)); // 다음 해상도 버튼 생성
            next.onClick.AddListener(() => CycleResolution(1)); // 다음 해상도 순환 연결
        }

        private void BuildScreenModeRow(Transform parent, Vector2 position)
        {
            GameObject row = CreateImage("ScreenModeRow", parent, position, new Vector2(720f, 88f), RowColor); // 화면 모드 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // 화면 모드 제목 생성
            label.text = "화면 모드"; // 화면 모드 제목 적용
            SetRect(label.rectTransform, new Vector2(-235f, 0f), new Vector2(200f, 50f)); // 화면 모드 제목 배치

            Button previous = CreateButton("Previous", row.transform, "‹", new Vector2(55f, 0f), new Vector2(60f, 54f)); // 이전 화면 모드 버튼 생성
            previous.onClick.AddListener(() => CycleScreenMode(-1)); // 이전 화면 모드 순환 연결

            _screenModeValueText = CreateText("Value", row.transform, 19, FontStyle.Bold, TextAnchor.MiddleCenter); // 화면 모드 값 문구 생성
            SetRect(_screenModeValueText.rectTransform, new Vector2(205f, 0f), new Vector2(220f, 54f)); // 화면 모드 값 배치

            Button next = CreateButton("Next", row.transform, "›", new Vector2(355f, 0f), new Vector2(60f, 54f)); // 다음 화면 모드 버튼 생성
            next.onClick.AddListener(() => CycleScreenMode(1)); // 다음 화면 모드 순환 연결
        }

        private void BuildUiScaleRow(Transform parent, Vector2 position)
        {
            GameObject row = CreateImage("UiScaleRow", parent, position, new Vector2(720f, 88f), RowColor); // UI Scale 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // UI Scale 제목 생성
            label.text = "UI Scale"; // UI Scale 제목 적용
            SetRect(label.rectTransform, new Vector2(-235f, 0f), new Vector2(200f, 50f)); // UI Scale 제목 배치

            _uiScaleSlider = CreateSlider("UiScaleSlider", row.transform, new Vector2(160f, 0f), new Vector2(300f, 40f)); // UI Scale Slider 생성
            _uiScaleSlider.minValue = GameSettingsData.MinimumUiScale; // UI Scale 최소값 적용
            _uiScaleSlider.maxValue = GameSettingsData.MaximumUiScale; // UI Scale 최대값 적용
            _uiScaleSlider.onValueChanged.AddListener(HandleUiScaleChanged); // UI Scale 미리보기 연결

            _uiScaleValueText = CreateText("Value", row.transform, 19, FontStyle.Bold, TextAnchor.MiddleRight); // UI Scale 퍼센트 문구 생성
            SetRect(_uiScaleValueText.rectTransform, new Vector2(340f, 0f), new Vector2(90f, 50f)); // UI Scale 값 배치
        }

        private void BuildAudioRow(Transform parent, string objectName, string labelText, Vector2 position, out Slider slider, out Text valueText, UnityEngine.Events.UnityAction<float> callback)
        {
            GameObject row = CreateImage(objectName, parent, position, new Vector2(720f, 88f), RowColor); // 오디오 설정 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // 오디오 항목 제목 생성
            label.text = labelText; // 오디오 항목 제목 적용
            SetRect(label.rectTransform, new Vector2(-235f, 0f), new Vector2(200f, 50f)); // 오디오 항목 제목 배치

            slider = CreateSlider("Slider", row.transform, new Vector2(160f, 0f), new Vector2(300f, 40f)); // 오디오 Slider 생성
            slider.minValue = 0f; // 오디오 최소 볼륨 적용
            slider.maxValue = 1f; // 오디오 최대 볼륨 적용
            slider.onValueChanged.AddListener(callback); // 오디오 실시간 미리보기 연결

            valueText = CreateText("Value", row.transform, 19, FontStyle.Bold, TextAnchor.MiddleRight); // 오디오 퍼센트 문구 생성
            SetRect(valueText.rectTransform, new Vector2(340f, 0f), new Vector2(90f, 50f)); // 오디오 값 배치
        }

        private void ShowCategory(SettingsCategory category)
        {
            if (!SettingsCategoryCatalog.IsImplemented(category)) return; // 미구현 카테고리 진입 차단

            _currentCategory = category; // 현재 카테고리 변경
            _lastImplementedCategory = category; // 다음 설정 진입용 마지막 카테고리 저장
            if (_displayContentRoot != null) _displayContentRoot.SetActive(category == SettingsCategory.Display); // 디스플레이 내용 표시 상태 적용
            if (_soundContentRoot != null) _soundContentRoot.SetActive(category == SettingsCategory.Sound); // 사운드 내용 표시 상태 적용
            RefreshCategoryHeader(); // 카테고리 제목·안내 갱신
            RefreshCategoryButtons(); // 카테고리 선택 강조 갱신
            RefreshResetButtonLabel(); // 현재 영역 초기화 문구 갱신
        }

        private void RefreshCategoryHeader()
        {
            if (_categoryTitleText == null || _categoryHintText == null) return; // 카테고리 헤더 준비 전 차단

            if (_currentCategory == SettingsCategory.Sound)
            {
                _categoryTitleText.text = "사운드"; // 사운드 카테고리 제목 적용
                _categoryHintText.text = "Master · BGM · SFX 볼륨은 즉시 미리보기되며 적용 시 settings.json에 저장됩니다."; // 사운드 안내 적용
                return; // 사운드 헤더 처리 종료
            }

            _categoryTitleText.text = "디스플레이"; // 디스플레이 카테고리 제목 적용
            _categoryHintText.text = "해상도와 화면 모드는 적용 시 변경되며 UI Scale은 즉시 미리보기됩니다."; // 디스플레이 안내 적용
        }

        private void RefreshCategoryButtons()
        {
            foreach (KeyValuePair<SettingsCategory, Button> pair in _categoryButtons)
            {
                Button button = pair.Value; // 현재 카테고리 버튼 조회
                if (button == null) continue; // 누락 버튼 제외

                Image image = button.GetComponent<Image>(); // 카테고리 버튼 배경 조회
                if (image == null) continue; // 배경 누락 버튼 제외

                if (!button.interactable)
                {
                    image.color = new Color(0.07f, 0.08f, 0.1f, 0.75f); // 준비 중 카테고리 비활성 색상 적용
                    continue; // 다음 버튼 처리
                }

                image.color = pair.Key == _currentCategory ? AccentColor : new Color(0.13f, 0.17f, 0.23f, 1f); // 현재 카테고리 강조 적용
            }
        }

        private void RefreshResetButtonLabel()
        {
            if (_resetButton == null) return; // 초기화 버튼 준비 전 차단
            Text label = _resetButton.GetComponentInChildren<Text>(true); // 초기화 버튼 문구 조회
            if (label == null) return; // 초기화 버튼 문구 누락 방어
            label.text = _currentCategory == SettingsCategory.Sound ? "사운드 초기화" : "디스플레이 초기화"; // 현재 카테고리 초기화 문구 적용
        }

        private void CycleResolution(int direction)
        {
            if (_state == null || _resolutions.Length == 0) return; // 설정 상태·해상도 목록 준비 확인
            _resolutionIndex = WrapIndex(_resolutionIndex + direction, _resolutions.Length); // 해상도 선택 인덱스 순환
            SettingsResolutionOption option = _resolutions[_resolutionIndex]; // 선택 해상도 조회
            _state.SetResolution(option.Width, option.Height); // 편집 해상도 변경
            RefreshControls(); // 변경 UI 반영
        }

        private void CycleScreenMode(int direction)
        {
            if (_state == null) return; // 설정 상태 준비 전 차단
            _screenModeIndex = WrapIndex(_screenModeIndex + direction, 2); // 두 화면 모드 순환
            FullScreenMode mode = _screenModeIndex == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow; // 선택 화면 모드 변환
            _state.SetScreenMode(mode); // 편집 화면 모드 변경
            RefreshControls(); // 변경 UI 반영
        }

        private void HandleUiScaleChanged(float value)
        {
            if (_state == null) return; // 설정 상태 준비 전 차단
            _state.SetUiScale(value); // 편집 UI Scale 변경
            GameSettingsService.PreviewUiScale(_state.Editing.UiScale); // 현재 화면 UI Scale 즉시 미리보기
            _uiScaleValueText.text = FormatPercent(_state.Editing.UiScale); // UI Scale 퍼센트 갱신
            RefreshDirtyState(); // 변경 상태 문구·적용 버튼 갱신
        }

        private void HandleMasterVolumeChanged(float value)
        {
            if (_state == null) return; // 설정 상태 준비 전 차단
            _state.SetMasterVolume(value); // 편집 Master 볼륨 변경
            PreviewEditingAudio(); // 현재 오디오 설정 즉시 미리보기
            _masterValueText.text = FormatPercent(_state.Editing.MasterVolume); // Master 퍼센트 갱신
            RefreshDirtyState(); // 변경 상태 문구·적용 버튼 갱신
        }

        private void HandleBgmVolumeChanged(float value)
        {
            if (_state == null) return; // 설정 상태 준비 전 차단
            _state.SetBgmVolume(value); // 편집 BGM 볼륨 변경
            PreviewEditingAudio(); // 현재 오디오 설정 즉시 미리보기
            _bgmValueText.text = FormatPercent(_state.Editing.BgmVolume); // BGM 퍼센트 갱신
            RefreshDirtyState(); // 변경 상태 문구·적용 버튼 갱신
        }

        private void HandleSfxVolumeChanged(float value)
        {
            if (_state == null) return; // 설정 상태 준비 전 차단
            _state.SetSfxVolume(value); // 편집 SFX 볼륨 변경
            PreviewEditingAudio(); // 현재 오디오 설정 즉시 미리보기
            _sfxValueText.text = FormatPercent(_state.Editing.SfxVolume); // SFX 퍼센트 갱신
            RefreshDirtyState(); // 변경 상태 문구·적용 버튼 갱신
        }

        private void PreviewEditingAudio()
        {
            GameSettingsData editing = _state.Editing; // 현재 편집 오디오 설정 조회
            GameSettingsService.PreviewAudio(editing.MasterVolume, editing.BgmVolume, editing.SfxVolume); // Master·BGM·SFX 미리보기 적용
        }

        private void HandleResetCurrentCategory()
        {
            if (_state == null) return; // 설정 상태 준비 전 차단

            _state.ResetCategory(_currentCategory); // 현재 표시 카테고리만 기본값 적용

            if (_currentCategory == SettingsCategory.Sound)
            {
                PreviewEditingAudio(); // 사운드 기본값 즉시 미리보기
            }
            else
            {
                _resolutionIndex = FindResolutionIndex(_state.Editing.ResolutionWidth, _state.Editing.ResolutionHeight); // 기본 해상도 선택 위치 계산
                _screenModeIndex = _state.Editing.ScreenMode == (int)FullScreenMode.Windowed ? 1 : 0; // 기본 화면 모드 선택 위치 계산
                GameSettingsService.PreviewUiScale(_state.Editing.UiScale); // 디스플레이 기본 UI Scale 미리보기
            }

            RefreshControls(); // 초기화 결과 UI 반영
            _statusText.text = $"{SettingsCategoryCatalog.GetDisplayName(_currentCategory)} 기본값으로 변경됨 · 적용 필요"; // 카테고리 초기화 상태 안내
        }

        private void HandleCancel()
        {
            if (_state != null) _state.Cancel(); // 미적용 전체 편집값 폐기
            GameSettingsData current = GameSettingsService.Current; // 마지막 적용 설정 조회
            GameSettingsService.PreviewUiScale(current.UiScale); // 저장 UI Scale 원상복구
            GameSettingsService.PreviewAudio(current.MasterVolume, current.BgmVolume, current.SfxVolume); // 저장 오디오 볼륨 원상복구
            _closeRequested?.Invoke(); // 외부 패널 닫기 요청
        }

        private void HandleApply()
        {
            if (_state == null || !_state.IsDirty) return; // 변경 없음 적용 차단

            GameSettingsData applied = _state.Apply(); // 전체 편집 설정 확정
            GameSettingsService.ApplyAndSave(applied); // 화면·UI·오디오 반영 및 settings.json 저장
            _resolutionIndex = FindResolutionIndex(applied.ResolutionWidth, applied.ResolutionHeight); // 적용 해상도 선택 위치 갱신
            _screenModeIndex = applied.ScreenMode == (int)FullScreenMode.Windowed ? 1 : 0; // 적용 화면 모드 선택 위치 갱신
            RefreshControls(); // 적용 상태 UI 갱신
            _statusText.text = "설정이 적용되고 저장되었습니다."; // 적용 완료 안내
        }

        private void RefreshControls()
        {
            if (_state == null) return; // 편집 상태 준비 전 차단

            if (_resolutionValueText != null) _resolutionValueText.text = $"{_state.Editing.ResolutionWidth} × {_state.Editing.ResolutionHeight}"; // 해상도 값 표시
            if (_screenModeValueText != null) _screenModeValueText.text = _state.Editing.ScreenMode == (int)FullScreenMode.Windowed ? "창모드" : "전체화면 창"; // 화면 모드 값 표시
            if (_uiScaleSlider != null) _uiScaleSlider.SetValueWithoutNotify(_state.Editing.UiScale); // UI Scale Slider 편집값 동기화
            if (_uiScaleValueText != null) _uiScaleValueText.text = FormatPercent(_state.Editing.UiScale); // UI Scale 퍼센트 표시
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(_state.Editing.MasterVolume); // Master Slider 편집값 동기화
            if (_masterValueText != null) _masterValueText.text = FormatPercent(_state.Editing.MasterVolume); // Master 퍼센트 표시
            if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(_state.Editing.BgmVolume); // BGM Slider 편집값 동기화
            if (_bgmValueText != null) _bgmValueText.text = FormatPercent(_state.Editing.BgmVolume); // BGM 퍼센트 표시
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(_state.Editing.SfxVolume); // SFX Slider 편집값 동기화
            if (_sfxValueText != null) _sfxValueText.text = FormatPercent(_state.Editing.SfxVolume); // SFX 퍼센트 표시
            RefreshDirtyState(); // 전체 변경 상태 갱신
        }

        private void RefreshDirtyState()
        {
            if (_state == null || _applyButton == null || _statusText == null) return; // 편집 상태·UI 준비 전 차단
            _applyButton.interactable = _state.IsDirty; // 변경 있을 때만 적용 버튼 활성화
            _statusText.text = _state.IsDirty ? "변경 사항 있음 · 적용 필요" : "현재 설정"; // 변경 상태 문구 적용
            _statusText.color = _state.IsDirty ? Color.white : SoftTextColor; // 변경 여부에 따른 상태 색상 적용
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutions.Length; i++)
            {
                if (_resolutions[i].Width == width && _resolutions[i].Height == height) return i; // 동일 해상도 인덱스 반환
            }

            return 0; // 목록에 없으면 첫 해상도 fallback
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0) return 0; // 빈 목록 fallback
            if (value < 0) return count - 1; // 이전 순환 마지막 항목 이동
            if (value >= count) return 0; // 다음 순환 첫 항목 이동
            return value; // 범위 내 인덱스 유지
        }

        private static string FormatPercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%"; // 0~1 값을 퍼센트 문구로 변환
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Image)); // Image UI GameObject 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(result.GetComponent<RectTransform>(), position, size); // 위치·크기 적용
            result.GetComponent<Image>().color = color; // 이미지 색상 적용
            return result; // 완성 UI 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // Button UI 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(result.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image image = result.GetComponent<Image>(); // 버튼 배경 조회
            Color baseColor = new Color(0.13f, 0.17f, 0.23f, 1f); // 기본 버튼 색상
            image.color = baseColor; // 기본 버튼 배경 적용

            Button button = result.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 연결
            ColorBlock colors = button.colors; // 버튼 상태 색상 조회
            colors.normalColor = Color.white; // Image.color 기준 기본 상태 유지
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); // Hover 밝기 적용
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f); // Pressed 밝기 적용
            colors.selectedColor = colors.highlightedColor; // 선택 밝기 적용
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.72f); // Disabled 밝기 적용
            colors.colorMultiplier = 1f; // 버튼 색상 배수 유지
            colors.fadeDuration = 0.08f; // 버튼 상태 전환 시간 적용
            button.colors = colors; // 버튼 상태 색상 저장

            Text text = CreateText("Label", result.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 6f); // 버튼 문구 전체 배치
            return button; // 완성 Button 반환
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider)); // Slider 루트 생성
            sliderObject.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(sliderObject.GetComponent<RectTransform>(), position, size); // Slider 위치·크기 적용

            GameObject background = CreateImage("Background", sliderObject.transform, Vector2.zero, size, new Color(0.08f, 0.1f, 0.13f, 1f)); // Slider 배경 생성
            Stretch(background.GetComponent<RectTransform>(), 0f); // Slider 배경 전체 배치

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform)); // Fill Area 생성
            fillArea.transform.SetParent(sliderObject.transform, false); // Slider 자식 연결
            Stretch(fillArea.GetComponent<RectTransform>(), 10f); // Fill Area 여백 배치

            GameObject fill = CreateImage("Fill", fillArea.transform, Vector2.zero, size, AccentColor); // Slider Fill 생성
            Stretch(fill.GetComponent<RectTransform>(), 0f); // Fill 전체 배치

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)); // Handle Area 생성
            handleArea.transform.SetParent(sliderObject.transform, false); // Slider 자식 연결
            Stretch(handleArea.GetComponent<RectTransform>(), 10f); // Handle Area 여백 배치

            GameObject handle = CreateImage("Handle", handleArea.transform, Vector2.zero, new Vector2(28f, 46f), Color.white); // Slider Handle 생성

            Slider slider = sliderObject.GetComponent<Slider>(); // Slider 컴포넌트 조회
            slider.fillRect = fill.GetComponent<RectTransform>(); // Slider Fill 연결
            slider.handleRect = handle.GetComponent<RectTransform>(); // Slider Handle 연결
            slider.targetGraphic = handle.GetComponent<Image>(); // Slider 대상 그래픽 연결
            slider.direction = Slider.Direction.LeftToRight; // 좌우 Slider 방향 적용
            slider.wholeNumbers = false; // 소수 설정값 허용
            return slider; // 완성 Slider 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text UI 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            Text text = result.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 텍스트 정렬 적용
            text.color = Color.white; // 기본 글자색 적용
            text.raycastTarget = false; // UI 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝 적용
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
