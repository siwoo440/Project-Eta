using System.Collections; // 초기화 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // Battle 씬 판정 사용
using ProjectEta.Battle; // BattleController·TurnManager 사용
using ProjectEta.Run; // RunState·RoundProgressStatus 사용

namespace ProjectEta.Round // 라운드 런타임 연결 네임스페이스
{
    [DefaultExecutionOrder(900)] // 기존 전투 시스템 초기화 이후 연결
    public sealed class RoundStateBattleBridge : MonoBehaviour // TurnManager 결과를 RunState 라운드 상태에 연결
    {
        private BattleController _battleController; // 현재 전투 컨트롤러
        private TurnManager _turnManager; // 현재 턴 매니저
        private RunState _runState; // 현재 런 상태
        private bool _isBound; // 이벤트 연결 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬 수정 없이 브리지 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 방지
            if (Object.FindFirstObjectByType<RoundStateBattleBridge>() != null) return; // 중복 생성 방지

            var bridgeObject = new GameObject("RoundStateBattleBridge_Day42"); // 브리지 오브젝트 생성
            bridgeObject.AddComponent<RoundStateBattleBridge>(); // 라운드 상태 연결 컴포넌트 추가
        }

        private IEnumerator Start() // 기존 전투 시스템 준비 후 연결
        {
            const int maxWaitFrames = 180; // 최대 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 전투 상태 생성 대기
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 탐색

                if (_battleController != null && _battleController.RunState != null && _battleController.TurnManager != null) // 필수 상태 준비 확인
                {
                    Bind(); // 실제 상태 연결
                    yield break; // 초기화 종료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("42일차 RoundStateBattleBridge 초기화 실패: BattleController 또는 RunState를 찾지 못했습니다."); // 연결 실패 기록
        }

        private void Bind() // RunState와 TurnManager 연결
        {
            if (_isBound) return; // 중복 연결 방지

            _runState = _battleController.RunState; // 현재 런 상태 저장
            _turnManager = _battleController.TurnManager; // 현재 턴 매니저 저장

            if (_runState.CurrentRoundStatus == RoundProgressStatus.NotStarted) // 아직 시작되지 않은 라운드라면
            {
                _runState.StartCurrentRound(); // 현재 라운드 진행 상태 시작
            }

            _turnManager.TurnChanged -= HandleTurnChanged; // 중복 이벤트 구독 제거
            _turnManager.TurnChanged += HandleTurnChanged; // 전투 종료 결과 구독
            _isBound = true; // 연결 완료 기록

            if (_turnManager.CurrentState == TurnState.BattleEnded) // 연결 이전 이미 전투가 끝났다면
            {
                SynchronizeOutcome(); // 종료 결과 즉시 동기화
            }

            Debug.Log($"42일차 라운드 상태 연결: Round={_runState.CurrentRound} / Boss={_runState.IsBossRound} / Status={_runState.CurrentRoundStatus}"); // 연결 결과 기록
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 상태 변경 수신
        {
            if (state != TurnState.BattleEnded) return; // 전투 종료 외 상태 제외
            SynchronizeOutcome(); // 전투 결과를 런 상태에 기록
        }

        private void SynchronizeOutcome() // TurnManager 결과를 RoundState에 반영
        {
            if (_runState == null || _turnManager == null) return; // 필수 상태 검사
            _runState.RecordBattleOutcome(_turnManager.Outcome); // 승리·패배 결과 기록
            Debug.Log($"42일차 라운드 결과 기록: Round={_runState.CurrentRound} / Status={_runState.CurrentRoundStatus} / Outcome={_runState.LastBattleOutcome}"); // 결과 기록 출력
        }

        private void OnDestroy() // 브리지 파괴 시 이벤트 정리
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 구독 해제
            _isBound = false; // 연결 상태 초기화
        }
    }
}
