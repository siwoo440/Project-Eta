namespace ProjectEta.Steam
{
    public interface ISteamRuntimeBackend
    {
        string Name { get; } // 백엔드 이름
        bool IsAvailable { get; } // 백엔드 사용 가능 여부
        bool IsInitialized { get; } // 초기화 상태

        bool Initialize(uint appId); // AppID 기반 초기화
        void PumpCallbacks(); // Steam 콜백 처리
        void Shutdown(); // Steam 종료
    }
}
