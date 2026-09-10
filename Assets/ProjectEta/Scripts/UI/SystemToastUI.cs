using UnityEngine; // MonoBehaviour·GameObject·Time 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 사용
using UnityEngine.UI; // Canvas·Image·Text 사용
using ProjectEta.Run; // Continue 저장 정보 사용
using ProjectEta.SceneFlow; // Battle 씬 이름 사용
using ProjectEta.Settings; // 저장 UI Scale 재적용 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1550)]
    public sealed class SystemToastUI : MonoBehaviour
    {
        private static SystemNotificationQueue _pending = new SystemNotificationQueue(); // 씬 초기화 전 포함 대기 알림 Queue
        private static SystemToastUI _instance; // 현재 Battle Toast UI 인스턴스
        private GameObject _toastRoot; // 현재 Toast 표시 루트
        private Text _titleText; // Toast 제목 문구
        private Text _bodyText; // Toast 본문 문구
        private float _hideAt; // 현재 Toast 종료 시각
        private bool _showing; // 현재 Toast 표시 여부
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null; // Domain Reload 비활성 환경 인스턴스 초기화
            _pending = new SystemNotificationQueue(); // 이전 플레이 세션 대기 알림 초기화
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != SceneFlowController.BattleSceneName) return; // Battle 씬 외 생성 차단
            EnsureInstance(); // Battle 시스템 Toast 인스턴스 보장
        }

        public static void Push(string title, string body, float duration = 1.8f)
        {
            _pending.Enqueue(new SystemNotificationMessage(title, body, duration)); // 시스템 알림 대기 Queue 등록
            if (SceneManager.GetActiveScene().name == SceneFlowController.BattleSceneName) EnsureInstance(); // Battle 씬 알림 UI 즉시 보장
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return; // 기존 Toast UI 재사용

            SystemToastUI existing = Object.FindFirstObjectByType<SystemToastUI>(); // 씬 기존 Toast UI 조회
            if (existing != null)
            {
                _instance = existing; // 기존 Toast UI 인스턴스 연결
                return; // 중복 생성 차단
            }

            GameObject host = new GameObject("SystemToastUI_Day68"); // 시스템 Toast 호스트 생성
            _instance = host.AddComponent<SystemToastUI>(); // Toast UI 컴포넌트 추가
        }

        private void Awake()
        {
            _instance = this; // 현재 Toast UI 인스턴스 등록
        }

        private void Start()
        {
            BuildUI(); // 시스템 Toast Canvas 생성
            GameSettingsService.ReapplyUiScale(); // 신규 Toast Canvas 저장 UI Scale 적용

            if (RunSaveSystem.TryGetContinueInfo(out RunContinueInfo info))
            {
                Push("불러오기 완료", $"STAGE {info.Stage} · {GetFlowLabel(info.FlowPhase)} · KING HP {info.KingHp}", 2.4f); // 이어하기 Battle 진입 복원 안내 등록
            }
        }

        private void Update()
        {
            if (_toastRoot == null) return; // Toast UI 준비 전 차단
            if (FirstRunTutorialController.IsAnyTutorialOpen) return; // 최초 튜토리얼 위 시스템 Toast 표시 지연

            if (_showing && Time.unscaledTime >= _hideAt)
            {
                _toastRoot.SetActive(false); // 표시 시간 종료 Toast 숨김
                _showing = false; // 현재 표시 상태 해제
            }

            if (_showing) return; // 현재 Toast 표시 중 다음 알림 대기
            if (!_pending.TryDequeue(out SystemNotificationMessage message)) return; // 대기 알림 없음 처리

            _titleText.text = message.Title; // 새 Toast 제목 적용
            _bodyText.text = message.Body; // 새 Toast 본문 적용
            _toastRoot.SetActive(true); // 새 Toast 표시
            _hideAt = Time.unscaledTime + message.Duration; // 새 Toast 종료 시각 계산
            _showing = true; // 현재 표시 상태 기록
        }

        private void BuildUI()
        {
            GameObject canvasObject = new GameObject("SystemToastCanvas_Day68", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 시스템 Toast Canvas 생성
            canvasObject.transform.SetParent(transform, false); // Toast 호스트 자식 연결

            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Toast Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 모드 적용
            canvas.sortingOrder = 2400; // 일반 전투·Pause UI 위 Toast 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // Toast CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 적용

            _toastRoot = new GameObject("ToastRoot", typeof(RectTransform), typeof(Image)); // Toast 카드 루트 생성
            _toastRoot.transform.SetParent(canvasObject.transform, false); // Toast Canvas 자식 연결
            RectTransform rect = _toastRoot.GetComponent<RectTransform>(); // Toast RectTransform 조회
            rect.anchorMin = new Vector2(1f, 1f); // 우상단 앵커 적용
            rect.anchorMax = new Vector2(1f, 1f); // 우상단 앵커 적용
            rect.pivot = new Vector2(1f, 1f); // 우상단 피벗 적용
            rect.anchoredPosition = new Vector2(-42f, -42f); // 화면 우상단 여백 적용
            rect.sizeDelta = new Vector2(500f, 124f); // Toast 카드 크기 적용
            _toastRoot.GetComponent<Image>().color = new Color(0.035f, 0.043f, 0.058f, 0.97f); // Toast 카드 배경 적용

            GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image)); // Toast 좌측 강조선 생성
            accent.transform.SetParent(_toastRoot.transform, false); // Toast 카드 자식 연결
            RectTransform accentRect = accent.GetComponent<RectTransform>(); // 강조선 RectTransform 조회
            accentRect.anchorMin = new Vector2(0f, 0f); // 좌측 Stretch 시작 앵커 적용
            accentRect.anchorMax = new Vector2(0f, 1f); // 좌측 Stretch 끝 앵커 적용
            accentRect.pivot = new Vector2(0f, 0.5f); // 좌측 중앙 피벗 적용
            accentRect.anchoredPosition = Vector2.zero; // 카드 좌측 정렬
            accentRect.sizeDelta = new Vector2(7f, 0f); // 강조선 너비 적용
            accent.GetComponent<Image>().color = new Color(0.38f, 0.68f, 0.88f, 1f); // 시스템 강조 색상 적용

            _titleText = CreateText("Title", _toastRoot.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // Toast 제목 생성
            SetRect(_titleText.rectTransform, new Vector2(30f, 24f), new Vector2(420f, 40f)); // Toast 제목 위치 적용

            _bodyText = CreateText("Body", _toastRoot.transform, 17, FontStyle.Normal, TextAnchor.MiddleLeft); // Toast 본문 생성
            _bodyText.color = new Color(0.72f, 0.76f, 0.82f, 1f); // Toast 본문 보조 색상 적용
            SetRect(_bodyText.rectTransform, new Vector2(30f, -22f), new Vector2(420f, 48f)); // Toast 본문 위치 적용

            _toastRoot.SetActive(false); // 최초 Toast 숨김
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
                    return phase.ToString(); // 기타 흐름 원본 표시 반환
            }
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Toast Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // Toast 카드 자식 연결
            Text text = textObject.GetComponent<Text>(); // Toast Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 글자 정렬 적용
            text.color = Color.white; // 기본 흰색 적용
            text.raycastTarget = false; // 시스템 Toast 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 20); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 시작 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 끝 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null; // 현재 정적 인스턴스 참조 정리
        }
    }
}
