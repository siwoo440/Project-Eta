using UnityEngine; // MonoBehaviour, GameObject, RectTransform, Font, Color 등을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, CanvasScaler, GraphicRaycaster, Image, Text, Shadow를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Round // 라운드 구성·증원 관련 타입을 모아두는 네임스페이스
{
    public sealed class RoundSummaryUI : MonoBehaviour // 상단 중앙 TurnStatusUI 바로 아래에 Round·Turn·현재 적 수를 표시하는 런타임 UI
    {
        private const float RefreshInterval = 0.12f; // 적 처치처럼 별도 라운드 이벤트가 없는 변화도 빠르게 반영할 폴링 간격

        private RoundRuntimeController _roundController; // 현재 라운드 데이터와 적 수를 제공하는 런타임 관리자
        private TurnManager _turnManager; // 현재 턴 번호를 제공하는 턴 매니저
        private RunState _runState; // 현재 라운드 번호와 보드를 제공하는 런 상태
        private Canvas _canvas; // 라운드 정보 전용 Screen Space Overlay Canvas
        private RectTransform _panelRect; // 상단 중앙 두 번째 줄 배경 패널
        private Image _panelImage; // 어두운 반투명 배경 이미지
        private Text _label; // Round : N / Turn : N / 현재 적 : N 문구
        private float _nextRefreshTime; // 다음 자동 갱신 시간
        private string _lastDisplayText = string.Empty; // 같은 문자열을 매 프레임 다시 설정하지 않기 위한 캐시

        public Canvas SummaryCanvas => _canvas; // 테스트·외부 코드에서 생성된 Canvas를 확인하기 위한 프로퍼티
        public RectTransform PanelRect => _panelRect; // 상단 중앙 패널 위치를 확인하기 위한 프로퍼티
        public string DisplayText => _label != null ? _label.text : string.Empty; // 현재 화면에 표시 중인 문구

        public void Bind(RoundRuntimeController roundController, TurnManager turnManager, RunState runState) // 라운드·턴·런 상태를 UI에 연결하는 메서드
        {
            Unsubscribe(); // 재바인딩 시 기존 이벤트 구독부터 안전하게 해제

            _roundController = roundController; // 현재 라운드 관리자 저장
            _turnManager = turnManager; // 현재 턴 매니저 저장
            _runState = runState; // 현재 런 상태 저장

            EnsureCanvas(); // 상단 중앙 라운드 정보 Canvas가 없으면 생성

            if (_roundController != null) _roundController.RoundStateChanged += HandleRoundStateChanged; // 증원·라운드 상태 변화 이벤트 구독
            if (_turnManager != null) _turnManager.TurnChanged += HandleTurnChanged; // 턴 전환 이벤트 구독

            Refresh(); // 연결 즉시 현재 값을 표시
        }

        private void Update() // 적 처치로 현재 적 수가 바뀌는 경우까지 자동 반영하는 가벼운 폴링
        {
            if (_label == null || _runState == null) return; // 아직 연결되지 않았으면 처리하지 않음
            if (Time.unscaledTime < _nextRefreshTime) return; // 갱신 간격 전이면 기존 표시 유지

            _nextRefreshTime = Time.unscaledTime + RefreshInterval; // 다음 자동 갱신 시간 예약
            Refresh(); // 현재 라운드·턴·적 수를 다시 계산
        }

        private void EnsureCanvas() // 현재 TurnStatusUI 바로 아래에 라운드 정보 UI를 한 번만 만드는 메서드
        {
            if (_canvas != null) return; // 이미 만들어졌으면 중복 생성하지 않음

            var canvasObject = new GameObject("RoundSummaryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 라운드 정보 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // RoundRuntimeController 오브젝트의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // 생성한 Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 좌표 기준으로 표시
            _canvas.sortingOrder = 99; // TurnStatusUI의 100 바로 아래 정렬 순서 사용

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 비율 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 TurnStatusUI와 동일한 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로·세로 비율을 함께 고려
            scaler.matchWidthOrHeight = 0.5f; // 가로와 세로 동일 비중

            var panelObject = new GameObject("RoundSummaryPanel", typeof(RectTransform), typeof(Image)); // 상단 정보 배경 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식으로 연결
            _panelRect = panelObject.GetComponent<RectTransform>(); // RectTransform 참조 확보
            _panelRect.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
            _panelRect.anchorMax = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
            _panelRect.pivot = new Vector2(0.5f, 1f); // 패널 윗중앙을 기준으로 배치
            _panelRect.anchoredPosition = new Vector2(0f, -96f); // TurnStatusUI(-24, 높이64)의 바로 아래 8픽셀 간격으로 배치
            _panelRect.sizeDelta = new Vector2(620f, 42f); // 기존 상단 턴 UI 폭 620과 맞춘 얇은 보조 정보 패널

            _panelImage = panelObject.GetComponent<Image>(); // 배경 Image 참조 확보
            _panelImage.color = new Color(0.035f, 0.045f, 0.065f, 0.88f); // 메인 턴 색상을 방해하지 않는 짙은 반투명 배경
            _panelImage.raycastTarget = false; // 보드 클릭을 가로채지 않도록 레이캐스트 비활성화

            var textObject = new GameObject("RoundSummaryText", typeof(RectTransform), typeof(Text), typeof(Shadow)); // 중앙 정보 텍스트 생성
            textObject.transform.SetParent(panelObject.transform, false); // 패널 자식으로 연결
            var textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 확보
            textRect.anchorMin = Vector2.zero; // 패널 전체 영역 사용
            textRect.anchorMax = Vector2.one; // 패널 전체 영역 사용
            textRect.offsetMin = new Vector2(12f, 0f); // 좌측 내부 여백
            textRect.offsetMax = new Vector2(-12f, 0f); // 우측 내부 여백

            _label = textObject.GetComponent<Text>(); // 텍스트 컴포넌트 확보
            _label.font = CreateRuntimeFont(); // 한글을 지원하는 시스템 폰트 우선 사용
            _label.fontSize = 19; // 메인 TurnStatusUI 24보다 작은 보조 정보 크기
            _label.fontStyle = FontStyle.Bold; // 작은 크기에서도 읽기 쉽게 굵게 표시
            _label.alignment = TextAnchor.MiddleCenter; // 화면 중앙에 한 줄로 정렬
            _label.color = new Color(0.94f, 0.96f, 1f, 1f); // 어두운 배경 위 밝은 청백색 텍스트
            _label.raycastTarget = false; // 텍스트도 보드 입력을 막지 않음

            var shadow = textObject.GetComponent<Shadow>(); // 글자 그림자 확보
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f); // 배경과 분리되도록 검은 그림자 적용
            shadow.effectDistance = new Vector2(1f, -1f); // 짧은 그림자 거리
            shadow.useGraphicAlpha = true; // 원본 알파값 사용
        }

        private void HandleRoundStateChanged() // 증원·초기 적 구성 변화 이벤트 처리
        {
            Refresh(); // 현재 적 수와 라운드 정보를 즉시 갱신
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 전환 이벤트 처리
        {
            Refresh(); // 전달값 대신 실제 TurnManager 값을 다시 읽어 한 줄 전체를 갱신
        }

        private void Refresh() // 현재 런 상태를 한 줄 UI 문자열로 변환하는 메서드
        {
            if (_label == null || _runState == null || _turnManager == null) return; // 필수 상태가 없으면 표시하지 않음

            int roundNumber = _runState.CurrentRound; // 현재 런의 실제 라운드 번호
            int currentTurn = _turnManager.TurnNumber; // 현재 일반 턴 번호
            int turnLimit = _roundController != null ? _roundController.TurnLimit : 30; // RoundDefinition 기반 턴 제한
            int enemyCount = _roundController != null ? _roundController.CurrentEnemyCount : RoundRuntimeController.CountCurrentEnemies(_runState.Board); // 현재 보드의 실제 적 수

            string displayText = BuildDisplayText(roundNumber, currentTurn, turnLimit, enemyCount); // 사용자 요청 형식으로 최종 문자열 생성

            if (_lastDisplayText == displayText) return; // 이전 프레임과 같으면 불필요한 Text 재설정 방지

            _lastDisplayText = displayText; // 최신 문자열 캐시
            _label.text = displayText; // 실제 화면에 표시
        }

        public static string BuildDisplayText(int roundNumber, int currentTurn, int turnLimit, int enemyCount) // 테스트와 런타임이 함께 사용하는 최종 UI 문구 생성 함수
        {
            return $"Round : {Mathf.Max(0, roundNumber)}    Turn : {Mathf.Max(0, currentTurn)} / {Mathf.Max(1, turnLimit)}    현재 적 : {Mathf.Max(0, enemyCount)}"; // 요청한 간격·표기 형식 그대로 반환
        }

        private static Font CreateRuntimeFont() // Windows 한글을 우선 지원하면서 다른 환경에서도 동작하도록 폰트를 찾는 메서드
        {
            var systemFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 20); // 운영체제 한글·기본 폰트 후보로 동적 폰트 생성
            if (systemFont != null) return systemFont; // 시스템 폰트를 만들었으면 그대로 사용

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 시스템 폰트가 없으면 Unity 기본 런타임 폰트 사용
        }

        private void Unsubscribe() // 기존 이벤트 구독을 안전하게 해제하는 공통 메서드
        {
            if (_roundController != null) _roundController.RoundStateChanged -= HandleRoundStateChanged; // 라운드 이벤트 해제
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 해제
        }

        private void OnDestroy() // UI 호스트가 제거될 때 이벤트 구독 정리
        {
            Unsubscribe(); // 남은 이벤트 참조를 제거
        }
    }
}
