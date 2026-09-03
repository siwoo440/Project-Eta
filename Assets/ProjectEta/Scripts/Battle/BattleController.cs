using System.Collections; // Coroutine과 IEnumerator를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, Debug 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // Space 키 기반 10일차 임시 행동 완료 입력을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬 이름을 확인하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public class BattleController : MonoBehaviour // Battle 씬에서 단일 RunState와 턴 흐름을 소유하는 전투 진입점
    {
        [SerializeField] private int _startingKingHp = 3; // 새 테스트 런의 시작 킹 체력
        [SerializeField] private float _dummyEnemyTurnDelay = 0.5f; // 10일차 더미 적 턴이 유지되는 테스트 시간
        [SerializeField] private BoardView _boardView; // 실제 RunState.Board를 표시할 보드 뷰
        [SerializeField] private BoardInputController _boardInputController; // 실제 RunState를 변경할 입력 컨트롤러

        public RunState RunState => _runState; // 현재 전투가 사용하는 단일 런 상태
        public TurnManager TurnManager => _turnManager; // 현재 전투가 사용하는 턴 매니저
        public TurnStatusUI TurnStatusUI => _turnStatusUI; // 화면 상단 중앙의 턴 상태 Canvas UI

        private RunState _runState; // 보드·손패·덱·킹 체력 등을 소유하는 단일 상태 객체
        private TurnManager _turnManager; // 플레이어/적 턴과 행동 권한을 관리하는 상태 객체
        private TurnStatusUI _turnStatusUI; // 현재 턴을 상단 중앙에 표시하는 Canvas UI
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
                    _boardInputController.EnsurePrototypeStartingHand(); // 기존 테스트용 King/Pawn을 실제 RunState.Hand에 넣음
                }
            }
            else // 외부 상태가 이미 있으면
            {
                BindState(); // 새로 만들지 않고 전달받은 상태를 그대로 연결
            }
        }

        private void Update() // 매 프레임 10일차 임시 턴 테스트 입력을 확인하는 메서드
        {
            if (_turnManager == null || Keyboard.current == null) // 턴 매니저나 키보드 입력이 없으면
            {
                return; // 처리하지 않고 종료
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame) // Space 키를 이번 프레임에 눌렀으면
            {
                TryCompletePlayerAction(); // 실제 이동이 연결되기 전 임시로 플레이어 일반 행동 완료 처리
            }
        }

        public void Initialize(RunState runState) // 이후 세이브 로드나 다른 씬에서 기존 RunState를 넘길 때 사용할 진입점
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
        }

        public bool TryCompletePlayerAction() // 플레이어 일반 행동 1회를 완료하고 적 턴을 시작하는 외부 진입점
        {
            if (_turnManager == null) // 턴 매니저가 아직 준비되지 않았다면
            {
                return false; // 행동 완료를 처리할 수 없으므로 실패 반환
            }

            if (!_turnManager.TryCompletePlayerAction()) // 현재 행동 권한이 없어 턴 매니저가 거부했다면
            {
                Debug.Log("플레이어 행동 완료 거부: 현재 플레이어가 행동할 수 있는 턴이 아닙니다."); // 개발용 거부 사유 출력
                return false; // 중복 행동 또는 잘못된 턴임을 반환
            }

            Debug.Log($"Turn {_turnManager.TurnNumber}: Player action completed -> EnemyTurn"); // 플레이어 턴 종료 결과 출력
            StartDummyEnemyTurn(); // 실제 AI 구현 전까지 짧은 더미 적 턴을 자동 실행
            return true; // 정상적으로 플레이어 행동을 완료했음을 반환
        }

        public void EndBattle() // 승리·패배 시스템이 완성됐을 때 호출할 전투 종료 진입점
        {
            if (_dummyEnemyTurnCoroutine != null) // 진행 중인 더미 적 턴이 있다면
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 전투 종료 후 다음 턴으로 넘어가지 않도록 코루틴 중단
                _dummyEnemyTurnCoroutine = null; // 코루틴 참조 초기화
            }

            _turnManager?.EndBattle(); // 턴 상태를 전투 종료로 변경
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
                _turnManager = new TurnManager(); // 1턴 플레이어 턴 상태로 새 턴 매니저 생성
            }

            if (_turnStatusUI == null) // 턴 상태 UI 컴포넌트가 없다면
            {
                _turnStatusUI = GetComponent<TurnStatusUI>(); // 같은 오브젝트에 기존 UI 컴포넌트가 있는지 먼저 확인
            }

            if (_turnStatusUI == null) // 기존 컴포넌트도 없다면
            {
                _turnStatusUI = gameObject.AddComponent<TurnStatusUI>(); // 상단 중앙 Canvas UI를 생성할 컴포넌트 자동 추가
            }

            _turnStatusUI.Bind(_turnManager); // 현재 턴 매니저를 UI에 연결해 즉시 1턴 플레이어 턴을 표시
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

            Debug.Log($"Battle state bound: Board={_boardView.IsBound}, Hand={_runState.Hand.Hand.Count}장, KingHP={_runState.KingHp}, Turn={_turnManager.TurnNumber}/{_turnManager.CurrentState}"); // 연결 결과를 개발용 로그로 확인
        }

        private void StartDummyEnemyTurn() // 실제 AI가 없는 10일차에서 적 턴 흐름만 검증하기 위한 메서드
        {
            if (_dummyEnemyTurnCoroutine != null) // 이전 더미 적 턴 코루틴이 남아 있다면
            {
                StopCoroutine(_dummyEnemyTurnCoroutine); // 중복 코루틴을 방지하기 위해 먼저 중단
            }

            _dummyEnemyTurnCoroutine = StartCoroutine(CompleteDummyEnemyTurnAfterDelay()); // 일정 시간 후 자동으로 적 턴을 끝내는 코루틴 시작
        }

        private IEnumerator CompleteDummyEnemyTurnAfterDelay() // 짧게 적 턴을 보여준 뒤 다음 플레이어 턴으로 넘어가는 코루틴
        {
            yield return new WaitForSeconds(_dummyEnemyTurnDelay); // Canvas UI에서 적 턴 상태를 확인할 수 있도록 잠시 대기

            if (_turnManager != null && _turnManager.CompleteEnemyTurn()) // 아직 적 턴이면 다음 플레이어 턴으로 정상 전환
            {
                Debug.Log($"Turn {_turnManager.TurnNumber}: Enemy turn completed -> PlayerTurn"); // 다음 턴 시작 결과 출력
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
