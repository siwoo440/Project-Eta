namespace ProjectEta.Steam
{
    public interface ISteamOverlayBackend
    {
        bool IsOverlayEnabled { get; } // Overlay 사용 가능 여부
        bool OpenOverlay(string dialog); // 지정 Overlay 열기
    }
}
