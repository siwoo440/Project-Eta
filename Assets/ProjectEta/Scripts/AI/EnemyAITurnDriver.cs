using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Debug, SceneManagement 보조 타입을 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController와 BoardView를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAITurnDriver : MonoBehaviour // Battle 씬의 EnemyTurn을 33일차 AI 코어에 자동 연결하는 런타임 드라이버
    {
        private BattleController _battleController; // 현재 Battle 씬의 전투 상태 소유자
        private BoardInputController _boardInput; // 기존 적 카드·보드 시스템 접근용 입력 컨트롤러
        private BoardView _boardView; // AI 이동 화면 연출에 사용할 실제 보드 뷰
        private TurnManager _turnManager; // 적 턴 시작 이벤트를 구독할 턴 매니저
        private readonly EnemyAIAdvancedPlanner _planner = new EnemyAIAdvancedPlanner(); // 35일차: Base·Role·Threat·Special 네 점수 계층을 사용하는 최종 플래너
        private Coroutine _enemyTurnCoroutine; // 한 EnemyTurn에 AI 실행이 중복되지 않도록 관리하는 코루틴
        private bool _isBound; // 실제 전투 객체와 이벤트 연결이 끝났는지 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 실행
        private static void AutoCreateForBattleScene() // 씬에 수동 컴포넌트 배치 없이 AI 드라이버를 만드는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 생성하지 않음
            if (UnityEngine.Object.FindFirstObjectByType<EnemyAITurnDriver>() != null) return; // 이미 존재하면 중복 생성 방지

            var driverObject = new GameObject("EnemyAITurnDriver_Day33"); // AI 턴 드라이버 전용 오브젝트 생성
            driverObject.AddComponent<EnemyAITurnDriver>(); // 컴포넌트 추가 후 Start에서 실제 전투 연결
        }

        private IEnumerator Start() // BattleController 자동 생성 순서와 무관하게 안전하게 참조를 찾는 초기화 코루틴
        {
            const int maxWaitFrames = 120; // 자동 생성 순서가 늦어져도 약 2초 정도 기다릴 최대 프레임
            int waitedFrames = 0; // 현재까지 기다린 프레임 수

            while (waitedFrames < maxWaitFrames) // 필요한 전투 객체가 준비될 때까지 제한적으로 반복
            {
                _battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색
                _boardInput = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 현재 보드 입력 컨트롤러 탐색
                _boardView = UnityEngine.Object.FindFirstObjectByType<BoardView>(); // 현재 보드 뷰 탐색

                if (_battleController != null && _boardInput != null && _boardView != null && _battleController.TurnManager != null && _battleController.RunState != null) // 핵심 객체가 모두 준비됐으면
                {
                    Bind(); // AI를 실제 턴 흐름에 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임까지 기다림
            }

            Debug.LogError("33일차 Enemy AI 초기화 실패: BattleController, BoardInputController 또는 BoardView를 찾지 못했습니다."); // 제한 시간 내 연결 실패 안내
        }

        private void Bind() // 기존 전투 흐름에 AI를 한 번만 연결하는 메서드
        {
            if (_isBound) return; // 이미 연결돼 있으면 중복 구독하지 않음

            _turnManager = _battleController.TurnManager; // 실제 TurnManager 참조 저장
            DisableLegacyEnemyCardSummon(); // 17~20일차 임시 '적 카드 1장 소환 후 즉시 종료' 흐름을 비워 AI 기물 행동이 우선되게 전환

            _turnManager.TurnChanged -= HandleTurnChanged; // 혹시 남은 중복 구독을 먼저 제거
            _turnManager.TurnChanged += HandleTurnChanged; // EnemyTurn 시작 이벤트 구독
            _isBound = true; // 연결 완료 기록

            Debug.Log("35일차 Enemy AI 연결 완료: 역할·위협·특수 기물 점수를 함께 적용합니다."); // 개발 로그 출력
        }

        private void DisableLegacyEnemyCardSummon() // 기존 프로토타입 적 손패/드로우를 비워 BattleController의 자동 소환이 실행되지 않게 하는 메서드
        {
            if (_boardInput == null) return; // 입력 컨트롤러가 없으면 처리할 수 없음

            while (_boardInput.EnemyHandState.Hand.Count > 0) // 기존 적 손패에 카드가 남아 있는 동안
            {
                var card = _boardInput.EnemyHandState.Hand[0]; // 첫 카드 조회
                _boardInput.EnemyHandState.RemoveCard(card); // 손패에서 제거해 자동 소환 대상에서 제외
            }

            while (_boardInput.EnemyDeck.TryDraw(out _)) // 기존 적 DrawPile에 남은 카드를 모두 꺼내
            {
                // 33일차부터는 적 보드 AI가 행동하므로 기존 무작위 카드 소환용 드로우 더미는 사용하지 않음
            }
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 전환 이벤트에서 EnemyTurn만 AI 실행으로 연결하는 메서드
        {
            if (state != TurnState.EnemyTurn) return; // 적 턴이 아니면 처리하지 않음
            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn) return; // 앞선 구독자가 이미 턴을 바꿨다면 중복 실행 금지

            if (_enemyTurnCoroutine != null) // 이전 AI 코루틴이 남아 있다면
            {
                StopCoroutine(_enemyTurnCoroutine); // 중복 행동을 막기 위해 정리
            }

            _enemyTurnCoroutine = StartCoroutine(ExecuteEnemyTurnNextFrame(turnNumber)); // BattleController가 기존 fallback 코루틴을 시작한 뒤 AI가 먼저 행동하도록 다음 프레임 실행
        }

        private IEnumerator ExecuteEnemyTurnNextFrame(int turnNumber) // 현재 EnemyTurn에서 행동 하나를 선택·실행하는 코루틴
        {
            yield return null; // 같은 TurnChanged 호출 스택을 벗어나 기존 전투 컨트롤러 처리와 충돌하지 않게 한 프레임 대기

            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn || _battleController == null || _battleController.RunState == null) // 대기 중 전투 상태가 바뀌었으면
            {
                _enemyTurnCoroutine = null; // 코루틴 참조 정리
                yield break; // AI 행동 취소
            }

            var board = _battleController.RunState.Board; // 현재 실제 전투 보드 참조
            if (_planner.TryChooseAction(board, out var action)) // 합법 후보 중 최고 점수 행동을 찾았으면
            {
                Debug.Log($"Enemy AI 선택: {action}"); // 후보 선택 결과 출력

                bool executed = EnemyAIActionExecutor.TryExecute( // 실제 보드·전투·턴 흐름으로 행동 실행
                    action, // 선택된 행동 후보
                    _battleController.RunState, // 현재 런 상태
                    _turnManager, // 실제 턴 매니저
                    _battleController.BattleHooks, // 상태 효과·로그·연출과 공유하는 기존 전투 훅
                    _boardView, // 이동·사망 연출에 사용할 보드 뷰
                    out _); // 공격 결과는 실행기 내부에서 킹 패배까지 처리하므로 드라이버에서는 별도 사용하지 않음

                if (!executed && _turnManager.CurrentState == TurnState.EnemyTurn) // 후보가 실행 직전에 무효화됐다면
                {
                    Debug.LogWarning("Enemy AI 후보 실행 실패 — 안전하게 적 턴을 종료합니다."); // 교착 방지 로그
                    CompleteTurnWithoutAction(); // 다음 턴으로 진행
                }
            }
            else // 적이 기절했거나 이동할 곳이 없는 등 합법 행동이 하나도 없으면
            {
                Debug.Log($"Enemy AI 행동 없음: Turn {turnNumber} — 적 턴을 안전하게 종료합니다."); // 행동 없음 로그
                CompleteTurnWithoutAction(); // 턴 교착 없이 정상 종료
            }

            _enemyTurnCoroutine = null; // 이번 적 턴 처리 완료 후 참조 정리
        }

        private void CompleteTurnWithoutAction() // 합법 행동이 없을 때도 상태 효과 정산과 다음 턴을 보장하는 fallback
        {
            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn) return; // 현재 적 턴이 아니면 중복 종료하지 않음

            if (_turnManager.CompleteEnemyTurn()) // 기존 TurnManager로 다음 상태 전환
            {
                _battleController?.BattleHooks?.RaiseTurnEnd(_turnManager.CurrentState, _turnManager.TurnNumber); // 기존 더미 적 턴과 같은 TurnEnd 훅 발행
            }
        }

        private void OnDestroy() // 드라이버가 파괴될 때 이벤트와 코루틴을 정리하는 메서드
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // TurnManager 이벤트 구독 해제

            if (_enemyTurnCoroutine != null) // 진행 중 AI 코루틴이 있으면
            {
                StopCoroutine(_enemyTurnCoroutine); // 파괴 후 행동이 실행되지 않도록 중단
                _enemyTurnCoroutine = null; // 참조 정리
            }
        }
    }
}
