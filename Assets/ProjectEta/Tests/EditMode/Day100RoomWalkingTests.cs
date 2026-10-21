using NUnit.Framework; // 거리 조건 검사
using UnityEngine; // 실제 위치 계산
using ProjectEta.Environment; // 이동 정책

namespace ProjectEta.Tests.EditMode // 방 이동 규칙 검사 영역
{ // 영역 시작
    public sealed class Day100RoomWalkingTests // 의자 접근 조건 검사
    { // 타입 시작
        [Test] // 가까운 의자 조건
        public void ChairReach_AcceptsNearbyFloorAndRejectsFarOrRaisedPosition() // 수평·높이 조건 확인
        { // 메서드 시작
            Vector3 chair = Day41BattleRoomLayout.PlayerChairRoot; // 실제 의자 위치
            Assert.That(RoomWalkingController.IsWithinChairReach(chair + Vector3.right * 2.8f), Is.True); // 의자 옆 앉기 허용
            Assert.That(RoomWalkingController.IsWithinChairReach(chair + Vector3.right * 4f), Is.False); // 먼 거리 앉기 거부
            Assert.That(RoomWalkingController.IsWithinChairReach(chair + Vector3.up * 2f), Is.False); // 높은 위치 앉기 거부
            Assert.That(RoomWalkingController.IsWithinChairReach(chair + Vector3.back * 3f), Is.True); // 의자 뒤 접근 허용
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
