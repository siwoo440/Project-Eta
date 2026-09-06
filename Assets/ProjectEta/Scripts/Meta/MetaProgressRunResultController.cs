using System.Collections; // 초기화 대기 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·Mathf 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Run; // RunState·RunFlowPhase·RoundState 사용

namespace ProjectEta.Meta
{
    [DefaultExecutionOrder(1050)]
    public sealed class MetaProgressRunResultController : MonoBehaviour
    {
        private BattleController _battleController; // 현재 런 소유 전투 컨트롤러
        private RunState _runState; // 현재 감시 런 상태
        private MetaProgressUI _progressUI; // 런 종료 메타 보상·해금 UI
        private bool _rewardGranted; // 현재 RunState 종료 보상 중복 지급 차단

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<MetaProgressRunResultController>() != null) return; // 중복 관리자 생성 차단
            var host = new GameObject("MetaProgressController_Day48"); // 48일차 영구 성장 호스트 생성
            host.AddComponent<MetaProgressRunResultController>(); // 런 종료 메타 진행 관리자 추가
        }

        private IEnumerator Start()
        {
            const int maxWaitFrames = 240; // BattleController 준비 최대 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 전투 컨트롤러 탐색

                if (_battleController != null && _battleController.RunState != null)
                {
                    BindRunState(_battleController.RunState); // 현재 런 상태 감시 시작
                    _progressUI = GetComponent<MetaProgressUI>(); // 같은 호스트의 기존 메타 UI 탐색
                    if (_progressUI == null) _progressUI = gameObject.AddComponent<MetaProgressUI>(); // 없으면 메타 결과 UI 자동 추가
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("48일차 MetaProgress 초기화 실패: BattleController와 RunState를 확인하세요."); // 초기화 실패 기록
        }

        private void Update()
        {
            if (_battleController == null) return; // 전투 컨트롤러 누락 방어

            if (_battleController.RunState != _runState)
            {
                BindRunState(_battleController.RunState); // 새 RunState 감시 상태 재설정
            }

            if (_runState == null || _rewardGranted) return; // 런 누락·이미 지급된 결과 차단

            RunFlowPhase phase = _runState.CurrentFlowPhase; // 현재 로그라이트 상위 진행 단계 조회
            if (phase != RunFlowPhase.Completed && phase != RunFlowPhase.Failed) return; // 승리·실패 종료 전 지급 차단
            GrantMetaReward(phase == RunFlowPhase.Completed); // 런 종료 메타 보상 지급
        }

        private void BindRunState(RunState runState)
        {
            _runState = runState; // 현재 감시 런 상태 교체
            _rewardGranted = false; // 새 런 종료 보상 지급 가능 상태 복구
            if (_progressUI != null) _progressUI.Hide(); // 이전 런 메타 결과 화면 정리
        }

        private void GrantMetaReward(bool completed)
        {
            if (_runState == null || _rewardGranted) return; // 런 누락·중복 지급 차단
            _rewardGranted = true; // 저장 전 중복 지급 플래그 선점

            int reachedStage = Mathf.Clamp(_runState.CurrentRound, RoundState.FirstRound, RoundState.FinalRound); // 현재 도달 단계 보정
            bool midBossDefeated = completed || reachedStage > 5; // 5단계를 넘겼다면 중간 보스 처치 판정
            bool finalBossDefeated = completed && reachedStage >= RoundState.FinalRound; // 최종 클리어일 때 최종 보스 처치 판정
            int earnedTokens = MetaRewardCalculator.Calculate(reachedStage, midBossDefeated, finalBossDefeated, completed); // 이번 런 메타 보상 계산

            MetaProgressState progress = MetaProgressService.Current; // 런 밖 영구 진행 상태 로드
            progress.AddTokens(earnedTokens); // 이번 런 메타 토큰 영구 잔액 반영
            bool saved = MetaProgressService.Save(); // 지급 결과 즉시 디스크 저장

            if (_progressUI == null)
            {
                _progressUI = GetComponent<MetaProgressUI>(); // 현재 호스트 메타 UI 재탐색
                if (_progressUI == null) _progressUI = gameObject.AddComponent<MetaProgressUI>(); // 누락 시 메타 UI 자동 추가
            }

            _progressUI.Show(progress, earnedTokens, reachedStage, completed); // 런 결과·영구 해금 UI 표시
            Debug.Log($"48일차 메타 보상 지급: Stage={reachedStage} / Completed={completed} / +{earnedTokens} / Total={progress.MetaTokens} / Saved={saved}"); // 영구 보상 결과 기록
        }
    }
}
