using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Boss; // BossHealthUI 표시 규칙 사용
using ProjectEta.Run; // BoardMode·RunFlowPhase 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class BossHealthPresentationTests
    {
        [TestCase(BoardMode.Battle, RunFlowPhase.Battle, true)]
        [TestCase(BoardMode.Map, RunFlowPhase.Map, false)]
        [TestCase(BoardMode.Map, RunFlowPhase.Reward, false)]
        [TestCase(BoardMode.Map, RunFlowPhase.Shop, false)]
        [TestCase(BoardMode.Map, RunFlowPhase.Event, false)]
        [TestCase(BoardMode.Battle, RunFlowPhase.Completed, false)]
        [TestCase(BoardMode.Battle, RunFlowPhase.Failed, false)]
        public void ShouldPresent_OnlyAllowsActualBattleScreen(BoardMode boardMode, RunFlowPhase flowPhase, bool expected)
        {
            bool actual = BossHealthUI.ShouldPresent(boardMode, flowPhase); // 보스 체력바 화면 표시 규칙 계산

            Assert.That(actual, Is.EqualTo(expected)); // 전투 화면 외 잔존 보스바 차단 확인
        }
    }
}
