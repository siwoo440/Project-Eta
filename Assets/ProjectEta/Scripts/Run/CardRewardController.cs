using System.Collections; // 초기화 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·Resources·Mathf 사용
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
        private CardRewardSource _pendingBattleRewardSource = CardRewardSource.BattleVictory; // 완료 전투별 대기 보상 경로
        private int _pendingRewardStage = RoundState.FirstRound; // 전투 전환 전 보상 Stage 보존
        private string _pendingRewardProfileId = string.Empty; // StageDefinition 전용 보상 프로필 ID 보존

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

            Debug.LogError("75일차 CardRewardController 초기화 실패: BattleController·RunState·RouteMapBoardController를 확인하세요."); // 초기화 실패 기록
        }

        private void Update()
        {
            if (_runState == null || _battleController == null) return; // 런타임 준비 전 차단
            if (_battleController.TurnManager != _turnManager) BindTurnManager(_battleController.TurnManager); // TurnManager 교체 대응
            if (_rewardState.IsActive) return; // 현재 보상 선택 중 중복 시작 차단

            if (_combatVictoryPending && _runState.CurrentFlowPhase == RunFlowPhase.Map)
            {
                BeginReward(_pendingBattleRewardSource, _pendingRewardStage, _pendingRewardProfileId); // 완료 전투 종류별 보상 시작
                return; // 같은 프레임 중복 보상 차단
            }

            if (_runState.CurrentFlowPhase == RunFlowPhase.Reward && _runState.RouteMap.HasSelectedNode)
            {
                StageDefinition definition = ResolveCurrentStageDefinition(); // 현재 Reward 노드 정의 조회
                string rewardProfileId = definition != null ? definition.RewardProfileId : string.Empty; // Reward 노드 프로필 ID 조회
                BeginReward(CardRewardSource.RewardNode, _runState.CurrentRound, rewardProfileId); // Reward 노드 보상 시작
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

            StageDefinition completedStage = ResolveCompletedBattleDefinition(); // 완료된 실제 StageDefinition 조회
            StageType completedType = completedStage != null ? completedStage.StageType : StageType.Battle; // 누락 시 일반 전투 fallback

            if (completedType == StageType.FinalBoss) return; // Final Boss 승리는 카드 보상 없이 Run Complete 유지

            _pendingBattleRewardSource = ResolveBattleRewardSource(completedType); // Battle·Elite·MidBoss 보상 경로 분리
            _pendingRewardStage = ResolveRewardStage(completedStage, _runState.CurrentRound); // Phase 전환 전 Stage 번호 보존
            _pendingRewardProfileId = completedStage != null ? completedStage.RewardProfileId : string.Empty; // StageDefinition 보상 ID 보존
            _combatVictoryPending = true; // 지도 전환 후 보상 예약
        }

        private void BeginReward(CardRewardSource source, int rewardStage, string rewardProfileId)
        {
            if (_runState == null || _rewardState.IsActive) return; // 잘못된 중복 시작 차단

            if (source != CardRewardSource.RewardNode)
            {
                _combatVictoryPending = false; // 이번 전투 보상 예약 소비
                _runState.Flow.EnterReward(); // 지도 배경 유지 Reward 흐름 진입
            }

            if (_rewardCatalog == null)
            {
                Debug.LogError($"75일차 카드 보상 실패: Resources/{RewardCatalogResourceName}를 찾지 못했습니다."); // 리소스 누락 기록
                CompleteRewardWithoutCard(source); // 보상 없이 흐름 복귀
                return; // 후보 생성 중단
            }

            int safeRewardStage = Mathf.Clamp(rewardStage, RoundState.FirstRound, RoundState.FinalRound); // 보상 계산 Stage 범위 보정
            CardRewardProfile profile = CardRewardQualityRules.GetProfile(safeRewardStage, source, rewardProfileId); // Stage·Source·ProfileId 기반 품질 계산
            int seed = CreateRewardSeed(source, safeRewardStage); // 현재 런 상태 기반 재현 가능한 시드 생성
            var candidates = CardRewardGenerator.Generate(_rewardCatalog.Cards, _runState.Deck.OwnedCardPool, CandidateCount, seed, profile); // 품질 가중 카드 3택 후보 생성

            if (candidates.Count == 0)
            {
                Debug.LogWarning("75일차 카드 보상 후보 없음: 현재 획득 가능한 카드가 없어 보상을 건너뜁니다."); // 보상 풀 소진 기록
                CompleteRewardWithoutCard(source); // 런 진행 유지
                return; // UI 표시 생략
            }

            _rewardState.Begin(candidates, source); // 현재 후보·발생 경로 저장
            _rewardUI.Show(_rewardState.Candidates, source, profile, HandleCardSelected); // Stage·품질 정보를 포함한 카드 3택 화면 표시
            Debug.Log($"75일차 카드 보상 시작: Source={source} / Stage={safeRewardStage} / Profile={rewardProfileId} / Quality={profile.Quality} / Weights={profile.OneStarWeight}:{profile.TwoStarWeight}:{profile.ThreeStarWeight} / Candidates={candidates.Count}"); // 품질 생성 결과 기록
        }

        private int CreateRewardSeed(CardRewardSource source, int rewardStage)
        {
            unchecked
            {
                int seed = rewardStage * 73856093; // 실제 보상 Stage 시드 반영
                seed ^= _runState.Deck.OwnedCardPool.Count * 19349663; // 현재 보유 카드 수 반영

                switch (source)
                {
                    case CardRewardSource.RewardNode:
                        seed ^= 83492791; // Reward 노드 경로 Salt
                        break;
                    case CardRewardSource.EliteVictory:
                        seed ^= 15485863; // Elite 전투 보상 Salt
                        break;
                    case CardRewardSource.MidBossVictory:
                        seed ^= 32452843; // Mid Boss 전투 보상 Salt
                        break;
                    default:
                        seed ^= 297121507; // 일반 전투 보상 Salt
                        break;
                }

                return seed; // 최종 보상 시드 반환
            }
        }

        private StageDefinition ResolveCompletedBattleDefinition()
        {
            StageBattleRuntimeController runtime = Object.FindFirstObjectByType<StageBattleRuntimeController>(); // 아직 살아 있는 완료 전투 런타임 조회
            if (runtime != null && runtime.StageDefinition != null) return runtime.StageDefinition; // 실제 전투 정의 우선 반환
            return ResolveCurrentStageDefinition(); // 런타임 누락 시 현재 지도 노드 정의 fallback
        }

        private StageDefinition ResolveCurrentStageDefinition()
        {
            if (_runState == null || _runState.RouteMap == null) return null; // 지도 상태 누락 방어
            StageNode currentNode = _runState.RouteMap.CurrentNode; // 현재 지도 노드 조회
            if (currentNode == null) return null; // 현재 노드 누락 반환
            return StageDefinitionCatalog.Resolve(currentNode.StageDefinitionId, currentNode.Depth); // 현재 노드 StageDefinition 반환
        }

        private static CardRewardSource ResolveBattleRewardSource(StageType stageType)
        {
            if (stageType == StageType.Elite) return CardRewardSource.EliteVictory; // Elite 전용 보상 경로
            if (stageType == StageType.MidBoss) return CardRewardSource.MidBossVictory; // Mid Boss 전용 보상 경로
            return CardRewardSource.BattleVictory; // 일반 전투 기본 보상 경로
        }

        private static int ResolveRewardStage(StageDefinition definition, int fallbackStage)
        {
            int safeFallback = Mathf.Clamp(fallbackStage, RoundState.FirstRound, RoundState.FinalRound); // 현재 런 Stage fallback 보정
            if (definition == null || string.IsNullOrWhiteSpace(definition.StageId)) return safeFallback; // StageDefinition 누락 fallback

            string stageId = definition.StageId; // 안정적인 stage_N_type ID 조회
            int firstSeparator = stageId.IndexOf('_'); // 첫 구분자 탐색
            if (firstSeparator < 0) return safeFallback; // 잘못된 StageId fallback
            int secondSeparator = stageId.IndexOf('_', firstSeparator + 1); // Stage 숫자 뒤 구분자 탐색
            if (secondSeparator <= firstSeparator + 1) return safeFallback; // 숫자 토큰 누락 fallback

            string stageToken = stageId.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1); // Stage 숫자 토큰 추출
            if (!int.TryParse(stageToken, out int parsedStage)) return safeFallback; // 숫자 변환 실패 fallback
            return Mathf.Clamp(parsedStage, RoundState.FirstRound, RoundState.FinalRound); // StageDefinition 기준 보상 Stage 반환
        }

        private void HandleCardSelected(PieceDefinition definition)
        {
            if (!_rewardState.IsActive || !_rewardState.Contains(definition)) return; // 현재 후보 외 선택 차단

            if (!CardRewardRules.TryAddOwnedCard(_runState.Deck, definition))
            {
                Debug.LogWarning($"75일차 카드 보상 획득 차단: {definition.DisplayName} / 동일 카드 보유 상한 도달"); // 획득 실패 기록
                return; // 보상 화면 유지
            }

            if (!_rewardState.TrySelect(definition)) return; // 한 보상 두 번째 선택 차단
            CardRewardSource source = _rewardState.Source; // 상태 초기화 전 발생 경로 저장
            _rewardUI.Hide(); // 보상 화면 종료
            Debug.Log($"75일차 카드 보상 획득: {definition.DisplayName} / Grade={definition.Grade} / Owned={_runState.Deck.OwnedCardPool.Count}"); // 획득 결과 기록
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
                Debug.LogWarning($"75일차 카드 보상 완료 거부: Source={source} / Flow={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound}"); // 진행 상태 불일치 기록
                return; // 잘못된 상태 변경 차단
            }

            ResetPendingBattleReward(); // 다음 전투 보상 대기 정보 초기화
            if (_runState.CurrentFlowPhase == RunFlowPhase.Map) _routeMapBoardController.RefreshMapVisuals(); // 지도 복귀 시 화면 갱신
            Debug.Log($"75일차 카드 보상 완료 -> {_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound} / Source={source}"); // 완료 결과 기록
        }

        private void CompleteRewardWithoutCard(CardRewardSource source)
        {
            _rewardState.Clear(); // 남은 임시 상태 제거

            bool completed = source == CardRewardSource.RewardNode
                ? RunStageFlowService.CompleteNonBattleStage(_runState)
                : RunStageFlowService.CompleteBattleReward(_runState); // 카드 획득 여부와 무관하게 동일 진행 규칙 적용

            if (!completed)
            {
                Debug.LogWarning($"75일차 카드 없는 보상 완료 거부: Source={source} / Flow={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound}"); // 진행 상태 불일치 기록
                return; // 잘못된 상태 변경 차단
            }

            ResetPendingBattleReward(); // 다음 전투 보상 대기 정보 초기화
            if (_runState.CurrentFlowPhase == RunFlowPhase.Map) _routeMapBoardController.RefreshMapVisuals(); // 정상 지도 복귀 시 화면 갱신
        }

        private void ResetPendingBattleReward()
        {
            _combatVictoryPending = false; // 전투 보상 대기 해제
            _pendingBattleRewardSource = CardRewardSource.BattleVictory; // 기본 일반 전투 보상 경로 복구
            _pendingRewardStage = RoundState.FirstRound; // 기본 Stage 복구
            _pendingRewardProfileId = string.Empty; // 보상 프로필 ID 제거
        }

        private void OnDestroy()
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 전투 종료 이벤트 해제
            if (_rewardUI != null) _rewardUI.Hide(); // 남은 보상 UI 숨김
        }
    }
}
