using UnityEngine; // MonoBehaviour, GameObject, Font, Color, Vector2를 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, CanvasScaler, Image, Text, Shadow를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public sealed class BossPhaseStatusUI : MonoBehaviour // 상단 라운드 정보 아래에 Phase 2와 현재 예고 패턴을 표시하는 개발용 런타임 UI
    {
        private Canvas _canvas; // 보스 상태 전용 Screen Space Overlay Canvas
        private Text _label; // 실제 보스 페이즈·예고 문구

        public string DisplayText => _label != null ? _label.text : string.Empty; // 현재 표시 문자열을 읽는 프로퍼티

        public void SetState(BossPhase phase, string pendingPatternName, bool reinforcementTriggered) // 현재 페이즈와 예고 상태를 화면에 적용하는 메서드
        {
            EnsureCanvas(); // 최초 호출이면 UI 생성

            if (_canvas == null || _label == null) return; // UI 생성에 실패했으면 종료
            _canvas.enabled = phase == BossPhase.Phase2; // Phase 2일 때만 별도 보스 상태 줄을 표시
            if (!_canvas.enabled) return; // Phase 1이면 문자열 갱신 불필요

            _label.text = BuildDisplayText(phase, pendingPatternName, reinforcementTriggered); // 현재 상태를 한 줄 문구로 변환해 표시
        }

        public static string BuildDisplayText(BossPhase phase, string pendingPatternName, bool reinforcementTriggered) // 테스트와 런타임이 함께 사용하는 최종 문구 생성 함수
        {
            if (phase != BossPhase.Phase2) return "BOSS PHASE 1"; // Phase 1 기본 문구

            string telegraph = string.IsNullOrWhiteSpace(pendingPatternName) ? "다음 패턴 준비" : $"예고 : {pendingPatternName}"; // 예고가 있으면 패턴 이름 표시
            string reinforcement = reinforcementTriggered ? "    증원 발생" : string.Empty; // Phase 전환 직후 한 번만 증원 안내 추가
            return $"BOSS PHASE 2    {telegraph}{reinforcement}"; // 상단 한 줄 최종 문구 반환
        }

        private void EnsureCanvas() // 기존 상단 UI와 겹치지 않는 보스 상태 줄을 런타임에 생성하는 메서드
        {
            if (_canvas != null) return; // 이미 생성됐으면 중복 생성 금지

            var canvasObject = new GameObject("BossPhaseStatusCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 보스 상태 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 현재 컨트롤러 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 좌표 기준 표시
            _canvas.sortingOrder = 98; // TurnStatusUI 100, RoundSummaryUI 99 아래에 배치
            _canvas.enabled = false; // Phase 2 전까지 숨김

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 따라 자동 스케일
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 상단 UI와 동일 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로세로 비율 함께 고려
            scaler.matchWidthOrHeight = 0.5f; // 동일 비중 사용

            var panelObject = new GameObject("BossPhaseStatusPanel", typeof(RectTransform), typeof(Image)); // 보스 상태 배경 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결
            var panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 확보
            panelRect.anchorMin = new Vector2(0.5f, 1f); // 상단 중앙 앵커
            panelRect.anchorMax = new Vector2(0.5f, 1f); // 상단 중앙 앵커
            panelRect.pivot = new Vector2(0.5f, 1f); // 윗중앙 기준 배치
            panelRect.anchoredPosition = new Vector2(0f, -208f); // 새 BossHealthUI(-146, 높이54) 아래 약 8픽셀 간격
            panelRect.sizeDelta = new Vector2(620f, 42f); // 기존 상단 UI와 같은 폭

            var image = panelObject.GetComponent<Image>(); // 패널 배경 Image 확보
            image.color = new Color(0.22f, 0.025f, 0.02f, 0.92f); // 보스 위험 상태를 강조하는 짙은 적색 배경
            image.raycastTarget = false; // 보드 클릭을 가로채지 않음

            var textObject = new GameObject("BossPhaseStatusText", typeof(RectTransform), typeof(Text), typeof(Shadow)); // 상태 텍스트 생성
            textObject.transform.SetParent(panelObject.transform, false); // 패널 자식 연결
            var textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 확보
            textRect.anchorMin = Vector2.zero; // 패널 전체 사용
            textRect.anchorMax = Vector2.one; // 패널 전체 사용
            textRect.offsetMin = new Vector2(12f, 0f); // 좌측 여백
            textRect.offsetMax = new Vector2(-12f, 0f); // 우측 여백

            _label = textObject.GetComponent<Text>(); // 실제 Text 컴포넌트 확보
            _label.font = CreateRuntimeFont(); // 한글 지원 시스템 폰트 우선 사용
            _label.fontSize = 19; // RoundSummaryUI와 같은 보조 정보 크기
            _label.fontStyle = FontStyle.Bold; // 위험 상태 가독성을 위해 굵게 표시
            _label.alignment = TextAnchor.MiddleCenter; // 중앙 한 줄 정렬
            _label.color = new Color(1f, 0.88f, 0.72f, 1f); // 어두운 적색 위 밝은 주황빛 글자
            _label.raycastTarget = false; // 보드 입력 차단 금지

            var shadow = textObject.GetComponent<Shadow>(); // 글자 그림자 확보
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f); // 명암 대비 강화
            shadow.effectDistance = new Vector2(1f, -1f); // 짧은 그림자 거리
            shadow.useGraphicAlpha = true; // 원본 알파값 사용
        }

        private static Font CreateRuntimeFont() // 기존 RoundSummaryUI와 같은 폰트 폴백 규칙을 사용하는 메서드
        {
            var systemFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 20); // 한국어 우선 시스템 폰트 탐색
            if (systemFont != null) return systemFont; // 찾으면 그대로 사용
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 마지막으로 Unity 기본 런타임 폰트 사용
        }
    }
}
