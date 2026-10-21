using UnityEngine; // 안내 화면 객체
using UnityEngine.UI; // Canvas·텍스트·입력 차단
using ProjectEta.UI; // 기존 UI 이미지 스킨
using ProjectEta.Settings; // 실제 일시정지 키 안내

namespace ProjectEta.Environment // 방 조작 안내 영역
{ // 영역 시작
    public sealed class RoomWalkingUI : MonoBehaviour // 의자 접근 안내와 UI 클릭 차단
    { // 타입 시작
        private RoomWalkingController _walker; // 이동 상태 참조
        private GameObject _root; // 생성한 안내 화면
        private Text _text; // 조작 안내 글자
        private string _previousText; // 중복 글자 변경 방지
        private Font _runtimeFont; // 한글 안내 글꼴
        private bool _ownsFont; // 생성 글꼴 정리 책임
        public void Configure(RoomWalkingController walker) // 이동 안내 연결
        { // 메서드 시작
            _walker = walker; // 이동기 저장
            if (_root != null) return; // 중복 화면 생성 차단
            _root = new GameObject("RoomWalkingCanvas_Day100", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 이동 안내 Canvas 생성
            _root.transform.SetParent(transform, false); // 카메라 호스트와 수명 연결
            Canvas canvas = _root.GetComponent<Canvas>(); // 화면 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 이동 중 화면 고정
            canvas.sortingOrder = 500; // 게임 UI 위·일시정지 아래 배치
            CanvasScaler scaler = _root.GetComponent<CanvasScaler>(); // 해상도 스케일 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 대응
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형
            var blocker = new GameObject("WalkingInputBlocker", typeof(RectTransform), typeof(Image)); // 게임 UI 클릭 차단막
            blocker.transform.SetParent(_root.transform, false); // 이동 화면에 연결
            RectTransform full = blocker.GetComponent<RectTransform>(); // 전체 화면 영역
            full.anchorMin = Vector2.zero; // 좌측 하단 기준
            full.anchorMax = Vector2.one; // 우측 상단 기준
            full.offsetMin = Vector2.zero; // 하단 여백 제거
            full.offsetMax = Vector2.zero; // 상단 여백 제거
            blocker.GetComponent<Image>().color = Color.clear; // 방을 가리지 않는 투명 차단막
            var panel = new GameObject("WalkingHint", typeof(RectTransform), typeof(Image)); // 하단 안내 배경
            panel.transform.SetParent(_root.transform, false); // 이동 화면에 연결
            RectTransform rect = panel.GetComponent<RectTransform>(); // 안내 위치 설정
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f); // 하단 중앙 정렬
            rect.pivot = new Vector2(0.5f, 0f); // 하단 기준 높이
            rect.anchoredPosition = new Vector2(0f, 24f); // 화면 아래 간격
            rect.sizeDelta = new Vector2(700f, 84f); // 두 줄 안내 공간
            panel.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.055f, 0.92f); // 어두운 청록 배경
            Day98UiSkin.TryApplyDarkPanel(panel.GetComponent<Image>()); // 기존 금속 패널 이미지 재사용
            var label = new GameObject("WalkingHintText", typeof(RectTransform), typeof(Text)); // 안내 글자 생성
            label.transform.SetParent(panel.transform, false); // 안내 배경에 연결
            RectTransform labelRect = label.GetComponent<RectTransform>(); // 글자 영역 조회
            labelRect.anchorMin = Vector2.zero; // 전체 배경 사용
            labelRect.anchorMax = Vector2.one; // 전체 배경 사용
            labelRect.offsetMin = new Vector2(12f, 6f); // 글자 하단 여백
            labelRect.offsetMax = new Vector2(-12f, -6f); // 글자 상단 여백
            _text = label.GetComponent<Text>(); // 안내 글자 저장
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 22); // 기존 UI와 동일한 한글 글꼴 생성
            _ownsFont = _runtimeFont != null; // 생성 글꼴 정리 소유 기록
            _text.font = _runtimeFont != null ? _runtimeFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 한글 글꼴과 기본 대체 연결
            _text.fontSize = 22; // 조작 안내 크기
            _text.alignment = TextAnchor.MiddleCenter; // 두 줄 가운데 정렬
            _text.color = new Color(1f, 0.86f, 0.45f); // 금빛 안내색
            _text.raycastTarget = false; // 글자 클릭 판정 제외
            _root.SetActive(false); // 처음에는 이동 안내 숨김
        } // 메서드 종료
        private void LateUpdate() // 이동 안내 갱신
        { // 메서드 시작
            if (_root == null || _walker == null) return; // 미연결 화면 제외
            bool walking = _walker.IsWalking; // 현재 이동 상태 조회
            if (_root.activeSelf != walking) _root.SetActive(walking); // 이동 중에만 클릭 차단과 안내 표시
            if (!walking) return; // 앉은 상태 글자 변경 생략
            string pauseKey = GameInputBindingService.GetDisplayName(GameInputAction.Pause); // 저장된 일시정지 키 조회
            string text = $"WASD 이동  ·  마우스 둘러보기  ·  {pauseKey} 일시정지\n" + (_walker.CanSit ? "F : 의자에 앉기" : "의자 가까이에서 F키로 앉을 수 있습니다."); // 거리별 앉기 안내
            if (_previousText == text) return; // 같은 안내 재배치 생략
            _text.text = text; // 새 조작 안내 적용
            _previousText = text; // 표시 글자 저장
        } // 메서드 종료
        private void OnDestroy() // 안내 화면 정리
        { // 메서드 시작
            if (_root != null) Destroy(_root); // 생성 화면 제거
            if (_ownsFont && _runtimeFont != null) Destroy(_runtimeFont); // 생성 글꼴 수명 정리
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
