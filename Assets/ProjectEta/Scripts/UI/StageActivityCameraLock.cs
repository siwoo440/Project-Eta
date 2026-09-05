using System; // Type 사용
using System.Reflection; // 기존 카메라 컨트롤러 필드 탐색
using UnityEngine; // Camera·MonoBehaviour·Vector3·Quaternion 사용
using ProjectEta.Board; // BoardView 사용

namespace ProjectEta.UI
{
    public static class StageActivityCameraPoseUtility
    {
        private const float PrimaryCameraHeightTiles = 9f; // Battle 씬 1번 카메라 높이
        private const float PrimaryCameraDepthTiles = -9f; // Battle 씬 1번 카메라 플레이어 측 거리
        private const float CanvasAnchorHeightTiles = 0.22f; // 돗자리 위 UI 하단 높이
        private const float CanvasAnchorDepthTiles = -2.75f; // 플레이어 쪽 UI 하단 위치

        public static Vector3 ResolvePrimaryCameraLocalPosition(float tileSize)
        {
            float safeTileSize = Mathf.Max(0.1f, tileSize); // 타일 크기 안전 보정
            return new Vector3(0f, PrimaryCameraHeightTiles * safeTileSize, PrimaryCameraDepthTiles * safeTileSize); // 1번 카메라 로컬 위치 반환
        }

        public static Quaternion ResolvePrimaryCameraLocalRotation()
        {
            return Quaternion.Euler(45f, 0f, 0f); // Battle 씬 1번 카메라 기본 45도 시점 반환
        }

        public static Vector3 ResolveCanvasLocalAnchor(float tileSize)
        {
            float safeTileSize = Mathf.Max(0.1f, tileSize); // 타일 크기 안전 보정
            return new Vector3(0f, CanvasAnchorHeightTiles * safeTileSize, CanvasAnchorDepthTiles * safeTileSize); // 플레이어 쪽 돗자리 하단 UI 위치 반환
        }

        public static Quaternion ResolveCanvasLocalRotation(float tileSize)
        {
            Vector3 cameraPosition = ResolvePrimaryCameraLocalPosition(tileSize); // 고정 카메라 위치 계산
            Vector3 canvasPosition = ResolveCanvasLocalAnchor(tileSize); // 고정 UI 위치 계산
            Vector3 toCamera = (cameraPosition - canvasPosition).normalized; // UI에서 카메라 방향 계산
            Vector3 textTop = Vector3.ProjectOnPlane(Vector3.forward, toCamera); // 보드 안쪽을 향하는 글자 윗방향 계산

            if (textTop.sqrMagnitude <= 0.0001f) textTop = Vector3.up; // 특수 각도 글자 방향 보정
            textTop.Normalize(); // 글자 윗방향 정규화

            return Quaternion.LookRotation(-toCamera, textTop); // 로컬 -Z UI 앞면이 1번 카메라를 향하는 고정 회전 반환
        }
    }

    public sealed class StageActivityCameraLock : MonoBehaviour
    {
        private static readonly BindingFlags CameraFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic; // 카메라 컨트롤러 필드 조회 범위

        private BoardView _boardView; // 고정 자세 기준 보드
        private Camera _targetCamera; // 실제 Main Camera
        private MonoBehaviour _cameraInputController; // W/S 등 기존 카메라 조작 컴포넌트
        private Vector3 _previousPosition; // Shop/Event 진입 전 카메라 위치
        private Quaternion _previousRotation; // Shop/Event 진입 전 카메라 회전
        private bool _cameraInputWasEnabled; // 기존 카메라 조작 활성 상태
        private bool _isLocked; // 현재 1번 카메라 고정 여부

        public bool IsLocked => _isLocked; // 외부 고정 상태 확인

        public void Initialize(BoardView boardView)
        {
            _boardView = boardView; // 기준 보드 저장
        }

        public bool LockToPrimaryView()
        {
            if (_isLocked) return true; // 이미 고정된 경우 중복 저장 차단
            if (_boardView == null) return false; // 기준 보드 누락 차단

            _targetCamera = Camera.main; // Main Camera 우선 조회
            if (_targetCamera == null) _targetCamera = UnityEngine.Object.FindFirstObjectByType<Camera>(); // 보조 카메라 탐색
            if (_targetCamera == null) return false; // 카메라 누락 차단

            _previousPosition = _targetCamera.transform.position; // 기존 카메라 위치 저장
            _previousRotation = _targetCamera.transform.rotation; // 기존 카메라 회전 저장
            _cameraInputController = FindCameraInputController(_targetCamera); // 기존 W/S 카메라 조작기 탐색

            if (_cameraInputController != null)
            {
                _cameraInputWasEnabled = _cameraInputController.enabled; // 기존 활성 상태 저장
                _cameraInputController.StopAllCoroutines(); // 진행 중 카메라 스냅 연출 정지
                _cameraInputController.enabled = false; // Shop/Event 동안 카메라 조작 잠금
            }

            _isLocked = true; // 강제 고정 상태 기록
            ApplyPrimaryPose(); // 즉시 1번 카메라 자세 적용
            return true; // 카메라 고정 성공 반환
        }

        public void RestorePreviousView()
        {
            if (!_isLocked) return; // 고정되지 않은 경우 복원 생략

            if (_targetCamera != null)
            {
                _targetCamera.transform.position = _previousPosition; // 진입 전 카메라 위치 복원
                _targetCamera.transform.rotation = _previousRotation; // 진입 전 카메라 회전 복원
            }

            if (_cameraInputController != null) _cameraInputController.enabled = _cameraInputWasEnabled; // 기존 카메라 입력 상태 복원

            _cameraInputController = null; // 카메라 조작기 참조 정리
            _targetCamera = null; // 카메라 참조 정리
            _isLocked = false; // 고정 상태 해제
        }

        private void LateUpdate()
        {
            if (!_isLocked) return; // Shop/Event 외 강제 자세 적용 차단
            ApplyPrimaryPose(); // 다른 시스템이 움직여도 프레임 마지막에 1번 카메라 유지
        }

        private void ApplyPrimaryPose()
        {
            if (_targetCamera == null || _boardView == null) return; // 필수 참조 누락 방어

            Vector3 localPosition = StageActivityCameraPoseUtility.ResolvePrimaryCameraLocalPosition(_boardView.TileSize); // 1번 카메라 보드 로컬 위치 계산
            Quaternion localRotation = StageActivityCameraPoseUtility.ResolvePrimaryCameraLocalRotation(); // 1번 카메라 보드 로컬 회전 계산
            _targetCamera.transform.position = _boardView.transform.TransformPoint(localPosition); // 보드 변환을 반영한 고정 월드 위치 적용
            _targetCamera.transform.rotation = _boardView.transform.rotation * localRotation; // 보드 변환을 반영한 고정 월드 회전 적용
        }

        private static MonoBehaviour FindCameraInputController(Camera targetCamera)
        {
            if (targetCamera == null) return null; // 카메라 누락 방어
            MonoBehaviour[] behaviours = targetCamera.GetComponents<MonoBehaviour>(); // 카메라 GameObject의 런타임 컴포넌트 조회

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i]; // 현재 카메라 컴포넌트 조회
                if (behaviour == null) continue; // Missing Script 제외

                Type type = behaviour.GetType(); // 실제 컴포넌트 타입 조회
                FieldInfo distanceField = type.GetField("_distance", CameraFieldFlags); // 프로젝트 카메라 거리 필드 확인
                FieldInfo minPitchField = type.GetField("_minPitchDegrees", CameraFieldFlags); // 프로젝트 최소 Pitch 필드 확인
                FieldInfo maxPitchField = type.GetField("_maxPitchDegrees", CameraFieldFlags); // 프로젝트 최대 Pitch 필드 확인
                FieldInfo snapField = type.GetField("_snapDurationSeconds", CameraFieldFlags); // 프로젝트 스냅 시간 필드 확인

                if (distanceField != null && minPitchField != null && maxPitchField != null && snapField != null) return behaviour; // 기존 카메라 조작 컴포넌트 반환
            }

            return null; // 전용 카메라 조작기 미탐색 반환
        }

        private void OnDisable()
        {
            RestorePreviousView(); // 비활성화 시 카메라 잠금 안전 해제
        }

        private void OnDestroy()
        {
            RestorePreviousView(); // 오브젝트 제거 시 카메라 잠금 안전 해제
        }
    }
}
