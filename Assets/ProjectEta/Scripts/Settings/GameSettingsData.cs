using System; // Serializable 사용
using UnityEngine; // FullScreenMode·Mathf 사용

namespace ProjectEta.Settings
{
    [Serializable]
    public sealed class GameSettingsData
    {
        public const int CurrentVersion = 4; // 69일차 조작키 설정 포함 저장 버전
        public const int MinimumWidth = 640; // 최소 지원 해상도 너비
        public const int MinimumHeight = 360; // 최소 지원 해상도 높이
        public const float MinimumUiScale = 0.75f; // 최소 UI 배율
        public const float MaximumUiScale = 1.35f; // 최대 UI 배율
        public const float DefaultUiScale = 1f; // 기본 UI 배율
        public const int DefaultWidth = 1920; // 기본 해상도 너비
        public const int DefaultHeight = 1080; // 기본 해상도 높이
        public const float DefaultMasterVolume = 1f; // 기본 Master 볼륨
        public const float DefaultBgmVolume = 1f; // 기본 BGM 볼륨
        public const float DefaultSfxVolume = 1f; // 기본 SFX 볼륨

        public int SettingsVersion; // 설정 저장 버전
        public int ResolutionWidth = DefaultWidth; // 저장 해상도 너비
        public int ResolutionHeight = DefaultHeight; // 저장 해상도 높이
        public int ScreenMode = (int)FullScreenMode.FullScreenWindow; // 저장 화면 모드
        public float UiScale = DefaultUiScale; // 저장 UI 배율
        public float MasterVolume = DefaultMasterVolume; // 저장 Master 볼륨
        public float BgmVolume = DefaultBgmVolume; // 저장 BGM 볼륨
        public float SfxVolume = DefaultSfxVolume; // 저장 SFX 볼륨
        public bool FirstTutorialCompleted; // 최초 전투 튜토리얼 완료 여부
        public string CompleteActionKey = GameInputBindingRules.DefaultCompleteActionKey; // 배치 종료·행동 완료 키 이름
        public string PauseKey = GameInputBindingRules.DefaultPauseKey; // Pause·뒤로가기 키 이름

        public static GameSettingsData CreateDefault()
        {
            return new GameSettingsData
            {
                SettingsVersion = CurrentVersion, // 최신 설정 버전 적용
                ResolutionWidth = DefaultWidth, // 기본 해상도 너비 적용
                ResolutionHeight = DefaultHeight, // 기본 해상도 높이 적용
                ScreenMode = (int)FullScreenMode.FullScreenWindow, // 기본 전체화면 창 적용
                UiScale = DefaultUiScale, // 기본 UI 배율 적용
                MasterVolume = DefaultMasterVolume, // 기본 Master 볼륨 적용
                BgmVolume = DefaultBgmVolume, // 기본 BGM 볼륨 적용
                SfxVolume = DefaultSfxVolume, // 기본 SFX 볼륨 적용
                FirstTutorialCompleted = false, // 최초 튜토리얼 미완료 적용
                CompleteActionKey = GameInputBindingRules.DefaultCompleteActionKey, // 기본 행동 완료 Space 적용
                PauseKey = GameInputBindingRules.DefaultPauseKey // 기본 Pause Escape 적용
            };
        }

        public GameSettingsData Clone()
        {
            return new GameSettingsData
            {
                SettingsVersion = SettingsVersion, // 설정 버전 복제
                ResolutionWidth = ResolutionWidth, // 해상도 너비 복제
                ResolutionHeight = ResolutionHeight, // 해상도 높이 복제
                ScreenMode = ScreenMode, // 화면 모드 복제
                UiScale = UiScale, // UI 배율 복제
                MasterVolume = MasterVolume, // Master 볼륨 복제
                BgmVolume = BgmVolume, // BGM 볼륨 복제
                SfxVolume = SfxVolume, // SFX 볼륨 복제
                FirstTutorialCompleted = FirstTutorialCompleted, // 튜토리얼 완료 상태 복제
                CompleteActionKey = CompleteActionKey, // 행동 완료 키 복제
                PauseKey = PauseKey // Pause 키 복제
            };
        }

        public GameSettingsData Normalized()
        {
            bool legacyAudioSettings = SettingsVersion < 2; // 56일차 이전 오디오 필드 누락 여부 판정
            bool tutorialCompleted = SettingsVersion >= 3 && FirstTutorialCompleted; // 68일차 이전 저장은 최초 튜토리얼 미완료 처리
            bool legacyControlSettings = SettingsVersion < 4; // 69일차 이전 조작키 필드 누락 여부 판정
            int width = Mathf.Max(MinimumWidth, ResolutionWidth); // 최소 너비 보정
            int height = Mathf.Max(MinimumHeight, ResolutionHeight); // 최소 높이 보정
            int mode = IsSupportedScreenMode(ScreenMode) ? ScreenMode : (int)FullScreenMode.FullScreenWindow; // 지원 화면 모드 보정
            float uiScale = Mathf.Clamp(UiScale, MinimumUiScale, MaximumUiScale); // UI 배율 안전 범위 보정
            float masterVolume = legacyAudioSettings ? DefaultMasterVolume : Mathf.Clamp01(MasterVolume); // Master 구버전 보정·범위 제한
            float bgmVolume = legacyAudioSettings ? DefaultBgmVolume : Mathf.Clamp01(BgmVolume); // BGM 구버전 보정·범위 제한
            float sfxVolume = legacyAudioSettings ? DefaultSfxVolume : Mathf.Clamp01(SfxVolume); // SFX 구버전 보정·범위 제한
            string completeActionKey = legacyControlSettings ? GameInputBindingRules.DefaultCompleteActionKey : GameInputBindingRules.NormalizeKeyName(CompleteActionKey, GameInputBindingRules.DefaultCompleteActionKey); // 행동 완료 키 보정
            string pauseKey = legacyControlSettings ? GameInputBindingRules.DefaultPauseKey : GameInputBindingRules.NormalizeKeyName(PauseKey, GameInputBindingRules.DefaultPauseKey); // Pause 키 보정

            return new GameSettingsData
            {
                SettingsVersion = CurrentVersion, // 최신 설정 버전 저장
                ResolutionWidth = width, // 보정 너비 저장
                ResolutionHeight = height, // 보정 높이 저장
                ScreenMode = mode, // 보정 화면 모드 저장
                UiScale = uiScale, // 보정 UI 배율 저장
                MasterVolume = masterVolume, // 보정 Master 볼륨 저장
                BgmVolume = bgmVolume, // 보정 BGM 볼륨 저장
                SfxVolume = sfxVolume, // 보정 SFX 볼륨 저장
                FirstTutorialCompleted = tutorialCompleted, // 보정 튜토리얼 완료 상태 저장
                CompleteActionKey = completeActionKey, // 보정 행동 완료 키 저장
                PauseKey = pauseKey // 보정 Pause 키 저장
            };
        }

        public bool ContentEquals(GameSettingsData other)
        {
            if (other == null) return false; // 비교 대상 누락 차단
            if (ResolutionWidth != other.ResolutionWidth) return false; // 너비 차이 확인
            if (ResolutionHeight != other.ResolutionHeight) return false; // 높이 차이 확인
            if (ScreenMode != other.ScreenMode) return false; // 화면 모드 차이 확인
            if (Mathf.Abs(UiScale - other.UiScale) >= 0.001f) return false; // UI 배율 차이 확인
            if (Mathf.Abs(MasterVolume - other.MasterVolume) >= 0.001f) return false; // Master 볼륨 차이 확인
            if (Mathf.Abs(BgmVolume - other.BgmVolume) >= 0.001f) return false; // BGM 볼륨 차이 확인
            if (Mathf.Abs(SfxVolume - other.SfxVolume) >= 0.001f) return false; // SFX 볼륨 차이 확인
            if (FirstTutorialCompleted != other.FirstTutorialCompleted) return false; // 튜토리얼 완료 상태 차이 확인
            if (!string.Equals(CompleteActionKey, other.CompleteActionKey, StringComparison.Ordinal)) return false; // 행동 완료 키 차이 확인
            return string.Equals(PauseKey, other.PauseKey, StringComparison.Ordinal); // Pause 키 동일 여부 반환
        }

        public static bool IsSupportedScreenMode(int mode)
        {
            return mode == (int)FullScreenMode.FullScreenWindow || mode == (int)FullScreenMode.Windowed; // 지원 화면 모드 확인
        }
    }
}
