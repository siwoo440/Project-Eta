using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour와 Debug를 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController와 BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // 38일차 2x2 보스 행동 플래너와 실행기를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAITurnDriver : MonoBehaviour // Battle 씬의 EnemyTurn을 일반 AI와 2x2 보스 전투에 자동 연결하는 런타임 드라이버
    {
        public static EnemyAIPerformanceStats LastTurnPerformanceStats { get; private set; } = EnemyAIPerformanceStats.Empty; // 실제 마지막 EnemyTurn에서 일반 AI가 사용한 계산량을 F1 창에 제공

        private BattleController _battleController; // 현재 Battle 씬의 전투 상태 소유자
        private BoardInputController _boardInput; // 기존 적 카드·보드 시스템 접근용 입력 컨트롤러
        private BoardView _boardView; // AI 이동 화면 연출에 사용할 실제 보드 뷰
        private TurnManager _turnManager; // 적 턴 시작 이벤트를 구독할 턴 매니저
        private readonly EnemyAIAdvancedPlanner _planner = new EnemyAIAdvancedPlanner(); // 39일차 캐시·후보 예산·fallback을 적용한 일반 적 최종 플래너
        private readonly BossActionPlanner _bossPlanner = new BossActionPlanner(); // 38일차 2x2 보스 이동·공격 기본 플래너
        private BossPhase2Controller _bossPhase2Controller; // 38일차 Phase 2 전환·텔레그래프·범위 공격을 EnemyTurn보다 먼저 처리할 관리자
        private Coroutine _enemyTurnCoroutine; // 한 EnemyTurn에 AI 실행이 중복되지 않도록 관리하는 코루틴
        private bool _isBound; // 실제 전투 객체와 이벤트 연결이 끝났는지 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 실행
        private static void AutoCreateForBattleScene() // 씬에 수동 컴포넌트 배치 없이 AI 드라이버를 만드는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 생성하지 않음
            if (UnityEngine.Object.FindFirstObjectByType<EnemyAITurnDriver>() != null) return; // 이미 존재하면 중복 생성 방지

            var driverObject = new GameObject("EnemyAITurnDriver_Day39"); // 39일차 최적화 일반 AI+보스 통합 턴 드라이버 전용 오브젝트 생성
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

            Debug.LogError("39일차 Enemy AI 초기화 실패: BattleController, BoardInputController 또는 BoardView를 찾지 못했습니다."); // 제한 시간 내 연결 실패 안내
        }

        private void Bind() // 기존 전투 흐름에 AI를 한 번만 연결하는 메서드
        {
            if (_isBound) return; // 이미 연결돼 있으면 중복 구독하지 않음

            _turnManager = _battleController.TurnManager; // 실제 TurnManager 참조 저장
            DisableLegacyEnemyCardSummon(); // 기존 임시 적 카드 자동 소환을 비워 보드 위 AI 행동만 사용
            EnsureBossPhase2Controller(); // 38일차 Phase 2 관리자도 같은 전투 상태에 연결

            _turnManager.TurnChanged -= HandleTurnChanged; // 혹시 남은 중복 구독을 먼저 제거
            _turnManager.TurnChanged += HandleTurnChanged; // EnemyTurn 시작 이벤트 구독
            _isBound = true; // 연결 완료 기록

            Debug.Log("39일차 Enemy AI 연결 완료: Lazy Threat·공유 캐시·후보 예산·fallback과 성능 측정을 적용합니다."); // 개발 로그 출력
        }

        private void EnsureBossPhase2Controller() // Phase 2 시스템을 별도 Inspector 설정 없이 현재 AI 드라이버와 같은 GameObject에 준비하는 메서드
        {
            _bossPhase2Controller = UnityEngine.Object.FindFirstObjectByType<BossPhase2Controller>(); // 이미 씬에 존재하는 관리자 우선 탐색
            if (_bossPhase2Controller == null) _bossPhase2Controller = gameObject.AddComponent<BossPhase2Controller>(); // 없으면 현재 드라이버 오브젝트에 자동 추가
            _bossPhase2Controller.Bind(_battleController, _boardInput, _boardView); // 현재 BattleController·스폰·보드 뷰를 직접 연결
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
                // 33일차 이후에는 보드 AI가 행동하므로 기존 무작위 카드 소환용 드로우 더미를 사용하지 않음
            }
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 전환 이벤트에서 EnemyTurn만 AI 실행으로 연결하는 메서드
        {
            if (state != TurnState.EnemyTurn) return; // 적 턴이 아니면 처리하지 않음
            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn) return; // 앞선 구독자가 이미 턴을 바꿨다면 중복 실행 금지

            if (_enemyTurnCoroutine != null) StopCoroutine(_enemyTurnCoroutine); // 이전 AI 코루틴이 남아 있다면 중복 행동을 막기 위해 정리

            _enemyTurnCoroutine = StartCoroutine(ExecuteEnemyTurnNextFrame(turnNumber)); // 기존 fallback과 충돌하지 않도록 다음 프레임 실행
        }

        private IEnumerator ExecuteEnemyTurnNextFrame(int turnNumber) // 현재 EnemyTurn에서 일반 적과 보스 후보를 비교해 행동 하나를 선택·실행하는 코루틴
        {
            yield return null; // 같은 TurnChanged 호출 스택을 벗어나 한 프레임 대기

            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn || _battleController == null || _battleController.RunState == null) // 대기 중 전투 상태가 바뀌었으면
            {
                _enemyTurnCoroutine = null; // 코루틴 참조 정리
                yield break; // AI 행동 취소
            }

            if (_bossPhase2Controller != null && _bossPhase2Controller.TryHandleEnemyTurn()) // Phase 2 보스가 예고 또는 예고 공격으로 이번 EnemyTurn을 소비했다면
            {
                LastTurnPerformanceStats = EnemyAIPerformanceStats.Empty; // 일반 AI를 계산하지 않은 턴임을 F1 통계에 명확히 표시
                _enemyTurnCoroutine = null; // 현재 적 턴 코루틴 참조 정리
                yield break; // 같은 턴에 일반 적·Phase 1 보스가 추가 행동하지 않도록 즉시 종료
            }

            var board = _battleController.RunState.Board; // 현재 실제 전투 보드 참조
            bool hasNormalAction = false; // 일반 적 행동 존재 여부 기본값
            AIActionCandidate normalAction = null; // 일반 적 최종 행동 후보

            try // 일반 AI 후보 생성 자체의 예상 밖 예외도 EnemyTurn 교착으로 이어지지 않게 보호
            {
                hasNormalAction = _planner.TryChooseAction(board, out normalAction); // 39일차 최적화 일반 적 최고 후보 계산
                LastTurnPerformanceStats = _planner.LastPerformanceStats; // 실제 게임 턴에서 발생한 계산량을 즉시 저장
                Debug.Log($"AI 성능: {LastTurnPerformanceStats}"); // 프로파일링 전에도 Console에서 턴별 계산량을 확인 가능하게 출력
            }
            catch (System.Exception exception) // Base Planner 등 외부 기존 코드에서 예외가 난 경우
            {
                LastTurnPerformanceStats = EnemyAIPerformanceStats.Empty.WithFallback(true); // 비정상 fallback 상태 기록
                Debug.LogError($"Enemy AI 평가 예외 — 보스 후보 또는 안전 종료로 전환합니다.\n{exception}"); // 예외 전체 스택을 남겨 이후 수정 가능하게 함
            }

            bool hasBossAction = false; // 보스 행동 존재 여부 기본값
            BossActionCandidate bossAction = null; // 38일차 보스 행동 후보

            try // 보스 플래너 예외도 적 턴 전체 교착으로 번지지 않도록 보호
            {
                hasBossAction = _bossPlanner.TryChooseAction(board, out bossAction); // 38일차 보스 최고 후보 계산
            }
            catch (System.Exception exception) // 보스 평가에서 예상 밖 예외가 난 경우
            {
                Debug.LogError($"Boss AI 평가 예외 — 일반 AI 또는 안전 종료로 전환합니다.\n{exception}"); // 문제를 남기고 일반 AI를 계속 사용할 수 있게 함
            }

            if (hasBossAction && (!hasNormalAction || bossAction.Score >= normalAction.Score)) // 보스 후보가 유일하거나 일반 AI보다 점수가 높으면
            {
                Debug.Log($"Boss AI 선택: {bossAction}"); // 선택된 보스 후보 출력

                bool executed = BossActionExecutor.TryExecute( // 2x2 점유·CombatResolver를 사용하는 보스 전용 실행
                    bossAction, // 선택된 보스 행동
                    _battleController.RunState, // 현재 런 상태
                    _turnManager, // 실제 턴 매니저
                    _battleController.BattleHooks, // 기존 이동·공격·피해 훅
                    _boardView, // 보스 모델 위치 보정에 사용할 보드 뷰
                    out _); // 공격 결과는 실행기 내부에서 상태를 모두 처리하므로 여기서는 사용하지 않음

                if (!executed && _turnManager.CurrentState == TurnState.EnemyTurn) // 실행 직전에 후보가 무효화됐다면
                {
                    Debug.LogWarning("Boss AI 후보 실행 실패 — 안전하게 적 턴을 종료합니다."); // 교착 방지 로그
                    CompleteTurnWithoutAction(); // 다음 턴 진행
                }
            }
            else if (hasNormalAction) // 보스보다 일반 적 행동 점수가 높거나 보스 후보가 없으면
            {
                Debug.Log($"Enemy AI 선택: {normalAction}"); // 일반 AI 선택 결과 출력

                bool executed = EnemyAIActionExecutor.TryExecute( // 기존 일반 AI 실행기 사용
                    normalAction, // 선택된 일반 행동 후보
                    _battleController.RunState, // 현재 런 상태
                    _turnManager, // 실제 턴 매니저
                    _battleController.BattleHooks, // 상태 효과·로그·연출 공통 훅
                    _boardView, // 이동·사망 연출 보드 뷰
                    out _); // 공격 결과는 실행기 내부 처리

                if (!executed && _turnManager.CurrentState == TurnState.EnemyTurn) // 일반 후보가 실행 직전 무효화됐다면
                {
                    Debug.LogWarning("Enemy AI 후보 실행 실패 — 안전하게 적 턴을 종료합니다."); // 교착 방지 로그
                    CompleteTurnWithoutAction(); // 다음 턴 진행
                }
            }
            else // 일반 적과 보스 모두 합법 행동이 하나도 없거나 양쪽 평가가 실패하면
            {
                Debug.Log($"Enemy/Boss AI 행동 없음: Turn {turnNumber} — 적 턴을 안전하게 종료합니다."); // 행동 없음 로그
                CompleteTurnWithoutAction(); // 턴 교착 없이 정상 종료
            }

            _enemyTurnCoroutine = null; // 이번 적 턴 처리 완료 후 참조 정리
        }

        private void CompleteTurnWithoutAction() // 합법 행동이 없을 때도 상태 효과 정산과 다음 턴을 보장하는 fallback
        {
            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn) return; // 현재 적 턴이 아니면 중복 종료하지 않음

            if (_turnManager.CompleteEnemyTurn()) // 기존 TurnManager로 다음 상태 전환
            {
                _battleController?.BattleHooks?.RaiseTurnEnd(_turnManager.CurrentState, _turnManager.TurnNumber); // 기존 AI와 같은 TurnEnd 훅 발행
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
