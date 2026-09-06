using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using ProjectEta.Battle; // BattleController·BattleHooks·CombatResult·DamageContext 사용
using ProjectEta.Run; // RunState·RunFlowPhase 사용

namespace ProjectEta.King
{
    [DefaultExecutionOrder(1060)]
    public sealed class KingAbilityController : MonoBehaviour
    {
        private BattleController _battleController; // 현재 전투 컨트롤러
        private RunState _runState; // 현재 런 상태
        private KingRunState _kingState; // 현재 런 킹 상태
        private BattleHooks _boundHooks; // 현재 구독 전투 훅
        private RunFlowPhase _previousFlowPhase; // 직전 런 진행 단계
        private bool _hasPreviousFlowPhase; // 직전 진행 단계 존재 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<KingAbilityController>() != null) return; // 중복 킹 능력 관리자 차단

            var host = new GameObject("KingAbilityController_Day49"); // 49일차 킹 능력 호스트 생성
            host.AddComponent<KingAbilityController>(); // 킹 능력 훅 관리자 추가
            host.AddComponent<KingSelectionUI>(); // 첫 런 킹 선택·상태 UI 추가
        }

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 탐색
            RebindRunStateIfNeeded(); // RunState 변경 대응
            RebindHooksIfNeeded(); // BattleHooks 변경 대응
            TrackRunFlow(); // 전투 종료 시 전투 한정 킹 상태 초기화
        }

        private void ResolveBattleController()
        {
            if (_battleController != null) return; // 기존 전투 컨트롤러 재사용
            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색
        }

        private void RebindRunStateIfNeeded()
        {
            RunState current = _battleController != null ? _battleController.RunState : null; // 현재 BattleController 런 조회
            if (current == _runState) return; // 동일 RunState 재연결 차단

            _runState = current; // 현재 런 상태 교체
            _kingState = KingRunStateService.Get(_runState); // 런 전체 킹 상태 연결
            _hasPreviousFlowPhase = false; // 새 런 진행 단계 추적 초기화
        }

        private void RebindHooksIfNeeded()
        {
            BattleHooks current = _battleController != null ? _battleController.BattleHooks : null; // 현재 전투 훅 조회
            if (current == _boundHooks) return; // 동일 훅 재구독 차단

            UnsubscribeHooks(); // 이전 전투 훅 구독 해제
            _boundHooks = current; // 새 전투 훅 저장

            if (_boundHooks == null) return; // 훅 누락 상태 종료
            _boundHooks.BeforeDamage += HandleBeforeDamage; // 공격형 킹 다음 공격 피해 보너스 연결
            _boundHooks.AfterAttack += HandleAfterAttack; // 공격형 킹 직접 처치 격노 연결
        }

        private void HandleBeforeDamage(DamageContext context)
        {
            int bonus = AttackKingAbility.ApplyBeforeDamage(_kingState, context); // 공격형 킹 격노 피해 적용
            if (bonus > 0) Debug.Log($"공격형 킹 처형의 연쇄: 격노 {bonus}스택 소비 / 피해 +{bonus}"); // 격노 소비 결과 출력
        }

        private void HandleAfterAttack(CombatResult result)
        {
            bool granted = AttackKingAbility.HandleAfterAttack(_kingState, result); // 공격형 킹 직접 처치 격노 획득 처리
            if (granted) Debug.Log($"공격형 킹 처형의 연쇄: 격노 {_kingState.RageStacks}/{KingRunState.AttackRageMaxStacks}"); // 격노 획득 결과 출력
        }

        private void TrackRunFlow()
        {
            if (_runState == null || _kingState == null) return; // 런·킹 상태 누락 방어
            RunFlowPhase current = _runState.CurrentFlowPhase; // 현재 런 진행 단계 조회

            if (_hasPreviousFlowPhase && _previousFlowPhase == RunFlowPhase.Battle && current != RunFlowPhase.Battle)
            {
                _kingState.ResetBattleScopedState(); // 전투 종료 후 공격형 격노 초기화
            }

            _previousFlowPhase = current; // 현재 진행 단계 저장
            _hasPreviousFlowPhase = true; // 진행 단계 추적 활성화
        }

        private void UnsubscribeHooks()
        {
            if (_boundHooks == null) return; // 구독 훅 누락 방어
            _boundHooks.BeforeDamage -= HandleBeforeDamage; // 피해 훅 구독 해제
            _boundHooks.AfterAttack -= HandleAfterAttack; // 공격 종료 훅 구독 해제
        }

        private void OnDestroy()
        {
            UnsubscribeHooks(); // 오브젝트 제거 시 훅 구독 정리
        }
    }
}
