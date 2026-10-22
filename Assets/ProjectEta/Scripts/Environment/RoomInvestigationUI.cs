using UnityEngine; // 실제 조사 화면
using UnityEngine.UI; // 기존 UGUI
using UnityEngine.EventSystems; // 키보드 포커스
using UnityEngine.InputSystem.UI; // 기존 UI 입력 모듈
using ProjectEta.UI; // 기존 이미지 스킨·화면 맞춤
using ProjectEta.Settings; // 저장된 UI 배율

namespace ProjectEta.Environment // 방 조사 표시
{ // 영역 시작
    public sealed class RoomInvestigationUI : MonoBehaviour // 조사 내용과 닫기 버튼
    { // 타입 시작
        private RoomInvestigationController _owner; // 입력 소유자
        private GameObject _root; // 화면 수명
        private Text _title; // 대상 제목
        private Text _description; // 실제 조사 문구
        private Button _close; // 실제 닫기 버튼
        private Font _font; // 생성 한글 글꼴
        private EventSystem _createdEvents; // 직접 만든 입력 시스템
        public void Configure(RoomInvestigationController owner) // 자동 담당자 연결
        { // 메서드 시작
            _owner = owner; // 실제 입력 연결
        } // 메서드 종료
        public void Show(RoomInvestigationDefinition definition) // 실제 조사 내용 표시
        { // 메서드 시작
            EnsureBuilt(); // 최초 조사에서 화면 생성
            _title.text = definition.Title; // 물건 이름
            _description.text = definition.Description; // 임시 조사 설명
            _root.SetActive(true); // 실제 모달 표시
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_close.gameObject); // Pause 복귀용 명시적 선택
        } // 메서드 종료
        public void Hide() // 조사 모달 숨김
        { // 메서드 시작
            if (_root != null) _root.SetActive(false); // 중복 숨김 허용
        } // 메서드 종료
        private void EnsureBuilt() // Inspector 없는 실제 UI 생성
        { // 메서드 시작
            if (_root != null) return; // 중복 생성 차단
            if (Object.FindFirstObjectByType<EventSystem>() == null) // 기존 EventSystem 보존
            { // 조건 시작
                var events = new GameObject("RoomInvestigationEvents_Day104", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 직접 실행 환경 입력 준비
                events.transform.SetParent(transform, false); // 생성 소유와 수명 연결
                _createdEvents = events.GetComponent<EventSystem>(); // 정리 책임 보관
            } // 조건 종료
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 기존 UI 계열 한글 글꼴
            _root = new GameObject("RoomInvestigationCanvas_Day104", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 조사 모달 Canvas
            _root.transform.SetParent(transform, false); // 카메라 호스트 수명과 연결
            Canvas canvas = _root.GetComponent<Canvas>(); // 실제 Canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 이동 중 고정 화면
            canvas.sortingOrder = 1000; // 걷기 안내 위·Pause 아래
            CanvasScaler scaler = _root.GetComponent<CanvasScaler>(); // 실제 해상도 대응
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기존 UI 기준
            scaler.matchWidthOrHeight = 0.5f; // 가로세로 균형
            Image backdrop = Image("Backdrop", _root.transform, Vector2.zero, Vector2.zero); // 전체 입력 차단막
            RectTransform full = backdrop.rectTransform; // 화면 크기 기준
            full.anchorMin = Vector2.zero; // 좌측 하단
            full.anchorMax = Vector2.one; // 우측 상단
            full.offsetMin = Vector2.zero; // 여백 없음
            full.offsetMax = Vector2.zero; // 여백 없음
            backdrop.color = new Color(0f, 0f, 0f, 0.72f); // 주변을 읽기 쉽게 낮춘 배경
            Image panel = Image("InvestigationPanel", _root.transform, Vector2.zero, new Vector2(920f, 560f)); // 조사 내용 패널
            Day98UiSkin.TryApplyDarkPanel(panel); // 기존 금속 프레임 재사용
            UiPanelViewportFit.Attach(panel.rectTransform); // 큰 UI 배율·화면비 대응
            Text section = Text("Section", panel.transform, new Vector2(0f, 208f), new Vector2(760f, 36f), 20); // 조사 구분
            section.text = "방 조사"; // 구현 용어 없는 구분 문구
            section.color = new Color(1f, 0.82f, 0.3f); // 금빛 구분색
            _title = Text("Title", panel.transform, new Vector2(0f, 140f), new Vector2(760f, 64f), 38); // 실제 대상 제목
            _title.resizeTextForBestFit = true; // 긴 이름 보정
            _title.resizeTextMinSize = 24; // 제목 최소 크기
            _title.resizeTextMaxSize = 38; // 제목 상한
            _description = Text("Description", panel.transform, new Vector2(0f, 0f), new Vector2(740f, 180f), 26); // 조사 설명 영역
            _description.alignment = TextAnchor.MiddleLeft; // 문장 읽기 정렬
            _description.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 문장 줄바꿈
            _description.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 글자 차단
            Image closeImage = Image("CloseInvestigation", panel.transform, new Vector2(0f, -170f), new Vector2(320f, 72f)); // 실제 닫기 버튼
            _close = closeImage.gameObject.AddComponent<StageOverlayButton>(); // 기본 Submit의 중복 확정 제외
            Day98UiSkin.TryApplyButton(_close, Day98UiSkin.ButtonBaseResourcePath); // 기존 버튼 이미지
            _close.onClick.AddListener(() => _owner.TryClose()); // 현재 모달 소유 검사를 거치는 클릭
            Text buttonText = Text("CloseLabel", closeImage.transform, Vector2.zero, new Vector2(290f, 58f), 24); // 버튼 글자 영역
            buttonText.text = "닫기  F / Enter"; // 실제 닫기 조작 안내
            Text hint = Text("CloseHint", panel.transform, new Vector2(0f, -238f), new Vector2(760f, 32f), 18); // 보조 닫기 안내
            hint.text = "Backspace로 닫기 · 일시정지 키로 메뉴 열기"; // 기존 Pause 바인딩과 충돌 없는 안내
            GameSettingsService.ReapplyUiScale(); // 저장된 UI 배율 연결
            _root.SetActive(false); // 실제 내용 확정 전 숨김
        } // 메서드 종료
        private Image Image(string name, Transform parent, Vector2 position, Vector2 size) // 실제 배경 생성
        { // 메서드 시작
            var host = new GameObject(name, typeof(RectTransform), typeof(Image)); // 이미지 객체
            host.transform.SetParent(parent, false); // 화면 소유 연결
            RectTransform rect = host.GetComponent<RectTransform>(); // 실제 배치 영역
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준
            rect.anchoredPosition = position; // 실제 위치
            rect.sizeDelta = size; // 실제 크기
            Image image = host.GetComponent<Image>(); // 실제 이미지
            image.color = new Color(0.04f, 0.06f, 0.075f, 0.98f); // 기존 UI 배경 계열
            return image; // 생성 결과
        } // 메서드 종료
        private Text Text(string name, Transform parent, Vector2 position, Vector2 size, int fontSize) // 실제 한글 안내 생성
        { // 메서드 시작
            var host = new GameObject(name, typeof(RectTransform), typeof(Text)); // 글자 객체
            host.transform.SetParent(parent, false); // 화면 소유 연결
            RectTransform rect = host.GetComponent<RectTransform>(); // 글자 영역
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준
            rect.anchoredPosition = position; // 실제 위치
            rect.sizeDelta = size; // 실제 크기
            Text text = host.GetComponent<Text>(); // 실제 글자
            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 한글 글꼴과 기본 대체
            text.fontSize = fontSize; // 역할별 크기
            text.color = Color.white; // 높은 대비
            text.alignment = TextAnchor.MiddleCenter; // 기본 중앙 정렬
            text.raycastTarget = false; // 글자의 버튼 입력 간섭 제외
            return text; // 생성 결과
        } // 메서드 종료
        private void OnDisable() // 화면 담당자 비활성화 정리
        { // 메서드 시작
            if (_owner != null) _owner.ForceClose(); // 보이지 않는 조사의 입력 소유 해제
        } // 메서드 종료
        private void OnDestroy() // 화면과 생성 자원 정리
        { // 메서드 시작
            if (_root != null) Destroy(_root); // 별도 Canvas 정리
            if (_font != null) Destroy(_font); // 생성 한글 글꼴 정리
            if (_createdEvents != null) Destroy(_createdEvents.gameObject); // 직접 만든 EventSystem만 정리
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
