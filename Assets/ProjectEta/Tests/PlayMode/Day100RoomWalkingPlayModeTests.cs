using System.Collections; // 프레임 대기
using NUnit.Framework; // 동작 검사
using UnityEngine; // 실제 카메라 생성
using UnityEngine.InputSystem; // 실제 입력 장치
using UnityEngine.InputSystem.LowLevel; // 키 상태 주입
using UnityEngine.TestTools; // 런타임 검사
using ProjectEta.Environment; // 좌석 카메라
using ProjectEta.Board; // 실제 보드 입력
using ProjectEta.Run; // 독립 런 상태
using ProjectEta.Battle; // 실제 전투 입력
using ProjectEta.UI; // 상점 카메라 잠금

namespace ProjectEta.Tests.PlayMode // 의자 이동 검사 영역
{ // 영역 시작
    public sealed class Day100RoomWalkingPlayModeTests // 실제 키 입력 검사
    { // 타입 시작
        private InputSettings.UpdateMode _previousUpdateMode; // 기존 입력 갱신 방식
        private InputSettings.BackgroundBehavior _previousBackground; // 기존 배경 입력 처리
        private InputSettings.EditorInputBehaviorInPlayMode _previousEditorBehavior; // 기존 에디터 입력 경로
        [SetUp] // 입력 환경 준비
        public void SetUpInput() // 자동 검사 입력 프레임 분리
        { // 메서드 시작
            _previousUpdateMode = InputSystem.settings.updateMode; // 기존 입력 모드 저장
            _previousBackground = InputSystem.settings.backgroundBehavior; // 기존 배경 처리 저장
            _previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 에디터 처리 저장
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually; // 주입 프레임만 입력 갱신
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 검사 창 포커스 독립
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 실제 게임 입력 경로 선택
        } // 메서드 종료
        [UnityTest] // 이동 중 필수 화면 검사
        public IEnumerator RewardOpenedWhileWalking_ReturnsToSeatAndReleasesTableInput() // 보상 화면 진행 불가 재현
        { // 메서드 시작
            var root = new GameObject("Day100ModalTest"); // 시험 카메라 호스트
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 카메라
            rig.Configure(root.AddComponent<Camera>()); // 카메라 연결
            RoomWalkingController walker = root.GetComponent<RoomWalkingController>(); // 실제 이동기
            CardRewardUI reward = root.AddComponent<CardRewardUI>(); // 실제 보상 화면
            try // 필수 화면 검사 범위
            { // 검사 시작
                Assert.That(walker.TryStand(), Is.True); // 의자 이탈
                reward.Show(new ProjectEta.Pieces.PieceDefinition[0], CardRewardSource.EliteVictory, null); // 실제 보상 화면 열기
                yield return null; // 화면 소유 상태 갱신
                yield return null; // 이동 안내 최종 갱신
                Assert.That(reward.IsVisible, Is.True); // 필수 보상 화면 유지
                Assert.That(walker.IsWalking, Is.False, "보상 화면이 뜨면 이동 차단막 해제를 위해 좌석 복귀 필요"); // 진행 불가 차단
                Assert.That(RoomWalkingController.BlocksTableInput, Is.False); // 필수 선택 입력 복원
                Assert.That(GameObject.Find("RoomWalkingCanvas_Day100"), Is.Null); // 이동 중 UI 차단막 비활성 확인
            } // 검사 종료
            finally // 시험 객체 정리
            { // 정리 시작
                Object.Destroy(root); // 카메라·보상 화면 제거
            } // 정리 종료
            yield return null; // 파괴 완료 대기
        } // 메서드 종료
        [TearDown] // 입력 환경 복원
        public void RestoreInput() // 다른 검사 입력 설정 보호
        { // 메서드 시작
            InputSystem.settings.updateMode = _previousUpdateMode; // 입력 갱신 방식 복원
            InputSystem.settings.backgroundBehavior = _previousBackground; // 배경 입력 처리 복원
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorBehavior; // 에디터 입력 경로 복원
        } // 메서드 종료
        [UnityTest] // 런타임 검사 지정
        public IEnumerator W_LeavesCameraViewUnchangedAndStartsWalking() // 이동키와 카메라 충돌 재현
        { // 메서드 시작
            var root = new GameObject("Day100WalkingTest"); // 카메라 호스트 생성
            Camera camera = root.AddComponent<Camera>(); // 실제 카메라 생성
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 좌석 리그 생성
            rig.Configure(camera); // 기본 시점 연결
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 생성
            try // 검사와 정리 분리
            { // 검사 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // W 누름 입력
                InputSystem.Update(); // 입력 상태 반영
                keyboard.MakeCurrent(); // 현재 시험 장치 선택
                yield return null; // 실제 리그 입력 처리
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Basic), "W는 시점 전환 대신 일어나기·이동 전용"); // 기존 시점 충돌 차단 확인
                Assert.That(rig.IsWalking, Is.True); // 실제 일어나기 확인
            } // 검사 종료
            finally // 시험 자원 정리
            { // 정리 시작
                InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
                Object.Destroy(root); // 시험 카메라 제거
            } // 정리 종료
            yield return null; // 파괴 완료 대기
        } // 메서드 종료
        [UnityTest] // 실제 이동·복귀 검사
        public IEnumerator Movement_CollidesWithTableAndWallsAndCannotSitFarAway() // 충돌과 거리 제한 확인
        { // 메서드 시작
            var root = new GameObject("Day100CollisionTest"); // 시험 카메라 호스트
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
            rig.Configure(root.AddComponent<Camera>()); // 실제 카메라 연결
            RoomWalkingController walker = root.GetComponent<RoomWalkingController>(); // 실제 이동기 조회
            try // 충돌 검사 범위
            { // 검사 시작
                Assert.That(walker.TryStand(), Is.True); // 의자 이탈 실행
                Physics.SyncTransforms(); // 생성된 충돌체 물리 반영
                for (int step = 0; step < 60; step++) walker.Move(Vector2.up, 0.1f, Vector2.zero); // 테이블 방향 이동
                Assert.That(walker.FeetPosition.z, Is.LessThan(-6.7f)); // 테이블 관통 차단
                Assert.That(walker.FeetPosition.y, Is.GreaterThanOrEqualTo(Day41BattleRoomLayout.FloorY - 0.1f)); // 바닥 낙하 차단
                Assert.That(walker.CanSit, Is.False); // 의자 거리 벗어남 확인
                Assert.That(walker.TrySit(), Is.False); // 멀리서 앉기 차단
                for (int step = 0; step < 80; step++) walker.Move(Vector2.right, 0.1f, Vector2.zero); // 동쪽 벽 방향 이동
                Assert.That(walker.FeetPosition.x, Is.InRange(14f, 15.6f)); // 벽 앞 정지 확인
                Assert.That(Day41BattleRoomLayout.IsInsideRoom(walker.EyePosition), Is.True); // 방 내부 유지
                RaycastHit[] hits = Physics.RaycastAll(new Vector3(3f, 8f, -10f), Vector3.down, 20f); // 기존 보드 Raycast 조건
                Assert.That(System.Array.Exists(hits, hit => hit.collider.transform.IsChildOf(GameObject.Find("RoomWalkingColliders_Day100").transform)), Is.False); // 이동 충돌체의 보드 클릭 가림 차단
            } // 검사 종료
            finally // 시험 객체 정리
            { // 정리 시작
                Object.Destroy(root); // 카메라·이동기 제거
            } // 정리 종료
            yield return null; // 충돌체 제거 대기
            yield return null; // 독립 루트 제거 대기
            Assert.That(RoomWalkingController.BlocksTableInput, Is.False); // 씬 종료 입력 차단 해제
            Assert.That(GameObject.Find("RoomWalkingColliders_Day100"), Is.Null); // 생성 환경 자원 정리
        } // 메서드 종료
        [UnityTest] // 복귀·일시정지 검사
        public IEnumerator Pause_BlocksMovementAndNearChairFRestoresSeatedView() // 실제 F키와 일시정지 확인
        { // 메서드 시작
            var root = new GameObject("Day100SeatTest"); // 좌석 시험 호스트
            Camera camera = root.AddComponent<Camera>(); // 시험 카메라
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
            rig.Configure(camera); // 기본 시점 연결
            RoomWalkingController walker = root.GetComponent<RoomWalkingController>(); // 실제 이동기
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드
            float previousScale = Time.timeScale; // 기존 시간 배율 보관
            CursorLockMode previousLock = Cursor.lockState; // 기존 커서 잠금 보관
            bool previousVisible = Cursor.visible; // 기존 커서 표시 보관
            try // 복귀 검사 범위
            { // 검사 시작
                Assert.That(walker.TryStand(), Is.True); // 일어나기 실행
                Vector3 start = walker.FeetPosition; // 이동 전 발 위치
                Time.timeScale = 0f; // 일시정지 재현
                walker.Move(Vector2.up, 0.1f, Vector2.one * 100f); // 일시정지 중 조작 시도
                Assert.That(walker.FeetPosition, Is.EqualTo(start)); // 이동 차단 확인
                Assert.That(walker.TrySit(), Is.False); // 일시정지 중 F 처리 차단
                Time.timeScale = previousScale; // 게임 시간 복원
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); // 의자 옆 F키 입력
                InputSystem.Update(); // 현재 입력 프레임 반영
                keyboard.MakeCurrent(); // 시험 장치 선택
                yield return null; // 실제 입력 프레임 대기
                yield return null; // 복귀 시점 갱신 대기
                Assert.That(walker.IsWalking, Is.False); // 실제 앉기 완료
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Basic)); // 이전 보드 시점 유지
                Assert.That(Vector3.Distance(camera.transform.position, Day41BattleRoomLayout.BasicCameraPosition), Is.LessThan(0.01f)); // 좌석 카메라 위치 복원
                Assert.That(Cursor.lockState, Is.EqualTo(previousLock)); // 커서 잠금 복원
                Assert.That(Cursor.visible, Is.EqualTo(previousVisible)); // 커서 표시 복원
            } // 검사 종료
            finally // 시험 상태 정리
            { // 정리 시작
                Time.timeScale = previousScale; // 시간 배율 안전 복원
                InputSystem.RemoveDevice(keyboard); // 키보드 제거
                Object.Destroy(root); // 좌석 시험 객체 제거
            } // 정리 종료
            yield return null; // 파괴 완료 대기
        } // 메서드 종료
        [UnityTest] // 전투 입력 검사
        public IEnumerator Walking_BlocksCardAndTurnActionsUntilSeated() // 실제 보드 권한 확인
        { // 메서드 시작
            var root = new GameObject("Day100InputTest"); // 시험 입력 호스트
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 좌석 리그 생성
            rig.Configure(root.AddComponent<Camera>()); // 실제 카메라 연결
            BoardView board = root.AddComponent<BoardView>(); // 실제 보드 생성
            BoardInputController input = root.AddComponent<BoardInputController>(); // 실제 입력 생성
            var run = new RunState(100); // 독립 런 생성
            var turns = new TurnManager(); // 실제 배치 턴 생성
            board.Bind(run.Board); // 보드 데이터 연결
            input.Bind(run, board, turns, new BattleHooks()); // 보드 입력 연결
            RoomWalkingController walker = root.GetComponent<RoomWalkingController>(); // 실제 이동기
            try // 입력 권한 검사
            { // 검사 시작
                Assert.That(input.CanUseDeploymentInput, Is.True); // 이동 전 배치 가능
                Assert.That(walker.TryStand(), Is.True); // 의자 이탈 실행
                Assert.That(input.CanReceivePlayerInput, Is.False); // 일반 입력 차단
                Assert.That(input.CanUseDeploymentInput, Is.False); // 배치 차단
                Assert.That(input.CanUseCardSummonInput, Is.False); // 카드 소환 차단
                Assert.That(input.CanUseFusionInput, Is.False); // 합성 차단
                Assert.That(walker.TrySit(), Is.True); // 의자 옆 복귀
                Assert.That(input.CanUseDeploymentInput, Is.True); // 좌석 복귀 후 배치 복원
            } // 검사 종료
            finally // 시험 객체 정리
            { // 정리 시작
                Object.Destroy(root); // 보드·이동기 제거
            } // 정리 종료
            yield return null; // 파괴 대기
        } // 메서드 종료
        [UnityTest] // 시점 키 검사
        public IEnumerator ViewKeys_PreserveHandKeysAndRestoreRoofAfterWalking() // 실제 시점 전환과 천장 확인
        { // 메서드 시작
            var root = new GameObject("Day100ViewTest"); // 시험 카메라 호스트
            var room = new GameObject(Day41BattleRoomLayout.RootName); // 시험 방 루트
            var roof = new GameObject(Day41BattleRoomLayout.TopViewOccluderRootName); // 시험 천장 구조
            roof.transform.SetParent(room.transform); // 천장 루트 연결
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
            rig.Configure(root.AddComponent<Camera>()); // 실제 카메라 연결
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드
            try // 시점 검사 범위
            { // 검사 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1)); // 기존 카드 선택 키
                InputSystem.Update(); // 현재 입력 프레임 반영
                keyboard.MakeCurrent(); // 시험 장치 선택
                yield return null; // 실제 입력 대기
                yield return null; // 리그 입력 완료 대기
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Basic)); // 숫자키 카메라 충돌 차단
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F2)); // 맨 위 시점 키
                InputSystem.Update(); // 현재 입력 프레임 반영
                keyboard.MakeCurrent(); // 시험 장치 선택
                yield return null; // 실제 입력 대기
                yield return null; // 리그 입력 완료 대기
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Top)); // 맨 위 전환 확인
                Assert.That(roof.activeSelf, Is.False); // 맨 위 시점 천장 숨김
                RoomWalkingController walker = root.GetComponent<RoomWalkingController>(); // 실제 이동기 조회
                Assert.That(walker.TryStand(), Is.True); // 맨 위 시점에서 일어나기
                Assert.That(roof.activeSelf, Is.True); // 걷기 중 천장 복원
                Assert.That(walker.TrySit(), Is.True); // 의자 복귀
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Top)); // 이전 선택 시점 유지
                Assert.That(roof.activeSelf, Is.False); // 맨 위 천장 상태 복원
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F4)); // 주변 둘러보기 키
                InputSystem.Update(); // 현재 입력 프레임 반영
                keyboard.MakeCurrent(); // 시험 장치 선택
                yield return null; // 실제 입력 대기
                yield return null; // 리그 입력 완료 대기
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Opponent)); // 기존 주변 둘러보기 유지
            } // 검사 종료
            finally // 시험 객체 정리
            { // 정리 시작
                InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
                Object.Destroy(root); // 시험 카메라 제거
                Object.Destroy(room); // 시험 천장 제거
            } // 정리 종료
            yield return null; // 파괴 완료 대기
        } // 메서드 종료
        [UnityTest] // 실제 상점 고정 연동 검사
        public IEnumerator ActivityLock_KeepsLegacyCameraDisabledAndRestoresView() // 기존 카메라 재활성 충돌 재현
        { // 메서드 시작
            var root = new GameObject("Day100ActivityLockTest"); // 실제 리그 호스트
            Camera camera = root.AddComponent<Camera>(); // 실제 카메라 생성
            camera.tag = "MainCamera"; // 기존 카메라 잠금 탐색 연결
            TableCameraRig legacy = root.AddComponent<TableCameraRig>(); // 기존 카메라 제어 생성
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 좌석 카메라 연결
            rig.Configure(camera); // 기본 시점 연결
            BoardView board = root.AddComponent<BoardView>(); // 실제 기준 보드
            StageActivityCameraLock cameraLock = root.AddComponent<StageActivityCameraLock>(); // 실제 상점 카메라 잠금
            cameraLock.Initialize(board); // 기준 보드 연결
            try // 잠금 검사 범위
            { // 검사 시작
                Assert.That(legacy.enabled, Is.False); // 좌석 리그의 기존 카메라 차단 확인
                Assert.That(cameraLock.LockToPrimaryView(), Is.True); // 실제 상점 고정 실행
                yield return null; // 카메라 최종 갱신 대기
                Assert.That(legacy.enabled, Is.False, "상점 고정 중 기존 W/S 카메라 재활성 금지"); // 고정 중 제어 충돌 차단
                Assert.That(root.GetComponent<RoomWalkingController>().TryStand(), Is.False); // 고정 화면 의자 이탈 차단
                cameraLock.RestorePreviousView(); // 실제 상점 종료 복원
                yield return null; // 좌석 시점 복원 대기
                Assert.That(legacy.enabled, Is.False); // 좌석 제어 소유 유지
                Assert.That(rig.enabled, Is.True); // 좌석 입력 복원
                Assert.That(rig.CurrentView, Is.EqualTo(Day41CameraView.Basic)); // 이전 시점 유지
            } // 검사 종료
            finally // 시험 객체 정리
            { // 정리 시작
                Object.Destroy(root); // 카메라·보드 제거
            } // 정리 종료
            yield return null; // 파괴 완료 대기
        } // 메서드 종료
        [UnityTest] // 대각선 이동 속도 검사
        public IEnumerator DiagonalMovement_HasSameSpeedAndLookPitchNeverFlips() // 이동·시선 제한 확인
        { // 메서드 시작
            var root = new GameObject("Day100DiagonalTest"); // 시험 카메라 생성
            Day41SeatedCameraRig rig = root.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 제어
            rig.Configure(root.AddComponent<Camera>()); // 실제 카메라 연결
            RoomWalkingController walker = root.GetComponent<RoomWalkingController>(); // 실제 이동기
            try // 이동 검사 범위
            { // 검사 시작
                Assert.That(walker.TryStand(), Is.True); // 의자 이탈
                Physics.SyncTransforms(); // 충돌 갱신
                Vector3 start = walker.FeetPosition; // 직선 시작 위치
                walker.Move(Vector2.up, 0.1f, Vector2.zero); // 직선 한 걸음
                Vector3 straight = walker.FeetPosition - start; // 직선 실제 이동
                Assert.That(walker.TrySit(), Is.True); // 의자 복귀
                Assert.That(walker.TryStand(), Is.True); // 같은 시작점 복원
                start = walker.FeetPosition; // 대각선 시작 위치
                walker.Move(Vector2.one, 0.1f, Vector2.zero); // 대각선 한 걸음
                Vector3 diagonal = walker.FeetPosition - start; // 대각선 실제 이동
                Assert.That(new Vector2(diagonal.x, diagonal.z).magnitude, Is.EqualTo(new Vector2(straight.x, straight.z).magnitude).Within(0.005f)); // 대각선 가속 차단
                walker.Move(Vector2.zero, 0.1f, new Vector2(12000f, 12000f)); // 과도한 마우스 회전
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, walker.LookRotation.eulerAngles.x)), Is.LessThanOrEqualTo(85.01f)); // 상하 뒤집힘 차단
            } // 검사 종료
            finally // 시험 객체 정리
            { // 정리 시작
                Object.Destroy(root); // 카메라·충돌체 제거
            } // 정리 종료
            yield return null; // 파괴 대기
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
