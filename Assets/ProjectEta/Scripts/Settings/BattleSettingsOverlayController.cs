using UnityEngine; // MonoBehaviour·GameObject·Time 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem; // Keyboard 사용
using UnityEngine.InputSystem.UI; // InputSystemUIInputModule 사용
using UnityEngine.UI; // Canvas·CanvasScaler·GraphicRaycaster 사용
using ProjectEta.Board; // BoardInputController·RouteMapBoardController 사용

namespace ProjectEta.Settings
{
    [DefaultExecutionOrder(1400)]
    public sealed class BattleSettingsOverlayController : MonoBehaviour
    {
        private GameObject _overlayRoot; // 인게임 설정 오버레이 루트
        private BoardInputController _boardInputController; // 전투 보드 입력
        private RouteMapBoardController _routeMapBoardController; // 경로 지도 입력
        private bool _boardInputWasEnabled; // 설정 열기 전 전투 입력 상태
        private bool _routeMapWasEnabled; // 설정 열기 전 지도 입력 상태
        private float _previousTimeScale = 1f; // 설정 열기 전 TimeScale
        private bool _isOpen; // 인게임 설정 열림 여부

        public bool IsOpen => _isOpen; // 외부 인게임 설정 상태 조회

        private void Start()
        {
            GameSettingsService.EnsureLoaded(); // 직접 Battle 실행 시 설정 로드 보장
            EnsureEventSystem(); // 인게임 설정 UI 입력 시스템 보장
            BuildOverlay(); // Battle 재사용 설정 패널 생성
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 입력 장치 조회
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return; // ESC 입력 없으면 처리 종료

            if (_isOpen) ClosePanel(); // 설정 열림 상태에서 ESC 닫기
            else OpenPanel(); // 일반 인게임 상태에서 ESC 설정 열기
        }

        public void OpenPanel()
        {
            if (_isOpen || _overlayRoot == null) return; // 중복 열기·UI 준비 전 차단

            _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 현재 전투 입력 조회
            _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 현재 지도 입력 조회
            _boardInputWasEnabled = _boardInputController != null && _boardInputController.enabled; // 전투 입력 기존 상태 저장
            _routeMapWasEnabled = _routeMapBoardController != null && _routeMapBoardController.enabled; // 지도 입력 기존 상태 저장

            if (_boardInputController != null) _boardInputController.enabled = false; // 설정 중 보드 입력 차단
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = false; // 설정 중 지도 노드 입력 차단

            _previousTimeScale = Time.timeScale; // 기존 TimeScale 저장
            Time.timeScale = 0f; // 설정 중 런타임 진행 일시 정지
            _overlayRoot.SetActive(true); // 설정 오버레이 표시
            _isOpen = true; // 설정 열림 상태 기록
        }

        public void ClosePanel()
        {
            if (!_isOpen) return; // 중복 닫기 차단

            _overlayRoot.SetActive(false); // 설정 오버레이 숨김
            if (_boardInputController != null) _boardInputController.enabled = _boardInputWasEnabled; // 전투 입력 기존 상태 복원
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = _routeMapWasEnabled; // 지도 입력 기존 상태 복원
            Time.timeScale = _previousTimeScale; // 기존 TimeScale 복원
            _isOpen = false; // 설정 닫힘 상태 기록
        }

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("BattleSettingsCanvas_Day56", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 인게임 설정 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결

            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전체 화면 오버레이 모드 적용
            canvas.sortingOrder = 2000; // 기존 Battle UI 위 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 적용

            _overlayRoot = new GameObject("BattleSettingsRoot_Day56", typeof(RectTransform)); // 인게임 설정 루트 생성
            _overlayRoot.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결
            Stretch(_overlayRoot.GetComponent<RectTransform>()); // 전체 화면 배치

            SettingsPanelController settingsPanel = _overlayRoot.AddComponent<SettingsPanelController>(); // 재사용 설정 UI 컴포넌트 추가
            settingsPanel.Initialize(ClosePanel); // 인게임 닫기 콜백 연결
            _overlayRoot.SetActive(false); // 기본 인게임 설정 숨김
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            new GameObject("EventSystem_Day56_Settings", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; // 전체 화면 시작 앵커 적용
            rect.anchorMax = Vector2.one; // 전체 화면 끝 앵커 적용
            rect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rect.offsetMax = Vector2.zero; // 우상단 여백 제거
        }

        private void OnDestroy()
        {
            if (!_isOpen) return; // 열린 설정이 없으면 복원 불필요
            if (_boardInputController != null) _boardInputController.enabled = _boardInputWasEnabled; // 전투 입력 상태 복원
            if (_routeMapBoardController != null) _routeMapBoardController.enabled = _routeMapWasEnabled; // 지도 입력 상태 복원
            Time.timeScale = _previousTimeScale; // 플레이 종료·씬 전환 전 TimeScale 복원
        }
    }
}
