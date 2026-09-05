using System; // Action 콜백 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Run; // StageDefinition·StageType 사용

namespace ProjectEta.UI // 비전투 스테이지 개발 UI 네임스페이스
{
    public sealed class StagePlaceholderUI : MonoBehaviour // 46·47일차 실제 기능 전까지 Reward·Shop·Event 진입을 검증하는 임시 패널
    {
        private Canvas _canvas; // 임시 스테이지 전용 Canvas
        private GameObject _panelRoot; // 표시·숨김용 패널 루트
        private Text _titleText; // 현재 StageDefinition 이름 표시
        private Text _descriptionText; // 현재 스테이지 타입 설명
        private Button _continueButton; // 다음 경로 지도로 복귀하는 개발용 버튼
        private Action _continueAction; // 현재 완료 버튼 콜백
        private EventSystem _createdEventSystem; // 직접 생성한 EventSystem 참조
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf; // 현재 패널 표시 여부 공개

        public void Show(StageDefinition definition, Action continueAction) // 선택한 비전투 StageDefinition 표시
        {
            EnsureUI(); // Canvas·패널 최초 생성 보장
            _continueAction = continueAction; // 완료 시 실행할 흐름 저장
            _titleText.text = definition != null ? definition.DisplayName : "스테이지"; // 현재 스테이지 이름 표시
            _descriptionText.text = GetDescription(definition != null ? definition.StageType : StageType.Event); // 타입별 다음 일정 안내 표시
            _panelRoot.SetActive(true); // 비전투 스테이지 패널 표시
        }

        public void Hide() // 현재 임시 스테이지 패널 숨김
        {
            if (_panelRoot != null) _panelRoot.SetActive(false); // 패널 비활성화
            _continueAction = null; // 이전 완료 콜백 제거
        }

        private void EnsureUI() // 런타임 임시 Canvas 한 번만 생성
        {
            if (_canvas != null) return; // 중복 생성 차단
            EnsureEventSystem(); // UI 클릭용 EventSystem 보장

            var canvasObject = new GameObject("StagePlaceholderCanvas_Day45", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 화면 전체 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위 직접 표시
            _canvas.sortingOrder = 120; // 기존 전투·지도 UI 위에 표시

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 개발 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            BuildPanel(canvasObject.transform); // 중앙 임시 스테이지 패널 생성
        }

        private void BuildPanel(Transform parent) // 비전투 스테이지 정보와 계속 버튼 생성
        {
            _panelRoot = new GameObject("StagePlaceholderPanel", typeof(RectTransform), typeof(Image)); // 중앙 패널 오브젝트 생성
            _panelRoot.transform.SetParent(parent, false); // Canvas 자식 연결

            var rect = _panelRoot.GetComponent<RectTransform>(); // 패널 RectTransform 확보
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 화면 중앙 앵커 지정
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 고정 중앙 앵커 사용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 사용
            rect.anchoredPosition = Vector2.zero; // 화면 중앙 배치
            rect.sizeDelta = new Vector2(520f, 250f); // 임시 패널 크기 지정

            var background = _panelRoot.GetComponent<Image>(); // 패널 배경 이미지 확보
            background.color = new Color(0.06f, 0.07f, 0.09f, 0.96f); // 어두운 반투명 배경 적용

            _titleText = CreateText("StageTitle", _panelRoot.transform, 28, FontStyle.Bold); // 제목 텍스트 생성
            SetRect(_titleText.rectTransform, new Vector2(0f, 72f), new Vector2(470f, 44f)); // 제목 위치·크기 적용

            _descriptionText = CreateText("StageDescription", _panelRoot.transform, 17, FontStyle.Normal); // 설명 텍스트 생성
            SetRect(_descriptionText.rectTransform, new Vector2(0f, 10f), new Vector2(450f, 70f)); // 설명 위치·크기 적용

            _continueButton = CreateButton(_panelRoot.transform); // 개발용 계속 버튼 생성
            _panelRoot.SetActive(false); // 기본 숨김 상태 지정
        }

        private Button CreateButton(Transform parent) // 경로 지도 복귀 버튼 생성
        {
            var buttonObject = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // 패널 자식 연결

            var rect = buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 확보
            SetRect(rect, new Vector2(0f, -82f), new Vector2(220f, 48f)); // 버튼 위치·크기 적용

            var image = buttonObject.GetComponent<Image>(); // 버튼 배경 확보
            image.color = new Color(0.23f, 0.48f, 0.78f, 1f); // 개발용 파란 버튼 색 적용

            var button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 확보
            button.targetGraphic = image; // 배경을 버튼 상태 그래픽으로 사용
            button.onClick.AddListener(HandleContinueClicked); // 클릭 완료 흐름 연결

            var label = CreateText("ContinueText", buttonObject.transform, 18, FontStyle.Bold); // 버튼 문구 생성
            label.text = "계속"; // 임시 스테이지 완료 문구 적용
            Stretch(label.rectTransform, 2f); // 버튼 내부 전체에 문구 배치
            return button; // 완성 버튼 반환
        }

        private void HandleContinueClicked() // 임시 비전투 스테이지 완료 버튼 처리
        {
            Action callback = _continueAction; // Hide 전에 현재 콜백 보존
            Hide(); // 패널 먼저 닫기
            callback?.Invoke(); // 실제 다음 경로 지도 준비 실행
        }

        private static string GetDescription(StageType stageType) // 현재 일차에서 아직 미구현인 비전투 기능 안내
        {
            if (stageType == StageType.Reward) return "카드 보상 노드 진입 확인 완료\n실제 3개 카드 선택은 46일차에 연결됩니다."; // 보상 안내
            if (stageType == StageType.Shop) return "상점 노드 진입 확인 완료\n구매·제거·회복 기능은 47일차에 연결됩니다."; // 상점 안내
            return "이벤트 노드 진입 확인 완료\n선택형 이벤트 결과는 47일차에 연결됩니다."; // 이벤트 안내
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle) // 공통 Text 생성
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 부모 자식 연결
            var text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 표시 가능한 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 굵기 적용
            text.alignment = TextAnchor.MiddleCenter; // 중앙 정렬 적용
            text.color = Color.white; // 흰색 문구 적용
            text.raycastTarget = false; // 버튼 클릭 방해 차단
            return text; // 완성 텍스트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) // 중앙 기준 UI 위치·크기 적용
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 지정
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 고정 앵커 사용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 지정
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float padding) // 부모 내부 전체 Stretch 적용
        {
            rect.anchorMin = Vector2.zero; // 좌하단 앵커 지정
            rect.anchorMax = Vector2.one; // 우상단 앵커 지정
            rect.offsetMin = new Vector2(padding, padding); // 좌하단 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 우상단 여백 적용
        }

        private void EnsureEventSystem() // UI 클릭용 EventSystem 보장
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 참조 저장
        }

        private static Font GetRuntimeFont() // 한글 표시 가능한 시스템 폰트 확보
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 폰트 캐시 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 우선 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 실패 시 Unity 기본 폰트 사용
            return _runtimeFont; // 최종 폰트 반환
        }

        private void OnDestroy() // UI 제거 시 이벤트·직접 생성 EventSystem 정리
        {
            if (_continueButton != null) _continueButton.onClick.RemoveListener(HandleContinueClicked); // 버튼 이벤트 구독 해제
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 만든 EventSystem 제거
        }
    }
}
