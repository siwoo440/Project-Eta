using UnityEngine; // 실제 Canvas 영역과 배치

namespace ProjectEta.UI // 화면 크기 보정
{ // 영역 시작
    [DisallowMultipleComponent] // 중복 배율 적용 방지
    public sealed class UiPanelViewportFit : MonoBehaviour // 설정 배율보다 화면 접근성 우선
    { // 타입 시작
        private RectTransform _panel; // 맞출 패널
        private Vector2 _contentSize; // 여러 패널을 묶은 논리 영역
        public static void Attach(RectTransform panel, Vector2 contentSize = default) // 기존 생성 코드의 자동 연결
        { // 메서드 시작
            if (panel == null) return; // 생성 전 패널 제외
            UiPanelViewportFit fit = panel.GetComponent<UiPanelViewportFit>(); // 기존 맞춤 조회
            if (fit == null) fit = panel.gameObject.AddComponent<UiPanelViewportFit>(); // 원본 크기를 유지하는 맞춤 추가
            fit._contentSize = contentSize; // 패널 묶음 또는 자신의 크기 사용
            fit.Fit(); // 첫 표시에도 즉시 배치
        } // 메서드 종료
        private void OnEnable() // 첫 표시 프레임의 실제 영역 반영
        { // 메서드 시작
            Canvas.willRenderCanvases += Fit; // Canvas 배율 갱신 후 표시 전 맞춤
            Fit(); // 이미 확정된 화면에는 즉시 적용
        } // 메서드 종료
        private void OnDisable() // 숨은 패널의 렌더 구독 정리
        { // 메서드 시작
            Canvas.willRenderCanvases -= Fit; // 다음 화면 참조 유지 방지
        } // 메서드 종료
        private void LateUpdate() // 해상도 변경 뒤 입력 영역도 보정
        { // 메서드 시작
            Fit(); // 다음 렌더 이전 배치 갱신
        } // 메서드 종료
        private void Fit() // 해상도·배율 변경 후 실제 영역 반영
        { // 메서드 시작
            if (_panel == null) _panel = GetComponent<RectTransform>(); // 자신의 논리 크기 조회
            RectTransform parent = _panel != null ? _panel.parent as RectTransform : null; // 전체 화면 기준
            if (parent == null || parent.rect.width <= 0f || parent.rect.height <= 0f) return; // 초기화 전 영역 제외
            float width = Mathf.Max(1f, (_contentSize.x > 0f ? _contentSize.x : _panel.rect.width)); // 0으로 나누기 방지
            float height = Mathf.Max(1f, (_contentSize.y > 0f ? _contentSize.y : _panel.rect.height)); // 실제 패널 높이
            float scale = Mathf.Min(1f, Mathf.Max(1f, parent.rect.width - 32f) / width, Mathf.Max(1f, parent.rect.height - 32f) / height); // 화면 여백을 제외한 균일 맞춤
            if (!Mathf.Approximately(_panel.localScale.x, scale)) _panel.localScale = Vector3.one * scale; // 글자·버튼을 같은 비율로 배치
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
