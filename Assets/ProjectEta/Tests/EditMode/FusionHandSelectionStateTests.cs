using NUnit.Framework; // EditMode 테스트 기능 사용
using ProjectEta.Fusion; // 합성 손패 선택 상태 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class FusionHandSelectionStateTests // 64일차 손패 슬롯 기반 합성 선택 테스트
    {
        [Test] // 같은 카드 정의여도 서로 다른 슬롯 두 개를 선택할 수 있는 기반 검증
        public void Toggle_TwoDifferentHandIndices_KeepsBothSelections()
        {
            var state = new FusionHandSelectionState(); // 빈 합성 선택 상태 생성

            Assert.IsTrue(state.Toggle(2)); // 첫 번째 동일 카드 슬롯 선택
            Assert.IsTrue(state.Toggle(5)); // 두 번째 동일 카드 슬롯 선택
            Assert.AreEqual(2, state.Count); // 재료 두 장 선택 확인
            Assert.AreEqual(2, state.Indices[0]); // 첫 번째 실제 슬롯 유지 확인
            Assert.AreEqual(5, state.Indices[1]); // 두 번째 실제 슬롯 유지 확인
        }

        [Test] // 같은 슬롯 재클릭 시 선택 해제 검증
        public void Toggle_SelectedHandIndex_RemovesOnlyThatSelection()
        {
            var state = new FusionHandSelectionState(); // 빈 합성 선택 상태 생성

            state.Toggle(1); // 첫 재료 슬롯 선택
            state.Toggle(4); // 둘째 재료 슬롯 선택

            Assert.IsTrue(state.Toggle(1)); // 첫 슬롯 다시 클릭
            Assert.AreEqual(1, state.Count); // 한 장만 남았는지 확인
            Assert.AreEqual(4, state.Indices[0]); // 다른 슬롯 선택 유지 확인
        }

        [Test] // 재료 두 장 이후 세 번째 선택 차단 검증
        public void Toggle_ThirdHandIndex_IsRejected()
        {
            var state = new FusionHandSelectionState(); // 빈 합성 선택 상태 생성

            state.Toggle(0); // 첫 재료 슬롯 선택
            state.Toggle(1); // 둘째 재료 슬롯 선택

            Assert.IsFalse(state.Toggle(2)); // 세 번째 슬롯 선택 거부 확인
            Assert.AreEqual(2, state.Count); // 기존 두 장 선택 유지 확인
        }
    }
}
