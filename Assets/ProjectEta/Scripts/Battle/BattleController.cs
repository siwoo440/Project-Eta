using System.Collections; // Coroutine과 IEnumerator를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Debug 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // Space 키 기반 일반 행동 완료·배치 턴 스킵 입력을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬 이름을 확인하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceMovementType을 사용해 킹 여부를 판별하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스
using ProjectEta.UI; // 18일차 이미지 손패 HandUI를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public class BattleController : MonoBehaviour // Battle 씬에서 단일 RunState와 턴 흐름을 소유하는 전투 진입점
    {
        [SerializeField] private int _startingKingHp = 3; // 새 테스트 런의 시작 킹 체력
        [SerializeField] private float _dummyEnemyTurnDelay = 0.5f; // 실제 AI 전까지 적 턴이 유지되는 테스트 시간
        [SerializeField] private int _turnLimitTestValue = 30; // 일반 라운드 턴 제한 테스트 값
        [SerializeField] private Vector2Int _testEnemySpawnPosition = new Vector2Int(4, 8); // 테스트용 적 부대 기준 배치 좌표
        [SerializeField] private BoardView _boardView; // 실제 RunState.Board를 표시할 보드 뷰
        [SerializeField] private BoardInputController _boardInputController; // 실제 RunState를 변경할 입력 컨트롤러

        public RunState RunState => _runState; // 현재 전투가 사용하는 단일 런 상태
        public TurnManager TurnManager => _turnManager; // 현재 전투가 사용하는 턴 매니저
        public TurnStatusUI TurnStatusUI => _turnStatusUI; // 화면 상단 중앙의 색상형 턴 상태 Canvas UI
        public HandUI HandUI => _handUI; // 화면 하단 중앙의 카드 이미지 손패 UI
        public DeckPanelUI DeckPanelUI => _deckPanelUI; // 19일차: 좌하단 뽑을 카드 덱 / 우하단 죽은 카드 덱 버튼·패널 UI
        public FusionPanelUI FusionPanelUI => _fusionPanelUI; // 21일차: 손패 위쪽 합성 버튼·재료 2장·결과 미리보기 패널 UI

        private RunState _runState; // 보드·손패·덱·킹 체력 등을 소유하는 단일 상태 객체
        private TurnManager _turnManager; // 플레이어/적/배치 턴과 행동 권한을 관리하는 상태 객체
        private TurnStatusUI _turnStatusUI; // 현재 턴을 상단 중앙에 표시하는 Canvas UI
        private HandUI _handUI; // 18일차: 실제 HandState를 카드 이미지와 드래그 Drop으로 표시하는 하단 UI
        private DeckPanelUI _deckPanelUI; // 19일차: 드로우 더미·죽은 카드 더미를 보여주는 좌우 버튼과 목록 패널
        private FusionPanelUI _fusionPanelUI; // 21일차: 합성 버튼과 재료·결과 미리보기 패널
        private Coroutine _dummyEnemyTurnCoroutine; // 임시 적 턴 자동 종료 코루틴 참조

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 씬 로드가 끝난 직후 자동 실행
        private static void AutoCreateForBattleScene() // Battle 씬에 컴포넌트를 직접 배치하지 않아도 전투 컨트롤러를 자동 생성하는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 씬이 Battle이 아니면
            {
                return; // 다른 씬에는 전투 컨트롤러를 만들지 않음
            }

            if (Object.FindFirstObjectByType<BattleController>() != null) // 이미 씬에 BattleController가 존재하면
            {
                return; // 중복 생성하지 않음
            }

            var controllerObject = new GameObject("BattleController"); // 전투 상태를 소유할 오브젝트 생성
            controllerObject.AddComponent<BattleController>(); // 컴포넌트 추가와 동시에 Awake에서 상태 연결 시작
        }

        private void Awake() // BattleController가 생성될 때 자동 호출되는 초기화 메서드
        {
            ResolveReferences(); // 기존 Battle 씬의 BoardView와 BoardInputController를 찾음
            EnsureTurnSystems(); // 턴 매니저와 상단 중앙 Canvas UI를 준비

            if (_runState == null) // 아직 외부에서 전달받은 런 상태가 없으면
            {
                _runState = new RunState(_startingKingHp); // 새 테스트 런을 하나만 생성
                BindState(); // RunState.Board/Hand와 TurnManager를 화면과 입력에 연결

                if (_boardInputController != null) // 입력 컨트롤러가 정상 연결됐으면
                {
                    _boardInputController.EnsurePrototypeStartingHand(); // 플레이어 실제 DeckState→HandState 시작 카드 흐름 구성
                    _boardInputController.EnsurePrototypeEnemyStartingHand(); // 적 턴마다 카드 1장을 실제 소비해 소환할 프로토타입 적 손패 구성
                    _boardInputController.SpawnTestEnemySquad(_testEnemySpawnPosition); // 기존 전투 테스트용 폰+룩 2기는 유지
                }
            }
            else // 외부 상태가 이미 있으면
            {
                BindState(); // 새로 만들지 않고 전달받은 상태를 그대로 연결
                _boardInputController?.EnsurePrototypeEnemyStartingHand(); // 로드된 런에서도 적 일반 턴 카드 소환용 손패를 준비
            }
        }

        private void Update() // 매 프레임 임시 Space 키 테스트 입력을 확인하는 메서드
        {
            if (_turnManager == null || Keyboard.current == null) // 턴 매니저나 키보드 입력이 없으면
            {
                return; // 처리하지 않고 종료
            }

            if (!Keyboard.current.spaceKey.wasPressedThisFrame) // 이번 프레임에 Space 키를 누르지 않았다면
            {
                return; // 별도 테스트 입력이 없으므로 종료
            }

            if (_turnManager.CurrentState == TurnState.DeploymentTurn) // 현재 배치 턴이면
            {
                if (_turnManager.TryEndDeploymentTurn()) // Space 키를 배치 턴 종료 입력으로 사용
                {
                    Debug.Log($"배치 턴 종료 -> {_turnManager.TurnNumber}턴 PlayerTurn"); // 다음 일반 턴 진입 결과 출력
                }
                else if (_turnManager.IsInitialDeployment && !_turnManager.IsInitialKingPlaced) // 시작 배치에서 킹이 아직 없다면
                {
                    Debug.Log("배치 턴을 종료할 수 없습니다. 먼저 킹을 아군 영역에 반드시 배치하세요."); // 필수 조건 안내
                }

                return; // 배치 턴에서는 일반 행동 완료를 호출하지 않음
            }

            TryCompletePlayerAction(); // 그 밖의 경우 기존 임시 일반 행동 완료 입력으로 처리
        }

        public void Initialize(RunState runState) // 세이브 로드나 다른 씬에서 기존 RunState를 넘길 때 사용할 진입점
        {
            if (runState == null) // 잘못된 상태를 전달하면
            {
                Debug.LogError("BattleController.Initialize: RunState가 null입니다."); // 원인을 콘솔에 표시
                return; // 기존 상태 유지
            }

            _runState = runState; // 전달받은 실제 런 상태를 사용
            ResolveReferences(); // 씬 참조를 다시 확보
            EnsureTurnSystems(); // 턴 매니저와 Canvas UI가 준비돼 있는지 확인
            BindState(); // 동일한 상태를 화면과 입력 양쪽에 연결
            _boardInputController?.EnsurePrototypeEnemyStartingHand(); // 세이브 로드 진입에서도 적 카드 손패를 중복 없이 준비
        }

        public bool TryCompletePlayerAction() // 플레이어 일반 행동 1회를 완료하고 적 턴을 시작하는 외부 진입점
        {
            if (_turnManager == null) // 턴 매니저가 아직 준비되지 않았다면
            {
                return false; // 행동 완료를 처리할 수 없으므로 실패 반환
            }

            if (!_turnManager.TryCompletePlayerAction()) // 현재 일반 행동 권한이 없어 턴 매니저가 거부했다면
            {
                Debug.Log("플레이어 행동 완료 거부: 현재 플레이어 일반 행동 턴이 아닙니다."); // 개발용 거부 사유 출력
                return false; // 중복 행동 또는 배치/적 턴임을 반환
            }

            Debug.Log($"Turn {_turnManager.TurnNumber}: Player action completed -> EnemyTurn"); // 플레이어 턴 종료 결과 출력
            return true; // 정상적으로 플레이어 행동을 완료했음을 반환
        }

        public void EndBattle(BattleOutcome outcome = BattleOutcome.Defeat) // 전투를 종료하는 진입점
        {
            if (_dummyEnemyTurnCoroutine != null) // 진행 중인 더미 적 턴이 있다면
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 전투 종료 후 다음 턴으로 넘어가지 않도록 코루틴 중단
                _dummyEnemyTurnCoroutine = null; // 코루틴 참조 초기화
            }

            _turnManager?.EndBattle(outcome); // 턴 상태를 전투 종료로 변경하고 결과를 기록
        }

        private void ResolveReferences() // 인스펙터 연결이 없어도 현재 Battle 씬에서 필요한 컴포넌트를 자동 탐색하는 메서드
        {
            if (_boardView == null) // 보드 뷰 참조가 없으면
            {
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 씬에서 첫 BoardView 탐색
            }

            if (_boardInputController == null) // 입력 컨트롤러 참조가 없으면
            {
                _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 씬에서 첫 BoardInputController 탐색
            }
        }

        private void EnsureTurnSystems() // 턴 상태와 Canvas 표시 시스템을 한 번만 준비하는 메서드
        {
            if (_turnManager == null) // 아직 턴 매니저가 없다면
            {
                _turnManager = new TurnManager(); // 전투 시작 킹 필수 자유 배치 턴 상태로 새 턴 매니저 생성
            }

            if (_turnStatusUI == null) // 턴 상태 UI 컴포넌트가 없다면
            {
                _turnStatusUI = GetComponent<TurnStatusUI>(); // 같은 오브젝트에 기존 UI 컴포넌트가 있는지 먼저 확인
            }

            if (_turnStatusUI == null) // 기존 컴포넌트도 없다면
            {
                _turnStatusUI = gameObject.AddComponent<TurnStatusUI>(); // 상단 중앙 색상형 Canvas UI를 생성할 컴포넌트 자동 추가
            }

            _turnStatusUI.Bind(_turnManager); // 현재 턴 매니저를 UI에 연결해 즉시 현재 턴 표시

            _turnManager.TurnChanged -= HandleTurnChanged; // 중복 구독을 막기 위해 먼저 해제
            _turnManager.TurnChanged += HandleTurnChanged; // 실제 이동·공격·배치 완료 등 모든 턴 변경을 직접 구독
        }

        private void EnsureHandUI() // 18일차 카드 이미지 손패 Canvas를 한 번만 준비하고 실제 BoardInputController에 연결하는 메서드
        {
            if (_handUI == null) // 아직 HandUI 참조가 없다면
            {
                _handUI = GetComponent<HandUI>(); // 같은 BattleController GameObject에 기존 HandUI가 있는지 먼저 확인
            }

            if (_handUI == null) // 기존 HandUI도 없다면
            {
                _handUI = gameObject.AddComponent<HandUI>(); // 하단 카드 손패 Canvas를 런타임 생성할 컴포넌트 추가
            }

            _handUI.Bind(_boardInputController); // 실제 HandState·턴·보드 소환 로직을 카드 UI에 연결
        }

        private void EnsureDeckPanelUI() // 19일차: 덱/무덤 버튼·패널 Canvas를 한 번만 준비하고 실제 BoardInputController에 연결하는 메서드
        {
            if (_deckPanelUI == null) // 아직 DeckPanelUI 참조가 없다면
            {
                _deckPanelUI = GetComponent<DeckPanelUI>(); // 같은 BattleController GameObject에 기존 컴포넌트가 있는지 먼저 확인
            }

            if (_deckPanelUI == null) // 기존 컴포넌트도 없다면
            {
                _deckPanelUI = gameObject.AddComponent<DeckPanelUI>(); // 좌하단·우하단 카드 더미 Canvas를 런타임 생성할 컴포넌트 추가
            }

            _deckPanelUI.Bind(_boardInputController); // 실제 RunState.Deck과 변경 이벤트를 UI에 연결
        }

        private void EnsureFusionPanelUI() // 21일차: 합성 버튼·패널 Canvas를 한 번만 준비하고 실제 BoardInputController에 연결하는 메서드
        {
            if (_fusionPanelUI == null) // 아직 FusionPanelUI 참조가 없다면
            {
                _fusionPanelUI = GetComponent<FusionPanelUI>(); // 같은 BattleController GameObject에 기존 컴포넌트가 있는지 먼저 확인
            }

            if (_fusionPanelUI == null) // 기존 컴포넌트도 없다면
            {
                _fusionPanelUI = gameObject.AddComponent<FusionPanelUI>(); // 합성 버튼·패널 Canvas를 런타임 생성할 컴포넌트 추가
            }

            _fusionPanelUI.Bind(_boardInputController); // 실제 합성 상태·규칙과 변경 이벤트를 UI에 연결
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 모든 턴 전환에 대한 전투 컨트롤러 후속 처리를 수행하는 메서드
        {
            if (state == TurnState.EnemyTurn) // 방금 적 턴으로 전환됐으면
            {
                if (_boardInputController != null && _boardInputController.TryEnemySummonOneCard()) // 적 손패 카드 1장 소환이 가능하면
                {
                    Debug.Log("EnemyTurn: 카드 1장 소환 행동 완료 -> 즉시 다음 턴"); // 소환 자체가 적 턴의 유일한 행동임을 표시
                    return; // TryEnemySummonOneCard 내부에서 CompleteEnemyTurn까지 처리했으므로 추가 턴 진행 금지
                }

                StartDummyEnemyTurn(); // 적 카드가 없거나 소환 공간이 없을 때만 기존 더미 적 턴 종료 흐름 사용
                return; // 적 턴 처리 후 나머지 조건은 확인하지 않음
            }

            if (state == TurnState.DeploymentTurn && _turnManager.IsInitialDeployment && !_turnManager.IsInitialKingPlaced) // 시작 배치에서 아직 킹이 없다면
            {
                Debug.Log("시작 배치 턴: 먼저 킹을 배치하세요. 킹 배치 후에도 원하는 카드를 계속 배치할 수 있으며 Space로 턴을 종료합니다."); // 초기 진행 조건 안내
                return; // 초기 배치에서는 일반 턴 로직을 실행하지 않음
            }

            if (state == TurnState.DeploymentTurn && _turnManager.IsInitialDeployment) // 킹을 놓은 뒤에도 시작 배치 턴이 계속 열려 있으면
            {
                Debug.Log($"시작 배치 턴 계속: 현재 {_turnManager.DeployedCardCount}장 배치 / 자유 배치 후 Space로 턴 종료"); // 자유 배치 상태 안내
                return; // 명시적 종료 전까지 배치 턴 유지
            }

            if (state == TurnState.DeploymentTurn) // 5턴 주기 일반 배치 턴으로 전환됐으면
            {
                Debug.Log($"{turnNumber}턴 종료 - 배치 턴 시작: 원하는 만큼 자유롭게 배치한 뒤 Space로 배치 턴을 종료하세요."); // 주기 배치 조작 안내 출력
                return; // 배치 턴에서는 일반 전투 로직을 실행하지 않음
            }

            if (state == TurnState.PlayerTurn && turnNumber > _turnLimitTestValue) // 배치 턴 완료를 포함해 새 일반 턴 번호가 제한을 넘겼으면
            {
                Debug.Log($"라운드 턴 제한({_turnLimitTestValue}턴) 초과 - 패배, 전투를 종료합니다."); // 패배 사유 출력
                EndBattle(BattleOutcome.Defeat); // 배치 턴 경유 여부와 관계없이 동일하게 턴 제한 패배 처리
            }
        }

        private void BindState() // RunState의 보드·손패와 TurnManager를 실제 화면/입력 시스템에 주입하는 메서드
        {
            if (_runState == null) // 연결할 런 상태가 없으면
            {
                Debug.LogError("BattleController.BindState: RunState가 없습니다."); // 오류 원인 출력
                return; // 연결 중단
            }

            if (_boardView == null || _boardInputController == null) // Battle 씬 핵심 컴포넌트가 누락됐으면
            {
                Debug.LogError("BattleController.BindState: BoardView 또는 BoardInputController를 찾지 못했습니다."); // 누락 안내
                return; // 잘못된 부분 연결을 피함
            }

            _boardView.Bind(_runState.Board); // 화면이 RunState.Board 바로 그 객체를 참조하도록 연결
            _boardInputController.Bind(_runState, _boardView, _turnManager); // 입력이 실제 RunState와 현재 TurnManager를 함께 참조하도록 연결
            EnsureHandUI(); // 실제 손패를 화면 하단 판타지 카드 UI와 드래그 Drop 소환으로 연결
            EnsureDeckPanelUI(); // 19일차: 좌하단 뽑을 카드 덱 / 우하단 죽은 카드 덱 버튼·패널을 실제 상태에 연결
            EnsureFusionPanelUI(); // 21일차: 합성 버튼·재료·결과 미리보기 패널을 실제 상태에 연결

            _boardInputController.AttackResolved -= HandleAttackResolved; // 재연결 시 중복 구독을 막기 위해 먼저 해제
            _boardInputController.AttackResolved += HandleAttackResolved; // 전투 결과를 받아 킹 HP와 승패를 판정

            Debug.Log($"Battle state bound: Board={_boardView.IsBound}, Hand={_runState.Hand.Hand.Count}장, KingHP={_runState.KingHp}, Turn={_turnManager.TurnNumber}/{_turnManager.CurrentState}"); // 연결 결과 출력
        }

        private void HandleAttackResolved(CombatResult result) // 전투 판정 결과를 받아 킹 HP·패배와 적 전멸 승리를 처리하는 메서드
        {
            var defender = result.Defender; // 이번 공격을 받은 기물

            if (defender.IsPlayerPiece && defender.Definition.MovementType == PieceMovementType.King) // 아군 킹이 공격받았으면
            {
                _runState.KingHp = defender.CurrentHp; // 보드 위 킹 기물의 실제 체력을 RunState.KingHp에 동기화
                Debug.Log($"킹 피격: 남은 KingHP={_runState.KingHp}"); // 킹 피격 결과를 콘솔에 출력

                if (_runState.IsDefeated) // 킹 체력이 0 이하가 됐으면
                {
                    Debug.Log("킹 HP 0 - 런 패배, 전투를 종료합니다."); // 패배 사유를 콘솔에 출력
                    EndBattle(BattleOutcome.Defeat); // 턴 진행을 멈추고 패배로 전투 종료
                    return; // 같은 판정에서 승리 조건까지 확인할 필요 없으므로 종료
                }
            }

            if (result.DefenderDied && !defender.IsPlayerPiece) // 이번 공격으로 적 기물이 사망했으면
            {
                int remainingEnemies = _runState.Board.CountPieces(isPlayerPiece: false); // 보드 위에 남은 적 기물 수 확인
                Debug.Log($"적 처치: 남은 적 {remainingEnemies}기"); // 처치 결과를 콘솔에 출력

                if (remainingEnemies == 0) // 남은 적이 없으면
                {
                    Debug.Log("적 전멸 - 승리, 전투를 종료합니다."); // 승리 사유를 콘솔에 출력
                    _boardInputController?.ReturnDeadPileToOwnedPool(); // 19일차: 라운드 클리어 시 죽은 카드를 보유 풀로 복귀
                    EndBattle(BattleOutcome.Victory); // 턴 진행을 멈추고 승리로 전투 종료
                }
            }
        }

        private void OnDestroy() // 오브젝트가 파괴될 때 이벤트 구독을 정리하는 메서드
        {
            if (_boardInputController != null) // 입력 컨트롤러 참조가 남아 있으면
            {
                _boardInputController.AttackResolved -= HandleAttackResolved; // 이벤트 구독 해제로 불필요한 참조 제거
            }

            if (_turnManager != null) // 턴 매니저 참조가 남아 있으면
            {
                _turnManager.TurnChanged -= HandleTurnChanged; // 이벤트 구독 해제로 불필요한 참조 제거
            }
        }

        private void StartDummyEnemyTurn() // 실제 AI가 없는 현재 단계에서 적 턴 흐름만 검증하기 위한 메서드
        {
            if (_dummyEnemyTurnCoroutine != null) // 이전 더미 적 턴 코루틴이 남아 있다면
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 중복 코루틴을 방지하기 위해 먼저 중단
            }

            _dummyEnemyTurnCoroutine = StartCoroutine(CompleteDummyEnemyTurnAfterDelay()); // 일정 시간 후 자동으로 적 턴을 끝내는 코루틴 시작
        }

        private IEnumerator CompleteDummyEnemyTurnAfterDelay() // 짧게 적 턴을 보여준 뒤 배치 턴 또는 다음 플레이어 턴으로 넘어가는 코루틴
        {
            yield return new WaitForSeconds(_dummyEnemyTurnDelay); // Canvas UI에서 적 턴 색상을 확인할 수 있도록 잠시 대기

            if (_turnManager != null && _turnManager.CompleteEnemyTurn()) // 아직 적 턴이면 다음 상태로 정상 전환
            {
                Debug.Log($"Enemy turn completed -> {_turnManager.CurrentState} / Turn {_turnManager.TurnNumber}"); // 배치 턴 포함 실제 전환 결과 출력
            }

            _dummyEnemyTurnCoroutine = null; // 코루틴 완료 후 참조 초기화
        }

        private void OnDisable() // 오브젝트가 비활성화될 때 진행 중인 임시 적 턴을 정리하는 메서드
        {
            if (_dummyEnemyTurnCoroutine != null) // 실행 중인 더미 적 턴 코루틴이 있다면
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 비활성화 이후 상태가 바뀌지 않도록 중단
                _dummyEnemyTurnCoroutine = null; // 코루틴 참조 초기화
            }
        }
    }
}
