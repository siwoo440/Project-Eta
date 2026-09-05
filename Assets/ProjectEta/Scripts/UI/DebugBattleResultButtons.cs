using System.Collections; // BattleController 준비 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 런타임 보장
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Battle; // BattleController·BattleOutcome·TurnManager·TurnState 사용
using ProjectEta.Run; // RunFlowPhase 사용

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 네임스페이스
{
    [DefaultExecutionOrder(950)] // BattleController·43일차 브리지 준비 이후 UI 연결 우선
    public sealed class DebugBattleResultButtons : MonoBehaviour // 43일차 진행 확인용 강제 승리·패배 버튼
    {
        private const float FusionButtonCenterX = -99f; // 기존 합성 버튼 우하단 중심 X
        private const float ResultButtonsBottomY = 168f; // 기존 합성 버튼 상단 바로 위 배치 Y
        private const float ResultButtonsWidth = 225f; // 기존 합성 버튼과 같은 전체 가로 폭
        private const float ResultButtonsHeight = 52f; // 결과 버튼 행 높이
        private const float ResultButtonWidth = 108f; // 개별 승리·패배 버튼 폭
        private const float ResultButtonHeight = 48f; // 개별 버튼 높이

        private BattleController _battleController; // 실제 전투 종료 진입점
        private TurnManager _turnManager; // 전투 종료 여부 감지용 턴 상태
        private Canvas _canvas; // 개발 결과 버튼 전용 Canvas
        private GameObject _buttonRoot; // 승리·패배 버튼 공통 루트
        private Button _victoryButton; // 강제 승리 버튼
        private Button _defeatButton; // 강제 패배 버튼
        private Text _victoryText; // 승리 버튼 문구
        private Text _defeatText; // 패배 버튼 문구
        private EventSystem _createdEventSystem; // 직접 생성한 EventSystem 참조
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시
        private bool _isBound; // 전투 상태 연결 완료 여부

        public Button VictoryButton => _victoryButton; // 테스트·디버그용 승리 버튼 공개
        public Button DefeatButton => _defeatButton; // 테스트·디버그용 패배 버튼 공개
        public bool IsBound => _isBound; // 전투 상태 연결 여부 공개

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 직후 자동 생성
        private static void AutoCreateForBattleScene() // 씬 수정 없이 개발 버튼 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 방지
            if (Object.FindFirstObjectByType<DebugBattleResultButtons>() != null) return; // 중복 생성 방지

            var host = new GameObject("DebugBattleResultButtons_Day43"); // 개발 결과 버튼 호스트 생성
            host.AddComponent<DebugBattleResultButtons>(); // 승리·패배 UI 컴포넌트 추가
        }

        private IEnumerator Start() // BattleController 생성 완료까지 기다린 뒤 UI 구성
        {
            const int maxWaitFrames = 180; // 최대 3초 안팎 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 필수 전투 객체 준비 대기
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색

                if (_battleController != null && _battleController.TurnManager != null && _battleController.RunState != null) // 필수 상태 준비 확인
                {
                    Bind(_battleController); // 실제 전투 상태에 개발 버튼 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("43일차 DebugBattleResultButtons 초기화 실패: BattleController 또는 RunState를 찾지 못했습니다."); // 초기화 실패 기록
        }

        public void Bind(BattleController battleController) // 외부 테스트·런타임에서 전투 컨트롤러 연결
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 이전 턴 이벤트 구독 해제

            _battleController = battleController; // 실제 BattleController 저장
            _turnManager = _battleController != null ? _battleController.TurnManager : null; // TurnManager 참조 저장

            EnsureUI(); // 승리·패배 버튼 UI 생성

            if (_turnManager != null) _turnManager.TurnChanged += HandleTurnChanged; // 전투 종료 시 버튼 상태 갱신

            _isBound = _battleController != null && _turnManager != null; // 연결 상태 기록
            RefreshInteractable(); // 현재 전투 상태 즉시 반영
        }

        public static bool CanUseResultButtons(TurnManager turnManager, RunFlowPhase flowPhase) // 개발 결과 버튼 공통 사용 가능 규칙
        {
            if (turnManager == null) return false; // 턴 상태가 없으면 버튼 차단
            if (turnManager.CurrentState == TurnState.BattleEnded) return false; // 이미 전투가 끝났으면 중복 결과 차단
            return flowPhase == RunFlowPhase.Battle; // 실제 전투 흐름에서만 결과 버튼 허용
        }

        private void OnVictoryClicked() // 개발용 승리 버튼 클릭 처리
        {
            if (!CanTriggerResult()) return; // 중복·잘못된 흐름 입력 차단

            Debug.Log("43일차 개발 버튼: 강제 승리 처리"); // 개발용 결과 기록
            _battleController.EndBattle(BattleOutcome.Victory); // 실제 전투 종료 진입점으로 승리 전달
            RefreshInteractable(); // 종료 직후 버튼 즉시 비활성화
        }

        private void OnDefeatClicked() // 개발용 패배 버튼 클릭 처리
        {
            if (!CanTriggerResult()) return; // 중복·잘못된 흐름 입력 차단

            Debug.Log("43일차 개발 버튼: 강제 패배 처리"); // 개발용 결과 기록
            _battleController.EndBattle(BattleOutcome.Defeat); // 실제 전투 종료 진입점으로 패배 전달
            RefreshInteractable(); // 종료 직후 버튼 즉시 비활성화
        }

        private bool CanTriggerResult() // 현재 실제 전투 상태 기준 클릭 허용 여부
        {
            if (_battleController == null || _battleController.RunState == null) return false; // 전투·런 상태 누락 차단
            return CanUseResultButtons(_turnManager, _battleController.RunState.CurrentFlowPhase); // 공통 규칙으로 최종 판정
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 상태 변화 수신
        {
            RefreshInteractable(); // 전투 종료 등 상태 변경을 버튼에 즉시 반영
        }

        private void RefreshInteractable() // 현재 런·전투 상태에 따라 두 버튼 활성화 갱신
        {
            bool canUse = CanTriggerResult(); // 현재 강제 결과 입력 가능 여부 계산

            if (_victoryButton != null) _victoryButton.interactable = canUse; // 승리 버튼 상태 반영
            if (_defeatButton != null) _defeatButton.interactable = canUse; // 패배 버튼 상태 반영

            if (_victoryText != null) _victoryText.color = canUse ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f); // 승리 문구 비활성 색 반영
            if (_defeatText != null) _defeatText.color = canUse ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f); // 패배 문구 비활성 색 반영
        }

        private void EnsureUI() // 개발 결과 버튼 Canvas를 한 번만 생성
        {
            if (_canvas != null) return; // 이미 생성된 경우 중복 생성 방지

            EnsureEventSystem(); // 버튼 클릭 입력을 처리할 EventSystem 보장

            var canvasObject = new GameObject("DebugBattleResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 호스트 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 직접 표시
            _canvas.sortingOrder = 97; // 합성 Canvas 96 바로 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 화면 크기 대응용 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 기반 UI 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 합성 UI와 동일 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildButtons(canvasObject.transform); // 합성 버튼 위 결과 버튼 행 생성
        }

        private void BuildButtons(Transform parent) // 승리·패배 버튼 2개를 합성 버튼 바로 위에 배치
        {
            _buttonRoot = new GameObject("DebugBattleResultButtons", typeof(RectTransform)); // 두 버튼 공통 루트 생성
            _buttonRoot.transform.SetParent(parent, false); // Canvas 자식으로 연결

            var rootRect = _buttonRoot.GetComponent<RectTransform>(); // 루트 RectTransform 확보
            rootRect.anchorMin = new Vector2(1f, 0f); // 기존 합성 버튼과 같은 우하단 앵커
            rootRect.anchorMax = new Vector2(1f, 0f); // 우하단 앵커 고정
            rootRect.pivot = new Vector2(0.5f, 0f); // 하단 중앙 피벗 사용
            rootRect.anchoredPosition = new Vector2(FusionButtonCenterX, ResultButtonsBottomY); // 합성 버튼 상단 위 위치
            rootRect.sizeDelta = new Vector2(ResultButtonsWidth, ResultButtonsHeight); // 합성 버튼과 같은 전체 폭

            _victoryButton = CreateResultButton("DebugVictoryButton", rootRect, "승리", new Color(0.18f, 0.5f, 0.22f, 0.96f), true, out _victoryText); // 좌측 승리 버튼 생성
            _victoryButton.onClick.AddListener(OnVictoryClicked); // 승리 처리 연결

            _defeatButton = CreateResultButton("DebugDefeatButton", rootRect, "패배", new Color(0.58f, 0.17f, 0.17f, 0.96f), false, out _defeatText); // 우측 패배 버튼 생성
            _defeatButton.onClick.AddListener(OnDefeatClicked); // 패배 처리 연결
        }

        private static Button CreateResultButton(string name, Transform parent, string label, Color color, bool alignLeft, out Text labelText) // 개발 결과 버튼 공통 생성
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 GameObject 생성
            buttonObject.transform.SetParent(parent, false); // 버튼 행 자식으로 연결

            var rect = buttonObject.GetComponent<RectTransform>(); // RectTransform 확보
            rect.anchorMin = new Vector2(alignLeft ? 0f : 1f, 0.5f); // 좌측 또는 우측 중앙 앵커 선택
            rect.anchorMax = rect.anchorMin; // 고정 앵커 사용
            rect.pivot = new Vector2(alignLeft ? 0f : 1f, 0.5f); // 바깥쪽 기준 피벗 설정
            rect.anchoredPosition = Vector2.zero; // 루트 가장자리 기준 배치
            rect.sizeDelta = new Vector2(ResultButtonWidth, ResultButtonHeight); // 개별 버튼 크기 적용

            var image = buttonObject.GetComponent<Image>(); // 버튼 배경 이미지 확보
            image.color = color; // 승리·패배 구분 색 적용

            var button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            button.targetGraphic = image; // 배경 이미지를 상태 전환 대상으로 지정

            var colors = button.colors; // 기본 상태 색 복사
            colors.normalColor = Color.white; // 기본 배경 원색 유지
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f); // 마우스 오버 강조
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f); // 클릭 시 눌림 표시
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.72f); // 비활성화 상태 표시
            button.colors = colors; // 색 상태 적용

            var outline = buttonObject.AddComponent<Outline>(); // 버튼 경계선 추가
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f); // 어두운 외곽선
            outline.effectDistance = new Vector2(1.5f, -1.5f); // 외곽선 두께 적용
            outline.useGraphicAlpha = true; // 원본 알파 반영

            labelText = CreateText($"{name}Text", buttonObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 버튼 텍스트 생성
            Stretch(labelText.rectTransform, 2f, 2f, 2f, 2f); // 버튼 내부 전체에 텍스트 배치
            labelText.text = label; // 승리·패배 문구 적용

            return button; // 완성 버튼 반환
        }

        private void EnsureEventSystem() // UI 클릭용 EventSystem 보장
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 카드·합성 UI EventSystem 재사용

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성한 EventSystem 저장
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment, Color color) // 런타임 Text 공통 생성
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 텍스트 GameObject 생성
            textObject.transform.SetParent(parent, false); // 부모에 연결

            var text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 표시 가능한 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 굵기 스타일 적용
            text.alignment = alignment; // 중앙 정렬 적용
            text.color = color; // 글자색 적용
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 폭 초과 시 줄바꿈
            text.verticalOverflow = VerticalWrapMode.Truncate; // 높이 초과 시 잘라 표시
            text.raycastTarget = false; // 버튼 클릭을 텍스트가 가로채지 않도록 차단

            return text; // 완성 텍스트 반환
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top) // 부모 안쪽으로 텍스트 Stretch
        {
            rect.anchorMin = Vector2.zero; // 좌하단 앵커
            rect.anchorMax = Vector2.one; // 우상단 앵커
            rect.offsetMin = new Vector2(left, bottom); // 좌·하단 여백 적용
            rect.offsetMax = new Vector2(-right, -top); // 우·상단 여백 적용
        }

        private static Font GetRuntimeFont() // 기존 합성 UI와 같은 한글 런타임 폰트 규칙
        {
            if (_runtimeFont != null) return _runtimeFont; // 생성된 폰트 캐시 재사용

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 한글 시스템 폰트 우선 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용

            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // 컴포넌트 제거 시 이벤트·직접 생성 객체 정리
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 구독 해제

            if (_victoryButton != null) _victoryButton.onClick.RemoveListener(OnVictoryClicked); // 승리 버튼 리스너 제거
            if (_defeatButton != null) _defeatButton.onClick.RemoveListener(OnDefeatClicked); // 패배 버튼 리스너 제거

            if (_createdEventSystem != null) // 직접 생성 EventSystem이 있을 때만 정리
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode 안전 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode 즉시 제거
            }
        }
    }
}
