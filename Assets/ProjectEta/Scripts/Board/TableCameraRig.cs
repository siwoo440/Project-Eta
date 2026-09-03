using UnityEngine; // MonoBehaviour, Transform 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System(Keyboard)을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class TableCameraRig : MonoBehaviour // 보드를 내려다보는 카메라 각도를 실행 중 조절해 가독성을 검증하는 컴포넌트
    {
        [SerializeField] private float _distance = 13f; // 보드 중심에서 카메라까지의 거리
        [SerializeField] private float _minPitchDegrees = 45f; // W/S로 전환할 최소 각도
        [SerializeField] private float _maxPitchDegrees = 60f; // W/S로 전환할 최대 각도
        [SerializeField] private float _snapDurationSeconds = 0.25f; // 최소~최대 각도 사이를 이동하는 데 걸리는 시간

        private float _pitchDegrees; // 현재 카메라 각도
        private float _targetPitchDegrees; // 지금 향하고 있는 목표 각도
        private float _pitchVelocity; // SmoothDamp 계산용 내부 속도 값

        private void Awake() // 씬 시작 시 자동 호출되는 초기화 메서드
        {
            _pitchDegrees = _minPitchDegrees; // 시작 각도는 최소값으로 설정
            _targetPitchDegrees = _minPitchDegrees; // 목표 각도도 최소값으로 시작
        }

        private void Update() // 매 프레임 자동 호출되는 메서드
        {
            HandlePitchInput(); // W/S 키로 목표 각도 전환
            ApplyTransform(); // 계산된 각도로 카메라 위치·회전 갱신
        }

        private void HandlePitchInput() // W/S 키로 목표 각도를 전환하는 메서드
        {
            if (Keyboard.current == null) // 키보드가 없으면
            {
                return; // 처리할 수 없으므로 종료
            }

            if (Keyboard.current.wKey.wasPressedThisFrame) // 이번 프레임에 W 키를 눌렀으면
            {
                _targetPitchDegrees = _maxPitchDegrees; // 목표 각도를 최대값으로 전환
            }

            if (Keyboard.current.sKey.wasPressedThisFrame) // 이번 프레임에 S 키를 눌렀으면
            {
                _targetPitchDegrees = _minPitchDegrees; // 목표 각도를 최소값으로 전환
            }

            float smoothTime = _snapDurationSeconds / 3f; // SmoothDamp가 받는 smoothTime을 체감 지속시간에 맞게 환산
            _pitchDegrees = Mathf.SmoothDamp(_pitchDegrees, _targetPitchDegrees, ref _pitchVelocity, smoothTime); // 목표 각도까지 빠르게 감속하며 이동
        }

        private void ApplyTransform() // 현재 각도를 실제 카메라 위치·회전에 반영하는 메서드
        {
            float pitchRadians = _pitchDegrees * Mathf.Deg2Rad; // 각도를 라디안으로 변환
            float height = Mathf.Sin(pitchRadians) * _distance; // 높이(Y) 성분 계산
            float depth = Mathf.Cos(pitchRadians) * _distance; // 깊이(Z) 성분 계산
            transform.localPosition = new Vector3(0f, height, -depth); // 보드 중심을 기준으로 카메라 위치 지정
            transform.localRotation = Quaternion.Euler(_pitchDegrees, 0f, 0f); // 각도만큼 아래를 보도록 회전 지정
        }

        private void OnGUI() // 화면에 현재 각도를 표시하는 메서드
        {
            GUI.Label(new Rect(10, 70, 420, 20), $"카메라 각도: {_pitchDegrees:F1}도 (W: {_maxPitchDegrees:F0}도로, S: {_minPitchDegrees:F0}도로 빠르게 전환)"); // 조작 안내와 현재 값 표시
        }
    }
}
