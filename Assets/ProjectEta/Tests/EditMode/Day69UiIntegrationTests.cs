using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.UI; // 69일차 UI 통합 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day69UiIntegrationTests
    {
        [Test]
        public void CanvasLayers_기본모달시스템오버레이순서를_유지한다()
        {
            Assert.That(UiLayerOrder.BattleHud, Is.LessThan(UiLayerOrder.RewardModal)); // 기본 HUD보다 Reward 모달이 위인지 검증
            Assert.That(UiLayerOrder.RewardModal, Is.LessThan(UiLayerOrder.RunResult)); // Reward보다 Run Result가 위인지 검증
            Assert.That(UiLayerOrder.RunResult, Is.LessThan(UiLayerOrder.BattleAnnouncement)); // Run Result보다 전투 알림이 위인지 검증
            Assert.That(UiLayerOrder.BattleAnnouncement, Is.LessThan(UiLayerOrder.Pause)); // 전투 알림보다 Pause가 위인지 검증
            Assert.That(UiLayerOrder.Pause, Is.LessThan(UiLayerOrder.Tutorial)); // Pause보다 튜토리얼이 위인지 검증
            Assert.That(UiLayerOrder.Tutorial, Is.LessThan(UiLayerOrder.SystemToast)); // 튜토리얼보다 시스템 Toast가 위인지 검증
        }

        [TestCase(false, false, false, false, false, true)]
        [TestCase(true, false, false, false, false, false)]
        [TestCase(false, true, false, false, false, false)]
        [TestCase(false, false, true, false, false, false)]
        [TestCase(false, false, false, true, false, false)]
        [TestCase(false, false, false, false, true, false)]
        public void SystemToast_모달화면이열리면_새표시를차단한다(bool pause, bool tutorial, bool reward, bool activity, bool runResult, bool expected)
        {
            var state = new Day69UiModalState(pause, tutorial, reward, activity, runResult); // 모달 상태 조합 생성
            Assert.That(Day69UiPresentationRules.CanShowSystemToast(state), Is.EqualTo(expected)); // Toast 표시 가능 여부 검증
        }

        [Test]
        public void SystemToast_전투알림재생중에는_새표시를차단한다()
        {
            var state = new Day69UiModalState(false, false, false, false, false, true); // 전투 알림 재생 상태 생성
            Assert.That(Day69UiPresentationRules.CanShowSystemToast(state), Is.False); // 전투 알림 위 Toast 중첩 차단 검증
        }

        [TestCase(false, false, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        public void Pause_튜토리얼과런결과에서는_새진입을차단한다(bool tutorial, bool runResult, bool expected)
        {
            var state = new Day69UiModalState(false, tutorial, false, false, runResult); // Pause 진입 관련 상태 생성
            Assert.That(Day69UiPresentationRules.CanOpenPause(state), Is.EqualTo(expected)); // Pause 진입 가능 여부 검증
        }

        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void DevelopmentUi_에디터또는개발빌드에서만_생성한다(bool isEditor, bool isDebugBuild, bool expected)
        {
            Assert.That(Day69UiPresentationRules.ShouldCreateDevelopmentUi(isEditor, isDebugBuild), Is.EqualTo(expected)); // 개발용 UI 생성 정책 검증
        }
    }
}
