using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour와 런타임 UI 생성을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 Scene 이름 확인을 사용하기 위한 네임스페이스
using UnityEngine.UI; // Canvas와 Text UI를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController와 TurnManager를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController를 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public class BattleInteractionStatusUI : MonoBehaviour // 전투 입력 상태와 보드 색상 의미를 간단히 안내하는 컴포넌트
    {
        private BoardInputController _boardInput; // 실제 선택·이동 후보 상태를 제공하는 보드 입력 컨트롤러
        private TurnManager _turnManager; // 현재 턴 상태와 최초 배치 상태를 제공하는 턴 매니저
        private Canvas _canvas; // 입력 안내 전용 Screen Space Overlay Canvas
        private GameObject _panelRoot; // 안내 패널 전체 표시 루트
        private Text _instructionText; // 현재 행동 안내 문구 Text
        private Text _legendText; // 이동·공격·배치 색상 범례 Text
        private string _lastInstruction = string.Empty; // 동일 문구 반복 갱신 방지용 이전 안내 문구
        private bool _legacyOverlaysSuppressed; // 62일차 BattleHUD와 중복되는 구형 턴 UI 숨김 완료 여부
        private static Font _runtimeFont; // 한글 표시용 런타임 폰트 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle Scene 로드 직후 자동 생성을 등록하는 특성
        private static void AutoCreateForBattleScene() // Battle Scene에서 전투 입력 안내 UI를 자동 생성하는 메서드
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 Scene이 Battle이 아니면
            {
                return; // 다른 Scene에는 전투 입력 안내를 생성하지 않음
            }

            if (Object.FindFirstObjectByType<BattleInteractionStatusUI>() != null) // 이미 같은 UI가 존재하면
            {
                return; // 중복 생성을 차단
            }

            var host = new GameObject("BattleInteractionStatusUI_Day63"); // 63일차 전투 입력 안내 호스트 생성
            host.AddComponent<BattleInteractionStatusUI>(); // 자동 Bind를 수행할 컴포넌트 추가
        }

        private void Start() // 런타임 생성 직후 호출되는 초기화 메서드
        {
            StartCoroutine(BindWhenBattleReady()); // BattleController 초기화가 끝난 뒤 연결하도록 코루틴 시작
        }

        private IEnumerator BindWhenBattleReady() // 전투 핵심 객체가 준비될 때까지 기다렸다 연결하는 코루틴
        {
            const int maxFrames = 180; // Scene 초기화 지연을 허용할 최대 프레임 수

            for (int frame = 0; frame < maxFrames; frame++) // 제한 프레임 동안 전투 객체를 탐색하며
            {
                var battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
                var boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 현재 BoardInputController 탐색

                if (battleController != null && boardInput != null && battleController.TurnManager != null) // 필요한 전투 객체가 모두 준비됐으면
                {
                    Bind(boardInput, battleController.TurnManager); // 실제 입력 상태와 턴 상태 연결
                    yield break; // 연결 완료 후 대기 코루틴 종료
                }

                yield return null; // 아직 준비되지 않았으면 다음 프레임까지 대기
            }

            Debug.LogWarning("Day63 BattleInteractionStatusUI: BattleController 또는 BoardInputController 연결을 찾지 못했습니다."); // 자동 연결 실패 원인 로그 출력
        }

        public void Bind(BoardInputController boardInput, TurnManager turnManager) // 실제 전투 입력 상태와 턴 상태를 UI에 연결하는 메서드
        {
            if (_boardInput != null) // 이전 보드 입력이 연결돼 있었다면
            {
                _boardInput.SelectionChanged -= HandleSelectionChanged; // 이전 기물 선택 이벤트 구독 해제
                _boardInput.HandChanged -= HandleHandChanged; // 이전 손패 변경 이벤트 구독 해제
            }

            if (_turnManager != null) // 이전 턴 매니저가 연결돼 있었다면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 이전 턴 변경 이벤트 구독 해제
            }

            _boardInput = boardInput; // 새 보드 입력 참조 저장
            _turnManager = turnManager; // 새 턴 매니저 참조 저장
            EnsureUI(); // 안내 Canvas와 패널 생성 보장

            if (_boardInput != null) // 정상 보드 입력이 연결됐으면
            {
                _boardInput.SelectionChanged += HandleSelectionChanged; // 기물 선택 변화 즉시 갱신 이벤트 구독
                _boardInput.HandChanged += HandleHandChanged; // 카드 소비·드로우 후 안내 갱신 이벤트 구독
            }

            if (_turnManager != null) // 정상 턴 매니저가 연결됐으면
            {
                _turnManager.TurnChanged += HandleTurnChanged; // 턴 전환 시 안내 갱신 이벤트 구독
            }

            SuppressLegacyTurnOverlays(); // BattleHUD와 중복되는 구형 턴 UI를 화면에서 숨김
            Refresh(); // 현재 전투 상태를 즉시 안내 문구에 반영
        }

        private void LateUpdate() // 선택·입력 상태가 같은 프레임에 연속 변경되는 경우를 보정하는 메서드
        {
            if (!_legacyOverlaysSuppressed) // 구형 턴 UI 숨김이 아직 끝나지 않았으면
            {
                SuppressLegacyTurnOverlays(); // 늦게 생성된 구형 Canvas까지 다시 탐색해 숨김
            }

            Refresh(); // 현재 상태와 마지막 문구가 달라졌을 때만 화면 갱신
        }

        private void HandleSelectionChanged(ProjectEta.Pieces.PieceRuntimeState piece) // 보드 기물 선택 변화 이벤트 처리 메서드
        {
            Refresh(); // 새 이동·공격 후보 수를 안내에 반영
        }

        private void HandleHandChanged() // 손패 변화 이벤트 처리 메서드
        {
            Refresh(); // 현재 전투 입력 안내를 다시 계산
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 변화 이벤트 처리 메서드
        {
            Refresh(); // 현재 턴에 맞는 조작 안내로 갱신
        }

        private void Refresh() // 현재 전투 상태를 안내 패널에 반영하는 메서드
        {
            if (_panelRoot == null || _instructionText == null || _turnManager == null) // UI 또는 턴 연결이 아직 준비되지 않았으면
            {
                return; // 화면 갱신을 생략
            }

            bool hasSelectedPiece = _boardInput != null && _boardInput.SelectedPiece != null; // 현재 기물 선택 여부 계산
            int moveCount = _boardInput?.PendingMovement?.MoveTiles.Count ?? 0; // 현재 이동 가능 칸 수 계산
            int attackCount = _boardInput?.PendingMovement?.AttackTiles.Count ?? 0; // 현재 공격 가능 칸 수 계산
            string instruction = BattleInteractionPresentation.BuildInstruction(_turnManager.CurrentState, _turnManager.IsInitialDeployment, _turnManager.IsInitialKingPlaced, hasSelectedPiece, moveCount, attackCount); // 최종 안내 문구 계산
            bool shouldShow = !string.IsNullOrEmpty(instruction); // 전투 종료가 아닌 표시 가능 상태 계산

            if (_panelRoot.activeSelf != shouldShow) // 패널 표시 상태가 달라졌으면
            {
                _panelRoot.SetActive(shouldShow); // 현재 전투 상태에 맞춰 패널 표시 여부 변경
            }

            if (!shouldShow || instruction == _lastInstruction) // 숨김 상태이거나 이전과 같은 문구면
            {
                return; // 불필요한 Text 재할당 생략
            }

            _lastInstruction = instruction; // 새 안내 문구 캐시 저장
            _instructionText.text = instruction; // 현재 행동 안내 문구 화면 반영
        }

        private void SuppressLegacyTurnOverlays() // BattleHUD와 역할이 겹치는 구형 턴 Canvas를 숨기는 메서드
        {
            bool turnStatusSuppressed = false; // 구형 턴 상태 Canvas 숨김 결과 초기화
            bool deploymentBannerSuppressed = false; // 구형 배치 배너 Canvas 숨김 결과 초기화
            var turnStatusCanvas = GameObject.Find("TurnStatusCanvas"); // 17일차 구형 턴 상태 Canvas 탐색
            var deploymentBannerCanvas = GameObject.Find("DeploymentTurnBannerCanvas"); // 32일차 구형 배치 배너 Canvas 탐색

            if (turnStatusCanvas != null) // 구형 턴 상태 Canvas가 활성 상태로 존재하면
            {
                turnStatusCanvas.SetActive(false); // 62일차 BattleHUD와 중복되지 않도록 숨김
                turnStatusSuppressed = true; // 구형 턴 상태 숨김 완료 기록
            }
            else // 이미 숨겨졌거나 아직 생성되지 않았다면
            {
                var turnStatus = Object.FindFirstObjectByType<TurnStatusUI>(); // TurnStatusUI 컴포넌트 자체를 탐색
                turnStatusSuppressed = turnStatus != null && turnStatus.StatusCanvas != null && !turnStatus.StatusCanvas.gameObject.activeSelf; // 이미 비활성화된 Canvas 상태 확인
            }

            if (deploymentBannerCanvas != null) // 구형 배치 배너 Canvas가 활성 상태로 존재하면
            {
                deploymentBannerCanvas.SetActive(false); // BattleHUD의 배치 상태 표시와 중복되지 않도록 숨김
                deploymentBannerSuppressed = true; // 구형 배치 배너 숨김 완료 기록
            }
            else // 이미 숨겨졌거나 아직 생성되지 않았다면
            {
                var deploymentBanner = Object.FindFirstObjectByType<DeploymentTurnBannerUI>(); // 배치 배너 컴포넌트 존재 여부 확인
                deploymentBannerSuppressed = deploymentBanner != null; // 컴포넌트가 준비됐다면 초기 숨김 시도 완료로 간주
            }

            _legacyOverlaysSuppressed = turnStatusSuppressed && deploymentBannerSuppressed; // 두 구형 UI 처리 완료 여부 저장
        }

        private void EnsureUI() // 입력 안내 Canvas와 패널을 한 번만 생성하는 메서드
        {
            if (_canvas != null) // 이미 Canvas가 생성되어 있으면
            {
                return; // 중복 UI 생성 생략
            }

            var canvasObject = new GameObject("BattleInteractionStatusCanvas_Day63", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // 전투 입력 안내 전용 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 63일차 UI 호스트의 자식으로 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // 생성한 Canvas 참조 확보
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 좌표 기반 UI로 표시
            _canvas.sortingOrder = 189; // 62일차 BattleHUD 바로 아래 계층으로 배치

            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응용 CanvasScaler 확보
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 기반 크기 보정 사용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 현재 프로젝트 공통 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로·세로 화면비를 함께 고려
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로를 동일 비중으로 보정

            var panelObject = new GameObject("BattleInteractionPanel", typeof(RectTransform), typeof(Image)); // 입력 안내 배경 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 자식으로 배치
            _panelRoot = panelObject; // 표시 제어용 패널 루트 저장
            var panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 확보
            panelRect.anchorMin = new Vector2(0f, 1f); // 화면 좌측 상단 기준 앵커 설정
            panelRect.anchorMax = new Vector2(0f, 1f); // 화면 좌측 상단 기준 앵커 고정
            panelRect.pivot = new Vector2(0f, 1f); // 패널 좌상단을 위치 기준점으로 사용
            panelRect.anchoredPosition = new Vector2(24f, -130f); // 상단 BattleHUD 아래쪽에 기능적 임시 위치 적용
            panelRect.sizeDelta = new Vector2(620f, 78f); // 안내 두 줄을 담을 패널 크기 적용
            var panelImage = panelObject.GetComponent<Image>(); // 패널 배경 Image 확보
            panelImage.color = new Color(0.035f, 0.045f, 0.06f, 0.88f); // 보드를 크게 가리지 않는 반투명 어두운 배경 적용
            panelImage.raycastTarget = false; // 안내 패널이 보드 클릭을 막지 않도록 설정

            _instructionText = CreateText("InstructionText", panelObject.transform, 21, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white); // 현재 행동 안내 Text 생성
            SetRect(_instructionText.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(584f, 34f)); // 패널 상단에 행동 안내 배치
            _instructionText.supportRichText = true; // 향후 부분 색상 표시가 가능하도록 Rich Text 활성화

            _legendText = CreateText("LegendText", panelObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.88f, 0.9f, 0.94f, 1f)); // 보드 색상 범례 Text 생성
            SetRect(_legendText.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -42f), new Vector2(584f, 26f)); // 행동 안내 아래에 범례 배치
            _legendText.supportRichText = true; // 색상 사각형 표시를 위한 Rich Text 활성화
            _legendText.text = BattleInteractionPresentation.BuildLegend(); // 고정 보드 색상 범례 문구 적용
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle style, TextAnchor alignment, Color color) // 공통 Text 생성 보조 메서드
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)); // RectTransform·Text·Outline을 가진 오브젝트 생성
            textObject.transform.SetParent(parent, false); // 전달받은 UI 부모에 연결
            var text = textObject.GetComponent<Text>(); // Text 컴포넌트 확보
            text.font = GetRuntimeFont(); // 한글 대응 런타임 폰트 적용
            text.fontSize = fontSize; // 지정 글자 크기 적용
            text.fontStyle = style; // 지정 글자 스타일 적용
            text.alignment = alignment; // 지정 정렬 방식 적용
            text.color = color; // 지정 기본 글자색 적용
            text.raycastTarget = false; // 텍스트가 마우스 입력을 막지 않도록 설정
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 긴 안내 문구의 불필요한 자동 줄바꿈 방지
            text.verticalOverflow = VerticalWrapMode.Overflow; // 한 줄 높이 제한으로 글자가 잘리지 않도록 설정
            var outline = textObject.GetComponent<Outline>(); // 글자 외곽선 컴포넌트 확보
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f); // 밝은 보드에서도 읽히는 검은 외곽선 적용
            outline.effectDistance = new Vector2(1f, -1f); // 짧은 외곽선 거리 적용
            outline.useGraphicAlpha = true; // Text 알파값을 외곽선에도 반영
            return text; // 구성 완료 Text 반환
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size) // 고정 앵커 RectTransform 배치 보조 메서드
        {
            rect.anchorMin = anchor; // 최소 앵커 적용
            rect.anchorMax = anchor; // 최대 앵커 적용
            rect.pivot = new Vector2(0f, 1f); // 좌상단 기준 피벗 적용
            rect.anchoredPosition = anchoredPosition; // 기준점 상대 위치 적용
            rect.sizeDelta = size; // UI 요소 크기 적용
        }

        private static Font GetRuntimeFont() // 프로젝트 공통 한글 표시용 런타임 폰트를 만드는 메서드
        {
            if (_runtimeFont != null) // 이미 생성한 폰트가 있으면
            {
                return _runtimeFont; // 기존 폰트 재사용
            }

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 21); // 운영체제 한글 폰트 우선 생성

            if (_runtimeFont == null) // 시스템 폰트 생성에 실패하면
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 런타임 폰트로 대체
            }

            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private void OnDestroy() // UI 호스트가 제거될 때 이벤트 연결을 정리하는 메서드
        {
            if (_boardInput != null) // 보드 입력 연결이 남아 있으면
            {
                _boardInput.SelectionChanged -= HandleSelectionChanged; // 기물 선택 이벤트 구독 해제
                _boardInput.HandChanged -= HandleHandChanged; // 손패 변경 이벤트 구독 해제
            }

            if (_turnManager != null) // 턴 매니저 연결이 남아 있으면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 턴 변경 이벤트 구독 해제
            }
        }
    }
}
