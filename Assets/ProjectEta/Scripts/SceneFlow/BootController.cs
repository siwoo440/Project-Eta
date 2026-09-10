using System.Collections; // IEnumerator 사용
using UnityEngine; // MonoBehaviour·Camera·Color·Time 사용
using UnityEngine.UI; // Canvas·CanvasScaler·Image·Text 사용
using ProjectEta.Meta; // MetaProgressService 사용
using ProjectEta.Run; // RunSaveSystem 사용
using ProjectEta.Settings; // GameSettingsService 사용

namespace ProjectEta.SceneFlow
{
    [DefaultExecutionOrder(-900)]
    public sealed class BootController : MonoBehaviour
    {
        private const float MinimumDisplaySeconds = 0.45f; // Boot 최소 표시 시간
        private Text _statusText; // Boot 현재 처리 문구
        private RectTransform _loadingMarkRect; // Boot 로딩 회전 마크
        private static Font _runtimeFont; // Boot 한글 런타임 폰트 캐시

        private void Update()
        {
            if (_loadingMarkRect == null) return; // 로딩 마크 준비 전 회전 차단
            _loadingMarkRect.Rotate(0f, 0f, -105f * Time.unscaledDeltaTime); // TimeScale과 무관한 Boot 회전 연출
        }

        private IEnumerator Start()
        {
            PrepareCamera(); // 빈 Boot 씬 배경 정리
            BuildLoadingUI(); // Boot 로딩 화면 생성
            float startedAt = Time.unscaledTime; // Boot 표시 시작 시각 기록

            SetStatus("설정을 불러오는 중..."); // 설정 로딩 상태 표시
            GameSettingsService.EnsureLoaded(); // 화면·오디오 설정 최초 로드
            yield return null; // 설정 적용 한 프레임 대기

            SetStatus("영구 성장 데이터를 불러오는 중..."); // 메타 데이터 로딩 상태 표시
            MetaProgressState progress = MetaProgressService.Current; // 영구 성장 데이터 최초 로드
            yield return null; // 메타 상태 준비 한 프레임 대기

            SetStatus("런 저장 데이터를 확인하는 중..."); // Continue 검사 상태 표시
            bool canContinue = RunSaveSystem.CanContinue; // 런 세이브 유효성 미리 확인
            yield return null; // 저장 검사 UI 반영 대기

            SetStatus(canContinue ? "이어하기 데이터를 확인했습니다." : "새로운 런을 준비합니다."); // 저장 유무 최종 상태 표시
            Debug.Log($"68일차 Boot 초기화: MetaToken={progress.MetaTokens} / Continue={canContinue}"); // 초기화 결과 기록

            while (Time.unscaledTime - startedAt < MinimumDisplaySeconds)
            {
                yield return null; // Boot 화면 최소 표시 시간 보장
            }

            if (!SceneFlowController.LoadMainMenu())
            {
                SetStatus("MainMenu 씬을 불러오지 못했습니다."); // 메인 메뉴 전환 실패 화면 표시
                Debug.LogError("68일차 Boot 전환 실패: MainMenu 씬을 Build Settings에서 확인하세요."); // 메인 메뉴 전환 실패 기록
            }
        }

        private void BuildLoadingUI()
        {
            GameObject canvasObject = new GameObject("BootLoadingCanvas_Day68", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Boot 로딩 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // BootController 자식 연결

            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Boot Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전체 화면 오버레이 적용
            canvas.sortingOrder = 100; // Boot 기본 카메라 위 UI 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // Boot CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 UI 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 적용

            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)); // Boot 전체 배경 생성
            backdrop.transform.SetParent(canvasObject.transform, false); // Boot Canvas 자식 연결
            Stretch(backdrop.GetComponent<RectTransform>()); // 전체 화면 배치
            backdrop.GetComponent<Image>().color = new Color(0.015f, 0.018f, 0.022f, 1f); // Boot 어두운 배경 적용

            Text eyebrow = CreateText("Eyebrow", canvasObject.transform, 17, FontStyle.Bold, TextAnchor.MiddleCenter); // Boot 상단 분류 문구 생성
            eyebrow.text = "TURN-BASED ROGUELITE STRATEGY"; // 장르 분류 문구 적용
            eyebrow.color = new Color(0.38f, 0.68f, 0.88f, 1f); // 프로젝트 강조 색상 적용
            SetRect(eyebrow.rectTransform, new Vector2(0f, 115f), new Vector2(700f, 40f)); // 분류 문구 위치 적용

            Text title = CreateText("Title", canvasObject.transform, 56, FontStyle.Bold, TextAnchor.MiddleCenter); // 프로젝트 제목 생성
            title.text = "PROJECT η"; // 프로젝트 제목 적용
            SetRect(title.rectTransform, new Vector2(0f, 45f), new Vector2(760f, 90f)); // 제목 위치 적용

            _statusText = CreateText("Status", canvasObject.transform, 20, FontStyle.Normal, TextAnchor.MiddleCenter); // Boot 상태 문구 생성
            _statusText.color = new Color(0.72f, 0.76f, 0.82f, 1f); // Boot 상태 보조 색상 적용
            SetRect(_statusText.rectTransform, new Vector2(0f, -80f), new Vector2(780f, 52f)); // 상태 문구 위치 적용

            Text loadingMark = CreateText("LoadingMark", canvasObject.transform, 28, FontStyle.Bold, TextAnchor.MiddleCenter); // Boot 로딩 마크 생성
            loadingMark.text = "◇"; // 단순 로딩 마크 적용
            loadingMark.color = new Color(0.38f, 0.68f, 0.88f, 0.92f); // 로딩 마크 강조 색상 적용
            SetRect(loadingMark.rectTransform, new Vector2(0f, -145f), new Vector2(80f, 50f)); // 로딩 마크 위치 적용
            _loadingMarkRect = loadingMark.rectTransform; // 회전 연출 대상 저장

        }

        private void SetStatus(string message)
        {
            if (_statusText != null) _statusText.text = message; // Boot 현재 처리 문구 갱신
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Boot Text 오브젝트 생성
            textObject.transform.SetParent(parent, false); // Boot Canvas 자식 연결
            Text text = textObject.GetComponent<Text>(); // Boot Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = style; // 글자 스타일 적용
            text.alignment = alignment; // 글자 정렬 적용
            text.color = Color.white; // 기본 흰색 적용
            text.raycastTarget = false; // Boot 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 Boot 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 Boot 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 시작 앵커 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 끝 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; // Stretch 시작 앵커 적용
            rect.anchorMax = Vector2.one; // Stretch 끝 앵커 적용
            rect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rect.offsetMax = Vector2.zero; // 우상단 여백 제거
        }

        private static void PrepareCamera()
        {
            Camera camera = Camera.main; // Boot 기본 카메라 조회
            if (camera == null) return; // 카메라 누락 허용

            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 초기화 화면 적용
            camera.backgroundColor = new Color(0.015f, 0.018f, 0.022f, 1f); // 어두운 부트 배경 적용
        }
    }
}
