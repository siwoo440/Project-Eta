namespace ProjectEta.Steam
{
    public static class SteamPlatform
    {
        private static SteamPlatformService _service; // 애플리케이션 전역 Steam 서비스

        public static SteamPlatformService Service => _service; // Cloud·Achievement 확장을 위한 서비스 접근점
        public static bool IsInitialized => _service != null && _service.IsInitialized; // Steam 초기화 상태
        public static bool IsOverlayAvailable => _service != null && _service.IsOverlayAvailable; // Overlay 사용 가능 상태
        public static bool IsOverlayActive => _service != null && _service.IsOverlayActive; // Overlay 활성 상태
        public static bool CanAcceptGameInput => _service == null || _service.CanAcceptGameInput; // Steam 미사용 환경 포함 게임 입력 허용 상태
        public static string BackendName => _service != null ? _service.BackendName : "None"; // 현재 Steam Backend 이름
        public static string LastError => _service != null ? _service.LastError : string.Empty; // 마지막 Steam 오류

        public static bool OpenOverlay(SteamOverlayPage page = SteamOverlayPage.Friends)
        {
            return _service != null && _service.OpenOverlay(page); // 전역 Overlay 요청 전달
        }

        internal static void Bind(SteamPlatformService service)
        {
            _service = service; // RuntimeController가 생성한 서비스 등록
        }

        internal static void Unbind(SteamPlatformService service)
        {
            if (_service == service) _service = null; // 현재 서비스와 동일할 때만 전역 참조 해제
        }
    }
}
