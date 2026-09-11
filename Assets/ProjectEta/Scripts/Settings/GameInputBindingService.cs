using System; // Enum 파싱 사용
using UnityEngine.InputSystem; // Keyboard·Key 사용

namespace ProjectEta.Settings
{
    public enum GameInputAction
    {
        CompleteAction = 0, // 배치 턴 종료·플레이어 행동 완료
        Pause = 1 // Pause·뒤로가기
    }

    public static class GameInputBindingRules
    {
        public const string DefaultCompleteActionKey = "Space"; // 기본 행동 완료 키 이름
        public const string DefaultPauseKey = "Escape"; // 기본 Pause 키 이름

        public static string NormalizeKeyName(string keyName, string fallbackKeyName)
        {
            if (TryParseKey(keyName, out Key key)) return key.ToString(); // 유효 키 이름을 표준 이름으로 정규화
            if (TryParseKey(fallbackKeyName, out Key fallbackKey)) return fallbackKey.ToString(); // 지정 기본 키 이름 사용
            return DefaultCompleteActionKey; // 잘못된 기본값까지 들어온 경우 Space 사용
        }

        public static bool TryParseKey(string keyName, out Key key)
        {
            key = Key.None; // 실패 기본값 초기화
            if (string.IsNullOrWhiteSpace(keyName)) return false; // 빈 키 이름 차단
            if (!Enum.TryParse(keyName.Trim(), true, out key)) return false; // Key 열거형 변환 실패 차단
            return key != Key.None; // 입력에 사용할 수 없는 None 제외
        }

        public static string GetDisplayName(Key key)
        {
            switch (key)
            {
                case Key.Space:
                    return "Space"; // Space 기본 표기
                case Key.Escape:
                    return "ESC"; // Escape 조작법 축약 표기
                case Key.Enter:
                    return "Enter"; // Enter 표기
                case Key.Tab:
                    return "Tab"; // Tab 표기
                case Key.Backspace:
                    return "Backspace"; // Backspace 표기
                case Key.LeftArrow:
                    return "←"; // 왼쪽 방향키 표기
                case Key.RightArrow:
                    return "→"; // 오른쪽 방향키 표기
                case Key.UpArrow:
                    return "↑"; // 위쪽 방향키 표기
                case Key.DownArrow:
                    return "↓"; // 아래쪽 방향키 표기
                case Key.LeftShift:
                    return "L Shift"; // 왼쪽 Shift 표기
                case Key.RightShift:
                    return "R Shift"; // 오른쪽 Shift 표기
                case Key.LeftCtrl:
                    return "L Ctrl"; // 왼쪽 Ctrl 표기
                case Key.RightCtrl:
                    return "R Ctrl"; // 오른쪽 Ctrl 표기
                case Key.LeftAlt:
                    return "L Alt"; // 왼쪽 Alt 표기
                case Key.RightAlt:
                    return "R Alt"; // 오른쪽 Alt 표기
            }

            string name = key.ToString(); // 기본 Key 이름 조회
            if (name.StartsWith("Digit", StringComparison.Ordinal) && name.Length > 5) return name.Substring(5); // 숫자열 키 간단 표기
            return name.Length == 1 ? name.ToUpperInvariant() : name; // 문자 키 대문자·기타 키 원본 이름 사용
        }
    }

    public static class GameInputBindingService
    {
        private static Key _completeActionKey = Key.Space; // 현재 행동 완료 키
        private static Key _pauseKey = Key.Escape; // 현재 Pause 키
        private static bool _initialized; // 설정 적용 완료 여부

        public static void Apply(GameSettingsData data)
        {
            GameSettingsData settings = (data ?? GameSettingsData.CreateDefault()).Normalized(); // 적용 설정 안전 보정
            _completeActionKey = ResolveKey(settings.CompleteActionKey, Key.Space); // 행동 완료 키 적용
            _pauseKey = ResolveKey(settings.PauseKey, Key.Escape); // Pause 키 적용
            _initialized = true; // 조작키 적용 상태 기록
        }

        public static Key GetKey(GameInputAction action)
        {
            EnsureInitialized(); // 설정 기반 조작키 준비 보장
            return action == GameInputAction.Pause ? _pauseKey : _completeActionKey; // 요청 행동의 현재 키 반환
        }

        public static string GetDisplayName(GameInputAction action)
        {
            return GameInputBindingRules.GetDisplayName(GetKey(action)); // 현재 조작키 사용자 표시 문구 반환
        }

        public static bool WasPressedThisFrame(GameInputAction action)
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회
            if (keyboard == null) return false; // 키보드 미연결 상태 차단
            Key key = GetKey(action); // 현재 행동에 연결된 키 조회
            return keyboard[key].wasPressedThisFrame; // 현재 프레임 입력 여부 반환
        }

        public static void ResetRuntimeState()
        {
            _completeActionKey = Key.Space; // 행동 완료 기본값 복원
            _pauseKey = Key.Escape; // Pause 기본값 복원
            _initialized = false; // 다음 사용 시 저장 설정 재적용 허용
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return; // 기존 적용값 재사용
            GameSettingsService.EnsureLoaded(); // 저장 설정 로드와 런타임 조작키 적용 요청
            if (!_initialized) Apply(GameSettingsData.CreateDefault()); // 설정 서비스 예외 시 기본값 보장
        }

        private static Key ResolveKey(string keyName, Key fallback)
        {
            if (GameInputBindingRules.TryParseKey(keyName, out Key key)) return key; // 저장된 유효 키 사용
            return fallback; // 잘못된 저장값 기본 키로 복구
        }
    }
}
