#if STEAMWORKS_NET
using System; // 예외 처리 사용
using Steamworks; // Steamworks.NET API 사용

namespace ProjectEta.Steam
{
    public sealed class SteamworksNetBackend : ISteamRuntimeBackend, ISteamOverlayBackend
    {
        private Callback<GameOverlayActivated_t> _overlayCallback; // Steam Overlay 활성 Callback
        private bool _initialized; // SteamAPI 초기화 상태

        public string BackendName => "Steamworks.NET"; // 진단용 Backend 이름
        public event Action<bool> OverlayActiveChanged; // Overlay 활성 상태 이벤트

        public bool Initialize()
        {
            if (_initialized) return true; // 중복 SteamAPI.Init 차단

            try
            {
                if (!SteamAPI.Init()) return false; // Steam Client·AppID 초기화 실패 반환
                _overlayCallback = Callback<GameOverlayActivated_t>.Create(HandleOverlayActivated); // Overlay Callback 등록
                _initialized = true; // Steam Runtime 활성 상태 저장
                return true; // 초기화 성공 반환
            }
            catch (DllNotFoundException)
            {
                return false; // Native Steam DLL 누락을 게임 실행 실패로 전파하지 않음
            }
            catch (TypeInitializationException)
            {
                return false; // Steamworks 초기 타입 로드 실패 안전 처리
            }
        }

        public void RunCallbacks()
        {
            if (!_initialized) return; // 미초기화 Callback 차단
            SteamAPI.RunCallbacks(); // Steam 이벤트 처리
        }

        public void Shutdown()
        {
            if (!_initialized) return; // 중복 Shutdown 차단

            _overlayCallback?.Dispose(); // Overlay Callback 해제
            _overlayCallback = null; // Callback 참조 제거
            SteamAPI.Shutdown(); // Steam Runtime 종료
            _initialized = false; // 초기화 상태 해제
        }

        public bool IsOverlayEnabled()
        {
            return _initialized && SteamUtils.IsOverlayEnabled(); // Steam Client Overlay 활성 가능 여부 반환
        }

        public bool OpenOverlay(SteamOverlayPage page)
        {
            if (!IsOverlayEnabled()) return false; // Overlay 비활성 상태 차단
            SteamFriends.ActivateGameOverlay(ToOverlayDialog(page)); // 지정 Steam Overlay 열기
            return true; // 호출 요청 성공 반환
        }

        private void HandleOverlayActivated(GameOverlayActivated_t callback)
        {
            OverlayActiveChanged?.Invoke(callback.m_bActive != 0); // Steam Overlay 활성 상태 전달
        }

        private static string ToOverlayDialog(SteamOverlayPage page)
        {
            switch (page)
            {
                case SteamOverlayPage.Community: return "Community"; // 커뮤니티 Overlay
                case SteamOverlayPage.Players: return "Players"; // 최근 플레이어 Overlay
                case SteamOverlayPage.Settings: return "Settings"; // Steam 설정 Overlay
                case SteamOverlayPage.Achievements: return "Achievements"; // 업적 Overlay
                default: return "Friends"; // 기본 친구 Overlay
            }
        }
    }
}
#endif
