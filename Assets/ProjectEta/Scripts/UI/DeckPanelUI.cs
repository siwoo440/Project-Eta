using System.Collections.Generic; // 현재 패널에 생성된 카드 썸네일 목록을 관리하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Vector2 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // EventSystem을 런타임 생성하기 위한 네임스페이스
using UnityEngine.InputSystem.UI; // 새 Input System 기반 UI 포인터 입력 모듈을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, Button, ScrollRect, GridLayoutGroup 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class DeckPanelUI : MonoBehaviour // 19일차: 좌하단 뽑을 카드 덱 / 우하단 죽은 카드 덱 버튼과 카드 목록 패널을 관리하는 컴포넌트
    {
        private const int ColumnsPerRow = 5; // 요청대로 카드를 가로 5장씩 배치
        private const float ThumbWidth = 132f; // 썸네일 카드 한 장의 너비
        private const float ThumbHeight = 168f; // 썸네일 카드 한 장의 높이
        private const float ThumbSpacing = 14f; // 썸네일 카드 사이 간격

        public bool IsPanelOpen => _isPanelOpen; // 테스트와 디버그에서 패널 열림 상태를 확인하는 프로퍼티
        public bool IsShowingDrawPile => _isShowingDrawPile; // 테스트와 디버그에서 현재 어떤 더미를 보여주는지 확인하는 프로퍼티
        public int PanelItemCount => _panelItems.Count; // 테스트에서 현재 패널에 그려진 카드 수를 확인하는 프로퍼티

        private BoardInputController _boardInput; // 실제 RunState.Deck을 제공하는 입력 컨트롤러
        private Canvas _canvas; // 이 UI 전용 Screen Space Overlay Canvas
        private Text _drawPileButtonText; // 좌하단 버튼에 표시되는 드로우 더미 장수 텍스트
        private Text _deadPileButtonText; // 우하단 버튼에 표시되는 죽은 카드 더미 장수 텍스트
        private GameObject _panelRoot; // 전체 화면 반투명 배경과 카드 목록 패널의 루트
        private Text _panelTitleText; // 패널 상단 제목("뽑을 카드 덱" / "죽은 카드 덱")
        private RectTransform _panelContent; // ScrollRect 내부의 실제 카드 썸네일 컨테이너
        private readonly List<GameObject> _panelItems = new List<GameObject>(); // 현재 패널에 생성된 카드 썸네일 목록
        private bool _isPanelOpen; // 현재 패널이 열려 있는지 여부
        private bool _isShowingDrawPile = true; // 현재 열린 패널이 드로우 더미인지(false면 죽은 카드 더미)
        private EventSystem _createdEventSystem; // 이 컴포넌트가 직접 만든 EventSystem 참조
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시

        public void Bind(BoardInputController boardInput) // 실제 BoardInputController와 RunState.Deck을 이 UI에 연결하는 메서드
        {
            if (_boardInput != null) // 이전에 연결된 입력 컨트롤러가 있었다면
            {
                _boardInput.DeckChanged -= HandleDeckChanged; // 이전 덱 변경 이벤트 구독 해제
            }

            _boardInput = boardInput; // 새 실제 입력 컨트롤러 저장
            EnsureUI(); // 버튼·패널 UI를 런타임 생성(최초 1회)

            if (_boardInput != null) // 정상 입력 컨트롤러가 전달됐다면
            {
                _boardInput.DeckChanged += HandleDeckChanged; // 드로우/죽은 카드 더미 변경 이벤트 구독
            }

            RefreshButtons(); // 현재 덱 상태로 버튼 장수 표시 즉시 갱신
            if (_isPanelOpen) RefreshPanelContent(); // 이미 패널이 열려 있었다면 새 상태로 다시 그림
        }

        private void HandleDeckChanged() // 드로우/죽은 카드 더미가 바뀔 때마다 호출되는 이벤트 처리 메서드
        {
            RefreshButtons(); // 버튼에 표시되는 장수 갱신
            if (_isPanelOpen) RefreshPanelContent(); // 패널이 열려 있으면 목록도 즉시 갱신
        }

        private void RefreshButtons() // 좌·우 버튼에 현재 덱 장수를 반영하는 메서드
        {
            if (_boardInput == null || _boardInput.RunState == null) return; // 실제 상태가 없으면 갱신하지 않음
            var deck = _boardInput.RunState.Deck; // 실제 RunState.Deck 참조
            if (_drawPileButtonText != null) _drawPileButtonText.text = $"뽑을 카드\n{deck.DrawPile.Count}장"; // 좌하단 버튼 문구 갱신
            if (_deadPileButtonText != null) _deadPileButtonText.text = $"죽은 카드\n{deck.DeadCardPile.Count}장"; // 우하단 버튼 문구 갱신
        }

        private void TogglePanel(bool showDrawPile) // 버튼 클릭 시 패널을 열거나 닫는 메서드
        {
            if (_isPanelOpen && _isShowingDrawPile == showDrawPile) // 이미 같은 더미를 보여주는 패널이 열려 있으면
            {
                ClosePanel(); // 다시 누르면 닫히는 토글 동작
                return; // 처리 종료
            }

            _isShowingDrawPile = showDrawPile; // 이번에 열 더미 종류 저장
            OpenPanel(); // 패널 열기
        }

        private void OpenPanel() // 카드 목록 패널을 열고 내용을 채우는 메서드
        {
            _isPanelOpen = true; // 열림 상태 기록
            _panelRoot.SetActive(true); // 패널 루트 활성화
            _panelTitleText.text = _isShowingDrawPile ? "뽑을 카드 덱" : "죽은 카드 덱"; // 현재 더미에 맞는 제목 표시
            RefreshPanelContent(); // 실제 카드 목록으로 채움
        }

        private void ClosePanel() // 카드 목록 패널을 닫는 메서드
        {
            _isPanelOpen = false; // 닫힘 상태 기록
            if (_panelRoot != null) _panelRoot.SetActive(false); // 패널 루트 비활성화
        }

        private void RefreshPanelContent() // 현재 선택된 더미의 카드 전체를 썸네일로 다시 그리는 메서드
        {
            ClearPanelItems(); // 이전에 그려진 썸네일 제거
            if (_boardInput == null || _boardInput.RunState == null) return; // 실제 덱 상태가 없으면 빈 목록 유지

            var deck = _boardInput.RunState.Deck; // 실제 RunState.Deck 참조
            var list = _isShowingDrawPile ? deck.DrawPile : deck.DeadCardPile; // 현재 보여줄 더미 선택
            for (int i = 0; i < list.Count; i++) // 더미의 카드를 순서대로 순회하며
            {
                CreateThumbnail(list[i], i); // 카드 1장을 썸네일로 생성
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelContent); // 카드 수 변경 직후 그리드·스크롤 범위 즉시 갱신
        }

        private void ClearPanelItems() // 현재 패널에 그려진 카드 썸네일을 모두 제거하는 메서드
        {
            foreach (var item in _panelItems) // 기존 썸네일 목록 순회
            {
                if (item == null) continue; // 이미 제거된 항목은 건너뜀
                if (Application.isPlaying) Destroy(item); // Play Mode에서는 프레임 종료 시 안전하게 제거
                else DestroyImmediate(item); // EditMode 테스트에서는 즉시 제거
            }

            _panelItems.Clear(); // 내부 목록도 비움
        }

        private void CreateThumbnail(PieceDefinition definition, int index) // 카드 1장을 5열 그리드에 들어갈 썸네일로 만드는 메서드
        {
            var item = CreatePanel($"CardThumb_{index}_{(definition != null ? definition.name : "None")}", _panelContent, new Color(0.13f, 0.15f, 0.19f, 1f)); // 어두운 카드 배경 패널 생성
            AddOutline(item.gameObject, new Color(0.5f, 0.5f, 0.55f, 0.8f), new Vector2(1.5f, -1.5f)); // 옅은 외곽선으로 카드 경계 표시

            bool hasArtwork = definition != null && definition.CardArtwork != null; // 실제 카드 일러스트 연결 여부 확인
            var artObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image)); // 상단 초상화/약칭 영역 생성
            artObject.transform.SetParent(item.transform, false); // 썸네일 배경 자식으로 연결
            var artImage = artObject.GetComponent<Image>(); // 초상화 Image 확보
            artImage.raycastTarget = false; // 카드 목록은 클릭 대상이 아니므로 Raycast 비활성화
            SetRect(artImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(ThumbWidth - 16f, 88f)); // 상단에 정사각형에 가까운 초상화 영역 배치

            if (hasArtwork) // 실제 Artwork Sprite가 있으면
            {
                artImage.sprite = definition.CardArtwork; // 실제 일러스트 적용
                artImage.color = Color.white; // 원본 색상 유지
                artImage.preserveAspect = true; // 비율 유지
            }
            else // Artwork가 없으면
            {
                artImage.color = new Color(0.22f, 0.27f, 0.33f, 1f); // 대체 배경색 적용
                var placeholder = CreateText("Placeholder", artObject.transform, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 기물 약칭 텍스트 생성
                Stretch(placeholder.rectTransform, 0f, 0f, 0f, 0f); // 초상화 영역 전체 사용
                placeholder.text = GetPortraitPlaceholder(definition); // 이동 타입 약칭 적용
            }

            var nameText = CreateText("NameText", item.transform, 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 카드 이름 텍스트 생성
            SetRect(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(ThumbWidth - 12f, 22f)); // 초상화 아래 이름 배치
            nameText.text = definition != null ? (string.IsNullOrEmpty(definition.DisplayName) ? definition.name : definition.DisplayName) : "?"; // 실제 이름 또는 대체 문구 적용
            nameText.resizeTextForBestFit = true; // 긴 이름 자동 축소
            nameText.resizeTextMinSize = 9; // 최소 글자 크기
            nameText.resizeTextMaxSize = 14; // 최대 글자 크기

            var statsText = CreateText("StatsText", item.transform, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f, 1f)); // 등급·ATK·HP 요약 텍스트 생성
            SetRect(statsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(ThumbWidth - 12f, 18f)); // 이름 아래 배치
            statsText.text = definition != null ? $"{Mathf.Max(1, (int)definition.Grade)}성 · ATK {definition.BaseAtk} · HP {definition.BaseHp}" : ""; // 등급·공격력·체력 요약 문구 적용

            _panelItems.Add(item.gameObject); // 생성한 썸네일을 정리 목록에 등록
        }

        private static string GetPortraitPlaceholder(PieceDefinition definition) // Artwork가 없을 때 초상화 자리에 표시할 약칭을 만드는 메서드
        {
            if (definition == null) return "?"; // 데이터가 없으면 물음표 반환
            switch (definition.MovementType) // 이동 타입에 따라 약칭 결정(CardView와 동일한 규칙)
            {
                case PieceMovementType.King: return "K"; // 킹
                case PieceMovementType.Queen: return "Q"; // 퀸
                case PieceMovementType.Rook: return "R"; // 룩
                case PieceMovementType.Bishop: return "B"; // 비숍
                case PieceMovementType.Knight: return "N"; // 나이트
                case PieceMovementType.Pawn: return "P"; // 폰
                default: return "?"; // 아직 약칭이 없는 페어리 기물
            }
        }

        private void EnsureUI() // 버튼·패널·Canvas를 한 번만 만드는 메서드
        {
            if (_canvas != null) return; // 이미 생성됐으면 중복 생성하지 않음
            EnsureEventSystem(); // 버튼 클릭·스크롤 입력을 처리할 EventSystem 보장

            var canvasObject = new GameObject("DeckPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BattleController 또는 테스트 호스트의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 95; // 손패(90)보다 위, 턴 상태(100)보다 아래에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildCornerButtons(canvasObject.transform); // 좌하단·우하단 버튼 생성
            BuildPanel(canvasObject.transform); // 카드 목록 패널(초기 비활성) 생성
        }

        private void BuildCornerButtons(Transform parent) // 좌하단 뽑을 카드 버튼과 우하단 죽은 카드 버튼을 만드는 메서드
        {
            var drawButton = CreateCornerButton(parent, "DrawPileButton", new Vector2(0f, 0f), new Vector2(24f, 24f), new Color(0.16f, 0.32f, 0.5f, 0.92f)); // 좌하단 버튼 생성
            drawButton.button.onClick.AddListener(() => TogglePanel(true)); // 클릭 시 드로우 더미 패널 토글
            _drawPileButtonText = drawButton.text; // 좌하단 버튼 텍스트 참조 저장

            var deadButton = CreateCornerButton(parent, "DeadPileButton", new Vector2(1f, 0f), new Vector2(-24f, 24f), new Color(0.5f, 0.18f, 0.18f, 0.92f)); // 우하단 버튼 생성
            deadButton.button.onClick.AddListener(() => TogglePanel(false)); // 클릭 시 죽은 카드 더미 패널 토글
            _deadPileButtonText = deadButton.text; // 우하단 버튼 텍스트 참조 저장
        }

        private (Button button, Text text) CreateCornerButton(Transform parent, string name, Vector2 anchor, Vector2 offset, Color color) // 화면 모서리에 배치되는 카드 더미 버튼을 만드는 공통 메서드
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 GameObject 생성
            buttonObject.transform.SetParent(parent, false); // Canvas 자식으로 연결
            var rect = buttonObject.GetComponent<RectTransform>(); // RectTransform 확보
            rect.anchorMin = anchor; // 모서리 앵커 지정(좌하단 또는 우하단)
            rect.anchorMax = anchor; // 동일 앵커로 고정
            rect.pivot = anchor; // 모서리를 기준점으로 사용해 offset 방향이 화면 안쪽을 향하게 함
            rect.anchoredPosition = offset; // 모서리에서 안쪽으로 띄운 위치 적용
            rect.sizeDelta = new Vector2(150f, 74f); // 버튼 크기 지정

            var image = buttonObject.GetComponent<Image>(); // 버튼 배경 Image 확보
            image.color = color; // 좌/우 구분 색상 적용

            var button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            var colors = button.colors; // 기본 컬러 트랜지션 값 조회
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f); // 마우스 오버 시 살짝 밝아지는 색 적용
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f); // 클릭 시 살짝 어두워지는 색 적용
            button.colors = colors; // 변경한 컬러 트랜지션 적용

            var text = CreateText(name + "Text", buttonObject.transform, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 버튼 문구 텍스트 생성
            Stretch(text.rectTransform, 4f, 4f, 4f, 4f); // 버튼 내부 여백 적용

            return (button, text); // 버튼과 텍스트 참조 반환
        }

        private void BuildPanel(Transform parent) // 카드 목록을 보여주는 전체 화면 모달 패널을 만드는 메서드
        {
            var backdrop = new GameObject("DeckPanelBackdrop", typeof(RectTransform), typeof(Image), typeof(Button)); // 화면 전체를 덮는 반투명 배경 겸 닫기 버튼 생성
            backdrop.transform.SetParent(parent, false); // Canvas 자식으로 연결
            _panelRoot = backdrop; // 패널 전체 루트로 저장(열고 닫을 때 이 오브젝트를 켜고 끔)
            var backdropRect = backdrop.GetComponent<RectTransform>(); // 배경 RectTransform 확보
            Stretch(backdropRect, 0f, 0f, 0f, 0f); // 화면 전체를 덮도록 Stretch
            var backdropImage = backdrop.GetComponent<Image>(); // 배경 Image 확보
            backdropImage.color = new Color(0f, 0f, 0f, 0.68f); // 어두운 반투명 배경으로 뒤쪽 보드 입력을 차단
            backdrop.GetComponent<Button>().onClick.AddListener(ClosePanel); // 배경 클릭 시 패널 닫기

            var body = CreatePanel("PanelBody", backdrop.transform, new Color(0.09f, 0.1f, 0.13f, 0.98f)); // 카드 목록을 담을 중앙 패널 생성
            SetRect(body.rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 640f)); // 화면 중앙에 고정 크기로 배치
            body.image.raycastTarget = true; // 패널 내부 클릭이 배경 닫기로 전파되지 않도록 자체적으로 Raycast 소비
            var bodyButtonBlocker = body.gameObject.AddComponent<Button>(); // 패널 몸체 클릭이 뒤 배경으로 전달되지 않도록 빈 Button으로 이벤트 소비
            bodyButtonBlocker.transition = Selectable.Transition.None; // 시각적 변화 없이 클릭만 차단하는 용도로 사용
            AddOutline(body.gameObject, new Color(0.5f, 0.5f, 0.55f, 0.9f), new Vector2(2f, -2f)); // 패널 외곽선 표시

            _panelTitleText = CreateText("PanelTitle", body.transform, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 패널 제목 텍스트 생성
            SetRect(_panelTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(760f, 40f)); // 패널 상단 중앙에 배치

            var closeButton = CreateCloseButton(body.transform); // 우상단 닫기 버튼 생성
            closeButton.onClick.AddListener(ClosePanel); // 닫기 버튼 클릭 시 패널 닫기

            BuildScrollArea(body.transform); // 제목 아래 실제 카드 목록 ScrollRect 생성

            backdrop.SetActive(false); // 처음에는 패널을 닫아 둔 상태로 시작
        }

        private Button CreateCloseButton(Transform parent) // 패널 우상단의 작은 닫기(X) 버튼을 만드는 메서드
        {
            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 닫기 버튼 GameObject 생성
            closeObject.transform.SetParent(parent, false); // 패널 몸체 자식으로 연결
            var rect = closeObject.GetComponent<RectTransform>(); // RectTransform 확보
            SetRect(rect, new Vector2(1f, 1f), new Vector2(-28f, -26f), new Vector2(40f, 40f)); // 패널 우상단에 배치
            closeObject.GetComponent<Image>().color = new Color(0.55f, 0.2f, 0.2f, 1f); // 닫기 버튼 배경색 적용

            var text = CreateText("CloseText", closeObject.transform, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // X 문구 텍스트 생성
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f); // 버튼 전체 영역 사용
            text.text = "X"; // 닫기 기호 표시

            return closeObject.GetComponent<Button>(); // 생성한 Button 컴포넌트 반환
        }

        private void BuildScrollArea(Transform parent) // 카드 5열 그리드를 세로로 스크롤해서 볼 수 있는 ScrollRect를 만드는 메서드
        {
            var scrollObject = new GameObject("CardScrollView", typeof(RectTransform), typeof(ScrollRect)); // ScrollRect 루트 생성
            scrollObject.transform.SetParent(parent, false); // 패널 몸체 자식으로 연결
            var scrollRect = scrollObject.GetComponent<RectTransform>(); // ScrollRect 루트 RectTransform 확보
            SetRect(scrollRect, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(860f, 540f)); // 제목 아래 남은 영역을 채우도록 배치(하단 기준)

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // 스크롤 영역 바깥을 가리는 Viewport 생성
            viewportObject.transform.SetParent(scrollObject.transform, false); // ScrollRect 자식으로 연결
            var viewportRect = viewportObject.GetComponent<RectTransform>(); // Viewport RectTransform 확보
            Stretch(viewportRect, 0f, 0f, 0f, 0f); // 부모 영역 전체 사용
            var viewportImage = viewportObject.GetComponent<Image>(); // Mask가 참조할 Image 확보
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f); // 거의 투명하지만 Raycast는 받도록 최소 알파 유지
            viewportObject.GetComponent<Mask>().showMaskGraphic = false; // 뷰포트 자체 이미지는 화면에 그리지 않음

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // 실제 카드 썸네일을 담을 Content 생성
            contentObject.transform.SetParent(viewportObject.transform, false); // Viewport 자식으로 연결
            _panelContent = contentObject.GetComponent<RectTransform>(); // Content RectTransform 저장
            _panelContent.anchorMin = new Vector2(0f, 1f); // 좌상단 기준 앵커(위에서부터 카드가 쌓이도록)
            _panelContent.anchorMax = new Vector2(1f, 1f); // 가로는 부모 전체를 사용
            _panelContent.pivot = new Vector2(0.5f, 1f); // 상단 중앙 피벗
            _panelContent.anchoredPosition = Vector2.zero; // 기본 위치 사용
            _panelContent.sizeDelta = new Vector2(0f, 0f); // 실제 높이는 GridLayoutGroup·ContentSizeFitter가 계산

            var grid = contentObject.GetComponent<GridLayoutGroup>(); // 5열 그리드 레이아웃 확보
            grid.cellSize = new Vector2(ThumbWidth, ThumbHeight); // 카드 썸네일 크기 지정
            grid.spacing = new Vector2(ThumbSpacing, ThumbSpacing); // 카드 사이 간격 지정
            grid.padding = new RectOffset(10, 10, 10, 10); // 목록 안쪽 여백 지정
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft; // 좌상단부터 카드 배치 시작
            grid.startAxis = GridLayoutGroup.Axis.Horizontal; // 가로 방향으로 먼저 채움
            grid.childAlignment = TextAnchor.UpperCenter; // 셀 내부 정렬
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 열 수를 고정해 요청대로 가로 5장씩 배치
            grid.constraintCount = ColumnsPerRow; // 고정 열 수 5 적용

            var fitter = contentObject.GetComponent<ContentSizeFitter>(); // Content 높이 자동 계산용 Fitter 확보
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 카드 수에 맞춰 세로 크기 자동 확장
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 가로는 부모(뷰포트) 폭을 그대로 사용

            var scroll = scrollObject.GetComponent<ScrollRect>(); // ScrollRect 컴포넌트 확보
            scroll.viewport = viewportRect; // 뷰포트 연결
            scroll.content = _panelContent; // 실제 스크롤 대상 Content 연결
            scroll.horizontal = false; // 가로 스크롤은 사용하지 않음(요청대로 세로만)
            scroll.vertical = true; // 세로 스크롤 허용
            scroll.movementType = ScrollRect.MovementType.Clamped; // 목록 범위를 벗어나지 않도록 제한
            scroll.scrollSensitivity = 24f; // 마우스 휠 스크롤 감도

            BuildScrollbar(scrollObject.transform, scroll); // 세로 스크롤바 추가
        }

        private void BuildScrollbar(Transform scrollParent, ScrollRect scrollRect) // 세로 스크롤 위치를 보여주는 간단한 스크롤바를 만드는 메서드
        {
            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar)); // 스크롤바 루트 생성
            scrollbarObject.transform.SetParent(scrollParent, false); // ScrollRect 자식으로 연결
            var rect = scrollbarObject.GetComponent<RectTransform>(); // 스크롤바 RectTransform 확보
            rect.anchorMin = new Vector2(1f, 0f); // 오른쪽 전체 높이를 기준 앵커로 사용
            rect.anchorMax = new Vector2(1f, 1f); // 오른쪽 전체 높이를 기준 앵커로 사용
            rect.pivot = new Vector2(1f, 1f); // 우상단 피벗
            rect.anchoredPosition = new Vector2(2f, 0f); // 카드 목록 오른쪽 바깥에 살짝 띄워 배치
            rect.sizeDelta = new Vector2(14f, 0f); // 얇은 세로 막대 크기 지정
            scrollbarObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f); // 스크롤바 배경 트랙 색상

            var handleAreaObject = new GameObject("SlidingArea", typeof(RectTransform)); // 스크롤바 손잡이가 움직일 영역 생성
            handleAreaObject.transform.SetParent(scrollbarObject.transform, false); // 스크롤바 자식으로 연결
            Stretch(handleAreaObject.GetComponent<RectTransform>(), 2f, 2f, 2f, 2f); // 트랙 안쪽으로 살짝 여백

            var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image)); // 실제 드래그 손잡이 생성
            handleObject.transform.SetParent(handleAreaObject.transform, false); // 슬라이딩 영역 자식으로 연결
            var handleImage = handleObject.GetComponent<Image>(); // 손잡이 Image 확보
            handleImage.color = new Color(0.7f, 0.75f, 0.85f, 0.85f); // 손잡이 색상 적용

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>(); // Scrollbar 컴포넌트 확보
            scrollbar.direction = Scrollbar.Direction.BottomToTop; // 위로 스크롤할수록 값이 커지는 세로 방향 지정
            scrollbar.handleRect = handleObject.GetComponent<RectTransform>(); // 손잡이 RectTransform 연결
            scrollbar.targetGraphic = handleImage; // 손잡이 클릭 대상 지정

            scrollRect.verticalScrollbar = scrollbar; // ScrollRect에 세로 스크롤바 연결
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport; // 카드가 적을 때는 자동으로 숨김
        }

        private void EnsureEventSystem() // 버튼·스크롤 입력을 처리할 EventSystem을 보장하는 메서드
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem이 있으면 그대로 사용(HandUI 등과 공유)
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System용 EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 이 컴포넌트가 만든 EventSystem 참조 저장
        }

        private static (GameObject gameObject, RectTransform rect, Image image, Transform transform) CreatePanel(string name, Transform parent, Color color) // 단색 UI 패널 생성 보조 메서드
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 패널 GameObject 생성
            panel.transform.SetParent(parent, false); // 부모에 연결
            var rect = panel.GetComponent<RectTransform>(); // RectTransform 확보
            var image = panel.GetComponent<Image>(); // Image 확보
            image.color = color; // 배경색 적용
            image.raycastTarget = false; // 기본값은 입력을 받지 않음(필요한 곳에서 개별적으로 켬)
            return (panel, rect, image, panel.transform); // 구성 요소 반환
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 폭을 넘으면 줄바꿈
            text.verticalOverflow = VerticalWrapMode.Truncate; // 영역을 넘으면 잘라 표시
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

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 고정 크기 UI 요소 위치 설정 보조 메서드
        {
            rect.anchorMin = anchor; // 최소 앵커 설정
            rect.anchorMax = anchor; // 최대 앵커 동일 설정
            rect.pivot = anchor; // 지정한 앵커를 기준점으로도 사용
            rect.anchoredPosition = position; // 앵커 기준 위치 적용
            rect.sizeDelta = size; // 고정 크기 적용
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

        private void OnDestroy() // 컴포넌트가 파괴될 때 이벤트와 직접 생성한 EventSystem을 정리하는 메서드
        {
            if (_boardInput != null) // 입력 컨트롤러가 연결돼 있으면
            {
                _boardInput.DeckChanged -= HandleDeckChanged; // 덱 변경 이벤트 구독 해제
            }

            if (_createdEventSystem != null) // 이 컴포넌트가 만든 EventSystem이 남아 있으면
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode에서는 안전하게 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode에서는 즉시 제거
            }
        }
    }
}
