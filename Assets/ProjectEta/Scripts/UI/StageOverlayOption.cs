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

        public StageOverlayOption(string title, string description, bool interactable, System.Action callback)
        {
            Title = title ?? string.Empty; // 제목 저장
            Description = description ?? string.Empty; // 설명 저장
            Interactable = interactable; // 활성 상태 저장
            Callback = callback; // 실행 콜백 저장
        }
    }
}
