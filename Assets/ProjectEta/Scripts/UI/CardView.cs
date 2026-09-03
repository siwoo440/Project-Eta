using UnityEngine; // MonoBehaviour, RectTransform, Color, Sprite 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // 카드 포인터·드래그 이벤트 인터페이스를 사용하기 위한 네임스페이스
using UnityEngine.UI; // Image, Text, Mask, LayoutElement, Outline, CanvasGroup을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // 손패 카드 1장을 표시하고 직접 드래그·우클릭 정리하는 컴포넌트
    {
        public PieceDefinition Definition => _definition; // 현재 카드가 표현하는 실제 PieceDefinition
        public bool IsInteractable => _isInteractable; // 현재 턴 규칙상 이 카드를 드래그해 소환할 수 있는지 여부
        public RectTransform RectTransform => _rectTransform; // 카드 루트 RectTransform

        private const float CardWidth = 178f; // 화면 하단 카드 한 장의 기본 너비
        private const float CardHeight = 254f; // 화면 하단 카드 한 장의 기본 높이

        private static Sprite _roundedSprite; // 카드 프레임이 공유할 둥근 사각형 Sprite 캐시
        private static Sprite _circleSprite; // 초상화·스탯 구슬이 공유할 원형 Sprite 캐시
        private static Font _runtimeFont; // 한글 카드 텍스트용 런타임 폰트 캐시

        private HandUI _owner; // 드래그 Drop을 실제 보드 소환으로 연결할 HandUI
        private PieceDefinition _definition; // 이 카드가 표현하는 실제 기물 정의
        private RectTransform _rectTransform; // 카드 루트 RectTransform
        private CanvasGroup _canvasGroup; // 드래그 중 투명도·Raycast 제어 컴포넌트
        private Image _rootImage; // 카드 외곽 프레임 이미지
        private Image _portraitImage; // 실제 카드 일러스트 이미지
        private Text _portraitPlaceholder; // Artwork가 없을 때 표시할 기물 약칭
        private Text _nameText; // 중앙 이름 배너 텍스트
        private Text _descriptionText; // 하단 설명 텍스트
        private Text _gradeText; // 좌상단 등급 수치
        private Text _attackText; // 좌하단 공격력 수치
        private Text _healthText; // 우하단 체력 수치
        private Text _slotText; // 우상단 숫자키 보조 입력 표시
        private GameObject _lockOverlay; // 사용할 수 없는 카드를 어둡게 표시하는 오버레이
        private bool _isInteractable; // 현재 카드 드래그 가능 상태
        private bool _isDragging; // 현재 드래그 중인지 여부
        private Transform _originalParent; // 드래그 전 손패 부모
        private int _originalSiblingIndex; // 드래그 전 손패 정렬 순서
        private Vector2 _originalAnchoredPosition; // 드래그 실패 시 복귀할 원래 위치

        public void Bind(PieceDefinition definition, int handIndex, HandUI owner) // PieceDefinition과 손패 슬롯 번호를 카드 UI에 연결하는 메서드
        {
            _definition = definition; // 실제 카드 데이터 저장
            _owner = owner; // Drop 처리자 저장
            EnsureVisualTree(); // 판타지 카드 프레임 계층이 없으면 런타임 생성
            RefreshVisual(handIndex); // 이미지·이름·설명·스탯·숫자키를 화면에 반영
            SetInteractable(_owner != null && _owner.CanDragCard(definition)); // 현재 턴에 맞는 활성 상태 적용
        }

        public void SetInteractable(bool interactable) // 턴 상태 변화에 따라 카드 활성/잠금 상태를 갱신하는 메서드
        {
            _isInteractable = interactable; // 실제 입력 가능 상태 저장
            if (_lockOverlay != null) _lockOverlay.SetActive(!interactable); // 잠긴 카드에만 반투명 오버레이 표시
            if (_rootImage != null) _rootImage.color = interactable ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f); // 잠긴 카드 명도 감소
        }

        public void OnBeginDrag(PointerEventData eventData) // 카드 드래그 시작 이벤트
        {
            if (eventData.button != PointerEventData.InputButton.Left) return; // 좌클릭 드래그만 카드 소환 입력으로 허용
            if (!_isInteractable || _owner == null || _definition == null) return; // 사용할 수 없는 카드는 드래그하지 않음
            EnsureRequiredComponents(); // 부분 초기화된 런타임 카드라도 CanvasGroup과 RectTransform을 반드시 보장
            _isDragging = true; // 드래그 상태 시작
            _originalParent = transform.parent; // 원래 손패 부모 저장
            _originalSiblingIndex = transform.GetSiblingIndex(); // 원래 손패 순서 저장
            _originalAnchoredPosition = _rectTransform.anchoredPosition; // 원래 위치 저장
            _canvasGroup.blocksRaycasts = false; // 드래그 카드가 보드 프리뷰 판정을 가로채지 않게 함
            _canvasGroup.alpha = 0f; // 드래그 중 카드 UI는 완전히 숨겨 3D 기물 고스트만 보이게 함
            _owner.BeginCardDrag(this, eventData.position); // 실제 커서 위치를 기준으로 보드 Drop 미리보기 시작
        }

        public void OnDrag(PointerEventData eventData) // 드래그 중 매 프레임 호출되는 이벤트
        {
            if (!_isDragging || eventData.button != PointerEventData.InputButton.Left) return; // 좌클릭으로 시작한 실제 드래그만 처리
            _owner.UpdateCardDrag(this, eventData.position); // 숨겨진 카드 대신 커서가 가리키는 보드 칸의 3D 기물 고스트만 갱신
        }

        public void OnEndDrag(PointerEventData eventData) // 좌클릭을 놓아 드래그가 끝날 때 호출되는 이벤트
        {
            if (!_isDragging || eventData.button != PointerEventData.InputButton.Left) return; // 좌클릭 Release만 실제 Drop·소환으로 처리
            _isDragging = false; // 드래그 상태 종료
            EnsureRequiredComponents(); // 드래그 도중 컴포넌트 상태가 바뀌었어도 안전하게 다시 확보
            bool summoned = _owner.TryDropCardFromScreen(this, eventData.position); // 좌클릭 Release 좌표에서만 실제 소환 판정
            _owner.EndCardDrag(); // 손패 위치와 3D 기물 고스트 미리보기 정리
            if (summoned) // 실제 소환에 성공했다면
            {
                gameObject.SetActive(false); // 다음 프레임 손패 UI 재구성 전까지 사용된 카드를 계속 숨김
                return; // 소비된 카드는 원래 손패로 복귀하지 않음
            }

            _canvasGroup.blocksRaycasts = true; // 실패 Drop이면 카드 UI Raycast 복원
            _canvasGroup.alpha = 1f; // 실패 Drop이면 숨겼던 카드 이미지를 다시 표시
            ReturnToHand(); // 잘못된 Drop이면 카드가 원래 손패 위치로 돌아감
        }

        public void OnPointerClick(PointerEventData eventData) // 19일차: 배치 턴에 카드를 우클릭해 드로우 더미 맨 아래로 정리하는 이벤트
        {
            if (eventData.button != PointerEventData.InputButton.Right) return; // 우클릭이 아니면 처리하지 않음(좌클릭은 드래그로 이미 처리)
            if (_owner == null || _definition == null) return; // 연결이 없으면 종료
            if (_owner.TryDiscardCard(this)) gameObject.SetActive(false); // 정리 성공 시 다음 프레임 손패 재구성 전까지 즉시 숨김
        }

        public void OnPointerEnter(PointerEventData eventData) // 마우스가 카드 위에 올라왔을 때 호출되는 이벤트
        {
            if (_isInteractable && !_isDragging) transform.localScale = Vector3.one * 1.05f; // 사용 가능한 카드를 살짝 확대
        }

        public void OnPointerExit(PointerEventData eventData) // 마우스가 카드 밖으로 나갔을 때 호출되는 이벤트
        {
            if (!_isDragging) transform.localScale = Vector3.one; // 기본 크기로 복원
        }

        private void ReturnToHand() // Drop 실패 시 원래 손패 위치로 돌아가는 메서드
        {
            EnsureRequiredComponents(); // 실패 복귀에서도 CanvasGroup 누락 없이 안전하게 복원
            _canvasGroup.blocksRaycasts = true; // 카드 UI 입력 복원
            _canvasGroup.alpha = 1f; // 카드 시각 표시 복원
            if (_originalParent == null) return; // 원래 부모가 없으면 복원하지 못하므로 종료
            transform.SetParent(_originalParent, false); // 손패 컨테이너에 다시 연결
            transform.SetSiblingIndex(Mathf.Clamp(_originalSiblingIndex, 0, Mathf.Max(0, _originalParent.childCount - 1))); // 원래 순서 복원
            _rectTransform.anchoredPosition = _originalAnchoredPosition; // 원래 위치 복원
            transform.localScale = Vector3.one; // 기본 카드 크기 복원
            var parentRect = _originalParent as RectTransform; // 레이아웃 재계산용 RectTransform 변환
            if (parentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect); // 즉시 손패 레이아웃 갱신
        }

        private void EnsureRequiredComponents() // 드래그 처리에서 항상 필요한 핵심 컴포넌트를 개별적으로 보장하는 메서드
        {
            // 버그 수정: "a ?? b" 형태는 파괴된(그러나 C# 참조는 남아있는) Unity 컴포넌트를 null로 인식하지 못해
            // MissingComponentException으로 이어질 수 있다. Unity의 오버로드된 == 연산자가 실제로 호출되는
            // 명시적 null 비교로 바꿔 파괴된 컴포넌트도 항상 다시 확보되도록 한다.
            if (_rectTransform == null) _rectTransform = gameObject.GetComponent<RectTransform>(); // 기존 RectTransform 재확보 시도
            if (_rectTransform == null) _rectTransform = gameObject.AddComponent<RectTransform>(); // 여전히 없으면 새로 추가

            if (_canvasGroup == null) _canvasGroup = gameObject.GetComponent<CanvasGroup>(); // 기존 CanvasGroup 재확보 시도
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>(); // 여전히 없으면 새로 추가
        }

        private void EnsureVisualTree() // 예시 이미지와 비슷한 판타지 카드 프레임 계층을 런타임 생성하는 메서드
        {
            EnsureRequiredComponents(); // RectTransform이 이미 있어도 CanvasGroup 등 드래그 필수 컴포넌트를 반드시 보장
            if (_rootImage != null) return; // 실제 카드 시각 트리가 이미 만들어진 경우에만 중복 생성을 건너뜀
            _rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight); // 카드 크기 적용
            var layout = gameObject.GetComponent<LayoutElement>(); // 손패 레이아웃용 크기 컴포넌트 재확보 시도(버그 수정: ?? 대신 명시적 null 비교 사용)
            if (layout == null) layout = gameObject.AddComponent<LayoutElement>(); // 여전히 없으면 새로 추가
            layout.preferredWidth = CardWidth; // 카드 권장 너비 지정
            layout.preferredHeight = CardHeight; // 카드 권장 높이 지정
            layout.flexibleWidth = 0f; // 자동 가로 확장 방지
            layout.flexibleHeight = 0f; // 자동 세로 확장 방지
            _rootImage = gameObject.GetComponent<Image>(); // 카드 전체 포인터 입력용 Image 재확보 시도(버그 수정: ?? 대신 명시적 null 비교 사용)
            if (_rootImage == null) _rootImage = gameObject.AddComponent<Image>(); // 여전히 없으면 새로 추가
            _rootImage.sprite = GetRoundedSprite(); // 둥근 카드 Sprite 적용
            _rootImage.type = Image.Type.Sliced; // 모서리 형태 유지
            _rootImage.color = Color.white; // 기본 색상 적용
            _rootImage.raycastTarget = true; // 카드 전체에서 드래그 이벤트를 받을 수 있게 함

            var outer = CreatePanel("OuterFrame", transform, new Color(0.075f, 0.09f, 0.12f, 1f)); // 어두운 금속 외곽 프레임 생성
            Stretch(outer.rect, 4f, 4f, 4f, 4f); // 카드 안쪽에 프레임 배치
            outer.image.sprite = GetRoundedSprite(); // 둥근 프레임 적용
            outer.image.type = Image.Type.Sliced; // 크기 변화 대응
            AddOutline(outer.gameObject, new Color(0.52f, 0.43f, 0.28f, 0.95f), new Vector2(2f, -2f)); // 청동 외곽선 적용

            var inner = CreatePanel("InnerFrame", outer.transform, new Color(0.13f, 0.17f, 0.20f, 1f)); // 청회색 내부 프레임 생성
            Stretch(inner.rect, 7f, 7f, 7f, 7f); // 외곽 프레임 안쪽에 배치
            inner.image.sprite = GetRoundedSprite(); // 둥근 모서리 적용
            inner.image.type = Image.Type.Sliced; // 크기 변화 대응

            var portraitFrame = CreatePanel("PortraitFrame", inner.transform, new Color(0.55f, 0.44f, 0.25f, 1f)); // 상단 초상화 청동 테두리 생성
            SetRect(portraitFrame.rect, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(132f, 132f)); // 카드 상단 중앙에 배치
            portraitFrame.image.sprite = GetCircleSprite(); // 원형 초상화 프레임 적용

            var portraitMask = CreatePanel("PortraitMask", portraitFrame.transform, new Color(0.05f, 0.32f, 0.38f, 1f)); // 초상화 내부 청록색 배경 생성
            Stretch(portraitMask.rect, 7f, 7f, 7f, 7f); // 청동 프레임 안쪽으로 여백 적용
            portraitMask.image.sprite = GetCircleSprite(); // 원형 Mask Sprite 적용
            var mask = portraitMask.gameObject.AddComponent<Mask>(); // Artwork가 원 밖으로 넘치지 않도록 Mask 추가
            mask.showMaskGraphic = true; // Artwork가 없을 때 청록 배경 표시

            var artObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image)); // 실제 카드 일러스트 Image 생성
            artObject.transform.SetParent(portraitMask.transform, false); // Mask 안에 연결
            Stretch(artObject.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f); // 초상화 영역 전체 사용
            _portraitImage = artObject.GetComponent<Image>(); // Artwork Image 참조 저장
            _portraitImage.preserveAspect = true; // 원본 이미지 비율 유지
            _portraitImage.raycastTarget = false; // 루트 카드가 포인터 입력을 받도록 비활성화

            _portraitPlaceholder = CreateText("PortraitPlaceholder", portraitMask.transform, 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // Artwork가 없을 때 기물 약칭 표시
            Stretch(_portraitPlaceholder.rectTransform, 0f, 0f, 0f, 0f); // 초상화 중앙 전체 사용

            var banner = CreatePanel("NameBanner", inner.transform, new Color(0.67f, 0.55f, 0.37f, 1f)); // 초상화 아래 이름 리본 생성
            SetRect(banner.rect, new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(158f, 31f)); // 초상화 하단에 겹치게 배치
            AddOutline(banner.gameObject, new Color(0.19f, 0.13f, 0.08f, 0.9f), new Vector2(1f, -1f)); // 이름 리본 경계 표시
            _nameText = CreateText("NameText", banner.transform, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.13f, 0.09f, 0.05f, 1f)); // 이름 Text 생성
            Stretch(_nameText.rectTransform, 6f, 3f, 6f, 3f); // 내부 여백 적용
            _nameText.resizeTextForBestFit = true; // 긴 이름 자동 축소
            _nameText.resizeTextMinSize = 10; // 최소 글자 크기
            _nameText.resizeTextMaxSize = 15; // 최대 글자 크기

            var description = CreatePanel("DescriptionPanel", inner.transform, new Color(0.78f, 0.72f, 0.61f, 1f)); // 하단 양피지 설명 패널 생성
            SetRect(description.rect, new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(150f, 74f)); // 카드 하단 중앙에 배치
            AddOutline(description.gameObject, new Color(0.22f, 0.18f, 0.13f, 0.85f), new Vector2(1f, -1f)); // 설명 영역 경계 표시
            _descriptionText = CreateText("DescriptionText", description.transform, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.11f, 0.09f, 0.07f, 1f)); // 설명 Text 생성
            Stretch(_descriptionText.rectTransform, 7f, 5f, 7f, 5f); // 내부 여백 적용
            _descriptionText.resizeTextForBestFit = true; // 설명 길이에 맞춰 폰트 자동 축소
            _descriptionText.resizeTextMinSize = 9; // 최소 글자 크기
            _descriptionText.resizeTextMaxSize = 12; // 최대 글자 크기

            _gradeText = CreateOrb("GradeOrb", inner.transform, new Vector2(-69f, -23f), new Color(0.12f, 0.36f, 0.70f, 1f), true); // 좌상단 파란 등급 구슬 생성
            _attackText = CreateOrb("AttackOrb", inner.transform, new Vector2(-62f, 19f), new Color(0.87f, 0.64f, 0.14f, 1f), false); // 좌하단 공격력 구슬 생성
            _healthText = CreateOrb("HealthOrb", inner.transform, new Vector2(62f, 19f), new Color(0.66f, 0.12f, 0.12f, 1f), false); // 우하단 체력 구슬 생성

            _slotText = CreateText("SlotKey", inner.transform, 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.94f, 0.94f, 1f)); // 숫자키 보조 입력 표시 Text 생성
            SetRect(_slotText.rectTransform, new Vector2(1f, 1f), new Vector2(-17f, -16f), new Vector2(28f, 24f)); // 우상단에 작게 배치

            var lockPanel = CreatePanel("LockOverlay", transform, new Color(0f, 0f, 0f, 0.56f)); // 사용할 수 없는 카드 잠금 오버레이 생성
            _lockOverlay = lockPanel.gameObject; // 오버레이 참조 저장
            Stretch(lockPanel.rect, 0f, 0f, 0f, 0f); // 카드 전체를 덮게 설정
            lockPanel.image.sprite = GetRoundedSprite(); // 카드 외형과 같은 둥근 Sprite 적용
            lockPanel.image.type = Image.Type.Sliced; // 모서리 형태 유지
        }

        private void RefreshVisual(int handIndex) // PieceDefinition 값을 실제 카드 UI에 반영하는 메서드
        {
            if (_definition == null) return; // 카드 데이터가 없으면 종료
            bool hasArtwork = _definition.CardArtwork != null; // 실제 카드 일러스트 연결 여부 확인
            _portraitImage.sprite = _definition.CardArtwork; // 초상화 Sprite 적용
            _portraitImage.enabled = hasArtwork; // 이미지가 있을 때만 Artwork Image 표시
            _portraitPlaceholder.gameObject.SetActive(!hasArtwork); // Artwork가 없으면 기물 약칭 표시
            _portraitPlaceholder.text = GetPortraitPlaceholder(_definition); // 기물 약칭 적용
            _nameText.text = string.IsNullOrEmpty(_definition.DisplayName) ? _definition.name : _definition.DisplayName; // 카드 이름 적용
            _descriptionText.text = string.IsNullOrEmpty(_definition.Description) ? "기물 카드\n드래그하여 보드에 소환" : _definition.Description; // 설명 또는 기본 안내 적용
            _gradeText.text = Mathf.Max(1, (int)_definition.Grade).ToString(); // 좌상단에 등급 숫자 표시
            _attackText.text = _definition.BaseAtk.ToString(); // 좌하단 공격력 표시
            _healthText.text = _definition.BaseHp.ToString(); // 우하단 체력 표시
            _slotText.text = handIndex == 9 ? "0" : (handIndex + 1).ToString(); // 숫자키 1~0 보조 입력 표시
        }

        private static string GetPortraitPlaceholder(PieceDefinition definition) // Artwork가 없을 때 초상화 중앙에 표시할 약칭을 만드는 메서드
        {
            switch (definition.MovementType) // 이동 타입에 따라 약칭 결정
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

        private static (GameObject gameObject, RectTransform rect, Image image, Transform transform) CreatePanel(string name, Transform parent, Color color) // 단색 UI 패널 생성 보조 메서드
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 패널 GameObject 생성
            panel.transform.SetParent(parent, false); // 부모에 연결
            var rect = panel.GetComponent<RectTransform>(); // RectTransform 확보
            var image = panel.GetComponent<Image>(); // Image 확보
            image.color = color; // 배경색 적용
            image.raycastTarget = false; // 카드 루트가 입력을 받도록 자식 Raycast 비활성화
            return (panel, rect, image, panel.transform); // 구성 요소 반환
        }

        private static Text CreateOrb(string name, Transform parent, Vector2 position, Color color, bool anchorTop) // 등급·공격력·체력 원형 구슬 생성 보조 메서드
        {
            var orb = CreatePanel(name, parent, color); // 원형 배경 패널 생성
            orb.image.sprite = GetCircleSprite(); // 원형 Sprite 적용
            Vector2 anchor = anchorTop ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f); // 상단/하단 기준 앵커 선택
            SetRect(orb.rect, anchor, position, new Vector2(42f, 42f)); // 구슬 크기와 위치 적용
            AddOutline(orb.gameObject, new Color(0.06f, 0.06f, 0.06f, 0.95f), new Vector2(2f, -2f)); // 어두운 외곽선 적용
            var text = CreateText(name + "Text", orb.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 구슬 수치 Text 생성
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f); // 구슬 전체 영역 사용
            AddOutline(text.gameObject, new Color(0f, 0f, 0f, 0.75f), new Vector2(1f, -1f)); // 수치 가독성 외곽선 적용
            return text; // 수치 Text 반환
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
            text.raycastTarget = false; // 카드 루트가 드래그 이벤트를 받도록 비활성화
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
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 사용
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

        private static Font GetRuntimeFont() // 한글 카드 텍스트를 표시할 런타임 폰트를 만드는 메서드
        {
            if (_runtimeFont != null) return _runtimeFont; // 이미 생성됐으면 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 한글 시스템 폰트 우선 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            return _runtimeFont; // 최종 폰트 반환
        }

        private static Sprite GetRoundedSprite() // 둥근 카드 실루엣용 런타임 Sprite를 만드는 메서드
        {
            if (_roundedSprite != null) return _roundedSprite; // 이미 있으면 캐시 재사용
            const int size = 64; // 텍스처 크기
            const float radius = 11f; // 모서리 반경
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); // 알파 텍스처 생성
            texture.name = "ProjectEtaRoundedCardRuntime"; // 런타임 디버그 이름 지정
            texture.wrapMode = TextureWrapMode.Clamp; // 테두리 반복 방지
            for (int y = 0; y < size; y++) // 세로 픽셀 순회
            {
                for (int x = 0; x < size; x++) // 가로 픽셀 순회
                {
                    float dx = Mathf.Max(radius - x, 0f, x - (size - 1 - radius)); // 좌우 모서리 거리 계산
                    float dy = Mathf.Max(radius - y, 0f, y - (size - 1 - radius)); // 상하 모서리 거리 계산
                    float alpha = dx * dx + dy * dy <= radius * radius ? 1f : 0f; // 둥근 모서리 안쪽만 불투명 처리
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // 픽셀 기록
                }
            }
            texture.Apply(); // 픽셀 데이터 반영
            _roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14f, 14f, 14f, 14f)); // Sliced 가능한 Sprite 생성
            _roundedSprite.name = "ProjectEtaRoundedCardSpriteRuntime"; // Sprite 이름 지정
            return _roundedSprite; // 완성 Sprite 반환
        }

        private static Sprite GetCircleSprite() // 원형 초상화·구슬용 런타임 Sprite를 만드는 메서드
        {
            if (_circleSprite != null) return _circleSprite; // 이미 있으면 캐시 재사용
            const int size = 64; // 텍스처 크기
            float center = (size - 1) * 0.5f; // 원 중심 좌표
            float radius = center - 1f; // 원 반경
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); // 알파 텍스처 생성
            texture.name = "ProjectEtaCircleRuntime"; // 런타임 디버그 이름 지정
            texture.wrapMode = TextureWrapMode.Clamp; // 외곽 반복 방지
            for (int y = 0; y < size; y++) // 세로 픽셀 순회
            {
                for (int x = 0; x < size; x++) // 가로 픽셀 순회
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)); // 중심과 픽셀 거리 계산
                    float alpha = distance <= radius ? 1f : 0f; // 원 안쪽만 불투명 처리
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha)); // 픽셀 기록
                }
            }
            texture.Apply(); // 픽셀 데이터 반영
            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f); // 원형 Sprite 생성
            _circleSprite.name = "ProjectEtaCircleSpriteRuntime"; // Sprite 이름 지정
            return _circleSprite; // 완성 Sprite 반환
        }
    }
}
