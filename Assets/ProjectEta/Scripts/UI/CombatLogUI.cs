using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Vector2, Color 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // EventSystem을 런타임 생성하기 위한 네임스페이스
using UnityEngine.InputSystem.UI; // 새 Input System 기반 UI 포인터 입력 모듈을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas, Button, ScrollRect, VerticalLayoutGroup 등을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks, CombatResult, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class CombatLogUI : MonoBehaviour // 32일차: 채팅창처럼 접혀 있다가 눌렀을 때 위로 펼쳐지는 전투 로그 패널을 관리하는 컴포넌트
    {
        private const int MaxEntries = 200; // 무한정 쌓이지 않도록 유지할 최대 로그 줄 수

        public bool IsExpanded => _isExpanded; // 테스트와 디버그에서 패널이 펼쳐져 있는지 확인하는 프로퍼티
        public int EntryCount => _entries.Count; // 테스트에서 현재 쌓인 로그 줄 수를 확인하는 프로퍼티
        public IReadOnlyList<string> Entries => _entries; // 테스트에서 실제 로그 문구 내용을 확인하는 프로퍼티

        private BoardInputController _boardInput; // 실제 전투 훅을 제공하는 입력 컨트롤러
        private Canvas _canvas; // 이 UI 전용 Screen Space Overlay Canvas
        private GameObject _expandedPanel; // 위로 펼쳐지는 로그 목록 패널 루트(토글에 따라 켜고 끔)
        private RectTransform _logContent; // ScrollRect 내부의 실제 로그 텍스트 컨테이너
        private ScrollRect _scrollRect; // 로그 목록 스크롤 컴포넌트
        private readonly List<string> _entries = new List<string>(); // 지금까지 쌓인 로그 문구 목록
        private EventSystem _createdEventSystem; // 이 컴포넌트가 직접 만든 EventSystem 참조
        private bool _isExpanded; // 현재 로그 패널이 펼쳐져 있는지 여부
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시

        public void Bind(BoardInputController boardInput) // 실제 BoardInputController와 전투 훅을 이 UI에 연결하는 메서드
        {
            UnsubscribeHooks(); // 이전 연결이 있었다면 먼저 구독 해제

            _boardInput = boardInput; // 새 실제 입력 컨트롤러 저장
            EnsureUI(); // 버튼·패널 UI를 런타임 생성(최초 1회)

            if (_boardInput != null && _boardInput.BattleHooks != null) // 정상 입력 컨트롤러와 훅 버스가 전달됐다면
            {
                _boardInput.BattleHooks.AfterMove += HandleAfterMove; // 이동 결과 기록을 위해 구독
                _boardInput.BattleHooks.AfterAttack += HandleAfterAttack; // 공격 결과 기록을 위해 구독
                _boardInput.BattleHooks.AfterDamage += HandleAfterDamage; // 상태 이상 등 비전투 피해 기록을 위해 구독
                _boardInput.BattleHooks.TurnStart += HandleTurnStart; // 새 턴 구분선 기록을 위해 구독
            }
        }

        private void UnsubscribeHooks() // 이전에 연결된 훅 구독을 모두 해제하는 공통 메서드
        {
            if (_boardInput == null || _boardInput.BattleHooks == null) return; // 연결된 게 없으면 할 일이 없음

            _boardInput.BattleHooks.AfterMove -= HandleAfterMove; // 이동 훅 구독 해제
            _boardInput.BattleHooks.AfterAttack -= HandleAfterAttack; // 공격 훅 구독 해제
            _boardInput.BattleHooks.AfterDamage -= HandleAfterDamage; // 피해 훅 구독 해제
            _boardInput.BattleHooks.TurnStart -= HandleTurnStart; // 턴 시작 훅 구독 해제
        }

        private void HandleAfterMove(PieceRuntimeState piece, Vector2Int origin, Vector2Int destination) // 기물이 실제로 이동을 마쳤을 때 호출되는 이벤트 처리 메서드
        {
            AddEntry($"{GetPieceName(piece)} 이동: {FormatCell(origin)} → {FormatCell(destination)}"); // 이동 결과를 로그 한 줄로 기록
        }

        private void HandleAfterAttack(CombatResult result) // 공격 판정과 사망 처리까지 모두 끝난 뒤 호출되는 이벤트 처리 메서드
        {
            if (result == null) return; // 결과가 없으면 기록하지 않음

            string outcome = result.DefenderDied ? " (처치)" : $" (생존, 남은 HP {result.Defender.CurrentHp})"; // 처치 여부에 따라 다른 결과 문구
            AddEntry($"{GetPieceName(result.Attacker)} → {GetPieceName(result.Defender)}: {result.DamageDealt} 피해{outcome}"); // 공격 결과를 로그 한 줄로 기록
        }

        private void HandleAfterDamage(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount) // 실제로 HP가 깎일 때마다 호출되는 이벤트 처리 메서드(32일차: 발생원 매개변수로 전투/상태이상 피해 구분)
        {
            if (appliedAmount <= 0) return; // 실제 피해가 없으면 기록하지 않음
            if (source != null) return; // 발생원이 있는 전투 피해는 AfterAttack에서 이미 기록되므로 중복 방지(상태 이상 틱 피해는 source가 항상 null)

            AddEntry($"{GetPieceName(target)} 상태 이상 피해 {appliedAmount}, 남은 HP {target.CurrentHp}"); // 독·화상 등 턴 종료 틱 피해를 로그 한 줄로 기록
        }

        private void HandleTurnStart(TurnState state, int turnNumber) // 새 일반 턴이 시작될 때 호출되는 이벤트 처리 메서드
        {
            AddEntry($"— {turnNumber}턴 시작 —"); // 턴 구분을 위한 로그 한 줄 추가
        }

        private static string GetPieceName(PieceRuntimeState piece) // 로그 문구에 쓸 기물 이름을 안전하게 계산하는 메서드
        {
            if (piece == null || piece.Definition == null) return "알 수 없음"; // 데이터가 없으면 안전한 기본값
            return string.IsNullOrEmpty(piece.Definition.DisplayName) ? piece.Definition.name : piece.Definition.DisplayName; // 표시 이름 우선 사용
        }

        private static string FormatCell(Vector2Int cell) // 좌표를 로그에 쓰기 좋은 짧은 문자열로 바꾸는 메서드
        {
            return $"({cell.x},{cell.y})"; // 괄호 좌표 형식
        }

        private void AddEntry(string text) // 로그 한 줄을 목록에 추가하고 화면에 반영하는 공통 메서드
        {
            _entries.Add(text); // 내부 목록에 추가
            if (_entries.Count > MaxEntries) // 최대 보관 줄 수를 넘으면
            {
                _entries.RemoveAt(0); // 가장 오래된 줄부터 제거
            }

            if (_isExpanded) // 패널이 펼쳐져 있으면 즉시 화면에도 반영
            {
                RebuildLogText(); // 로그 전체를 다시 그림
            }
        }

        private void ToggleExpanded() // 로그 바를 눌렀을 때 펼치거나 접는 메서드
        {
            _isExpanded = !_isExpanded; // 상태 반전
            _expandedPanel.SetActive(_isExpanded); // 펼쳐진 패널 표시 여부 갱신

            if (_isExpanded) // 이번에 펼쳐졌다면
            {
                RebuildLogText(); // 현재까지 쌓인 로그를 다시 그림
            }
        }

        private void RebuildLogText() // 현재 쌓인 로그 전체로 목록 텍스트를 다시 구성하고 맨 아래로 스크롤하는 메서드
        {
            for (int i = _logContent.childCount - 1; i >= 0; i--) // 기존에 그려진 로그 줄을 모두 제거
            {
                var child = _logContent.GetChild(i).gameObject; // 제거할 자식 오브젝트
                if (Application.isPlaying) Destroy(child); // Play Mode에서는 프레임 종료 시 안전하게 제거
                else DestroyImmediate(child); // EditMode 테스트에서는 즉시 제거
            }

            foreach (var entry in _entries) // 현재 로그를 오래된 순서대로 다시 그림
            {
                var lineText = CreateText("LogLine", _logContent, 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.88f, 0.88f, 0.88f, 1f)); // 로그 한 줄 텍스트 생성
                lineText.text = entry; // 실제 로그 문구 적용
                lineText.horizontalOverflow = HorizontalWrapMode.Wrap; // 폭을 넘으면 줄바꿈
                var lineRect = lineText.rectTransform; // 레이아웃 계산용 RectTransform
                var fitter = lineText.gameObject.AddComponent<ContentSizeFitter>(); // 줄바꿈된 실제 높이만큼 셀 크기를 맞추기 위한 Fitter
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 세로 크기를 텍스트 내용에 맞춤
                var layoutElement = lineText.gameObject.AddComponent<LayoutElement>(); // VerticalLayoutGroup이 폭을 인식하도록 요소 추가
                layoutElement.minWidth = 300f; // 목록 폭에 맞춘 최소 너비 지정
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_logContent); // 줄 수 변경 직후 스크롤 범위 즉시 갱신
            if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f; // 새 로그가 보이도록 맨 아래(가장 최근)로 스크롤
        }

        private void EnsureUI() // 로그 바·펼침 패널·Canvas를 한 번만 만드는 메서드
        {
            if (_canvas != null) return; // 이미 생성됐으면 중복 생성하지 않음
            EnsureEventSystem(); // 버튼 클릭·스크롤 입력을 처리할 EventSystem 보장

            var canvasObject = new GameObject("CombatLogCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BattleController의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            _canvas.sortingOrder = 94; // 덱 패널(95)보다 아래, 손패(90)보다 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기에 맞춰 UI 보정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildLogBar(canvasObject.transform); // 좌하단 뽑을 카드 버튼 위, 채팅창 같은 한 줄 막대 생성
            BuildExpandedPanel(canvasObject.transform); // 막대 위로 펼쳐지는 로그 목록 패널(초기 비활성) 생성
        }

        private void BuildLogBar(Transform parent) // 평소에 보이는 한 줄짜리 로그 바(버튼)를 만드는 메서드
        {
            var barObject = new GameObject("CombatLogBar", typeof(RectTransform), typeof(Image), typeof(Button)); // 로그 바 GameObject 생성
            barObject.transform.SetParent(parent, false); // Canvas 자식으로 연결
            var rect = barObject.GetComponent<RectTransform>(); // RectTransform 확보
            rect.anchorMin = new Vector2(0f, 0f); // 좌하단 앵커(뽑을 카드 버튼과 같은 기준)
            rect.anchorMax = new Vector2(0f, 0f); // 좌하단 앵커
            rect.pivot = new Vector2(0f, 0f); // 좌하단 피벗
            rect.anchoredPosition = new Vector2(24f, 106f); // 뽑을 카드 버튼(높이 74, y=24) 바로 위에 배치
            rect.sizeDelta = new Vector2(220f, 36f); // 채팅창처럼 얇고 긴 한 줄 크기

            var image = barObject.GetComponent<Image>(); // 배경 Image 확보
            image.color = new Color(0.14f, 0.14f, 0.17f, 0.92f); // 어두운 채팅창 톤 배경

            var button = barObject.GetComponent<Button>(); // Button 컴포넌트 확보
            button.onClick.AddListener(ToggleExpanded); // 클릭 시 로그 패널 펼치기/접기 토글

            var label = CreateText("CombatLogLabel", barObject.transform, 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 바 문구 텍스트 생성
            Stretch(label.rectTransform, 8f, 4f, 8f, 4f); // 바 내부 여백 적용
            label.text = "전투 로그 ▲"; // 위로 펼쳐진다는 것을 암시하는 문구
        }

        private void BuildExpandedPanel(Transform parent) // 로그 바를 누르면 그 위로 펼쳐지는 스크롤 가능한 목록 패널을 만드는 메서드
        {
            var panelObject = new GameObject("CombatLogExpandedPanel", typeof(RectTransform), typeof(Image)); // 펼침 패널 GameObject 생성
            panelObject.transform.SetParent(parent, false); // Canvas 자식으로 연결
            _expandedPanel = panelObject; // 토글 대상 루트로 저장
            var rect = panelObject.GetComponent<RectTransform>(); // RectTransform 확보
            rect.anchorMin = new Vector2(0f, 0f); // 로그 바와 같은 좌하단 기준 앵커
            rect.anchorMax = new Vector2(0f, 0f); // 좌하단 앵커
            rect.pivot = new Vector2(0f, 0f); // 좌하단 피벗
            rect.anchoredPosition = new Vector2(24f, 146f); // 로그 바(y=106, 높이 36) 바로 위에서부터 위로 펼쳐짐
            rect.sizeDelta = new Vector2(340f, 420f); // 여러 줄을 볼 수 있는 세로로 긴 패널 크기

            var image = panelObject.GetComponent<Image>(); // 배경 Image 확보
            image.color = new Color(0.09f, 0.1f, 0.13f, 0.96f); // 채팅창 본문과 같은 톤의 어두운 배경
            image.raycastTarget = true; // 패널 위 휠 스크롤이 아래 보드로 새지 않도록 자체적으로 Raycast 소비
            AddOutline(panelObject, new Color(0.5f, 0.5f, 0.55f, 0.8f), new Vector2(1.5f, -1.5f)); // 옅은 외곽선으로 패널 경계 표시

            BuildScrollArea(panelObject.transform); // 실제 로그 목록을 담을 세로 스크롤 영역 생성

            panelObject.SetActive(false); // 처음에는 접힌 상태로 시작
        }

        private void BuildScrollArea(Transform parent) // 로그 줄을 세로로 스크롤해서 볼 수 있는 ScrollRect를 만드는 메서드
        {
            var scrollObject = new GameObject("LogScrollView", typeof(RectTransform), typeof(ScrollRect)); // ScrollRect 루트 생성
            scrollObject.transform.SetParent(parent, false); // 펼침 패널 자식으로 연결
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>(); // ScrollRect 루트 RectTransform 확보
            Stretch(scrollRectTransform, 8f, 8f, 8f, 8f); // 패널 안쪽 여백만큼 띄워 전체 영역 사용

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // 스크롤 영역 바깥을 가리는 Viewport 생성
            viewportObject.transform.SetParent(scrollObject.transform, false); // ScrollRect 자식으로 연결
            var viewportRect = viewportObject.GetComponent<RectTransform>(); // Viewport RectTransform 확보
            Stretch(viewportRect, 0f, 0f, 0f, 0f); // 부모 영역 전체 사용
            var viewportImage = viewportObject.GetComponent<Image>(); // Mask가 참조할 Image 확보
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f); // 거의 투명하지만 Raycast는 받도록 최소 알파 유지
            viewportObject.GetComponent<Mask>().showMaskGraphic = false; // 뷰포트 자체 이미지는 화면에 그리지 않음

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); // 실제 로그 줄을 담을 Content 생성
            contentObject.transform.SetParent(viewportObject.transform, false); // Viewport 자식으로 연결
            _logContent = contentObject.GetComponent<RectTransform>(); // Content RectTransform 저장
            _logContent.anchorMin = new Vector2(0f, 1f); // 좌상단 기준 앵커(위에서부터 로그가 쌓이도록)
            _logContent.anchorMax = new Vector2(1f, 1f); // 가로는 부모 전체를 사용
            _logContent.pivot = new Vector2(0.5f, 1f); // 상단 중앙 피벗
            _logContent.anchoredPosition = Vector2.zero; // 기본 위치 사용
            _logContent.sizeDelta = new Vector2(0f, 0f); // 실제 높이는 VerticalLayoutGroup·ContentSizeFitter가 계산

            var layoutGroup = contentObject.GetComponent<VerticalLayoutGroup>(); // 로그 줄을 세로로 쌓는 레이아웃 확보
            layoutGroup.childAlignment = TextAnchor.UpperLeft; // 왼쪽 정렬로 줄글처럼 표시
            layoutGroup.childControlWidth = true; // 자식 폭을 부모에 맞춰 자동 조정
            layoutGroup.childControlHeight = true; // 자식 높이를 텍스트 내용에 맞춰 자동 조정
            layoutGroup.childForceExpandWidth = true; // 폭은 항상 꽉 채움
            layoutGroup.childForceExpandHeight = false; // 높이는 내용만큼만 차지
            layoutGroup.spacing = 4f; // 로그 줄 사이 간격
            layoutGroup.padding = new RectOffset(6, 6, 6, 6); // 목록 안쪽 여백

            var fitter = contentObject.GetComponent<ContentSizeFitter>(); // Content 높이 자동 계산용 Fitter 확보
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 로그 줄 수에 맞춰 세로 크기 자동 확장
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 가로는 부모(뷰포트) 폭을 그대로 사용

            _scrollRect = scrollObject.GetComponent<ScrollRect>(); // ScrollRect 컴포넌트 확보
            _scrollRect.viewport = viewportRect; // 뷰포트 연결
            _scrollRect.content = _logContent; // 실제 스크롤 대상 Content 연결
            _scrollRect.horizontal = false; // 가로 스크롤은 사용하지 않음
            _scrollRect.vertical = true; // 세로 스크롤(마우스 휠)만 사용
            _scrollRect.movementType = ScrollRect.MovementType.Clamped; // 목록 범위를 벗어나지 않도록 제한
            _scrollRect.scrollSensitivity = 24f; // 마우스 휠 스크롤 감도
        }

        private void EnsureEventSystem() // 버튼 클릭·휠 스크롤 입력을 처리할 EventSystem을 보장하는 메서드
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem이 있으면 그대로 사용(HandUI·DeckPanelUI 등과 공유)
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System용 EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 이 컴포넌트가 만든 EventSystem 참조 저장
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
            UnsubscribeHooks(); // 남아 있는 훅 구독 모두 해제

            if (_createdEventSystem != null) // 이 컴포넌트가 만든 EventSystem이 남아 있으면
            {
                if (Application.isPlaying) Destroy(_createdEventSystem.gameObject); // Play Mode에서는 안전하게 제거
                else DestroyImmediate(_createdEventSystem.gameObject); // EditMode에서는 즉시 제거
            }
        }
    }
}
