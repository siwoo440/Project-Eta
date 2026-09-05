using UnityEngine; // 카메라·Transform·수학 기능
using UnityEngine.InputSystem; // 키보드·마우스 입력
using ProjectEta.Board; // 기존 TableCameraRig 호환

namespace ProjectEta.Environment // 전투 공간 프레젠테이션 네임스페이스
{
    public enum Day41CameraView // 41일차 카메라 뷰 구분
    {
        Top = 1, // 맨 위 기존 시점
        Basic = 2, // 기본 기존 시점
        Opponent = 3 // 상대를 보는 좌석 시점
    }

    [DisallowMultipleComponent] // 중복 카메라 리그 방지
    public sealed class Day41SeatedCameraRig : MonoBehaviour // 세 가지 전투 카메라 뷰 제어 리그
    {
        [SerializeField] private Camera _camera; // 제어 대상 전투 카메라
        [SerializeField] private Day41CameraView _currentView = Day41CameraView.Basic; // 시작 기본 시점
        [SerializeField] private float _fieldOfView = Day41BattleRoomLayout.SeatedCameraFieldOfView; // 현재 시야각
        [SerializeField] private float _zoomSensitivity = 0.012f; // 마우스 휠 줌 감도
        [SerializeField] private float _transitionSpeed = 10f; // 뷰 전환 보간 속도
        [SerializeField] private float _lookSensitivity = Day41BattleRoomLayout.OpponentLookSensitivity; // 상대 시점 회전 감도
        private TableCameraRig _legacyRig; // 기존 자유 테이블 카메라
        private bool _legacyRigWasEnabled; // 기존 카메라 활성 상태
        private bool _ownsLegacyDisable; // 기존 카메라 비활성화 소유 여부
        private Vector2 _opponentLookAngles; // 상대 시점 yaw·pitch 누적값
        private GameObject _topViewOccluders; // 맨 위 시점에서 숨길 천장 구조 루트
        private bool _snapPose; // 즉시 위치 적용 여부

        public Day41CameraView CurrentView => _currentView; // 현재 뷰 외부 조회

        private void Awake() // 런타임 카메라 참조 초기화
        {
            ResolveCamera(); // 자식·메인 카메라 탐색
        }

        private void OnEnable() // 세 뷰 리그 활성화
        {
            ResolveCamera(); // 카메라 참조 보정
            DisableLegacyRig(); // 기존 W/S 카메라 제어 비활성화
            _currentView = Day41CameraView.Basic; // 기존 기본 뷰로 시작
            _opponentLookAngles = Vector2.zero; // 상대 시점 회전값 초기화
            ResolveTopViewOccluders(); // 맨 위 시점 가림 구조 탐색
            ApplyTopViewOccluderVisibility(); // 기본 뷰에서 천장 구조 활성화
            _snapPose = true; // 첫 프레임 즉시 배치 예약
            ApplyCameraPose(); // 즉시 기본 시점 적용
        }

        private void Update() // 매 프레임 입력 처리
        {
            HandleViewInput(); // W/S 뷰 전환 처리
            HandleOpponentLookInput(); // 상대 시점 자유 회전 처리
            HandleZoomInput(); // 상대 시점 제한 줌 처리
        }

        private void LateUpdate() // 다른 카메라 갱신 이후 최종 시점 적용
        {
            ApplyCameraPose(); // 선택 뷰 위치·회전 적용
        }

        public void Configure(Camera targetCamera) // 런타임 부트스트랩 카메라 주입
        {
            _camera = targetCamera; // 대상 카메라 저장
            _currentView = Day41CameraView.Basic; // 기본 시점 선택
            _fieldOfView = Day41BattleRoomLayout.SeatedCameraFieldOfView; // 기본 시야각 복원
            _opponentLookAngles = Vector2.zero; // 상대 시점 회전 초기화
            DisableLegacyRig(); // 기존 자유 카메라 비활성화
            ResolveTopViewOccluders(); // 맨 위 시점 가림 구조 탐색
            ApplyTopViewOccluderVisibility(); // 기본 뷰에서 천장 구조 활성화
            _snapPose = true; // 최초 위치 즉시 적용 예약
            ApplyCameraPose(); // 주입 직후 기본 시점 적용
        }

        private void ResolveCamera() // 카메라 참조 탐색
        {
            if (_camera != null) return; // 기존 참조 유지
            _camera = GetComponentInChildren<Camera>(true); // 리그 하위 카메라 탐색
            if (_camera == null) _camera = Camera.main; // 메인 카메라 대체 탐색
            if (_camera == null) _camera = Object.FindFirstObjectByType<Camera>(); // 씬 전체 카메라 최종 탐색
        }

        private void DisableLegacyRig() // 기존 TableCameraRig 제어 차단
        {
            if (_legacyRig == null) _legacyRig = GetComponent<TableCameraRig>(); // 동일 리그 기존 카메라 탐색
            if (_legacyRig == null && _camera != null) _legacyRig = _camera.GetComponentInParent<TableCameraRig>(); // 카메라 부모 기존 리그 탐색
            if (_legacyRig == null || _ownsLegacyDisable) return; // 대상 없음·이미 처리 상태 제외

            _legacyRigWasEnabled = _legacyRig.enabled; // 기존 활성 상태 저장
            _legacyRig.enabled = false; // 기존 W/S 처리 차단
            _ownsLegacyDisable = true; // 비활성화 소유 기록
        }

        private void HandleViewInput() // W/S 세 뷰 전환 처리
        {
            if (Keyboard.current == null) return; // 키보드 누락 제외

            if (Keyboard.current.wKey.wasPressedThisFrame) // W 입력 확인
            {
                _currentView = ResolveWView(_currentView); // 기존 맨 위·기본 뷰 전환
                _opponentLookAngles = Vector2.zero; // 상대 회전값 초기화
                ApplyTopViewOccluderVisibility(); // 변경된 뷰에 맞춰 천장 구조 표시 갱신
            }

            if (Keyboard.current.sKey.wasPressedThisFrame) // S 입력 확인
            {
                _currentView = ResolveSView(_currentView); // 상대 좌석 시점 선택
                _opponentLookAngles = Vector2.zero; // 상대 정면으로 시선 초기화
                ApplyTopViewOccluderVisibility(); // 상대 시점에서 천장 구조 다시 활성화
            }
        }


        private void ResolveTopViewOccluders() // 맨 위 시점에서 가리는 천장 구조 탐색
        {
            if (_topViewOccluders != null) return; // 기존 참조 유지
            GameObject roomRoot = GameObject.Find(Day41BattleRoomLayout.RootName); // 41일차 방 루트 탐색
            if (roomRoot == null) return; // 환경 루트가 아직 없으면 대기
            Transform occluderRoot = roomRoot.transform.Find(Day41BattleRoomLayout.TopViewOccluderRootName); // 가림 구조 하위 그룹 탐색
            if (occluderRoot != null) _topViewOccluders = occluderRoot.gameObject; // 찾은 그룹 참조 저장
        }

        private void ApplyTopViewOccluderVisibility() // 현재 카메라 뷰에 따라 천장 가림 구조 표시 전환
        {
            ResolveTopViewOccluders(); // 지연 생성 환경까지 다시 탐색
            if (_topViewOccluders == null) return; // 가림 구조가 없으면 종료
            bool shouldBeVisible = ShouldShowTopViewOccluders(_currentView); // 현재 뷰 표시 여부 계산
            if (_topViewOccluders.activeSelf == shouldBeVisible) return; // 동일 상태면 변경 생략
            _topViewOccluders.SetActive(shouldBeVisible); // 맨 위에서 숨기고 다른 뷰에서 복원
        }

        private void HandleOpponentLookInput() // 상대 시점 마우스 자유 회전 처리
        {
            if (_currentView != Day41CameraView.Opponent) return; // 상대 시점 외 입력 제외
            if (Mouse.current == null || !Mouse.current.rightButton.isPressed) return; // 우클릭 드래그 외 입력 제외

            Vector2 delta = Mouse.current.delta.ReadValue(); // 마우스 이동량 조회
            _opponentLookAngles.x += delta.x * _lookSensitivity; // 좌우 회전 누적
            _opponentLookAngles.y -= delta.y * _lookSensitivity; // 상하 회전 누적
            _opponentLookAngles = ClampOpponentLookAngles(_opponentLookAngles); // 상하좌우 회전 범위 제한
        }

        private void HandleZoomInput() // 상대 좌석을 벗어나지 않는 시야각 줌
        {
            if (_currentView != Day41CameraView.Opponent) return; // 상대 시점 외 줌 제외
            if (_camera == null || Mouse.current == null) return; // 입력 장치·카메라 누락 제외

            float scroll = Mouse.current.scroll.ReadValue().y; // 마우스 휠 값 조회
            if (Mathf.Abs(scroll) < 0.01f) return; // 무입력 프레임 제외

            _fieldOfView -= scroll * _zoomSensitivity; // 휠 방향에 따른 시야각 조절
            _fieldOfView = Mathf.Clamp(_fieldOfView, Day41BattleRoomLayout.SeatedCameraMinFieldOfView, Day41BattleRoomLayout.SeatedCameraMaxFieldOfView); // 줌 범위 제한
        }

        private void ApplyCameraPose() // 현재 뷰의 카메라 위치·회전·렌즈 적용
        {
            if (_camera == null) return; // 카메라 누락 제외

            ResolveTargetPose(out Vector3 targetPosition, out Quaternion targetRotation); // 현재 뷰 목표 포즈 계산
            float blend = _snapPose ? 1f : 1f - Mathf.Exp(-_transitionSpeed * Time.unscaledDeltaTime); // 프레임 독립 보간값 계산

            _camera.orthographic = false; // 원근 시점 강제
            _camera.fieldOfView = _fieldOfView; // 현재 시야각 적용
            _camera.nearClipPlane = 0.08f; // 테이블 근접 클리핑 보정
            _camera.farClipPlane = 90f; // 거대 방 전체 렌더 거리
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPosition, blend); // 목표 위치로 부드럽게 이동
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, targetRotation, blend); // 목표 회전으로 부드럽게 이동
            _snapPose = false; // 즉시 적용 상태 해제
        }

        private void ResolveTargetPose(out Vector3 targetPosition, out Quaternion targetRotation) // 현재 뷰 목표 포즈 계산
        {
            if (_currentView == Day41CameraView.Top) // 맨 위 시점 분기
            {
                targetPosition = Day41BattleRoomLayout.TopCameraPosition; // 기존 60도 위치 적용
                targetRotation = CalculateLookRotation(targetPosition, Day41BattleRoomLayout.BoardCenter); // 보드 중심 시선 적용
                return; // 계산 종료
            }

            if (_currentView == Day41CameraView.Basic) // 기본 시점 분기
            {
                targetPosition = Day41BattleRoomLayout.BasicCameraPosition; // 기존 45도 위치 적용
                targetRotation = CalculateLookRotation(targetPosition, Day41BattleRoomLayout.BoardCenter); // 보드 중심 시선 적용
                return; // 계산 종료
            }

            targetPosition = Day41BattleRoomLayout.SeatedCameraPosition; // 상대 좌석 눈높이 위치 적용
            Quaternion baseRotation = CalculateLookRotation(targetPosition, Day41BattleRoomLayout.OpponentLookTarget); // 상대 정면 기본 회전 계산
            Quaternion lookOffset = Quaternion.Euler(_opponentLookAngles.y, _opponentLookAngles.x, 0f); // 자유 고개 회전 오프셋 계산
            targetRotation = baseRotation * lookOffset; // 상대 정면 기준 자유 회전 적용
        }


        public static bool ShouldShowTopViewOccluders(Day41CameraView currentView) // 카메라별 천장 구조 표시 규칙 계산
        {
            return currentView != Day41CameraView.Top; // 맨 위 시점에서만 가림 구조 비활성화
        }

        public static Day41CameraView ResolveWView(Day41CameraView currentView) // W 입력 결과 계산
        {
            if (currentView == Day41CameraView.Top) return Day41CameraView.Basic; // 맨 위에서 기본으로 전환
            if (currentView == Day41CameraView.Basic) return Day41CameraView.Top; // 기본에서 맨 위로 전환
            return Day41CameraView.Basic; // 상대 시점에서 기본으로 복귀
        }

        public static Day41CameraView ResolveSView(Day41CameraView currentView) // S 입력 결과 계산
        {
            return Day41CameraView.Opponent; // 모든 상태에서 상대 시점 선택
        }

        public static Vector2 ClampOpponentLookAngles(Vector2 angles) // 상대 시점 자유 회전 제한 계산
        {
            float yaw = Mathf.Clamp(angles.x, -Day41BattleRoomLayout.OpponentYawHalfRange, Day41BattleRoomLayout.OpponentYawHalfRange); // 좌우 총 180도 제한
            float pitch = Mathf.Clamp(angles.y, -Day41BattleRoomLayout.OpponentPitchHalfRange, Day41BattleRoomLayout.OpponentPitchHalfRange); // 상하 뒤집힘 방지 제한
            return new Vector2(yaw, pitch); // 제한된 각도 반환
        }

        public static Quaternion CalculateLookRotation(Vector3 position, Vector3 target) // 테스트 가능한 시선 회전 계산
        {
            Vector3 direction = target - position; // 목표 방향 벡터 계산
            if (direction.sqrMagnitude < 0.0001f) return Quaternion.identity; // 동일 위치 예외 처리
            return Quaternion.LookRotation(direction.normalized, Vector3.up); // 월드 위쪽 기준 회전 반환
        }

        private void OnGUI() // 현재 카메라 뷰·조작 안내 표시
        {
            string viewName = GetViewName(_currentView); // 현재 뷰 이름 조회
            GUI.Label(new Rect(10, 70, 760, 20), $"카메라 뷰: {(int)_currentView} {viewName}  |  W: 맨 위/기본 전환  |  S: 상대 시점  |  상대 시점: 우클릭+마우스 고개 회전"); // 카메라 조작 안내 표시
        }

        private static string GetViewName(Day41CameraView view) // 뷰 표시 이름 반환
        {
            if (view == Day41CameraView.Top) return "맨 위"; // 맨 위 이름 반환
            if (view == Day41CameraView.Basic) return "기본"; // 기본 이름 반환
            return "상대"; // 상대 이름 반환
        }

        private void OnDisable() // 세 뷰 리그 해제 처리
        {
            if (!_ownsLegacyDisable || _legacyRig == null) return; // 기존 리그 복원 불필요 상태 제외

            _legacyRig.enabled = _legacyRigWasEnabled; // 기존 활성 상태 복원
            _ownsLegacyDisable = false; // 비활성화 소유 해제
        }
    }
}
