using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Pieces; // PieceGrade 사용
using ProjectEta.Run; // Day71 상점 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day71ShopContentTests
    {
        [Test]
        public void ShopSeed_같은노드는항상동일하다()
        {
            int first = ShopOfferGenerator.CreateSeed(12031, 2, 4, "phase_2_depth_4_shop", 8); // 첫 상점 시드 생성
            int second = ShopOfferGenerator.CreateSeed(12031, 2, 4, "phase_2_depth_4_shop", 8); // 동일 조건 시드 재생성

            Assert.AreEqual(first, second); // 동일 노드 재현성 검증
        }

        [Test]
        public void ShopSeed_페이즈가다르면달라진다()
        {
            int phaseOne = ShopOfferGenerator.CreateSeed(12031, 1, 4, "depth_4_shop", 8); // 1페이즈 상점 시드 생성
            int phaseTwo = ShopOfferGenerator.CreateSeed(12031, 2, 4, "phase_2_depth_4_shop", 8); // 2페이즈 상점 시드 생성

            Assert.AreNotEqual(phaseOne, phaseTwo); // 페이즈별 상품 시드 분리 검증
        }

        [Test]
        public void PurchasePrice_등급이높을수록비싸다()
        {
            int oneStar = ShopPriceRules.GetCardPurchasePrice(PieceGrade.OneStar, 1, 4); // 1성 가격 조회
            int twoStar = ShopPriceRules.GetCardPurchasePrice(PieceGrade.TwoStar, 1, 4); // 2성 가격 조회
            int threeStar = ShopPriceRules.GetCardPurchasePrice(PieceGrade.ThreeStar, 1, 4); // 3성 가격 조회

            Assert.Less(oneStar, twoStar); // 1성보다 2성 고가 검증
            Assert.Less(twoStar, threeStar); // 2성보다 3성 고가 검증
        }

        [Test]
        public void PurchasePrice_후반페이즈가초반보다비싸다()
        {
            int early = ShopPriceRules.GetCardPurchasePrice(PieceGrade.TwoStar, 1, 4); // 초반 구매 가격 조회
            int late = ShopPriceRules.GetCardPurchasePrice(PieceGrade.TwoStar, 5, 4); // 후반 구매 가격 조회

            Assert.Greater(late, early); // 페이즈 진행 가격 증가 검증
        }

        [Test]
        public void ServicePrices_후반페이즈에서증가한다()
        {
            Assert.Greater(ShopPriceRules.GetCardRemovePrice(5), ShopPriceRules.GetCardRemovePrice(1)); // 제거 가격 진행도 증가 검증
            Assert.Greater(ShopPriceRules.GetHealPrice(5), ShopPriceRules.GetHealPrice(1)); // 회복 가격 진행도 증가 검증
            Assert.Greater(ShopPriceRules.GetUpgradePrice(5, 8), ShopPriceRules.GetUpgradePrice(1, 2)); // 강화 가격 진행도 증가 검증
        }

        [Test]
        public void ShopOffer_구매완료상태를기록한다()
        {
            var offer = new ShopOffer(null, 25); // 테스트 상품 생성

            offer.MarkPurchased(); // 구매 완료 처리 실행

            Assert.IsTrue(offer.IsPurchased); // 구매 완료 상태 검증
        }
    }
}
