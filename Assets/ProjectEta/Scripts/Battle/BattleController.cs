using System.Collections; // Coroutine·IEnumerator 사용
using UnityEngine; // MonoBehaviour·GameObject·Debug 사용
using UnityEngine.InputSystem; // Space 키 입력 사용
using UnityEngine.SceneManagement; // 현재 씬 이름 확인
using ProjectEta.Board; // BoardView·BoardInputController 사용
using ProjectEta.Pieces; // PieceMovementType 사용
using ProjectEta.Run; // RunState·RunSaveSystem 사용
using ProjectEta.UI; // 전투 UI 사용

namespace ProjectEta.Battle
{
    public class BattleController : MonoBehaviour
    {
        [SerializeField] private int _startingKingHp = 3; // 새 테스트 런 시작 킹 체력
        [SerializeField] private float _dummyEnemyTurnDelay = 0.5f; // 임시 적 턴 유지 시간
        [SerializeField] private int _turnLimitTestValue = 30; // 일반 라운드 턴 제한 테스트 값
        [SerializeField] private Vector2Int _testEnemySpawnPosition = new Vector2Int(4, 8); // 테스트 적 부대 기준 좌표
        [SerializeField] private BoardView _boardView; // 실제 RunState.Board 보드 뷰
        [SerializeField] private BoardInputController _boardInputController; // 실제 RunState 입력 컨트롤러

        public RunState RunState => _runState; // 현재 전투 런 상태
        public TurnManager TurnManager => _turnManager; // 현재 턴 매니저
        public BattleHooks BattleHooks => _battleHooks; // 현재 전투 훅 버스
        public TurnStatusUI TurnStatusUI => _turnStatusUI; // 턴 상태 UI
        public HandUI HandUI => _handUI; // 손패 UI
        public DeckPanelUI DeckPanelUI => _deckPanelUI; // 덱·무덤 UI
        public FusionPanelUI FusionPanelUI => _fusionPanelUI; // 합성 UI
        public PieceInfoPanelUI PieceInfoPanelUI => _pieceInfoPanelUI; // 기물 정보 UI
        public CombatLogUI CombatLogUI => _combatLogUI; // 전투 로그 UI
        public DeploymentTurnBannerUI DeploymentTurnBannerUI => _deploymentTurnBannerUI; // 배치 턴 배너 UI

        private RunState _runState; // 전체 런 상태
        private TurnManager _turnManager; // 플레이어·적·배치 턴 상태
        private BattleHooks _battleHooks; // 전투 훅 버스
        private TurnStatusUI _turnStatusUI; // 턴 상태 UI 참조
        private HandUI _handUI; // 손패 UI 참조
        private DeckPanelUI _deckPanelUI; // 덱 패널 UI 참조
        private FusionPanelUI _fusionPanelUI; // 합성 UI 참조
        private PieceInfoPanelUI _pieceInfoPanelUI; // 기물 정보 UI 참조
        private CombatLogUI _combatLogUI; // 전투 로그 UI 참조
        private DeploymentTurnBannerUI _deploymentTurnBannerUI; // 배치 턴 배너 UI 참조
        private Coroutine _dummyEnemyTurnCoroutine; // 임시 적 턴 코루틴

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle")
            {
                return; // Battle 씬 외 생성 차단
            }

            if (Object.FindFirstObjectByType<BattleController>() != null)
            {
                return; // 중복 BattleController 생성 차단
            }

            var controllerObject = new GameObject("BattleController"); // 전투 상태 호스트 생성
            controllerObject.AddComponent<BattleController>(); // BattleController 자동 추가
        }

        private void Awake()
        {
            ResolveReferences(); // Battle 씬 핵심 참조 탐색
            EnsureTurnSystems(); // 턴·UI 시스템 준비

            if (_runState == null && RunSaveSystem.TryLoadSafe(out RunState restoredRun))
            {
                _runState = restoredRun; // 51일차 안전 지점 런 자동 복원
                BindState(); // 복원 RunState를 보드·입력·UI에 연결
                _boardInputController?.EnsurePrototypeEnemyStartingHand(); // 이후 전투용 적 프로토타입 카드 준비
                Debug.Log($"51일차 런 자동 복원: Phase={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound} / Node={_runState.RouteMap.CurrentNodeId}"); // 복원 결과 출력
                return; // 신규 런·테스트 적 생성 차단
            }

            if (_runState == null)
            {
                _runState = new RunState(_startingKingHp); // 저장이 없으면 새 테스트 런 생성
                BindState(); // 새 RunState 연결

                if (_boardInputController != null)
                {
                    _boardInputController.EnsurePrototypeStartingHand(); // 시작 플레이어 덱·손패 구성
                    _boardInputController.EnsurePrototypeEnemyStartingHand(); // 시작 적 덱·손패 구성
                    _boardInputController.SpawnTestEnemySquad(_testEnemySpawnPosition); // 기존 테스트 적 부대 생성
                }

                return; // 신규 런 초기화 완료
            }

            BindState(); // 외부에서 전달된 기존 RunState 연결
            _boardInputController?.EnsurePrototypeEnemyStartingHand(); // 기존 런 적 카드 준비
        }

        private void Update()
        {
            if (_turnManager == null || Keyboard.current == null)
            {
                return; // 턴 매니저·키보드 누락 시 입력 처리 차단
            }

            if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return; // Space 입력 없는 프레임 종료
            }

            if (_turnManager.CurrentState == TurnState.DeploymentTurn)
            {
                if (_turnManager.TryEndDeploymentTurn())
                {
                    Debug.Log($"배치 턴 종료 -> {_turnManager.TurnNumber}턴 PlayerTurn"); // 배치 턴 종료 결과 출력
                }
                else if (_turnManager.IsInitialDeployment && !_turnManager.IsInitialKingPlaced)
                {
                    Debug.Log("배치 턴을 종료할 수 없습니다. 먼저 킹을 아군 영역에 반드시 배치하세요."); // 시작 킹 필수 조건 안내
                }

                return; // 배치 턴 입력 처리 종료
            }

            TryCompletePlayerAction(); // 일반 행동 완료 입력 처리
        }

        public void Initialize(RunState runState)
        {
            if (runState == null)
            {
                Debug.LogError("BattleController.Initialize: RunState가 null입니다."); // 잘못된 초기화 원인 출력
                return; // 기존 상태 유지
            }

            _runState = runState; // 전달 RunState 적용
            ResolveReferences(); // 씬 참조 재탐색
            EnsureTurnSystems(); // 턴·UI 시스템 준비
            BindState(); // 상태 재연결
            _boardInputController?.EnsurePrototypeEnemyStartingHand(); // 적 프로토타입 손패 보장
        }

        public bool TryCompletePlayerAction()
        {
            if (_turnManager == null)
            {
                return false; // 턴 매니저 준비 전 행동 완료 차단
            }

            if (!_turnManager.TryCompletePlayerAction())
            {
                Debug.Log("플레이어 행동 완료 거부: 현재 플레이어 일반 행동 턴이 아닙니다."); // 행동 거부 사유 출력
                return false; // 행동 완료 실패 반환
            }

            Debug.Log($"Turn {_turnManager.TurnNumber}: Player action completed -> EnemyTurn"); // 적 턴 전환 결과 출력
            return true; // 행동 완료 성공 반환
        }

        public void EndBattle(BattleOutcome outcome = BattleOutcome.Defeat)
        {
            if (_dummyEnemyTurnCoroutine != null)
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 전투 종료 시 더미 적 턴 중단
                _dummyEnemyTurnCoroutine = null; // 코루틴 참조 초기화
            }

            if (_turnManager == null) return; // 턴 매니저 누락 시 전투 결과 처리 차단

            _turnManager.EndBattle(outcome); // 먼저 BattleEnded 이벤트를 발행해 카드 보상 등 기존 구독자에게 결과 전달
            RunStageFlowService.CompleteBattle(_runState, outcome); // TurnManager 결과를 RunState Map·Completed·Failed 흐름과 동기화
        }

        private void ResolveReferences()
        {
            if (_boardView == null)
            {
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 씬 BoardView 자동 탐색
            }

            if (_boardInputController == null)
            {
                _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 씬 BoardInputController 자동 탐색
            }
        }

        private void EnsureTurnSystems()
        {
            if (_turnManager == null)
            {
                _turnManager = new TurnManager(); // 시작 배치 상태 턴 매니저 생성
            }

            if (_battleHooks == null)
            {
                _battleHooks = new BattleHooks(); // 현재 전투 훅 버스 생성
            }

            if (_turnStatusUI == null)
            {
                _turnStatusUI = GetComponent<TurnStatusUI>(); // 기존 턴 상태 UI 탐색
            }

            if (_turnStatusUI == null)
            {
                _turnStatusUI = gameObject.AddComponent<TurnStatusUI>(); // 턴 상태 UI 자동 추가
            }

            _turnStatusUI.Bind(_turnManager); // 현재 턴 매니저 UI 연결

            if (_deploymentTurnBannerUI == null)
            {
                _deploymentTurnBannerUI = GetComponent<DeploymentTurnBannerUI>(); // 기존 배치 턴 배너 탐색
            }

            if (_deploymentTurnBannerUI == null)
            {
                _deploymentTurnBannerUI = gameObject.AddComponent<DeploymentTurnBannerUI>(); // 배치 턴 배너 자동 추가
            }

            _deploymentTurnBannerUI.Bind(_turnManager); // 배치 턴 배너 턴 매니저 연결
            _turnManager.TurnChanged -= HandleTurnChanged; // 중복 턴 이벤트 구독 제거
            _turnManager.TurnChanged += HandleTurnChanged; // 턴 변경 후속 처리 구독
        }

        private void EnsureHandUI()
        {
            if (_handUI == null)
            {
                _handUI = GetComponent<HandUI>(); // 기존 HandUI 탐색
            }

            if (_handUI == null)
            {
                _handUI = gameObject.AddComponent<HandUI>(); // HandUI 자동 추가
            }

            _handUI.Bind(_boardInputController); // 실제 손패 입력 상태 연결
        }

        private void EnsureDeckPanelUI()
        {
            if (_deckPanelUI == null)
            {
                _deckPanelUI = GetComponent<DeckPanelUI>(); // 기존 DeckPanelUI 탐색
            }

            if (_deckPanelUI == null)
            {
                _deckPanelUI = gameObject.AddComponent<DeckPanelUI>(); // DeckPanelUI 자동 추가
            }

            _deckPanelUI.Bind(_boardInputController); // 실제 덱 상태 연결
        }

        private void EnsureFusionPanelUI()
        {
            if (_fusionPanelUI == null)
            {
                _fusionPanelUI = GetComponent<FusionPanelUI>(); // 기존 FusionPanelUI 탐색
            }

            if (_fusionPanelUI == null)
            {
                _fusionPanelUI = gameObject.AddComponent<FusionPanelUI>(); // FusionPanelUI 자동 추가
            }

            _fusionPanelUI.Bind(_boardInputController); // 실제 합성 상태 연결
        }

        private void EnsurePieceInfoPanelUI()
        {
            if (_pieceInfoPanelUI == null)
            {
                _pieceInfoPanelUI = GetComponent<PieceInfoPanelUI>(); // 기존 PieceInfoPanelUI 탐색
            }

            if (_pieceInfoPanelUI == null)
            {
                _pieceInfoPanelUI = gameObject.AddComponent<PieceInfoPanelUI>(); // PieceInfoPanelUI 자동 추가
            }

            _pieceInfoPanelUI.Bind(_boardInputController); // 실제 기물 선택 상태 연결
        }

        private void EnsureCombatLogUI()
        {
            if (_combatLogUI == null)
            {
                _combatLogUI = GetComponent<CombatLogUI>(); // 기존 CombatLogUI 탐색
            }

            if (_combatLogUI == null)
            {
                _combatLogUI = gameObject.AddComponent<CombatLogUI>(); // CombatLogUI 자동 추가
            }

            _combatLogUI.Bind(_boardInputController); // 실제 전투 훅 연결
        }

        private void HandleTurnChanged(TurnState state, int turnNumber)
        {
            if (state == TurnState.PlayerTurn)
            {
                _battleHooks?.RaiseTurnStart(state, turnNumber); // 새 일반 턴 시작 훅 발행
            }

            if (state == TurnState.EnemyTurn)
            {
                if (_boardInputController != null && _boardInputController.TryEnemySummonOneCard())
                {
                    Debug.Log("EnemyTurn: 카드 1장 소환 행동 완료 -> 즉시 다음 턴"); // 적 카드 소환 결과 출력
                    return; // 내부 턴 완료 후 추가 진행 차단
                }

                StartDummyEnemyTurn(); // 소환 불가 시 더미 적 턴 시작
                return; // 적 턴 처리 종료
            }

            if (state == TurnState.DeploymentTurn && _turnManager.IsInitialDeployment && !_turnManager.IsInitialKingPlaced)
            {
                Debug.Log("시작 배치 턴: 먼저 킹을 배치하세요. 킹 배치 후에도 원하는 카드를 계속 배치할 수 있으며 Space로 턴을 종료합니다."); // 시작 배치 안내
                return; // 초기 배치 후속 처리 종료
            }

            if (state == TurnState.DeploymentTurn && _turnManager.IsInitialDeployment)
            {
                Debug.Log($"시작 배치 턴 계속: 현재 {_turnManager.DeployedCardCount}장 배치 / 자유 배치 후 Space로 턴 종료"); // 자유 배치 안내
                return; // 초기 배치 유지
            }

            if (state == TurnState.DeploymentTurn)
            {
                Debug.Log($"{turnNumber}턴 종료 - 배치 턴 시작: 원하는 만큼 자유롭게 배치한 뒤 Space로 배치 턴을 종료하세요."); // 주기 배치 안내
                return; // 배치 턴 처리 종료
            }

            if (state == TurnState.PlayerTurn && turnNumber > _turnLimitTestValue)
            {
                Debug.Log($"라운드 턴 제한({_turnLimitTestValue}턴) 초과 - 패배, 전투를 종료합니다."); // 턴 제한 패배 출력
                EndBattle(BattleOutcome.Defeat); // 턴 제한 패배 처리
            }
        }

        private void BindState()
        {
            if (_runState == null)
            {
                Debug.LogError("BattleController.BindState: RunState가 없습니다."); // 런 상태 누락 오류 출력
                return; // 연결 중단
            }

            if (_boardView == null || _boardInputController == null)
            {
                Debug.LogError("BattleController.BindState: BoardView 또는 BoardInputController를 찾지 못했습니다."); // 핵심 씬 참조 누락 오류
                return; // 부분 연결 차단
            }

            _boardView.Bind(_runState.Board); // 보드 뷰 실제 RunState.Board 연결
            _boardInputController.Bind(_runState, _boardView, _turnManager, _battleHooks); // 보드 입력 전체 상태 연결
            EnsureHandUI(); // 손패 UI 연결
            EnsureDeckPanelUI(); // 덱 패널 UI 연결
            EnsureFusionPanelUI(); // 합성 UI 연결
            EnsurePieceInfoPanelUI(); // 기물 정보 UI 연결
            EnsureCombatLogUI(); // 전투 로그 UI 연결

            _boardInputController.AttackResolved -= HandleAttackResolved; // 중복 공격 결과 구독 제거
            _boardInputController.AttackResolved += HandleAttackResolved; // 킹 HP·승패 판정 구독

            Debug.Log($"Battle state bound: Board={_boardView.IsBound}, Hand={_runState.Hand.Hand.Count}장, KingHP={_runState.KingHp}, Turn={_turnManager.TurnNumber}/{_turnManager.CurrentState}"); // 연결 결과 출력
        }

        private void HandleAttackResolved(CombatResult result)
        {
            var defender = result.Defender; // 이번 공격 방어자 조회

            if (defender.IsPlayerPiece && defender.Definition.MovementType == PieceMovementType.King)
            {
                _runState.KingHp = defender.CurrentHp; // 보드 킹 실제 HP를 RunState와 동기화
                Debug.Log($"킹 피격: 남은 KingHP={_runState.KingHp}"); // 킹 피격 결과 출력

                if (_runState.IsDefeated)
                {
                    Debug.Log("킹 HP 0 - 런 패배, 전투를 종료합니다."); // 런 패배 사유 출력
                    EndBattle(BattleOutcome.Defeat); // 패배 전투 종료
                    return; // 같은 결과 승리 판정 차단
                }
            }

            if (result.DefenderDied && !defender.IsPlayerPiece)
            {
                int remainingEnemies = _runState.Board.CountPieces(isPlayerPiece: false); // 남은 적 기물 수 조회
                Debug.Log($"적 처치: 남은 적 {remainingEnemies}기"); // 처치 결과 출력

                if (remainingEnemies == 0)
                {
                    Debug.Log("적 전멸 - 승리, 전투를 종료합니다."); // 전투 승리 사유 출력
                    _boardInputController?.ReturnDeadPileToOwnedPool(); // 죽은 아군 카드 소유 풀 복귀
                    EndBattle(BattleOutcome.Victory); // 승리 전투 종료
                }
            }
        }

        private void OnDestroy()
        {
            if (_boardInputController != null)
            {
                _boardInputController.AttackResolved -= HandleAttackResolved; // 공격 결과 이벤트 구독 정리
            }

            if (_turnManager != null)
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 턴 변경 이벤트 구독 정리
            }
        }

        private void StartDummyEnemyTurn()
        {
            if (_dummyEnemyTurnCoroutine != null)
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 이전 더미 적 턴 중복 방지
            }

            _dummyEnemyTurnCoroutine = StartCoroutine(CompleteDummyEnemyTurnAfterDelay()); // 지연 적 턴 종료 시작
        }

        private IEnumerator CompleteDummyEnemyTurnAfterDelay()
        {
            yield return new WaitForSeconds(_dummyEnemyTurnDelay); // 적 턴 표시 시간 대기

            if (_turnManager != null && _turnManager.CompleteEnemyTurn())
            {
                Debug.Log($"Enemy turn completed -> {_turnManager.CurrentState} / Turn {_turnManager.TurnNumber}"); // 턴 전환 결과 출력
                _battleHooks?.RaiseTurnEnd(_turnManager.CurrentState, _turnManager.TurnNumber); // 플레이어+적 턴 종료 훅 발행
            }

            _dummyEnemyTurnCoroutine = null; // 코루틴 완료 참조 초기화
        }

        private void OnDisable()
        {
            if (_dummyEnemyTurnCoroutine != null)
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 비활성화 시 더미 적 턴 중단
                _dummyEnemyTurnCoroutine = null; // 코루틴 참조 초기화
            }
        }
    }
}
