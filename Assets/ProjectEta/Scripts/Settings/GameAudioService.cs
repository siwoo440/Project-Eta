using System; // Action 이벤트 사용
using UnityEngine; // AudioListener·Mathf 사용

namespace ProjectEta.Settings
{
    public enum AudioChannel
    {
        Bgm = 0, // 배경 음악 채널
        Sfx = 1 // 효과음 채널
    }

    public static class GameAudioService
    {
        private static float _masterVolume = GameSettingsData.DefaultMasterVolume; // 현재 Master 볼륨
        private static float _bgmVolume = GameSettingsData.DefaultBgmVolume; // 현재 BGM 볼륨
        private static float _sfxVolume = GameSettingsData.DefaultSfxVolume; // 현재 SFX 볼륨

        public static float MasterVolume => _masterVolume; // 현재 Master 볼륨 조회
        public static float BgmVolume => _bgmVolume; // 현재 BGM 볼륨 조회
        public static float SfxVolume => _sfxVolume; // 현재 SFX 볼륨 조회

        public static event Action VolumesChanged; // 오디오 출력 설정 변경 이벤트

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ResetRuntimeState(); // Domain Reload 비활성 환경 오디오 상태 초기화
        }

        public static void Apply(GameSettingsData data)
        {
            if (data == null)
            {
                Apply(GameSettingsData.DefaultMasterVolume, GameSettingsData.DefaultBgmVolume, GameSettingsData.DefaultSfxVolume); // null 설정 기본 볼륨 적용
                return; // 기본값 적용 후 종료
            }

            Apply(data.MasterVolume, data.BgmVolume, data.SfxVolume); // 설정 데이터 오디오 값 적용
        }

        public static void Apply(float masterVolume, float bgmVolume, float sfxVolume)
        {
            _masterVolume = Mathf.Clamp01(masterVolume); // Master 볼륨 안전 범위 적용
            _bgmVolume = Mathf.Clamp01(bgmVolume); // BGM 볼륨 안전 범위 적용
            _sfxVolume = Mathf.Clamp01(sfxVolume); // SFX 볼륨 안전 범위 적용
            AudioListener.volume = _masterVolume; // 전체 Unity 오디오 Master 볼륨 적용
            VolumesChanged?.Invoke(); // 등록 AudioSource 채널 볼륨 갱신 알림
        }

        public static float GetChannelVolume(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Bgm:
                    return _bgmVolume; // BGM 채널 볼륨 반환
                case AudioChannel.Sfx:
                    return _sfxVolume; // SFX 채널 볼륨 반환
                default:
                    return 1f; // 알 수 없는 채널 기본 볼륨 반환
            }
        }

        public static float CalculateEffectiveVolume(float masterVolume, float channelVolume)
        {
            return Mathf.Clamp01(masterVolume) * Mathf.Clamp01(channelVolume); // Master·채널 최종 볼륨 비율 계산
        }

        public static void ResetRuntimeState()
        {
            _masterVolume = GameSettingsData.DefaultMasterVolume; // Master 기본값 복원
            _bgmVolume = GameSettingsData.DefaultBgmVolume; // BGM 기본값 복원
            _sfxVolume = GameSettingsData.DefaultSfxVolume; // SFX 기본값 복원
            AudioListener.volume = _masterVolume; // Unity Master 볼륨 복원
            VolumesChanged = null; // 이전 플레이 세션 구독 정리
        }
    }
}
