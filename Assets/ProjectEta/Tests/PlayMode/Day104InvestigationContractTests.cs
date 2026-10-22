using System.Collections; // 실제 컴포넌트 초기화 대기
using NUnit.Framework; // 자동 연결 계약 검사
using UnityEngine; // 실제 방 객체
using UnityEngine.TestTools; // PlayMode 실행
using ProjectEta.Environment; // 기존 이동기

namespace ProjectEta.Tests.PlayMode // 조사 자동 설치 계약
{ // 영역 시작
    public sealed class Day104InvestigationContractTests // 아직 없는 기능의 실패 재현
    { // 타입 시작
        [UnityTest] // 자동 연결 검사
        public IEnumerator WalkingSetup_InstallsOneInvestigationControllerAndFourTargets() // Inspector 없는 조사 배치
        { // 메서드 시작
            var host = new GameObject("Day104Contract"); // 독립 시험 호스트
            try // 자원 정리 보장
            { // 검사 시작
                Day41SeatedCameraRig rig = host.AddComponent<Day41SeatedCameraRig>(); // 실제 좌석 리그
                rig.Configure(host.AddComponent<Camera>()); // 실제 자동 이동 연결
                yield return null; // 자동 설치 갱신
                var type = typeof(RoomWalkingController).Assembly.GetType("ProjectEta.Environment.RoomInvestigationController"); // 실제 런타임 타입
                Assert.That(type, Is.Not.Null, "조사 담당자 없음"); // 누락 재현
                Assert.That(host.GetComponents(type).Length, Is.EqualTo(1)); // 중복 설치 없음
                var target = typeof(RoomWalkingController).Assembly.GetType("ProjectEta.Environment.RoomInvestigationTarget"); // 실제 조사 대상 타입
                Assert.That(target, Is.Not.Null); // 대상 데이터 연결
                var owner = host.GetComponent(type); // 자동 설치 담당자
                var targets = type.GetProperty("Targets").GetValue(owner) as System.Collections.ICollection; // 실제 자동 생성 대상
                Assert.That(targets, Is.Not.Null); // 조회 가능한 대상 목록
                Assert.That(targets.Count, Is.EqualTo(4)); // 기초 조사 네 곳
                rig.Configure(host.GetComponent<Camera>()); // 같은 리그 재연결
                Assert.That(host.GetComponents(type).Length, Is.EqualTo(1)); // 재설정 중복 방지
            } // 검사 종료
            finally // 소유 객체 정리
            { // 정리 시작
                Object.Destroy(host); // 시험 객체 제거
            } // 정리 종료
            yield return null; // 환경 충돌체 정리
            yield return null; // 자동 생성물 정리
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
