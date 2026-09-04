using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Vector2, Color 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // EventSystem을 런타임 생성하기 위한 네임스페이스
using UnityEngine.InputSystem.UI; // 새 Input System 기반 UI 포인터 입력 모듈을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, Image, Text 등을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks, CombatResult, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceRoleTag, StatusEffectType을 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class PieceInfoPanelUI : MonoBehaviour // 31일차: 우측 상단에서 현재 선택된 기물의 실시간 정보를 보여주는 컴포넌트
    {
        public bool IsPanelVisible => _panelRoot != null && _panelRoot.activeSelf; // 테스트와 디버그에서 패널이 열려 있는지 확인하는 프로퍼티

        private BoardInputController _boardInput; // 실제 선택 상태를 제공하는 입력 컨트롤러
        private PieceRuntimeState _displayedPiece; // 현재 패널에 표시 중인 기물(훅 갱신 대상 판별용)
        private Canvas _canvas; // 이 UI 전용 Screen Space Overlay Canvas
        private GameObject _panelRoot; // 정보 패널 전체 루트(선택 여부에 따라 켜고 끔)
        private Image _artworkImage; // 카드 아트 이미지
        private Text _portraitPlaceholderText; // Artwork가 없을 때 표시할 기물 약칭
        private Text _nameText; // 기물 이름 텍스트
        private Text _gradeText; // 등급 텍스트
        private Text _atkText; // 공격력 텍스트
        private Text _hpText; // 현재 체력 / 기본 체력 텍스트
        private Text _roleTagsText; // 역할 태그 요약 텍스트
        private Text _statusEffectsText; // 현재 걸린 상태 이상 요약 텍스트
        private Text _descriptionText; // 기물 설명 텍스트
        private EventSystem _createdEventSystem; // 이 컴포넌트가 직접 만든 EventSystem 참조
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시

        public void Bind(BoardInputController boardInput) // 실제 BoardInputController와 선택 상태를 이 UI에 연결하는 메서드
        {
            if (_boardInput != null) // 이전에 연결된 입력 컨트롤러가 있었다면
            {
                _boardInput.SelectionChanged -= HandleSelectionChanged; // 이전 선택 변경 이벤트 구독 해제
                if (_boardInput.BattleHooks != null) // 이전 훅 버스가 있었다면
                {
                    _boardInput.BattleHooks.AfterDamage -= HandleAfterDamage; // 이전 피해 훅 구독 해제
                    _boardInput.BattleHooks.TurnEnd -= HandleTurnEnd; // 이전 턴 종료 훅 구독 해제
                }
            }

            _boardInput = boardInput; // 새 실제 입력 컨트롤러 저장
            EnsureUI(); // 패널 UI를 런타임 생성(최초 1회)

            if (_boardInput != null) // 정상 입력 컨트롤러가 전달됐다면
            {
                _boardInput.SelectionChanged += HandleSelectionChanged; // 기물 선택/해제 이벤트 구독
                if (_boardInput.BattleHooks != null) // 훅 버스가 실제로 연결돼 있으면
                {
                    _boardInput.BattleHooks.AfterDamage += HandleAfterDamage; // 표시 중인 기물이 피해를 받으면 즉시 갱신하기 위해 구독
                    _boardInput.BattleHooks.TurnEnd += HandleTurnEnd; // 상태 이상 지속 턴·중첩 변화를 반영하기 위해 구독
                }
            }

            Refresh(null); // 처음에는 선택된 기물이 없는 상태로 시작
        }

        private void HandleSelectionChanged(PieceRuntimeState piece) // 보드 위 기물 선택이 바뀌거나 해제될 때 호출되는 이벤트 처리 메서드
        {
            Refresh(piece); // 새로 선택된(또는 null) 기물 기준으로 패널을 다시 그림
        }

        private void HandleAfterDamage(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount) // 29일차 훅: 누군가 피해를 입을 때마다 호출되는 이벤트 처리 메서드(32일차: 발생원 매개변수 추가)
        {
            if (_displayedPiece != null && target == _displayedPiece) // 지금 패널에 표시 중인 바로 그 기물이 피해를 입었으면
            {
                Refresh(_displayedPiece); // 갱신된 HP를 즉시 반영
            }
        }

        private void HandleTurnEnd(TurnState state, int turnNumber) // 29일차 훅: 1턴(플레이어+적 행동)이 끝날 때마다 호출되는 이벤트 처리 메서드
        {
            if (_displayedPiece != null) // 지금 표시 중인 기물이 있으면
            {
                Refresh(_displayedPiece); // 상태 이상 지속 턴·중첩 변화를 즉시 반영
            }
        }

        private void Refresh(PieceRuntimeState piece) // 지정한 기물(또는 null) 기준으로 패널 전체 내용을 갱신하는 메서드
        {
            _displayedPiece = piece; // 훅 갱신 대상 판별을 위해 현재 표시 기물 저장
            if (_panelRoot != null) _panelRoot.SetActive(piece != null); // 선택된 기물이 없으면 패널 전체를 숨김
            if (piece == null) return; // 표시할 내용이 없으면 여기서 종료

            var definition = piece.Definition; // 이 기물의 고정 데이터
            bool hasArtwork = definition != null && definition.CardArtwork != null; // 실제 Artwork 연결 여부 확인

            if (_artworkImage != null) // 아트 이미지가 존재하면
            {
                _artworkImage.enabled = hasArtwork; // Artwork가 있을 때만 이미지 표시
                _artworkImage.sprite = hasArtwork ? definition.CardArtwork : null; // Artwork 적용(없으면 비움)
            }

            if (_portraitPlaceholderText != null) // 약칭 텍스트가 존재하면
            {
                _portraitPlaceholderText.gameObject.SetActive(!hasArtwork); // Artwork가 없을 때만 약칭 표시
                _portraitPlaceholderText.text = hasArtwork ? "" : GetPortraitPlaceholder(definition); // 약칭 문구 적용
            }

            _nameText.text = definition != null ? (string.IsNullOrEmpty(definition.DisplayName) ? definition.name : definition.DisplayName) : "이름 없음"; // 기물 이름 표시
            _gradeText.text = definition != null ? $"{Mathf.Max(1, (int)definition.Grade)}성" : "-"; // 등급 표시
            _atkText.text = definition != null ? $"ATK {definition.BaseAtk}" : "ATK 0"; // 공격력 표시
            _hpText.text = definition != null ? $"HP {piece.CurrentHp} / {definition.BaseHp}" : $"HP {piece.CurrentHp}"; // 31일차: 기본 체력이 아닌 현재 체력을 최대치와 함께 표시
            _roleTagsText.text = definition != null ? BuildRoleTagsLabel(definition.RoleTags) : "-"; // 역할 태그 한글 요약 표시
            _statusEffectsText.text = BuildStatusEffectsLabel(piece); // 27~28일차 상태 이상 목록을 그대로 요약해 표시
            _descriptionText.text = definition != null ? definition.Description : ""; // 기물 설명 표시
        }

        private static string BuildRoleTagsLabel(PieceRoleTag tags) // 역할 태그 비트 플래그를 한글 요약 문구로 바꾸는 메서드
        {
            if (tags == PieceRoleTag.None) return "-"; // 역할이 없으면 대시로 표시

            var labels = new List<string>(); // 보유한 역할 이름을 순서대로 모을 목록
            if ((tags & PieceRoleTag.Melee) != 0) labels.Add("근접"); // 근접 역할
            if ((tags & PieceRoleTag.Ranged) != 0) labels.Add("원거리"); // 원거리 역할
            if ((tags & PieceRoleTag.Jumper) != 0) labels.Add("도약"); // 도약 역할
            if ((tags & PieceRoleTag.Slider) != 0) labels.Add("슬라이더"); // 슬라이드 역할
            if ((tags & PieceRoleTag.Rider) != 0) labels.Add("라이더"); // 라이더 역할
            if ((tags & PieceRoleTag.Support) != 0) labels.Add("지원"); // 지원 역할
            if ((tags & PieceRoleTag.Tanker) != 0) labels.Add("탱커"); // 탱커 역할
            if ((tags & PieceRoleTag.Attacker) != 0) labels.Add("공격"); // 공격 역할
            if ((tags & PieceRoleTag.Summoner) != 0) labels.Add("소환"); // 소환 역할

            return labels.Count > 0 ? string.Join(" · ", labels) : "-"; // 보유 역할을 가운뎃점으로 이어 표시
        }

        private static string BuildStatusEffectsLabel(PieceRuntimeState piece) // 27~28일차 상태 이상 목록을 한 줄 요약 문구로 바꾸는 메서드
        {
            if (piece.StatusEffects.Count == 0) return "없음"; // 걸린 상태가 없으면 명시적으로 안내

            var parts = new List<string>(); // 상태별 표시 문구를 모을 목록
            foreach (var effect in piece.StatusEffects) // 현재 걸려 있는 모든 상태 이상을 순회
            {
                string name = GetStatusDisplayName(effect.Definition.StatusType); // 상태 종류의 한글 이름
                parts.Add(effect.StackCount > 1 // 중첩이 2 이상이면 중첩 수까지 함께 표시
                    ? $"{name} {effect.StackCount}중첩({effect.RemainingTurns}턴)"
                    : $"{name}({effect.RemainingTurns}턴)");
            }

            return string.Join(", ", parts); // 여러 상태를 쉼표로 이어 표시
        }

        private static string GetStatusDisplayName(StatusEffectType statusType) // 상태 이상 종류를 한글 이름으로 바꾸는 메서드
        {
            switch (statusType) // 종류에 따라 분기
            {
                case StatusEffectType.Poison: return "독"; // 독
                case StatusEffectType.Burn: return "화상"; // 화상
                case StatusEffectType.Stun: return "기절"; // 기절
                case StatusEffectType.Root: return "속박"; // 속박
                default: return statusType.ToString(); // 정의되지 않은 종류는 영문 그대로 표시
            }
        }

        private static string GetPortraitPlaceholder(PieceDefinition definition) // Artwork가 없을 때 표시할 앞 3글자 약칭을 만드는 메서드(CardView와 동일한 규칙)
        {
            if (definition == null) return "?"; // 정의가 없으면 안전한 기본값 반환

            string source = !string.IsNullOrWhiteSpace(definition.PieceId) ? definition.PieceId : definition.name; // PieceId를 우선 사용
            if (string.IsNullOrWhiteSpace(source)) return "?"; // 이름 정보가 없으면 기본값 반환

            source = source.Trim().ToLowerInvariant(); // 앞 3글자를 일관된 소문자로 표시
            if (source.Length <= 3) return source; // 3글자 이하이면 그대로 표시

            return source.Substring(0, 3); // 4글자 이상이면 앞 3글자만 표시
        }

        private void EnsureUI() // 패널·Canvas를 한 번만 만드는 메서드
        {
            if (_canvas != null) return; // 이미 생성됐으면 중복 생성하지 않음
            EnsureEventSystem(); // 패널이 보드 클릭을 가로채려면 EventSystem이 필요하므로 보장

            var canvasObject = new GameObject("PieceInfoPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BattleController의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 97; // 덱 패널(95)보다 위, 합성 패널(96)보다도 위, 턴 상태(100)보다는 아래에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildPanel(canvasObject.transform); // 우측 상단 정보 패널 생성
        }

        private void BuildPanel(Transform parent) // 우측 상단에 배치되는 기물 정보 패널을 만드는 메서드
        {
            var body = CreatePanel("PieceInfoPanelBody", parent, new Color(0.09f, 0.1f, 0.13f, 0.96f)); // 패널 배경 생성
            _panelRoot = body.gameObject; // 패널 전체 루트로 저장(선택 여부에 따라 켜고 끔)
            SetRect(body.rect, new Vector2(1f, 1f), new Vector2(-150f, -170f), new Vector2(280f, 320f)); // 화면 우측 상단에 고정 배치
            body.image.raycastTarget = true; // 패널이 아래 보드 클릭을 가로채지 않도록 자체적으로 Raycast 소비
            var bodyBlocker = body.gameObject.AddComponent<Button>(); // 패널 배경 클릭이 보드 쪽으로 새지 않도록 빈 Button으로 이벤트 소비
            bodyBlocker.transition = Selectable.Transition.None; // 시각적 변화 없이 클릭만 차단하는 용도
            AddOutline(body.gameObject, new Color(0.5f, 0.5f, 0.55f, 0.9f), new Vector2(2f, -2f)); // 옅은 외곽선으로 패널 경계 표시

            var artObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image)); // 카드 아트 이미지 생성
            artObject.transform.SetParent(body.rect, false); // 패널 자식으로 연결
            _artworkImage = artObject.GetComponent<Image>(); // 아트 이미지 참조 저장
            _artworkImage.raycastTarget = false; // 정보 패널은 클릭 대상이 아니므로 Raycast 비활성화
            SetRect(_artworkImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(110f, 96f)); // 패널 상단에 초상화 영역 배치
            _artworkImage.enabled = false; // Artwork가 없을 때는 숨김 상태로 시작

            _portraitPlaceholderText = CreateText("PortraitPlaceholder", artObject.transform, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // Artwork가 없을 때 표시할 약칭 텍스트 생성
            Stretch(_portraitPlaceholderText.rectTransform, 0f, 0f, 0f, 0f); // 초상화 영역 전체 사용

            _nameText = CreateText("NameText", body.rect, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 이름 텍스트 생성
            SetRect(_nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(260f, 26f)); // 초상화 아래 배치

            _gradeText = CreateText("GradeText", body.rect, 13, FontStyle.Bold, new Color(1f, 0.85f, 0.4f, 1f)); // 등급 텍스트 생성
            SetRect(_gradeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -154f), new Vector2(260f, 20f)); // 이름 아래 배치

            var statsRow = new GameObject("StatsRow", typeof(RectTransform)); // ATK/HP를 한 줄에 나란히 배치할 컨테이너
            statsRow.transform.SetParent(body.rect, false); // 패널 자식으로 연결
            SetRect(statsRow.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(260f, 26f)); // 등급 아래 배치

            _atkText = CreateText("AtkText", statsRow.transform, 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.75f, 0.35f, 1f)); // 공격력 텍스트 생성
            SetRect(_atkText.rectTransform, new Vector2(0f, 0.5f), new Vector2(65f, 0f), new Vector2(120f, 26f)); // 왼쪽 절반에 배치
            _atkText.text = "ATK 0"; // 초기 문구(실제 값은 Refresh에서 "ATK "와 함께 재설정)

            _hpText = CreateText("HpText", statsRow.transform, 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.85f, 0.3f, 0.3f, 1f)); // 체력 텍스트 생성
            SetRect(_hpText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-65f, 0f), new Vector2(120f, 26f)); // 오른쪽 절반에 배치

            _roleTagsText = CreateText("RoleTagsText", body.rect, 12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.75f, 0.8f, 0.9f, 1f)); // 역할 태그 텍스트 생성
            SetRect(_roleTagsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -206f), new Vector2(260f, 20f)); // 스탯 아래 배치

            _statusEffectsText = CreateText("StatusEffectsText", body.rect, 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.7f, 0.95f, 0.6f, 1f)); // 상태 이상 텍스트 생성
            _statusEffectsText.horizontalOverflow = HorizontalWrapMode.Wrap; // 여러 상태가 걸리면 줄바꿈 허용
            SetRect(_statusEffectsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(260f, 36f)); // 역할 태그 아래 배치

            _descriptionText = CreateText("DescriptionText", body.rect, 11, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.85f, 1f)); // 설명 텍스트 생성
            SetRect(_descriptionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(260f, 66f)); // 패널 하단에 여러 줄 설명 배치

            body.gameObject.SetActive(false); // 처음에는 선택된 기물이 없으므로 패널을 숨긴 채 시작
        }

        private void EnsureEventSystem() // 보드 클릭 차단에 필요한 EventSystem을 보장하는 메서드
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem이 있으면 그대로 사용(HandUI·DeckPanelUI·FusionPanelUI 등과 공유)
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System용 EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 이 컴포넌트가 만든 EventSystem 참조 저장
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, Color color) // 가운데 정렬을 기본으로 하는 간이 오버로드
        {
            return CreateText(name, parent, fontSize, style, TextAnchor.MiddleCenter, color); // 정렬만 기본값으로 고정해 위임
        }

        private static (GameObject gameObject, RectTransform rect, Image image) CreatePanel(string name, Transform parent, Color color) // 단색 UI 패널 생성 보조 메서드
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 패널 GameObject 생성
            panel.transform.SetParent(parent, false); // 부모에 연결
            var rect = panel.GetComponent<RectTransform>(); // RectTransform 확보
            var image = panel.GetComponent<Image>(); // Image 확보
            image.color = color; // 배경색 적용
            image.raycastTarget = false; // 기본값은 입력을 받지 않음(필요한 곳에서 개별적으로 켬)
            return (panel, rect, image); // 구성 요소 반환
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 기본은 잘리지 않도록 허용(필요한 곳에서 개별적으로 Wrap 지정)
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
                _boardInput.SelectionChanged -= HandleSelectionChanged; // 선택 변경 이벤트 구독 해제
                if (_boardInput.BattleHooks != null) // 훅 버스가 연결돼 있으면
                {
                    _boardInput.BattleHooks.AfterDamage -= HandleAfterDamage; // 피해 훅 구독 해제
                    _boardInput.BattleHooks.TurnEnd -= HandleTurnEnd; // 턴 종료 훅 구독 해제
                }
            }

            if (_createdEventSystem != null) // 이 컴포넌트가 만든 EventSystem이 남아 있으면
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode에서는 안전하게 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode에서는 즉시 제거
            }
        }
    }
}
