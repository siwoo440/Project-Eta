using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public sealed class ShopOffer
    {
        public PieceDefinition Card { get; } // 판매 카드
        public int Price { get; } // 구매 가격
        public bool IsPurchased { get; private set; } // 구매 완료 여부

        public ShopOffer(PieceDefinition card, int price)
        {
            Card = card; // 판매 카드 저장
            Price = price < 0 ? 0 : price; // 음수 가격 차단
            IsPurchased = false; // 신규 상품 미구매 상태
        }

        public void MarkPurchased()
        {
            IsPurchased = true; // 구매 완료 상태 적용
        }
    }
}
