using System.Reflection; // 비공개 레이아웃 계산 호출
using NUnit.Framework; // NUnit 테스트 기능
using UnityEngine; // RectTransform·GameObject 사용
using UnityEngine.UI; // HorizontalLayoutGroup·LayoutRebuilder 사용
using ProjectEta.UI; // Day66CardLiftState·손패 모션 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 범위
    public sealed class Day66CardLiftStateTests // 손패 카드 상하 상태 테스트
    { // 테스트 클래스 범위
        [Test] // 기본 카드 위치 검증
        public void ResolveTargetOffset_IdleCard_UsesLoweredOffset() // 대기 카드 하강 확인
        { // 테스트 범위
            float result = Day66CardLiftState.ResolveTargetOffset(true, false, false, false); // 대기 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.IdleOffset)); // 기본 하강 위치 확인
        } // 테스트 범위 종료

        [Test] // Hover 카드 위치 검증
        public void ResolveTargetOffset_HoveredInteractableCard_UsesRaisedOffset() // Hover 카드 상승 확인
        { // 테스트 범위
            float result = Day66CardLiftState.ResolveTargetOffset(true, true, false, false); // Hover 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.RaisedOffset)); // Hover 상승 위치 확인
        } // 테스트 범위 종료

        [Test] // 합성 선택 카드 위치 검증
        public void ResolveTargetOffset_FusionSelectedCard_StaysRaised() // 합성 카드 상승 유지 확인
        { // 테스트 범위
            float result = Day66CardLiftState.ResolveTargetOffset(false, false, true, false); // Fusion 선택 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.RaisedOffset)); // 선택 유지 상승 확인
        } // 테스트 범위 종료

        [Test] // 잠긴 카드 위치 검증
        public void ResolveTargetOffset_LockedHoveredCard_StaysLowered() // 잠긴 카드 하강 확인
        { // 테스트 범위
            float result = Day66CardLiftState.ResolveTargetOffset(false, true, false, false); // 잠긴 카드 Hover 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.IdleOffset)); // 잠긴 카드 하강 유지 확인
        } // 테스트 범위 종료

        [Test] // 입력 중 카드 위치 검증
        public void ResolveTargetOffset_PointerHeldInteractableCard_UsesRaisedOffset() // 입력 중 카드 상승 확인
        { // 테스트 범위
            float result = Day66CardLiftState.ResolveTargetOffset(true, false, false, true); // 선택 입력 중 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.RaisedOffset)); // 선택 입력 상승 확인
        } // 테스트 범위 종료

        [Test] // 하단 정렬 좌표 회귀 검증
        public void ResolveLayoutBaseY_하단정렬손패에서_레이아웃좌표를유지한다() // 손패 위쪽 이동 방지 확인
        { // 테스트 범위
            GameObject parentObject = new GameObject("HandRoot_Test", typeof(RectTransform), typeof(HorizontalLayoutGroup)); // 테스트 손패 부모 생성
            GameObject cardObject = new GameObject("Card_Test", typeof(RectTransform), typeof(LayoutElement)); // 테스트 카드 생성

            try // 테스트 객체 정리 보장
            { // 정리 범위
                RectTransform parent = parentObject.GetComponent<RectTransform>(); // 손패 부모 RectTransform 조회
                parent.sizeDelta = new Vector2(1540f, 270f); // 실제 손패 부모 크기 적용
                HorizontalLayoutGroup layout = parentObject.GetComponent<HorizontalLayoutGroup>(); // 손패 레이아웃 조회
                layout.childAlignment = TextAnchor.LowerCenter; // 실제 하단 중앙 정렬 적용
                layout.padding = new RectOffset(8, 8, 5, 5); // 실제 손패 여백 적용
                layout.childControlWidth = false; // 실제 고정 너비 적용
                layout.childControlHeight = false; // 실제 고정 높이 적용
                layout.childForceExpandWidth = false; // 가로 확장 차단
                layout.childForceExpandHeight = false; // 세로 확장 차단
                RectTransform card = cardObject.GetComponent<RectTransform>(); // 카드 RectTransform 조회
                card.SetParent(parent, false); // 손패 부모에 카드 연결
                card.sizeDelta = new Vector2(178f, 254f); // 실제 카드 크기 적용
                card.pivot = new Vector2(0.5f, 0.5f); // 실제 카드 피벗 적용
                LayoutElement element = cardObject.GetComponent<LayoutElement>(); // 카드 레이아웃 요소 조회
                element.preferredWidth = 178f; // 실제 카드 너비 적용
                element.preferredHeight = 254f; // 실제 카드 높이 적용
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent); // 실제 레이아웃 좌표 계산
                float expected = card.anchoredPosition.y; // Unity가 계산한 정상 Y 저장
                MethodInfo method = typeof(Day66HandCardMotionController).GetMethod("ResolveLayoutBaseY", BindingFlags.Static | BindingFlags.NonPublic); // 실제 계산 메서드 조회
                Assert.That(method, Is.Not.Null); // 계산 메서드 존재 검증

                float actual = (float)method.Invoke(null, new object[] { card, layout }); // 현재 손패 기본 Y 계산

                Assert.That(expected, Is.LessThan(0f)); // 상단 앵커 기준 음수 좌표 검증
                Assert.That(actual, Is.EqualTo(expected).Within(0.01f)); // Unity 레이아웃 좌표 보존 검증
            } // 정리 범위 종료
            finally // 성공·실패 공통 정리
            { // 정리 범위
                Object.DestroyImmediate(parentObject); // 부모와 자식 테스트 객체 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료
    } // 테스트 클래스 종료
} // 네임스페이스 종료
