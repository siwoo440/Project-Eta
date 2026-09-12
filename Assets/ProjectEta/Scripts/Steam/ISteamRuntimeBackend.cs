namespace ProjectEta.Steam
{
    public interface ISteamRuntimeBackend
    {
        string BackendName { get; } // 진단용 Backend 이름
        bool Initialize(); // Steam Runtime 초기화
        void RunCallbacks(); // Steam Callback 처리
        void Shutdown(); // Steam Runtime 종료
    }
}
