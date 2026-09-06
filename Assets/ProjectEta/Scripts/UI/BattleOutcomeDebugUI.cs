using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 사용
using UnityEngine.UI; // Canvas·CanvasScaler·Button·Image·Text 사용
using ProjectEta.Battle; // BattleController·BattleOutcome·TurnState 사용
using ProjectEta.Run; // RunFlowPhase·RunState 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1460)]
    public sealed class BattleOutcomeDebugUI : MonoBehaviour
    {
        private BattleController _battleController; // 현재 BattleController
        private Canvas _canvas; // 승리·패배 버튼 Canvas
        private GameObject _root; // 승리·패배 버튼 루트
        private Button _victoryButton; // 승리 버튼
        private Button _defeatButton; // 패배 버튼
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 탐색
            RefreshVisibility(); // 현재 전투 상태에 따른 버튼 표시 갱신
        }

        private void ResolveBattleController()
        {
            if (_battleController != null) return; // 기존 BattleController 재사용
            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색
        }

        private void RefreshVisibility()
        {
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            TurnManager turnManager = _battleController != null ? _battleController.TurnManager : null; // 현재 턴 매니저 조회
            bool shouldShow = runState != null
                && turnManager != null
                && runState.CurrentFlowPhase == RunFlowPhase.Battle
                && turnManager.CurrentState != TurnState.BattleEnded
                && turnManager.Outcome == BattleOutcome.None; // 실제 진행 중 전투에서만 버튼 표시

            if (shouldShow) EnsureUI(); // 전투 중 승리·패배 버튼 생성 보장
            if (_root != null) _root.SetActive(shouldShow); // 현재 전투 상태 기반 버튼 루트 표시 적용

            if (_victoryButton != null) _victoryButton.interactable = shouldShow; // 승리 버튼 입력 상태 적용
            if (_defeatButton != null) _defeatButton.interactable = shouldShow; // 패배 버튼 입력 상태 적용
        }

        private void TriggerVictory()
        {
            if (!CanTrigger()) return; // 중복·잘못된 전투 상태 승리 처리 차단
            SetButtonsInteractable(false); // 클릭 즉시 승패 버튼 중복 입력 차단
            _battleController.EndBattle(BattleOutcome.Victory); // 기존 승리 전투 종료·보상·Stage 흐름 실행
        }

        private void TriggerDefeat()
        {
            if (!CanTrigger()) return; // 중복·잘못된 전투 상태 패배 처리 차단
            SetButtonsInteractable(false); // 클릭 즉시 승패 버튼 중복 입력 차단
            _battleController.EndBattle(BattleOutcome.Defeat); // 기존 패배 전투 종료·Run Failed 흐름 실행
        }

        private bool CanTrigger()
        {
            if (_battleController == null || _battleController.RunState == null || _battleController.TurnManager == null) return false; // 전투 핵심 상태 누락 차단
            if (_battleController.RunState.CurrentFlowPhase != RunFlowPhase.Battle) return false; // Battle 외 흐름 승패 버튼 차단
            if (_battleController.TurnManager.CurrentState == TurnState.BattleEnded) return false; // 이미 종료된 전투 중복 처리 차단
            return _battleController.TurnManager.Outcome == BattleOutcome.None; // 결과 미확정 전투만 승패 처리 허용
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_victoryButton != null) _victoryButton.interactable = interactable; // 승리 버튼 입력 상태 변경
            if (_defeatButton != null) _defeatButton.interactable = interactable; // 패배 버튼 입력 상태 변경
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 승패 UI 생성 차단
            EnsureEventSystem(); // 승패 버튼 클릭 입력 보장

            var canvasObject = new GameObject("BattleOutcomeDebugCanvas_Day61", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 승패 버튼 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 런타임 UI 호스트 자식 연결

            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 UI 모드 적용
            _canvas.sortingOrder = 205; // 일반 Battle UI 위·King 선택 화면 아래 정렬 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 모드 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용

            _root = new GameObject("BattleOutcomeButtons_Day61", typeof(RectTransform)); // 승패 버튼 루트 생성
            _root.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결

            RectTransform rootRect = _root.GetComponent<RectTransform>(); // 승패 버튼 루트 RectTransform 조회
            rootRect.anchorMin = new Vector2(1f, 1f); // 우측 상단 앵커 시작 적용
            rootRect.anchorMax = new Vector2(1f, 1f); // 우측 상단 앵커 끝 적용
            rootRect.pivot = new Vector2(1f, 1f); // 우측 상단 피벗 적용
            rootRect.anchoredPosition = new Vector2(-28f, -28f); // 화면 우측 상단 여백 적용
            rootRect.sizeDelta = new Vector2(340f, 72f); // 승패 버튼 영역 크기 적용

            _victoryButton = CreateButton("VictoryButton", _root.transform, "승리", new Vector2(-90f, -36f), new Vector2(150f, 60f), new Color(0.16f, 0.42f, 0.28f, 0.96f)); // 승리 버튼 생성
            _victoryButton.onClick.AddListener(TriggerVictory); // 승리 버튼 기존 전투 승리 흐름 연결

            _defeatButton = CreateButton("DefeatButton", _root.transform, "패배", new Vector2(-255f, -36f), new Vector2(150f, 60f), new Color(0.48f, 0.18f, 0.18f, 0.96f)); // 패배 버튼 생성
            _defeatButton.onClick.AddListener(TriggerDefeat); // 패배 버튼 기존 전투 패배 흐름 연결
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // Button UI 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // UI 부모 연결

            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회
            rect.anchorMin = new Vector2(1f, 1f); // 우측 상단 앵커 시작 적용
            rect.anchorMax = new Vector2(1f, 1f); // 우측 상단 앵커 끝 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // 버튼 위치 적용
            rect.sizeDelta = size; // 버튼 크기 적용

            Image background = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
            background.color = color; // 요청 버튼 배경색 적용

            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = background; // 버튼 대상 그래픽 지정

            Text text = CreateText("Label", buttonObject.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 4f); // 버튼 문구 전체 배치
            return button; // 완성 버튼 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text UI 오브젝트 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결

            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = alignment; // 텍스트 정렬 적용
            text.color = Color.white; // 기본 글자색 적용
            text.raycastTarget = false; // 버튼 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem_Day61_Outcome", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
        }

        private void OnDestroy()
        {
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
