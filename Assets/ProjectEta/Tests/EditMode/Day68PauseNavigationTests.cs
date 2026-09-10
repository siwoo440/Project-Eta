using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Settings; // Pause 내비게이션 상태 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day68PauseNavigationTests
    {
        [Test]
        public void PauseNavigation_설정에서뒤로가면_Pause로복귀한다()
        {
            var state = new BattlePauseNavigationState(); // Pause 내비게이션 상태 생성
            state.OpenPause(); // Pause 화면 진입
            state.ShowSettings(); // 설정 화면 진입

            bool handled = state.TryBack(); // 설정 화면 뒤로가기 실행

            Assert.IsTrue(handled); // 뒤로가기 처리 성공 검증
            Assert.AreEqual(BattlePausePanel.Pause, state.CurrentPanel); // Pause 화면 복귀 검증
        }

        [Test]
        public void PauseNavigation_Pause에서뒤로가면_닫힌다()
        {
            var state = new BattlePauseNavigationState(); // Pause 내비게이션 상태 생성
            state.OpenPause(); // Pause 화면 진입

            bool handled = state.TryBack(); // Pause 화면 뒤로가기 실행

            Assert.IsTrue(handled); // 뒤로가기 처리 성공 검증
            Assert.AreEqual(BattlePausePanel.Closed, state.CurrentPanel); // 전체 Pause 닫힘 검증
        }

        [Test]
        public void PauseNavigation_닫힌상태의뒤로가기는_처리하지않는다()
        {
            var state = new BattlePauseNavigationState(); // 기본 닫힌 상태 생성

            Assert.IsFalse(state.TryBack()); // 닫힌 상태 중복 뒤로가기 차단 검증
            Assert.IsFalse(state.IsOpen); // 닫힘 상태 유지 검증
        }
    }
}
