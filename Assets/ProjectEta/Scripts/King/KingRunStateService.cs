using System.Collections.Generic; // Dictionary<T> 사용
using UnityEngine; // RuntimeInitializeOnLoadMethod 사용
using ProjectEta.Run; // RunState 사용

namespace ProjectEta.King
{
    public static class KingRunStateService
    {
        private static readonly Dictionary<RunState, KingRunState> States = new Dictionary<RunState, KingRunState>(); // 런별 킹 상태 저장소

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            States.Clear(); // 플레이 세션 시작 시 이전 런 참조 제거
        }

        public static KingRunState Get(RunState runState)
        {
            if (runState == null) return null; // 잘못된 런 상태 차단

            if (!States.TryGetValue(runState, out KingRunState state))
            {
                state = new KingRunState(); // 신규 런 킹 상태 생성
                States.Add(runState, state); // RunState 기준 킹 상태 등록
            }

            return state; // 런 전체 공유 킹 상태 반환
        }

        public static void Remove(RunState runState)
        {
            if (runState == null) return; // 잘못된 런 상태 차단
            States.Remove(runState); // 종료 런 킹 상태 제거
        }
    }
}
