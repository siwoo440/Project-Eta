using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 런타임 보장
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Battle; // CombatSpeedSettings 사용

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 네임스페이스
{
    [DefaultExecutionOrder(960)] // 승리·패배 버튼 UI 이후 생성
    public sealed class DebugCombatSpeedButtons : MonoBehaviour // 승리·패배 버튼 위의 단일 순환 전투 배속 개발 UI
    {
        private const float FusionButtonCenterX = -99f; // 기존 합성·결과 버튼과 동일 우하단 중심 X
        private const float SpeedButtonBottomY = 226f; // 승리·패배 버튼 바로 위 배치 Y
        private const float SpeedButtonWidth = 225f; // 기존 결과 버튼과 동일 전체 폭
        private const float SpeedButtonHeight = 42f; // 단일 배속 버튼 높이

        private static readonly Color ButtonColor = new Color(0.86f, 0.61f, 0.12f, 0.98f); // 배속 버튼 금색 계열 배경

        private Canvas _canvas; // 배속 버튼 전용 Canvas
        private Button _speedButton; // 1→2→3→1 순환 단일 버튼
        private Image _speedButtonImage; // 배속 버튼 배경
        private Text _speedButtonText; // 현재 배속 표시 문구
        private EventSystem _createdEventSystem; // 직접 생성한 EventSystem 참조
        private static Font _runtimeFont; // 런타임 한글 폰트 캐시

        public int CurrentSpeed => CombatSpeedSettings.CurrentSpeed; // 현재 선택 배속 공개
        public Button SpeedButton => _speedButton; // 테스트·디버그용 단일 배속 버튼 공개

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬·Inspector 수정 없이 배속 UI 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<DebugCombatSpeedButtons>() != null) return; // 중복 생성 차단

            var host = new GameObject("DebugCombatSpeedButtons_Day44"); // 배속 UI 호스트 생성
            host.AddComponent<DebugCombatSpeedButtons>(); // 배속 버튼 컴포넌트 추가
        }

        private void Start() // 배속 UI 초기화
        {
            EnsureUI(); // 단일 배속 버튼 생성
            CombatSpeedSettings.SpeedChanged -= HandleSpeedChanged; // 중복 구독 방지
            CombatSpeedSettings.SpeedChanged += HandleSpeedChanged; // 외부 배속 변경 반영
            RefreshLabel(); // 현재 기본 3배속 문구 표시
        }

        private void EnsureUI() // 배속 버튼 Canvas를 한 번만 생성
        {
            if (_canvas != null) return; // 중복 생성 방지

            EnsureEventSystem(); // UI 클릭용 EventSystem 보장

            var canvasObject = new GameObject("DebugCombatSpeedCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 배속 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 직접 표시
            _canvas.sortingOrder = 98; // 승리·패배 Canvas 97보다 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 개발 UI와 동일 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildButton(canvasObject.transform); // 승패 버튼 위 단일 배속 버튼 생성
        }

        private void BuildButton(Transform parent) // 현재 배속 표시와 다음 단계 순환을 담당하는 단일 버튼 생성
        {
            var buttonObject = new GameObject("DebugCombatSpeedCycleButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 단일 배속 버튼 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // Canvas 자식 연결

            var rect = buttonObject.GetComponent<RectTransform>(); // RectTransform 확보
            rect.anchorMin = new Vector2(1f, 0f); // 우하단 앵커 사용
            rect.anchorMax = new Vector2(1f, 0f); // 우하단 앵커 고정
            rect.pivot = new Vector2(0.5f, 0f); // 하단 중앙 피벗 사용
            rect.anchoredPosition = new Vector2(FusionButtonCenterX, SpeedButtonBottomY); // 승리·패배 버튼 바로 위 배치
            rect.sizeDelta = new Vector2(SpeedButtonWidth, SpeedButtonHeight); // 결과 버튼과 동일 폭 적용

            _speedButtonImage = buttonObject.GetComponent<Image>(); // 배경 이미지 확보
            _speedButtonImage.color = ButtonColor; // 기본 버튼 색 적용

            _speedButton = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            _speedButton.targetGraphic = _speedButtonImage; // 배경을 버튼 상태 그래픽으로 사용
            _speedButton.onClick.AddListener(OnSpeedButtonClicked); // 클릭 시 다음 배속으로 순환

            var colors = _speedButton.colors; // 기본 버튼 색 상태 복사
            colors.normalColor = Color.white; // 기본 원색 유지
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f); // 마우스 오버 강조
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f); // 눌림 표시
            _speedButton.colors = colors; // 버튼 상태 색 적용

            var outline = buttonObject.AddComponent<Outline>(); // 버튼 외곽선 추가
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f); // 어두운 경계선 색 적용
            outline.effectDistance = new Vector2(1.5f, -1.5f); // 경계선 두께 적용

            _speedButtonText = CreateText("DebugCombatSpeedCycleText", buttonObject.transform); // 현재 배속 문구 생성
            RefreshLabel(); // 생성 직후 현재 상태 문구 적용
        }

        private void OnSpeedButtonClicked() // 단일 버튼 클릭 시 1→2→3→1 순환 처리
        {
            int speed = CombatSpeedSettings.CycleToNextSpeed(); // 현재 배속의 다음 단계 적용
            Debug.Log($"44일차 전투 배속 변경: {speed}배 / TimeScale={CombatSpeedSettings.CurrentTimeScale:0.###}"); // 개발 로그 기록
            RefreshLabel(); // 버튼 문구 즉시 갱신
        }

        private void HandleSpeedChanged(int speed) // 외부에서 배속이 바뀐 경우 UI 동기화
        {
            RefreshLabel(); // 현재 상태 문구 갱신
        }

        private void RefreshLabel() // 버튼에 현재 배속 상태 문구 표시
        {
            if (_speedButtonText == null) return; // Text 생성 전 호출 방어
            _speedButtonText.text = $"{CombatSpeedSettings.CurrentSpeed}배속"; // 1배속·2배속·3배속 현재 상태 표시
        }

        private void EnsureEventSystem() // UI 클릭용 EventSystem 보장
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 UI EventSystem 재사용

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 참조 저장
        }

        private static Text CreateText(string name, Transform parent) // 배속 버튼 문구 생성
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 버튼 자식 연결

            var text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 런타임 폰트 적용
            text.fontSize = 16; // 버튼 크기에 맞는 글자 크기 적용
            text.fontStyle = FontStyle.Bold; // 현재 상태 강조를 위해 굵게 표시
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 문구 적용
            text.raycastTarget = false; // 버튼 클릭 가로채기 방지

            var rect = text.rectTransform; // 텍스트 RectTransform 확보
            rect.anchorMin = Vector2.zero; // 부모 전체 Stretch 시작
            rect.anchorMax = Vector2.one; // 부모 전체 Stretch 끝
            rect.offsetMin = new Vector2(2f, 2f); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-2f, -2f); // 우상단 여백 적용
            return text; // 완성 텍스트 반환
        }

        private static Font GetRuntimeFont() // 한글 표시 가능한 런타임 폰트 확보
        {
            if (_runtimeFont != null) return _runtimeFont; // 캐시 재사용

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 한글 시스템 폰트 우선 사용
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // 컴포넌트 파괴 시 이벤트·직접 생성 객체 정리
        {
            CombatSpeedSettings.SpeedChanged -= HandleSpeedChanged; // 배속 변경 이벤트 구독 해제

            if (_createdEventSystem != null) // 직접 생성 EventSystem이 있을 때만 제거
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode 안전 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode 즉시 제거
            }
        }
    }
}
