using System; // Action 사용
using System.Collections.Generic; // List<T> 사용
using UnityEngine; // MonoBehaviour·Screen·Color·Vector2 사용
using UnityEngine.UI; // Button·Image·Slider·Text 사용

namespace ProjectEta.Settings
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.085f, 0.99f); // 설정 패널 배경 색상
        private static readonly Color RowColor = new Color(0.035f, 0.043f, 0.058f, 0.96f); // 설정 항목 배경 색상
        private static readonly Color AccentColor = new Color(0.38f, 0.68f, 0.88f, 1f); // 설정 강조 색상
        private static readonly Color SoftTextColor = new Color(0.68f, 0.72f, 0.79f, 1f); // 보조 텍스트 색상

        private Action _closeRequested; // 외부 패널 닫기 요청 콜백
        private GameSettingsEditState _state; // 현재 설정 편집 상태
        private SettingsResolutionOption[] _resolutions = Array.Empty<SettingsResolutionOption>(); // 선택 가능한 해상도 목록
        private int _resolutionIndex; // 현재 해상도 선택 인덱스
        private int _screenModeIndex; // 현재 화면 모드 선택 인덱스
        private Text _resolutionValueText; // 해상도 선택값 문구
        private Text _screenModeValueText; // 화면 모드 선택값 문구
        private Text _uiScaleValueText; // UI Scale 값 문구
        private Text _statusText; // 적용 상태 문구
        private Slider _uiScaleSlider; // UI Scale Slider
        private Button _applyButton; // 설정 적용 버튼
        private bool _initialized; // 설정 패널 초기화 완료 여부
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public void Initialize(Action closeRequested)
        {
            if (_initialized) return; // 중복 설정 UI 생성 차단

            _closeRequested = closeRequested; // 외부 닫기 콜백 저장
            GameSettingsService.EnsureLoaded(); // 저장 설정 로드 보장
            BuildResolutionCatalog(); // 현재 시스템 해상도 목록 생성
            BuildUI(); // 재사용 설정 패널 UI 생성
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
            GameSettingsService.PreviewUiScale(GameSettingsService.Current.UiScale); // UI Scale 미리보기 원상복구
        }

        private void BeginSession()
        {
            _state = new GameSettingsEditState(GameSettingsService.Current); // 현재 적용 설정 기반 편집 상태 생성
            _resolutionIndex = FindResolutionIndex(_state.Editing.ResolutionWidth, _state.Editing.ResolutionHeight); // 저장 해상도 선택 위치 계산
            _screenModeIndex = _state.Editing.ScreenMode == (int)FullScreenMode.Windowed ? 1 : 0; // 저장 화면 모드 선택 위치 계산
            RefreshControls(); // 편집 상태 UI 반영
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
            GameObject backdrop = CreateImage("SettingsBackdrop_Day56", transform, Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.72f)); // 전체 화면 입력 차단 배경 생성
            Stretch(backdrop.GetComponent<RectTransform>(), 0f); // 전체 화면 Stretch 적용

            GameObject panel = CreateImage("SettingsPanel_Day56", transform, Vector2.zero, new Vector2(1040f, 820f), PanelColor); // 중앙 설정 패널 생성

            Text section = CreateText("Section", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft); // 설정 분류 문구 생성
            section.text = "DISPLAY & INTERFACE"; // 설정 분류 문구 적용
            section.color = AccentColor; // 강조 색상 적용
            SetRect(section.rectTransform, new Vector2(0f, 340f), new Vector2(850f, 36f)); // 분류 문구 배치

            Text title = CreateText("Title", panel.transform, 44, FontStyle.Bold, TextAnchor.MiddleLeft); // 설정 제목 생성
            title.text = "설정"; // 설정 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 285f), new Vector2(850f, 64f)); // 설정 제목 배치

            Text hint = CreateText("Hint", panel.transform, 17, FontStyle.Normal, TextAnchor.MiddleLeft); // 설정 안내 생성
            hint.text = "해상도와 화면 모드는 적용 버튼을 누를 때 변경됩니다. UI Scale은 미리보기로 즉시 반영됩니다."; // 설정 적용 방식 안내
            hint.color = SoftTextColor; // 안내 보조 색상 적용
            SetRect(hint.rectTransform, new Vector2(0f, 235f), new Vector2(850f, 42f)); // 설정 안내 배치

            BuildResolutionRow(panel.transform, new Vector2(0f, 125f)); // 해상도 설정 행 생성
            BuildScreenModeRow(panel.transform, new Vector2(0f, 15f)); // 화면 모드 설정 행 생성
            BuildUiScaleRow(panel.transform, new Vector2(0f, -95f)); // UI Scale 설정 행 생성

            _statusText = CreateText("Status", panel.transform, 18, FontStyle.Normal, TextAnchor.MiddleLeft); // 변경 상태 문구 생성
            _statusText.color = SoftTextColor; // 상태 문구 보조 색상 적용
            SetRect(_statusText.rectTransform, new Vector2(0f, -190f), new Vector2(850f, 36f)); // 상태 문구 배치

            Button resetButton = CreateButton("Reset", panel.transform, "초기화", new Vector2(-290f, -295f), new Vector2(250f, 68f)); // 기본값 초기화 버튼 생성
            resetButton.onClick.AddListener(HandleReset); // 기본값 편집 연결

            Button cancelButton = CreateButton("Cancel", panel.transform, "취소", new Vector2(0f, -295f), new Vector2(250f, 68f)); // 편집 취소 버튼 생성
            cancelButton.onClick.AddListener(HandleCancel); // 편집 취소·닫기 연결

            _applyButton = CreateButton("Apply", panel.transform, "적용", new Vector2(290f, -295f), new Vector2(250f, 68f)); // 설정 적용 버튼 생성
            _applyButton.onClick.AddListener(HandleApply); // 런타임 적용·저장 연결
        }

        private void BuildResolutionRow(Transform parent, Vector2 position)
        {
            GameObject row = CreateImage("ResolutionRow", parent, position, new Vector2(850f, 88f), RowColor); // 해상도 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // 해상도 제목 생성
            label.text = "해상도"; // 해상도 제목 적용
            SetRect(label.rectTransform, new Vector2(-280f, 0f), new Vector2(220f, 50f)); // 해상도 제목 배치

            Button previous = CreateButton("Previous", row.transform, "‹", new Vector2(80f, 0f), new Vector2(64f, 54f)); // 이전 해상도 버튼 생성
            previous.onClick.AddListener(() => CycleResolution(-1)); // 이전 해상도 순환 연결

            _resolutionValueText = CreateText("Value", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter); // 해상도 값 문구 생성
            SetRect(_resolutionValueText.rectTransform, new Vector2(245f, 0f), new Vector2(250f, 54f)); // 해상도 값 배치

            Button next = CreateButton("Next", row.transform, "›", new Vector2(410f, 0f), new Vector2(64f, 54f)); // 다음 해상도 버튼 생성
            next.onClick.AddListener(() => CycleResolution(1)); // 다음 해상도 순환 연결
        }

        private void BuildScreenModeRow(Transform parent, Vector2 position)
        {
            GameObject row = CreateImage("ScreenModeRow", parent, position, new Vector2(850f, 88f), RowColor); // 화면 모드 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // 화면 모드 제목 생성
            label.text = "화면 모드"; // 화면 모드 제목 적용
            SetRect(label.rectTransform, new Vector2(-280f, 0f), new Vector2(220f, 50f)); // 화면 모드 제목 배치

            Button previous = CreateButton("Previous", row.transform, "‹", new Vector2(80f, 0f), new Vector2(64f, 54f)); // 이전 화면 모드 버튼 생성
            previous.onClick.AddListener(() => CycleScreenMode(-1)); // 이전 화면 모드 순환 연결

            _screenModeValueText = CreateText("Value", row.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter); // 화면 모드 값 문구 생성
            SetRect(_screenModeValueText.rectTransform, new Vector2(245f, 0f), new Vector2(250f, 54f)); // 화면 모드 값 배치

            Button next = CreateButton("Next", row.transform, "›", new Vector2(410f, 0f), new Vector2(64f, 54f)); // 다음 화면 모드 버튼 생성
            next.onClick.AddListener(() => CycleScreenMode(1)); // 다음 화면 모드 순환 연결
        }

        private void BuildUiScaleRow(Transform parent, Vector2 position)
        {
            GameObject row = CreateImage("UiScaleRow", parent, position, new Vector2(850f, 88f), RowColor); // UI Scale 행 배경 생성
            Text label = CreateText("Label", row.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // UI Scale 제목 생성
            label.text = "UI Scale"; // UI Scale 제목 적용
            SetRect(label.rectTransform, new Vector2(-280f, 0f), new Vector2(220f, 50f)); // UI Scale 제목 배치

            _uiScaleSlider = CreateSlider("UiScaleSlider", row.transform, new Vector2(235f, 0f), new Vector2(360f, 40f)); // UI Scale Slider 생성
            _uiScaleSlider.minValue = GameSettingsData.MinimumUiScale; // UI Scale 최소값 적용
            _uiScaleSlider.maxValue = GameSettingsData.MaximumUiScale; // UI Scale 최대값 적용
            _uiScaleSlider.onValueChanged.AddListener(HandleUiScaleChanged); // UI Scale 미리보기 연결

            _uiScaleValueText = CreateText("Value", row.transform, 19, FontStyle.Bold, TextAnchor.MiddleRight); // UI Scale 퍼센트 문구 생성
            SetRect(_uiScaleValueText.rectTransform, new Vector2(420f, 0f), new Vector2(110f, 50f)); // UI Scale 값 배치
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
            RefreshDirtyState(); // 변경 상태 문구·적용 버튼 갱신
            _uiScaleValueText.text = $"{Mathf.RoundToInt(_state.Editing.UiScale * 100f)}%"; // UI Scale 퍼센트 갱신
        }

        private void HandleReset()
        {
            if (_state == null) return; // 설정 상태 준비 전 차단
            _state.ResetToDefault(); // 기본 설정을 편집 상태에 적용
            _resolutionIndex = FindResolutionIndex(_state.Editing.ResolutionWidth, _state.Editing.ResolutionHeight); // 기본 해상도 선택 위치 계산
            _screenModeIndex = 0; // 기본 전체화면 창 선택
            GameSettingsService.PreviewUiScale(_state.Editing.UiScale); // 기본 UI Scale 미리보기
            RefreshControls(); // 초기화 결과 UI 반영
            _statusText.text = "기본값으로 변경됨 · 적용 필요"; // 초기화 상태 안내
        }

        private void HandleCancel()
        {
            if (_state != null) _state.Cancel(); // 미적용 편집값 폐기
            GameSettingsService.PreviewUiScale(GameSettingsService.Current.UiScale); // 저장 UI Scale 원상복구
            _closeRequested?.Invoke(); // 외부 패널 닫기 요청
        }

        private void HandleApply()
        {
            if (_state == null || !_state.IsDirty) return; // 변경 없음 적용 차단
            GameSettingsData applied = _state.Apply(); // 편집 설정 확정
            GameSettingsService.ApplyAndSave(applied); // 화면 반영·settings.json 저장
            _resolutionIndex = FindResolutionIndex(applied.ResolutionWidth, applied.ResolutionHeight); // 적용 해상도 선택 위치 갱신
            _screenModeIndex = applied.ScreenMode == (int)FullScreenMode.Windowed ? 1 : 0; // 적용 화면 모드 선택 위치 갱신
            RefreshControls(); // 적용 상태 UI 갱신
            _statusText.text = "설정이 적용되고 저장되었습니다."; // 적용 완료 안내
        }

        private void RefreshControls()
        {
            if (_state == null) return; // 편집 상태 준비 전 차단
            _resolutionValueText.text = $"{_state.Editing.ResolutionWidth} × {_state.Editing.ResolutionHeight}"; // 해상도 값 표시
            _screenModeValueText.text = _state.Editing.ScreenMode == (int)FullScreenMode.Windowed ? "창모드" : "전체화면 창"; // 화면 모드 값 표시
            _uiScaleSlider.SetValueWithoutNotify(_state.Editing.UiScale); // UI Scale Slider 편집값 동기화
            _uiScaleValueText.text = $"{Mathf.RoundToInt(_state.Editing.UiScale * 100f)}%"; // UI Scale 퍼센트 표시
            RefreshDirtyState(); // 변경 상태 갱신
        }

        private void RefreshDirtyState()
        {
            if (_state == null) return; // 편집 상태 준비 전 차단
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
            colors.normalColor = baseColor; // 기본 상태 색상 적용
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.38f, 1f); // Hover 색상 적용
            colors.pressedColor = new Color(0.08f, 0.12f, 0.17f, 1f); // Pressed 색상 적용
            colors.selectedColor = colors.highlightedColor; // 선택 색상 적용
            colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.7f); // Disabled 색상 적용
            button.colors = colors; // 버튼 상태 색상 저장

            Text text = CreateText("Label", result.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
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
            slider.wholeNumbers = false; // 소수 UI Scale 허용
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
