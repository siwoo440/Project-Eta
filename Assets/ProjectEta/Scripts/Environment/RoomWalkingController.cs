using UnityEngine; // 이동·충돌·카메라 기능
using UnityEngine.InputSystem; // 키보드·마우스 입력
using ProjectEta.Debugging; // 디버그 패널 상태
using ProjectEta.UI; // 모달 화면 상태

namespace ProjectEta.Environment // 전투 방 이동 영역
{ // 영역 시작
    [DefaultExecutionOrder(-200)] // 보드 입력 이전 이동 상태 처리
    [DisallowMultipleComponent] // 중복 이동기 방지
    public sealed class RoomWalkingController : MonoBehaviour // 의자 이탈과 복귀 제어
    { // 타입 시작
        public const float EyeHeight = 5.25f; // 바닥 기준 선 눈높이
        public const float ChairReach = 3.4f; // 의자 접근 거리
        public const float WalkSpeed = 8f; // 실제 초당 이동 거리
        private static RoomWalkingController _activeWalker; // 현재 이동 중인 플레이어
        private Day41SeatedCameraRig _rig; // 기존 좌석 카메라
        private CharacterController _body; // 실제 이동 충돌체
        private GameObject _collisionRoot; // 보드 레이에서 제외할 환경 충돌체
        private RoomWalkingUI _ui; // 조작·앉기 안내
        private RoomWalkingHudVisibility _hudVisibility; // 걷기 전용 화면 표시 소유
        private Vector2 _lookAngles; // 좌우·상하 회전
        private float _verticalSpeed; // 중력 속도
        private CursorLockMode _previousCursorLock; // 일어나기 전 커서 잠금
        private bool _previousCursorVisible; // 일어나기 전 커서 표시
        private bool _ownsCursor; // 커서 상태 복원 소유
        public static bool BlocksTableInput => _activeWalker != null && _activeWalker.IsWalking; // 이동 중 보드·지도 입력 차단
        public bool IsWalking { get; private set; } // 의자 이탈 상태
        public Vector3 FeetPosition => _body != null ? _body.transform.position : Day41BattleRoomLayout.PlayerChairRoot; // 발 위치 조회
        public Vector3 EyePosition => FeetPosition + Vector3.up * EyeHeight; // 선 눈높이 위치
        public Quaternion LookRotation => Quaternion.Euler(_lookAngles.y, _lookAngles.x, 0f); // 자유 시선 회전
        public bool CanSit => IsWalking && IsWithinChairReach(FeetPosition); // 앉기 거리 조건
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 재시작 초기화 지정
        private static void ResetRuntimeState() // 이전 플레이 참조 정리
        { // 메서드 시작
            _activeWalker = null; // 이동 중 참조 해제
        } // 메서드 종료
        public void Configure(Day41SeatedCameraRig rig) // 기존 카메라와 연결
        { // 메서드 시작
            _rig = rig; // 좌석 리그 저장
            if (_body != null) return; // 기존 생성물 재사용
            _collisionRoot = new GameObject("RoomWalkingColliders_Day100"); // 이동용 환경 충돌 루트
            BuildColliders(_collisionRoot.transform); // 방·테이블·의자 충돌 구성
            var bodyRoot = new GameObject("RoomWalkingBody_Day100"); // 카메라와 독립된 이동 루트
            bodyRoot.transform.SetParent(_collisionRoot.transform, true); // 환경 루트와 수명 연결
            _body = bodyRoot.AddComponent<CharacterController>(); // 미끄러짐 가능한 이동 충돌체
            _body.height = 5.5f; // 몸체 전체 높이
            _body.radius = 0.45f; // 몸체 가로 반경
            _body.center = Vector3.up * (_body.height * 0.5f); // 발 기준 몸체 중심
            _body.stepOffset = 0.25f; // 낮은 바닥 턱 허용
            _body.skinWidth = 0.04f; // 충돌 여유 폭
            _body.minMoveDistance = 0f; // 작은 이동도 반영
            _body.enabled = false; // 앉은 상태 충돌 해제
            _ui = gameObject.AddComponent<RoomWalkingUI>(); // 자동 안내 화면 연결
            _ui.Configure(this); // 이동 상태 연결
            _hudVisibility = gameObject.AddComponent<RoomWalkingHudVisibility>(); // 게임 진행과 독립된 화면 표시 연결
            RoomInvestigationController investigation = GetComponent<RoomInvestigationController>(); // 기존 조사 담당자 재사용
            if (investigation == null) investigation = gameObject.AddComponent<RoomInvestigationController>(); // Inspector 없는 조사 자동 설치
            investigation.Configure(this); // 현재 이동기와 방 조사 연결
        } // 메서드 종료
        private void Update() // 실제 조작 입력 처리
        { // 메서드 시작
            if (_rig == null || !_rig.isActiveAndEnabled || _body == null) return; // 비활성·미연결 제외
            Day69UiModalState modal = Day69UiPresentationRules.CaptureCurrent(); // 필수 진행 화면 상태 조회
            if (IsWalking && (modal.RewardOpen || modal.ActivityOpen || modal.RunResultOpen || modal.TutorialOpen)) // 이동 중 필수 선택 화면 확인
            { // 조건 시작
                ReturnToSeat(); // 진행 화면의 클릭과 좌석 시점 복원
                return; // 해당 프레임 이동 중단
            } // 조건 종료
            if (_rig.IsPoseLocked) return; // 필수 모달 좌석 복귀 후 외부 시점 고정 보호
            if (IsInputBlocked()) // 모달·일시정지 상태 확인
            { // 조건 시작
                if (IsWalking) SetWalkingCursor(false); // 모달 화면 커서 해제
                return; // 이동·앉기 입력 차단
            } // 조건 종료
            Keyboard keyboard = Keyboard.current; // 현재 키보드 조회
            Vector2 movement = ReadMovement(keyboard); // WASD 이동 방향
            if (!IsWalking && movement.sqrMagnitude > 0f) TryStand(); // 이동 입력으로 의자 이탈
            if (!IsWalking) return; // 앉은 상태 추가 이동 차단
            SetWalkingCursor(true); // 이동 중 마우스 잠금
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame && TrySit()) return; // 의자 접근 후 F 복귀
            Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero; // 마우스 시선 입력
            Move(movement, Time.unscaledDeltaTime, look); // 게임 배속과 독립된 실제 충돌 이동
        } // 메서드 종료
        public bool TryStand() // 의자에서 일어나기
        { // 메서드 시작
            if (IsWalking || _body == null || _rig == null || !_rig.isActiveAndEnabled || _rig.IsPoseLocked || IsInputBlocked()) return false; // 중복·고정·모달 상태 차단
            _body.transform.position = new Vector3(2.8f, Day41BattleRoomLayout.FloorY + 0.05f, -10.7f); // 의자 오른쪽 안전한 바닥 위치
            _lookAngles = new Vector2(_rig.CameraRotation.eulerAngles.y, Mathf.DeltaAngle(0f, _rig.CameraRotation.eulerAngles.x)); // 기존 시선 방향 유지
            _lookAngles.y = Mathf.Clamp(_lookAngles.y, -85f, 85f); // 뒤집힌 시선 방지
            _verticalSpeed = 0f; // 낙하 속도 초기화
            _body.enabled = true; // 이동 충돌 활성화
            IsWalking = true; // 의자 이탈 상태 기록
            _activeWalker = this; // 보드 입력 차단 소유
            _hudVisibility.SetWalking(true); // 전투·지도·손패 화면 표시 억제
            _previousCursorLock = Cursor.lockState; // 기존 커서 잠금 저장
            _previousCursorVisible = Cursor.visible; // 기존 커서 표시 저장
            _ownsCursor = true; // 커서 복원 책임 기록
            SetWalkingCursor(true); // 자유 시선 입력 시작
            _rig.SnapToCurrentView(); // 선 눈높이 즉시 적용
            return true; // 의자 이탈 성공
        } // 메서드 종료
        public bool TrySit() // 의자 접근 후 앉기
        { // 메서드 시작
            if (!CanSit || IsInputBlocked()) return false; // 거리·모달 조건 확인
            ReturnToSeat(); // 원래 좌석 시점 복원
            return true; // 복귀 성공
        } // 메서드 종료
        public void ReturnToSeat() // 좌석 복귀와 입력 자원 해제
        { // 메서드 시작
            RoomInvestigationController investigation = GetComponent<RoomInvestigationController>(); // 조사 모달 소유 확인
            if (investigation != null) investigation.ForceClose(); // 강제 좌석 복귀 전 조사 입력 해제
            IsWalking = false; // 이동 상태 해제
            if (_hudVisibility != null) _hudVisibility.SetWalking(false); // 이전 표시 소유자의 상태 복원
            if (_activeWalker == this) _activeWalker = null; // 보드 입력 차단 해제
            if (_body != null) _body.enabled = false; // 좌석 내부 충돌 해제
            if (_ownsCursor) // 저장된 커서 상태 확인
            { // 조건 시작
                Cursor.lockState = _previousCursorLock; // 기존 잠금 상태 복원
                Cursor.visible = _previousCursorVisible; // 기존 표시 상태 복원
                _ownsCursor = false; // 커서 소유 해제
            } // 조건 종료
            if (_rig != null && _rig.isActiveAndEnabled) _rig.SnapToCurrentView(); // 이전 보드 시점 복원
        } // 메서드 종료
        public void Move(Vector2 input, float deltaTime, Vector2 lookDelta) // 프레임 독립 이동과 자유 시선
        { // 메서드 시작
            if (!IsWalking || _body == null || !_body.enabled || IsInputBlocked()) return; // 이동 가능 상태 확인
            float remaining = Mathf.Max(0f, deltaTime); // 실제 경과 시간 보존
            _lookAngles.x = Mathf.Repeat(_lookAngles.x + lookDelta.x * 0.12f, 360f); // 좌우 전체 회전
            _lookAngles.y = Mathf.Clamp(_lookAngles.y - lookDelta.y * 0.12f, -85f, 85f); // 상하 시선 제한
            Vector2 normalized = Vector2.ClampMagnitude(input, 1f); // 대각선 속도 증가 차단
            Vector3 horizontal = Quaternion.Euler(0f, _lookAngles.x, 0f) * new Vector3(normalized.x, 0f, normalized.y); // 바라보는 방향 기준 이동
            while (remaining > 0f) // 긴 프레임을 짧은 충돌 이동으로 분할
            { // 반복 시작
                float step = Mathf.Min(remaining, 0.05f); // 한 번의 물리 이동 시간 제한
                _verticalSpeed = _body.isGrounded ? -2f : Mathf.Max(-30f, _verticalSpeed - 9.81f * step); // 바닥 접지와 중력 속도
                _body.Move((horizontal * WalkSpeed + Vector3.up * _verticalSpeed) * step); // 실제 환경 충돌·미끄러짐 적용
                remaining -= step; // 처리한 실제 경과 시간 차감
            } // 반복 종료
        } // 메서드 종료
        public static bool IsWithinChairReach(Vector3 feet) // 의자 접근 거리 판정
        { // 메서드 시작
            Vector3 offset = feet - Day41BattleRoomLayout.PlayerChairRoot; // 의자 중심과 발 위치 차이
            return Mathf.Abs(offset.y) <= 0.75f && new Vector2(offset.x, offset.z).sqrMagnitude <= ChairReach * ChairReach; // 같은 바닥 높이와 수평 거리 확인
        } // 메서드 종료
        private static Vector2 ReadMovement(Keyboard keyboard) // WASD 입력 조합
        { // 메서드 시작
            if (keyboard == null) return Vector2.zero; // 키보드 없음 처리
            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f); // 좌우 이동 방향
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f); // 전후 이동 방향
            return new Vector2(x, y); // 입력 방향 반환
        } // 메서드 종료
        private static bool IsInputBlocked() // 다른 화면의 입력 소유 확인
        { // 메서드 시작
            return !Day69UiPresentationRules.CanUseWorldInput(); // 시스템 키를 누른 프레임부터 이동 차단
        } // 메서드 종료
        private static void SetWalkingCursor(bool locked) // 걷기와 모달 커서 전환
        { // 메서드 시작
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; // 마우스 잠금 전환
            Cursor.visible = !locked; // 모달 화면 포인터 표시
        } // 메서드 종료
        private static void BuildColliders(Transform root) // 장식 모델과 별개 충돌 구성
        { // 메서드 시작
            float floor = Day41BattleRoomLayout.FloorY; // 방 바닥 높이
            AddBox(root, "WalkFloor", new Vector3(0f, floor - 0.2f, 0f), new Vector3(34f, 0.4f, 38f)); // 바닥 낙하 방지
            AddBox(root, "WalkNorthWall", new Vector3(0f, 4.5f, 18.4f), new Vector3(34f, 14f, 1.2f)); // 북쪽 벽 관통 방지
            AddBox(root, "WalkSouthWall", new Vector3(0f, 4.5f, -18.4f), new Vector3(34f, 14f, 1.2f)); // 남쪽 벽 관통 방지
            AddBox(root, "WalkEastWall", new Vector3(16.4f, 4.5f, 0f), new Vector3(1.2f, 14f, 38f)); // 동쪽 벽 관통 방지
            AddBox(root, "WalkWestWall", new Vector3(-16.4f, 4.5f, 0f), new Vector3(1.2f, 14f, 38f)); // 서쪽 벽 관통 방지
            AddBox(root, "WalkTable", new Vector3(0f, -1.1f, 0f), new Vector3(12.9f, 2.8f, 12.9f)); // 테이블 내부 진입 방지
            AddBox(root, "WalkPlayerChair", new Vector3(0f, 0.4f, -10.7f), new Vector3(3.85f, 5.6f, 2.9f)); // 플레이어 의자 모델 관통 방지
            AddBox(root, "WalkOpponentChair", new Vector3(0f, 0.4f, 10.7f), new Vector3(3.85f, 5.6f, 2.9f)); // 상대 의자 모델 관통 방지
            AddBox(root, "WalkOpponent", new Vector3(0f, 1f, 8.7f), new Vector3(3.1f, 6.8f, 2.4f)); // 상대 실루엣 관통 방지
        } // 메서드 종료
        private static void AddBox(Transform parent, string name, Vector3 position, Vector3 size) // 이동 전용 박스 생성
        { // 메서드 시작
            var part = new GameObject(name); // 충돌 전용 객체 생성
            part.layer = 2; // Ignore Raycast 레이어 적용
            part.transform.SetParent(parent, true); // 환경 루트에 연결
            part.transform.position = position; // 실제 방 좌표 적용
            part.AddComponent<BoxCollider>().size = size; // 보드 클릭을 가리지 않는 충돌 크기
        } // 메서드 종료
        private void OnDisable() // 비활성화 상태 정리
        { // 메서드 시작
            ReturnToSeat(); // 이동·커서 소유 해제
        } // 메서드 종료
        private void OnDestroy() // 씬 종료 자원 정리
        { // 메서드 시작
            ReturnToSeat(); // 남은 입력 소유 해제
            RoomInvestigationController investigation = GetComponent<RoomInvestigationController>(); // 자동 조사 생성물 소유
            if (investigation != null) Destroy(investigation); // 이동기 단독 제거의 조사 자원 정리
            if (_collisionRoot != null) Destroy(_collisionRoot); // 독립 몸체·환경 충돌 제거
            if (_ui != null) Destroy(_ui); // 안내 컴포넌트 정리
            if (_hudVisibility != null) Destroy(_hudVisibility); // 전용 화면 표시 소유자 정리
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
