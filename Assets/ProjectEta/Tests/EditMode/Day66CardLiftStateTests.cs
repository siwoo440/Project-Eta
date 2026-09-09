using NUnit.Framework; // NUnit 테스트 기능
using ProjectEta.UI; // Day66CardLiftState 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day66CardLiftStateTests
    {
        [Test]
        public void ResolveTargetOffset_IdleCard_UsesLoweredOffset()
        {
            float result = Day66CardLiftState.ResolveTargetOffset(true, false, false, false); // 대기 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.IdleOffset)); // 기본 하강 위치 확인
        }

        [Test]
        public void ResolveTargetOffset_HoveredInteractableCard_UsesRaisedOffset()
        {
            float result = Day66CardLiftState.ResolveTargetOffset(true, true, false, false); // Hover 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.RaisedOffset)); // Hover 상승 위치 확인
        }

        [Test]
        public void ResolveTargetOffset_FusionSelectedCard_StaysRaised()
        {
            float result = Day66CardLiftState.ResolveTargetOffset(false, false, true, false); // Fusion 선택 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.RaisedOffset)); // 선택 유지 상승 확인
        }

        [Test]
        public void ResolveTargetOffset_LockedHoveredCard_StaysLowered()
        {
            float result = Day66CardLiftState.ResolveTargetOffset(false, true, false, false); // 잠긴 카드 Hover 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.IdleOffset)); // 잠긴 카드 하강 유지 확인
        }

        [Test]
        public void ResolveTargetOffset_PointerHeldInteractableCard_UsesRaisedOffset()
        {
            float result = Day66CardLiftState.ResolveTargetOffset(true, false, false, true); // 선택 입력 중 카드 목표 위치 계산

            Assert.That(result, Is.EqualTo(Day66CardLiftState.RaisedOffset)); // 선택 입력 상승 확인
        }
    }
}
