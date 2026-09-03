using UnityEngine; // MonoBehaviour, GameObject, Debug 등을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬 이름을 확인하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public class BattleController : MonoBehaviour // Battle 씬에서 하나의 RunState를 생성·소유하고 화면/입력에 연결하는 진입점
    {
        [SerializeField] private int _startingKingHp = 3; // 새 테스트 런의 시작 킹 체력
        [SerializeField] private BoardView _boardView; // 실제 RunState.Board를 표시할 보드 뷰
        [SerializeField] private BoardInputController _boardInputController; // 실제 RunState를 변경할 입력 컨트롤러

        public RunState RunState => _runState; // 현재 전투가 사용하는 단일 런 상태

        private RunState _runState; // 보드·손패·덱·킹 체력 등을 소유하는 단일 상태 객체

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 씬 로드가 끝난 직후 자동 실행
        private static void AutoCreateForBattleScene() // Battle 씬에 컴포넌트를 직접 배치하지 않아도 9일차 구조를 바로 적용하는 부트스트랩
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

            if (_runState == null) // 아직 외부에서 전달받은 런 상태가 없으면
            {
                _runState = new RunState(_startingKingHp); // 새 테스트 런을 하나만 생성
                BindState(); // RunState.Board/Hand를 화면과 입력에 연결

                if (_boardInputController != null) // 입력 컨트롤러가 정상 연결됐으면
                {
                    _boardInputController.EnsurePrototypeStartingHand(); // 기존 5일차 테스트용 King/Pawn을 실제 RunState.Hand에 넣음
                }
            }
            else // 외부 상태가 이미 있으면
            {
                BindState(); // 새로 만들지 않고 전달받은 상태를 그대로 연결
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
            BindState(); // 동일한 상태를 화면과 입력 양쪽에 연결
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

        private void BindState() // RunState의 보드와 손패를 실제 화면/입력 시스템에 주입하는 메서드
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
            _boardInputController.Bind(_runState, _boardView); // 입력이 RunState.Hand와 RunState.Board를 직접 변경하도록 연결

            Debug.Log($"Battle state bound: Board={_boardView.IsBound}, Hand={_runState.Hand.Hand.Count}장, KingHP={_runState.KingHp}"); // 연결 결과를 개발용 로그로 확인
        }
    }
}
