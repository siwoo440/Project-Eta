using System.Collections; // 초기화 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·Resources 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleController·TurnManager·TurnState·BattleOutcome 사용
using ProjectEta.Board; // RouteMapBoardController 사용
using ProjectEta.Cards; // PlayerStartingDeckCatalog 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.UI; // CardRewardUI 사용

namespace ProjectEta.Run
{
    [DefaultExecutionOrder(1030)]
    public sealed class CardRewardController : MonoBehaviour
    {
        private const string RewardCatalogResourceName = "PlayerStartingDeck26"; // 보상 원본 카드 카탈로그 이름
        private const int CandidateCount = 3; // 기본 카드 보상 후보 수

        private readonly CardRewardState _rewardState = new CardRewardState(); // 현재 보상 상태
        private BattleController _battleController; // 전투 결과 접근
        private TurnManager _turnManager; // 전투 종료 이벤트 대상
        private RouteMapBoardController _routeMapBoardController; // 지도 화면 갱신
        private PlayerStartingDeckCatalog _rewardCatalog; // 보상 원본 풀
        private CardRewardUI _rewardUI; // 카드 3택 UI
        private RunState _runState; // 현재 런 상태
        private bool _combatVictoryPending; // 전투 승리 후 지도 전환 대기

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<CardRewardController>() != null) return; // 중복 생성 차단

            var host = new GameObject("CardRewardController_Day46"); // 카드 보상 호스트 생성
            host.AddComponent<CardRewardController>(); // 카드 보상 관리자 추가
        }

        private IEnumerator Start()
        {
            const int maxWaitFrames = 240; // 최대 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 전투 컨트롤러 탐색
                _routeMapBoardController = Object.FindFirstObjectByType<RouteMapBoardController>(); // 지도 컨트롤러 탐색

                if (_battleController != null && _battleController.RunState != null && _battleController.TurnManager != null && _routeMapBoardController != null)
                {
                    _runState = _battleController.RunState; // 현재 런 상태 저장
                    _rewardCatalog = Resources.Load<PlayerStartingDeckCatalog>(RewardCatalogResourceName); // 보상 카탈로그 로드
                    _rewardUI = GetComponent<CardRewardUI>(); // 기존 보상 UI 탐색
                    if (_rewardUI == null) _rewardUI = gameObject.AddComponent<CardRewardUI>(); // 보상 UI 자동 추가
                    BindTurnManager(_battleController.TurnManager); // 전투 종료 이벤트 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("70일차 CardRewardController 초기화 실패: BattleController·RunState·RouteMapBoardController를 확인하세요."); // 초기화 실패 기록
        }

        private void Update()
        {
            if (_runState == null || _battleController == null) return; // 런타임 준비 전 차단
            if (_battleController.TurnManager != _turnManager) BindTurnManager(_battleController.TurnManager); // TurnManager 교체 대응
            if (_rewardState.IsActive) return; // 현재 보상 선택 중 중복 시작 차단

            if (_combatVictoryPending && _runState.CurrentFlowPhase == RunFlowPhase.Map)
            {
                BeginReward(CardRewardSource.BattleVictory); // 전투 승리 보상 시작
                return; // 같은 프레임 중복 보상 차단
            }

            if (_runState.CurrentFlowPhase == RunFlowPhase.Reward && _runState.RouteMap.HasSelectedNode)
            {
                BeginReward(CardRewardSource.RewardNode); // Reward 노드 보상 시작
            }
        }

        private void BindTurnManager(TurnManager turnManager)
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 이전 이벤트 해제
            _turnManager = turnManager; // 현재 TurnManager 저장
            if (_turnManager != null) _turnManager.TurnChanged += HandleTurnChanged; // 새 이벤트 연결
        }

        private void HandleTurnChanged(TurnState state, int turnNumber)
        {
            if (_runState == null || _turnManager == null) return; // 필수 상태 누락 방어
            if (state != TurnState.BattleEnded || _turnManager.Outcome != BattleOutcome.Victory) return; // 승리 외 결과 제외
            if (_runState.CurrentRound >= RoundState.FinalRound) return; // 최종 보스 승리 보상 제외
            _combatVictoryPending = true; // 지도 전환 후 보상 예약
        }

        private void BeginReward(CardRewardSource source)
        {
            if (_runState == null || _rewardState.IsActive) return; // 잘못된 중복 시작 차단

            if (source == CardRewardSource.BattleVictory)
            {
                _combatVictoryPending = false; // 이번 승리 보상 예약 소비
                _runState.Flow.EnterReward(); // 지도 배경 유지 Reward 흐름 진입
            }

            if (_rewardCatalog == null)
            {
                Debug.LogError($"70일차 카드 보상 실패: Resources/{RewardCatalogResourceName}를 찾지 못했습니다."); // 리소스 누락 기록
                CompleteRewardWithoutCard(source); // 보상 없이 흐름 복귀
                return; // 후보 생성 중단
            }

            CardRewardProfile profile = CardRewardQualityRules.GetProfile(_runState.CurrentRound, source); // Stage·보상 경로 기반 품질 규칙 계산
            int seed = CreateRewardSeed(source); // 현재 런 상태 기반 재현 가능한 시드 생성
            var candidates = CardRewardGenerator.Generate(_rewardCatalog.Cards, _runState.Deck.OwnedCardPool, CandidateCount, seed, profile); // 품질 가중 카드 3택 후보 생성

            if (candidates.Count == 0)
            {
                Debug.LogWarning("70일차 카드 보상 후보 없음: 현재 획득 가능한 카드가 없어 보상을 건너뜁니다."); // 보상 풀 소진 기록
                CompleteRewardWithoutCard(source); // 런 진행 유지
                return; // UI 표시 생략
            }

            _rewardState.Begin(candidates, source); // 현재 후보·발생 경로 저장
            _rewardUI.Show(_rewardState.Candidates, source, profile, HandleCardSelected); // Stage·품질 정보를 포함한 카드 3택 화면 표시
            Debug.Log($"70일차 카드 보상 시작: Source={source} / Stage={_runState.CurrentRound} / Quality={profile.Quality} / Weights={profile.OneStarWeight}:{profile.TwoStarWeight}:{profile.ThreeStarWeight} / Candidates={candidates.Count}"); // 품질 생성 결과 기록
        }

        private int CreateRewardSeed(CardRewardSource source)
        {
            unchecked
            {
                int seed = _runState.CurrentRound * 73856093; // 현재 Stage 시드 반영
                seed ^= _runState.Deck.OwnedCardPool.Count * 19349663; // 현재 보유 카드 수 반영
                seed ^= source == CardRewardSource.RewardNode ? 83492791 : 297121507; // 보상 경로 반영
                return seed; // 최종 보상 시드 반환
            }
        }

        private void HandleCardSelected(PieceDefinition definition)
        {
            if (!_rewardState.IsActive || !_rewardState.Contains(definition)) return; // 현재 후보 외 선택 차단

            if (!CardRewardRules.TryAddOwnedCard(_runState.Deck, definition))
            {
                Debug.LogWarning($"70일차 카드 보상 획득 차단: {definition.DisplayName} / 동일 카드 보유 상한 도달"); // 획득 실패 기록
                return; // 보상 화면 유지
            }

            if (!_rewardState.TrySelect(definition)) return; // 한 보상 두 번째 선택 차단
            CardRewardSource source = _rewardState.Source; // 상태 초기화 전 발생 경로 저장
            _rewardUI.Hide(); // 보상 화면 종료
            Debug.Log($"70일차 카드 보상 획득: {definition.DisplayName} / Grade={definition.Grade} / Owned={_runState.Deck.OwnedCardPool.Count}"); // 획득 결과 기록
            CompleteReward(source); // 발생 경로에 맞춰 지도 복귀
        }

        private void CompleteReward(CardRewardSource source)
        {
            _rewardState.Clear(); // 현재 후보·선택 상태 정리

            bool completed = source == CardRewardSource.RewardNode
                ? RunStageFlowService.CompleteNonBattleStage(_runState)
                : RunStageFlowService.CompleteBattleReward(_runState); // 발생 경로별 완료 처리

            if (!completed)
            {
                Debug.LogWarning($"70일차 카드 보상 완료 거부: Source={source} / Flow={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound}"); // 진행 상태 불일치 기록
                return; // 잘못된 상태 변경 차단
            }

            if (_runState.CurrentFlowPhase == RunFlowPhase.Map) _routeMapBoardController.RefreshMapVisuals(); // 지도 복귀 시 화면 갱신
            Debug.Log($"70일차 카드 보상 완료 -> {_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound} / Source={source}"); // 완료 결과 기록
        }

        private void CompleteRewardWithoutCard(CardRewardSource source)
        {
            _rewardState.Clear(); // 남은 임시 상태 제거

            bool completed = source == CardRewardSource.RewardNode
                ? RunStageFlowService.CompleteNonBattleStage(_runState)
                : RunStageFlowService.CompleteBattleReward(_runState); // 카드 획득 여부와 무관하게 동일 진행 규칙 적용

            if (!completed)
            {
                Debug.LogWarning($"70일차 카드 없는 보상 완료 거부: Source={source} / Flow={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound}"); // 진행 상태 불일치 기록
                return; // 잘못된 상태 변경 차단
            }

            if (_runState.CurrentFlowPhase == RunFlowPhase.Map) _routeMapBoardController.RefreshMapVisuals(); // 정상 지도 복귀 시 화면 갱신
        }

        private void OnDestroy()
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 전투 종료 이벤트 해제
            if (_rewardUI != null) _rewardUI.Hide(); // 남은 보상 UI 숨김
        }
    }
}
