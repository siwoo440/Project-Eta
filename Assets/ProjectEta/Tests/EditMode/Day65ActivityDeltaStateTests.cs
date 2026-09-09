using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectEta.Run; // RunFlowPhase·RunEconomyRules 사용
using ProjectEta.UI; // Day65ActivityDeltaState 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class Day65ActivityDeltaStateTests // 65일차 Stage Activity 결과 표시 상태 테스트
    {
        [Test] // 첫 스냅샷 기준점 테스트
        public void Capture_FirstSnapshot_ReturnsNoChange() // 최초 값은 결과 알림으로 취급하지 않음
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성

            Day65ActivityDelta delta = state.Capture(100, 3, 5); // 최초 Gold·HP·카드 수 입력

            Assert.That(delta.HasChange, Is.False); // 초기 기준점은 변화 없음 확인
        }

        [Test] // 상점 카드 구매 결과 테스트
        public void Capture_ShopCardPurchase_DescribesPurchase() // Gold 감소와 카드 증가를 구매로 판정
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성
            state.Capture(100, 3, 5); // 초기 기준점 저장

            Day65ActivityDelta delta = state.Capture(100 - RunEconomyRules.CardPurchasePrice, 3, 6); // 카드 구매 결과 입력

            Assert.That(delta.BuildSummary(RunFlowPhase.Shop), Is.EqualTo("카드 구매 완료 · Gold -30")); // 구매 결과 문구 확인
        }

        [Test] // 상점 카드 제거 결과 테스트
        public void Capture_ShopCardRemove_DescribesRemoval() // Gold 감소와 카드 감소를 제거로 판정
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성
            state.Capture(100, 3, 5); // 초기 기준점 저장

            Day65ActivityDelta delta = state.Capture(100 - RunEconomyRules.CardRemovePrice, 3, 4); // 카드 제거 결과 입력

            Assert.That(delta.BuildSummary(RunFlowPhase.Shop), Is.EqualTo("카드 제거 완료 · Gold -40")); // 제거 결과 문구 확인
        }

        [Test] // 상점 회복 결과 테스트
        public void Capture_ShopHeal_DescribesKingRecovery() // Gold 감소와 HP 증가를 회복으로 판정
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성
            state.Capture(100, 1, 5); // 초기 기준점 저장

            Day65ActivityDelta delta = state.Capture(100 - RunEconomyRules.HealPrice, 2, 5); // 회복 결과 입력

            Assert.That(delta.BuildSummary(RunFlowPhase.Shop), Is.EqualTo("King HP 회복 · HP +1 · Gold -20")); // 회복 결과 문구 확인
        }

        [Test] // 상점 강화 결과 테스트
        public void Capture_ShopUpgrade_DescribesUpgrade() // Gold 감소만 있는 강화 비용을 강화로 판정
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성
            state.Capture(100, 3, 5); // 초기 기준점 저장

            Day65ActivityDelta delta = state.Capture(100 - RunEconomyRules.CardUpgradePrice, 3, 5); // 강화 결과 입력

            Assert.That(delta.BuildSummary(RunFlowPhase.Shop), Is.EqualTo("카드 강화 완료 · Gold -50")); // 강화 결과 문구 확인
        }

        [Test] // 이벤트 위험 계약 결과 테스트
        public void Capture_EventRisk_DescribesRiskReward() // Gold 증가와 HP 감소를 위험 계약으로 판정
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성
            state.Capture(40, 3, 5); // 초기 기준점 저장

            Day65ActivityDelta delta = state.Capture(40 + RunEconomyRules.RiskRewardCurrency, 2, 5); // 위험 계약 결과 입력

            Assert.That(delta.BuildSummary(RunFlowPhase.Event), Is.EqualTo("위험한 계약 · HP -1 · Gold +60")); // 위험 계약 결과 문구 확인
        }

        [Test] // 이벤트 무료 카드 결과 테스트
        public void Capture_EventCard_DescribesCardGain() // 카드 수 증가를 무료 카드 획득으로 판정
        {
            var state = new Day65ActivityDeltaState(); // 결과 추적 상태 생성
            state.Capture(100, 3, 5); // 초기 기준점 저장

            Day65ActivityDelta delta = state.Capture(100, 3, 6); // 무료 카드 결과 입력

            Assert.That(delta.BuildSummary(RunFlowPhase.Event), Is.EqualTo("카드 획득 · 보유 카드 +1")); // 이벤트 카드 결과 문구 확인
        }
    }
}
