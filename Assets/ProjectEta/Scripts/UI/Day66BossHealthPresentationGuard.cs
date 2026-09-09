using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // Day66MapPresentationRules 사용
using ProjectEta.Boss; // BossHealthUI 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(22000)]
    public sealed class Day66BossHealthPresentationGuard : MonoBehaviour
    {
        private BattleController _battleController; // 현재 RunState 제공 전투 컨트롤러
        private BossHealthUI _bossHealthUI; // 실제 보스 체력 UI

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 Battle 씬 여부 확인
            {
                return; // 다른 씬 자동 생성 차단
            }

            if (Object.FindFirstObjectByType<Day66BossHealthPresentationGuard>() != null) // 기존 Guard 확인
            {
                return; // 중복 생성 차단
            }

            GameObject host = new GameObject("Day66BossHealthPresentationGuard"); // 보스바 상태 Guard 생성
            host.AddComponent<Day66BossHealthPresentationGuard>(); // 전환 상태 보스바 제어 추가
        }

        private void LateUpdate()
        {
            ResolveReferences(); // 최신 전투·보스 UI 참조 확보

            if (_bossHealthUI == null) // 보스 UI 생성 여부 확인
            {
                return; // 아직 생성되지 않았으면 다음 프레임 재시도
            }

            if (_battleController == null || _battleController.RunState == null) // 유효한 런 상태 확인
            {
                _bossHealthUI.Hide(); // 런 상태가 없으면 잔존 보스바 제거
                return; // 추가 판정 종료
            }

            bool visible = Day66MapPresentationRules.ShouldShowBossHealth(_battleController.RunState.CurrentBoardMode, _battleController.RunState.CurrentFlowPhase); // 현재 화면에서 보스바 허용 여부 판정

            if (!visible) // 전투 화면 외 상태 확인
            {
                _bossHealthUI.Hide(); // 지도·보상·상점·이벤트에서 이전 보스바 강제 숨김
            }
        }

        private void ResolveReferences()
        {
            if (_battleController == null) // BattleController 캐시 확인
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색
            }

            if (_bossHealthUI == null) // BossHealthUI 캐시 확인
            {
                _bossHealthUI = Object.FindFirstObjectByType<BossHealthUI>(); // 현재 보스 체력 UI 탐색
            }
        }
    }
}
