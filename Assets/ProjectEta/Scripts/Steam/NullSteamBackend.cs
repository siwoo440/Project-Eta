namespace ProjectEta.Steam
{
    public sealed class NullSteamBackend : ISteamRuntimeBackend
    {
        public string BackendName => "NullSteam"; // Steam 미사용 Backend 이름

        public bool Initialize()
        {
            return false; // Steam 미설치·미지원 환경은 비활성 상태 유지
        }

        public void RunCallbacks()
        {
        }

        public void Shutdown()
        {
        }
    }
}
