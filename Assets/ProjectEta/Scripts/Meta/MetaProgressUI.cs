using System.Collections.Generic; // List<T>·IReadOnlyList<T> 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용

namespace ProjectEta.Meta
{
    public sealed class MetaProgressUI : MonoBehaviour
    {
        private const int UnlockButtonCount = 5; // 프로토타입 영구 해금 버튼 수

        private readonly List<Button> _unlockButtons = new List<Button>(); // 재사용 영구 해금 버튼 목록
        private Canvas _canvas; // 메타 진행 전용 Canvas
        private GameObject _root; // 전체 메타 결과 UI 루트
        private Text _titleText; // 런 결과 제목
        private Text _rewardText; // 이번 메타 토큰 보상
        private Text _totalText; // 현재 영구 메타 토큰 잔액
        private Text _statusText; // 해금 결과 안내
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private MetaProgressState _progress; // 현재 표시 영구 진행 상태
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        public bool IsVisible => _root != null && _root.activeSelf; // 현재 메타 결과 UI 표시 여부

        public void Show(MetaProgressState progress, int earnedTokens, int reachedStage, bool completed)
        {
            EnsureUI(); // 메타 결과 Canvas 생성 보장
            _progress = progress; // 현재 영구 진행 상태 저장
            _titleText.text = completed ? "런 클리어" : "런 종료"; // 승리·실패 제목 표시
            _rewardText.text = $"도달 단계 {reachedStage}\n메타 토큰 +{earnedTokens}"; // 이번 런 메타 보상 표시
            _statusText.text = "메타 토큰은 게임을 종료해도 유지됩니다."; // 영구 진행 안내 표시
            RefreshProgress(); // 토큰·해금 상태 즉시 갱신
            _root.SetActive(true); // 메타 결과 화면 표시
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false); // 메타 결과 화면 숨김
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 UI 생성 차단
            EnsureEventSystem(); // UI 클릭용 EventSystem 보장

            var canvasObject = new GameObject("MetaProgressCanvas_Day48", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 메타 진행 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 런 종료 결과를 화면 위에 표시
            _canvas.sortingOrder = 240; // 기존 전투·지도 UI 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 개발 UI 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildRoot(canvasObject.transform); // 메타 결과 화면 구성
        }

        private void BuildRoot(Transform parent)
        {
            _root = new GameObject("MetaProgressRoot", typeof(RectTransform), typeof(Image)); // 전체 화면 결과 루트 생성
            _root.transform.SetParent(parent, false); // Canvas 자식 연결

            RectTransform rootRect = _root.GetComponent<RectTransform>(); // 전체 화면 RectTransform 확보
            rootRect.anchorMin = Vector2.zero; // 좌하단 Stretch 시작
            rootRect.anchorMax = Vector2.one; // 우상단 Stretch 끝
            rootRect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rootRect.offsetMax = Vector2.zero; // 우상단 여백 제거

            Image blocker = _root.GetComponent<Image>(); // 전체 화면 어두운 배경 확보
            blocker.color = new Color(0.02f, 0.018f, 0.015f, 0.92f); // 런 종료 집중 배경 적용
            blocker.raycastTarget = true; // 뒤쪽 보드 입력 차단

            var panelObject = new GameObject("MetaPanel", typeof(RectTransform), typeof(Image)); // 중앙 영구 성장 패널 생성
            panelObject.transform.SetParent(_root.transform, false); // 결과 루트 자식 연결
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 중앙 패널 RectTransform 확보
            SetRect(panelRect, Vector2.zero, new Vector2(980f, 820f)); // 중앙 패널 위치·크기 적용
            Image panelImage = panelObject.GetComponent<Image>(); // 중앙 패널 배경 확보
            panelImage.color = new Color(0.14f, 0.09f, 0.05f, 0.98f); // 테이블 분위기 갈색 배경 적용

            _titleText = CreateText("Title", panelObject.transform, 42, FontStyle.Bold); // 결과 제목 생성
            SetRect(_titleText.rectTransform, new Vector2(0f, 335f), new Vector2(840f, 66f)); // 결과 제목 위치 적용

            _rewardText = CreateText("Reward", panelObject.transform, 26, FontStyle.Bold); // 이번 보상 텍스트 생성
            SetRect(_rewardText.rectTransform, new Vector2(-245f, 245f), new Vector2(410f, 100f)); // 이번 보상 위치 적용

            _totalText = CreateText("Total", panelObject.transform, 30, FontStyle.Bold); // 총 메타 토큰 텍스트 생성
            SetRect(_totalText.rectTransform, new Vector2(245f, 245f), new Vector2(410f, 100f)); // 총 토큰 위치 적용

            Text unlockTitle = CreateText("UnlockTitle", panelObject.transform, 28, FontStyle.Bold); // 영구 해금 제목 생성
            unlockTitle.text = "영구 해금"; // 영구 해금 제목 적용
            SetRect(unlockTitle.rectTransform, new Vector2(0f, 150f), new Vector2(700f, 52f)); // 영구 해금 제목 위치 적용

            for (int i = 0; i < UnlockButtonCount; i++)
            {
                Button button = CreateUnlockButton(panelObject.transform, i); // 영구 해금 버튼 생성
                _unlockButtons.Add(button); // 재사용 버튼 목록 등록
            }

            _statusText = CreateText("Status", panelObject.transform, 20, FontStyle.Normal); // 결과·안내 텍스트 생성
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap; // 안내 줄바꿈 허용
            SetRect(_statusText.rectTransform, new Vector2(0f, -278f), new Vector2(820f, 70f)); // 안내 위치 적용

            Button closeButton = CreateButton("CloseButton", panelObject.transform, "닫기", new Vector2(0f, -355f), new Vector2(280f, 64f)); // 닫기 버튼 생성
            closeButton.onClick.AddListener(Hide); // 메타 결과 화면 닫기 연결
            _root.SetActive(false); // 기본 숨김 상태
        }

        private Button CreateUnlockButton(Transform parent, int index)
        {
            int column = index % 2; // 2열 해금 버튼 열 계산
            int row = index / 2; // 해금 버튼 행 계산
            float x = column == 0 ? -225f : 225f; // 좌우 버튼 위치 계산
            float y = 65f - row * 105f; // 위에서 아래 버튼 위치 계산
            return CreateButton($"Unlock_{index}", parent, string.Empty, new Vector2(x, y), new Vector2(410f, 82f)); // 영구 해금 버튼 생성
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 공통 버튼 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // 부모 자식 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 확보
            image.color = new Color(0.33f, 0.23f, 0.12f, 0.98f); // 목재 카드형 배경 적용
            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            button.targetGraphic = image; // 버튼 대상 그래픽 지정

            Text text = CreateText("Label", buttonObject.transform, 20, FontStyle.Bold); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 8f); // 버튼 내부 여백 적용
            return button; // 완성 버튼 반환
        }

        private void RefreshProgress()
        {
            if (_progress == null) return; // 영구 진행 상태 누락 방어
            _totalText.text = $"보유 메타 토큰\n{_progress.MetaTokens}"; // 현재 영구 토큰 잔액 표시

            IReadOnlyList<MetaUnlockDefinition> definitions = MetaUnlockCatalog.All; // 프로토타입 해금 목록 조회

            for (int i = 0; i < _unlockButtons.Count; i++)
            {
                Button button = _unlockButtons[i]; // 현재 해금 버튼 조회
                button.onClick.RemoveAllListeners(); // 이전 해금 콜백 제거

                if (i >= definitions.Count)
                {
                    button.gameObject.SetActive(false); // 정의가 없는 남는 버튼 숨김
                    continue;
                }

                MetaUnlockDefinition definition = definitions[i]; // 현재 영구 해금 정의 조회
                bool unlocked = _progress.IsUnlocked(definition.UnlockType, definition.UnlockId); // 현재 해금 완료 여부 조회
                bool canUnlock = MetaUnlockService.CanUnlock(_progress, definition); // 현재 비용 충족 여부 조회
                Text label = button.GetComponentInChildren<Text>(true); // 버튼 문구 Text 조회

                if (label != null)
                {
                    label.text = unlocked
                        ? $"{definition.DisplayName}\n해금 완료"
                        : $"{definition.DisplayName}\n{definition.Cost} Token"; // 해금 상태·비용 표시
                }

                button.interactable = canUnlock; // 완료·비용 부족 버튼 비활성화
                MetaUnlockDefinition captured = definition; // 버튼 클로저 해금 정의 고정
                button.onClick.AddListener(() => TryUnlock(captured)); // 영구 해금 실행 연결
                button.gameObject.SetActive(true); // 현재 해금 버튼 표시
            }
        }

        private void TryUnlock(MetaUnlockDefinition definition)
        {
            if (_progress == null || definition == null) return; // 잘못된 해금 요청 차단

            if (!MetaUnlockService.TryUnlock(_progress, definition))
            {
                _statusText.text = "메타 토큰이 부족하거나 이미 해금된 항목입니다."; // 해금 실패 안내
                RefreshProgress(); // 현재 상태 재표시
                return;
            }

            bool saved = MetaProgressService.Save(); // 영구 해금 즉시 디스크 저장
            _statusText.text = saved
                ? $"{definition.DisplayName} 영구 해금 완료"
                : $"{definition.DisplayName} 해금 완료 · 저장 실패"; // 영구 해금 결과 안내
            RefreshProgress(); // 해금·잔액 상태 갱신
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
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
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 문구 적용
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
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
