using System.Collections; // 연출 완료 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·WaitForSeconds 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleController·TurnManager 사용

namespace ProjectEta.UI // 전투 흐름 보조 런타임 네임스페이스
{
    [DefaultExecutionOrder(930)] // AI 턴 드라이버보다 먼저 플레이어 행동 지연 모드 연결
    public sealed class PlayerActionTurnDelayController : MonoBehaviour // 플레이어 기물 연출이 끝난 뒤 EnemyTurn을 시작하는 런타임 게이트
    {
        private const float PlayerVisualSettleSeconds = 0.55f; // 비치명 공격 0.48초 전체 연출보다 길게 잡아 복귀 완료 후 적 턴 시작

        private BattleController _battleController; // 현재 Battle 씬 전투 컨트롤러
        private TurnManager _turnManager; // 실제 턴 상태 객체
        private Coroutine _releaseCoroutine; // 현재 플레이어 연출 대기 코루틴
        private bool _isBound; // 런타임 연결 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬·Inspector 수정 없이 턴 연출 게이트 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<PlayerActionTurnDelayController>() != null) return; // 중복 생성 차단

            var host = new GameObject("PlayerActionTurnDelayController_Day44"); // 턴 지연 게이트 호스트 생성
            host.AddComponent<PlayerActionTurnDelayController>(); // 런타임 게이트 컴포넌트 추가
        }

        private IEnumerator Start() // BattleController 자동 생성 순서와 무관하게 안전하게 연결
        {
            const int maxWaitFrames = 180; // 최대 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색

                if (_battleController != null && _battleController.TurnManager != null)
                {
                    Bind(_battleController.TurnManager); // 실제 턴 매니저에 연출 완료 지연 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("44일차 PlayerActionTurnDelayController 초기화 실패: BattleController 또는 TurnManager를 찾지 못했습니다."); // 연결 실패 기록
        }

        private void Bind(TurnManager turnManager) // 플레이어 행동 지연 모드를 실제 턴 상태에 연결
        {
            if (_turnManager != null) _turnManager.PlayerActionTransitionPending -= HandlePlayerActionTransitionPending; // 이전 이벤트 구독 해제

            _turnManager = turnManager; // 현재 턴 매니저 저장
            _turnManager.SetPlayerActionTransitionDeferred(true); // 모든 일반 플레이어 행동의 EnemyTurn 진입을 연출 완료까지 보류
            _turnManager.PlayerActionTransitionPending += HandlePlayerActionTransitionPending; // 행동 완료 예약 이벤트 구독
            _isBound = true; // 연결 완료 기록

            Debug.Log("44일차 플레이어 행동 연출 게이트 연결: 기물 연출 완료 후 적 턴을 시작합니다."); // 개발 로그 기록
        }

        private void HandlePlayerActionTransitionPending() // 플레이어 행동 완료 후 연출 대기 시작
        {
            if (!_isBound || _turnManager == null) return; // 연결 전 이벤트 방어

            if (_releaseCoroutine != null) StopCoroutine(_releaseCoroutine); // 중복 대기 코루틴 제거
            _releaseCoroutine = StartCoroutine(ReleaseEnemyTurnAfterVisual()); // 현재 배속이 반영되는 연출 대기 시작
        }

        private IEnumerator ReleaseEnemyTurnAfterVisual() // 공격 상승·접근·복귀·착지가 보인 뒤 EnemyTurn 시작
        {
            yield return new WaitForSeconds(PlayerVisualSettleSeconds); // Time.timeScale에 따라 1·2·3배속과 동일하게 느려지는 연출 대기

            _releaseCoroutine = null; // 대기 코루틴 참조 초기화

            if (_turnManager == null || !_turnManager.IsPlayerActionTransitionPending) yield break; // 전투 종료·상태 변경 시 늦은 전환 차단

            if (_turnManager.ReleaseDeferredPlayerActionTransition()) Debug.Log("44일차 플레이어 연출 완료 -> EnemyTurn 시작"); // 실제 AI 턴 진입 기록
        }

        private void OnDestroy() // 씬 종료 시 이벤트·대기 상태 정리
        {
            if (_releaseCoroutine != null) StopCoroutine(_releaseCoroutine); // 진행 중 대기 코루틴 정리

            if (_turnManager != null)
            {
                _turnManager.PlayerActionTransitionPending -= HandlePlayerActionTransitionPending; // 이벤트 구독 해제
                _turnManager.SetPlayerActionTransitionDeferred(false); // 런타임 지연 모드 해제
            }

            _releaseCoroutine = null; // 코루틴 참조 초기화
            _isBound = false; // 연결 상태 초기화
        }
    }
}
