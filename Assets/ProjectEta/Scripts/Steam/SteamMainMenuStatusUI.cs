using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.UI; // Canvas·Text·Button·Image 사용

namespace ProjectEta.Steam
{
    [DefaultExecutionOrder(2200)]
    [DisallowMultipleComponent]
    public sealed class SteamMainMenuStatusUI : MonoBehaviour
    {
        private Canvas canvas; // Steam 상태 UI Canvas
        private Text statusText; // Steam 연결 상태 문구
        private Button overlayButton; // Steam Overlay 버튼
        private Text overlayButtonText; // Steam Overlay 버튼 문구
        private Font runtimeFont; // Steam 상태 UI 런타임 폰트
        private float nextRefreshTime; // 다음 상태 갱신 시각

        private void Start() // Steam MainMenu 상태 UI 초기화
        {
            BuildUI(); // Steam 상태 UI 생성
            RefreshState(); // 최초 Steam 상태 반영
        }

        private void Update() // Steam MainMenu 상태 주기 갱신
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            RefreshState(); // Steam 상태 재조회
            nextRefreshTime = Time.unscaledTime + 0.5f; // 다음 갱신 시각 설정
        }

        private void BuildUI() // Steam 상태 패널 생성
        {
            GameObject canvasObject = new GameObject("SteamStatusCanvas_Day80", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Steam 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // UI 호스트 자식 연결
            canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 모드 적용
            canvas.sortingOrder = 160; // MainMenu 기본 Canvas 위 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // Steam Canvas 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // MainMenu 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정

            GameObject panelObject = new GameObject("SteamStatusPanel", typeof(RectTransform), typeof(Image)); // Steam 상태 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 조회
            panelRect.anchorMin = new Vector2(1f, 1f); // 우측 상단 기준 Anchor 설정
            panelRect.anchorMax = new Vector2(1f, 1f); // 우측 상단 기준 Anchor 설정
            panelRect.pivot = new Vector2(1f, 1f); // 우측 상단 Pivot 설정
            panelRect.anchoredPosition = new Vector2(-28f, -28f); // 화면 여백 적용
            panelRect.sizeDelta = new Vector2(470f, 132f); // Steam 패널 크기 적용
            panelObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.96f); // Steam 상태 패널 배경 적용

            runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial", "Liberation Sans" }, 18); // 한글 우선 런타임 폰트 생성

            statusText = CreateText("SteamStatusText", panelObject.transform, 18, TextAnchor.UpperLeft); // Steam 상태 문구 생성
            RectTransform statusRect = statusText.rectTransform; // 상태 문구 RectTransform 조회
            statusRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 Anchor 설정
            statusRect.anchorMax = new Vector2(1f, 1f); // 우측 상단 Anchor 설정
            statusRect.pivot = new Vector2(0.5f, 1f); // 상단 Pivot 설정
            statusRect.anchoredPosition = new Vector2(0f, -12f); // 상태 문구 상단 여백 적용
            statusRect.sizeDelta = new Vector2(-24f, 58f); // 상태 문구 영역 크기 적용
            statusText.color = new Color(0.82f, 0.88f, 0.95f, 1f); // Steam 상태 문구 색상 적용

            overlayButton = CreateButton("SteamOverlayButton", panelObject.transform, "Steam Overlay 열기"); // Steam Overlay 버튼 생성
            RectTransform buttonRect = overlayButton.GetComponent<RectTransform>(); // Overlay 버튼 RectTransform 조회
            buttonRect.anchorMin = new Vector2(0f, 0f); // 좌측 하단 Anchor 설정
            buttonRect.anchorMax = new Vector2(1f, 0f); // 우측 하단 Anchor 설정
            buttonRect.pivot = new Vector2(0.5f, 0f); // 하단 Pivot 설정
            buttonRect.anchoredPosition = new Vector2(0f, 12f); // Overlay 버튼 하단 여백 적용
            buttonRect.sizeDelta = new Vector2(-24f, 46f); // Overlay 버튼 크기 적용
            overlayButton.onClick.AddListener(HandleOpenOverlay); // Steam Overlay 호출 연결
        }

        private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment) // Steam UI Text 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 지정 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = runtimeFont; // 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.alignment = alignment; // 정렬 적용
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 가로 줄바꿈 허용
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 내용 표시 허용
            return text; // 생성 Text 반환
        }

        private Button CreateButton(string name, Transform parent, string label) // Steam UI Button 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // Button 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // 지정 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 Image 조회
            image.color = new Color(0.08f, 0.20f, 0.30f, 0.98f); // Steam 버튼 배경 색상 적용

            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            ColorBlock colors = button.colors; // 버튼 상태 색상 조회
            colors.highlightedColor = new Color(0.12f, 0.30f, 0.44f, 1f); // Hover 색상 적용
            colors.pressedColor = new Color(0.06f, 0.15f, 0.23f, 1f); // Press 색상 적용
            colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.65f); // 비활성 색상 적용
            button.colors = colors; // 버튼 상태 색상 저장

            overlayButtonText = CreateText("Label", buttonObject.transform, 18, TextAnchor.MiddleCenter); // Overlay 버튼 문구 생성
            overlayButtonText.text = label; // Overlay 버튼 문구 적용
            overlayButtonText.color = Color.white; // Overlay 버튼 문구 색상 적용
            RectTransform labelRect = overlayButtonText.rectTransform; // 버튼 문구 RectTransform 조회
            labelRect.anchorMin = Vector2.zero; // 버튼 전체 Anchor 최소값 적용
            labelRect.anchorMax = Vector2.one; // 버튼 전체 Anchor 최대값 적용
            labelRect.offsetMin = Vector2.zero; // 버튼 문구 좌하단 여백 제거
            labelRect.offsetMax = Vector2.zero; // 버튼 문구 우상단 여백 제거
            return button; // 생성 Button 반환
        }

        private void RefreshState() // Steam Runtime·기능 상태 UI 반영
        {
            if (statusText == null || overlayButton == null)
            {
                return;
            }

            if (!SteamPlatform.IsInitialized)
            {
                statusText.text = "STEAM · OFFLINE\n로컬 모드로 정상 실행 중"; // Steam 미초기화 상태 표시
                overlayButton.interactable = false; // Overlay 버튼 비활성화
                return;
            }

            string overlayState = SteamPlatform.IsOverlayEnabled ? "ON" : "OFF"; // Overlay 상태 문구 계산
            string cloudState = SteamPlatform.IsCloudEnabled ? "ON" : "OFF"; // Cloud 상태 문구 계산
            string achievementState = SteamPlatform.IsAchievementEnabled ? "ON" : "OFF"; // Achievement 상태 문구 계산
            statusText.text = $"STEAM · {SteamPlatform.BackendName}\nOverlay {overlayState}  ·  Cloud {cloudState}  ·  Achievement {achievementState}"; // Steam 통합 상태 표시
            overlayButton.interactable = SteamPlatform.IsOverlayEnabled; // Overlay 가능 상태 버튼 반영
        }

        private void HandleOpenOverlay() // MainMenu Steam Overlay 열기
        {
            SteamPlatform.OpenOverlay(); // Steam 친구 Overlay 호출
            RefreshState(); // 호출 후 Steam 상태 재확인
        }
    }
}
