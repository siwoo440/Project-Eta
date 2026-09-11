using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.UI; // 공통 UI 계층 순서 사용

namespace ProjectEta.Meta
{
    public sealed class MetaProgressUI : MonoBehaviour
    {
        private Canvas _canvas; // 메타 진행 전용 Canvas
        private GameObject _resultRoot; // 67일차 런 결과 요약 UI 루트
        private GameObject _progressRoot; // 재사용 영구 성장 UI 루트
        private Text _titleText; // RUN COMPLETED·FAILED 제목
        private Text _stageText; // 도달 Stage 표시
        private Text _rewardText; // 이번 Meta Token 보상
        private Text _totalText; // 현재 영구 Meta Token 잔액
        private Text _statusText; // 결과 안내
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private MetaProgressState _progress; // 현재 표시 영구 진행 상태
        private MetaProgressPanelController _progressPanelController; // 재사용 영구 성장 패널
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsVisible => (_resultRoot != null && _resultRoot.activeSelf) || (_progressRoot != null && _progressRoot.activeSelf); // 현재 결과·영구 성장 UI 표시 여부

        public void Show(MetaProgressState progress, int earnedTokens, int reachedStage, bool completed)
        {
            EnsureUI(); // 67일차 런 결과 Canvas 생성 보장
            _progress = progress; // 현재 영구 진행 상태 저장
            _titleText.text = completed ? "RUN COMPLETED" : "RUN FAILED"; // 런 완료·실패 명확한 결과 제목 표시
            _titleText.color = completed ? new Color(0.98f, 0.80f, 0.28f, 1f) : new Color(1f, 0.28f, 0.22f, 1f); // 승패 결과 강조색 적용
            _stageText.text = $"REACHED STAGE  {reachedStage}"; // 실제 도달 Stage 표시
            _rewardText.text = $"META TOKEN\n+{earnedTokens}"; // 이번 런 메타 토큰 획득량 표시
            _statusText.text = completed
                ? "런을 완료했습니다. 획득한 Meta Token은 영구 저장됩니다." // 클리어 결과 안내
                : "런이 종료되었습니다. 이번 런에서 획득한 Meta Token이 저장됩니다."; // 실패 결과 안내
            RefreshSummary(); // 현재 Meta Token 총액 갱신
            _progressRoot.SetActive(false); // 결과 표시 전 영구 성장 상세 숨김
            _resultRoot.SetActive(true); // 67일차 결과 요약 화면 표시
        }

        public void Hide()
        {
            if (_progressRoot != null) _progressRoot.SetActive(false); // 영구 성장 상세 화면 숨김
            if (_resultRoot != null) _resultRoot.SetActive(false); // 런 결과 요약 화면 숨김
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 UI 생성 차단
            EnsureEventSystem(); // UI 클릭용 EventSystem 보장

            GameObject canvasObject = new GameObject("MetaProgressCanvas_Day67", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 67일차 런 결과 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 런 종료 결과를 화면 위에 표시
            _canvas.sortingOrder = UiLayerOrder.RunResult; // 공통 런 결과 모달 계층 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 공통 기준 해상도 사용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildResultRoot(canvasObject.transform); // 런 결과 요약 화면 생성
            BuildProgressRoot(canvasObject.transform); // 재사용 영구 성장 상세 화면 생성
        }

        private void BuildResultRoot(Transform parent)
        {
            _resultRoot = new GameObject("RunResultRoot_Day67", typeof(RectTransform), typeof(Image)); // 전체 화면 결과 루트 생성
            _resultRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(_resultRoot.GetComponent<RectTransform>(), 0f); // 전체 화면 배치

            Image blocker = _resultRoot.GetComponent<Image>(); // 전체 화면 어두운 배경 조회
            blocker.color = new Color(0.015f, 0.02f, 0.03f, 0.94f); // 전투판을 낮추는 결과 배경 적용
            blocker.raycastTarget = true; // 결과 화면 뒤쪽 입력 차단

            GameObject panel = CreatePanel("RunResultPanel", _resultRoot.transform, Vector2.zero, new Vector2(980f, 730f), new Color(0.045f, 0.055f, 0.075f, 0.99f)); // 중앙 결과 패널 생성

            Text eyebrow = CreateText("Eyebrow", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter); // 결과 분류 문구 생성
            eyebrow.text = "RUN RESULT"; // 결과 분류 표시
            eyebrow.color = new Color(0.48f, 0.72f, 0.92f, 1f); // 분류 문구 강조색 적용
            SetRect(eyebrow.rectTransform, new Vector2(0f, 296f), new Vector2(340f, 34f)); // 패널 상단 배치

            _titleText = CreateText("Title", panel.transform, 48, FontStyle.Bold, TextAnchor.MiddleCenter); // 큰 최종 결과 제목 생성
            SetRect(_titleText.rectTransform, new Vector2(0f, 236f), new Vector2(760f, 74f)); // 제목 중앙 배치

            _stageText = CreateText("Stage", panel.transform, 23, FontStyle.Bold, TextAnchor.MiddleCenter); // 도달 Stage 문구 생성
            _stageText.color = new Color(0.82f, 0.86f, 0.92f, 1f); // 보조 정보 색상 적용
            SetRect(_stageText.rectTransform, new Vector2(0f, 174f), new Vector2(520f, 42f)); // 제목 아래 배치

            GameObject rewardPanel = CreatePanel("RewardPanel", panel.transform, new Vector2(-226f, 54f), new Vector2(390f, 170f), new Color(0.075f, 0.09f, 0.12f, 1f)); // 이번 런 획득 카드 영역 생성
            _rewardText = CreateText("Reward", rewardPanel.transform, 27, FontStyle.Bold, TextAnchor.MiddleCenter); // 이번 보상 문구 생성
            _rewardText.color = new Color(0.98f, 0.80f, 0.28f, 1f); // Meta Token 획득 강조
            Stretch(_rewardText.rectTransform, 12f); // 보상 카드 내부 전체 사용

            GameObject totalPanel = CreatePanel("TotalPanel", panel.transform, new Vector2(226f, 54f), new Vector2(390f, 170f), new Color(0.075f, 0.09f, 0.12f, 1f)); // 누적 토큰 카드 영역 생성
            _totalText = CreateText("Total", totalPanel.transform, 27, FontStyle.Bold, TextAnchor.MiddleCenter); // 총 Meta Token 문구 생성
            _totalText.color = new Color(0.48f, 0.82f, 1f, 1f); // 누적 토큰 청색 강조
            Stretch(_totalText.rectTransform, 12f); // 누적 카드 내부 전체 사용

            _statusText = CreateText("Status", panel.transform, 18, FontStyle.Normal, TextAnchor.MiddleCenter); // 결과 안내 문구 생성
            _statusText.color = new Color(0.72f, 0.77f, 0.84f, 1f); // 안내 보조 색상 적용
            SetRect(_statusText.rectTransform, new Vector2(0f, -70f), new Vector2(800f, 64f)); // 보상 카드 아래 안내 배치

            Button openProgress = CreateButton("OpenMetaProgress", panel.transform, "영구 성장 보기", new Vector2(0f, -185f), new Vector2(430f, 72f)); // 영구 성장 상세 진입 버튼 생성
            openProgress.onClick.AddListener(OpenProgressPanel); // 재사용 영구 성장 패널 열기 연결

            Button close = CreateButton("Close", panel.transform, "닫기", new Vector2(0f, -278f), new Vector2(300f, 58f)); // 결과 UI 닫기 버튼 생성
            close.onClick.AddListener(Hide); // 결과 UI 닫기 연결

            _resultRoot.SetActive(false); // 기본 런 결과 숨김
        }

        private void BuildProgressRoot(Transform parent)
        {
            _progressRoot = new GameObject("MetaProgressDetailRoot_Day67", typeof(RectTransform)); // 재사용 영구 성장 상세 루트 생성
            _progressRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            Stretch(_progressRoot.GetComponent<RectTransform>(), 0f); // 전체 화면 배치
            _progressRoot.SetActive(false); // 초기 영구 성장 상세 숨김

            _progressPanelController = _progressRoot.AddComponent<MetaProgressPanelController>(); // 기존 영구 성장 패널 재사용
            _progressPanelController.Initialize(CloseProgressPanel); // 런 결과 복귀 콜백 연결
        }

        private void OpenProgressPanel()
        {
            _resultRoot.SetActive(false); // 런 결과 요약 숨김
            _progressRoot.SetActive(true); // 영구 성장 상세 화면 표시
        }

        private void CloseProgressPanel()
        {
            _progressRoot.SetActive(false); // 영구 성장 상세 화면 숨김
            RefreshSummary(); // 해금 후 최신 토큰 잔액 갱신
            _resultRoot.SetActive(true); // 런 결과 요약 화면 복귀
        }

        private void RefreshSummary()
        {
            MetaProgressState progress = MetaProgressService.Current; // 최신 영구 진행 상태 조회
            if (_progress != null) progress = _progress; // 지급 직후 전달 진행 상태 우선 사용
            int totalTokens = progress != null ? progress.MetaTokens : 0; // 안전한 누적 Meta Token 조회
            _totalText.text = $"TOTAL META TOKEN\n{totalTokens}"; // 누적 영구 토큰 표시
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            GameObject eventSystemObject = new GameObject("EventSystem_Day67_RunResult", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 공통 패널 오브젝트 생성
            panel.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(panel.GetComponent<RectTransform>(), position, size); // 패널 위치·크기 적용
            Image image = panel.GetComponent<Image>(); // 패널 이미지 조회
            image.color = color; // 패널 배경 색상 적용
            image.raycastTarget = false; // 패널 자체 뒤 입력 처리 불필요
            return panel; // 완성 패널 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline)); // 공통 버튼 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 확보
            image.color = new Color(0.12f, 0.17f, 0.24f, 1f); // 공통 버튼 배경 적용

            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            button.targetGraphic = image; // 버튼 대상 그래픽 지정
            ColorBlock colors = button.colors; // 버튼 상태 색상 조회
            colors.normalColor = Color.white; // Image.color 기준 기본 상태 유지
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); // Hover 밝기 적용
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f); // Pressed 밝기 적용
            colors.selectedColor = colors.highlightedColor; // 선택 밝기 적용
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.72f); // Disabled 밝기 적용
            button.colors = colors; // 버튼 상태 색상 저장

            Outline outline = buttonObject.GetComponent<Outline>(); // 버튼 외곽선 조회
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f); // 어두운 경계선 적용
            outline.effectDistance = new Vector2(1.5f, -1.5f); // 외곽선 두께 적용

            Text text = CreateText("Label", buttonObject.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 요청 버튼 문구 적용
            Stretch(text.rectTransform, 8f); // 버튼 내부 여백 적용
            return button; // 완성 버튼 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 요청 정렬 적용
            text.color = Color.white; // 기본 흰색 문구 적용
            text.raycastTarget = false; // 버튼 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 26); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            return _runtimeFont; // 최종 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 고정
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백
        }

        private void OnDestroy()
        {
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
