using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // AttackAnimationPhase, AttackAnimationStateMachine, AttackAnimationTimings를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day30AttackAnimationTests // 30일차 공격 연출 상태 머신의 단계 전이 순서·타이밍을 검증하는 테스트 모음(Unity 좌표·코루틴과 무관한 순수 로직만 검증)
    {
        private static AttackAnimationTimings CreateShortTimings() // 테스트 계산을 단순화하기 위한 짧고 균일한 타이밍(모두 0.1초)
        {
            return new AttackAnimationTimings
            {
                RisingSeconds = 0.1f,
                ApproachingSeconds = 0.1f,
                StrikingSeconds = 0.1f,
                RecoveringSeconds = 0.1f
            };
        }

        [Test] // 시작 직후에는 Idle이 아니라 Rising 단계여야 함을 검증
        public void Start_EntersRisingPhase()
        {
            var machine = new AttackAnimationStateMachine(CreateShortTimings()); // 테스트용 짧은 타이밍으로 생성

            machine.Start(); // 연출 시작

            Assert.AreEqual(AttackAnimationPhase.Rising, machine.CurrentPhase); // 첫 단계는 Rising이어야 함
            Assert.IsFalse(machine.IsComplete); // 아직 완료 상태가 아니어야 함
        }

        [Test] // Rising -> Approaching -> Striking -> Recovering -> Complete 순서로 정확히 전이되는지 검증
        public void Advance_TransitionsThroughAllPhasesInOrder()
        {
            var machine = new AttackAnimationStateMachine(CreateShortTimings()); // 각 단계 0.1초로 균일한 테스트용 타이밍
            machine.Start(); // Rising으로 시작

            machine.Advance(0.1f); // Rising 지속 시간 소진
            Assert.AreEqual(AttackAnimationPhase.Approaching, machine.CurrentPhase); // Approaching으로 전이돼야 함

            machine.Advance(0.1f); // Approaching 지속 시간 소진
            Assert.AreEqual(AttackAnimationPhase.Striking, machine.CurrentPhase); // Striking으로 전이돼야 함

            machine.Advance(0.1f); // Striking 지속 시간 소진
            Assert.AreEqual(AttackAnimationPhase.Recovering, machine.CurrentPhase); // Recovering으로 전이돼야 함

            machine.Advance(0.1f); // Recovering 지속 시간 소진
            Assert.AreEqual(AttackAnimationPhase.Complete, machine.CurrentPhase); // Complete로 전이돼야 함
            Assert.IsTrue(machine.IsComplete); // 완료 플래그도 true여야 함
        }

        [Test] // 단계 지속 시간에 못 미치는 시간만 흘러도 같은 단계에 머무는지 검증
        public void Advance_StaysInSamePhase_WhenElapsedTimeIsBelowDuration()
        {
            var machine = new AttackAnimationStateMachine(CreateShortTimings()); // 각 단계 0.1초
            machine.Start(); // Rising으로 시작

            machine.Advance(0.05f); // 지속 시간의 절반만 경과

            Assert.AreEqual(AttackAnimationPhase.Rising, machine.CurrentPhase); // 아직 Rising에 머물러야 함
            Assert.AreEqual(0.5f, machine.GetPhaseProgress01(), 0.0001f); // 진행률은 절반이어야 함
        }

        [Test] // 프레임 드랍으로 한 번에 여러 단계 분량의 시간이 흘러도 한 번의 Advance 호출 안에서 정확한 최종 단계까지 이월되는지 검증
        public void Advance_LargeDeltaTime_CatchesUpMultiplePhasesInOneCall()
        {
            var machine = new AttackAnimationStateMachine(CreateShortTimings()); // 각 단계 0.1초
            machine.Start(); // Rising으로 시작

            machine.Advance(0.35f); // Rising(0.1) + Approaching(0.1) + Striking(0.1)을 넘는 큰 델타타임을 한 번에 전달

            Assert.AreEqual(AttackAnimationPhase.Recovering, machine.CurrentPhase, "Advance 한 번 호출로도 초과분만큼 여러 단계를 이월해 정확한 단계에 도달해야 합니다."); // 누적 이월로 Recovering까지 도달
        }

        [Test] // Idle 상태에서는 Advance를 호출해도 아무 일도 일어나지 않는지 검증(Start를 호출하지 않은 경우 안전한지 확인)
        public void Advance_BeforeStart_DoesNothing()
        {
            var machine = new AttackAnimationStateMachine(CreateShortTimings()); // Start를 호출하지 않은 상태

            machine.Advance(1f); // 임의의 큰 델타타임 전달

            Assert.AreEqual(AttackAnimationPhase.Idle, machine.CurrentPhase); // 여전히 Idle이어야 함
            Assert.IsFalse(machine.IsComplete); // 완료 상태도 아니어야 함
        }

        [Test] // Complete 이후에는 더 이상 단계가 바뀌지 않고 그대로 유지되는지 검증
        public void Advance_AfterComplete_RemainsComplete()
        {
            var machine = new AttackAnimationStateMachine(CreateShortTimings()); // 각 단계 0.1초
            machine.Start(); // Rising으로 시작
            machine.Advance(10f); // 전체 연출을 한 번에 끝낼 만큼 충분히 큰 델타타임

            Assert.IsTrue(machine.IsComplete); // 이미 완료 상태여야 함

            machine.Advance(1f); // 완료 이후 추가 Advance 호출

            Assert.AreEqual(AttackAnimationPhase.Complete, machine.CurrentPhase); // 여전히 Complete여야 함
        }

        [Test] // 타이밍을 지정하지 않으면 기본 임시값(0보다 큰 지속 시간)으로 생성되는지 검증
        public void DefaultTimings_AreAllPositive()
        {
            var machine = new AttackAnimationStateMachine(); // 기본 생성자(타이밍 미지정)
            machine.Start(); // Rising으로 시작

            machine.Advance(0.0001f); // 아주 작은 시간만 경과

            Assert.AreEqual(AttackAnimationPhase.Rising, machine.CurrentPhase, "기본 타이밍도 0보다 커서 아주 작은 델타타임으로는 다음 단계로 넘어가지 않아야 합니다."); // 기본값이 0이 아님을 간접 확인
        }
    }
}
