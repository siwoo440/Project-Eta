using System.Collections.Generic; // List<T>·IReadOnlyList<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // CardRewardSource 사용

namespace ProjectEta.UI // 카드 보상 UI 네임스페이스
{
    public sealed class CardRewardUI : MonoBehaviour // 전투 승리·Reward 노드에서 카드 3장 중 1장을 고르는 개발용 UI
    {
        private const float CardWidth = 250f; // 보상 카드 버튼 폭
        private const float CardHeight = 330f; // 보상 카드 버튼 높이
        private const float CardGap = 28f; // 카드 사이 간격

        private readonly List<GameObject> _cardObjects = new List<GameObject>(); // 현재 생성된 후보 카드 UI 목록
        private Canvas _canvas; // 카드 보상 전용 Canvas
        private GameObject _root; // 전체 화면 보상 루트
        private Text _titleText; // 보상 제목
        private System.Action<PieceDefinition> _selectionCallback; // 카드 선택 완료 콜백
        private EventSystem _createdEventSystem; // 직접 생성한 EventSystem 참조
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsVisible => _root != null && _root.activeSelf; // 현재 카드 보상 화면 표시 여부

        public void Show(IReadOnlyList<PieceDefinition> candidates, CardRewardSource source, System.Action<PieceDefinition> selectionCallback) // 카드 후보 화면 표시
        {
            EnsureUI(); // 보상 Canvas 최초 생성
            ClearCardObjects(); // 이전 후보 UI 제거
            _selectionCallback = selectionCallback; // 현재 선택 콜백 저장
            _titleText.text = source == CardRewardSource.RewardNode ? "카드 보상 스테이지" : "전투 승리 보상"; // 보상 발생 경로 표시

            int count = candidates != null ? candidates.Count : 0; // 실제 후보 수 계산
            float totalWidth = count > 0 ? CardWidth * count + CardGap * (count - 1) : 0f; // 후보 카드 전체 폭 계산
            float startX = -totalWidth * 0.5f + CardWidth * 0.5f; // 첫 카드 중심 X 계산

            for (int i = 0; i < count; i++) // 후보 카드 순회
            {
                PieceDefinition definition = candidates[i]; // 현재 후보 정의 조회
                if (definition == null) continue; // 빈 후보 제외
                float x = startX + i * (CardWidth + CardGap); // 현재 카드 화면 X 계산
                CreateRewardCard(definition, x); // 실제 선택 카드 UI 생성
            }

            _root.SetActive(true); // 보상 화면 활성화
        }

        public void Hide() // 현재 카드 보상 화면 숨김
        {
            if (_root != null) _root.SetActive(false); // 전체 보상 루트 비활성화
            _selectionCallback = null; // 이전 선택 콜백 제거
            ClearCardObjects(); // 후보 카드 UI 제거
        }

        private void EnsureUI() // 카드 보상 Canvas와 배경을 한 번만 생성
        {
            if (_canvas != null) return; // 중복 생성 차단
            EnsureEventSystem(); // UI 클릭용 EventSystem 보장

            var canvasObject = new GameObject("CardRewardCanvas_Day46", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 보상 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 직접 표시
            _canvas.sortingOrder = 130; // 기존 지도·전투 UI 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 개발 UI 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildRoot(canvasObject.transform); // 전체 보상 배경·제목 생성
        }

        private void BuildRoot(Transform parent) // 전체 화면 입력 차단 배경과 제목 생성
        {
            _root = new GameObject("CardRewardRoot", typeof(RectTransform), typeof(Image)); // 전체 화면 보상 루트 생성
            _root.transform.SetParent(parent, false); // Canvas 자식 연결

            var rect = _root.GetComponent<RectTransform>(); // 전체 화면 RectTransform 확보
            rect.anchorMin = Vector2.zero; // 좌하단 Stretch 시작
            rect.anchorMax = Vector2.one; // 우상단 Stretch 끝
            rect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rect.offsetMax = Vector2.zero; // 우상단 여백 제거

            var blocker = _root.GetComponent<Image>(); // 전체 화면 배경 이미지 확보
            blocker.color = new Color(0.025f, 0.03f, 0.045f, 0.92f); // 지도 위 어두운 보상 배경 적용
            blocker.raycastTarget = true; // 지도 클릭 차단

            _titleText = CreateText("RewardTitle", _root.transform, 32, FontStyle.Bold); // 보상 제목 생성
            SetRect(_titleText.rectTransform, new Vector2(0f, 228f), new Vector2(850f, 58f)); // 제목 위치·크기 적용
            _root.SetActive(false); // 기본 숨김 상태 지정
        }

        private void CreateRewardCard(PieceDefinition definition, float centerX) // 카드 후보 1장의 선택 UI 생성
        {
            var cardObject = new GameObject($"RewardCard_{definition.PieceId}", typeof(RectTransform), typeof(Image), typeof(Button)); // 카드 버튼 오브젝트 생성
            cardObject.transform.SetParent(_root.transform, false); // 보상 루트 자식 연결
            _cardObjects.Add(cardObject); // 제거용 카드 목록 등록

            var rect = cardObject.GetComponent<RectTransform>(); // 카드 RectTransform 확보
            SetRect(rect, new Vector2(centerX, 0f), new Vector2(CardWidth, CardHeight)); // 카드 위치·크기 적용

            var background = cardObject.GetComponent<Image>(); // 카드 배경 이미지 확보
            background.color = GetGradeColor(definition); // 등급별 임시 카드 배경 적용

            var button = cardObject.GetComponent<Button>(); // 카드 선택 Button 확보
            button.targetGraphic = background; // 카드 배경을 버튼 상태 그래픽으로 사용
            PieceDefinition capturedDefinition = definition; // 반복문 클로저용 현재 카드 고정
            button.onClick.AddListener(() => HandleCardClicked(capturedDefinition)); // 카드 선택 콜백 연결

            var nameText = CreateText("Name", cardObject.transform, 23, FontStyle.Bold); // 기물 이름 텍스트 생성
            nameText.text = definition.DisplayName; // 기물 표시 이름 적용
            SetRect(nameText.rectTransform, new Vector2(0f, 118f), new Vector2(220f, 44f)); // 이름 위치 적용

            var gradeText = CreateText("Grade", cardObject.transform, 20, FontStyle.Bold); // 등급 텍스트 생성
            gradeText.text = new string('★', Mathf.Clamp((int)definition.Grade, 1, 5)); // 1~5성 별표 표시
            SetRect(gradeText.rectTransform, new Vector2(0f, 78f), new Vector2(220f, 34f)); // 등급 위치 적용

            var statText = CreateText("Stats", cardObject.transform, 19, FontStyle.Bold); // HP·ATK 텍스트 생성
            statText.text = $"HP  {definition.BaseHp}\nATK {definition.BaseAtk}"; // 기본 능력치 표시
            SetRect(statText.rectTransform, new Vector2(0f, 12f), new Vector2(210f, 72f)); // 능력치 위치 적용

            var categoryText = CreateText("Category", cardObject.transform, 15, FontStyle.Normal); // 분류 텍스트 생성
            categoryText.text = $"{definition.Category} / {definition.MovementType}"; // 획득 분류·이동 타입 표시
            SetRect(categoryText.rectTransform, new Vector2(0f, -52f), new Vector2(220f, 36f)); // 분류 위치 적용

            var descriptionText = CreateText("Description", cardObject.transform, 14, FontStyle.Normal); // 카드 설명 텍스트 생성
            descriptionText.text = string.IsNullOrWhiteSpace(definition.Description) ? "보상 카드" : definition.Description; // 설명 기본값 보정
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 카드 폭 안에서 줄바꿈
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate; // 카드 높이 초과 문구 잘라내기
            SetRect(descriptionText.rectTransform, new Vector2(0f, -116f), new Vector2(210f, 78f)); // 설명 위치 적용
        }

        private void HandleCardClicked(PieceDefinition definition) // 후보 카드 클릭 처리
        {
            if (definition == null || _selectionCallback == null) return; // 잘못된 선택 차단
            _selectionCallback.Invoke(definition); // 실제 OwnedCardPool 획득 처리 요청
        }

        private void ClearCardObjects() // 현재 후보 카드 UI 정리
        {
            for (int i = 0; i < _cardObjects.Count; i++) // 생성 카드 순회
            {
                if (_cardObjects[i] != null) Destroy(_cardObjects[i]); // 런타임 카드 버튼 제거
            }

            _cardObjects.Clear(); // 카드 UI 목록 초기화
        }

        private static Color GetGradeColor(PieceDefinition definition) // 현재 카드 등급에 따른 개발용 배경색 선택
        {
            int grade = definition != null ? (int)definition.Grade : 1; // 카드 등급 숫자 변환
            if (grade >= 3) return new Color(0.28f, 0.18f, 0.42f, 0.98f); // 3성 보라 계열
            if (grade == 2) return new Color(0.18f, 0.31f, 0.46f, 0.98f); // 2성 청색 계열
            return new Color(0.23f, 0.24f, 0.27f, 0.98f); // 1성 회색 계열
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle) // 공통 Text 생성
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 부모 자식 연결
            var text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 굵기 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 문구 적용
            text.raycastTarget = false; // 카드 버튼 클릭 방해 차단
            return text; // 완성 Text 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) // 중앙 기준 UI 위치·크기 적용
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private void EnsureEventSystem() // 카드 선택 클릭용 EventSystem 보장
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 참조 저장
        }

        private static Font GetRuntimeFont() // 한글 표시 가능한 런타임 폰트 확보
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 우선 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // UI 제거 시 직접 생성 EventSystem 정리
        {
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 만든 EventSystem 제거
        }
    }
}
