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

        public static KingRunState Restore(RunState runState, KingArchetype archetype)
        {
            KingRunState state = Get(runState); // 복원 대상 런 킹 상태 조회
            if (state == null) return null; // 런 상태 누락 방어

            if ((int)archetype < (int)KingArchetype.Default || (int)archetype > (int)KingArchetype.Strategy)
            {
                archetype = KingArchetype.Default; // 저장 값 범위 밖 기본 킹 보정
            }

            if (state.Archetype != archetype) state.Select(archetype); // 저장 킹 타입 적용
            state.ResetBattleScopedState(); // 전투 한정 격노·방벽·선택 상태는 복원하지 않음
            return state; // 복원 킹 상태 반환
        }

        public static bool TryGet(RunState runState, out KingRunState state)
        {
            if (runState == null)
            {
                state = null; // 잘못된 런 상태 결과 초기화
                return false; // 조회 실패 반환
            }

            return States.TryGetValue(runState, out state); // 등록 킹 상태 조회
        }

        public static void Remove(RunState runState)
        {
            if (runState == null) return; // 잘못된 런 상태 차단
            States.Remove(runState); // 종료 런 킹 상태 제거
        }
    }
}
