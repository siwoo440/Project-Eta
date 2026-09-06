using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // InputSystemUIInputModule 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Run; // RunFlowPhase 사용
using ProjectEta.SceneFlow; // SceneFlowController 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1120)]
    public sealed class RunResultMainMenuController : MonoBehaviour
    {
        private BattleController _battleController; // 현재 런 상태 소유 전투 컨트롤러
        private Canvas _canvas; // 런 종료 복귀 UI Canvas
        private GameObject _root; // 런 종료 복귀 UI 루트
        private Button _mainMenuButton; // 메인 메뉴 복귀 버튼
        private Text _resultText; // Completed·Failed 결과 문구
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private void Update()
        {
            if (_battleController == null) _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 RunState 조회
            if (runState == null) return; // RunState 준비 전 처리 차단

            bool finished = runState.CurrentFlowPhase == RunFlowPhase.Completed || runState.CurrentFlowPhase == RunFlowPhase.Failed; // 런 종료 여부 계산

            if (!finished)
            {
                if (_root != null) _root.SetActive(false); // 런 진행 중 결과 복귀 UI 숨김
                return; // 종료 전 처리 종료
            }

            EnsureUI(); // 런 종료 시 복귀 UI 생성 보장
            _resultText.text = runState.CurrentFlowPhase == RunFlowPhase.Completed ? "런 클리어" : "런 종료"; // 종료 결과 문구 적용
            _mainMenuButton.interactable = !SceneFlowController.IsTransitioning; // 씬 전환 중 버튼 연타 차단
            _root.SetActive(true); // 메인 메뉴 복귀 UI 표시
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 UI 생성 차단
            EnsureEventSystem(); // UI 클릭 EventSystem 보장

            var canvasObject = new GameObject("RunResultReturnCanvas_Day54", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런 종료 복귀 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 모드 적용
            _canvas.sortingOrder = 500; // 기존 MetaProgressUI 240보다 위에 복귀 버튼 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 화면 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 개발 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            _root = new GameObject("RunResultReturnRoot", typeof(RectTransform)); // 결과 복귀 UI 루트 생성
            _root.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결
            Stretch(_root.GetComponent<RectTransform>()); // 전체 화면 배치

            var panel = new GameObject("ReturnPanel", typeof(RectTransform), typeof(Image)); // 하단 복귀 패널 생성
            panel.transform.SetParent(_root.transform, false); // 결과 루트 자식 연결
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(0f, -405f), new Vector2(620f, 155f)); // 하단 위치·크기 적용
            panel.GetComponent<Image>().color = new Color(0.045f, 0.05f, 0.065f, 0.97f); // 복귀 패널 배경 적용

            _resultText = CreateText("Result", panel.transform, 24, FontStyle.Bold); // 종료 결과 문구 생성
            SetRect(_resultText.rectTransform, new Vector2(0f, 38f), new Vector2(520f, 45f)); // 결과 문구 위치 적용

            _mainMenuButton = CreateButton(panel.transform); // 메인 메뉴 버튼 생성
            _mainMenuButton.onClick.AddListener(HandleReturnToMainMenu); // 메인 메뉴 씬 전환 연결
            _root.SetActive(false); // 런 종료 전 기본 숨김
        }

        private void HandleReturnToMainMenu()
        {
            _mainMenuButton.interactable = false; // 클릭 즉시 중복 입력 차단

            if (!SceneFlowController.ReturnToMainMenu())
            {
                _mainMenuButton.interactable = true; // 씬 전환 실패 시 입력 복구
            }
        }

        private static Button CreateButton(Transform parent)
        {
            var buttonObject = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 메인 메뉴 버튼 생성
            buttonObject.transform.SetParent(parent, false); // 패널 자식 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, -35f), new Vector2(360f, 62f)); // 버튼 위치·크기 적용
            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
            image.color = new Color(0.18f, 0.22f, 0.30f, 1f); // 버튼 배경 적용
            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 대상 그래픽 지정

            Text label = CreateText("Label", buttonObject.transform, 22, FontStyle.Bold); // 버튼 문구 생성
            label.text = "메인 메뉴"; // 버튼 문구 적용
            Stretch(label.rectTransform); // 버튼 내부 전체 배치
            return button; // 완성 버튼 반환
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
            text.raycastTarget = false; // 버튼 클릭 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 폰트 반환
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            new GameObject("EventSystem_Day54_Result", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // 위치 적용
            rect.sizeDelta = size; // 크기 적용
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rect.offsetMax = Vector2.zero; // 우상단 여백 제거
        }
    }
}
