namespace ProjectEta.Steam
{
    public sealed class NullSteamBackend : ISteamRuntimeBackend, ISteamOverlayBackend, ISteamCloudBackend, ISteamAchievementBackend
    {
        public string Name => "NullSteamBackend"; // Null Backend 이름
        public bool IsAvailable => false; // Steam 사용 가능 상태
        public bool IsInitialized => false; // Steam 초기화 상태
        public bool IsOverlayEnabled => false; // Overlay 사용 가능 상태
        public bool IsCloudEnabled => false; // Cloud 사용 가능 상태
        public bool IsAchievementEnabled => false; // Achievement 사용 가능 상태

        public bool Initialize(uint appId) // Null Runtime 초기화
        {
            return false;
        }

        public void PumpCallbacks() // Null Callback 처리
        {
        }

        public bool OpenOverlay(string dialog) // Null Overlay 열기
        {
            return false;
        }

        public bool CloudFileExists(string fileName) // Null Cloud 파일 존재 확인
        {
            return false;
        }

        public bool TryReadCloudFile(string fileName, out byte[] data) // Null Cloud 파일 읽기
        {
            data = null;
            return false;
        }

        public bool TryWriteCloudFile(string fileName, byte[] data) // Null Cloud 파일 쓰기
        {
            return false;
        }

        public bool TryUnlockAchievement(string apiName) // Null Achievement 해금
        {
            return false;
        }

        public bool TryGetAchievementUnlocked(string apiName, out bool unlocked) // Null Achievement 상태 조회
        {
            unlocked = false;
            return false;
        }

        public void Shutdown() // Null Runtime 종료
        {
        }
    }
}
