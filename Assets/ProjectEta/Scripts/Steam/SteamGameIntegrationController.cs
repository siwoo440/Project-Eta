using System.Collections; // Run 완료 지연 확인 Coroutine 사용
using System.Collections.Generic; // Achievement 큐와 카드 Snapshot 사용
using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // 씬 전환 통합 처리
using ProjectEta.Battle; // BattleController·TurnManager·BattleOutcome 사용
using ProjectEta.Pieces; // PieceDefinition·PieceGrade 사용
using ProjectEta.Run; // RunState·RunFlowPhase 사용

namespace ProjectEta.Steam
{
    [DefaultExecutionOrder(2000)]
    [DisallowMultipleComponent]
    public sealed class SteamGameIntegrationController : MonoBehaviour
    {
        private static SteamGameIntegrationController instance; // Steam 게임 통합 단일 인스턴스

        private readonly HashSet<string> pendingAchievementIds = new HashSet<string>(); // Steam 준비 전 대기 Achievement ID
        private readonly HashSet<string> completedAchievementIds = new HashSet<string>(); // 현재 실행 중 처리 완료 Achievement ID
        private readonly List<string> flushBuffer = new List<string>(); // Achievement 큐 순회 버퍼
        private readonly Dictionary<PieceDefinition, int> previousOwnedCounts = new Dictionary<PieceDefinition, int>(); // 이전 보유 카드 개수 Snapshot
        private readonly Dictionary<PieceDefinition, int> currentOwnedCounts = new Dictionary<PieceDefinition, int>(); // 현재 보유 카드 개수 Snapshot

        private BattleController battleController; // 현재 BattleController
        private TurnManager turnManager; // 현재 TurnManager
        private RunState runState; // 현재 RunState
        private int previousOwnedTotal; // 이전 보유 카드 총수
        private bool hasOwnedSnapshot; // 보유 카드 Snapshot 준비 상태
        private Coroutine postBattleCoroutine; // 전투 후 Run 완료 확인 Coroutine

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate() // Steam 게임 통합 Controller 자동 생성
        {
            if (FindFirstObjectByType<SteamGameIntegrationController>() != null)
            {
                return;
            }

            GameObject host = new GameObject(nameof(SteamGameIntegrationController)); // Steam 게임 통합 호스트 생성
            host.AddComponent<SteamGameIntegrationController>(); // Steam 게임 통합 Controller 연결
        }

        private void Awake() // Steam 게임 통합 초기화
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject); // 중복 통합 Controller 제거
                return;
            }

            instance = this; // 통합 Controller 인스턴스 등록
            DontDestroyOnLoad(gameObject); // 전체 게임 씬에서 Steam 통합 유지
            SceneManager.sceneLoaded += HandleSceneLoaded; // 씬 전환 후 통합 갱신 연결
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); // 최초 씬 통합 즉시 적용
        }

        private void Update() // Steam 게임 이벤트 감시 및 대기 Achievement 처리
        {
            EnsureBattleBinding(); // 현재 전투 상태 연결
            RefreshOwnedCardSnapshot(); // 합성·5성 획득 상태 감시
            FlushPendingAchievements(); // Steam 준비 완료 Achievement 처리
        }

        private void OnDestroy() // Steam 게임 통합 종료
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded; // 씬 전환 이벤트 해제
            UnbindBattle(); // 전투 이벤트 연결 해제
            instance = null; // 통합 Controller 인스턴스 해제
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) // 씬별 Steam 통합 갱신
        {
            UnbindBattle(); // 이전 씬 전투 연결 정리
            ResetOwnedCardSnapshot(); // 이전 런 카드 Snapshot 정리

            if (scene.name == "MainMenu")
            {
                EnsureMainMenuStatusUI(); // MainMenu Steam 상태 UI 보장
            }
        }

        private void EnsureMainMenuStatusUI() // MainMenu Steam 상태 UI 자동 생성
        {
            if (FindFirstObjectByType<SteamMainMenuStatusUI>() != null)
            {
                return;
            }

            GameObject host = new GameObject(nameof(SteamMainMenuStatusUI)); // MainMenu Steam UI 호스트 생성
            host.AddComponent<SteamMainMenuStatusUI>(); // Steam 상태 UI 연결
        }

        private void EnsureBattleBinding() // 현재 BattleController와 TurnManager 연결
        {
            if (battleController != null
                && turnManager != null
                && runState != null
                && battleController.TurnManager == turnManager
                && battleController.RunState == runState)
            {
                return;
            }

            BattleController foundController = FindFirstObjectByType<BattleController>(); // 현재 씬 BattleController 탐색
            if (foundController == null || foundController.TurnManager == null || foundController.RunState == null)
            {
                return;
            }

            UnbindBattle(); // 이전 전투 이벤트 연결 해제
            battleController = foundController; // 현재 BattleController 저장
            turnManager = foundController.TurnManager; // 현재 TurnManager 저장
            runState = foundController.RunState; // 현재 RunState 저장
            turnManager.TurnChanged += HandleTurnChanged; // 전투 종료 이벤트 감시 연결
            ResetOwnedCardSnapshot(); // 새 Run 보유 카드 Snapshot 초기화
        }

        private void UnbindBattle() // 현재 전투 이벤트 연결 해제
        {
            if (turnManager != null)
            {
                turnManager.TurnChanged -= HandleTurnChanged; // TurnManager 이벤트 구독 해제
            }

            if (postBattleCoroutine != null)
            {
                StopCoroutine(postBattleCoroutine); // 이전 전투 후속 확인 중단
                postBattleCoroutine = null; // Coroutine 참조 초기화
            }

            battleController = null; // BattleController 참조 초기화
            turnManager = null; // TurnManager 참조 초기화
            runState = null; // RunState 참조 초기화
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 전투 상태 변화 Achievement 연결
        {
            if (state != TurnState.BattleEnded
                || turnManager == null
                || runState == null
                || turnManager.Outcome != BattleOutcome.Victory)
            {
                return;
            }

            int completedRound = runState.CurrentRound; // 흐름 전환 전 완료 Stage 번호 보존
            SteamGameEventBridge.QueueBattleVictory(completedRound, QueueAchievement); // 승리·중간 보스 Achievement 큐 등록

            if (postBattleCoroutine != null)
            {
                StopCoroutine(postBattleCoroutine); // 중복 Run 완료 확인 제거
            }

            postBattleCoroutine = StartCoroutine(QueueRunClearAfterFlow(runState)); // Run 흐름 갱신 후 최종 클리어 확인
        }

        private IEnumerator QueueRunClearAfterFlow(RunState completedRun) // 전투 후 Run 완료 Achievement 확인
        {
            yield return null; // BattleController의 RunStageFlowService 처리 완료 대기

            if (completedRun != null && completedRun.CurrentFlowPhase == RunFlowPhase.Completed)
            {
                SteamGameEventBridge.QueueRunCompleted(QueueAchievement); // 최종 Run 클리어 Achievement 큐 등록
            }

            postBattleCoroutine = null; // Run 완료 확인 Coroutine 참조 정리
        }

        private void RefreshOwnedCardSnapshot() // 보유 카드 변화에서 합성·5성 획득 감지
        {
            if (runState == null || runState.Deck == null || runState.Deck.OwnedCardPool == null)
            {
                return;
            }

            currentOwnedCounts.Clear(); // 현재 카드 Snapshot 버퍼 초기화
            int currentOwnedTotal = 0; // 현재 보유 카드 총수 초기화
            bool hasFiveStar = false; // 현재 5성 카드 보유 상태 초기화

            for (int i = 0; i < runState.Deck.OwnedCardPool.Count; i++)
            {
                PieceDefinition card = runState.Deck.OwnedCardPool[i]; // 현재 보유 카드 조회
                if (card == null)
                {
                    continue;
                }

                currentOwnedTotal++; // 유효 보유 카드 총수 증가
                hasFiveStar |= card.Grade == PieceGrade.FiveStar; // 5성 카드 보유 여부 누적

                if (!currentOwnedCounts.TryGetValue(card, out int count))
                {
                    count = 0; // 첫 카드 개수 기본값
                }

                currentOwnedCounts[card] = count + 1; // 카드 정의별 보유 개수 기록
            }

            if (hasFiveStar)
            {
                SteamGameEventBridge.QueueCardAcquired(PieceGrade.FiveStar, QueueAchievement); // 모든 획득 경로의 5성 Achievement 보장
            }

            if (hasOwnedSnapshot)
            {
                bool hasAddedCard = false; // 이전 Snapshot 대비 새 카드 존재 여부
                PieceGrade addedGrade = PieceGrade.OneStar; // 합성 결과 후보 등급 기본값

                foreach (KeyValuePair<PieceDefinition, int> pair in currentOwnedCounts)
                {
                    previousOwnedCounts.TryGetValue(pair.Key, out int previousCount); // 이전 동일 카드 보유 개수 조회
                    if (pair.Value <= previousCount)
                    {
                        continue;
                    }

                    hasAddedCard = true; // 새로 증가한 카드 존재 기록

                    if ((int)pair.Key.Grade > (int)addedGrade)
                    {
                        addedGrade = pair.Key.Grade; // 증가 카드 중 가장 높은 등급 보존
                    }
                }

                if (SteamGameEventBridge.IsFusionPoolDelta(previousOwnedTotal, currentOwnedTotal, hasAddedCard))
                {
                    SteamGameEventBridge.QueueFusionCompleted(addedGrade, QueueAchievement); // 재료 2→결과 1 변화 기반 합성 Achievement 등록
                }
            }

            previousOwnedCounts.Clear(); // 이전 Snapshot 저장소 초기화

            foreach (KeyValuePair<PieceDefinition, int> pair in currentOwnedCounts)
            {
                previousOwnedCounts[pair.Key] = pair.Value; // 현재 카드 개수를 다음 프레임 기준으로 저장
            }

            previousOwnedTotal = currentOwnedTotal; // 현재 보유 총수를 다음 프레임 기준으로 저장
            hasOwnedSnapshot = true; // 카드 Snapshot 준비 완료 표시
        }

        private void ResetOwnedCardSnapshot() // 카드 Snapshot 상태 초기화
        {
            previousOwnedCounts.Clear(); // 이전 카드 개수 제거
            currentOwnedCounts.Clear(); // 현재 카드 개수 제거
            previousOwnedTotal = 0; // 이전 카드 총수 초기화
            hasOwnedSnapshot = false; // Snapshot 미준비 상태 복원
        }

        private void QueueAchievement(string apiName) // Achievement ID 안전 큐 등록
        {
            if (string.IsNullOrWhiteSpace(apiName) || completedAchievementIds.Contains(apiName))
            {
                return;
            }

            pendingAchievementIds.Add(apiName); // 중복 없는 대기 Achievement 등록
        }

        private void FlushPendingAchievements() // 대기 Achievement Steam 해금 처리
        {
            if (!SteamPlatform.IsAchievementEnabled || pendingAchievementIds.Count == 0)
            {
                return;
            }

            flushBuffer.Clear(); // 순회 버퍼 초기화

            foreach (string apiName in pendingAchievementIds)
            {
                flushBuffer.Add(apiName); // 수정 안전한 순회 목록 복사
            }

            for (int i = 0; i < flushBuffer.Count; i++)
            {
                string apiName = flushBuffer[i]; // 현재 Achievement ID 조회
                if (!SteamPlatform.UnlockAchievement(apiName))
                {
                    continue;
                }

                pendingAchievementIds.Remove(apiName); // 성공 Achievement 대기 큐 제거
                completedAchievementIds.Add(apiName); // 현재 실행 중 완료 Achievement 등록
            }
        }
    }
}
