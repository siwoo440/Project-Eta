using System.Collections; // IEnumerator 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2·Vector3 사용
using UnityEngine.EventSystems; // EventSystem 사용
using UnityEngine.InputSystem; // Keyboard 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 사용
using UnityEngine.UI; // Canvas·Button·Image·Text·CanvasGroup 사용
using ProjectEta.Battle; // BattleController·TurnManager 사용
using ProjectEta.Meta; // MetaProgressService 사용
using ProjectEta.Run; // BoardMode·RunState 사용

namespace ProjectEta.King
{
    public sealed class KingSelectionUI : MonoBehaviour
    {
        private const float SlideDuration = 0.25f; // King 카드 슬라이드 시간
        private const int CarouselSlotCount = 5; // 좌우 숨김 슬롯 포함 카드 수

        private BattleController _battleController; // 현재 BattleController
        private Canvas _canvas; // King 선택 전용 Canvas
        private GameObject _selectionRoot; // 초기 King 선택 전체 화면
        private GameObject _placementRoot; // 선택 확정 후 최초 King 배치 안내 패널
        private Text _placementText; // 선택 확정 후 최초 King 배치 안내 문구
        private Button _previousButton; // 이전 King 화살표 버튼
        private Button _nextButton; // 다음 King 화살표 버튼
        private Button _confirmButton; // 현재 King 확정 버튼
        private Text _confirmButtonText; // King 확정 버튼 문구
        private Text _pageIndicatorText; // 현재 King 페이지 문구
        private Text _detailNameText; // 상세 King 이름
        private Text _detailHpText; // 상세 King HP
        private Text _detailPassiveNameText; // 상세 패시브 이름
        private Text _detailDescriptionText; // 상세 패시브 설명
        private Text _detailStyleText; // 상세 플레이 스타일
        private Text _detailUnlockText; // 상세 해금 상태
        private readonly CarouselCardView[] _carouselCards = new CarouselCardView[CarouselSlotCount]; // 좌우 미리보기 포함 캐러셀 카드
        private KingSelectionCarouselState _carouselState; // 현재 캐러셀 페이지 상태
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem
        private string _observedRunId = string.Empty; // 현재 UI가 추적하는 런 ID
        private bool _isSliding; // 캐러셀 슬라이드 진행 여부
        private bool _selectionConfirmed; // 초기 King 선택 확정 여부
        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private sealed class CarouselCardView
        {
            public RectTransform Rect; // 카드 위치·크기
            public CanvasGroup CanvasGroup; // 카드 투명도
            public Image Background; // 카드 배경
            public Text NameText; // King 이름
            public Text EmblemText; // King 카드 중앙 표시
            public Text PassiveText; // 패시브 이름
            public Text UnlockText; // 해금 상태
        }

        private readonly struct CardPose
        {
            public Vector2 Position { get; } // 카드 상대 위치
            public Vector3 Scale { get; } // 카드 상대 크기
            public float Alpha { get; } // 카드 투명도

            public CardPose(Vector2 position, Vector3 scale, float alpha)
            {
                Position = position; // 카드 위치 저장
                Scale = scale; // 카드 크기 저장
                Alpha = alpha; // 카드 투명도 저장
            }
        }

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 탐색
            RefreshPresentation(); // King 선택·최초 배치 안내 UI 갱신
        }

        private void ResolveBattleController()
        {
            if (_battleController != null) return; // 기존 BattleController 재사용
            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색
        }

        private void RefreshPresentation()
        {
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            if (runState == null) return; // 런 준비 전 UI 생성 지연

            EnsureUI(); // King 선택 UI 생성 보장
            HandleRunChanged(runState); // 새 런 진입 시 캐러셀·확정 상태 초기화

            KingRunState kingState = KingRunStateService.Get(runState); // 현재 런 King 상태 조회
            bool canSelect = CanSelectBeforeInitialPlacement(runState); // 최초 King 배치 전 선택 가능 여부 조회

            if (canSelect && !_selectionConfirmed)
            {
                ShowSelectionMode(runState, kingState); // 캐러셀 King 선택 화면 표시
                HandleNavigationInput(); // 좌우 키보드 캐러셀 입력 처리
                return; // 선택 화면 갱신 종료
            }

            if (canSelect)
            {
                ShowPlacementPendingStatus(kingState); // 선택 확정 후 King 배치 안내 표시
                return; // 배치 안내 갱신 종료
            }

            _selectionRoot.SetActive(false); // 최초 선택 완료 후 캐러셀 화면 숨김
            _placementRoot.SetActive(false); // King 배치 완료 후 선택 UI의 안내 패널 숨김
        }

        private void HandleRunChanged(RunState runState)
        {
            string runId = runState != null ? runState.RunId : string.Empty; // 현재 런 ID 조회
            if (string.Equals(_observedRunId, runId, System.StringComparison.Ordinal)) return; // 같은 런 상태 유지

            _observedRunId = runId ?? string.Empty; // 새 런 ID 저장
            _selectionConfirmed = false; // 새 런 King 선택 확정 상태 초기화
            _isSliding = false; // 새 런 캐러셀 이동 상태 초기화

            KingRunState kingState = runState != null ? KingRunStateService.Get(runState) : null; // 새 런 King 상태 조회
            KingArchetype initial = kingState != null ? kingState.Archetype : KingArchetype.Default; // 초기 중앙 King 결정
            _carouselState = new KingSelectionCarouselState(initial); // 새 런 캐러셀 상태 생성
            RefreshCarouselContents(); // 캐러셀 카드 내용 초기화
            ApplyRestingCardPoses(); // 캐러셀 카드 기본 위치 적용
        }

        private bool CanSelectBeforeInitialPlacement(RunState runState)
        {
            if (runState == null || _battleController == null) return false; // 런·전투 컨트롤러 누락 차단
            if (runState.CurrentRound != RoundState.FirstRound) return false; // 첫 Stage 외 King 교체 차단
            if (runState.CurrentBoardMode != BoardMode.Battle) return false; // 지도 모드 King 교체 차단

            TurnManager turnManager = _battleController.TurnManager; // 현재 턴 매니저 조회
            if (turnManager == null) return false; // 턴 상태 누락 차단
            return turnManager.IsInitialDeployment && !turnManager.IsInitialKingPlaced && !turnManager.IsDeploymentChoicePending; // 최초 King 배치 전·전략 선택 외 상태만 선택 허용
        }

        private void ShowSelectionMode(RunState runState, KingRunState kingState)
        {
            _selectionRoot.SetActive(true); // 전체 화면 King 선택 UI 표시
            _placementRoot.SetActive(false); // King 선택 중 배치 안내 패널 숨김

            if (_carouselState == null)
            {
                KingArchetype initial = kingState != null ? kingState.Archetype : KingArchetype.Default; // 캐러셀 누락 시 초기 King 결정
                _carouselState = new KingSelectionCarouselState(initial); // 캐러셀 상태 복구
                RefreshCarouselContents(); // 카드 내용 복구
                ApplyRestingCardPoses(); // 카드 위치 복구
            }

            if (!_isSliding)
            {
                RefreshCarouselContents(); // 해금 상태 포함 카드 내용 최신화
                ApplyRestingCardPoses(); // 정지 상태 카드 위치 유지
                RefreshDetails(runState, kingState); // 중앙 King 상세 정보 갱신
            }

            RefreshNavigationButtons(); // 슬라이드 상태 기반 화살표 입력 갱신
        }

        private void ShowPlacementPendingStatus(KingRunState kingState)
        {
            _selectionRoot.SetActive(false); // 선택 완료 후 전체 화면 차단 해제
            _placementRoot.SetActive(true); // 선택 확정 후 King 배치 안내 패널 표시

            string displayName = kingState != null ? KingArchetypeNames.GetDisplayName(kingState.Archetype) : "기본 킹"; // 확정 King 표시 이름 조회
            _placementText.text = $"선택 완료 · {displayName}  |  보드에 King을 배치하세요"; // 초기 King 배치 안내 표시
        }

        private void HandleNavigationInput()
        {
            if (_isSliding || _carouselState == null) return; // 슬라이드 중 키보드 중복 입력 차단

            Keyboard keyboard = Keyboard.current; // 현재 키보드 입력 장치 조회
            if (keyboard == null) return; // 키보드 없음 처리

            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                RequestCarouselMove(-1); // 왼쪽 화살표 이전 King 이동
                return; // 한 프레임 단일 이동 처리
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                RequestCarouselMove(1); // 오른쪽 화살표 다음 King 이동
            }
        }

        private void RequestCarouselMove(int direction)
        {
            if (_isSliding || _carouselState == null || direction == 0) return; // 중복·잘못된 캐러셀 이동 차단
            StartCoroutine(AnimateCarousel(direction > 0 ? 1 : -1)); // 방향 정규화 후 슬라이드 시작
        }

        private IEnumerator AnimateCarousel(int direction)
        {
            _isSliding = true; // 슬라이드 진행 상태 활성화
            RefreshNavigationButtons(); // 이동 중 화살표 입력 잠금

            float elapsed = 0f; // 현재 슬라이드 경과 시간

            while (elapsed < SlideDuration)
            {
                elapsed += Time.unscaledDeltaTime; // TimeScale 영향을 받지 않는 UI 시간 누적
                float normalized = Mathf.Clamp01(elapsed / SlideDuration); // 0~1 진행률 계산
                float eased = normalized * normalized * (3f - 2f * normalized); // 부드러운 SmoothStep 보간

                for (int i = 0; i < _carouselCards.Length; i++)
                {
                    int offset = i - 2; // 현재 카드 논리 상대 위치 계산
                    CardPose from = GetCardPose(offset); // 현재 슬롯 Pose 조회
                    CardPose to = GetCardPose(offset - direction); // 이동 방향 목표 Pose 조회
                    ApplyInterpolatedPose(_carouselCards[i], from, to, eased); // 카드 위치·크기·투명도 슬라이드 적용
                }

                yield return null; // 다음 프레임 애니메이션 진행
            }

            _carouselState.Move(direction); // 슬라이드 완료 후 중앙 King 페이지 변경
            RefreshCarouselContents(); // 새 중앙 기준 카드 내용 재배치
            ApplyRestingCardPoses(); // 카드 슬롯 정지 Pose 재적용
            RefreshDetails(_battleController != null ? _battleController.RunState : null, ResolveKingState()); // 새 중앙 King 상세 정보 갱신
            _isSliding = false; // 슬라이드 진행 상태 해제
            RefreshNavigationButtons(); // 화살표 입력 재활성화
        }

        private KingRunState ResolveKingState()
        {
            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            return runState != null ? KingRunStateService.Get(runState) : null; // 현재 런 King 상태 반환
        }

        private void RefreshCarouselContents()
        {
            if (_carouselState == null) return; // 캐러셀 상태 준비 전 차단

            MetaProgressState progress = MetaProgressService.Current; // 현재 영구 진행 상태 조회

            for (int i = 0; i < _carouselCards.Length; i++)
            {
                int offset = i - 2; // 현재 카드 상대 페이지 위치 계산
                KingArchetype archetype = _carouselState.Peek(offset); // 상대 위치 King 타입 조회
                KingSelectionPresentation presentation = KingSelectionPresentationCatalog.Get(archetype); // King 카드 표시 정보 조회
                bool unlocked = KingUnlockRules.CanSelect(archetype, progress); // 현재 런 Snapshot 기준 King 해금 여부 조회
                ConfigureCarouselCard(_carouselCards[i], presentation, unlocked); // 카드 이름·패시브·잠금 상태 갱신
            }

            RefreshPageIndicator(); // 현재 페이지 표시 갱신
        }

        private void ConfigureCarouselCard(CarouselCardView view, KingSelectionPresentation presentation, bool unlocked)
        {
            if (view == null || presentation == null) return; // 카드 뷰·표시 정보 누락 차단

            view.NameText.text = presentation.DisplayName; // King 카드 이름 적용
            view.EmblemText.text = "KING"; // 임시 King 카드 중앙 표식 적용
            view.PassiveText.text = presentation.PassiveName; // King 카드 패시브 이름 적용
            view.UnlockText.text = unlocked ? "사용 가능" : "잠김"; // King 카드 해금 상태 적용
            view.UnlockText.color = unlocked ? new Color(0.62f, 0.88f, 0.72f, 1f) : new Color(0.9f, 0.55f, 0.5f, 1f); // 해금 상태 색상 적용
            view.Background.color = unlocked ? GetArchetypeColor(presentation.Archetype) : new Color(0.12f, 0.12f, 0.14f, 1f); // 해금 여부 기반 카드 배경 적용
        }

        private void RefreshDetails(RunState runState, KingRunState kingState)
        {
            if (_carouselState == null) return; // 캐러셀 상태 준비 전 차단

            KingArchetype archetype = _carouselState.CurrentArchetype; // 현재 중앙 King 타입 조회
            KingSelectionPresentation presentation = KingSelectionPresentationCatalog.Get(archetype); // 현재 중앙 King 표시 정보 조회
            MetaProgressState progress = MetaProgressService.Current; // 현재 영구 진행 상태 조회
            bool unlocked = KingUnlockRules.CanSelect(archetype, progress); // 현재 런 Snapshot 기준 해금 여부 조회
            bool selected = kingState != null && kingState.Archetype == archetype; // 현재 RunState 선택 King 여부 계산

            _detailNameText.text = presentation.DisplayName; // 상세 King 이름 표시
            _detailHpText.text = $"현재 King HP  {Mathf.Max(0, runState != null ? runState.KingHp : 0)}"; // 현재 런 King HP 표시
            _detailPassiveNameText.text = $"패시브  {presentation.PassiveName}"; // 상세 패시브 이름 표시
            _detailDescriptionText.text = presentation.PassiveDescription; // 상세 패시브 설명 표시
            _detailStyleText.text = $"플레이 스타일  {presentation.PlayStyle}"; // 플레이 스타일 표시

            if (unlocked)
            {
                _detailUnlockText.text = selected ? "상태  현재 선택 · 사용 가능" : "상태  선택 가능"; // 해금 완료·현재 선택 상태 표시
                _detailUnlockText.color = new Color(0.62f, 0.88f, 0.72f, 1f); // 사용 가능 상태 색상 적용
                _confirmButton.interactable = !_isSliding; // 슬라이드 중 외 현재 King 확정 허용
                _confirmButtonText.text = "이 King으로 시작"; // King 선택 확정 버튼 문구 적용
            }
            else
            {
                _detailUnlockText.text = $"상태  잠김 · 영구 성장에서 {presentation.DisplayName} 해금 필요"; // 잠긴 King 해금 안내 표시
                _detailUnlockText.color = new Color(0.9f, 0.55f, 0.5f, 1f); // 잠김 상태 색상 적용
                _confirmButton.interactable = false; // 잠긴 King 확정 차단
                _confirmButtonText.text = "영구 성장에서 해금 필요"; // 잠긴 King 버튼 문구 적용
            }
        }

        private void ConfirmCurrentKing()
        {
            if (_isSliding || _carouselState == null) return; // 슬라이드 중·캐러셀 누락 확정 차단

            RunState runState = _battleController != null ? _battleController.RunState : null; // 현재 런 상태 조회
            if (!CanSelectBeforeInitialPlacement(runState)) return; // 최초 King 배치 가능 구간 외 확정 차단

            KingArchetype archetype = _carouselState.CurrentArchetype; // 현재 중앙 King 타입 조회
            MetaProgressState progress = MetaProgressService.Current; // 현재 영구 진행 상태 조회
            if (!KingUnlockRules.CanSelect(archetype, progress)) return; // 잠긴 King 확정 차단

            KingRunState kingState = KingRunStateService.Get(runState); // 현재 런 King 상태 조회
            if (kingState != null) kingState.Select(archetype); // 선택한 King 타입 런 전체 적용
            _selectionConfirmed = true; // 초기 King 선택 확정 상태 기록
            Debug.Log($"60일차 King 선택 확정: {KingArchetypeNames.GetDisplayName(archetype)}"); // King 선택 확정 결과 출력
            ShowPlacementPendingStatus(kingState); // 확정 후 보드 King 배치 안내 전환
        }

        private void RefreshPageIndicator()
        {
            if (_carouselState == null || _pageIndicatorText == null) return; // 페이지 UI 준비 전 차단

            string dots = string.Empty; // 페이지 점 문자열 초기화

            for (int i = 0; i < _carouselState.Count; i++)
            {
                if (i > 0) dots += "   "; // 페이지 점 간격 추가
                dots += i == _carouselState.CurrentIndex ? "●" : "○"; // 현재 페이지 채움 점 표시
            }

            _pageIndicatorText.text = $"{dots}     {_carouselState.PageNumber} / {_carouselState.Count}"; // 페이지 점·번호 동시 표시
        }

        private void RefreshNavigationButtons()
        {
            bool canNavigate = !_isSliding && _selectionRoot != null && _selectionRoot.activeSelf; // 현재 캐러셀 이동 가능 여부 계산
            if (_previousButton != null) _previousButton.interactable = canNavigate; // 이전 화살표 입력 상태 적용
            if (_nextButton != null) _nextButton.interactable = canNavigate; // 다음 화살표 입력 상태 적용
        }

        private void ApplyRestingCardPoses()
        {
            for (int i = 0; i < _carouselCards.Length; i++)
            {
                int offset = i - 2; // 현재 카드 논리 상대 위치 계산
                ApplyCardPose(_carouselCards[i], GetCardPose(offset)); // 정지 상태 카드 위치·크기·투명도 적용
            }
        }

        private static CardPose GetCardPose(int relativeOffset)
        {
            if (relativeOffset == 0) return new CardPose(Vector2.zero, Vector3.one, 1f); // 중앙 카드 크게·선명하게 표시

            int sign = relativeOffset < 0 ? -1 : 1; // 좌우 방향 부호 계산
            int distance = Mathf.Abs(relativeOffset); // 중앙에서 떨어진 슬롯 거리 계산

            if (distance == 1)
            {
                return new CardPose(new Vector2(sign * 520f, 6f), Vector3.one * 0.72f, 0.58f); // 좌우 이전·다음 미리보기 카드 표시
            }

            if (distance == 2)
            {
                return new CardPose(new Vector2(sign * 890f, 12f), Vector3.one * 0.56f, 0f); // 다음 슬라이드 준비용 숨김 카드 배치
            }

            return new CardPose(new Vector2(sign * 1180f, 18f), Vector3.one * 0.45f, 0f); // 화면 밖 슬라이드 퇴장 카드 배치
        }

        private static void ApplyCardPose(CarouselCardView view, CardPose pose)
        {
            if (view == null) return; // 카드 뷰 누락 방어
            view.Rect.anchoredPosition = pose.Position; // 카드 위치 적용
            view.Rect.localScale = pose.Scale; // 카드 크기 적용
            view.CanvasGroup.alpha = pose.Alpha; // 카드 투명도 적용
            view.CanvasGroup.blocksRaycasts = pose.Alpha > 0.9f; // 중앙 카드 외 Raycast 차단
        }

        private static void ApplyInterpolatedPose(CarouselCardView view, CardPose from, CardPose to, float t)
        {
            if (view == null) return; // 카드 뷰 누락 방어
            view.Rect.anchoredPosition = Vector2.LerpUnclamped(from.Position, to.Position, t); // 슬라이드 위치 보간
            view.Rect.localScale = Vector3.LerpUnclamped(from.Scale, to.Scale, t); // 슬라이드 크기 보간
            view.CanvasGroup.alpha = Mathf.LerpUnclamped(from.Alpha, to.Alpha, t); // 슬라이드 투명도 보간
            view.CanvasGroup.blocksRaycasts = false; // 이동 중 카드 입력 차단
        }

        private void EnsureUI()
        {
            if (_canvas != null) return; // 중복 Canvas 생성 차단
            EnsureEventSystem(); // King 선택 버튼 입력 보장

            var canvasObject = new GameObject("KingSelectionCanvas_Day60", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 60일차 King 선택 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // King 시스템 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 선택 UI 적용
            _canvas.sortingOrder = 215; // 일반 전투 UI 위 King 선택 UI 순서 적용

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 적용
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용

            BuildSelectionRoot(canvasObject.transform); // 캐러셀 초기 선택 화면 생성
            BuildPlacementRoot(canvasObject.transform); // 선택 확정 후 최초 King 배치 안내 패널 생성
        }

        private void BuildSelectionRoot(Transform parent)
        {
            _selectionRoot = CreateImageObject("KingSelectionRoot_Day60", parent, Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.76f)); // 초기 선택 전체 화면 입력 차단 배경 생성
            Stretch(_selectionRoot.GetComponent<RectTransform>(), 0f); // 초기 선택 배경 전체 화면 배치

            GameObject panel = CreateImageObject("KingSelectionPanel_Day60", _selectionRoot.transform, Vector2.zero, new Vector2(1540f, 1000f), new Color(0.055f, 0.065f, 0.085f, 0.99f)); // 중앙 King 선택 패널 생성

            Text eyebrow = CreateText("Eyebrow", panel.transform, 17, FontStyle.Bold, TextAnchor.MiddleCenter); // King 선택 상단 분류 문구 생성
            eyebrow.text = "RUN KING SELECTION"; // King 선택 상단 분류 문구 적용
            eyebrow.color = new Color(0.38f, 0.68f, 0.88f, 1f); // 상단 분류 강조 색상 적용
            SetCenteredRect(eyebrow.rectTransform, new Vector2(0f, 440f), new Vector2(520f, 34f)); // 상단 분류 문구 배치

            Text title = CreateText("Title", panel.transform, 38, FontStyle.Bold, TextAnchor.MiddleCenter); // King 선택 제목 생성
            title.text = "이번 런의 King을 선택하세요"; // King 선택 제목 적용
            SetCenteredRect(title.rectTransform, new Vector2(0f, 395f), new Vector2(760f, 58f)); // King 선택 제목 배치

            GameObject carouselRoot = new GameObject("CarouselRoot_Day60", typeof(RectTransform)); // King 캐러셀 루트 생성
            carouselRoot.transform.SetParent(panel.transform, false); // 선택 패널 자식 연결
            SetCenteredRect(carouselRoot.GetComponent<RectTransform>(), new Vector2(0f, 195f), new Vector2(1420f, 330f)); // 캐러셀 표시 영역 배치
            CreateCarouselCards(carouselRoot.transform); // 중앙·좌우 미리보기 카드 생성

            _previousButton = CreateArrowButton("PreviousButton", panel.transform, "◀", new Vector2(-695f, 195f)); // 이전 King 화살표 버튼 생성
            _previousButton.onClick.AddListener(() => RequestCarouselMove(-1)); // 이전 King 슬라이드 연결

            _nextButton = CreateArrowButton("NextButton", panel.transform, "▶", new Vector2(695f, 195f)); // 다음 King 화살표 버튼 생성
            _nextButton.onClick.AddListener(() => RequestCarouselMove(1)); // 다음 King 슬라이드 연결

            _pageIndicatorText = CreateText("PageIndicator", panel.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter); // King 페이지 인디케이터 생성
            SetCenteredRect(_pageIndicatorText.rectTransform, new Vector2(0f, 8f), new Vector2(620f, 40f)); // 페이지 인디케이터 배치

            GameObject detailPanel = CreateImageObject("KingDetailPanel", panel.transform, new Vector2(0f, -205f), new Vector2(1260f, 330f), new Color(0.032f, 0.04f, 0.055f, 0.98f)); // 하단 King 상세 정보 패널 생성
            BuildDetailPanel(detailPanel.transform); // King 상세 정보 항목 생성

            _confirmButton = CreateButton("ConfirmKingButton", panel.transform, "이 King으로 시작", new Vector2(0f, -430f), new Vector2(420f, 72f), out _confirmButtonText); // King 선택 확정 버튼 생성
            _confirmButton.onClick.AddListener(ConfirmCurrentKing); // 현재 중앙 King 확정 연결
        }

        private void CreateCarouselCards(Transform parent)
        {
            for (int i = 0; i < _carouselCards.Length; i++)
            {
                GameObject cardObject = new GameObject($"CarouselCard_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline)); // 캐러셀 카드 오브젝트 생성
                cardObject.transform.SetParent(parent, false); // 캐러셀 루트 자식 연결

                RectTransform rect = cardObject.GetComponent<RectTransform>(); // 카드 RectTransform 조회
                SetCenteredRect(rect, Vector2.zero, new Vector2(360f, 310f)); // 공통 카드 원본 크기 적용

                CanvasGroup canvasGroup = cardObject.GetComponent<CanvasGroup>(); // 카드 투명도 그룹 조회
                Image background = cardObject.GetComponent<Image>(); // 카드 배경 이미지 조회
                background.color = new Color(0.2f, 0.16f, 0.11f, 1f); // 기본 King 카드 배경 적용

                Outline outline = cardObject.GetComponent<Outline>(); // 카드 외곽선 조회
                outline.effectColor = new Color(0.48f, 0.64f, 0.78f, 0.7f); // 카드 외곽 강조 색상 적용
                outline.effectDistance = new Vector2(2f, -2f); // 카드 외곽선 두께 적용

                Text nameText = CreateText("KingName", cardObject.transform, 25, FontStyle.Bold, TextAnchor.MiddleCenter); // 카드 King 이름 생성
                SetCenteredRect(nameText.rectTransform, new Vector2(0f, 118f), new Vector2(310f, 46f)); // 카드 King 이름 배치

                GameObject portraitFrame = CreateImageObject("PortraitFrame", cardObject.transform, new Vector2(0f, 24f), new Vector2(210f, 125f), new Color(0.05f, 0.06f, 0.08f, 0.75f)); // 임시 King 초상 영역 생성
                Text emblemText = CreateText("Emblem", portraitFrame.transform, 30, FontStyle.Bold, TextAnchor.MiddleCenter); // 임시 King 중앙 표식 생성
                emblemText.text = "KING"; // 임시 King 중앙 표식 적용
                Stretch(emblemText.rectTransform, 4f); // 임시 King 중앙 표식 전체 배치

                Text passiveText = CreateText("Passive", cardObject.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter); // 카드 패시브 이름 생성
                passiveText.color = new Color(0.78f, 0.84f, 0.92f, 1f); // 패시브 이름 보조 강조 적용
                SetCenteredRect(passiveText.rectTransform, new Vector2(0f, -67f), new Vector2(310f, 42f)); // 카드 패시브 이름 배치

                Text unlockText = CreateText("Unlock", cardObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter); // 카드 해금 상태 생성
                SetCenteredRect(unlockText.rectTransform, new Vector2(0f, -118f), new Vector2(310f, 36f)); // 카드 해금 상태 배치

                _carouselCards[i] = new CarouselCardView
                {
                    Rect = rect, // 카드 RectTransform 저장
                    CanvasGroup = canvasGroup, // 카드 투명도 그룹 저장
                    Background = background, // 카드 배경 저장
                    NameText = nameText, // 카드 King 이름 저장
                    EmblemText = emblemText, // 카드 중앙 표식 저장
                    PassiveText = passiveText, // 카드 패시브 이름 저장
                    UnlockText = unlockText // 카드 해금 상태 저장
                };
            }
        }

        private void BuildDetailPanel(Transform parent)
        {
            _detailNameText = CreateText("DetailName", parent, 31, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 King 이름 생성
            SetCenteredRect(_detailNameText.rectTransform, new Vector2(-355f, 112f), new Vector2(450f, 54f)); // 상세 King 이름 배치

            _detailHpText = CreateText("DetailHp", parent, 19, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 King HP 생성
            _detailHpText.color = new Color(0.78f, 0.84f, 0.92f, 1f); // King HP 보조 색상 적용
            SetCenteredRect(_detailHpText.rectTransform, new Vector2(-355f, 62f), new Vector2(450f, 38f)); // 상세 King HP 배치

            _detailPassiveNameText = CreateText("PassiveName", parent, 21, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 패시브 이름 생성
            SetCenteredRect(_detailPassiveNameText.rectTransform, new Vector2(-355f, 12f), new Vector2(450f, 44f)); // 상세 패시브 이름 배치

            _detailStyleText = CreateText("PlayStyle", parent, 17, FontStyle.Normal, TextAnchor.MiddleLeft); // 상세 플레이 스타일 생성
            _detailStyleText.color = new Color(0.68f, 0.72f, 0.79f, 1f); // 플레이 스타일 보조 색상 적용
            SetCenteredRect(_detailStyleText.rectTransform, new Vector2(-355f, -45f), new Vector2(450f, 44f)); // 상세 플레이 스타일 배치

            _detailUnlockText = CreateText("UnlockState", parent, 17, FontStyle.Bold, TextAnchor.MiddleLeft); // 상세 해금 상태 생성
            SetCenteredRect(_detailUnlockText.rectTransform, new Vector2(-355f, -102f), new Vector2(450f, 50f)); // 상세 해금 상태 배치

            Text descriptionLabel = CreateText("DescriptionLabel", parent, 17, FontStyle.Bold, TextAnchor.MiddleLeft); // 패시브 설명 제목 생성
            descriptionLabel.text = "패시브 설명"; // 패시브 설명 제목 적용
            descriptionLabel.color = new Color(0.38f, 0.68f, 0.88f, 1f); // 패시브 설명 제목 강조 적용
            SetCenteredRect(descriptionLabel.rectTransform, new Vector2(275f, 105f), new Vector2(560f, 38f)); // 패시브 설명 제목 배치

            _detailDescriptionText = CreateText("Description", parent, 19, FontStyle.Normal, TextAnchor.UpperLeft); // 상세 패시브 설명 생성
            _detailDescriptionText.color = new Color(0.84f, 0.86f, 0.9f, 1f); // 패시브 설명 색상 적용
            _detailDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 패시브 설명 줄바꿈 허용
            _detailDescriptionText.verticalOverflow = VerticalWrapMode.Truncate; // 패시브 설명 영역 초과 잘라내기
            _detailDescriptionText.lineSpacing = 1.15f; // 패시브 설명 줄 간격 적용
            SetCenteredRect(_detailDescriptionText.rectTransform, new Vector2(275f, -20f), new Vector2(560f, 220f)); // 상세 패시브 설명 배치
        }

        private void BuildPlacementRoot(Transform parent)
        {
            _placementRoot = CreateImageObject("KingPlacementRoot_Day61", parent, new Vector2(285f, -62f), new Vector2(520f, 84f), new Color(0.07f, 0.045f, 0.025f, 0.93f)); // 좌측 상단 King 배치 안내 패널 생성
            RectTransform rect = _placementRoot.GetComponent<RectTransform>(); // King 배치 안내 패널 RectTransform 조회
            rect.anchorMin = new Vector2(0f, 1f); // 좌측 상단 앵커 적용
            rect.anchorMax = new Vector2(0f, 1f); // 좌측 상단 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = new Vector2(285f, -62f); // 좌측 상단 여백 적용

            _placementText = CreateText("PlacementStatus", _placementRoot.transform, 17, FontStyle.Bold, TextAnchor.MiddleCenter); // King 배치 안내 문구 생성
            _placementText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 King 배치 안내 줄바꿈 허용
            _placementText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 배치 안내 문구 잘라내기
            Stretch(_placementText.rectTransform, 14f); // King 배치 안내 문구 내부 여백 적용
            _placementRoot.SetActive(false); // 초기 배치 안내 패널 숨김
        }

        private static Button CreateArrowButton(string name, Transform parent, string label, Vector2 position)
        {
            Button button = CreateButton(name, parent, label, position, new Vector2(82f, 82f), out Text text); // 캐러셀 화살표 버튼 생성
            text.fontSize = 30; // 화살표 문구 크기 확대
            return button; // 캐러셀 화살표 버튼 반환
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, out Text text)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 공통 버튼 오브젝트 생성
            buttonObject.transform.SetParent(parent, false); // UI 부모 연결
            SetCenteredRect(buttonObject.GetComponent<RectTransform>(), position, size); // 버튼 위치·크기 적용

            Image background = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
            background.color = new Color(0.13f, 0.17f, 0.23f, 1f); // 공통 버튼 배경 적용

            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = background; // 버튼 대상 그래픽 지정
            ColorBlock colors = button.colors; // 버튼 상태 색상 조회
            colors.normalColor = Color.white; // Image 색상 기준 기본 상태 유지
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); // Hover 밝기 적용
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f); // Pressed 밝기 적용
            colors.selectedColor = colors.highlightedColor; // 선택 밝기 적용
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.72f); // Disabled 밝기 적용
            colors.fadeDuration = 0.08f; // 버튼 상태 전환 시간 적용
            button.colors = colors; // 버튼 상태 색상 저장

            text = CreateText("Label", buttonObject.transform, 21, FontStyle.Bold, TextAnchor.MiddleCenter); // 버튼 문구 생성
            text.text = label; // 버튼 문구 적용
            Stretch(text.rectTransform, 6f); // 버튼 문구 내부 여백 적용
            return button; // 완성 버튼 반환
        }

        private static GameObject CreateImageObject(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(Image)); // 공통 Image UI 오브젝트 생성
            result.transform.SetParent(parent, false); // UI 부모 연결
            SetCenteredRect(result.GetComponent<RectTransform>(), position, size); // Image 위치·크기 적용
            result.GetComponent<Image>().color = color; // Image 배경 색상 적용
            return result; // 완성 Image UI 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 런타임 Text 생성
            textObject.transform.SetParent(parent, false); // UI 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = alignment; // 요청 텍스트 정렬 적용
            text.color = Color.white; // 기본 흰색 문구 적용
            text.raycastTarget = false; // 보드 입력 간섭 제거
            return text; // 완성 Text 반환
        }

        private static Font GetRuntimeFont()
        {
            if (_runtimeFont != null) return _runtimeFont; // 기존 런타임 폰트 재사용
            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 시스템 한글 폰트 생성
            if (_runtimeFont == null) _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체
            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private static Color GetArchetypeColor(KingArchetype archetype)
        {
            if (archetype == KingArchetype.Attack) return new Color(0.31f, 0.105f, 0.09f, 1f); // 공격형 King 카드 색상
            if (archetype == KingArchetype.Defense) return new Color(0.09f, 0.18f, 0.3f, 1f); // 방어형 King 카드 색상
            if (archetype == KingArchetype.Strategy) return new Color(0.2f, 0.12f, 0.31f, 1f); // 전략형 King 카드 색상
            return new Color(0.28f, 0.22f, 0.12f, 1f); // 기본 King 카드 색상
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 앵커 시작 적용
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 앵커 끝 적용
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

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return; // 기존 EventSystem 재사용
            var eventSystemObject = new GameObject("EventSystem_Day60_King", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
        }

        private void OnDestroy()
        {
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 생성 EventSystem 제거
        }
    }
}
