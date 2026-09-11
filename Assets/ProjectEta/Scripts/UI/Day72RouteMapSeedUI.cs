using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using UnityEngine.UI; // Canvas·CanvasScaler·Image·Text 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Run; // RunState·RunFlowPhase 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1250)]
    public sealed class Day72RouteMapSeedUI : MonoBehaviour
    {
        private const string CanvasObjectName = "Day72RouteMapSeedCanvas"; // Seed Canvas 오브젝트 이름
        private const string PanelObjectName = "RouteMapSeedPanel"; // Seed 패널 오브젝트 이름
        private const string TextObjectName = "MapSeed"; // Seed 텍스트 오브젝트 이름

        private BattleController _battleController; // 현재 런 상태 제공 전투 컨트롤러
        private RunState _runState; // 현재 런 상태
        private Canvas _canvas; // Seed 전용 Screen Space Canvas
        private GameObject _seedRoot; // Seed 패널 루트
        private Text _seedText; // Seed 표시 텍스트
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 씬 로드 콜백 제거
            SceneManager.sceneLoaded += HandleSceneLoaded; // Battle 씬 진입 감시 등록
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<Day72RouteMapSeedUI>() != null) return; // 중복 생성 차단

            GameObject host = new GameObject("Day72RouteMapSeedUI"); // Seed UI 호스트 생성
            host.AddComponent<Day72RouteMapSeedUI>(); // Seed UI 컴포넌트 추가
        }

        private void Awake()
        {
            EnsureUI(); // Seed UI 선생성
            SetVisible(false); // 지도 준비 전 기본 숨김
        }

        private void Update()
        {
            ResolveBindings(); // BattleController·RunState 연결 보장
            EnsureUI(); // 부분 삭제·도메인 재로드 후 UI 복구 보장

            bool mapSelectionActive =
                _runState != null &&
                _runState.CurrentFlowPhase == RunFlowPhase.Map &&
                _runState.RouteMap.HasPreparedRoute; // 실제 경로 선택 상태 확인

            SetVisible(mapSelectionActive); // 경로 선택 상태에 맞춰 표시 전환
            if (!mapSelectionActive) return; // 지도 외 Seed 갱신 차단

            _seedText.text = $"SEED {_runState.RouteMap.MapSeed}"; // 현재 RouteMap Seed 표시
        }

        private void ResolveBindings()
        {
            if (_battleController == null)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 지연 탐색
            }

            if (_battleController == null || _battleController.RunState == null)
            {
                _runState = null; // 준비되지 않은 런 참조 제거
                return;
            }

            _runState = _battleController.RunState; // 현재 RunState 연결
        }

        private void EnsureUI()
        {
            EnsureCanvas(); // Seed 전용 Canvas 생성·복구
            EnsureSeedPanel(); // Seed 패널·텍스트 생성·복구
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return; // 유효 Canvas 재사용

            Transform existingCanvasTransform = transform.Find(CanvasObjectName); // 기존 Seed Canvas 탐색
            GameObject canvasObject = existingCanvasTransform != null
                ? existingCanvasTransform.gameObject
                : new GameObject(CanvasObjectName, typeof(RectTransform)); // 기존 Canvas 재사용 또는 신규 생성

            if (existingCanvasTransform == null)
            {
                canvasObject.transform.SetParent(transform, false); // 신규 Canvas 호스트 연결
            }

            _canvas = canvasObject.GetComponent<Canvas>(); // 기존 Canvas 컴포넌트 조회
            if (_canvas == null) _canvas = canvasObject.AddComponent<Canvas>(); // Canvas 컴포넌트 누락 복구
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 HUD 적용
            _canvas.sortingOrder = 216; // Day66 RouteMap HUD 위에 Seed 표시

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 기존 CanvasScaler 조회
            if (scaler == null) scaler = canvasObject.AddComponent<CanvasScaler>(); // CanvasScaler 누락 복구
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 보정
        }

        private void EnsureSeedPanel()
        {
            if (_canvas == null) return; // Canvas 준비 전 생성 차단

            if (_seedRoot == null)
            {
                Transform existingPanel = _canvas.transform.Find(PanelObjectName); // 기존 Seed 패널 탐색

                if (existingPanel != null)
                {
                    _seedRoot = existingPanel.gameObject; // 기존 Seed 패널 재사용
                }
                else
                {
                    _seedRoot = new GameObject(PanelObjectName, typeof(RectTransform), typeof(Image)); // Seed 패널 신규 생성
                    _seedRoot.transform.SetParent(_canvas.transform, false); // Seed Canvas 자식 연결
                }
            }

            RectTransform rootRect = _seedRoot.GetComponent<RectTransform>(); // Seed 패널 RectTransform 조회
            rootRect.anchorMin = new Vector2(1f, 0f); // 우하단 앵커 시작 적용
            rootRect.anchorMax = new Vector2(1f, 0f); // 우하단 앵커 끝 적용
            rootRect.pivot = new Vector2(1f, 0f); // 우하단 피벗 적용
            rootRect.anchoredPosition = new Vector2(-28f, 24f); // 화면 우하단 여백 적용
            rootRect.sizeDelta = new Vector2(330f, 48f); // Seed 패널 크기 적용

            Image background = _seedRoot.GetComponent<Image>(); // Seed 배경 이미지 조회
            if (background == null) background = _seedRoot.AddComponent<Image>(); // 배경 이미지 누락 복구
            background.color = new Color(0.025f, 0.030f, 0.043f, 0.86f); // 지도 HUD 계열 배경 적용
            background.raycastTarget = false; // 지도 입력 간섭 제거

            if (_seedText == null)
            {
                Transform existingText = _seedRoot.transform.Find(TextObjectName); // 기존 Seed 텍스트 탐색

                if (existingText != null)
                {
                    _seedText = existingText.GetComponent<Text>(); // 기존 Text 컴포넌트 조회
                }

                if (_seedText == null)
                {
                    GameObject textObject = existingText != null
                        ? existingText.gameObject
                        : new GameObject(TextObjectName, typeof(RectTransform)); // 기존 텍스트 오브젝트 재사용 또는 신규 생성

                    if (existingText == null)
                    {
                        textObject.transform.SetParent(_seedRoot.transform, false); // 신규 텍스트 패널 연결
                    }

                    _seedText = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
                    if (_seedText == null) _seedText = textObject.AddComponent<Text>(); // Text 컴포넌트 누락 복구
                }
            }

            ConfigureSeedText(); // Seed 텍스트 표시 형식 적용
        }

        private void ConfigureSeedText()
        {
            if (_seedText == null) return; // Seed Text 누락 방어

            _seedText.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            _seedText.fontSize = 18; // Seed 글자 크기 적용
            _seedText.fontStyle = FontStyle.Normal; // 일반 글자 스타일 적용
            _seedText.alignment = TextAnchor.MiddleCenter; // 패널 중앙 정렬
            _seedText.color = new Color(0.78f, 0.82f, 0.90f, 1f); // 보조 텍스트 색상 적용
            _seedText.raycastTarget = false; // 지도 포인터 입력 간섭 제거

            RectTransform textRect = _seedText.rectTransform; // Seed 텍스트 RectTransform 조회
            textRect.anchorMin = Vector2.zero; // 패널 전체 영역 시작 적용
            textRect.anchorMax = Vector2.one; // 패널 전체 영역 끝 적용
            textRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            textRect.offsetMin = new Vector2(16f, 4f); // 좌하단 내부 여백 적용
            textRect.offsetMax = new Vector2(-16f, -4f); // 우상단 내부 여백 적용
        }

        private void SetVisible(bool visible)
        {
            if (_seedRoot == null) return; // Seed 패널 생성 전 처리 차단
            if (_seedRoot.activeSelf == visible) return; // 동일 활성 상태 중복 처리 차단
            _seedRoot.SetActive(visible); // Seed 패널 표시 상태 적용
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 폰트 캐시 재사용

            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" },
                24); // 한글 시스템 폰트 생성

            if (_runtimeFont == null)
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            }

            return _runtimeFont; // 최종 폰트 반환
        }
    }
}
