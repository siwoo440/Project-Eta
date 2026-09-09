using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 확인
using UnityEngine.UI; // Canvas·Image·Text 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Run; // RunState·RunEconomyState·RunFlowPhase 사용

namespace ProjectEta.UI // 65일차 UI 네임스페이스
{
    [DefaultExecutionOrder(1180)] // 기존 Stage Activity 상태 변경 뒤 표시 갱신
    public sealed class Day65StageActivityHUD : MonoBehaviour // Reward·Shop·Event 공통 상태·결과 HUD
    {
        private const float ResultDuration = 2.8f; // 선택 결과 알림 유지 시간

        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private readonly Day65ActivityDeltaState _deltaState = new Day65ActivityDeltaState(); // Gold·HP·카드 수 변화 추적 상태
        private BattleController _battleController; // 현재 런 상태 제공 전투 컨트롤러
        private RunState _runState; // 현재 런 상태
        private RunEconomyState _economy; // 현재 Gold 상태
        private StagePlaceholderUI _legacyPlaceholder; // 45일차 임시 UI 숨김 대상
        private Canvas _canvas; // 공통 Screen Space HUD Canvas
        private GameObject _statusRoot; // Shop·Event 상단 상태 바
        private Text _activityText; // 현재 활동 이름
        private Text _statusText; // Gold·King HP·보유 카드 상태
        private GameObject _resultRoot; // 선택 결과 토스트 루트
        private Text _resultText; // 선택 결과 문구
        private float _resultRemaining; // 결과 토스트 남은 시간
        private RunFlowPhase _lastObservedPhase = RunFlowPhase.Battle; // 직전 프레임 흐름 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 설치
        private static void AutoCreateForBattleScene() // 씬 수동 배치 없이 65일차 HUD 생성
        {
            if (SceneManager.GetActiveScene().name != "Battle") // Battle 씬 여부 확인
            {
                return; // 다른 씬 생성 차단
            }

            if (Object.FindFirstObjectByType<Day65StageActivityHUD>() != null) // 기존 HUD 존재 여부 확인
            {
                return; // 중복 생성 차단
            }

            var host = new GameObject("Day65StageActivityHUD"); // 65일차 공통 HUD 호스트 생성
            host.AddComponent<Day65StageActivityHUD>(); // 자동 표시 컴포넌트 추가
        }

        private void Awake() // HUD 초기화
        {
            EnsureUI(); // 공통 Canvas 생성
        }

        private void Update() // 런 상태와 UI 실시간 동기화
        {
            ResolveBindings(); // BattleController·RunState·Gold 연결 보장

            if (_runState == null || _economy == null) // 런 상태 연결 전 확인
            {
                HideStatus(); // 상태 바 숨김
                UpdateResultTimer(); // 남은 토스트 시간 갱신
                return; // 다음 프레임 재시도
            }

            RunFlowPhase currentPhase = _runState.CurrentFlowPhase; // 현재 Run 흐름 조회
            RunFlowPhase phaseForDelta = IsActivityPhase(_lastObservedPhase) ? _lastObservedPhase : currentPhase; // 즉시 Map 전환된 결과는 직전 활동 기준으로 판정
            int ownedCardCount = _runState.Deck != null ? _runState.Deck.OwnedCardPool.Count : 0; // 현재 보유 카드 수 조회
            Day65ActivityDelta delta = _deltaState.Capture(_economy.Currency, _runState.KingHp, ownedCardCount); // Gold·HP·카드 수 변화 계산

            if (delta.HasChange) // 실제 상태 변화 발생 확인
            {
                string summary = delta.BuildSummary(phaseForDelta); // 활동 타입에 맞는 결과 문구 생성

                if (!string.IsNullOrWhiteSpace(summary)) // 표시할 결과 문구 확인
                {
                    ShowResult(summary); // 선택 결과 토스트 표시
                }
            }

            if (currentPhase == RunFlowPhase.Shop || currentPhase == RunFlowPhase.Event) // Shop·Event 진행 중 확인
            {
                ShowStatus(currentPhase, ownedCardCount); // 공통 Gold·HP 상태 바 표시
                HideLegacyPlaceholder(); // 구형 전체 화면 Placeholder 중복 표시 억제
            }
            else
            {
                HideStatus(); // 다른 흐름에서는 공통 상태 바 숨김
            }

            _lastObservedPhase = currentPhase; // 현재 흐름을 다음 프레임 기준으로 저장
            UpdateResultTimer(); // 결과 토스트 시간 갱신
        }

        private void ResolveBindings() // 런 상태 제공 객체 자동 연결
        {
            if (_battleController != null && _runState != null && _economy != null) // 기존 연결 유효 여부 확인
            {
                return; // 현재 연결 재사용
            }

            _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색

            if (_battleController == null || _battleController.RunState == null) // 전투 컨트롤러 준비 여부 확인
            {
                return; // 다음 프레임 재탐색
            }

            RunState foundRunState = _battleController.RunState; // 현재 런 상태 조회

            if (_runState != foundRunState) // 런 인스턴스 변경 여부 확인
            {
                _runState = foundRunState; // 새 런 상태 저장
                _economy = RunEconomyService.GetOrCreate(_runState); // 새 런 Gold 상태 연결
                _deltaState.Clear(); // 이전 런 변화 기준 제거
                _lastObservedPhase = _runState.CurrentFlowPhase; // 현재 흐름을 기준으로 저장
            }
            else if (_economy == null) // 런은 같지만 경제 상태만 누락된 경우 확인
            {
                _economy = RunEconomyService.GetOrCreate(_runState); // 기존 런 Gold 상태 재연결
            }
        }

        private void ShowStatus(RunFlowPhase phase, int ownedCardCount) // Shop·Event 공통 상단 상태 바 표시
        {
            EnsureUI(); // UI 생성 보장
            _activityText.text = phase == RunFlowPhase.Shop ? "SHOP" : "EVENT"; // 현재 활동 이름 표시
            _statusText.text = $"Gold {_economy.Currency}    |    King HP {_runState.KingHp}/{RunEconomyRules.PrototypeKingMaxHp}    |    Cards {ownedCardCount}"; // 공통 상태 문구 표시
            _statusRoot.SetActive(true); // 상단 상태 바 활성화
        }

        private void HideStatus() // 공통 상태 바 숨김
        {
            if (_statusRoot != null) // 상태 바 생성 여부 확인
            {
                _statusRoot.SetActive(false); // 상단 상태 바 비활성화
            }
        }

        private void ShowResult(string summary) // 선택 결과 토스트 표시
        {
            EnsureUI(); // UI 생성 보장
            _resultText.text = summary; // 결과 문구 적용
            _resultRoot.SetActive(true); // 결과 토스트 활성화
            _resultRemaining = ResultDuration; // 유지 시간 초기화
        }

        private void UpdateResultTimer() // 결과 토스트 유지 시간 처리
        {
            if (_resultRemaining <= 0f) // 표시 중인 결과 없음 확인
            {
                return; // 시간 처리 생략
            }

            _resultRemaining -= Time.unscaledDeltaTime; // 일시정지와 무관하게 유지 시간 감소

            if (_resultRemaining <= 0f) // 유지 시간 종료 확인
            {
                _resultRemaining = 0f; // 음수 시간 보정

                if (_resultRoot != null) // 결과 루트 생성 여부 확인
                {
                    _resultRoot.SetActive(false); // 결과 토스트 숨김
                }
            }
        }

        private void HideLegacyPlaceholder() // 45일차 임시 UI 중복 표시 억제
        {
            if (_legacyPlaceholder == null) // 임시 UI 캐시 확인
            {
                _legacyPlaceholder = Object.FindFirstObjectByType<StagePlaceholderUI>(); // 구형 Placeholder 탐색
            }

            if (_legacyPlaceholder != null && _legacyPlaceholder.IsVisible) // 실제 표시 중인지 확인
            {
                _legacyPlaceholder.Hide(); // 정식 Stage Activity UI만 남김
            }
        }

        private void EnsureUI() // 65일차 공통 HUD Canvas 최초 생성
        {
            if (_canvas != null) // 기존 Canvas 존재 여부 확인
            {
                return; // 중복 생성 차단
            }

            var canvasObject = new GameObject("Day65StageActivityCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 공통 HUD Canvas 생성
            canvasObject.transform.SetParent(transform, false); // HUD 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 고정 HUD 모드 적용
            _canvas.sortingOrder = 235; // Stage World UI와 일반 HUD 위에 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildStatusBar(canvasObject.transform); // 상단 상태 바 생성
            BuildResultToast(canvasObject.transform); // 선택 결과 토스트 생성
        }

        private void BuildStatusBar(Transform parent) // Shop·Event 공통 상태 바 생성
        {
            _statusRoot = new GameObject("ActivityStatusBar", typeof(RectTransform), typeof(Image)); // 상태 바 루트 생성
            _statusRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = _statusRoot.GetComponent<RectTransform>(); // 상태 바 RectTransform 확보
            SetRect(rootRect, new Vector2(0f, 470f), new Vector2(920f, 86f)); // 화면 상단 중앙 배치
            Image background = _statusRoot.GetComponent<Image>(); // 상태 바 배경 확보
            background.color = new Color(0.025f, 0.03f, 0.045f, 0.94f); // 전투 UI와 맞춘 어두운 배경 적용
            background.raycastTarget = false; // 기존 Stage Activity 클릭 방해 차단

            _activityText = CreateText("ActivityName", _statusRoot.transform, 24, FontStyle.Bold); // 활동 이름 텍스트 생성
            _activityText.alignment = TextAnchor.MiddleLeft; // 좌측 정렬 적용
            SetRect(_activityText.rectTransform, new Vector2(-362f, 0f), new Vector2(150f, 58f)); // 활동 이름 위치 적용

            _statusText = CreateText("ActivityState", _statusRoot.transform, 22, FontStyle.Bold); // Gold·HP 상태 텍스트 생성
            _statusText.alignment = TextAnchor.MiddleRight; // 우측 정렬 적용
            SetRect(_statusText.rectTransform, new Vector2(98f, 0f), new Vector2(700f, 58f)); // 상태 문구 위치 적용
            _statusRoot.SetActive(false); // 기본 숨김 상태 지정
        }

        private void BuildResultToast(Transform parent) // 선택 결과 확인용 중앙 상단 토스트 생성
        {
            _resultRoot = new GameObject("ActivityResultToast", typeof(RectTransform), typeof(Image)); // 결과 토스트 루트 생성
            _resultRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = _resultRoot.GetComponent<RectTransform>(); // 결과 토스트 RectTransform 확보
            SetRect(rootRect, new Vector2(0f, 370f), new Vector2(760f, 72f)); // 상태 바 아래 결과 위치 적용
            Image background = _resultRoot.GetComponent<Image>(); // 결과 토스트 배경 확보
            background.color = new Color(0.12f, 0.09f, 0.035f, 0.96f); // 결과 강조 배경 적용
            background.raycastTarget = false; // 기존 입력 방해 차단

            _resultText = CreateText("ResultText", _resultRoot.transform, 24, FontStyle.Bold); // 결과 문구 텍스트 생성
            SetRect(_resultText.rectTransform, Vector2.zero, new Vector2(720f, 54f)); // 결과 문구 영역 적용
            _resultRoot.SetActive(false); // 기본 결과 토스트 숨김
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle) // 공통 Text 생성
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 부모 자식 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 표시용 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = TextAnchor.MiddleCenter; // 기본 중앙 정렬 적용
            text.color = Color.white; // 기본 흰색 글자 적용
            text.raycastTarget = false; // UI 클릭 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont() // 한글 표시 가능한 런타임 폰트 확보
        {
            if (_runtimeFont != null) // 기존 폰트 캐시 확인
            {
                return _runtimeFont; // 기존 폰트 재사용
            }

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 우선 생성

            if (_runtimeFont == null) // 시스템 폰트 생성 실패 확인
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            }

            return _runtimeFont; // 최종 폰트 반환
        }

        private static bool IsActivityPhase(RunFlowPhase phase) // 결과 판정에 사용하는 Stage Activity 흐름 확인
        {
            return phase == RunFlowPhase.Reward || phase == RunFlowPhase.Shop || phase == RunFlowPhase.Event; // Reward·Shop·Event만 활동으로 인정
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) // 중앙 기준 RectTransform 설정
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }
    }
}
