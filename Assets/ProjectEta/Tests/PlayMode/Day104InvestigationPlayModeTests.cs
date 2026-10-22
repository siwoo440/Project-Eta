using System; // 콜백과 빈 목록
using System.Collections; // 프레임 대기
using System.Linq; // 실제 대상 조회
using System.Reflection; // 아직 없는 조사 진입 계약 검사
using NUnit.Framework; // 실제 동작 검사
using UnityEngine; // 실제 물리와 화면
using UnityEngine.InputSystem; // 실제 키 입력
using UnityEngine.InputSystem.LowLevel; // 입력 프레임 주입
using UnityEngine.TestTools; // PlayMode 검사
using ProjectEta.Environment; // 실제 방 이동·대상
using ProjectEta.Settings; // 실제 Pause
using ProjectEta.UI; // 실제 모달
using ProjectEta.Board; // 실제 보드 기준

namespace ProjectEta.Tests.PlayMode // 실제 조사 흐름 검사
{ // 영역 시작
    public sealed class Day104InvestigationPlayModeTests // 거리·시선·입력·수명 회귀
    { // 타입 시작
        private GameObject _host; // 시험 호스트
        private RoomWalkingController _walker; // 실제 이동기
        private RoomInvestigationController _investigation; // 자동 조사 담당자
        private Day41SeatedCameraRig _rig; // 실제 시점
        private BattleSettingsOverlayController _pause; // 실제 일시정지
        private Keyboard _keyboard; // 시험 입력 장치
        private InputSettings.UpdateMode _oldMode; // 원래 갱신 정책
        private InputSettings.BackgroundBehavior _oldBackground; // 원래 배경 정책
        private InputSettings.EditorInputBehaviorInPlayMode _oldEditor; // 원래 에디터 정책
        private float _oldScale; // 원래 시간
        private CursorLockMode _oldLock; // 원래 잠금
        private bool _oldVisible; // 원래 표시
        [UnitySetUp] // 실제 환경 구성
        public IEnumerator SetUp() // 독립 입력과 실제 방 준비
        { // 메서드 시작
            _oldScale = Time.timeScale; // 시간 보관
            _oldLock = Cursor.lockState; // 잠금 보관
            _oldVisible = Cursor.visible; // 표시 보관
            Time.timeScale = 1f; // 시험 기본 시간
            Cursor.lockState = CursorLockMode.None; // 좌석 커서 기준
            Cursor.visible = true; // 좌석 표시 기준
            _oldMode = InputSystem.settings.updateMode; // 입력 갱신 보관
            _oldBackground = InputSystem.settings.backgroundBehavior; // 배경 정책 보관
            _oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 에디터 정책 보관
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually; // 명시적 입력 갱신
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 창 포커스 독립
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 실제 게임 입력 경로
            _keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드
            _keyboard.MakeCurrent(); // 현재 장치 선택
            _host = new GameObject("Day104InvestigationTest"); // 독립 시험 호스트
            _rig = _host.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
            _rig.Configure(_host.AddComponent<Camera>()); // 자동 이동·조사 설치
            _walker = _host.GetComponent<RoomWalkingController>(); // 실제 자동 이동기
            _investigation = _host.GetComponent<RoomInvestigationController>(); // 실제 자동 조사
            _pause = _host.AddComponent<BattleSettingsOverlayController>(); // 실제 Pause
            yield return null; // 화면 Start 실행
            GameInputBindingService.Apply(GameSettingsData.CreateDefault()); // Esc 기본 입력 고정
            Physics.SyncTransforms(); // 실제 충돌 위치 갱신
        } // 메서드 종료
        [UnityTearDown] // 소유 상태 복원
        public IEnumerator TearDown() // 시험 자원 정리
        { // 메서드 시작
            _pause.ClosePanel(); // Pause 소유 해제
            if (_walker != null) _walker.ReturnToSeat(); // 남은 이동 소유 해제
            UnityEngine.Object.Destroy(_host); // 실제 생성물 정리
            InputSystem.RemoveDevice(_keyboard); // 시험 장치 제거
            InputSystem.settings.updateMode = _oldMode; // 입력 정책 복원
            InputSystem.settings.backgroundBehavior = _oldBackground; // 배경 정책 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = _oldEditor; // 에디터 정책 복원
            GameInputBindingService.Apply(GameSettingsService.Current); // 기존 키 복원
            Time.timeScale = _oldScale; // 시간 복원
            Cursor.lockState = _oldLock; // 잠금 복원
            Cursor.visible = _oldVisible; // 표시 복원
            yield return null; // 실제 객체 제거
            yield return null; // 별도 월드 모델 제거
        } // 메서드 종료
        [UnityTest] // 실제 네 곳 접근 판정
        public IEnumerator EveryTarget_IsInsideRoomAndInspectableFromItsClearApproach() // 도달 가능한 대상 배치
        { // 메서드 시작
            Assert.That(_investigation.Targets.Select(target => target.Definition.Id).Distinct().Count(), Is.EqualTo(4)); // 저장 키 중복 없음
            foreach (RoomInvestigationTarget target in _investigation.Targets) // 모든 자동 배치
            { // 반복 시작
                Vector3 eye = ApproachEye(target); // 전면 통로의 실제 눈높이
                Assert.That(Day41BattleRoomLayout.IsInsideRoom(eye), Is.True); // 방 내부 접근
                Assert.That(_investigation.FindFocus(eye, target.Anchor - eye), Is.SameAs(target), target.Definition.Id); // 거리·시선·물리 가림 연결
            } // 반복 종료
            yield return null; // 물리 프레임 정리
        } // 메서드 종료
        [UnityTest] // 시선과 장애물 검사
        public IEnumerator DistanceBackwardsInactiveAndOccluder_RejectInspection() // 벽 뒤 조사 방지
        { // 메서드 시작
            RoomInvestigationTarget target = _investigation.Targets[0]; // 실제 기록대
            Vector3 eye = ApproachEye(target); // 접근 눈높이
            Assert.That(target.CanInspectFrom(eye + target.Definition.Facing * 10f, target.Anchor - eye), Is.False); // 먼 거리 차단
            Assert.That(target.CanInspectFrom(eye, eye - target.Anchor), Is.False); // 뒤돌아보기 차단
            target.enabled = false; // 비활성 대상
            Assert.That(target.CanInspectFrom(eye, target.Anchor - eye), Is.False); // 비활성 입력 차단
            target.enabled = true; // 대상 복원
            var blocker = new GameObject("InvestigationOccluder", typeof(BoxCollider)); // 실제 시선 장애물
            blocker.transform.SetParent(_host.transform); // 시험 소유
            blocker.transform.position = Vector3.Lerp(eye, target.Anchor, 0.5f); // 눈과 물체 사이
            blocker.GetComponent<BoxCollider>().size = Vector3.one * 1.2f; // 실제 가림 크기
            Physics.SyncTransforms(); // 장애물 반영
            Assert.That(_investigation.FindFocus(eye, target.Anchor - eye), Is.Null); // 가림 뒤 대상 차단
            yield return null; // 물리 프레임 정리
        } // 메서드 종료
        [UnityTest] // 실제 F 입력 검사
        public IEnumerator F_OpensFocusedTargetAndShowsTitle() // 바라본 물건 조사
        { // 메서드 시작
            RoomInvestigationTarget target = PrepareTarget(0); // 실제 기록대 접근
            Press(Key.F); // 실제 조사 입력
            yield return null; // 담당자 입력 처리
            Assert.That(Open, Is.True); // 모달 진입
            Assert.That(_host.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(label => label.gameObject.activeInHierarchy && label.text == target.Definition.Title), Is.True); // 실제 조사 제목 표시
            Assert.That(Cursor.visible, Is.True); // 조사 클릭 커서
            Assert.That(Value<int>("ViewedCount"), Is.EqualTo(1)); // 방 안의 첫 확인 기록
        } // 메서드 종료
        [UnityTest] // 조사 중 입력 소유 검사
        public IEnumerator Inspection_FreezesMovementAndLookThenRestoresWalking() // 조사 모달과 걷기 분리
        { // 메서드 시작
            RoomInvestigationTarget target = PrepareTarget(0); // 조사 가능한 실제 위치
            Assert.That(Call("TryInspect", target), Is.True); // 실제 조사 진입
            Vector3 position = _walker.FeetPosition; // 조사 전 위치
            Quaternion rotation = _walker.LookRotation; // 조사 전 시선
            _walker.Move(Vector2.right, 0.5f, new Vector2(120f, 90f)); // 직접 이동 입력 경로
            Assert.That(_walker.FeetPosition, Is.EqualTo(position)); // 이동 차단
            Assert.That(Quaternion.Angle(_walker.LookRotation, rotation), Is.LessThan(0.01f)); // 시선 차단
            Press(); // 키 해제
            yield return null; // 진입 프레임 종료
            Assert.That(Call("TryClose"), Is.True); // 조사 닫기
            yield return null; // 닫기 프레임 종료
            _walker.Move(Vector2.right, 0.1f, Vector2.zero); // 원래 걷기 복원
            Assert.That((_walker.FeetPosition - position).magnitude, Is.GreaterThan(0.5f)); // 다시 실제 이동 가능
            Assert.That(_walker.IsWalking, Is.True); // 좌석 강제 복귀 없음
        } // 메서드 종료
        [UnityTest] // 모달 중첩 검사
        public IEnumerator Pause_CoversInspectionAndDoesNotCloseIt() // Pause 복귀 후 같은 조사 유지
        { // 메서드 시작
            Assert.That(Call("TryInspect", PrepareTarget(0)), Is.True); // 조사 진입
            _pause.OpenPanel(); // 조사 위 Pause
            Assert.That(Call("TryClose"), Is.False); // 상위 모달 아래 닫기 입력 차단
            Assert.That(Open, Is.True); // 조사 내용 유지
            _pause.ClosePanel(); // 조사 화면 복귀
            Press(); // 입력 해제
            yield return null; // 같은 프레임 재사용 방지
            Assert.That(Open, Is.True); // 원래 모달 유지
            Assert.That(Call("TryClose"), Is.True); // 정상 복귀 후 닫기
        } // 메서드 종료
        [UnityTest] // 같은 키 중복 검사
        public IEnumerator HeldF_DoesNotImmediatelyCloseOrDuplicateViewedRecord() // 조사 열기와 닫기 입력 분리
        { // 메서드 시작
            PrepareTarget(0); // 실제 대상 접근
            Press(Key.F); // 첫 누름
            yield return null; // 조사 진입
            Press(Key.F); // 같은 키 유지
            yield return null; // 유지 입력 처리
            Assert.That(Open, Is.True); // 누르고 있으면 조사 유지
            Assert.That(Value<int>("ViewedCount"), Is.EqualTo(1)); // 한 번만 확인
            Press(); // 키 놓기
            yield return null; // 해제 반영
            Press(Key.F); // 새 누름
            yield return null; // 조사 닫기
            Assert.That(Open, Is.False); // 새 입력으로만 닫기
            Assert.That(_walker.IsWalking, Is.True); // 이동 상태 유지
        } // 메서드 종료
        [UnityTest] // 의자 입력 우선순위 검사
        public IEnumerator F_NearChairSitsAndDirectInspectionIsRejected() // 동일 F키의 좌석 우선
        { // 메서드 시작
            Assert.That(_walker.TryStand(), Is.True); // 의자 옆 기립
            Assert.That(Call("TryInspect", _investigation.Targets[0]), Is.False); // 좌석 범위 조사 진입 차단
            Press(Key.F); // 실제 좌석 복귀 입력
            yield return null; // F 처리
            Assert.That(_walker.IsWalking, Is.False); // 의자 복귀
            Assert.That(Open, Is.False); // 조사창 없음
            Assert.That(Value<int>("ViewedCount"), Is.Zero); // 확인 보상 없음
        } // 메서드 종료
        [UnityTest] // 강제 좌석 복귀 정리
        public IEnumerator ReturnToSeat_ClosesInspectionAndReleasesModal() // 조사 중 복귀 잔존 차단
        { // 메서드 시작
            Assert.That(Call("TryInspect", PrepareTarget(0)), Is.True); // 실제 조사 진입
            _walker.ReturnToSeat(); // 강제 좌석 복귀
            Assert.That(Open, Is.False); // 남은 조사 정리
            Assert.That(Cursor.visible, Is.True); // 원래 좌석 포인터
            Assert.That(RoomWalkingController.BlocksTableInput, Is.False); // 이동 소유 해제
            yield return null; // 수명 정리
        } // 메서드 종료
        [UnityTest] // 카메라 고정 상태 필수 화면 검사
        public IEnumerator ActivityWithPoseLock_ClosesInspectionAndReturnsToSeat() // 고정 시점에서도 필수 진행 우선
        { // 메서드 시작
            Assert.That(Call("TryInspect", PrepareTarget(0)), Is.True); // 조사 진입
            StageBoardOverlayUI activity = _host.AddComponent<StageBoardOverlayUI>(); // 실제 필수 화면
            BoardView board = _host.AddComponent<BoardView>(); // 실제 보드 기준
            activity.Initialize(board); // 자동 UI 생성
            activity.ShowPage(StageOverlayMode.Event, "진행 이벤트", "선택 필요", Array.Empty<StageOverlayOption>()); // 실제 필수 선택
            _rig.SetExternalPoseLock(true); // 실제 상점·이벤트 시점 고정
            yield return null; // 담당자와 이동기 처리
            Assert.That(Open, Is.False); // 조사 입력 소유 해제
            Assert.That(_walker.IsWalking, Is.False); // 필수 화면 좌석 복귀
            Assert.That(activity.IsVisible, Is.True); // 필수 선택 화면 유지
            _rig.SetExternalPoseLock(false); // 시험 시점 복원
            activity.Hide(); // 시험 화면 정리
        } // 메서드 종료
        [UnityTest] // 재조사 이력 검사
        public IEnumerator Reinspection_RetainsSingleRuntimeRecord() // 영구 단서와 분리한 확인 상태
        { // 메서드 시작
            RoomInvestigationTarget target = PrepareTarget(0); // 실제 대상
            Assert.That(Call("TryInspect", target), Is.True); // 첫 조사
            yield return null; // 진입 프레임 종료
            Assert.That(Call("TryClose"), Is.True); // 첫 닫기
            yield return null; // 닫기 프레임 종료
            Assert.That(Call("TryInspect", target), Is.True); // 같은 물체 재조사
            Assert.That(Value<int>("ViewedCount"), Is.EqualTo(1)); // 방 인스턴스의 고유 확인 개수
        } // 메서드 종료
        [UnityTest] // 비활성화 생명주기 검사
        public IEnumerator DisablingController_ClosesInspectionAndReleasesWalking() // 모달 소유 잔존 방지
        { // 메서드 시작
            Assert.That(Call("TryInspect", PrepareTarget(0)), Is.True); // 실제 조사 진입
            _investigation.enabled = false; // 조사 담당자 비활성화
            Assert.That(Open, Is.False); // 즉시 모달 정리
            Press(); // 입력 해제
            yield return null; // 전환 프레임 종료
            yield return null; // 커서 복원 갱신
            Assert.That(_walker.IsWalking, Is.True); // 원래 걷기 유지
            Assert.That(Cursor.visible, Is.False); // 걷기 포인터 정책 복원
        } // 메서드 종료
        [UnityTest] // 대상의 실제 이동 충돌 검사
        public IEnumerator EveryTarget_StopsWalkingThroughItsModel() // 조사 모델 관통 차단
        { // 메서드 시작
            for (int index = 0; index < _investigation.Targets.Length; index++) // 네 곳 실제 충돌
            { // 반복 시작
                RoomInvestigationTarget target = PrepareTarget(index); // 전면 접근
                _walker.Move(Vector2.up, 0.65f, Vector2.zero); // 모델을 향해 실제 충돌 이동
                float frontDistance = Vector3.Dot(_walker.FeetPosition - target.Definition.Position, target.Definition.Facing); // 앞면까지 실제 거리
                Assert.That(frontDistance, Is.GreaterThanOrEqualTo(target.Definition.Size.z * 0.5f + 0.3f), target.Definition.Id); // 몸체 반경을 포함한 정지
            } // 반복 종료
            yield return null; // 물리 프레임 정리
        } // 메서드 종료
        [UnityTest] // 실제 바라보기 안내 검사
        public IEnumerator LookingAtTarget_ShowsOneGoldBorderAndInvestigationHint() // 조사 가능한 물체의 명확한 피드백
        { // 메서드 시작
            RoomInvestigationTarget target = PrepareTarget(0); // 기록대 접근
            yield return null; // 실제 시선·하단 안내 갱신
            yield return null; // 하단 안내 LateUpdate 완료 대기
            Assert.That(_investigation.FocusTarget, Is.SameAs(target)); // 실제 시선 대상
            Assert.That(target.IsFocused, Is.True); // 금빛 테두리
            Assert.That(_investigation.Targets.Count(item => item.IsFocused), Is.EqualTo(1)); // 단일 대상 강조
            Assert.That(_host.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(label => label.gameObject.activeInHierarchy && label.text.Contains("F : " + target.Definition.Title + " 조사")), Is.True); // 실제 조사 안내
            _walker.Move(Vector2.zero, 0f, new Vector2(1500f, 0f)); // 반대편으로 시선 전환
            yield return null; // 대상 강조 갱신
            Assert.That(target.IsFocused, Is.False); // 바라보지 않는 대상 해제
        } // 메서드 종료
        [UnityTest] // 같은 프레임 직접 콜백 검사
        public IEnumerator OpeningAndClosing_ConsumeInputAndBlockDuplicateCalls() // 같은 키로 열고 닫기 방지
        { // 메서드 시작
            RoomInvestigationTarget target = PrepareTarget(0); // 실제 조사 위치
            Assert.That(Call("TryInspect", target), Is.True); // 첫 조사
            Assert.That(Call("TryInspect", target), Is.False); // 중복 조사 차단
            Assert.That(Call("TryClose"), Is.False); // 열기 프레임의 닫기 차단
            yield return null; // 새 프레임
            Assert.That(Call("TryClose"), Is.True); // 정상 닫기
            Assert.That(Call("TryInspect", target), Is.False); // 닫기 프레임의 재열기 차단
            Assert.That(Value<int>("ViewedCount"), Is.EqualTo(1)); // 한 번 확인 이력
        } // 메서드 종료
        [UnityTest] // UI 컴포넌트 수명 검사
        public IEnumerator DisablingInvestigationUi_ClosesItsOwnedModal() // 화면 수명 종료의 입력 소유 해제
        { // 메서드 시작
            Assert.That(Call("TryInspect", PrepareTarget(0)), Is.True); // 실제 조사 진입
            RoomInvestigationUI ui = _host.GetComponent<RoomInvestigationUI>(); // 자동 연결된 화면 담당자
            ui.enabled = false; // 실제 UI 컴포넌트 비활성화
            Assert.That(Open, Is.False); // 없는 화면의 모달 잔존 금지
            yield return null; // 소유 해제 반영
            Assert.That(Call("TryInspect", _investigation.Targets[0]), Is.False); // 비활성 화면에 조사 재진입 금지
        } // 메서드 종료
        [UnityTest] // 자동 생성물의 이동기 수명 검사
        public IEnumerator DestroyingWalker_CleansInvestigationAndWorldModels() // 이동기 단독 제거의 생성물 정리
        { // 메서드 시작
            GameObject targetRoot = _investigation.Targets[0].transform.parent.gameObject; // 독립 조사 모델 루트
            Assert.That(Call("TryInspect", PrepareTarget(0)), Is.True); // 열린 조사 소유
            UnityEngine.Object.Destroy(_walker); // 호스트를 유지한 이동기 제거
            yield return null; // 이동기 제거 반영
            yield return null; // 조사 담당자 제거 반영
            yield return null; // 별도 모델·UI 제거 반영
            Assert.That(_investigation == null, Is.True); // 자동 조사 담당자 정리
            Assert.That(targetRoot == null, Is.True); // 카메라 밖 생성 모델 정리
            Assert.That(_host.GetComponent<RoomInvestigationUI>() == null, Is.True); // 조사 화면 담당자 정리
            Assert.That(RoomInvestigationController.IsAnyOpen, Is.False); // 전역 입력 소유 정리
        } // 메서드 종료
        private bool Open => Value<bool>("IsOpen"); // 실제 모달 상태 계약
        private T Value<T>(string name) // 실제 담당자 표시 상태 조회
        { // 메서드 시작
            PropertyInfo property = _investigation.GetType().GetProperty(name); // 공개 계약 조회
            Assert.That(property, Is.Not.Null, "조사 상태 없음: " + name); // 누락 기능의 명확한 실패
            return (T)property.GetValue(_investigation); // 실제 상태
        } // 메서드 종료
        private bool Call(string name, params object[] arguments) // 실제 입력 진입점 호출
        { // 메서드 시작
            MethodInfo method = _investigation.GetType().GetMethod(name); // 공개 조사 계약
            Assert.That(method, Is.Not.Null, "조사 입력 없음: " + name); // 누락 기능의 명확한 실패
            return (bool)method.Invoke(_investigation, arguments); // 실제 입력 처리
        } // 메서드 종료
        private RoomInvestigationTarget PrepareTarget(int index) // 실제 접근 위치와 시선 구성
        { // 메서드 시작
            if (!_walker.IsWalking) Assert.That(_walker.TryStand(), Is.True); // 실제 이동 시작
            RoomInvestigationTarget target = _investigation.Targets[index]; // 실제 모델
            CharacterController body = (CharacterController)typeof(RoomWalkingController).GetField("_body", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_walker); // 실제 이동 충돌체
            Vector3 eye = ApproachEye(target); // 도달 가능한 전면 위치
            body.enabled = false; // 시험 위치 설정 중 충돌 해제
            body.transform.position = new Vector3(eye.x, Day41BattleRoomLayout.FloorY + 0.05f, eye.z); // 실제 방 바닥
            body.enabled = true; // 실제 충돌 이동 복원
            Quaternion desired = Quaternion.LookRotation(target.Anchor - _walker.EyePosition); // 실제 조사 시선
            Vector3 current = _walker.LookRotation.eulerAngles; // 현재 시선
            Vector3 euler = desired.eulerAngles; // 목표 시선
            _walker.Move(Vector2.zero, 0f, new Vector2(Mathf.DeltaAngle(current.y, euler.y) / 0.12f, -Mathf.DeltaAngle(current.x, euler.x) / 0.12f)); // 실제 시선 입력 경로
            Physics.SyncTransforms(); // 배치된 실제 충돌 반영
            return target; // 준비 대상
        } // 메서드 종료
        private static Vector3 ApproachEye(RoomInvestigationTarget target) // 실제 눈높이의 전면 접근
        { // 메서드 시작
            Vector3 position = target.Anchor + target.Definition.Facing * 3f; // 전면 통로
            position.y = Day41BattleRoomLayout.FloorY + RoomWalkingController.EyeHeight + 0.05f; // 실제 발 높이와 눈높이
            return position; // 실제 조사 기준 위치
        } // 메서드 종료
        private void Press(params Key[] keys) // 실제 입력 프레임 주입
        { // 메서드 시작
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); // 키 상태 입력
            InputSystem.Update(); // 입력 갱신
            _keyboard.MakeCurrent(); // 현재 장치 유지
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
