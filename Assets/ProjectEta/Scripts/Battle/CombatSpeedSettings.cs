using System; // Action 이벤트 사용
using UnityEngine; // Time.timeScale·RuntimeInitializeOnLoadMethod 사용

namespace ProjectEta.Battle // 전투 공통 배속 상태 네임스페이스
{
    public static class CombatSpeedSettings // 현재 기존 전투 속도를 3배속 기준으로 취급하는 런타임 배속 설정
    {
        public const int MinimumSpeed = 1; // 최소 선택 배속
        public const int MaximumSpeed = 3; // 최대 선택 배속
        public const int DefaultSpeed = 3; // 현재 프로젝트의 기존 속도를 3배속 기준으로 사용

        private static int _currentSpeed = DefaultSpeed; // 현재 선택 배속

        public static int CurrentSpeed => _currentSpeed; // 현재 배속 공개
        public static float CurrentTimeScale => SpeedToTimeScale(_currentSpeed); // Unity 실제 시간 배율 공개
        public static event Action<int> SpeedChanged; // 배속 선택 변경 알림

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // Play Mode 시작 시 정적 상태 초기화
        private static void ResetRuntimeState() // 이전 Play 세션 배속 잔존 방지
        {
            _currentSpeed = DefaultSpeed; // 기본 3배속 복구
            Time.timeScale = 1f; // 기존 프로젝트 실제 속도 유지
            SpeedChanged = null; // 이전 세션 이벤트 구독 제거
        }

        public static bool TrySetSpeed(int speed) // 1·2·3배속 선택 적용
        {
            if (speed < MinimumSpeed || speed > MaximumSpeed) return false; // 지원하지 않는 배속 차단

            _currentSpeed = speed; // 현재 선택 값 갱신
            Time.timeScale = SpeedToTimeScale(speed); // 기존 속도=3배속 기준 실제 시간 배율 적용
            SpeedChanged?.Invoke(_currentSpeed); // UI 등 구독자에 변경 알림
            return true; // 정상 적용 반환
        }

        public static int CycleToNextSpeed() // 1→2→3→1 순서로 다음 배속 선택
        {
            int nextSpeed = _currentSpeed >= MaximumSpeed ? MinimumSpeed : _currentSpeed + 1; // 현재 배속 다음 단계 계산
            TrySetSpeed(nextSpeed); // 계산된 다음 배속 적용
            return _currentSpeed; // 적용된 배속 반환
        }

        public static void ResetToDefault() // 테스트·개발용 기본 속도 복구
        {
            TrySetSpeed(DefaultSpeed); // 3배속 기준으로 복구
        }

        public static float SpeedToTimeScale(int speed) // 표시 배속을 Unity Time.timeScale로 변환
        {
            int clamped = Mathf.Clamp(speed, MinimumSpeed, MaximumSpeed); // 안전한 1~3 범위 보정
            return clamped / (float)DefaultSpeed; // 1배=0.333, 2배=0.667, 3배=1.0 반환
        }
    }
}
