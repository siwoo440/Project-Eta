using System; // 기본 타입과 예외
using System.Reflection; // 기능 존재 확인
using NUnit.Framework; // 회귀 검사
using UnityEngine; // 실제 Resources 조회
using ProjectEta.Run; // 실제 런 타입

namespace ProjectEta.Tests.EditMode // 102일차 기획 연결 검사
{ // 영역 시작
    public sealed class Day102InitialContractTests // 구현 전 누락 재현
    { // 타입 시작
        [Test] // 실제 이벤트 데이터 누락 검사
        public void EventResource_ContainsAllThirtyFivePlannedIds() // 기획 전체 목록 존재
        { // 메서드 시작
            TextAsset asset = Resources.Load<TextAsset>("Events102"); // 실제 데이터 조회
            Assert.That(asset, Is.Not.Null, "EV01~EV35 실행 데이터 누락"); // 누락 원인 확인
            for (int id = 1; id <= 35; id++) // 전체 이벤트 순회
            { // 반복 시작
                StringAssert.Contains("EV" + id.ToString("00"), asset.text); // 개별 ID 누락 확인
            } // 반복 종료
        } // 메서드 종료
        [Test] // 새 경로 규칙 진입 검사
        public void RouteRules_ExposePhaseAwareGraphCreation() // 페이즈별 새 그래프
        { // 메서드 시작
            Type type = typeof(RunState).Assembly.GetType("ProjectEta.Run.Day102RouteRules"); // 실제 런타임 경로 타입
            Assert.That(type, Is.Not.Null, "전투 횟수 제한 경로 규칙 누락"); // 규칙 누락 재현
            Assert.That(type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static), Is.Not.Null); // 실행 진입 확인
        } // 메서드 종료
        [Test] // 결과 저장 계약 검사
        public void EventResolution_PreservesSessionAndHistoryInRunData() // 런 저장 구조 연결
        { // 메서드 시작
            Assert.That(typeof(RunBalanceData).GetField("activeEvent102"), Is.Not.Null, "이벤트 선택 고정 저장 누락"); // 활성 이벤트 스냅샷
            Assert.That(typeof(RunBalanceData).GetField("eventHistory102"), Is.Not.Null, "이벤트 손익 저장 누락"); // 완료 결과 이력
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
