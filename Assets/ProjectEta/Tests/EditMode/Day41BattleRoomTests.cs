using NUnit.Framework; // NUnit 테스트 기능
using UnityEngine; // Vector2·Vector3 수학 기능
using ProjectEta.Environment; // 41일차 전투 공간·카메라 레이아웃

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class Day41BattleRoomTests // 41일차 전투 공간 회귀 테스트
    {
        [Test] // 보드·테이블 크기 관계 검증
        public void Layout_TableAndFrameLeaveBoardSurfaceClear() // 기존 10×10 보드 클릭 영역 보존 검증
        {
            Assert.Greater(Day41BattleRoomLayout.BoardFrameInnerSize, Day41BattleRoomLayout.BoardPlayableSize); // 프레임 내부 여유 검증
            Assert.Greater(Day41BattleRoomLayout.BoardFrameOuterSize, Day41BattleRoomLayout.BoardFrameInnerSize); // 프레임 외곽 크기 검증
            Assert.Greater(Day41BattleRoomLayout.TableTopSize.x, Day41BattleRoomLayout.BoardFrameOuterSize); // 테이블 가로 여유 검증
            Assert.Greater(Day41BattleRoomLayout.TableTopSize.y, Day41BattleRoomLayout.BoardFrameOuterSize); // 테이블 세로 여유 검증
        }

        [Test] // 좌석 대칭 검증
        public void Layout_PlayerAndOpponentSeatsFaceAcrossBoard() // 플레이어·상대 마주보기 구도 검증
        {
            Assert.That(Day41BattleRoomLayout.PlayerChairRoot.x, Is.EqualTo(Day41BattleRoomLayout.OpponentChairRoot.x).Within(0.001f)); // 좌석 중앙선 검증
            Assert.That(Day41BattleRoomLayout.PlayerChairRoot.z, Is.EqualTo(-Day41BattleRoomLayout.OpponentChairRoot.z).Within(0.001f)); // 좌석 전후 대칭 검증
            Assert.Less(Day41BattleRoomLayout.PlayerChairRoot.z, 0f); // 플레이어 보드 남쪽 배치 검증
            Assert.Greater(Day41BattleRoomLayout.OpponentChairRoot.z, 0f); // 상대 보드 북쪽 배치 검증
        }

        [Test] // 카메라 위치 검증
        public void Layout_AllCameraViewsStayInsideRoom() // 맨 위·기본·상대 시점 방 내부 배치 검증
        {
            Assert.IsTrue(Day41BattleRoomLayout.IsInsideRoom(Day41BattleRoomLayout.TopCameraPosition)); // 맨 위 시점 방 내부 검증
            Assert.IsTrue(Day41BattleRoomLayout.IsInsideRoom(Day41BattleRoomLayout.BasicCameraPosition)); // 기본 시점 방 내부 검증
            Assert.IsTrue(Day41BattleRoomLayout.IsInsideRoom(Day41BattleRoomLayout.SeatedCameraPosition)); // 상대 시점 방 내부 검증
            Assert.Greater(Day41BattleRoomLayout.TopCameraPosition.y, Day41BattleRoomLayout.BasicCameraPosition.y); // 맨 위 시점 높이 검증
            Assert.Greater(Day41BattleRoomLayout.BasicCameraPosition.y, Day41BattleRoomLayout.SeatedCameraPosition.y); // 기본 시점 높이 검증
        }

        [Test] // W 카메라 전환 규칙 검증
        public void Camera_WTogglesTopAndBasicAndReturnsOpponentToBasic() // 기존 두 뷰 W 전환 검증
        {
            Assert.AreEqual(Day41CameraView.Top, Day41SeatedCameraRig.ResolveWView(Day41CameraView.Basic)); // 기본에서 W로 맨 위 전환 검증
            Assert.AreEqual(Day41CameraView.Basic, Day41SeatedCameraRig.ResolveWView(Day41CameraView.Top)); // 맨 위에서 W로 기본 전환 검증
            Assert.AreEqual(Day41CameraView.Basic, Day41SeatedCameraRig.ResolveWView(Day41CameraView.Opponent)); // 상대 시점에서 W로 기본 복귀 검증
        }

        [Test] // S 카메라 전환 규칙 검증
        public void Camera_SAlwaysSelectsOpponentView() // 현재 상대 시점을 S로 선택하는 규칙 검증
        {
            Assert.AreEqual(Day41CameraView.Opponent, Day41SeatedCameraRig.ResolveSView(Day41CameraView.Top)); // 맨 위에서 S 전환 검증
            Assert.AreEqual(Day41CameraView.Opponent, Day41SeatedCameraRig.ResolveSView(Day41CameraView.Basic)); // 기본에서 S 전환 검증
            Assert.AreEqual(Day41CameraView.Opponent, Day41SeatedCameraRig.ResolveSView(Day41CameraView.Opponent)); // 상대 시점 유지 검증
        }

        [Test] // 맨 위 시점 천장 구조 가림 방지 검증
        public void Camera_TopViewHidesCeilingOccludersAndOtherViewsRestoreThem() // 1번 뷰에서만 천장 조명 구조 비활성화 검증
        {
            Assert.IsFalse(Day41SeatedCameraRig.ShouldShowTopViewOccluders(Day41CameraView.Top)); // 맨 위 시점 숨김 검증
            Assert.IsTrue(Day41SeatedCameraRig.ShouldShowTopViewOccluders(Day41CameraView.Basic)); // 기본 시점 복원 검증
            Assert.IsTrue(Day41SeatedCameraRig.ShouldShowTopViewOccluders(Day41CameraView.Opponent)); // 상대 시점 복원 검증
        }

        [Test] // 상대 시점 자유 회전 범위 검증
        public void Camera_OpponentLookIsClampedInsideOneHundredEightyDegreeRange() // 상하좌우 180도 이내 회전 검증
        {
            Vector2 clamped = Day41SeatedCameraRig.ClampOpponentLookAngles(new Vector2(300f, -300f)); // 초과 회전값 제한 계산

            Assert.That(clamped.x, Is.EqualTo(Day41BattleRoomLayout.OpponentYawHalfRange).Within(0.001f)); // 좌우 총 180도 범위 검증
            Assert.That(clamped.y, Is.EqualTo(-Day41BattleRoomLayout.OpponentPitchHalfRange).Within(0.001f)); // 상하 총 180도 미만 범위 검증
        }

        [Test] // 방 크기 검증
        public void Layout_RoomContainsTableSeatsAndCamera() // 거대 방 내부 핵심 요소 수용 검증
        {
            Assert.Greater(Day41BattleRoomLayout.RoomWidth, Day41BattleRoomLayout.TableTopSize.x * 2f); // 방 가로 여유 검증
            Assert.Greater(Day41BattleRoomLayout.RoomDepth, Mathf.Abs(Day41BattleRoomLayout.PlayerChairRoot.z) * 2f + 4f); // 방 깊이 여유 검증
            Assert.IsTrue(Day41BattleRoomLayout.IsInsideRoom(Day41BattleRoomLayout.PlayerChairRoot + Vector3.up)); // 플레이어 좌석 방 내부 검증
            Assert.IsTrue(Day41BattleRoomLayout.IsInsideRoom(Day41BattleRoomLayout.OpponentChairRoot + Vector3.up)); // 상대 좌석 방 내부 검증
        }

        [Test] // 패널 변형 규칙 검증
        public void Layout_WallPanelDepthVariationIsDeterministicAndSubtle() // 참고 이미지식 벽 패널 돌출 변화 검증
        {
            float first = Day41BattleRoomLayout.GetWallPanelDepthOffset(3, 2, 1); // 첫 패널 돌출값 계산
            float second = Day41BattleRoomLayout.GetWallPanelDepthOffset(3, 2, 1); // 동일 패널 돌출값 재계산

            Assert.That(first, Is.EqualTo(second).Within(0.0001f)); // 결정적 배치 검증
            Assert.That(first, Is.InRange(-0.08f, 0.24f)); // 과도한 돌출 방지 검증
        }
    }
}
