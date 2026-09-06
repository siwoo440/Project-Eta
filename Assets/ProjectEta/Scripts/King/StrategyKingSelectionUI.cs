using System.Collections.Generic; // IReadOnlyList<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.King
{
    public sealed class StrategyKingSelectionUI : MonoBehaviour
    {
        private readonly Button[] _buttons = new Button[StrategyKingAbility.CandidateCount]; // 최대 3장 카드 선택 버튼
        private readonly Text[] _buttonTexts = new Text[StrategyKingAbility.CandidateCount]; // 카드 이름 표시 텍스트
        private Canvas _canvas; // 전략형 선택 전용 Canvas
        private GameObject _root; // 전체 화면 입력 차단 루트
        private Text _statusText; // 전술적 준비 설명 텍스트
        private System.Func<int, bool> _onSelected; // 카드 선택 결과 처리 콜백
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsVisible => _root != null && _root.activeSelf; // 현재 전략형 선택 UI 표시 여부

        public void Show(IReadOnlyList<PieceDefinition> candidates, System.Func<int, bool> onSelected)
        {
            if (candidates == null || candidates.Count == 0) return; // 후보 없는 화면 표시 차단
            EnsureUI(); // 전략형 선택 UI 생성 보장
            _onSelected = onSelected; // 선택 처리 콜백 저장

            for (int i = 0; i < _buttons.Length; i++)
            {
                bool visible = i < candidates.Count; // 현재 후보 수 안의 버튼만 표시
                _buttons[i].gameObject.SetActive(visible); // 후보 유무에 따라 버튼 표시 전환

                if (!visible) continue; // 숨긴 버튼 추가 갱신 생략

                PieceDefinition candidate = candidates[i]; // 현재 카드 후보 조회
                string displayName = candidate != null ? candidate.DisplayName : "알 수 없는 카드"; // 카드 표시 이름 안전 처리
                _buttonTexts[i].text = $"{displayName}\n선택"; // 카드 이름 중심 선택 문구 적용
            }

            _statusText.text = "덱 위 카드 중 1장을 손패로 가져옵니다.\n선택하지 않은 카드는 덱 맨 아래로 이동합니다."; // 패시브 규칙 안내
            _root.SetActive(true); // 전체 화면 전략 선택 UI 표시
        }

        public void Hide()
        {
            _onSelected = null; // 이전 선택 콜백 참조 해제
            if (_root != null) _root.SetActive(false); // 전략형 선택 화면 숨김
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 UI 생성 차단
            EnsureEventSystem(); // 카드 선택 포인터 입력 보장

            var canvasObject = new GameObject("StrategyKingCanvas_Day50", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전략형 선택 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 킹 능력 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 카드 선택 UI 적용
            _canvas.sortingOrder = 230; // 킹 선택 UI보다 위에 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 화면 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용

            BuildRoot(canvasObject.transform); // 전체 선택 화면 생성
        }

        private void BuildRoot(Transform parent)
        {
            _root = new GameObject("StrategyKingSelectionRoot", typeof(RectTransform), typeof(Image)); // 전체 화면 입력 차단 루트 생성
            _root.transform.SetParent(parent, false); // Canvas 자식 연결

            RectTransform rootRect = _root.GetComponent<RectTransform>(); // 전체 화면 RectTransform 조회
            rootRect.anchorMin = Vector2.zero; // 좌하단 Stretch 시작
            rootRect.anchorMax = Vector2.one; // 우상단 Stretch 끝
            rootRect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rootRect.offsetMax = Vector2.zero; // 우상단 여백 제거

            Image blocker = _root.GetComponent<Image>(); // 전체 화면 반투명 입력 차단 이미지 조회
            blocker.color = new Color(0.02f, 0.02f, 0.025f, 0.78f); // 보드를 남겨 보이는 어두운 배경 적용
            blocker.raycastTarget = true; // 뒤쪽 카드·보드 입력 차단

            var panelObject = new GameObject("StrategyPanel", typeof(RectTransform), typeof(Image)); // 중앙 전략 카드 선택 패널 생성
            panelObject.transform.SetParent(_root.transform, false); // 전체 화면 루트 자식 연결
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 중앙 패널 RectTransform 조회
            SetCenteredRect(panelRect, new Vector2(0f, 20f), new Vector2(980f, 430f)); // 중앙 패널 위치·크기 적용

            Image panelImage = panelObject.GetComponent<Image>(); // 중앙 패널 배경 조회
            panelImage.color = new Color(0.10f, 0.08f, 0.16f, 0.98f); // 전략형 보라 계열 패널 적용

            Text title = CreateText("Title", panelObject.transform, 38, FontStyle.Bold); // 전술적 준비 제목 생성
            title.text = "전술적 준비"; // 전략형 패시브 이름 적용
            SetCenteredRect(title.rectTransform, new Vector2(0f, 160f), new Vector2(760f, 60f)); // 제목 위치·크기 적용

            float[] xPositions = { -300f, 0f, 300f }; // 카드 3장 가로 배치 위치

            for (int i = 0; i < _buttons.Length; i++)
            {
                int capturedIndex = i; // 버튼 콜백 후보 인덱스 고정
                _buttons[i] = CreateCardButton(panelObject.transform, xPositions[i], out _buttonTexts[i]); // 카드 선택 버튼 생성
                _buttons[i].onClick.AddListener(() => TryChoose(capturedIndex)); // 카드 선택 콜백 연결
            }

            _statusText = CreateText("Status", panelObject.transform, 20, FontStyle.Normal); // 전략형 상세 안내 생성
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap; // 안내 문구 줄바꿈 허용
            SetCenteredRect(_statusText.rectTransform, new Vector2(0f, -160f), new Vector2(850f, 70f)); // 안내 위치·크기 적용

            _root.SetActive(false); // 기본 상태 전략 선택 UI 숨김
        }

        private Button CreateCardButton(Transform parent, float x, out Text label)
        {
            var buttonObject = new GameObject("Candidate", typeof(RectTransform), typeof(Image), typeof(Button)); // 카드 후보 버튼 생성
            buttonObject.transform.SetParent(parent, false); // 중앙 패널 자식 연결
            SetCenteredRect(buttonObject.GetComponent<RectTransform>(), new Vector2(x, 10f), new Vector2(250f, 210f)); // 카드형 버튼 위치·크기 적용

            Image background = buttonObject.GetComponent<Image>(); // 카드 후보 배경 조회
            background.color = new Color(0.29f, 0.22f, 0.38f, 0.98f); // 전략형 카드 배경 적용

            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = background; // 버튼 대상 그래픽 지정

            label = CreateText("Label", buttonObject.transform, 25, FontStyle.Bold); // 카드 이름 텍스트 생성
            Stretch(label.rectTransform, 12f); // 카드 내부 여백 적용
            return button; // 완성 카드 버튼 반환
        }

        private void TryChoose(int index)
        {
            if (_onSelected == null) return; // 선택 처리 콜백 누락 방어
            if (!_onSelected(index)) return; // 덱 상태 변경 등으로 처리 실패 시 화면 유지
            Hide(); // 정상 선택 완료 후 전략형 선택 화면 닫기
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 글자 적용
            text.raycastTarget = false; // 버튼 포인터 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            return _runtimeFont; // 최종 폰트 반환
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }

        private void OnDestroy()
        {
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
