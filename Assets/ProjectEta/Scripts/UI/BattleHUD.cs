using System.Reflection; // 기존 BattleController 턴 제한 필드 조회
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.UI; // Canvas·CanvasScaler·Image·Text 사용
using ProjectEta.Battle; // BattleController·TurnManager·TurnState 사용
using ProjectEta.Run; // RunState·RunEconomyState·RunFlowPhase 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(900)]
    public sealed class BattleHUD : MonoBehaviour
    {
        private const int CanvasOrder = 190; // 일반 전투 UI보다 위인 HUD 정렬 순서
        private static readonly BindingFlags TurnLimitBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic; // 비공개 인스턴스 필드 조회 규칙
        private static FieldInfo _turnLimitField; // BattleController 턴 제한 필드 캐시
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private BattleController _battleController; // 현재 전투 컨트롤러
        private RunState _runState; // 현재 런 상태
        private TurnManager _turnManager; // 현재 턴 상태
        private Canvas _canvas; // Battle HUD Canvas
        private GameObject _root; // Battle HUD 루트
        private Text _stageText; // Stage 표시
        private Text _turnStateText; // 현재 턴 상태 표시
        private Text _goldText; // Gold 표시
        private Text _battleText; // Battle 흐름 표시
        private Text _turnNumberText; // 턴 번호 표시
        private Text _deploymentText; // 배치 안내 표시

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 참조 갱신
            SuppressLegacyResultButtons(); // 43일차 중복 승패 UI 런타임 차단
            Refresh(); // 현재 전투 전체 HUD 갱신
        }

        private void ResolveBattleController()
        {
            if (_battleController != null && _battleController.RunState != null && _battleController.TurnManager != null)
            {
                _runState = _battleController.RunState; // 기존 RunState 참조 동기화
                _turnManager = _battleController.TurnManager; // 기존 TurnManager 참조 동기화
                return; // 기존 전투 참조 재사용
            }

            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색

            if (_battleController == null)
            {
                _runState = null; // 전투 컨트롤러 누락 상태 반영
                _turnManager = null; // 턴 매니저 누락 상태 반영
                return; // 참조 탐색 종료
            }

            _runState = _battleController.RunState; // 탐색한 RunState 저장
            _turnManager = _battleController.TurnManager; // 탐색한 TurnManager 저장
        }

        private void Refresh()
        {
            bool shouldShow = _runState != null
                && _turnManager != null
                && _runState.CurrentFlowPhase == RunFlowPhase.Battle; // 실제 Battle 흐름 표시 조건 계산

            if (shouldShow)
            {
                EnsureUI(); // Battle HUD 생성 보장
            }

            if (_root != null)
            {
                _root.SetActive(shouldShow); // Battle 외 흐름 HUD 숨김 적용
            }

            if (!shouldShow || _root == null)
            {
                return; // 표시 불가 상태 갱신 종료
            }

            int currency = ResolveCurrency(); // 현재 런 Gold 조회
            int turnLimit = ResolveTurnLimit(); // 실제 BattleController 턴 제한 조회

            _stageText.text = BattleHudPresentation.BuildStageText(_runState.CurrentRound); // 현재 Stage 문구 갱신
            _turnStateText.text = BattleHudPresentation.BuildTurnStateText(_turnManager.CurrentState, _turnManager.IsInitialDeployment); // 현재 턴 상태 문구 갱신
            _goldText.text = BattleHudPresentation.BuildGoldText(currency); // 현재 Gold 문구 갱신
            _battleText.text = "BATTLE"; // 현재 상위 전투 흐름 표시
            _turnNumberText.text = BattleHudPresentation.BuildTurnText(_turnManager.TurnNumber, turnLimit); // 현재 턴 번호·제한 갱신
            _deploymentText.text = BattleHudPresentation.BuildDeploymentText(_turnManager.CurrentState, _turnManager.IsInitialDeployment, _turnManager.DeployedCardCount, _turnManager.TurnNumber); // 배치 관련 안내 갱신
        }

        private int ResolveCurrency()
        {
            if (_runState != null && RunEconomyService.TryGet(_runState, out RunEconomyState economy))
            {
                return economy.Currency; // 등록된 현재 런 Gold 반환
            }

            return RunEconomyRules.StartingCurrency; // 미등록 신규 런 기본 Gold 표시
        }

        private int ResolveTurnLimit()
        {
            if (_battleController == null)
            {
                return 0; // 전투 컨트롤러 누락 시 제한 미표시
            }

            if (_turnLimitField == null)
            {
                _turnLimitField = typeof(BattleController).GetField("_turnLimitTestValue", TurnLimitBindingFlags); // 기존 직렬화 턴 제한 필드 탐색
            }

            if (_turnLimitField == null)
            {
                return 0; // 턴 제한 필드 변경 시 안전한 기본 표시
            }

            object rawValue = _turnLimitField.GetValue(_battleController); // 현재 인스턴스 턴 제한 값 조회

            if (rawValue is int turnLimit)
            {
                return Mathf.Max(0, turnLimit); // 음수 없는 턴 제한 반환
            }

            return 0; // 잘못된 필드 값 안전 처리
        }

        private void SuppressLegacyResultButtons()
        {
            BattleOutcomeDebugUI currentOutcomeUI = Object.FindFirstObjectByType<BattleOutcomeDebugUI>(); // 61일차 정식 개발 승패 UI 확인

            if (currentOutcomeUI == null)
            {
                return; // 61일차 승패 UI가 없으면 구형 UI 유지
            }

            DebugBattleResultButtons legacyButtons = Object.FindFirstObjectByType<DebugBattleResultButtons>(); // 43일차 구형 승패 UI 탐색

            if (legacyButtons == null)
            {
                return; // 구형 UI가 없으면 처리 종료
            }

            legacyButtons.gameObject.SetActive(false); // 같은 프레임 구형 버튼 노출 차단
            Destroy(legacyButtons.gameObject); // 43일차 중복 승패 UI 호스트 제거
        }

        private void EnsureUI()
        {
            if (_canvas != null)
            {
                return; // 중복 Battle HUD 생성 차단
            }

            var canvasObject = new GameObject("BattleHUDCanvas_Day62", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 공통 전투 HUD Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 런타임 HUD 호스트 자식 연결

            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 UI 모드 적용
            _canvas.sortingOrder = CanvasOrder; // 승패 버튼보다 아래 HUD 정렬 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 모드 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용

            _root = new GameObject("BattleHUDRoot_Day62", typeof(RectTransform), typeof(Image)); // 상단 공통 HUD 배경 생성
            _root.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결

            RectTransform rootRect = _root.GetComponent<RectTransform>(); // HUD 루트 RectTransform 조회
            rootRect.anchorMin = new Vector2(0.5f, 1f); // 상단 중앙 앵커 시작 적용
            rootRect.anchorMax = new Vector2(0.5f, 1f); // 상단 중앙 앵커 끝 적용
            rootRect.pivot = new Vector2(0.5f, 1f); // 상단 중앙 피벗 적용
            rootRect.anchoredPosition = new Vector2(0f, -24f); // 기존 좌우 HUD와 분리된 상단 위치 적용
            rootRect.sizeDelta = new Vector2(1120f, 112f); // 공통 HUD 영역 크기 적용

            Image background = _root.GetComponent<Image>(); // HUD 배경 이미지 조회
            background.color = new Color(0.035f, 0.045f, 0.06f, 0.88f); // 어두운 반투명 HUD 배경 적용
            background.raycastTarget = false; // 보드·버튼 입력 간섭 제거

            _stageText = CreateText("StageText", _root.transform, 25, FontStyle.Bold, TextAnchor.MiddleLeft); // 좌측 Stage 문구 생성
            SetRect(_stageText.rectTransform, new Vector2(16f, -12f), new Vector2(360f, 42f)); // Stage 문구 위치 적용

            _turnStateText = CreateText("TurnStateText", _root.transform, 28, FontStyle.Bold, TextAnchor.MiddleCenter); // 중앙 턴 상태 문구 생성
            SetRect(_turnStateText.rectTransform, new Vector2(380f, -12f), new Vector2(360f, 42f)); // 턴 상태 문구 위치 적용

            _goldText = CreateText("GoldText", _root.transform, 25, FontStyle.Bold, TextAnchor.MiddleRight); // 우측 Gold 문구 생성
            SetRect(_goldText.rectTransform, new Vector2(760f, -12f), new Vector2(344f, 42f)); // Gold 문구 위치 적용

            _battleText = CreateText("BattleText", _root.transform, 19, FontStyle.Normal, TextAnchor.MiddleLeft); // 좌측 Battle 흐름 문구 생성
            SetRect(_battleText.rectTransform, new Vector2(16f, -58f), new Vector2(360f, 36f)); // Battle 문구 위치 적용

            _turnNumberText = CreateText("TurnNumberText", _root.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter); // 중앙 턴 번호 문구 생성
            SetRect(_turnNumberText.rectTransform, new Vector2(380f, -58f), new Vector2(360f, 36f)); // 턴 번호 문구 위치 적용

            _deploymentText = CreateText("DeploymentText", _root.transform, 18, FontStyle.Normal, TextAnchor.MiddleRight); // 우측 배치 안내 문구 생성
            SetRect(_deploymentText.rectTransform, new Vector2(760f, -58f), new Vector2(344f, 36f)); // 배치 안내 문구 위치 적용
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text UI 오브젝트 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결

            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = alignment; // 문구 정렬 적용
            text.color = Color.white; // 기본 글자색 적용
            text.raycastTarget = false; // UI 입력 간섭 제거
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 긴 Stage 문구 가로 보존
            text.verticalOverflow = VerticalWrapMode.Truncate; // 지정 높이 밖 문구 차단
            return text; // 완성 Text 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 기준 앵커 시작 적용
            rect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 기준 앵커 끝 적용
            rect.pivot = new Vector2(0f, 1f); // 좌측 상단 피벗 적용
            rect.anchoredPosition = position; // 요청 위치 적용
            rect.sizeDelta = size; // 요청 크기 적용
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null)
            {
                return _runtimeFont; // 기존 한글 런타임 폰트 재사용
            }

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 주요 OS 한글 폰트 생성

            if (_runtimeFont == null)
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체 적용
            }

            return _runtimeFont; // 최종 런타임 폰트 반환
        }
    }
}
