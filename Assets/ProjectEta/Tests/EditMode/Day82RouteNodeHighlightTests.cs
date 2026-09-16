using System.Reflection; // 반짝임 계산 메서드 호출
using NUnit.Framework; // NUnit 검증 기능
using UnityEngine; // GameObject·Color·Renderer 사용
using ProjectEta.Board; // 경로 노드 표시 컴포넌트 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 시작
    public sealed class Day82RouteNodeHighlightTests // 다음 이동 노드 금빛 고리 회귀 테스트
    { // 테스트 클래스 시작
        [Test] // 선택 가능 노드 강조 전체 동작 검증
        public void SelectableNode_CreatesPulsingGoldRingAndHidesAfterSelection() // 금빛 반짝임과 선택 후 해제 검증
        { // 테스트 시작
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // 실제 선택 노드 원판 생성
            Material markerMaterial = null; // 테스트 전용 원판 머티리얼 참조

            try // 테스트 자원 정리 보장
            { // 보호 구간 시작
                Renderer markerRenderer = marker.GetComponent<Renderer>(); // 원판 렌더러 조회
                markerMaterial = new Material(Shader.Find("Standard")); // 테스트 전용 머티리얼 생성
                markerRenderer.sharedMaterial = markerMaterial; // 공유 기본 머티리얼 변경 방지
                RouteMapNodeView nodeView = marker.AddComponent<RouteMapNodeView>(); // 실제 노드 표시 컴포넌트 추가
                nodeView.Initialize("next_node", markerRenderer, Color.cyan, Color.white, Color.yellow, Color.gray); // 선택 가능 노드 초기화

                Transform highlight = marker.transform.Find("RouteNodeSelectableHighlight"); // 금빛 고리 루트 조회
                Assert.That(highlight, Is.Not.Null); // 선택 가능 노드 고리 생성 확인
                LineRenderer ring = highlight.GetComponent<LineRenderer>(); // 실제 원형 선 렌더러 조회
                Assert.That(ring, Is.Not.Null); // 고리 렌더러 존재 확인
                Assert.That(ring.loop, Is.True); // 닫힌 원형 고리 확인
                Assert.That(ring.positionCount, Is.GreaterThanOrEqualTo(48)); // 매끄러운 원형 분할 수 확인
                Assert.That(ring.startColor.r, Is.GreaterThan(ring.startColor.g)); // 금색 붉은 성분 우세 확인
                Assert.That(ring.startColor.g, Is.GreaterThan(ring.startColor.b)); // 금색 초록 성분 우세 확인

                Component pulse = marker.GetComponent("RouteNodeSelectionHighlight"); // 노드에 연결된 반짝임 컴포넌트 조회
                Assert.That(pulse, Is.Not.Null); // 반짝임 동작 연결 확인
                MethodInfo evaluateMethod = pulse.GetType().GetMethod("EvaluatePulseScale", BindingFlags.Public | BindingFlags.Static); // 시간별 크기 계산 메서드 조회
                Assert.That(evaluateMethod, Is.Not.Null); // 반짝임 계산 기능 확인
                float middleScale = (float)evaluateMethod.Invoke(null, new object[] { 0f }); // 주기 중간 크기 계산
                float peakScale = (float)evaluateMethod.Invoke(null, new object[] { 0.3f }); // 주기 최대 크기 계산
                Assert.That(peakScale, Is.GreaterThan(middleScale)); // 시간에 따른 크기 변화 확인

                nodeView.SetSelectionState(true, false); // 다음 노드 선택 완료 상태 적용
                Assert.That(highlight.gameObject.activeSelf, Is.False); // 선택 완료 뒤 고리 비활성화 확인
            } // 보호 구간 종료
            finally // 테스트 자원 정리
            { // 정리 구간 시작
                Object.DestroyImmediate(marker); // 테스트 원판 제거
                if (markerMaterial != null) Object.DestroyImmediate(markerMaterial); // 테스트 머티리얼 제거
            } // 정리 구간 종료
        } // 테스트 종료
    } // 테스트 클래스 종료
} // 네임스페이스 종료
