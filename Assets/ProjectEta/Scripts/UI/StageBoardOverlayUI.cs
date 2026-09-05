using System.Collections.Generic; // 옵션 목록 사용
using UnityEngine; // 보드 위 3D 오버레이 생성
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.UI; // World Space Canvas UI 사용
using ProjectEta.Board; // BoardView 사용

namespace ProjectEta.UI
{
    public sealed class StageBoardOverlayUI : MonoBehaviour
    {
        private const int MaxOptionButtons = 8; // 한 화면 최대 선택지 수
        private const float CanvasPixelWidth = 1400f; // 월드 UI 기준 폭
        private const float CanvasPixelHeight = 980f; // 월드 UI 기준 높이
        private const float CanvasScalePerTile = 0.00580f; // 1번 카메라에서 더 크게 읽히는 World Canvas 크기
        private const float CanvasHeightOffsetTiles = 0.03f; // 돗자리와 겹치지 않는 최소 UI 높이
        private const float CanvasDepthOffsetTiles = -0.55f; // UI 전체를 플레이어 쪽으로 내려 가까이 배치
        private const float TitlePlaqueWidth = 460f; // 확대된 상단 제목 팻말 폭
        private const float TitlePlaqueHeight = 100f; // 확대된 상단 제목 팻말 높이
        private const float SubtitlePlaqueWidth = 720f; // 확대된 상태 줄 팻말 폭
        private const float SubtitlePlaqueHeight = 54f; // 확대된 상태 줄 팻말 높이
        private const float NormalCardWidth = 390f; // 확대된 기본 선택 카드 폭
        private const float NormalCardHeight = 170f; // 확대된 기본 선택 카드 높이
        private const float BottomCardWidth = 470f; // 확대된 하단 전용 카드 폭
        private const float BottomCardHeight = 148f; // 확대된 하단 전용 카드 높이
        private const float TitleY = 330f; // 확대 UI 상단 제목 팻말 위치
        private const float SubtitleY = 258f; // 확대 UI 상태 줄 팻말 위치
        private const float HoverDescriptionY = -365f; // 마우스 오버 설명 하단 위치
        private const float HoverDescriptionWidth = 980f; // 마우스 오버 설명 표시 폭
        private const float HoverDescriptionHeight = 112f; // 마우스 오버 설명 표시 높이

        private sealed class OptionView
        {
            public Button Button; // 실제 입력 버튼
            public RectTransform RootRect; // 카드 루트 RectTransform
            public Image FrameImage; // 카드 외곽 프레임
            public Image PaperImage; // 카드 내부 종이
            public Text TitleText; // 카드 제목
            public Text DescriptionText; // 카드 설명
            public StageOverlayHoverRelay HoverRelay; // 마우스 오버 설명 연결기
        }

        private readonly List<OptionView> _optionViews = new List<OptionView>(); // 카드형 선택지 뷰 목록
        private BoardView _boardView; // 오버레이 기준 보드
        private GameObject _overlayRoot; // 전체 판 위 오버레이 루트
        private GameObject _matPlane; // 돗자리 본체
        private Renderer _matRenderer; // 돗자리 색상 렌더러
        private readonly List<Renderer> _trimRenderers = new List<Renderer>(); // 돗자리 장식 렌더러
        private Canvas _canvas; // 판 위 World Space Canvas
        private RectTransform _canvasRect; // Canvas 크기 제어
        private Image _titlePlaqueImage; // 제목 팻말 배경
        private Text _titleText; // 페이지 제목
        private Image _subtitlePlaqueImage; // 상태 줄 팻말 배경
        private Text _subtitleText; // 페이지 상태·설명
        private GameObject _hoverDescriptionRoot; // 하단 마우스 오버 설명 루트
        private Text _hoverDescriptionText; // 하단 흰색 마우스 오버 설명
        private GameObject _mapVisualRoot; // 숨길 기존 경로 지도 표시
        private bool _mapVisualWasActive; // 지도 표시 복원 플래그
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private Material _matMaterial; // 돗자리 머티리얼
        private Material _trimMaterial; // 돗자리 장식 머티리얼
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsVisible => _overlayRoot != null && _overlayRoot.activeSelf; // 현재 판 위 UI 표시 여부

        public void Initialize(BoardView boardView)
        {
            _boardView = boardView; // 기준 보드 저장
            EnsureBuilt(); // 돗자리·Canvas 생성
        }

        public void ShowPage(StageOverlayMode mode, string title, string subtitle, IReadOnlyList<StageOverlayOption> options)
        {
            EnsureBuilt(); // UI 생성 보장
            if (_overlayRoot == null) return; // 오버레이 생성 실패 방어

            HideRouteMapVisuals(); // 돗자리 아래 지도 노드·킹 숨김
            ApplyModeVisual(mode); // Shop/Event 돗자리 색상 적용
            _titleText.text = title ?? string.Empty; // 페이지 제목 적용
            _subtitleText.text = subtitle ?? string.Empty; // 페이지 설명 적용
            RebuildOptions(options); // 카드형 선택지 재배치
            ApplyPrimaryCameraPresentation(); // 1번 카메라 전용 고정 UI 위치·각도 적용
            _overlayRoot.SetActive(true); // 판 위 오버레이 표시
        }

        public void Hide()
        {
            HideHoverDescription(); // 남은 마우스 오버 설명 제거
            if (_overlayRoot != null) _overlayRoot.SetActive(false); // 돗자리·UI 숨김
            RestoreRouteMapVisuals(); // 기존 지도 시각 복원
        }

        private void EnsureBuilt()
        {
            if (_overlayRoot != null || _boardView == null) return; // 중복 생성·보드 누락 방어

            EnsureEventSystem(); // UI 포인터 입력 보장
            _overlayRoot = new GameObject("StageBoardOverlay_Day47"); // 판 위 오버레이 루트 생성
            _overlayRoot.transform.SetParent(_boardView.transform, false); // 보드 로컬 좌표계 연결

            BuildMat(); // 돗자리 시각 생성
            BuildCanvas(); // 카드형 World Space UI 생성
            _overlayRoot.SetActive(false); // 기본 숨김 상태
        }

        private void BuildMat()
        {
            float tileSize = Mathf.Max(0.1f, _boardView.TileSize); // 보드 타일 크기 보정
            float matWidth = 7.2f * tileSize; // 인스크립션 느낌에 맞춘 중앙 돗자리 폭
            float matDepth = 5.4f * tileSize; // 인스크립션 느낌에 맞춘 중앙 돗자리 깊이

            _matPlane = GameObject.CreatePrimitive(PrimitiveType.Plane); // 수평 돗자리 면 생성
            _matPlane.name = "StageMat"; // Hierarchy 식별 이름
            _matPlane.transform.SetParent(_overlayRoot.transform, false); // 오버레이 루트 연결
            _matPlane.transform.localPosition = new Vector3(0f, 0.125f, 0.15f * tileSize); // 보드 중앙 약간 위쪽 배치
            _matPlane.transform.localRotation = Quaternion.identity; // 보드와 평행한 정렬 유지
            _matPlane.transform.localScale = new Vector3(matWidth / 10f, 1f, matDepth / 10f); // Unity Plane 10×10 기준 크기 보정
            RemoveCollider(_matPlane); // 보드 레이캐스트 간섭 제거
            _matRenderer = _matPlane.GetComponent<Renderer>(); // 돗자리 렌더러 확보

            _matMaterial = CreateMaterial(new Color(0.26f, 0.12f, 0.05f, 1f)); // 기본 어두운 천 색상
            _trimMaterial = CreateMaterial(new Color(0.72f, 0.49f, 0.15f, 1f)); // 기본 황동 장식 색상
            _matRenderer.sharedMaterial = _matMaterial; // 돗자리 머티리얼 적용

            CreateTrim("Top", new Vector3(0f, 0.147f, matDepth * 0.5f), new Vector3(matWidth * 0.98f, 0.018f, 0.06f * tileSize)); // 위쪽 장식 생성
            CreateTrim("Bottom", new Vector3(0f, 0.147f, -matDepth * 0.5f), new Vector3(matWidth * 0.98f, 0.018f, 0.06f * tileSize)); // 아래쪽 장식 생성
            CreateTrim("Left", new Vector3(-matWidth * 0.5f, 0.147f, 0f), new Vector3(0.06f * tileSize, 0.018f, matDepth * 0.98f)); // 왼쪽 장식 생성
            CreateTrim("Right", new Vector3(matWidth * 0.5f, 0.147f, 0f), new Vector3(0.06f * tileSize, 0.018f, matDepth * 0.98f)); // 오른쪽 장식 생성
            CreateTrim("TitleBar", new Vector3(0f, 0.147f, 1.72f * tileSize), new Vector3(matWidth * 0.54f, 0.022f, 0.07f * tileSize)); // 상단 팻말 아래 장식 생성
        }

        private void CreateTrim(string suffix, Vector3 position, Vector3 scale)
        {
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube); // 돗자리 장식 생성
            trim.name = $"StageMatTrim_{suffix}"; // 장식 식별 이름
            trim.transform.SetParent(_overlayRoot.transform, false); // 오버레이 루트 연결
            trim.transform.localPosition = position; // 장식 위치 적용
            trim.transform.localRotation = Quaternion.identity; // 보드와 평행한 정렬 유지
            trim.transform.localScale = scale; // 장식 크기 적용
            RemoveCollider(trim); // 물리 충돌 제거
            Renderer renderer = trim.GetComponent<Renderer>(); // 장식 렌더러 확보
            renderer.sharedMaterial = _trimMaterial; // 공통 장식 머티리얼 적용
            _trimRenderers.Add(renderer); // 색상 전환 목록 등록
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("StageWorldCanvas_Day47", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); // World Space Canvas 생성
            canvasObject.transform.SetParent(_overlayRoot.transform, false); // 오버레이 루트 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.WorldSpace; // 판 위 월드 UI 모드 적용
            _canvas.worldCamera = Camera.main; // 포인터 레이캐스트 카메라 적용
            _canvas.sortingOrder = 200; // 다른 World Canvas보다 앞 순서
            _canvasRect = canvasObject.GetComponent<RectTransform>(); // Canvas RectTransform 확보
            _canvasRect.sizeDelta = new Vector2(CanvasPixelWidth, CanvasPixelHeight); // 기준 픽셀 크기 적용
            _canvasRect.pivot = new Vector2(0.5f, 0f); // 플레이어 쪽 하단을 UI 기준점으로 사용
            _canvasRect.localScale = Vector3.one * CanvasScalePerTile; // 기본 스케일 초기화

            CreateTitlePlaque(canvasObject.transform); // 상단 제목 팻말 생성
            CreateSubtitlePlaque(canvasObject.transform); // 상태 줄 팻말 생성
            CreateHoverDescription(canvasObject.transform); // 하단 마우스 오버 설명 생성

            for (int i = 0; i < MaxOptionButtons; i++)
            {
                _optionViews.Add(CreateOptionView(canvasObject.transform, i)); // 카드형 선택지 생성
            }
        }

        private void CreateTitlePlaque(Transform parent)
        {
            var plaqueObject = new GameObject("TitlePlaque", typeof(RectTransform), typeof(Image)); // 제목 팻말 오브젝트 생성
            plaqueObject.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rect = plaqueObject.GetComponent<RectTransform>(); // 제목 팻말 RectTransform 확보
            SetRect(rect, new Vector2(0f, TitleY), new Vector2(TitlePlaqueWidth, TitlePlaqueHeight)); // 제목 팻말 위치·크기 적용
            _titlePlaqueImage = plaqueObject.GetComponent<Image>(); // 제목 팻말 배경 확보
            _titlePlaqueImage.color = new Color(0.16f, 0.09f, 0.04f, 0.95f); // 어두운 목재 느낌 배경 적용

            _titleText = CreateText("Title", plaqueObject.transform, 46, FontStyle.Bold); // 제목 텍스트 생성
            SetRect(_titleText.rectTransform, Vector2.zero, new Vector2(TitlePlaqueWidth - 24f, TitlePlaqueHeight - 18f)); // 제목 텍스트 위치·크기 적용
        }

        private void CreateSubtitlePlaque(Transform parent)
        {
            var plaqueObject = new GameObject("SubtitlePlaque", typeof(RectTransform), typeof(Image)); // 상태 줄 팻말 오브젝트 생성
            plaqueObject.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rect = plaqueObject.GetComponent<RectTransform>(); // 상태 줄 RectTransform 확보
            SetRect(rect, new Vector2(0f, SubtitleY), new Vector2(SubtitlePlaqueWidth, SubtitlePlaqueHeight)); // 상태 줄 위치·크기 적용
            _subtitlePlaqueImage = plaqueObject.GetComponent<Image>(); // 상태 줄 배경 확보
            _subtitlePlaqueImage.color = new Color(0.07f, 0.05f, 0.03f, 0.84f); // 얇은 정보 띠 배경 적용

            _subtitleText = CreateText("Subtitle", plaqueObject.transform, 22, FontStyle.Bold); // 상태 줄 텍스트 생성
            _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap; // 설명 줄바꿈 허용
            _subtitleText.verticalOverflow = VerticalWrapMode.Truncate; // 영역 밖 글자 잘라내기
            SetRect(_subtitleText.rectTransform, Vector2.zero, new Vector2(SubtitlePlaqueWidth - 18f, SubtitlePlaqueHeight - 6f)); // 상태 줄 텍스트 위치·크기 적용
        }

        private void CreateHoverDescription(Transform parent)
        {
            _hoverDescriptionRoot = new GameObject("HoverDescription", typeof(RectTransform), typeof(Image)); // 하단 설명 루트 생성
            _hoverDescriptionRoot.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = _hoverDescriptionRoot.GetComponent<RectTransform>(); // 설명 루트 RectTransform 확보
            SetRect(rootRect, new Vector2(0f, HoverDescriptionY), new Vector2(HoverDescriptionWidth, HoverDescriptionHeight)); // 하단 설명 위치·크기 적용
            Image background = _hoverDescriptionRoot.GetComponent<Image>(); // 설명 배경 확보
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.48f); // 흰 글자 가독성용 약한 어두운 배경 적용
            background.raycastTarget = false; // 카드 마우스 입력 간섭 제거

            _hoverDescriptionText = CreateText("HoverDescriptionText", _hoverDescriptionRoot.transform, 27, FontStyle.Bold); // 하단 흰색 설명 텍스트 생성
            _hoverDescriptionText.color = Color.white; // 요청한 흰색 설명 글자 적용
            _hoverDescriptionText.alignment = TextAnchor.MiddleCenter; // 설명 중앙 정렬 적용
            _hoverDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 설명 줄바꿈 허용
            _hoverDescriptionText.verticalOverflow = VerticalWrapMode.Truncate; // 영역 초과 설명 잘라내기
            SetRect(_hoverDescriptionText.rectTransform, Vector2.zero, new Vector2(HoverDescriptionWidth - 36f, HoverDescriptionHeight - 18f)); // 설명 텍스트 여백 적용
            _hoverDescriptionRoot.SetActive(false); // 기본 마우스 오버 설명 숨김
        }

        private OptionView CreateOptionView(Transform parent, int index)
        {
            var rootObject = new GameObject($"Option_{index}", typeof(RectTransform), typeof(Image), typeof(Button)); // 카드 루트 생성
            rootObject.transform.SetParent(parent, false); // Canvas 자식 연결
            RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 카드 루트 RectTransform 확보
            SetRect(rootRect, Vector2.zero, new Vector2(NormalCardWidth, NormalCardHeight)); // 기본 카드 크기 적용
            Image frameImage = rootObject.GetComponent<Image>(); // 카드 프레임 확보
            frameImage.color = new Color(0.18f, 0.11f, 0.05f, 0.98f); // 진한 프레임 색상 적용
            Button button = rootObject.GetComponent<Button>(); // 버튼 컴포넌트 확보
            StageOverlayHoverRelay hoverRelay = rootObject.AddComponent<StageOverlayHoverRelay>(); // 카드 마우스 오버 연결기 추가
            button.targetGraphic = frameImage; // 버튼 대상 그래픽 지정
            Navigation navigation = button.navigation; // 버튼 네비게이션 설정 조회
            navigation.mode = Navigation.Mode.None; // 방향키 자동 이동 비활성화
            button.navigation = navigation; // 버튼 네비게이션 적용

            var paperObject = new GameObject("Paper", typeof(RectTransform), typeof(Image)); // 카드 내부 종이 생성
            paperObject.transform.SetParent(rootObject.transform, false); // 카드 루트 자식 연결
            RectTransform paperRect = paperObject.GetComponent<RectTransform>(); // 카드 내부 종이 RectTransform 확보
            Stretch(paperRect, 9f); // 프레임 안쪽 여백 적용
            Image paperImage = paperObject.GetComponent<Image>(); // 카드 내부 종이 이미지 확보
            paperImage.color = new Color(0.83f, 0.75f, 0.58f, 0.90f); // 낡은 종이 색상 적용
            paperImage.raycastTarget = false; // 클릭 입력은 루트 버튼에만 전달

            var bandObject = new GameObject("TitleBand", typeof(RectTransform), typeof(Image)); // 카드 제목 띠 생성
            bandObject.transform.SetParent(paperObject.transform, false); // 카드 종이 자식 연결
            RectTransform bandRect = bandObject.GetComponent<RectTransform>(); // 제목 띠 RectTransform 확보
            SetRect(bandRect, Vector2.zero, new Vector2(NormalCardWidth - 36f, NormalCardHeight - 28f)); // 제목 영역을 카드 전체로 확장
            Image bandImage = bandObject.GetComponent<Image>(); // 제목 띠 이미지 확보
            bandImage.color = new Color(0f, 0f, 0f, 0f); // 카드 전체 제목 사용을 위해 별도 제목 띠 배경 제거
            bandImage.raycastTarget = false; // 클릭 입력은 루트 버튼에만 전달

            Text titleText = CreateText("Title", bandObject.transform, 27, FontStyle.Bold); // 카드 제목 텍스트 생성
            titleText.color = new Color(0.96f, 0.94f, 0.90f, 1f); // 밝은 제목 색상 적용
            SetRect(titleText.rectTransform, Vector2.zero, new Vector2(NormalCardWidth - 56f, NormalCardHeight - 38f)); // 카드 전체를 사용하는 큰 제목 영역 적용

            Text descriptionText = CreateText("Description", paperObject.transform, 1, FontStyle.Normal); // 하단 Hover 설명 전용으로 카드 내부 설명 오브젝트 유지
            descriptionText.text = string.Empty; // 카드 내부 작은 설명 제거
            descriptionText.gameObject.SetActive(false); // 카드 내부 설명 오브젝트 비활성화

            rootObject.SetActive(false); // 기본 미사용 상태

            return new OptionView
            {
                Button = button, // 버튼 저장
                RootRect = rootRect, // 카드 RectTransform 저장
                FrameImage = frameImage, // 프레임 이미지 저장
                PaperImage = paperImage, // 종이 이미지 저장
                TitleText = titleText, // 제목 텍스트 저장
                DescriptionText = descriptionText, // 설명 텍스트 저장
                HoverRelay = hoverRelay, // 마우스 오버 연결기 저장
            };
        }

        private void RebuildOptions(IReadOnlyList<StageOverlayOption> options)
        {
            for (int i = 0; i < _optionViews.Count; i++)
            {
                OptionView view = _optionViews[i]; // 현재 카드형 선택지 조회
                view.Button.onClick.RemoveAllListeners(); // 이전 페이지 콜백 제거
                if (view.HoverRelay != null) view.HoverRelay.Clear(); // 이전 마우스 오버 콜백 제거
                view.Button.gameObject.SetActive(false); // 기본 숨김 처리
                view.RootRect.localRotation = Quaternion.identity; // 이전 회전 초기화
            }

            HideHoverDescription(); // 페이지 전환 시 기존 하단 설명 제거
            int count = options != null ? Mathf.Min(options.Count, _optionViews.Count) : 0; // 표시 가능한 옵션 수 계산
            if (count <= 0) return; // 표시할 선택지 없음 방어

            int bottomIndex = HasDedicatedBottomCard(options, count) ? count - 1 : -1; // 하단 전용 카드 인덱스 계산
            int primaryCount = bottomIndex >= 0 ? count - 1 : count; // 일반 카드 수 계산
            int viewIndex = 0; // 현재 사용 중 카드 뷰 인덱스

            for (int i = 0; i < primaryCount; i++)
            {
                ConfigureOptionView(_optionViews[viewIndex], options[i], GetPrimaryCardPosition(i, primaryCount), new Vector2(NormalCardWidth, NormalCardHeight), GetPrimaryCardRotation(i, primaryCount), false); // 일반 카드 배치
                viewIndex++; // 다음 카드 뷰로 이동
            }

            if (bottomIndex >= 0 && viewIndex < _optionViews.Count)
            {
                ConfigureOptionView(_optionViews[viewIndex], options[bottomIndex], GetBottomCardPosition(primaryCount), new Vector2(BottomCardWidth, BottomCardHeight), 0f, true); // 하단 전용 카드 배치
            }
        }

        private void ConfigureOptionView(OptionView view, StageOverlayOption option, Vector2 position, Vector2 size, float rotationZ, bool isBottomCard)
        {
            if (view == null || option == null) return; // 잘못된 카드 뷰·옵션 방어

            view.RootRect.sizeDelta = size; // 카드 크기 적용
            RectTransform titleBandRect = view.TitleText.transform.parent.GetComponent<RectTransform>(); // 현재 카드 제목 영역 조회
            if (titleBandRect != null) titleBandRect.sizeDelta = new Vector2(size.x - 36f, size.y - 28f); // 카드별 실제 크기에 제목 영역 맞춤
            view.TitleText.rectTransform.sizeDelta = new Vector2(size.x - 56f, size.y - 38f); // 카드 전체를 사용하는 제목 텍스트 크기 적용
            view.RootRect.anchoredPosition = position; // 카드 위치 적용
            view.RootRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ); // 카드 약간 기울어진 배치 적용
            view.TitleText.text = option.Title ?? string.Empty; // 카드 제목 적용
            view.DescriptionText.text = string.Empty; // 카드 내부 작은 설명은 사용하지 않고 Hover 하단 설명만 사용
            view.Button.interactable = option.Interactable; // 카드 선택 가능 여부 적용
            view.Button.onClick.AddListener(() => option.Callback?.Invoke()); // 카드 선택 콜백 연결
            if (view.HoverRelay != null) view.HoverRelay.Configure(() => ShowHoverDescription(option), HideHoverDescription); // 마우스 오버 설명 연결
            view.Button.gameObject.SetActive(true); // 카드 표시

            Color frameColor = isBottomCard ? new Color(0.23f, 0.14f, 0.06f, 0.98f) : new Color(0.18f, 0.11f, 0.05f, 0.98f); // 카드 종류별 프레임 색상
            Color paperColor = isBottomCard ? new Color(0.86f, 0.79f, 0.62f, 0.93f) : new Color(0.83f, 0.75f, 0.58f, 0.90f); // 카드 종류별 종이 색상

            if (!option.Interactable)
            {
                frameColor = new Color(0.16f, 0.14f, 0.12f, 0.92f); // 비활성 카드 프레임 색상
                paperColor = new Color(0.52f, 0.50f, 0.46f, 0.82f); // 비활성 카드 종이 색상
            }

            view.FrameImage.color = frameColor; // 프레임 색상 적용
            view.PaperImage.color = paperColor; // 종이 색상 적용
            view.TitleText.fontSize = isBottomCard ? 38 : 34; // 모든 카드 제목을 카드 전체에서 크게 표시
            view.TitleText.alignment = TextAnchor.MiddleCenter; // 카드 중앙에 제목 정렬
            view.DescriptionText.gameObject.SetActive(false); // 모든 카드 내부 작은 설명 숨김 유지
        }

        private void ShowHoverDescription(StageOverlayOption option)
        {
            if (_hoverDescriptionRoot == null || _hoverDescriptionText == null || option == null) return; // 하단 설명 참조 누락 방어
            string title = option.Title ?? string.Empty; // 마우스 오버 카드 제목 조회
            string description = option.Description ?? string.Empty; // 마우스 오버 카드 설명 조회
            _hoverDescriptionText.text = string.IsNullOrWhiteSpace(description) ? title : $"{title}\n{description}"; // 제목과 설명을 하단 흰 글자로 표시
            _hoverDescriptionRoot.SetActive(true); // 하단 마우스 오버 설명 표시
        }

        private void HideHoverDescription()
        {
            if (_hoverDescriptionRoot == null) return; // 설명 루트 누락 방어
            _hoverDescriptionRoot.SetActive(false); // 하단 마우스 오버 설명 숨김
            if (_hoverDescriptionText != null) _hoverDescriptionText.text = string.Empty; // 이전 설명 문구 제거
        }

        private static bool HasDedicatedBottomCard(IReadOnlyList<StageOverlayOption> options, int count)
        {
            if (options == null || count <= 0) return false; // 입력 누락 방어
            string title = options[count - 1] != null ? options[count - 1].Title : string.Empty; // 마지막 카드 제목 조회
            if (string.IsNullOrWhiteSpace(title)) return false; // 빈 제목 방어
            return title.Contains("나가기", System.StringComparison.Ordinal) || title.Contains("뒤로", System.StringComparison.Ordinal) || title.Contains("취소", System.StringComparison.Ordinal); // 하단 전용 카드 조건 판정
        }

        private static Vector2 GetPrimaryCardPosition(int index, int count)
        {
            switch (count)
            {
                case 1:
                    return new Vector2(0f, 58f); // 단일 카드 중앙 배치
                case 2:
                    return index == 0 ? new Vector2(-255f, 52f) : new Vector2(255f, 52f); // 2장 좌우 배치
                case 3:
                    return index switch
                    {
                        0 => new Vector2(-255f, 104f), // 3장 좌상 배치
                        1 => new Vector2(255f, 104f), // 3장 우상 배치
                        _ => new Vector2(0f, -82f), // 3장 하단 중앙 배치
                    };
                case 4:
                    return index switch
                    {
                        0 => new Vector2(-255f, 104f), // 4장 좌상 배치
                        1 => new Vector2(255f, 104f), // 4장 우상 배치
                        2 => new Vector2(-255f, -82f), // 4장 좌하 배치
                        _ => new Vector2(255f, -82f), // 4장 우하 배치
                    };
                case 5:
                    return index switch
                    {
                        0 => new Vector2(-255f, 140f), // 5장 1행 좌측 배치
                        1 => new Vector2(255f, 140f), // 5장 1행 우측 배치
                        2 => new Vector2(-255f, -28f), // 5장 2행 좌측 배치
                        3 => new Vector2(255f, -28f), // 5장 2행 우측 배치
                        _ => new Vector2(0f, -196f), // 5장 3행 중앙 배치
                    };
                case 6:
                    return index switch
                    {
                        0 => new Vector2(-220f, 148f), // 6장 1행 좌측 배치
                        1 => new Vector2(220f, 148f), // 6장 1행 우측 배치
                        2 => new Vector2(-220f, 8f), // 6장 2행 좌측 배치
                        3 => new Vector2(220f, 8f), // 6장 2행 우측 배치
                        4 => new Vector2(-220f, -132f), // 6장 3행 좌측 배치
                        _ => new Vector2(220f, -132f), // 6장 3행 우측 배치
                    };
                case 7:
                    return index switch
                    {
                        0 => new Vector2(-220f, 168f), // 7장 1행 좌측 배치
                        1 => new Vector2(220f, 168f), // 7장 1행 우측 배치
                        2 => new Vector2(-220f, 38f), // 7장 2행 좌측 배치
                        3 => new Vector2(220f, 38f), // 7장 2행 우측 배치
                        4 => new Vector2(-220f, -92f), // 7장 3행 좌측 배치
                        5 => new Vector2(220f, -92f), // 7장 3행 우측 배치
                        _ => new Vector2(0f, -222f), // 7장 4행 중앙 배치
                    };
                default:
                    return index switch
                    {
                        0 => new Vector2(-220f, 188f), // 8장 1행 좌측 배치
                        1 => new Vector2(220f, 188f), // 8장 1행 우측 배치
                        2 => new Vector2(-220f, 68f), // 8장 2행 좌측 배치
                        3 => new Vector2(220f, 68f), // 8장 2행 우측 배치
                        4 => new Vector2(-220f, -52f), // 8장 3행 좌측 배치
                        5 => new Vector2(220f, -52f), // 8장 3행 우측 배치
                        6 => new Vector2(-220f, -172f), // 8장 4행 좌측 배치
                        _ => new Vector2(220f, -172f), // 8장 4행 우측 배치
                    };
            }
        }

        private static Vector2 GetBottomCardPosition(int primaryCount)
        {
            return primaryCount >= 6 ? new Vector2(0f, -250f) : new Vector2(0f, -225f); // 확대 UI 하단 전용 카드 위치 계산
        }

        private static float GetPrimaryCardRotation(int index, int count)
        {
            if (count <= 2) return index == 0 ? -3f : 3f; // 카드가 적을 때 좌우 기울기 적용
            return index % 4 switch
            {
                0 => -3.5f, // 첫 번째 카드 기울기
                1 => 2.5f, // 두 번째 카드 기울기
                2 => -2.2f, // 세 번째 카드 기울기
                _ => 3.2f, // 네 번째 카드 기울기
            };
        }

        private void ApplyModeVisual(StageOverlayMode mode)
        {
            Color matColor = mode == StageOverlayMode.Shop
                ? new Color(0.27f, 0.12f, 0.05f, 1f)
                : new Color(0.14f, 0.07f, 0.17f, 1f); // 타입별 돗자리 본체 색상

            Color trimColor = mode == StageOverlayMode.Shop
                ? new Color(0.76f, 0.51f, 0.18f, 1f)
                : new Color(0.57f, 0.28f, 0.69f, 1f); // 타입별 돗자리 장식 색상

            if (_matMaterial != null) _matMaterial.color = matColor; // 본체 색상 갱신
            if (_trimMaterial != null) _trimMaterial.color = trimColor; // 장식 색상 갱신
            if (_titlePlaqueImage != null) _titlePlaqueImage.color = mode == StageOverlayMode.Shop ? new Color(0.18f, 0.09f, 0.04f, 0.97f) : new Color(0.10f, 0.05f, 0.15f, 0.97f); // 제목 팻말 색상 갱신
            if (_subtitlePlaqueImage != null) _subtitlePlaqueImage.color = mode == StageOverlayMode.Shop ? new Color(0.07f, 0.05f, 0.03f, 0.84f) : new Color(0.08f, 0.05f, 0.10f, 0.84f); // 상태 줄 팻말 색상 갱신
        }

        private void ApplyPrimaryCameraPresentation()
        {
            if (_canvas == null || _canvasRect == null || _boardView == null) return; // 필수 참조 누락 방어

            Camera targetCamera = Camera.main; // 1번 고정 대상 Main Camera 조회
            if (targetCamera == null) targetCamera = Object.FindFirstObjectByType<Camera>(); // 보조 카메라 탐색
            if (targetCamera != null && _canvas.worldCamera != targetCamera) _canvas.worldCamera = targetCamera; // World Space UI 레이캐스트 카메라 동기화

            float tileSize = Mathf.Max(0.1f, _boardView.TileSize); // 타일 크기 보정
            Vector3 baseAnchor = StageActivityCameraPoseUtility.ResolveCanvasLocalAnchor(tileSize); // 기본 고정 UI 하단 위치 조회
            Vector3 offset = new Vector3(0f, CanvasHeightOffsetTiles * tileSize, CanvasDepthOffsetTiles * tileSize); // 카드형 배치를 위한 추가 위치 보정
            _canvasRect.localScale = Vector3.one * (tileSize * CanvasScalePerTile); // 현재 타일 크기 기준 고정 스케일 계산
            _canvasRect.localPosition = baseAnchor + offset; // 1번 카메라에 맞춘 실제 UI 위치 적용
            _canvasRect.localRotation = StageActivityCameraPoseUtility.ResolveCanvasLocalRotation(tileSize); // 1번 카메라를 정면으로 보는 UI 회전 적용
        }

        private void HideRouteMapVisuals()
        {
            _mapVisualRoot = _boardView != null ? _boardView.transform.Find("RouteMapVisuals_Day44")?.gameObject : null; // 현재 지도 표시 루트 조회
            _mapVisualWasActive = _mapVisualRoot != null && _mapVisualRoot.activeSelf; // 기존 활성 상태 기록
            if (_mapVisualWasActive) _mapVisualRoot.SetActive(false); // 돗자리 아래 지도 기물·노드 숨김
        }

        private void RestoreRouteMapVisuals()
        {
            if (_mapVisualRoot != null && _mapVisualWasActive) _mapVisualRoot.SetActive(true); // 기존 지도 표시 복원
            _mapVisualRoot = null; // 지도 루트 참조 정리
            _mapVisualWasActive = false; // 복원 플래그 초기화
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = TextAnchor.MiddleCenter; // 기본 중앙 정렬
            text.color = Color.white; // 기본 흰색 텍스트 적용
            text.raycastTarget = false; // 버튼 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            return _runtimeFont; // 최종 폰트 반환
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); // URP Unlit 우선 조회
            if (shader == null) shader = Shader.Find("Unlit/Color"); // 기본 Unlit 대체
            if (shader == null) shader = Shader.Find("Standard"); // 최종 Standard 대체

            var material = new Material(shader); // 런타임 머티리얼 생성
            material.color = color; // 머티리얼 색상 적용
            return material; // 완성 머티리얼 반환
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target != null ? target.GetComponent<Collider>() : null; // 기본 프리미티브 Collider 조회
            if (collider != null) Object.Destroy(collider); // 보드 입력 간섭 Collider 제거
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백
        }

        private void OnDestroy()
        {
            RestoreRouteMapVisuals(); // 지도 표시 안전 복원
            if (_matMaterial != null) Destroy(_matMaterial); // 런타임 돗자리 머티리얼 제거
            if (_trimMaterial != null) Destroy(_trimMaterial); // 런타임 장식 머티리얼 제거
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
