using UnityEngine; // MonoBehaviour·AudioSource·Mathf 사용

namespace ProjectEta.Settings
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioChannelVolumeBinding : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource; // 볼륨을 적용할 AudioSource
        [SerializeField] private AudioChannel _channel = AudioChannel.Sfx; // 적용 오디오 채널
        [SerializeField] private float _baseVolume = -1f; // 설정 적용 전 원본 AudioSource 볼륨

        public AudioChannel Channel => _channel; // 현재 바인딩 채널 조회

        private void Awake()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>(); // 같은 오브젝트 AudioSource 자동 연결
            if (_audioSource != null && _baseVolume < 0f) _baseVolume = _audioSource.volume; // 최초 원본 볼륨 저장
        }

        private void OnEnable()
        {
            GameAudioService.VolumesChanged += ApplyCurrentVolume; // 오디오 설정 변경 이벤트 구독
            ApplyCurrentVolume(); // 활성 시 현재 채널 볼륨 즉시 적용
        }

        private void OnDisable()
        {
            GameAudioService.VolumesChanged -= ApplyCurrentVolume; // 비활성 시 오디오 설정 이벤트 구독 해제
        }

        public void SetChannel(AudioChannel channel)
        {
            _channel = channel; // 런타임 오디오 채널 변경
            ApplyCurrentVolume(); // 변경 채널 볼륨 즉시 적용
        }

        private void ApplyCurrentVolume()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>(); // 누락 AudioSource 재탐색
            if (_audioSource == null) return; // AudioSource 누락 방어
            if (_baseVolume < 0f) _baseVolume = _audioSource.volume; // 원본 볼륨 지연 저장

            float baseVolume = Mathf.Clamp01(_baseVolume); // 원본 AudioSource 볼륨 안전 보정
            float channelVolume = GameAudioService.GetChannelVolume(_channel); // 현재 채널 설정 볼륨 조회
            _audioSource.volume = baseVolume * channelVolume; // 채널 볼륨을 AudioSource에 적용
        }
    }
}
