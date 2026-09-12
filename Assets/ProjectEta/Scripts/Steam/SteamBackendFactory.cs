namespace ProjectEta.Steam
{
    public static class SteamBackendFactory
    {
        public static ISteamRuntimeBackend Create()
        {
#if STEAMWORKS_NET
            return new SteamworksNetBackend(); // Steamworks.NET 설치 환경 Backend 사용
#else
            return new NullSteamBackend(); // Steamworks.NET 미설치·비활성 환경 fallback
#endif
        }
    }
}
