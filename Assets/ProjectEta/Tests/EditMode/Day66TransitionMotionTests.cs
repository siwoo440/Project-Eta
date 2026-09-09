using NUnit.Framework; // NUnit 테스트 사용
using ProjectEta.UI; // Day66TransitionMotion 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day66TransitionMotionTests
    {
        [Test]
        public void EvaluateWhooshY_StartAndEnd_MovesUpward()
        {
            float startY = 1.25f; // 시작 높이 지정
            float height = 3.0f; // 상승 거리 지정

            float start = Day66TransitionMotion.EvaluateWhooshY(startY, height, 0f); // 시작 위치 계산
            float end = Day66TransitionMotion.EvaluateWhooshY(startY, height, 1f); // 종료 위치 계산

            Assert.That(start, Is.EqualTo(startY).Within(0.0001f)); // 시작 높이 유지 확인
            Assert.That(end, Is.EqualTo(startY + height).Within(0.0001f)); // 위쪽 종료 높이 확인
        }

        [Test]
        public void EvaluateDropY_StartAndEnd_DropsToLandingHeight()
        {
            float landingY = 0.5f; // 최종 착지 높이 지정
            float height = 3.0f; // 낙하 시작 거리 지정

            float start = Day66TransitionMotion.EvaluateDropY(landingY, height, 0f); // 낙하 시작 높이 계산
            float end = Day66TransitionMotion.EvaluateDropY(landingY, height, 1f); // 착지 높이 계산

            Assert.That(start, Is.EqualTo(landingY + height).Within(0.0001f)); // 공중 시작 위치 확인
            Assert.That(end, Is.EqualTo(landingY).Within(0.0001f)); // 최종 착지 위치 확인
        }

        [Test]
        public void GetStaggerDelay_LaterIndex_HasLongerDelay()
        {
            float first = Day66TransitionMotion.GetStaggerDelay(0); // 첫 오브젝트 지연 계산
            float fourth = Day66TransitionMotion.GetStaggerDelay(3); // 네 번째 오브젝트 지연 계산

            Assert.That(first, Is.EqualTo(0f).Within(0.0001f)); // 첫 오브젝트 즉시 시작 확인
            Assert.That(fourth, Is.GreaterThan(first)); // 뒤 오브젝트 순차 지연 확인
        }

        [Test]
        public void GetSequenceDuration_MultipleObjects_IncludesStaggerAndMotion()
        {
            float one = Day66TransitionMotion.GetSequenceDuration(1); // 단일 오브젝트 연출 길이 계산
            float five = Day66TransitionMotion.GetSequenceDuration(5); // 다섯 오브젝트 연출 길이 계산

            Assert.That(one, Is.GreaterThan(0f)); // 단일 연출 시간 존재 확인
            Assert.That(five, Is.GreaterThan(one)); // 다중 오브젝트 순차 연출 시간 증가 확인
        }
    }
}
