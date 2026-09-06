using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.UI; // Canvas·CanvasScaler·Image·Text 사용
using ProjectEta.Battle; // BattleController·TurnState 사용
using ProjectEta.Run; // RunState·RunFlowPhase 사용

namespace ProjectEta.King
{
    [DefaultExecutionOrder(1450)]
    public sealed class KingCombatHUD : MonoBehaviour
    {
        private BattleController _battleController; // 현재 BattleController
        private Canvas _canvas; // King 전투 HUD Canvas
        private GameObject _root; // King 전투 HUD 루트
        private Text _kingNameText; // King 이름 문구
        private Text _hpText; // King HP 문구
        private Text _passiveNameText; // 패시브 이름 문구
        private Text _primaryStatusText; // 핵심 패시브 상태 문구
        private Text _secondaryStatusText; // 보조 패시브 상태 문구
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 탐색
            RefreshHud(); // 현재 런 King 상태 HUD 갱신
        }

        private void ResolveBattleController()
        {
            if (_battleController != null) return; // 기존 BattleController 재사용
            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색
        }

        private void RefreshHud()
        {
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            TurnManager turnManager = _battleController != null ? _battleController.TurnManager : null; // 현재 턴 매니저 조회

            if (!ShouldShow(runState, turnManager))
            {
                if (_root != null) _root.SetActive(false); // 비전투·King 미배치 상태 HUD 숨김
                return; // HUD 갱신 종료
            }

            EnsureUI(); // King 전투 HUD 생성 보장
            _root.SetActive(true); // King 전투 HUD 표시

            KingRunState kingState = KingRunStateService.Get(runState); // 현재 런 King 상태 조회
            bool isDeploymentTurn = turnManager.CurrentState == TurnState.DeploymentTurn; // 현재 배치 턴 여부 계산
            KingCombatHudPresentation presentation = KingCombatHudPresentation.Build(kingState, runState.KingHp, isDeploymentTurn); // 현재 King HUD 표시 정보 생성

            _kingNameText.text = presentation.KingName; // King 이름 표시
            _hpText.text = presentation.HpText; // King HP 표시
            _passiveNameText.text = presentation.PassiveName; // 패시브 이름 표시
            _primaryStatusText.text = presentation.PrimaryStatus; // 핵심 패시브 상태 표시
            _secondaryStatusText.text = presentation.SecondaryStatus; // 보조 패시브 상태 표시
        }

        private static bool ShouldShow(RunState runState, TurnManager turnManager)
        {
            if (runState == null || turnManager == null) return false; // 런·턴 상태 누락 시 HUD 숨김
            if (runState.CurrentFlowPhase != RunFlowPhase.Battle) return false; // 전투 외 Map·Reward·Shop·Event에서 HUD 숨김
            if (!turnManager.IsInitialKingPlaced) return false; // 최초 King 배치 전 HUD 숨김
            if (turnManager.CurrentState == TurnState.BattleEnded) return false; // 전투 종료 후 HUD 숨김
            return true; // 전투 중 King HUD 표시 허용
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 HUD 생성 차단

            var canvasObject = new GameObject("KingCombatHUDCanvas_Day61", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // King 전투 HUD Canvas 생성
            canvasObject.transform.SetParent(transform, false); // King 시스템 호스트 자식 연결

            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 HUD 모드 적용
            _canvas.sortingOrder = 190; // 선택 화면 아래·일반 전투 UI 위 정렬 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 모드 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용

            _root = CreateImage("KingCombatHUDRoot_Day61", canvasObject.transform, new Vector2(305f, -170f), new Vector2(560f, 260f), new Color(0.045f, 0.052f, 0.07f, 0.96f)); // 좌측 상단 King 전투 HUD 패널 생성
            RectTransform rootRect = _root.GetComponent<RectTransform>(); // HUD 루트 RectTransform 조회
            rootRect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 앵커 시작 적용
            rootRect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 앵커 끝 적용
            rootRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rootRect.anchoredPosition = new Vector2(305f, -170f); // 좌측 상단 HUD 위치 적용

            Text eyebrow = CreateText("Eyebrow", _root.transform, 15, FontStyle.Bold, TextAnchor.MiddleLeft); // King HUD 분류 문구 생성
            eyebrow.text = "KING"; // King HUD 분류 문구 적용
            eyebrow.color = new Color(0.38f, 0.68f, 0.88f, 1f); // King HUD 분류 강조 색상 적용
            SetRect(eyebrow.rectTransform, new Vector2(-205f, 98f), new Vector2(100f, 28f)); // King HUD 분류 문구 배치

            _kingNameText = CreateText("KingName", _root.transform, 25, FontStyle.Bold, TextAnchor.MiddleLeft); // King 이름 문구 생성
            SetRect(_kingNameText.rectTransform, new Vector2(-105f, 62f), new Vector2(300f, 48f)); // King 이름 문구 배치

            _hpText = CreateText("KingHp", _root.transform, 21, FontStyle.Bold, TextAnchor.MiddleRight); // King HP 문구 생성
            _hpText.color = new Color(0.92f, 0.72f, 0.72f, 1f); // King HP 강조 색상 적용
            SetRect(_hpText.rectTransform, new Vector2(165f, 62f), new Vector2(170f, 48f)); // King HP 문구 배치

            _passiveNameText = CreateText("PassiveName", _root.transform, 19, FontStyle.Bold, TextAnchor.MiddleLeft); // 패시브 이름 문구 생성
            _passiveNameText.color = new Color(0.78f, 0.84f, 0.92f, 1f); // 패시브 이름 보조 강조 적용
            SetRect(_passiveNameText.rectTransform, new Vector2(0f, 12f), new Vector2(470f, 40f)); // 패시브 이름 문구 배치

            _primaryStatusText = CreateText("PrimaryStatus", _root.transform, 20, FontStyle.Bold, TextAnchor.MiddleLeft); // 핵심 패시브 상태 문구 생성
            SetRect(_primaryStatusText.rectTransform, new Vector2(0f, -38f), new Vector2(470f, 42f)); // 핵심 패시브 상태 문구 배치

            _secondaryStatusText = CreateText("SecondaryStatus", _root.transform, 16, FontStyle.Normal, TextAnchor.MiddleLeft); // 보조 패시브 상태 문구 생성
            _secondaryStatusText.color = new Color(0.68f, 0.72f, 0.79f, 1f); // 보조 상태 색상 적용
            _secondaryStatusText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 보조 상태 줄바꿈 허용
            _secondaryStatusText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 문구 잘라내기
            SetRect(_secondaryStatusText.rectTransform, new Vector2(0f, -88f), new Vector2(470f, 52f)); // 보조 패시브 상태 문구 배치
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Image)); // Image UI 오브젝트 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            SetRect(result.GetComponent<RectTransform>(), position, size); // UI 위치·크기 적용
            result.GetComponent<Image>().color = color; // UI 배경 색상 적용
            return result; // 완성 Image UI 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text UI 오브젝트 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결

            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = alignment; // 텍스트 정렬 적용
            text.color = Color.white; // 기본 글자색 적용
            text.raycastTarget = false; // 전투 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 fallback
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // UI 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }
    }
}
