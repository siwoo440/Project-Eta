namespace ProjectEta.UI
{
    public enum StageOverlayMode
    {
        Shop = 0, // 상점 돗자리
        Event = 1 // 이벤트 돗자리
    }

    public sealed class StageOverlayOption
    {
        public string Title { get; } // 버튼 제목
        public string Description { get; } // 버튼 설명
        public bool Interactable { get; } // 선택 가능 여부
        public System.Action Callback { get; } // 선택 콜백
        public int? Price { get; } // 상품 가격 선택 정보
        public bool IsPurchased { get; } // 상품 구매 완료 정보

        public StageOverlayOption(string title, string description, bool interactable, System.Action callback, int? price = null, bool isPurchased = false) // 선택지 표시 정보 구성
        { // 생성자 시작
            Title = title ?? string.Empty; // 제목 저장
            Description = description ?? string.Empty; // 설명 저장
            Interactable = interactable; // 활성 상태 저장
            Callback = callback; // 실행 콜백 저장
            Price = price; // 선택 가격 저장
            IsPurchased = isPurchased; // 구매 완료 상태 저장
        }
    }
}
