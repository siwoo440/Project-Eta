using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Vector2, Color 등을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, Text, CanvasGroup 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 턴 관련 타입을 모아두는 네임스페이스
{
    public class DeploymentTurnBannerUI : MonoBehaviour // 32일차: 배치 턴으로 전환될 때 화면 중앙에 "배치 턴" 문구를 잠깐 띄웠다 지우는 컴포넌트
    {
        [SerializeField] private float _holdSeconds = 1.1f; // 완전히 보인 채로 유지되는 시간
        [SerializeField] private float _fadeInSeconds = 0.2f; // 서서히 나타나는 데 걸리는 시간
        [SerializeField] private float _fadeOutSeconds = 0.35f; // 서서히 사라지는 데 걸리는 시간

        public bool IsShowing => _label != null && _label.gameObject.activeSelf; // 테스트와 디버그에서 배너 표시 시퀀스가 진행 중인지 확인하는 프로퍼티

        private TurnManager _turnManager; // 배치 턴 전환을 감지할 실제 턴 매니저
        private Canvas _canvas; // 이 UI 전용 Screen Space Overlay Canvas
        private CanvasGroup _canvasGroup; // 페이드 인/아웃에 사용할 알파 제어 그룹
        private Text _label; // "배치 턴" 문구 텍스트
        private Coroutine _showCoroutine; // 현재 재생 중인 표시 코루틴(중복 배치 턴 진입 시 재시작하기 위함)
        private TurnState? _previousState; // 32일차 수정: 직전 턴 상태(같은 배치 턴 안에서의 재발행과 실제 전환을 구분하기 위함)
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시

        public void Bind(TurnManager turnManager) // 턴 매니저를 UI에 연결하는 메서드
        {
            if (_turnManager != null) // 이전에 연결된 턴 매니저가 있었다면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 이전 이벤트 구독 해제
            }

            _turnManager = turnManager; // 새 턴 매니저 참조 저장
            _previousState = _turnManager?.CurrentState; // 현재 상태를 기준으로 초기화해 연결 직후 오탐지 방지
            EnsureUI(); // 배너 Canvas를 런타임 생성(최초 1회)

            if (_turnManager != null) // 정상 턴 매니저가 전달됐다면
            {
                _turnManager.TurnChanged += HandleTurnChanged; // 턴 전환 이벤트 구독
            }
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴이 바뀔 때마다 호출되는 이벤트 처리 메서드
        {
            bool isEnteringDeploymentTurn = state == TurnState.DeploymentTurn && _previousState != TurnState.DeploymentTurn; // 32일차 수정: 배치 턴 "진입" 순간인지 확인(배치 턴 도중 카드 배치·킹 배치마다 재발행되는 것과 구분)
            _previousState = state; // 다음 비교를 위해 이번 상태를 기억

            if (!isEnteringDeploymentTurn) return; // 이미 배치 턴이던 중의 재발행이면 배너를 다시 띄우지 않음

            if (_showCoroutine != null) StopCoroutine(_showCoroutine); // 이미 재생 중인 배너가 있으면 중단하고 새로 시작
            _showCoroutine = StartCoroutine(PlayShowSequence()); // 나타났다 사라지는 연출 시작
        }

        private IEnumerator PlayShowSequence() // 배너를 페이드 인 → 유지 → 페이드 아웃 순서로 재생하는 코루틴
        {
            _label.gameObject.SetActive(true); // 배너 표시 시작

            float elapsed = 0f; // 경과 시간
            while (elapsed < _fadeInSeconds) // 페이드 인 구간
            {
                elapsed += Time.unscaledDeltaTime; // 타임스케일과 무관하게 진행
                _canvasGroup.alpha = _fadeInSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / _fadeInSeconds); // 0에서 1로 서서히 나타남
                yield return null; // 다음 프레임까지 대기
            }
            _canvasGroup.alpha = 1f; // 오차 누적 방지를 위해 완전히 보이는 상태로 고정

            yield return new WaitForSecondsRealtime(_holdSeconds); // 완전히 보이는 상태를 잠시 유지

            elapsed = 0f; // 페이드 아웃을 위해 경과 시간 재사용
            while (elapsed < _fadeOutSeconds) // 페이드 아웃 구간
            {
                elapsed += Time.unscaledDeltaTime; // 타임스케일과 무관하게 진행
                _canvasGroup.alpha = _fadeOutSeconds <= 0f ? 0f : 1f - Mathf.Clamp01(elapsed / _fadeOutSeconds); // 1에서 0으로 서서히 사라짐
                yield return null; // 다음 프레임까지 대기
            }
            _canvasGroup.alpha = 0f; // 오차 누적 방지를 위해 완전히 투명한 상태로 고정

            _label.gameObject.SetActive(false); // 완전히 사라진 뒤에는 오브젝트도 비활성화
            _showCoroutine = null; // 완료 후 참조 정리
        }

        private void EnsureUI() // 배너 Canvas를 한 번만 만드는 메서드
        {
            if (_canvas != null) return; // 이미 생성됐으면 중복 생성하지 않음

            var canvasObject = new GameObject("DeploymentTurnBannerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BattleController의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 101; // 턴 상태 Canvas(100)보다도 위에 표시해 항상 최상단에 보이게 함

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            var groupObject = new GameObject("BannerGroup", typeof(RectTransform), typeof(CanvasGroup)); // 알파 제어용 그룹 오브젝트 생성
            groupObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식으로 연결
            var groupRect = groupObject.GetComponent<RectTransform>(); // 그룹 RectTransform 확보
            groupRect.anchorMin = new Vector2(0.5f, 0.5f); // 화면 정중앙 앵커
            groupRect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 정중앙 앵커
            groupRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗
            groupRect.anchoredPosition = Vector2.zero; // 정중앙 위치
            groupRect.sizeDelta = new Vector2(600f, 120f); // 문구를 담을 충분한 크기
            _canvasGroup = groupObject.GetComponent<CanvasGroup>(); // 페이드용 CanvasGroup 확보
            _canvasGroup.alpha = 0f; // 평소에는 완전히 투명하게 시작
            _canvasGroup.blocksRaycasts = false; // 배너가 보드·UI 클릭을 가로채지 않도록 설정
            _canvasGroup.interactable = false; // 클릭 불가능한 순수 표시용

            _label = CreateText("DeploymentTurnLabel", groupRect, 48, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // "배치 턴" 문구 텍스트 생성
            Stretch(_label.rectTransform, 0f, 0f, 0f, 0f); // 그룹 영역 전체 사용
            AddOutline(_label.gameObject, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f)); // 밝은 배경에서도 잘 보이도록 그림자 외곽선 추가
            _label.text = "배치 턴"; // 고정 문구
            _label.gameObject.SetActive(false); // 처음에는 숨긴 상태로 시작
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment, Color color) // 공통 런타임 Text 생성 보조 메서드
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI Text GameObject 생성
            textObject.transform.SetParent(parent, false); // 부모에 연결
            var text = textObject.GetComponent<Text>(); // Text 확보
            text.font = GetRuntimeFont(); // 한글 시스템 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 정렬 적용
            text.color = color; // 글자색 적용
            text.raycastTarget = false; // 텍스트는 입력을 받지 않음
            return text; // 구성된 Text 반환
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance) // Image/Text에 간단한 외곽선 효과를 추가하는 보조 메서드
        {
            var outline = target.AddComponent<Outline>(); // Outline 추가
            outline.effectColor = color; // 외곽선 색 적용
            outline.effectDistance = distance; // 외곽선 두께 적용
            outline.useGraphicAlpha = true; // 원본 알파 반영
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top) // 부모 영역 안쪽으로 Stretch하는 보조 메서드
        {
            rect.anchorMin = Vector2.zero; // 좌하단 앵커
            rect.anchorMax = Vector2.one; // 우상단 앵커
            rect.offsetMin = new Vector2(left, bottom); // 좌·하단 여백 적용
            rect.offsetMax = new Vector2(-right, -top); // 우·상단 여백 적용
        }

        private static Font GetRuntimeFont() // 한글 텍스트를 표시할 런타임 폰트를 만드는 메서드
        {
            if (_runtimeFont != null) return _runtimeFont; // 이미 생성됐으면 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 한글 시스템 폰트 우선 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // 컴포넌트가 파괴될 때 이벤트 구독을 정리하는 메서드
        {
            if (_turnManager != null) // 턴 매니저가 연결돼 있으면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 턴 전환 이벤트 구독 해제
            }
        }
    }
}
