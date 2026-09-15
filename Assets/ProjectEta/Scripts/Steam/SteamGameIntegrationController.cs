using System.Collections; // Run 완료 지연 확인 Coroutine 사용
using System.Collections.Generic; // Achievement 큐 사용
using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // 씬 전환 통합 처리
using ProjectEta.Battle; // BattleController·TurnManager·BattleOutcome 사용
using ProjectEta.Board; // BoardInputController 합성 이벤트 사용
using ProjectEta.Cards; // DeckState 카드 획득 이벤트 사용
using ProjectEta.Fusion; // FusionRecipe 합성 결과 사용
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

        private BattleController battleController; // 현재 BattleController
        private BoardInputController boardInputController; // 현재 합성 입력 Controller
        private DeckState deckState; // 현재 플레이어 카드 보유 상태
        private TurnManager turnManager; // 현재 TurnManager
        private RunState runState; // 현재 RunState
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
            BattleController foundController = FindFirstObjectByType<BattleController>(); // 현재 씬 BattleController 탐색
            BoardInputController foundBoardInput = FindFirstObjectByType<BoardInputController>(); // 현재 씬 합성 입력 탐색
            if (foundController == null || foundController.TurnManager == null || foundController.RunState == null)
            {
                return;
            }

            if (battleController == foundController
                && boardInputController == foundBoardInput
                && deckState == foundController.RunState.Deck
                && turnManager == foundController.TurnManager
                && runState == foundController.RunState)
            {
                return;
            }

            UnbindBattle(); // 이전 전투 이벤트 연결 해제
            battleController = foundController; // 현재 BattleController 저장
            boardInputController = foundBoardInput; // 현재 합성 입력 저장
            deckState = foundController.RunState.Deck; // 현재 플레이어 카드 보유 상태 저장
            turnManager = foundController.TurnManager; // 현재 TurnManager 저장
            runState = foundController.RunState; // 현재 RunState 저장
            turnManager.TurnChanged += HandleTurnChanged; // 전투 종료 이벤트 감시 연결

            if (boardInputController != null)
            {
                boardInputController.FusionCompleted += HandleFusionCompleted; // 실제 합성 완료 이벤트 감시 연결
            }

            if (deckState != null)
            {
                deckState.CardAcquired += HandleCardAcquired; // 보상·상점·이벤트 카드 획득 감시 연결
                QueueExistingCardAchievements(deckState); // 복원된 5성 카드 Achievement 동기화
            }
        }

        private void UnbindBattle() // 현재 전투 이벤트 연결 해제
        {
            if (turnManager != null)
            {
                turnManager.TurnChanged -= HandleTurnChanged; // TurnManager 이벤트 구독 해제
            }

            if (boardInputController != null)
            {
                boardInputController.FusionCompleted -= HandleFusionCompleted; // 합성 완료 이벤트 구독 해제
            }

            if (deckState != null)
            {
                deckState.CardAcquired -= HandleCardAcquired; // 카드 획득 이벤트 구독 해제
            }

            if (postBattleCoroutine != null)
            {
                StopCoroutine(postBattleCoroutine); // 이전 전투 후속 확인 중단
                postBattleCoroutine = null; // Coroutine 참조 초기화
            }

            battleController = null; // BattleController 참조 초기화
            boardInputController = null; // 합성 입력 참조 초기화
            deckState = null; // 카드 보유 상태 참조 초기화
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

        private void HandleFusionCompleted(FusionRecipe recipe) // 실제 합성 성공 Achievement 연결
        {
            if (recipe == null || recipe.Result == null)
            {
                return;
            }

            SteamGameEventBridge.QueueFusionCompleted(recipe.Result.Grade, QueueAchievement); // 합성과 5성 Achievement 큐 등록
        }

        private void HandleCardAcquired(PieceDefinition card) // 실제 외부 카드 획득 Achievement 연결
        {
            if (card == null)
            {
                return;
            }

            SteamGameEventBridge.QueueCardAcquired(card.Grade, QueueAchievement); // 5성 카드 획득 Achievement 큐 등록
        }

        private void QueueExistingCardAchievements(DeckState currentDeck) // 복원 카드 Achievement 상태 동기화
        {
            if (currentDeck == null)
            {
                return;
            }

            if (!HasFiveStar(currentDeck.OwnedCardPool) && !HasFiveStar(currentDeck.DeadCardPile)) // 정상·사망 5성 보유 여부 확인
            {
                return;
            }

            SteamGameEventBridge.QueueCardAcquired(PieceGrade.FiveStar, QueueAchievement); // 복원된 5성 Achievement 큐 등록
        }

        private static bool HasFiveStar(IReadOnlyList<PieceDefinition> cards) // 카드 목록의 5성 보유 여부 확인
        {
            if (cards == null)
            {
                return false;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                PieceDefinition card = cards[i]; // 현재 복원 카드 조회
                if (card != null && card.Grade == PieceGrade.FiveStar)
                {
                    return true;
                }
            }

            return false;
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
