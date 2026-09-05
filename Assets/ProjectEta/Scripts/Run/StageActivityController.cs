using System.Collections; // 초기화 대기 코루틴 사용
using System.Collections.Generic; // 상점 카드·UI 옵션 목록 사용
using UnityEngine; // MonoBehaviour·Resources 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using ProjectEta.Battle; // BattleController·BattleOutcome 사용
using ProjectEta.Board; // BoardView·RouteMapBoardController 사용
using ProjectEta.Cards; // PlayerStartingDeckCatalog 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.UI; // 판 위 StageBoardOverlayUI 사용

namespace ProjectEta.Run
{
    [DefaultExecutionOrder(1040)]
    public sealed class StageActivityController : MonoBehaviour
    {
        private const string CardCatalogResourceName = "PlayerStartingDeck26"; // 상점·이벤트 카드 원본 카탈로그
        private const int MaxOwnedChoiceCards = 6; // 제거·강화 페이지 최대 표시 카드 수

        private readonly List<PieceDefinition> _shopOffers = new List<PieceDefinition>(); // 현재 상점 카드 상품
        private BattleController _battleController; // RunState 접근 전투 컨트롤러
        private RouteMapBoardController _routeMapBoardController; // 비전투 완료 후 지도 갱신
        private BoardView _boardView; // 판 위 오버레이 기준 보드
        private StagePlaceholderUI _placeholderUI; // 45일차 임시 UI 숨김 대상
        private StageBoardOverlayUI _overlayUI; // 판 위 돗자리 UI
        private StageActivityCameraLock _cameraLock; // Shop·Event 동안 1번 카메라 강제 고정
        private PlayerStartingDeckCatalog _cardCatalog; // 카드 구매·이벤트 원본 풀
        private RunState _runState; // 현재 런 상태
        private RunEconomyState _economy; // 현재 런 전용 재화
        private bool _activityActive; // Shop/Event 화면 진행 여부
        private RunFlowPhase _activePhase; // 현재 비전투 타입
        private StageEventScenario _eventScenario; // 현재 이벤트 시나리오

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return;
            if (Object.FindFirstObjectByType<StageActivityController>() != null) return;

            var host = new GameObject("StageActivityController_Day47"); // 47일차 상점·이벤트 호스트 생성
            host.AddComponent<StageActivityController>(); // 실제 비전투 관리자 추가
        }

        private IEnumerator Start()
        {
            const int maxWaitFrames = 240; // 런타임 의존성 최대 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 전투 컨트롤러 탐색
                _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 지도 컨트롤러 탐색
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 보드 뷰 탐색
                _placeholderUI = Object.FindFirstObjectByType<StagePlaceholderUI>(); // 45일차 임시 UI 탐색

                if (_battleController != null && _battleController.RunState != null && _routeMapBoardController != null && _boardView != null)
                {
                    _runState = _battleController.RunState; // 현재 런 상태 연결
                    _economy = RunEconomyService.GetOrCreate(_runState); // RunState별 런 전용 경제 상태 연결
                    _cardCatalog = Resources.Load<PlayerStartingDeckCatalog>(CardCatalogResourceName); // 카드 상점 카탈로그 로드
                    _overlayUI = GetComponent<StageBoardOverlayUI>(); // 같은 호스트 판 위 UI 탐색
                    if (_overlayUI == null) _overlayUI = gameObject.AddComponent<StageBoardOverlayUI>(); // 없으면 판 위 UI 자동 추가
                    _overlayUI.Initialize(_boardView); // 10×10 보드 기준 돗자리 생성
                    _cameraLock = GetComponent<StageActivityCameraLock>(); // 같은 호스트의 카메라 잠금기 탐색
                    if (_cameraLock == null) _cameraLock = gameObject.AddComponent<StageActivityCameraLock>(); // 없으면 47일차 카메라 잠금기 자동 추가
                    _cameraLock.Initialize(_boardView); // 1번 카메라 기준 보드 연결
                    yield break;
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null;
            }

            Debug.LogError("47일차 StageActivityController 초기화 실패: BattleController·RunState·RouteMapBoardController·BoardView를 확인하세요."); // 초기화 실패 로그
        }

        private void Update()
        {
            if (_runState == null || _overlayUI == null) return;

            if (!_activityActive && _runState.CurrentFlowPhase == RunFlowPhase.Shop)
            {
                BeginShop(); // 상점 노드 실제 진입
                return;
            }

            if (!_activityActive && _runState.CurrentFlowPhase == RunFlowPhase.Event)
            {
                BeginEvent(); // 이벤트 노드 실제 진입
                return;
            }

            if (_activityActive && _runState.CurrentFlowPhase != _activePhase)
            {
                CloseOverlayOnly(); // 외부 흐름 변경 시 판 위 UI 안전 정리
            }
        }

        private void BeginShop()
        {
            _activityActive = true; // 상점 진행 상태 설정
            _activePhase = RunFlowPhase.Shop; // 현재 타입 저장
            HideLegacyPlaceholder(); // 45일차 화면 전체 Placeholder 차단
            if (_cameraLock != null) _cameraLock.LockToPrimaryView(); // 상점 동안 1번 카메라로 강제 고정
            PrepareShopOffers(); // 현재 상점 카드 상품 생성
            ShowShopMain(); // 판 위 상점 메인 메뉴 표시
            Debug.Log($"47일차 Shop 진입: Depth={_runState.CurrentRound} / Gold={_economy.Currency} / Offers={_shopOffers.Count}"); // 상점 진입 로그
        }

        private void PrepareShopOffers()
        {
            _shopOffers.Clear(); // 이전 상점 상품 초기화
            if (_cardCatalog == null) return;

            int seed = _runState.CurrentRound * 104729 + _runState.Deck.OwnedCardPool.Count * 97; // 깊이·덱 기반 상점 시드
            IReadOnlyList<PieceDefinition> generated = CardRewardGenerator.Generate(_cardCatalog.Cards, _runState.Deck.OwnedCardPool, 3, seed); // 보상 규칙 재사용 상점 카드 생성

            for (int i = 0; i < generated.Count; i++)
            {
                if (generated[i] != null) _shopOffers.Add(generated[i]); // 유효 상품 등록
            }
        }

        private void ShowShopMain()
        {
            var options = new List<StageOverlayOption>(); // 상점 메인 선택지 생성
            bool hasPurchasable = _shopOffers.Count > 0; // 카드 상품 존재 여부
            bool hasRemovable = CollectOwnedCards().Count > 0; // 제거 가능 카드 존재 여부
            bool canHeal = _runState.KingHp < RunEconomyRules.PrototypeKingMaxHp; // 회복 필요 여부
            bool hasUpgradable = CollectOwnedCards().Count > 0; // 강화 가능 카드 존재 여부

            options.Add(new StageOverlayOption(
                "카드 구매",
                $"후보 최대 3장 · {RunEconomyRules.CardPurchasePrice} Gold",
                hasPurchasable,
                ShowPurchasePage)); // 카드 구매 진입

            options.Add(new StageOverlayOption(
                "카드 제거",
                $"덱에서 카드 1장 제거 · {RunEconomyRules.CardRemovePrice} Gold",
                hasRemovable,
                ShowRemovePage)); // 카드 제거 진입

            options.Add(new StageOverlayOption(
                "킹 HP 회복",
                $"HP +1 · {RunEconomyRules.HealPrice} Gold",
                canHeal,
                HealKing)); // 킹 회복 실행

            options.Add(new StageOverlayOption(
                "카드 업그레이드",
                $"선택 카드 HP/ATK +1 · {RunEconomyRules.CardUpgradePrice} Gold",
                hasUpgradable,
                ShowUpgradePage)); // 카드 강화 진입

            options.Add(new StageOverlayOption(
                "상점 나가기",
                "구매를 마치고 다음 경로를 선택합니다.",
                true,
                CompleteCurrentStage)); // 상점 완료

            string subtitle = $"Gold {_economy.Currency}   |   King HP {_runState.KingHp}/{RunEconomyRules.PrototypeKingMaxHp}"; // 상점 현재 상태 문구
            _overlayUI.ShowPage(StageOverlayMode.Shop, "상점", subtitle, options); // 판 위 상점 돗자리 표시
        }

        private void ShowPurchasePage()
        {
            var options = new List<StageOverlayOption>(); // 구매 카드 선택지 생성

            for (int i = 0; i < _shopOffers.Count; i++)
            {
                PieceDefinition card = _shopOffers[i]; // 현재 상점 카드 조회
                if (card == null) continue;
                bool canBuy = _economy.Currency >= RunEconomyRules.CardPurchasePrice && CardRewardRules.CanOffer(card, _runState.Deck.OwnedCardPool); // 현재 구매 가능 여부
                PieceDefinition captured = card; // 버튼 클로저 카드 고정

                options.Add(new StageOverlayOption(
                    $"{card.DisplayName}  {GetStars(card)}",
                    $"HP {card.BaseHp} / ATK {card.BaseAtk}   ·   {RunEconomyRules.CardPurchasePrice} Gold",
                    canBuy,
                    () => PurchaseCard(captured))); // 카드 구매 콜백
            }

            options.Add(new StageOverlayOption("뒤로", "상점 메인으로 돌아갑니다.", true, ShowShopMain)); // 메인 복귀 버튼
            _overlayUI.ShowPage(StageOverlayMode.Shop, "카드 구매", $"Gold {_economy.Currency}", options); // 판 위 구매 페이지 표시
        }

        private void PurchaseCard(PieceDefinition card)
        {
            if (card == null || !_shopOffers.Contains(card)) return;
            if (!CardRewardRules.CanOffer(card, _runState.Deck.OwnedCardPool)) return;
            if (!_economy.TrySpend(RunEconomyRules.CardPurchasePrice))
            {
                ShowPurchasePage(); // 재화 부족 상태 갱신
                return;
            }

            if (!CardRewardRules.TryAddOwnedCard(_runState.Deck, card))
            {
                _economy.Add(RunEconomyRules.CardPurchasePrice); // 카드 추가 실패 시 재화 환불
                ShowPurchasePage();
                return;
            }

            _shopOffers.Remove(card); // 구매 완료 상품 제거
            RecordResult(new StageChoiceResult(
                StageChoiceEffectType.CardAdded,
                $"{card.DisplayName} 구매",
                -RunEconomyRules.CardPurchasePrice,
                0,
                card)); // 공통 선택 결과 기록

            ShowPurchasePage(); // 남은 상품·재화 갱신
        }

        private void ShowRemovePage()
        {
            IReadOnlyList<PieceDefinition> owned = CollectOwnedCards(); // 제거 가능한 카드 목록
            var options = new List<StageOverlayOption>(); // 제거 선택지 생성

            for (int i = 0; i < owned.Count; i++)
            {
                PieceDefinition card = owned[i]; // 현재 제거 카드 조회
                bool canRemove = _economy.Currency >= RunEconomyRules.CardRemovePrice; // 제거 비용 지불 가능 여부
                PieceDefinition captured = card; // 버튼 클로저 카드 고정

                options.Add(new StageOverlayOption(
                    $"{card.DisplayName} 제거",
                    $"{GetStars(card)}   HP {card.BaseHp} / ATK {card.BaseAtk}   ·   {RunEconomyRules.CardRemovePrice} Gold",
                    canRemove,
                    () => RemoveCard(captured))); // 카드 제거 콜백
            }

            options.Add(new StageOverlayOption("뒤로", "상점 메인으로 돌아갑니다.", true, ShowShopMain)); // 메인 복귀 버튼
            _overlayUI.ShowPage(StageOverlayMode.Shop, "카드 제거", $"Gold {_economy.Currency} · King은 제거할 수 없습니다.", options); // 판 위 제거 페이지
        }

        private void RemoveCard(PieceDefinition card)
        {
            if (card == null || card.MovementType == PieceMovementType.King) return;
            if (!_economy.TrySpend(RunEconomyRules.CardRemovePrice))
            {
                ShowRemovePage();
                return;
            }

            if (!_runState.Deck.RemoveFromOwnedPool(card))
            {
                _economy.Add(RunEconomyRules.CardRemovePrice); // 제거 실패 시 재화 환불
                ShowRemovePage();
                return;
            }

            RecordResult(new StageChoiceResult(
                StageChoiceEffectType.CardRemoved,
                $"{card.DisplayName} 제거",
                -RunEconomyRules.CardRemovePrice,
                0,
                card)); // 공통 제거 결과 기록

            ShowRemovePage(); // 남은 보유 카드 갱신
        }

        private void HealKing()
        {
            if (_runState.KingHp >= RunEconomyRules.PrototypeKingMaxHp)
            {
                ShowShopMain();
                return;
            }

            if (!_economy.TrySpend(RunEconomyRules.HealPrice))
            {
                ShowShopMain();
                return;
            }

            int before = _runState.KingHp; // 회복 전 HP 저장
            _runState.KingHp = Mathf.Min(RunEconomyRules.PrototypeKingMaxHp, _runState.KingHp + 1); // 킹 HP +1 적용

            RecordResult(new StageChoiceResult(
                StageChoiceEffectType.KingHpChanged,
                "킹 HP 회복",
                -RunEconomyRules.HealPrice,
                _runState.KingHp - before,
                null)); // 공통 회복 결과 기록

            ShowShopMain(); // 상점 상태 갱신
        }

        private void ShowUpgradePage()
        {
            IReadOnlyList<PieceDefinition> owned = CollectOwnedCards(); // 강화 가능한 카드 목록
            var options = new List<StageOverlayOption>(); // 강화 선택지 생성

            for (int i = 0; i < owned.Count; i++)
            {
                PieceDefinition card = owned[i]; // 현재 강화 카드 조회
                bool canUpgrade = _economy.Currency >= RunEconomyRules.CardUpgradePrice; // 강화 비용 지불 가능 여부
                PieceDefinition captured = card; // 버튼 클로저 카드 고정

                options.Add(new StageOverlayOption(
                    $"{card.DisplayName} 강화",
                    $"HP {card.BaseHp}→{card.BaseHp + 1} / ATK {card.BaseAtk}→{card.BaseAtk + 1}   ·   {RunEconomyRules.CardUpgradePrice} Gold",
                    canUpgrade,
                    () => UpgradeCard(captured))); // 카드 강화 콜백
            }

            options.Add(new StageOverlayOption("뒤로", "상점 메인으로 돌아갑니다.", true, ShowShopMain)); // 메인 복귀 버튼
            _overlayUI.ShowPage(StageOverlayMode.Shop, "카드 업그레이드", $"Gold {_economy.Currency} · 런타임 강화", options); // 판 위 강화 페이지
        }

        private void UpgradeCard(PieceDefinition card)
        {
            if (card == null || card.MovementType == PieceMovementType.King) return;
            if (!_economy.TrySpend(RunEconomyRules.CardUpgradePrice))
            {
                ShowUpgradePage();
                return;
            }

            if (!RuntimeCardUpgradeService.TryUpgradeOwnedCard(_runState.Deck, card, out PieceDefinition upgraded))
            {
                _economy.Add(RunEconomyRules.CardUpgradePrice); // 강화 실패 시 재화 환불
                ShowUpgradePage();
                return;
            }

            RecordResult(new StageChoiceResult(
                StageChoiceEffectType.CardUpgraded,
                $"{card.DisplayName} → {upgraded.DisplayName}",
                -RunEconomyRules.CardUpgradePrice,
                0,
                upgraded)); // 공통 강화 결과 기록

            ShowUpgradePage(); // 강화된 덱 상태 갱신
        }

        private void BeginEvent()
        {
            _activityActive = true; // 이벤트 진행 상태 설정
            _activePhase = RunFlowPhase.Event; // 현재 타입 저장
            HideLegacyPlaceholder(); // 45일차 화면 전체 Placeholder 차단
            if (_cameraLock != null) _cameraLock.LockToPrimaryView(); // 이벤트 동안 1번 카메라로 강제 고정
            _eventScenario = StageEventGenerator.Create(_runState.CurrentRound); // 현재 깊이 이벤트 생성
            ShowEventMain(); // 판 위 이벤트 돗자리 표시
            Debug.Log($"47일차 Event 진입: Depth={_runState.CurrentRound} / Type={_eventScenario.EventType}"); // 이벤트 진입 로그
        }

        private void ShowEventMain()
        {
            if (_eventScenario == null)
            {
                CompleteCurrentStage(); // 잘못된 이벤트 안전 종료
                return;
            }

            var options = new List<StageOverlayOption>(); // 이벤트 선택지 생성

            if (_eventScenario.EventType == StageEventType.CardFind)
            {
                options.Add(new StageOverlayOption("카드 꾸러미를 연다", "카드 후보 중 한 장을 무료로 획득합니다.", true, ShowEventCardChoices)); // 카드 획득 선택지
                options.Add(new StageOverlayOption("그냥 지나간다", "아무 변화 없이 다음 경로로 이동합니다.", true, CompleteCurrentStage)); // 무변화 선택지
            }
            else if (_eventScenario.EventType == StageEventType.Rest)
            {
                bool canRest = _runState.KingHp < RunEconomyRules.PrototypeKingMaxHp; // 현재 회복 가능 여부
                options.Add(new StageOverlayOption("잠시 휴식한다", "King HP +1", canRest, ApplyFreeHealEvent)); // 무료 회복 선택지
                options.Add(new StageOverlayOption("바로 떠난다", "아무 변화 없이 다음 경로로 이동합니다.", true, CompleteCurrentStage)); // 무변화 선택지
            }
            else
            {
                bool canRisk = _runState.KingHp > 1; // 이벤트로 즉사하지 않는 경우만 허용
                options.Add(new StageOverlayOption(
                    "위험한 계약을 맺는다",
                    $"King HP -1 / Gold +{RunEconomyRules.RiskRewardCurrency} / 카드 1장",
                    canRisk,
                    ApplyRiskEvent)); // 위험 보상 선택지
                options.Add(new StageOverlayOption("계약을 거절한다", "아무 변화 없이 다음 경로로 이동합니다.", true, CompleteCurrentStage)); // 무변화 선택지
            }

            string subtitle = $"{_eventScenario.Description}\nGold {_economy.Currency}   |   King HP {_runState.KingHp}/{RunEconomyRules.PrototypeKingMaxHp}"; // 이벤트 상태 설명
            _overlayUI.ShowPage(StageOverlayMode.Event, _eventScenario.Title, subtitle, options); // 판 위 이벤트 돗자리 표시
        }

        private void ShowEventCardChoices()
        {
            IReadOnlyList<PieceDefinition> candidates = GenerateEventCardCandidates(); // 무료 카드 후보 생성
            if (candidates.Count == 0)
            {
                RecordResult(new StageChoiceResult(StageChoiceEffectType.None, "획득 가능한 카드 없음", 0, 0, null)); // 후보 없음 결과 기록
                CompleteCurrentStage();
                return;
            }

            var options = new List<StageOverlayOption>(); // 이벤트 카드 선택지 생성

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition card = candidates[i]; // 현재 이벤트 카드 조회
                PieceDefinition captured = card; // 버튼 클로저 카드 고정

                options.Add(new StageOverlayOption(
                    $"{card.DisplayName}  {GetStars(card)}",
                    $"HP {card.BaseHp} / ATK {card.BaseAtk}",
                    true,
                    () => TakeEventCard(captured))); // 무료 카드 획득 콜백
            }

            options.Add(new StageOverlayOption("취소", "이벤트 선택으로 돌아갑니다.", true, ShowEventMain)); // 이벤트 메인 복귀
            _overlayUI.ShowPage(StageOverlayMode.Event, "카드 한 장 선택", "무료 카드 보상", options); // 판 위 카드 선택 표시
        }

        private IReadOnlyList<PieceDefinition> GenerateEventCardCandidates()
        {
            if (_cardCatalog == null) return new List<PieceDefinition>(); // 카드 카탈로그 누락 방어
            int seed = _runState.CurrentRound * 32452843 + _runState.Deck.OwnedCardPool.Count * 131; // 이벤트 카드 후보 시드
            return CardRewardGenerator.Generate(_cardCatalog.Cards, _runState.Deck.OwnedCardPool, 3, seed); // 46일차 카드 보상 규칙 재사용
        }

        private void TakeEventCard(PieceDefinition card)
        {
            if (card == null) return;
            if (!CardRewardRules.TryAddOwnedCard(_runState.Deck, card))
            {
                ShowEventCardChoices();
                return;
            }

            RecordResult(new StageChoiceResult(StageChoiceEffectType.CardAdded, $"{card.DisplayName} 획득", 0, 0, card)); // 무료 카드 결과 기록
            CompleteCurrentStage(); // 이벤트 완료
        }

        private void ApplyFreeHealEvent()
        {
            int before = _runState.KingHp; // 이벤트 전 HP 저장
            _runState.KingHp = Mathf.Min(RunEconomyRules.PrototypeKingMaxHp, _runState.KingHp + 1); // 무료 HP +1 적용

            RecordResult(new StageChoiceResult(
                StageChoiceEffectType.KingHpChanged,
                "휴식으로 킹 HP 회복",
                0,
                _runState.KingHp - before,
                null)); // 회복 이벤트 결과 기록

            CompleteCurrentStage(); // 이벤트 완료
        }

        private void ApplyRiskEvent()
        {
            if (_runState.KingHp <= 1)
            {
                ShowEventMain();
                return;
            }

            _runState.KingHp -= 1; // 위험 계약 체력 비용 적용
            _economy.Add(RunEconomyRules.RiskRewardCurrency); // 위험 계약 재화 보상 지급

            RecordResult(new StageChoiceResult(
                StageChoiceEffectType.Mixed,
                "위험한 계약: HP -1, Gold 보상",
                RunEconomyRules.RiskRewardCurrency,
                -1,
                null)); // 위험 계약 1차 결과 기록

            ShowEventCardChoices(); // 추가 카드 보상 선택 연결
        }

        private IReadOnlyList<PieceDefinition> CollectOwnedCards()
        {
            var result = new List<PieceDefinition>(); // 제거·강화 후보 목록
            if (_runState == null) return result;

            for (int i = 0; i < _runState.Deck.OwnedCardPool.Count && result.Count < MaxOwnedChoiceCards; i++)
            {
                PieceDefinition card = _runState.Deck.OwnedCardPool[i]; // 현재 보유 카드 조회
                if (card == null) continue;
                if (card.MovementType == PieceMovementType.King) continue;
                if (card.Category == PieceCategory.Monster || card.Category == PieceCategory.Boss) continue;
                result.Add(card); // 조작 가능한 플레이어 카드 등록
            }

            return result;
        }

        private void HideLegacyPlaceholder()
        {
            if (_placeholderUI == null) _placeholderUI = Object.FindFirstObjectByType<StagePlaceholderUI>(); // 임시 UI 지연 탐색
            if (_placeholderUI != null) _placeholderUI.Hide(); // 45일차 Screen Space Placeholder 숨김
        }

        private void CompleteCurrentStage()
        {
            CloseOverlayOnly(); // 돗자리·UI 먼저 제거
            _runState.Round.Restore(_runState.CurrentRound, RoundProgressStatus.Cleared, BattleOutcome.Victory); // 현재 비전투 스테이지 완료 기록

            if (_runState.CurrentRound >= RoundState.FinalRound)
            {
                _runState.Flow.CompleteRun(); // 최종 깊이 안전 완료
                return;
            }

            _runState.RouteMap.PreparePrototypeAfterBattle(_runState.CurrentRound); // 현재 위치 기준 다음 2~3개 경로 생성
            _runState.Flow.EnterMap(); // 경로 지도 선택 상태 복귀
            _routeMapBoardController.RefreshMapVisuals(); // 새 경로 지도 즉시 재구성
            Debug.Log($"47일차 비전투 스테이지 완료 -> Map / Depth={_runState.CurrentRound} / Gold={_economy.Currency}"); // 지도 복귀 로그
        }

        private void CloseOverlayOnly()
        {
            if (_overlayUI != null) _overlayUI.Hide(); // 판 위 돗자리·UI 숨김
            if (_cameraLock != null) _cameraLock.RestorePreviousView(); // Shop·Event 진입 전 카메라 시점 복원
            _activityActive = false; // 현재 비전투 화면 종료
            _eventScenario = null; // 이벤트 상태 정리
        }

        private static string GetStars(PieceDefinition card)
        {
            if (card == null) return string.Empty;
            int grade = Mathf.Clamp((int)card.Grade, 1, 5); // 등급 범위 보정
            return new string('★', grade); // 별 등급 문구 생성
        }

        private static void RecordResult(StageChoiceResult result)
        {
            if (result == null) return;
            string cardName = result.Card != null ? result.Card.DisplayName : "-"; // 관련 카드 이름 변환
            Debug.Log($"47일차 선택 결과: {result.EffectType} / {result.Summary} / GoldΔ={result.CurrencyDelta} / HPΔ={result.KingHpDelta} / Card={cardName}"); // 공통 결과 로그
        }

        private void OnDestroy()
        {
            CloseOverlayOnly(); // 씬 종료 시 판 위 UI 정리
        }
    }
}
