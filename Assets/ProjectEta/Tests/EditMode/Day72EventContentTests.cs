using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Pieces; // PieceGrade 사용
using ProjectEta.Run; // Day72 Event 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day72EventContentTests
    {
        [Test]
        public void EventSeed_같은노드는항상동일하다()
        {
            int first = StageEventGenerator.CreateSeed(12031, 3, 6, "phase_3_depth_6_event"); // 첫 이벤트 Seed 생성
            int second = StageEventGenerator.CreateSeed(12031, 3, 6, "phase_3_depth_6_event"); // 동일 조건 Seed 재생성

            Assert.AreEqual(first, second); // 동일 Node 이벤트 재현성 검증
        }

        [Test]
        public void EventSeed_페이즈가다르면달라진다()
        {
            int phaseOne = StageEventGenerator.CreateSeed(12031, 1, 4, "depth_4_event"); // 1페이즈 이벤트 Seed 생성
            int phaseTwo = StageEventGenerator.CreateSeed(12031, 2, 4, "phase_2_depth_4_event"); // 2페이즈 이벤트 Seed 생성

            Assert.AreNotEqual(phaseOne, phaseTwo); // Phase별 이벤트 Seed 분리 검증
        }

        [Test]
        public void EventCatalog_정식이벤트7종을포함한다()
        {
            Assert.GreaterOrEqual(StageEventGenerator.AllDefinitions.Count, 7); // Day72 이벤트 종류 확장 검증
        }

        [Test]
        public void EventPool_초반과후반콘텐츠가분리된다()
        {
            bool phaseOneHasTraining = ContainsEventType(StageEventGenerator.GetEligibleDefinitions(1), StageEventType.Training); // 1페이즈 훈련 등장 여부 조회
            bool phaseThreeHasTraining = ContainsEventType(StageEventGenerator.GetEligibleDefinitions(3), StageEventType.Training); // 3페이즈 훈련 등장 여부 조회
            bool phaseOneHasMerchant = ContainsEventType(StageEventGenerator.GetEligibleDefinitions(1), StageEventType.TravelingMerchant); // 1페이즈 상인 등장 여부 조회
            bool phaseTwoHasMerchant = ContainsEventType(StageEventGenerator.GetEligibleDefinitions(2), StageEventType.TravelingMerchant); // 2페이즈 상인 등장 여부 조회

            Assert.IsFalse(phaseOneHasTraining); // 초반 훈련 이벤트 제한 검증
            Assert.IsTrue(phaseThreeHasTraining); // 중반 이후 훈련 이벤트 등장 검증
            Assert.IsFalse(phaseOneHasMerchant); // 1페이즈 상인 이벤트 제한 검증
            Assert.IsTrue(phaseTwoHasMerchant); // 2페이즈 이후 상인 이벤트 등장 검증
        }

        [Test]
        public void RiskChoice_HP1에서는선택할수없다()
        {
            var choice = new StageEventChoice("risk", "위험", "테스트", StageEventChoiceEffectType.RiskRewardCard); // 위험 선택지 생성

            bool blocked = StageEventService.CanChoose(1, 100, true, choice, 3); // HP 1 선택 가능 여부 조회
            bool allowed = StageEventService.CanChoose(2, 100, true, choice, 3); // HP 2 선택 가능 여부 조회

            Assert.IsFalse(blocked); // 즉사 위험 선택 차단 검증
            Assert.IsTrue(allowed); // 생존 가능 위험 선택 허용 검증
        }

        [Test]
        public void PaidChoices_재화와상태조건을검사한다()
        {
            var purchase = new StageEventChoice("buy", "구매", "테스트", StageEventChoiceEffectType.PurchaseCard); // 카드 구매 선택지 생성
            var paidHeal = new StageEventChoice("heal", "회복", "테스트", StageEventChoiceEffectType.PaidHeal); // 유료 회복 선택지 생성

            Assert.IsFalse(StageEventService.CanChoose(2, StageEventRules.TravelingMerchantCardPrice - 1, true, purchase, 3)); // Gold 부족 구매 차단 검증
            Assert.IsTrue(StageEventService.CanChoose(2, StageEventRules.TravelingMerchantCardPrice, true, purchase, 3)); // 정확한 Gold 구매 허용 검증
            Assert.IsFalse(StageEventService.CanChoose(3, 100, true, paidHeal, 3)); // 최대 HP 유료 회복 차단 검증
            Assert.IsTrue(StageEventService.CanChoose(2, StageEventRules.ShrineHealPrice, true, paidHeal, 3)); // 회복 필요·Gold 충족 허용 검증
        }

        [Test]
        public void EventCardProfile_후반에3성가중치가높아진다()
        {
            CardRewardProfile early = StageEventRules.GetCardRewardProfile(1, 2); // 초반 이벤트 카드 품질 조회
            CardRewardProfile late = StageEventRules.GetCardRewardProfile(5, 8); // 후반 이벤트 카드 품질 조회

            Assert.Greater(late.GetGradeWeight(PieceGrade.ThreeStar), early.GetGradeWeight(PieceGrade.ThreeStar)); // 후반 3성 가중치 증가 검증
            Assert.Less(late.GetGradeWeight(PieceGrade.OneStar), early.GetGradeWeight(PieceGrade.OneStar)); // 후반 1성 가중치 감소 검증
        }

        private static bool ContainsEventType(System.Collections.Generic.IReadOnlyList<StageEventDefinition> definitions, StageEventType eventType)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].EventType == eventType) return true; // 이벤트 종류 발견 반환
            }

            return false; // 이벤트 종류 없음 반환
        }
    }
}
