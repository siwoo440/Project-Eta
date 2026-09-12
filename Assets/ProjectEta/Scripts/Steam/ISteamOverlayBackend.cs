using System; // Action 사용

namespace ProjectEta.Steam
{
    public interface ISteamOverlayBackend
    {
        event Action<bool> OverlayActiveChanged; // Overlay 활성 상태 변경 이벤트
        bool IsOverlayEnabled(); // Overlay 사용 가능 여부
        bool OpenOverlay(SteamOverlayPage page); // 지정 Overlay 페이지 열기
    }
}
