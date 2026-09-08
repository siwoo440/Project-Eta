using System.Collections.Generic; // 카드 입력 오버레이 목록 사용
using UnityEngine; // MonoBehaviour·GameObject·Color·Vector2 사용
using UnityEngine.EventSystems; // 포인터 클릭·EventSystem 사용
using UnityEngine.InputSystem.UI; // 새 Input System UI 입력 모듈 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 확인
using UnityEngine.UI; // Canvas·Button·Image·Text 사용
using ProjectEta.Board; // BoardInputController 사용
using ProjectEta.Fusion; // FusionRecipe·FusionBlockReason·FusionHandSelectionState 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 네임스페이스
{
    [DefaultExecutionOrder(1200)] // 기존 전투 입력·UI 갱신 뒤 실행 순서
    public sealed class Day64FusionUI : MonoBehaviour // 64일차 카드·Fusion 통합 정식 UI
    {
        private const int CanvasOrder = 98; // 손패보다 위쪽 Fusion UI 정렬 순서
        private const float DiscoveryNoticeDuration = 3f; // Recipe 발견 알림 유지 시간

        private static Font _runtimeFont; // 한글 런타임 폰트 캐시

        private readonly FusionHandSelectionState _selection = new FusionHandSelectionState(); // 실제 손패 슬롯 기반 재료 선택 상태
        private readonly List<Day64FusionCardHitArea> _cardHitAreas = new List<Day64FusionCardHitArea>(); // 현재 손패 카드 입력 오버레이 목록

        private BoardInputController _boardInput; // 실제 합성 규칙·손패 상태 제공자
        private Canvas _canvas; // 64일차 Fusion 전용 Canvas
        private GameObject _panelRoot; // A+B=C 결과 패널 루트
        private Button _toggleButton; // 합성 모드 진입·종료 버튼
        private Text _toggleButtonText; // 합성 토글 버튼 문구
        private Button _materialAButton; // 재료 A 선택 해제 버튼
        private Text _materialAText; // 재료 A 이름
        private Button _materialBButton; // 재료 B 선택 해제 버튼
        private Text _materialBText; // 재료 B 이름
        private Text _expressionText; // A+B=C 문구
        private Image _resultArtwork; // 결과 카드 Artwork
        private Text _resultNameText; // 결과 카드 이름
        private Text _resultStatsText; // 결과 등급·ATK·HP
        private Text _resultDescriptionText; // 결과 카드 설명
        private Text _statusText; // 현재 합성 선택 안내·차단 사유
        private Button _confirmButton; // 실제 합성 확정 버튼
        private Text _confirmButtonText; // 합성 확정 버튼 문구
        private Text _discoveryNoticeText; // Recipe 발견 알림
        private float _discoveryNoticeRemaining; // Recipe 발견 알림 남은 시간
        private EventSystem _createdEventSystem; // 직접 만든 EventSystem 참조
        private GameObject _legacyFusionCanvas; // 21~22일차 구형 Fusion Canvas 참조

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 진입 뒤 자동 설치
        private static void AutoCreateForBattleScene() // 씬 수동 배치 없이 64일차 UI 자동 생성
        {
            if (SceneManager.GetActiveScene().name != "Battle") // Battle 씬 여부 확인
            {
                return; // 다른 씬 생성 차단
            }

            if (Object.FindFirstObjectByType<Day64FusionUI>() != null) // 기존 64일차 UI 확인
            {
                return; // 중복 생성 차단
            }

            var host = new GameObject("Day64FusionUI"); // 64일차 Fusion UI 호스트 생성
            host.AddComponent<Day64FusionUI>(); // 자동 동작 컴포넌트 추가
        }

        private void Awake() // 컴포넌트 초기화
        {
            EnsureEventSystem(); // UI 클릭 입력 시스템 보장
            EnsureUI(); // 64일차 Fusion UI 생성
        }

        private void Update() // 전투 상태와 손패 UI 동기화
        {
            ResolveBindings(); // BoardInputController 연결 보장
            SuppressLegacyFusionUI(); // 구형 Fusion Canvas 중복 표시 차단
            UpdateDiscoveryNotice(); // Recipe 발견 알림 시간 갱신
            RefreshToggleState(); // 배치 턴 기준 합성 버튼 상태 갱신
            SyncCardHitAreas(); // 현재 손패 카드 강조·클릭 오버레이 동기화
        }

        private void ResolveBindings() // 현재 Battle 씬 입력 컨트롤러 자동 연결
        {
            if (_boardInput != null) // 이미 연결된 입력 컨트롤러 확인
            {
                return; // 기존 연결 재사용
            }

            BoardInputController found = Object.FindFirstObjectByType<BoardInputController>(); // 씬 BoardInputController 탐색

            if (found == null) // 아직 전투 입력이 생성되지 않은 상태 확인
            {
                return; // 다음 프레임 재탐색
            }

            _boardInput = found; // 실제 입력 컨트롤러 저장
            _boardInput.HandChanged += HandleHandChanged; // 합성·드로우·소환 뒤 손패 갱신 구독
            _boardInput.FusionSelectionChanged += HandleFusionModeChanged; // 기존 합성 모드 상태 변경 구독
            _selection.Clear(); // 이전 선택 상태 초기화
            RefreshAll(); // 연결 직후 전체 UI 갱신
        }

        private void HandleHandChanged() // 실제 손패 변경 이벤트 처리
        {
            _selection.Clear(); // 손패 인덱스 변경 가능성에 대비한 재료 선택 초기화
            RefreshAll(); // 새 손패 기준 Fusion UI 갱신
        }

        private void HandleFusionModeChanged() // BoardInputController 합성 모드 변경 처리
        {
            if (_boardInput == null || !_boardInput.IsFusionModeActive) // 합성 모드 종료 여부 확인
            {
                _selection.Clear(); // 종료 시 재료 선택 초기화
            }

            RefreshAll(); // 모드 상태와 패널 갱신
        }

        private void OnToggleButtonClicked() // 64일차 합성 모드 버튼 처리
        {
            if (_boardInput == null) // 입력 컨트롤러 연결 확인
            {
                return; // 연결 전 클릭 무시
            }

            bool nextActive = !_boardInput.IsFusionModeActive; // 다음 합성 모드 상태 계산
            bool changed = _boardInput.SetFusionModeActive(nextActive); // 기존 배치 턴 합성 모드 규칙 사용

            if (!changed) // 배치 턴 외 진입 거부 확인
            {
                RefreshAll(); // 현재 상태만 다시 표시
                return; // 추가 처리 종료
            }

            if (!nextActive) // 합성 모드 종료 여부 확인
            {
                _selection.Clear(); // 재료 선택 초기화
            }

            RefreshAll(); // 버튼·패널·카드 강조 즉시 갱신
        }

        public void ToggleHandCard(int handIndex) // 카드 오버레이에서 실제 손패 슬롯 재료 선택 처리
        {
            if (_boardInput == null || !_boardInput.IsFusionModeActive || !_boardInput.CanUseFusionInput) // 합성 입력 가능 상태 확인
            {
                return; // 배치 턴 외 선택 차단
            }

            IReadOnlyList<PieceDefinition> hand = _boardInput.HandState != null ? _boardInput.HandState.Hand : null; // 현재 실제 손패 조회

            if (hand == null) // 손패 연결 확인
            {
                return; // 손패 없으면 선택 차단
            }

            _selection.Prune(hand.Count); // 현재 손패 범위 밖 선택 제거

            if (handIndex < 0 || handIndex >= hand.Count) // 클릭 슬롯 범위 확인
            {
                return; // 잘못된 슬롯 클릭 차단
            }

            if (_selection.Contains(handIndex)) // 이미 선택된 재료 카드 확인
            {
                _selection.Toggle(handIndex); // 해당 슬롯만 선택 해제
                RefreshAll(); // 선택 해제 즉시 반영
                return; // 처리 종료
            }

            if (_selection.Count == 0) // 첫 번째 재료 선택 단계 확인
            {
                if (!CanUseAsFirstMaterial(handIndex, hand)) // 현재 손패에서 합성 상대 존재 여부 확인
                {
                    RefreshAll(); // 비활성 상태 유지
                    return; // 조합 불가능 카드 선택 차단
                }

                _selection.Toggle(handIndex); // 첫 번째 재료의 실제 손패 슬롯 저장
                RefreshAll(); // 두 번째 후보 강조 갱신
                return; // 첫 재료 처리 종료
            }

            if (_selection.Count == 1) // 두 번째 재료 선택 단계 확인
            {
                int firstIndex = _selection.Indices[0]; // 첫 재료 실제 손패 슬롯 조회

                if (!CanPair(firstIndex, handIndex, hand)) // 두 카드 실제 합성 가능 여부 확인
                {
                    RefreshAll(); // 현재 후보 강조 유지
                    return; // 합성 불가 후보 선택 차단
                }

                _selection.Toggle(handIndex); // 두 번째 재료의 실제 손패 슬롯 저장
                RefreshAll(); // A+B=C 결과 미리보기 갱신
            }
        }

        private bool CanUseAsFirstMaterial(int handIndex, IReadOnlyList<PieceDefinition> hand) // 첫 재료로 선택 가능한 카드인지 판정
        {
            for (int i = 0; i < hand.Count; i++) // 현재 손패의 모든 상대 카드 검사
            {
                if (i == handIndex) // 자기 자신 슬롯 확인
                {
                    continue; // 같은 실제 슬롯 조합 제외
                }

                if (CanPair(handIndex, i, hand)) // 유효한 합성 상대 확인
                {
                    return true; // 첫 재료 선택 가능 반환
                }
            }

            return false; // 현재 손패에 합성 상대 없음 반환
        }

        private bool CanPair(int firstIndex, int secondIndex, IReadOnlyList<PieceDefinition> hand) // 서로 다른 두 실제 손패 슬롯 합성 가능 여부 판정
        {
            if (_boardInput == null || hand == null) // 필수 참조 확인
            {
                return false; // 판정 불가 반환
            }

            if (firstIndex < 0 || firstIndex >= hand.Count || secondIndex < 0 || secondIndex >= hand.Count) // 두 슬롯 범위 확인
            {
                return false; // 잘못된 슬롯 차단
            }

            if (firstIndex == secondIndex) // 동일 실제 슬롯 여부 확인
            {
                return false; // 카드 한 장을 두 번 쓰는 조합 차단
            }

            FusionBlockReason reason = _boardInput.EvaluateFusion(hand[firstIndex], hand[secondIndex], out FusionRecipe recipe); // 기존 합성 규칙으로 후보 평가
            return reason == FusionBlockReason.None && recipe != null && recipe.Result != null; // 실제 결과가 있는 조합만 후보 허용
        }

        private void OnMaterialSlotClicked(int selectionSlot) // 패널 재료 A/B 클릭으로 개별 선택 해제
        {
            if (_selection.RemoveSelectionSlot(selectionSlot)) // 지정 선택 슬롯 제거 성공 확인
            {
                RefreshAll(); // 카드 강조·미리보기 동기화
            }
        }

        private void OnConfirmButtonClicked() // A+B=C 합성 확정 처리
        {
            if (_boardInput == null || _selection.Count != 2 || _boardInput.HandState == null) // 필수 선택·손패 상태 확인
            {
                return; // 합성 실행 차단
            }

            IReadOnlyList<PieceDefinition> hand = _boardInput.HandState.Hand; // 현재 손패 참조
            _selection.Prune(hand.Count); // 손패 변경으로 무효화된 선택 제거

            if (_selection.Count != 2) // 선택 유효성 재확인
            {
                RefreshAll(); // 무효 선택 정리 반영
                return; // 합성 실행 차단
            }

            int firstIndex = _selection.Indices[0]; // 재료 A 실제 손패 슬롯 조회
            int secondIndex = _selection.Indices[1]; // 재료 B 실제 손패 슬롯 조회
            PieceDefinition materialA = hand[firstIndex]; // 재료 A 정의 조회
            PieceDefinition materialB = hand[secondIndex]; // 재료 B 정의 조회
            FusionBlockReason reason = _boardInput.EvaluateFusion(materialA, materialB, out FusionRecipe recipe); // 확정 직전 기존 규칙 재검증

            if (reason != FusionBlockReason.None || recipe == null || recipe.Result == null) // 레시피 무효화 여부 확인
            {
                RefreshAll(); // 최신 차단 사유 표시
                return; // 합성 상태 변경 차단
            }

            bool wasUndiscovered = _boardInput.RunState != null && !_boardInput.RunState.FusionDiscovery.IsDiscovered(recipe); // 합성 전 숨김 Recipe 발견 여부 저장
            bool fused = _boardInput.TryFuseCards(materialA, materialB); // 기존 카드 소모·결과 추가·보유 풀·발견 저장 로직 실행

            if (!fused) // 합성 실행 실패 확인
            {
                RefreshAll(); // 실패 원인 기준 UI 재평가
                return; // 처리 종료
            }

            _selection.Clear(); // 성공 뒤 다음 연속 합성을 위한 재료 선택 초기화

            if (wasUndiscovered) // 이번 합성으로 새 Recipe 발견 여부 확인
            {
                ShowDiscoveryNotice(recipe); // Recipe 발견 알림 표시
            }

            RefreshAll(); // 합성 결과 손패·패널 즉시 갱신
        }

        private void ShowDiscoveryNotice(FusionRecipe recipe) // 새 Recipe 발견 알림 표시
        {
            if (_discoveryNoticeText == null || recipe == null || recipe.Result == null) // 표시 필수 정보 확인
            {
                return; // 잘못된 알림 차단
            }

            _discoveryNoticeText.text = $"Recipe 발견! {GetDisplayName(recipe.Result)}"; // 발견 결과 카드 이름 표시
            _discoveryNoticeText.gameObject.SetActive(true); // 발견 알림 활성화
            _discoveryNoticeRemaining = DiscoveryNoticeDuration; // 자동 숨김 시간 시작
        }

        private void UpdateDiscoveryNotice() // Recipe 발견 알림 시간 갱신
        {
            if (_discoveryNoticeRemaining <= 0f) // 활성 알림 시간 확인
            {
                return; // 표시 중인 알림 없음
            }

            _discoveryNoticeRemaining -= Time.unscaledDeltaTime; // 게임 시간 배율과 무관한 알림 시간 감소

            if (_discoveryNoticeRemaining > 0f) // 남은 시간 확인
            {
                return; // 알림 유지
            }

            _discoveryNoticeRemaining = 0f; // 남은 시간 보정

            if (_discoveryNoticeText != null) // 알림 Text 존재 확인
            {
                _discoveryNoticeText.gameObject.SetActive(false); // Recipe 발견 알림 숨김
            }
        }

        private void RefreshAll() // 64일차 Fusion UI 전체 상태 갱신
        {
            RefreshToggleState(); // 합성 버튼 상태 갱신
            RefreshPanel(); // A+B=C 패널 갱신
            SyncCardHitAreas(); // 손패 카드 선택·후보 강조 갱신
        }

        private void RefreshToggleState() // 합성 진입 버튼 상태 갱신
        {
            if (_toggleButton == null) // 버튼 생성 여부 확인
            {
                return; // 생성 전 갱신 차단
            }

            bool canUse = _boardInput != null && _boardInput.CanUseFusionInput; // 현재 배치 턴 합성 사용 가능 여부 계산
            bool active = _boardInput != null && _boardInput.IsFusionModeActive; // 현재 합성 모드 여부 계산
            _toggleButton.interactable = canUse; // 배치 턴 외 버튼 비활성화
            _toggleButtonText.text = active ? "합성 종료" : "합성"; // 현재 모드 문구 표시

            if (!canUse && active) // 턴 변경 직후 모드 잔존 여부 확인
            {
                _boardInput.SetFusionModeActive(false); // 기존 합성 모드 자동 종료
                _selection.Clear(); // 재료 선택 초기화
            }
        }

        private void RefreshPanel() // 재료·A+B=C·결과 미리보기 갱신
        {
            if (_panelRoot == null) // 패널 생성 여부 확인
            {
                return; // 생성 전 갱신 차단
            }

            bool active = _boardInput != null && _boardInput.IsFusionModeActive; // 합성 모드 활성 여부 계산
            _panelRoot.SetActive(active); // 합성 모드에서만 패널 표시

            if (!active || _boardInput.HandState == null) // 패널 표시 불가 상태 확인
            {
                return; // 상세 갱신 종료
            }

            IReadOnlyList<PieceDefinition> hand = _boardInput.HandState.Hand; // 현재 실제 손패 조회
            _selection.Prune(hand.Count); // 현재 손패 범위에 맞춰 선택 정리
            PieceDefinition materialA = GetSelectedMaterial(0, hand); // 재료 A 조회
            PieceDefinition materialB = GetSelectedMaterial(1, hand); // 재료 B 조회
            RefreshMaterialButton(_materialAButton, _materialAText, materialA, "재료 A"); // 재료 A 슬롯 갱신
            RefreshMaterialButton(_materialBButton, _materialBText, materialB, "재료 B"); // 재료 B 슬롯 갱신

            if (_selection.Count == 0) // 재료 미선택 상태 확인
            {
                _expressionText.text = "? + ? = ?"; // 빈 A+B=C 표시
                _statusText.text = "강조된 카드 중 첫 번째 재료를 선택하세요."; // 첫 선택 안내
                ClearResultPreview(); // 결과 미리보기 초기화
                SetConfirmState(false); // 합성 확정 비활성화
                return; // 상세 갱신 종료
            }

            if (_selection.Count == 1) // 첫 재료만 선택된 상태 확인
            {
                _expressionText.text = $"{GetDisplayName(materialA)} + ? = ?"; // 첫 재료가 반영된 A+B=C 표시
                _statusText.text = "초록 테두리 카드 중 두 번째 재료를 선택하세요."; // 합성 가능 후보 안내
                ClearResultPreview(); // 아직 결과 없음 표시
                SetConfirmState(false); // 합성 확정 비활성화
                return; // 상세 갱신 종료
            }

            FusionBlockReason reason = _boardInput.EvaluateFusion(materialA, materialB, out FusionRecipe recipe); // 두 재료 최종 합성 가능 여부 계산

            if (reason != FusionBlockReason.None || recipe == null || recipe.Result == null) // 합성 불가 상태 확인
            {
                _expressionText.text = $"{GetDisplayName(materialA)} + {GetDisplayName(materialB)} = 불가"; // 실패 A+B=C 표시
                _statusText.text = FusionRuleValidator.DescribeBlockReason(reason); // 기존 구체적 차단 사유 표시
                ClearResultPreview(); // 잘못된 결과 노출 차단
                SetConfirmState(false); // 합성 확정 비활성화
                return; // 상세 갱신 종료
            }

            bool undiscovered = _boardInput.RunState != null && !_boardInput.RunState.FusionDiscovery.IsDiscovered(recipe); // 숨김 Recipe 발견 여부 확인
            string resultName = undiscovered ? "???" : GetDisplayName(recipe.Result); // 발견 상태에 따른 결과 이름 결정
            _expressionText.text = $"{GetDisplayName(materialA)} + {GetDisplayName(materialB)} = {resultName}"; // 완성 A+B=C 표시
            _statusText.text = undiscovered ? "숨김 Recipe · 합성 후 결과가 공개됩니다." : "합성 가능 · 같은 배치 턴에 결과 카드를 바로 사용할 수 있습니다."; // 발견·즉시 사용 안내
            RefreshResultPreview(recipe, undiscovered); // 결과 카드 미리보기 표시
            SetConfirmState(true); // 합성 확정 활성화
        }

        private PieceDefinition GetSelectedMaterial(int selectionSlot, IReadOnlyList<PieceDefinition> hand) // 선택 슬롯에서 실제 손패 카드 조회
        {
            if (selectionSlot < 0 || selectionSlot >= _selection.Count) // 선택 슬롯 범위 확인
            {
                return null; // 빈 재료 반환
            }

            int handIndex = _selection.Indices[selectionSlot]; // 선택된 실제 손패 인덱스 조회

            if (handIndex < 0 || handIndex >= hand.Count) // 현재 손패 범위 확인
            {
                return null; // 무효 재료 반환
            }

            return hand[handIndex]; // 실제 선택 카드 반환
        }

        private void RefreshMaterialButton(Button button, Text text, PieceDefinition material, string emptyLabel) // 재료 슬롯 버튼 표시 갱신
        {
            if (button != null) // 버튼 참조 확인
            {
                button.interactable = material != null; // 재료가 들어간 슬롯만 선택 해제 허용
            }

            if (text != null) // 슬롯 Text 참조 확인
            {
                text.text = material != null ? GetDisplayName(material) : emptyLabel; // 카드 이름 또는 빈 슬롯 문구 표시
            }
        }

        private void ClearResultPreview() // 결과 카드 미리보기 초기화
        {
            if (_resultArtwork != null) // 결과 Artwork 참조 확인
            {
                _resultArtwork.sprite = null; // 이전 Artwork 제거
                _resultArtwork.enabled = false; // 빈 결과 이미지 숨김
            }

            _resultNameText.text = "결과"; // 결과 이름 기본 문구
            _resultStatsText.text = ""; // 결과 스탯 초기화
            _resultDescriptionText.text = ""; // 결과 설명 초기화
        }

        private void RefreshResultPreview(FusionRecipe recipe, bool undiscovered) // 결과 카드 상세 미리보기 갱신
        {
            if (recipe == null || recipe.Result == null) // 결과 레시피 확인
            {
                ClearResultPreview(); // 잘못된 결과 초기화
                return; // 처리 종료
            }

            PieceDefinition result = recipe.Result; // 결과 카드 정의 참조

            if (undiscovered) // 숨김 Recipe 미발견 상태 확인
            {
                _resultArtwork.sprite = null; // 결과 Artwork 숨김
                _resultArtwork.enabled = false; // 결과 이미지 비활성화
                _resultNameText.text = "???"; // 결과 이름 숨김
                _resultStatsText.text = "숨김 Recipe"; // 숨김 상태 표시
                _resultDescriptionText.text = "합성 성공 후 결과 정보 공개"; // 발견 전 설명 표시
                return; // 실제 결과 정보 노출 차단
            }

            bool hasArtwork = result.CardArtwork != null; // 결과 카드 Artwork 연결 여부 확인
            _resultArtwork.sprite = result.CardArtwork; // 결과 Artwork 적용
            _resultArtwork.enabled = hasArtwork; // Artwork가 있을 때만 표시
            _resultNameText.text = GetDisplayName(result); // 결과 카드 이름 표시
            _resultStatsText.text = $"{Mathf.Max(1, (int)result.Grade)}성 · ATK {result.BaseAtk} · HP {result.BaseHp}"; // 결과 핵심 스탯 표시
            _resultDescriptionText.text = string.IsNullOrEmpty(result.Description) ? "합성 결과 카드" : result.Description; // 결과 설명 표시
        }

        private void SetConfirmState(bool interactable) // 합성 확정 버튼 상태 갱신
        {
            _confirmButton.interactable = interactable; // 실제 클릭 가능 여부 적용
            _confirmButtonText.text = "합성"; // 확정 버튼 문구 유지
        }

        private void SyncCardHitAreas() // 현재 손패 카드에 64일차 선택·후보 강조 오버레이 적용
        {
            if (_boardInput == null || _boardInput.HandState == null) // 손패 상태 연결 확인
            {
                return; // 연결 전 카드 처리 차단
            }

            bool fusionActive = _boardInput.IsFusionModeActive && _boardInput.CanUseFusionInput; // 현재 합성 카드 입력 활성 여부 계산
            IReadOnlyList<PieceDefinition> hand = _boardInput.HandState.Hand; // 현재 손패 참조
            _selection.Prune(hand.Count); // 유효 선택 슬롯 유지
            CardView[] views = Object.FindObjectsByType<CardView>(FindObjectsSortMode.None); // 현재 생성된 모든 CardView 탐색
            _cardHitAreas.Clear(); // 이번 프레임 오버레이 목록 초기화

            for (int i = 0; i < views.Length; i++) // 모든 CardView 순회
            {
                CardView cardView = views[i]; // 현재 카드 뷰 조회

                if (cardView == null || !cardView.gameObject.activeInHierarchy) // 파괴 예약·비활성 카드 확인
                {
                    continue; // 유효하지 않은 CardView 제외
                }

                if (cardView.transform.parent == null || cardView.transform.parent.name != "HandRoot") // 실제 플레이어 손패 카드 여부 확인
                {
                    continue; // 다른 CardView 사용처 제외
                }

                int handIndex = cardView.transform.GetSiblingIndex(); // 현재 손패 슬롯 인덱스 조회

                if (handIndex < 0 || handIndex >= hand.Count) // 손패 데이터 범위 확인
                {
                    continue; // 레이아웃 갱신 중 잘못된 카드 제외
                }

                Day64FusionCardHitArea hitArea = EnsureCardHitArea(cardView); // 카드 최상단 Fusion 클릭 오버레이 보장
                bool selected = _selection.Contains(handIndex); // 현재 재료 선택 카드 여부 계산
                bool candidate = false; // 합성 가능 후보 기본값

                if (fusionActive && !selected) // 합성 모드의 미선택 카드 확인
                {
                    if (_selection.Count == 0) // 첫 재료 선택 단계 확인
                    {
                        candidate = CanUseAsFirstMaterial(handIndex, hand); // 현재 손패에서 조합 가능한 카드만 강조
                    }
                    else if (_selection.Count == 1) // 두 번째 재료 선택 단계 확인
                    {
                        candidate = CanPair(_selection.Indices[0], handIndex, hand); // 첫 재료와 실제 합성 가능한 카드만 강조
                    }
                }

                bool canClick = fusionActive && (selected || candidate); // 선택 해제 또는 유효 후보만 클릭 허용
                hitArea.Configure(this, handIndex); // 현재 슬롯 클릭 대상 연결
                hitArea.ApplyState(fusionActive, selected, candidate, canClick); // 카드 선택·후보·잠금 시각 상태 적용
                _cardHitAreas.Add(hitArea); // 현재 활성 오버레이 목록 등록
            }
        }

        private static Day64FusionCardHitArea EnsureCardHitArea(CardView cardView) // CardView 최상단 Fusion 입력 오버레이 생성 보조
        {
            Transform existing = cardView.transform.Find("Day64FusionHitArea"); // 기존 오버레이 자식 탐색
            GameObject overlayObject = existing != null ? existing.gameObject : null; // 기존 GameObject 변환

            if (overlayObject == null) // 오버레이 최초 생성 여부 확인
            {
                overlayObject = new GameObject("Day64FusionHitArea", typeof(RectTransform), typeof(Image), typeof(Outline)); // 카드 전체 클릭·강조 오버레이 생성
                overlayObject.transform.SetParent(cardView.transform, false); // 해당 CardView 자식 연결
                RectTransform rect = overlayObject.GetComponent<RectTransform>(); // 오버레이 RectTransform 조회
                rect.anchorMin = Vector2.zero; // 카드 좌하단 앵커 적용
                rect.anchorMax = Vector2.one; // 카드 우상단 앵커 적용
                rect.offsetMin = Vector2.zero; // 내부 여백 제거
                rect.offsetMax = Vector2.zero; // 내부 여백 제거
            }

            overlayObject.transform.SetAsLastSibling(); // 카드의 모든 기존 표시보다 위에 배치
            Day64FusionCardHitArea hitArea = overlayObject.GetComponent<Day64FusionCardHitArea>(); // 기존 클릭 컴포넌트 조회

            if (hitArea == null) // 클릭 컴포넌트 최초 생성 여부 확인
            {
                hitArea = overlayObject.AddComponent<Day64FusionCardHitArea>(); // Fusion 카드 클릭 컴포넌트 추가
            }

            return hitArea; // 준비된 오버레이 반환
        }

        private void SuppressLegacyFusionUI() // 기존 21~22일차 Fusion Canvas 표시 차단
        {
            if (_legacyFusionCanvas == null) // 구형 Canvas 참조 확인
            {
                _legacyFusionCanvas = GameObject.Find("FusionPanelCanvas"); // 기존 FusionPanelUI Canvas 탐색
            }

            if (_legacyFusionCanvas != null && _legacyFusionCanvas.activeSelf) // 구형 Canvas 활성 상태 확인
            {
                _legacyFusionCanvas.SetActive(false); // 64일차 정식 UI와 중복되지 않도록 숨김
            }
        }

        private void EnsureUI() // 64일차 Fusion Canvas·패널 생성
        {
            if (_canvas != null) // 기존 Canvas 확인
            {
                return; // 중복 생성 차단
            }

            var canvasObject = new GameObject("FusionPanelCanvas_Day64", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 64일차 Fusion Canvas 생성
            canvasObject.transform.SetParent(transform, false); // UI 호스트 자식 연결
            _canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 UI 모드 적용
            _canvas.sortingOrder = CanvasOrder; // 손패보다 위 정렬 적용
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 다양한 화면비 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 스케일 적용
            BuildToggleButton(canvasObject.transform); // 합성 모드 버튼 생성
            BuildPanel(canvasObject.transform); // A+B=C 패널 생성
        }

        private void BuildToggleButton(Transform parent) // 우하단 합성 진입 버튼 생성
        {
            GameObject buttonObject = CreateButtonObject("Day64FusionToggleButton", parent, new Vector2(1f, 0f), new Vector2(-99f, 106f), new Vector2(225f, 56f)); // 기존 HUD 흐름과 맞는 버튼 배치
            _toggleButton = buttonObject.GetComponent<Button>(); // Button 참조 저장
            _toggleButton.onClick.AddListener(OnToggleButtonClicked); // 합성 모드 토글 이벤트 연결
            buttonObject.GetComponent<Image>().color = new Color(0.47f, 0.31f, 0.12f, 0.96f); // 청동 계열 버튼 배경 적용
            _toggleButtonText = CreateText("Day64FusionToggleText", buttonObject.transform, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 버튼 문구 생성
            Stretch(_toggleButtonText.rectTransform, 4f, 4f, 4f, 4f); // 버튼 내부 여백 적용
            _toggleButtonText.text = "합성"; // 기본 버튼 문구 적용
        }

        private void BuildPanel(Transform parent) // 재료 선택·A+B=C·결과 미리보기 패널 생성
        {
            GameObject panel = new GameObject("Day64FusionPanel", typeof(RectTransform), typeof(Image)); // 정식 Fusion 패널 생성
            panel.transform.SetParent(parent, false); // Canvas 자식 연결
            _panelRoot = panel; // 패널 루트 참조 저장
            RectTransform panelRect = panel.GetComponent<RectTransform>(); // 패널 RectTransform 조회
            SetRect(panelRect, new Vector2(0.5f, 0f), new Vector2(0f, 430f), new Vector2(980f, 310f)); // 손패 바로 위 중앙 배치
            Image panelImage = panel.GetComponent<Image>(); // 패널 배경 Image 조회
            panelImage.color = new Color(0.035f, 0.045f, 0.06f, 0.97f); // 전투 HUD와 통일된 어두운 배경 적용
            panelImage.raycastTarget = true; // 패널 아래 보드 클릭 차단
            Outline panelOutline = panel.AddComponent<Outline>(); // 패널 경계 Outline 추가
            panelOutline.effectColor = new Color(0.55f, 0.43f, 0.24f, 0.95f); // 청동색 패널 경계 적용
            panelOutline.effectDistance = new Vector2(2f, -2f); // 패널 외곽선 두께 적용

            Text title = CreateText("Day64FusionTitle", panel.transform, 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white); // 패널 제목 생성
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(260f, 34f)); // 좌상단 제목 배치
            title.text = "FUSION"; // 패널 제목 문구 적용

            _discoveryNoticeText = CreateText("Day64RecipeNotice", panel.transform, 17, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.84f, 0.3f, 1f)); // Recipe 발견 알림 생성
            SetRect(_discoveryNoticeText.rectTransform, new Vector2(1f, 1f), new Vector2(-24f, -16f), new Vector2(440f, 34f)); // 우상단 알림 배치
            _discoveryNoticeText.gameObject.SetActive(false); // 기본 발견 알림 숨김

            _expressionText = CreateText("Day64FusionExpression", panel.transform, 25, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // A+B=C 문구 생성
            SetRect(_expressionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(900f, 44f)); // 패널 상단 중앙 표현식 배치
            _expressionText.text = "? + ? = ?"; // 기본 표현식 적용

            GameObject materialAObject = CreateButtonObject("Day64MaterialA", panel.transform, new Vector2(0f, 0.5f), new Vector2(120f, 18f), new Vector2(190f, 78f)); // 재료 A 버튼 생성
            _materialAButton = materialAObject.GetComponent<Button>(); // 재료 A Button 저장
            _materialAButton.onClick.AddListener(() => OnMaterialSlotClicked(0)); // 재료 A 개별 해제 연결
            _materialAText = CreateText("Day64MaterialAText", materialAObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 재료 A 이름 Text 생성
            Stretch(_materialAText.rectTransform, 8f, 6f, 8f, 6f); // 재료 A 내부 여백 적용

            Text plusText = CreateText("Day64Plus", panel.transform, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 더하기 기호 생성
            SetRect(plusText.rectTransform, new Vector2(0f, 0.5f), new Vector2(235f, 18f), new Vector2(40f, 50f)); // 재료 사이 배치
            plusText.text = "+"; // 더하기 문구 적용

            GameObject materialBObject = CreateButtonObject("Day64MaterialB", panel.transform, new Vector2(0f, 0.5f), new Vector2(350f, 18f), new Vector2(190f, 78f)); // 재료 B 버튼 생성
            _materialBButton = materialBObject.GetComponent<Button>(); // 재료 B Button 저장
            _materialBButton.onClick.AddListener(() => OnMaterialSlotClicked(1)); // 재료 B 개별 해제 연결
            _materialBText = CreateText("Day64MaterialBText", materialBObject.transform, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 재료 B 이름 Text 생성
            Stretch(_materialBText.rectTransform, 8f, 6f, 8f, 6f); // 재료 B 내부 여백 적용

            Text equalsText = CreateText("Day64Equals", panel.transform, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 등호 기호 생성
            SetRect(equalsText.rectTransform, new Vector2(0f, 0.5f), new Vector2(465f, 18f), new Vector2(40f, 50f)); // 결과 사이 배치
            equalsText.text = "="; // 등호 문구 적용

            BuildResultArea(panel.transform); // 결과 카드 미리보기 영역 생성

            _statusText = CreateText("Day64FusionStatus", panel.transform, 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.86f, 0.88f, 0.92f, 1f)); // 합성 상태 안내 Text 생성
            SetRect(_statusText.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(700f, 46f)); // 패널 좌하단 상태 문구 배치

            GameObject confirmObject = CreateButtonObject("Day64FusionConfirm", panel.transform, new Vector2(1f, 0f), new Vector2(-92f, 24f), new Vector2(150f, 48f)); // 우하단 합성 확정 버튼 생성
            _confirmButton = confirmObject.GetComponent<Button>(); // 확정 Button 저장
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked); // 실제 합성 처리 연결
            confirmObject.GetComponent<Image>().color = new Color(0.18f, 0.48f, 0.24f, 1f); // 확정 버튼 배경 적용
            _confirmButtonText = CreateText("Day64FusionConfirmText", confirmObject.transform, 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white); // 확정 버튼 문구 생성
            Stretch(_confirmButtonText.rectTransform, 4f, 4f, 4f, 4f); // 확정 버튼 내부 여백 적용
            _confirmButtonText.text = "합성"; // 확정 기본 문구 적용

            _panelRoot.SetActive(false); // 합성 모드 진입 전 패널 숨김
        }

        private void BuildResultArea(Transform parent) // 결과 Artwork·이름·스탯·설명 영역 생성
        {
            GameObject resultObject = new GameObject("Day64FusionResult", typeof(RectTransform), typeof(Image)); // 결과 카드 미리보기 패널 생성
            resultObject.transform.SetParent(parent, false); // Fusion 패널 자식 연결
            RectTransform resultRect = resultObject.GetComponent<RectTransform>(); // 결과 패널 RectTransform 조회
            SetRect(resultRect, new Vector2(0f, 0.5f), new Vector2(690f, 18f), new Vector2(330f, 126f)); // 우측 결과 영역 배치
            Image background = resultObject.GetComponent<Image>(); // 결과 패널 배경 Image 조회
            background.color = new Color(0.08f, 0.09f, 0.12f, 0.96f); // 결과 영역 어두운 배경 적용
            background.raycastTarget = false; // 카드 선택 입력 간섭 제거

            GameObject artworkObject = new GameObject("Day64ResultArtwork", typeof(RectTransform), typeof(Image)); // 결과 Artwork Image 생성
            artworkObject.transform.SetParent(resultObject.transform, false); // 결과 패널 자식 연결
            _resultArtwork = artworkObject.GetComponent<Image>(); // 결과 Artwork 참조 저장
            SetRect(_resultArtwork.rectTransform, new Vector2(0f, 0.5f), new Vector2(55f, 0f), new Vector2(88f, 88f)); // 결과 좌측 Artwork 배치
            _resultArtwork.preserveAspect = true; // Artwork 비율 유지
            _resultArtwork.raycastTarget = false; // UI 입력 간섭 제거

            _resultNameText = CreateText("Day64ResultName", resultObject.transform, 18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white); // 결과 이름 Text 생성
            SetRect(_resultNameText.rectTransform, new Vector2(0f, 1f), new Vector2(112f, -14f), new Vector2(200f, 28f)); // 결과 우측 상단 이름 배치
            _resultStatsText = CreateText("Day64ResultStats", resultObject.transform, 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.9f, 0.82f, 0.55f, 1f)); // 결과 스탯 Text 생성
            SetRect(_resultStatsText.rectTransform, new Vector2(0f, 1f), new Vector2(112f, -46f), new Vector2(200f, 24f)); // 이름 아래 스탯 배치
            _resultDescriptionText = CreateText("Day64ResultDescription", resultObject.transform, 12, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.82f, 0.84f, 0.88f, 1f)); // 결과 설명 Text 생성
            SetRect(_resultDescriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(112f, -72f), new Vector2(200f, 44f)); // 스탯 아래 설명 배치
            _resultDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 설명 줄바꿈 적용
        }

        private static GameObject CreateButtonObject(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size) // 공통 런타임 Button 생성 보조
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // Button GameObject 생성
            buttonObject.transform.SetParent(parent, false); // 요청 부모 연결
            SetRect(buttonObject.GetComponent<RectTransform>(), anchor, position, size); // 버튼 위치·크기 적용
            Image image = buttonObject.GetComponent<Image>(); // 버튼 배경 Image 조회
            image.color = new Color(0.16f, 0.18f, 0.22f, 0.98f); // 기본 버튼 배경 적용
            return buttonObject; // 완성 Button GameObject 반환
        }

        private static Text CreateText(string name, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color) // 공통 런타임 Text 생성 보조
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // Text GameObject 생성
            textObject.transform.SetParent(parent, false); // 요청 부모 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = GetRuntimeFont(); // 한글 런타임 폰트 적용
            text.fontSize = fontSize; // 글자 크기 적용
            text.fontStyle = fontStyle; // 글자 스타일 적용
            text.alignment = alignment; // 글자 정렬 적용
            text.color = color; // 글자색 적용
            text.raycastTarget = false; // 카드·버튼 입력 간섭 제거
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 기본 가로 문구 보존
            text.verticalOverflow = VerticalWrapMode.Truncate; // 영역 밖 세로 문구 차단
            return text; // 완성 Text 반환
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) // 고정 앵커 UI 배치 보조
        {
            rect.anchorMin = anchor; // 최소 앵커 적용
            rect.anchorMax = anchor; // 최대 앵커 적용
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 적용
            rect.anchoredPosition = position; // 앵커 기준 위치 적용
            rect.sizeDelta = size; // UI 크기 적용
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top) // 부모 전체 Stretch 배치 보조
        {
            rect.anchorMin = Vector2.zero; // 좌하단 앵커 적용
            rect.anchorMax = Vector2.one; // 우상단 앵커 적용
            rect.offsetMin = new Vector2(left, bottom); // 좌·하단 여백 적용
            rect.offsetMax = new Vector2(-right, -top); // 우·상단 여백 적용
        }

        private static string GetDisplayName(PieceDefinition definition) // 카드 표시 이름 결정 보조
        {
            if (definition == null) // 카드 정의 확인
            {
                return "?"; // 빈 카드 기본 문구 반환
            }

            return string.IsNullOrEmpty(definition.DisplayName) ? definition.name : definition.DisplayName; // DisplayName 우선 반환
        }

        private static Font GetRuntimeFont() // 한글 런타임 폰트 생성
        {
            if (_runtimeFont != null) // 기존 폰트 캐시 확인
            {
                return _runtimeFont; // 캐시 폰트 재사용
            }

            _runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 24); // 주요 OS 한글 폰트 생성

            if (_runtimeFont == null) // 시스템 폰트 생성 실패 확인
            {
                _runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 대체 적용
            }

            return _runtimeFont; // 최종 런타임 폰트 반환
        }

        private void EnsureEventSystem() // UI 버튼·카드 오버레이 클릭 입력 시스템 보장
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) // 기존 EventSystem 확인
            {
                return; // 기존 입력 시스템 재사용
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
            _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성한 EventSystem 저장
        }

        private void OnDestroy() // 64일차 UI 제거 시 이벤트 정리
        {
            if (_boardInput != null) // 연결된 BoardInputController 확인
            {
                _boardInput.HandChanged -= HandleHandChanged; // 손패 변경 이벤트 구독 해제
                _boardInput.FusionSelectionChanged -= HandleFusionModeChanged; // 합성 모드 이벤트 구독 해제
            }

            if (_createdEventSystem != null) // 직접 만든 EventSystem 확인
            {
                if (Application.isPlaying) // Play Mode 제거 방식 확인
                {
                    Destroy(_createdEventSystem.gameObject); // 프레임 종료 시 EventSystem 제거
                }
                else // EditMode 제거 방식 선택
                {
                    DestroyImmediate(_createdEventSystem.gameObject); // 즉시 EventSystem 제거
                }
            }
        }
    }

    public sealed class Day64FusionCardHitArea : MonoBehaviour, IPointerClickHandler // 합성 모드에서 CardView 클릭을 슬롯 인덱스로 가로채는 오버레이
    {
        private Day64FusionUI _owner; // 64일차 Fusion UI 처리자
        private int _handIndex; // 이 오버레이가 표현하는 실제 손패 슬롯
        private bool _canClick; // 현재 후보·선택 카드 클릭 가능 여부
        private Image _image; // 카드 전체 입력·상태 배경
        private Outline _outline; // 후보·선택 테두리 표시

        public void Configure(Day64FusionUI owner, int handIndex) // 현재 카드 슬롯과 처리자 연결
        {
            _owner = owner; // Fusion UI 처리자 저장
            _handIndex = handIndex; // 실제 손패 슬롯 저장
            EnsureVisuals(); // Image·Outline 참조 보장
        }

        public void ApplyState(bool active, bool selected, bool candidate, bool canClick) // 카드 합성 상태 시각화 적용
        {
            EnsureVisuals(); // 오버레이 구성 보장
            gameObject.SetActive(active); // 합성 모드에서만 오버레이 표시
            _canClick = canClick; // 현재 클릭 허용 상태 저장

            if (!active) // 합성 모드 비활성 확인
            {
                return; // 시각 상태 계산 종료
            }

            _image.raycastTarget = true; // 기존 CardView 클릭 대신 최상단 오버레이가 입력 소비

            if (selected) // 현재 재료 선택 카드 확인
            {
                _image.color = new Color(1f, 0.78f, 0.16f, 0.08f); // 선택 카드 약한 금색 배경 적용
                _outline.enabled = true; // 선택 테두리 표시
                _outline.effectColor = new Color(1f, 0.82f, 0.22f, 1f); // 선택 금색 테두리 적용
                _outline.effectDistance = new Vector2(4f, -4f); // 선택 테두리 두께 적용
                return; // 선택 상태 적용 종료
            }

            if (candidate) // 현재 합성 가능 후보 확인
            {
                _image.color = new Color(0.25f, 0.9f, 0.48f, 0.05f); // 후보 카드 약한 초록 배경 적용
                _outline.enabled = true; // 후보 테두리 표시
                _outline.effectColor = new Color(0.3f, 0.95f, 0.5f, 0.95f); // 합성 가능 초록 테두리 적용
                _outline.effectDistance = new Vector2(3f, -3f); // 후보 테두리 두께 적용
                return; // 후보 상태 적용 종료
            }

            _image.color = new Color(0f, 0f, 0f, 0.34f); // 합성 불가 카드 어둡게 표시
            _outline.enabled = false; // 불가 카드 테두리 숨김
        }

        public void OnPointerClick(PointerEventData eventData) // 카드 오버레이 포인터 클릭 처리
        {
            if (eventData.button != PointerEventData.InputButton.Left) // 좌클릭 여부 확인
            {
                return; // 우클릭 등 다른 입력 소비 후 종료
            }

            if (!_canClick || _owner == null) // 현재 후보·선택 여부와 처리자 확인
            {
                return; // 불가 카드 선택 차단
            }

            _owner.ToggleHandCard(_handIndex); // 실제 손패 슬롯 기반 재료 선택 토글
        }

        private void EnsureVisuals() // Image·Outline 컴포넌트 참조 보장
        {
            if (_image == null) // Image 캐시 확인
            {
                _image = GetComponent<Image>(); // 기존 Image 조회
            }

            if (_outline == null) // Outline 캐시 확인
            {
                _outline = GetComponent<Outline>(); // 기존 Outline 조회
            }
        }
    }
}
