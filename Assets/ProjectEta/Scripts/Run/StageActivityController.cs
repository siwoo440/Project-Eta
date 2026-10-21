using System.Collections; // 초기화 대기 코루틴 사용
using System.Collections.Generic; // 상점 카드·UI 옵션 목록 사용
using UnityEngine; // MonoBehaviour·Resources 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // BoardView·RouteMapBoardController 사용
using ProjectEta.Cards; // PlayerStartingDeckCatalog 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Meta; // 런 시작 해금 상태 조회
using ProjectEta.UI; // 판 위 StageBoardOverlayUI 사용

namespace ProjectEta.Run
{
    [DefaultExecutionOrder(1040)]
    public sealed class StageActivityController : MonoBehaviour
    {
        private const string CardCatalogResourceName = "PlayerStartingDeck26"; // 상점·이벤트 카드 원본 카탈로그
        private const int ShopOfferCount = 3; // 상점 카드 상품 수
        private const int OwnedCardsPerPage = 4; // 제거·강화 페이지 카드 수

        private readonly List<ShopOffer> _shopOffers = new List<ShopOffer>(); // 현재 상점 상품 목록
        private BattleController _battleController; // RunState 접근 전투 컨트롤러
        private RouteMapBoardController _routeMapBoardController; // 비전투 완료 후 지도 갱신
        private BoardView _boardView; // 판 위 오버레이 기준 보드
        private StagePlaceholderUI _placeholderUI; // 기존 임시 UI 숨김 대상
        private StageBoardOverlayUI _overlayUI; // 판 위 상점·이벤트 UI
        private StageActivityCameraLock _cameraLock; // Shop·Event 카메라 잠금
        private PlayerStartingDeckCatalog _cardCatalog; // 카드 상품·이벤트 원본 풀
        private RunState _runState; // 현재 런 상태
        private RunEconomyState _economy; // 현재 런 Gold 상태
        private bool _activityActive; // Shop/Event 진행 여부
        private RunFlowPhase _activePhase; // 현재 비전투 타입
        private RunEventPresenter _eventPresenter; // 102일차 이벤트 화면 흐름
        private int _removePageIndex; // 카드 제거 페이지 번호
        private int _upgradePageIndex; // 카드 강화 페이지 번호

        private IEnumerator Start()
        {
            const int maxWaitFrames = 240; // 런타임 의존성 최대 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 전투 컨트롤러 탐색
                _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 지도 컨트롤러 탐색
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 보드 뷰 탐색
                _placeholderUI = Object.FindFirstObjectByType<StagePlaceholderUI>(); // 기존 임시 UI 탐색

                if (_battleController != null && _battleController.RunState != null && _routeMapBoardController != null && _boardView != null)
                {
                    _runState = _battleController.RunState; // 현재 런 상태 연결
                    _economy = RunEconomyService.GetOrCreate(_runState); // 런 Gold 상태 연결
                    _cardCatalog = Resources.Load<PlayerStartingDeckCatalog>(CardCatalogResourceName); // 카드 카탈로그 로드
                    _overlayUI = GetComponent<StageBoardOverlayUI>(); // 기존 판 위 UI 탐색
                    if (_overlayUI == null) _overlayUI = gameObject.AddComponent<StageBoardOverlayUI>(); // 판 위 UI 자동 추가
                    _overlayUI.Initialize(_boardView); // 판 위 UI 보드 연결
                    _cameraLock = GetComponent<StageActivityCameraLock>(); // 기존 카메라 잠금기 탐색
                    if (_cameraLock == null) _cameraLock = gameObject.AddComponent<StageActivityCameraLock>(); // 카메라 잠금기 자동 추가
                    _cameraLock.Initialize(_boardView); // 카메라 잠금 보드 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("71일차 StageActivityController 초기화 실패: BattleController·RunState·RouteMapBoardController·BoardView를 확인하세요."); // 초기화 실패 로그
        }

        private void Update()
        {
            if (_runState == null || _overlayUI == null) return; // 런타임 준비 전 차단

            if (!_activityActive && _runState.CurrentFlowPhase == RunFlowPhase.Shop)
            {
                BeginShop(); // 상점 노드 진입
                return; // 같은 프레임 이벤트 진입 차단
            }

            if (!_activityActive && _runState.CurrentFlowPhase == RunFlowPhase.Event)
            {
                BeginEvent(); // 이벤트 노드 진입
                return; // 같은 프레임 추가 처리 차단
            }

            if (_activityActive && _runState.CurrentFlowPhase != _activePhase)
            {
                CloseOverlayOnly(); // 외부 흐름 변경 UI 정리
            }
        }

        private void BeginShop()
        {
            _activityActive = true; // 상점 진행 상태 설정
            _activePhase = RunFlowPhase.Shop; // 현재 상점 타입 저장
            _removePageIndex = 0; // 제거 페이지 초기화
            _upgradePageIndex = 0; // 강화 페이지 초기화
            HideLegacyPlaceholder(); // 기존 임시 UI 숨김
            if (_cameraLock != null) _cameraLock.LockToPrimaryView(); // 상점 카메라 고정
            PrepareShopOffers(); // 상점 상품 생성
            ShowShopMain(); // 상점 메인 표시
            Debug.Log($"71일차 Shop 진입: Phase={GetCurrentPhase()} / Stage={_runState.CurrentRound} / Gold={_economy.Currency} / Offers={_shopOffers.Count}"); // 상점 진입 로그
        }

        private void PrepareShopOffers()
        {
            _shopOffers.Clear(); // 이전 상품 초기화
            if (_cardCatalog == null) return; // 카드 카탈로그 누락 차단

            int phase = GetCurrentPhase(); // 현재 페이즈 조회
            IReadOnlyList<ShopOffer> generated = ShopOfferGenerator.Generate(
                _cardCatalog.Cards,
                _runState.Deck.OwnedCardPool,
                ShopOfferCount,
                _runState.RouteMap.MapSeed,
                phase,
                _runState.CurrentRound,
                _runState.RouteMap.CurrentNodeId,
                _runState.Deck.DeadCardPile); // Phase·Stage·Node·전체 소유 카드 기반 상품 생성

            var cards = new List<PieceDefinition>(); // 상품 후보 기록 목록
            foreach (var offer in generated) // 실제 상품 순회
            { // 범위 시작
                if (offer?.Card != null) // 유효 카드 확인
                { // 범위 시작
                    cards.Add(offer.Card); // 후보 카드 추가
                } // 범위 종료
            } // 범위 종료
            int offerSeed = ShopOfferGenerator.CreateSeed(_runState.RouteMap.MapSeed, phase, _runState.CurrentRound, _runState.RouteMap.CurrentNodeId, _runState.Deck.OwnedCardPool.Count); // 상품 생성 Seed
            RunBalanceTelemetry.RecordOffers(_runState, "Shop", offerSeed, cards); // 상점 후보 기록
            for (int i = 0; i < generated.Count; i++)
            {
                ShopOffer offer = generated[i]; // 현재 생성 상품 조회
                if (offer != null && offer.Card != null) _shopOffers.Add(offer); // 유효 상품 등록
            }
        }

        private void ShowShopMain()
        {
            int phase = GetCurrentPhase(); // 현재 페이즈 조회
            IReadOnlyList<PieceDefinition> manageable = ShopService.GetManageableCards(_runState); // 제거·강화 가능 카드 조회
            int removePrice = ShopPriceRules.GetCardRemovePrice(phase); // 카드 제거 비용 조회
            int healPrice = ShopPriceRules.GetHealPrice(phase); // 킹 회복 비용 조회
            int upgradePrice = ShopPriceRules.GetUpgradePrice(phase, _runState.CurrentRound); // 카드 강화 비용 조회
            var options = new List<StageOverlayOption>(); // 상점 메인 선택지 생성

            options.Add(new StageOverlayOption(
                "카드 구매",
                "등급·진행도별 가격 · 후보 최대 3장",
                HasPurchasableOffer(),
                ShowPurchasePage)); // 카드 구매 진입

            options.Add(new StageOverlayOption(
                "카드 제거",
                $"덱에서 카드 1장 제거 · {removePrice} Gold",
                manageable.Count > 0,
                OpenRemovePage)); // 카드 제거 진입

            options.Add(new StageOverlayOption(
                "킹 HP 회복",
                $"HP +{ShopPriceRules.HealAmount} · {healPrice} Gold",
                _runState.KingHp < RunEconomyRules.PrototypeKingMaxHp,
                HealKing)); // 킹 회복 실행

            options.Add(new StageOverlayOption(
                "카드 업그레이드",
                $"선택 카드 HP/ATK +1 · {upgradePrice} Gold",
                manageable.Count > 0,
                OpenUpgradePage)); // 카드 강화 진입

            options.Add(new StageOverlayOption(
                "상점 나가기",
                "구매를 마치고 다음 경로를 선택합니다.",
                true,
                CompleteCurrentStage)); // 상점 완료

            string subtitle = $"Phase {phase}/{RunPhaseProgressService.TotalPhases}   |   Gold {_economy.Currency}   |   King HP {_runState.KingHp}/{RunEconomyRules.PrototypeKingMaxHp}"; // 상점 상태 문구
            _overlayUI.ShowPage(StageOverlayMode.Shop, "상점", subtitle, options); // 상점 메인 표시
        }

        private void ShowPurchasePage()
        {
            var options = new List<StageOverlayOption>(); // 구매 상품 선택지 생성

            for (int i = 0; i < _shopOffers.Count; i++)
            {
                ShopOffer offer = _shopOffers[i]; // 현재 상품 조회
                if (offer == null || offer.Card == null) continue; // 빈 상품 제외
                PieceDefinition card = offer.Card; // 상품 카드 조회
                bool canBuy = !offer.IsPurchased && _economy.Currency >= offer.Price && CardRewardRules.CanOffer(card, _runState.Deck.OwnedCardPool, _runState.Deck.DeadCardPile); // 정상·사망 보유 기준 현재 구매 가능 여부 계산
                ShopOffer captured = offer; // 버튼 콜백 상품 고정
                string title = offer.IsPurchased ? $"✓ {card.DisplayName}  {GetStars(card)}" : $"{card.DisplayName}  {GetStars(card)}"; // 구매 상태 제목 생성
                string description = offer.IsPurchased ? "구매 완료" : $"HP {card.BaseHp} / ATK {card.BaseAtk}   ·   {offer.Price} Gold"; // 구매 상태 설명 생성

                options.Add(new StageOverlayOption(
                    title,
                    description,
                    canBuy,
                    () => PurchaseCard(captured), // 카드 구매 콜백
                    offer.Price, // 실제 상품 가격 전달
                    offer.IsPurchased)); // 실제 구매 완료 상태 전달
            }

            options.Add(new StageOverlayOption("뒤로", "상점 메인으로 돌아갑니다.", true, ShowShopMain)); // 메인 복귀 버튼
            var snapshot = RunContentUnlockSnapshotService.GetOrCreate(_runState, MetaProgressService.Current); // 이번 런의 고정 해금 조회
            int locked = RunContentEligibility.CountLocked(snapshot); // 실제 원본 후보의 미해금 수 조회
            _overlayUI.ShowPage(StageOverlayMode.Shop, "카드 구매", $"Gold {_economy.Currency} · 1성 기물\n미해금 {locked}종 제외 · 해금은 다음 런부터", options); // 구매·잠금 정책 안내
        }

        private void PurchaseCard(ShopOffer offer)
        {
            if (ShopService.TryPurchaseCard(_runState, _economy, offer, out StageChoiceResult result))
            {
                RecordResult(result); // 구매 성공 결과 기록
            }

            ShowPurchasePage(); // 구매 페이지 상태 갱신
        }

        private void OpenRemovePage()
        {
            _removePageIndex = 0; // 제거 페이지 첫 장 이동
            ShowRemovePage(); // 제거 페이지 표시
        }

        private void ShowRemovePage()
        {
            IReadOnlyList<PieceDefinition> owned = ShopService.GetManageableCards(_runState); // 전체 제거 가능 카드 조회
            int pageCount = GetPageCount(owned.Count); // 전체 제거 페이지 수 계산
            _removePageIndex = Mathf.Clamp(_removePageIndex, 0, pageCount - 1); // 제거 페이지 범위 보정
            int startIndex = _removePageIndex * OwnedCardsPerPage; // 현재 페이지 시작 위치 계산
            int endIndex = Mathf.Min(startIndex + OwnedCardsPerPage, owned.Count); // 현재 페이지 끝 위치 계산
            int phase = GetCurrentPhase(); // 현재 페이즈 조회
            int removePrice = ShopPriceRules.GetCardRemovePrice(phase); // 현재 제거 가격 계산
            var options = new List<StageOverlayOption>(); // 제거 선택지 생성

            for (int i = startIndex; i < endIndex; i++)
            {
                PieceDefinition card = owned[i]; // 현재 제거 카드 조회
                PieceDefinition captured = card; // 버튼 콜백 카드 고정
                bool canRemove = _economy.Currency >= removePrice; // 제거 비용 지불 가능 여부 계산

                options.Add(new StageOverlayOption(
                    $"{card.DisplayName} 제거",
                    $"{GetStars(card)}   HP {card.BaseHp} / ATK {card.BaseAtk}   ·   {removePrice} Gold",
                    canRemove,
                    () => RemoveCard(captured))); // 카드 제거 콜백
            }

            if (_removePageIndex > 0) options.Add(new StageOverlayOption("◀ 이전", "이전 카드 목록을 표시합니다.", true, ShowPreviousRemovePage)); // 이전 페이지 버튼
            if (_removePageIndex + 1 < pageCount) options.Add(new StageOverlayOption("다음 ▶", "다음 카드 목록을 표시합니다.", true, ShowNextRemovePage)); // 다음 페이지 버튼
            options.Add(new StageOverlayOption("뒤로", "상점 메인으로 돌아갑니다.", true, ShowShopMain)); // 메인 복귀 버튼

            string subtitle = $"Gold {_economy.Currency} · {Mathf.Min(_removePageIndex + 1, pageCount)}/{pageCount} 페이지 · King은 제거할 수 없습니다."; // 제거 페이지 상태 문구
            _overlayUI.ShowPage(StageOverlayMode.Shop, "카드 제거", subtitle, options); // 제거 페이지 표시
        }

        private void ShowPreviousRemovePage()
        {
            _removePageIndex--; // 제거 페이지 감소
            ShowRemovePage(); // 제거 페이지 갱신
        }

        private void ShowNextRemovePage()
        {
            _removePageIndex++; // 제거 페이지 증가
            ShowRemovePage(); // 제거 페이지 갱신
        }

        private void RemoveCard(PieceDefinition card)
        {
            int phase = GetCurrentPhase(); // 현재 페이즈 조회

            if (ShopService.TryRemoveCard(_runState, _economy, card, phase, out StageChoiceResult result))
            {
                RecordResult(result); // 제거 성공 결과 기록
            }

            ShowRemovePage(); // 제거 페이지 상태 갱신
        }

        private void HealKing()
        {
            int phase = GetCurrentPhase(); // 현재 페이즈 조회

            if (ShopService.TryHealKing(_runState, _economy, phase, RunEconomyRules.PrototypeKingMaxHp, out StageChoiceResult result))
            {
                RecordResult(result); // 회복 성공 결과 기록
            }

            ShowShopMain(); // 상점 메인 상태 갱신
        }

        private void OpenUpgradePage()
        {
            _upgradePageIndex = 0; // 강화 페이지 첫 장 이동
            ShowUpgradePage(); // 강화 페이지 표시
        }

        private void ShowUpgradePage()
        {
            IReadOnlyList<PieceDefinition> owned = ShopService.GetManageableCards(_runState); // 전체 강화 가능 카드 조회
            int pageCount = GetPageCount(owned.Count); // 전체 강화 페이지 수 계산
            _upgradePageIndex = Mathf.Clamp(_upgradePageIndex, 0, pageCount - 1); // 강화 페이지 범위 보정
            int startIndex = _upgradePageIndex * OwnedCardsPerPage; // 현재 페이지 시작 위치 계산
            int endIndex = Mathf.Min(startIndex + OwnedCardsPerPage, owned.Count); // 현재 페이지 끝 위치 계산
            int phase = GetCurrentPhase(); // 현재 페이즈 조회
            int upgradePrice = ShopPriceRules.GetUpgradePrice(phase, _runState.CurrentRound); // 현재 강화 가격 계산
            var options = new List<StageOverlayOption>(); // 강화 선택지 생성

            for (int i = startIndex; i < endIndex; i++)
            {
                PieceDefinition card = owned[i]; // 현재 강화 카드 조회
                PieceDefinition captured = card; // 버튼 콜백 카드 고정
                bool canUpgrade = _economy.Currency >= upgradePrice; // 강화 비용 지불 가능 여부 계산

                options.Add(new StageOverlayOption(
                    $"{card.DisplayName} 강화",
                    $"HP {card.BaseHp}→{card.BaseHp + 1} / ATK {card.BaseAtk}→{card.BaseAtk + 1}   ·   {upgradePrice} Gold",
                    canUpgrade,
                    () => UpgradeCard(captured))); // 카드 강화 콜백
            }

            if (_upgradePageIndex > 0) options.Add(new StageOverlayOption("◀ 이전", "이전 카드 목록을 표시합니다.", true, ShowPreviousUpgradePage)); // 이전 페이지 버튼
            if (_upgradePageIndex + 1 < pageCount) options.Add(new StageOverlayOption("다음 ▶", "다음 카드 목록을 표시합니다.", true, ShowNextUpgradePage)); // 다음 페이지 버튼
            options.Add(new StageOverlayOption("뒤로", "상점 메인으로 돌아갑니다.", true, ShowShopMain)); // 메인 복귀 버튼

            string subtitle = $"Gold {_economy.Currency} · {Mathf.Min(_upgradePageIndex + 1, pageCount)}/{pageCount} 페이지 · 런타임 강화"; // 강화 페이지 상태 문구
            _overlayUI.ShowPage(StageOverlayMode.Shop, "카드 업그레이드", subtitle, options); // 강화 페이지 표시
        }

        private void ShowPreviousUpgradePage()
        {
            _upgradePageIndex--; // 강화 페이지 감소
            ShowUpgradePage(); // 강화 페이지 갱신
        }

        private void ShowNextUpgradePage()
        {
            _upgradePageIndex++; // 강화 페이지 증가
            ShowUpgradePage(); // 강화 페이지 갱신
        }

        private void UpgradeCard(PieceDefinition card)
        {
            int phase = GetCurrentPhase(); // 현재 페이즈 조회

            if (ShopService.TryUpgradeCard(_runState, _economy, card, phase, _runState.CurrentRound, out StageChoiceResult result))
            {
                RecordResult(result); // 강화 성공 결과 기록
            }

            ShowUpgradePage(); // 강화 페이지 상태 갱신
        }

        private bool HasPurchasableOffer()
        {
            for (int i = 0; i < _shopOffers.Count; i++)
            {
                ShopOffer offer = _shopOffers[i]; // 현재 상품 조회
                if (offer == null || offer.Card == null || offer.IsPurchased) continue; // 빈 상품·구매 완료 제외
                if (CardRewardRules.CanOffer(offer.Card, _runState.Deck.OwnedCardPool, _runState.Deck.DeadCardPile)) return true; // 정상·사망 보유 기준 구매 가능 상품 확인
            }

            return false; // 구매 가능 상품 없음
        }

        private int GetCurrentPhase()
        {
            return RunPhaseProgressService.GetCurrentPhase(_runState); // 현재 RouteMap 페이즈 반환
        }

        private static int GetPageCount(int itemCount)
        {
            if (itemCount <= 0) return 1; // 빈 목록 단일 페이지 처리
            return (itemCount + OwnedCardsPerPage - 1) / OwnedCardsPerPage; // 올림 페이지 수 계산
        }

        private void BeginEvent() // 기획 이벤트 35종 화면 진입
        { // 메서드 시작
            _activityActive = true; // 중복 화면 생성 방지
            _activePhase = RunFlowPhase.Event; // 현재 비전투 흐름
            HideLegacyPlaceholder(); // 구형 화면 숨김
            if (_cameraLock != null) _cameraLock.LockToPrimaryView(); // 이벤트 입력용 카메라 고정
            BoardInputController input = Object.FindFirstObjectByType<BoardInputController>(); // 실제 합성 데이터 연결
            var unlocks = RunContentUnlockSnapshotService.GetOrCreate(_runState, MetaProgressService.Current); // 해당 런 고정 해금
            _eventPresenter = new RunEventPresenter(_runState, _economy, input?.PieceDatabase?.Definitions ?? _cardCatalog?.Cards, input?.FusionRecipeDatabase?.Recipes, unlocks, MetaProgressService.Current, _overlayUI, SaveEventCheckpoint, CompleteCurrentStage); // 전체 기물 데이터와 기존 보드 오버레이 연결
            _eventPresenter.Open(); // 고정 선택 또는 저장된 결과 표시
        } // 메서드 종료

        private bool SaveEventCheckpoint() // 선택과 결과의 안전 저장
        { // 메서드 시작
            if (!RunSaveSystem.IsSafeCheckpoint(_runState)) return false; // 전투 중 저장 진입 차단
            if (!RunSaveSystem.TrySave(_runState)) return false; // 실제 저장 실패 확인
            return MetaProgressService.Save(); // 영구 정보 열람 저장 확인
        } // 메서드 종료
        private void HideLegacyPlaceholder()
        {
            if (_placeholderUI == null) _placeholderUI = Object.FindFirstObjectByType<StagePlaceholderUI>(); // 기존 임시 UI 지연 탐색
            if (_placeholderUI != null) _placeholderUI.Hide(); // 기존 임시 UI 숨김
        }

        private void CompleteCurrentStage()
        {
            CloseOverlayOnly(); // 판 위 UI 먼저 정리

            if (!RunStageFlowService.CompleteNonBattleStage(_runState))
            {
                Debug.LogWarning($"72일차 Shop/Event 완료 거부: Flow={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound}"); // 잘못된 완료 상태 기록
                return; // 중복 상태 변경 차단
            }

            if (_runState.CurrentFlowPhase == RunFlowPhase.Map) _routeMapBoardController.RefreshMapVisuals(); // 지도 복귀 시 시각 갱신
            if (RunSaveSystem.IsSafeCheckpoint(_runState)) RunSaveSystem.TrySave(_runState); // 완료된 지도 체크포인트 저장
            Debug.Log($"72일차 비전투 스테이지 완료 -> {_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound} / Gold={_economy.Currency}"); // 완료 결과 로그
        }

        private void CloseOverlayOnly()
        {
            if (_overlayUI != null) _overlayUI.Hide(); // 판 위 UI 숨김
            if (_cameraLock != null) _cameraLock.RestorePreviousView(); // 이전 카메라 복원
            _activityActive = false; // 비전투 진행 상태 종료
            _eventPresenter?.Close(); // 남은 이벤트 콜백 차단
            _eventPresenter = null; // 이벤트 화면 소유 해제
        }

        private static string GetStars(PieceDefinition card)
        {
            if (card == null) return string.Empty; // 빈 카드 별 표시 없음
            int grade = Mathf.Clamp((int)card.Grade, 1, 5); // 등급 범위 보정
            return new string('★', grade); // 별 등급 문구 생성
        }

        private static void RecordResult(StageChoiceResult result)
        {
            if (result == null) return; // 빈 결과 기록 차단
            string cardName = result.Card != null ? result.Card.DisplayName : "-"; // 관련 카드 이름 변환
            Debug.Log($"72일차 선택 결과: {result.EffectType} / {result.Summary} / GoldΔ={result.CurrencyDelta} / HPΔ={result.KingHpDelta} / Card={cardName}"); // 선택 결과 로그
        }

        private void OnDestroy()
        {
            CloseOverlayOnly(); // 씬 종료 UI 정리
        }
    }
}
