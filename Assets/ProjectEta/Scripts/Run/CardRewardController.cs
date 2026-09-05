using System.Collections; // 초기화 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·Resources 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleController·TurnManager·TurnState·BattleOutcome 사용
using ProjectEta.Board; // RouteMapBoardController 사용
using ProjectEta.Cards; // PlayerStartingDeckCatalog 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.UI; // CardRewardUI 사용

namespace ProjectEta.Run // 카드 보상 런타임 네임스페이스
{
    [DefaultExecutionOrder(1030)] // 지도 전환 이후 보상 흐름을 감지
    public sealed class CardRewardController : MonoBehaviour // 전투 승리·Reward 노드를 카드 3장 선택 흐름으로 연결하는 46일차 관리자
    {
        private const string RewardCatalogResourceName = "PlayerStartingDeck26"; // 26종 보상 원본 카드 카탈로그 Resources 이름
        private const int CandidateCount = 3; // 기본 카드 보상 후보 수

        private readonly CardRewardState _rewardState = new CardRewardState(); // 현재 보상 후보·선택 상태
        private BattleController _battleController; // 전투 결과·TurnManager 접근
        private TurnManager _turnManager; // 전투 승리 이벤트 감지 대상
        private RouteMapBoardController _routeMapBoardController; // 보상 완료 후 같은 지도 화면 갱신
        private PlayerStartingDeckCatalog _rewardCatalog; // 카드 보상 원본 풀
        private CardRewardUI _rewardUI; // 카드 3장 선택 화면
        private RunState _runState; // 런 전체 덱·흐름 상태
        private bool _combatVictoryPending; // 전투 승리 후 RunState가 Map으로 바뀌기를 기다리는 플래그

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬·Inspector 수정 없이 카드 보상 관리자 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<CardRewardController>() != null) return; // 중복 생성 차단

            var host = new GameObject("CardRewardController_Day46"); // 카드 보상 호스트 생성
            host.AddComponent<CardRewardController>(); // 카드 보상 관리자 추가
        }

        private IEnumerator Start() // BattleController·지도 컨트롤러 준비 후 보상 시스템 연결
        {
            const int maxWaitFrames = 240; // 최대 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 필수 런타임 객체 준비 대기
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색
                _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 현재 지도 컨트롤러 탐색

                if (_battleController != null && _battleController.RunState != null && _battleController.TurnManager != null && _routeMapBoardController != null) // 필수 객체 준비 확인
                {
                    _runState = _battleController.RunState; // 현재 런 상태 저장
                    _rewardCatalog = Resources.Load<PlayerStartingDeckCatalog>(RewardCatalogResourceName); // 보상 원본 카드 풀 로드
                    _rewardUI = GetComponent<CardRewardUI>(); // 같은 호스트의 기존 보상 UI 탐색
                    if (_rewardUI == null) _rewardUI = gameObject.AddComponent<CardRewardUI>(); // 없으면 보상 UI 자동 추가
                    BindTurnManager(_battleController.TurnManager); // 전투 승리 이벤트 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("46일차 CardRewardController 초기화 실패: BattleController·RunState·RouteMapBoardController를 확인하세요."); // 초기화 실패 기록
        }

        private void Update() // 전투 승리 후 Map 전환과 Reward 노드 진입 감지
        {
            if (_runState == null || _battleController == null) return; // 런타임 준비 전 처리 차단
            if (_battleController.TurnManager != _turnManager) BindTurnManager(_battleController.TurnManager); // TurnManager 교체 시 이벤트 재연결
            if (_rewardState.IsActive) return; // 현재 카드 선택 중 중복 시작 차단

            if (_combatVictoryPending && _runState.CurrentFlowPhase == RunFlowPhase.Map) // 전투 결과 처리가 Map까지 완료됐는지 확인
            {
                BeginReward(CardRewardSource.BattleVictory); // 전투 승리 카드 보상 시작
                return; // 같은 프레임 다른 보상 진입 차단
            }

            if (_runState.CurrentFlowPhase == RunFlowPhase.Reward && _runState.RouteMap.HasSelectedNode) // Reward StageNode 진입 확인
            {
                BeginReward(CardRewardSource.RewardNode); // 독립 카드 보상 스테이지 시작
            }
        }

        private void BindTurnManager(TurnManager turnManager) // 현재 TurnManager 전투 종료 이벤트 연결
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 이전 이벤트 구독 해제
            _turnManager = turnManager; // 현재 TurnManager 저장
            if (_turnManager != null) _turnManager.TurnChanged += HandleTurnChanged; // 새 TurnManager 이벤트 구독
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 전투 종료 순간 카드 보상 예약
        {
            if (_runState == null || _turnManager == null) return; // 필수 상태 누락 방어
            if (state != TurnState.BattleEnded || _turnManager.Outcome != BattleOutcome.Victory) return; // 전투 승리 외 결과 제외
            if (_runState.CurrentRound >= RoundState.FinalRound) return; // 최종 보스 승리는 런 완료 흐름 유지
            _combatVictoryPending = true; // RunState가 승리 결과를 Map으로 반영한 뒤 보상 시작 예약
        }

        private void BeginReward(CardRewardSource source) // 현재 보유 풀 기준 카드 3장 후보 생성·표시
        {
            if (_runState == null || _rewardState.IsActive) return; // 잘못된 중복 시작 차단

            if (source == CardRewardSource.BattleVictory) // 전투 승리 보상 준비
            {
                _combatVictoryPending = false; // 이번 승리 보상 예약 소비
                _runState.Flow.EnterReward(); // 지도 배경을 유지한 보상 흐름 진입
            }

            if (_rewardCatalog == null) // 보상 카탈로그 누락 확인
            {
                Debug.LogError($"46일차 카드 보상 실패: Resources/{RewardCatalogResourceName}를 찾지 못했습니다."); // 리소스 누락 기록
                CompleteRewardWithoutCard(source); // 진행 불가 방지를 위해 보상 없이 흐름 복귀
                return; // 후보 생성 중단
            }

            int seed = CreateRewardSeed(source); // 현재 깊이·보유 카드 기반 보상 난수 시드 생성
            var candidates = CardRewardGenerator.Generate(_rewardCatalog.Cards, _runState.Deck.OwnedCardPool, CandidateCount, seed); // 중복·상한·등급 예외를 적용한 후보 생성

            if (candidates.Count == 0) // 획득 가능한 후보 없음 확인
            {
                Debug.LogWarning("46일차 카드 보상 후보 없음: 현재 획득 가능한 카드가 없어 보상을 건너뜁니다."); // 보상 풀 소진 기록
                CompleteRewardWithoutCard(source); // 런 진행 유지
                return; // UI 표시 생략
            }

            _rewardState.Begin(candidates, source); // 현재 후보·보상 발생 경로 상태 저장
            _rewardUI.Show(_rewardState.Candidates, source, HandleCardSelected); // 카드 3장 선택 화면 표시
            Debug.Log($"46일차 카드 보상 시작: Source={source} / Round={_runState.CurrentRound} / Candidates={candidates.Count}"); // 후보 생성 결과 기록
        }

        private int CreateRewardSeed(CardRewardSource source) // 현재 런 상태 기반 재현 가능한 프로토타입 보상 시드 생성
        {
            unchecked // 정수 오버플로를 의도적인 시드 혼합으로 허용
            {
                int seed = _runState.CurrentRound * 73856093; // 현재 깊이 시드 반영
                seed ^= _runState.Deck.OwnedCardPool.Count * 19349663; // 현재 보유 카드 수 반영
                seed ^= source == CardRewardSource.RewardNode ? 83492791 : 297121507; // 보상 발생 경로 반영
                return seed; // 최종 보상 시드 반환
            }
        }

        private void HandleCardSelected(PieceDefinition definition) // UI에서 선택한 카드 1장을 런 보유 풀에 반영
        {
            if (!_rewardState.IsActive || !_rewardState.Contains(definition)) return; // 현재 후보 외 선택 차단
            if (!CardRewardRules.TryAddOwnedCard(_runState.Deck, definition)) // 보유 상한 재검증 후 OwnedCardPool 추가
            {
                Debug.LogWarning($"46일차 카드 보상 획득 차단: {definition.DisplayName} / 동일 카드 보유 상한 도달"); // 획득 실패 이유 기록
                return; // 보상 화면 유지
            }

            if (!_rewardState.TrySelect(definition)) return; // 한 보상에서 두 번째 선택 차단
            CardRewardSource source = _rewardState.Source; // 상태 초기화 전 보상 발생 경로 저장
            _rewardUI.Hide(); // 보상 선택 화면 종료
            Debug.Log($"46일차 카드 보상 획득: {definition.DisplayName} / Owned={_runState.Deck.OwnedCardPool.Count}"); // 보유 풀 반영 결과 기록
            CompleteReward(source); // 보상 발생 경로에 맞춰 지도 복귀
        }

        private void CompleteReward(CardRewardSource source) // 카드 획득 완료 후 경로 지도 상태 복귀
        {
            _rewardState.Clear(); // 현재 후보·선택 상태 정리

            if (source == CardRewardSource.RewardNode) // 독립 Reward 노드 완료 처리
            {
                _runState.Round.Restore(_runState.CurrentRound, RoundProgressStatus.Cleared, BattleOutcome.Victory); // 현재 보상 스테이지 완료 기록

                if (_runState.CurrentRound >= RoundState.FinalRound) // 최종 깊이 안전 처리
                {
                    _runState.Flow.CompleteRun(); // 최종 깊이라면 런 완료
                    return; // 다음 지도 생성 없음
                }

                _runState.RouteMap.PreparePrototypeAfterBattle(_runState.CurrentRound); // 현재 보상 노드 위치 기준 다음 깊이 분기 생성
            }

            _runState.Flow.EnterMap(); // 같은 10×10 체스판 경로 지도 모드 복귀
            _routeMapBoardController.RefreshMapVisuals(); // 현재 RouteMapState를 즉시 화면에 다시 표시
            Debug.Log($"46일차 카드 보상 완료 -> Map / Round={_runState.CurrentRound} / Source={source}"); // 지도 복귀 결과 기록
        }

        private void CompleteRewardWithoutCard(CardRewardSource source) // 후보 없음·리소스 누락 시 진행 차단 방지용 완료 처리
        {
            _rewardState.Clear(); // 남은 임시 상태 제거

            if (source == CardRewardSource.RewardNode) // 독립 Reward 노드 완료 처리
            {
                _runState.Round.Restore(_runState.CurrentRound, RoundProgressStatus.Cleared, BattleOutcome.Victory); // 보상 노드 완료 기록

                if (_runState.CurrentRound >= RoundState.FinalRound) // 최종 깊이 안전 처리
                {
                    _runState.Flow.CompleteRun(); // 런 완료 처리
                    return; // 지도 생성 생략
                }

                _runState.RouteMap.PreparePrototypeAfterBattle(_runState.CurrentRound); // 다음 깊이 분기 생성
            }

            _runState.Flow.EnterMap(); // 경로 지도 흐름 복귀
            _routeMapBoardController.RefreshMapVisuals(); // 지도 화면 즉시 갱신
        }

        private void OnDestroy() // 카드 보상 관리자 제거 시 이벤트·UI 정리
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 전투 종료 이벤트 구독 해제
            if (_rewardUI != null) _rewardUI.Hide(); // 남은 보상 UI 숨김
        }
    }
}
