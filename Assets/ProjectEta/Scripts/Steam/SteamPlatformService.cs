namespace ProjectEta.Steam
{
    public static class SteamPlatformService
    {
        private static ISteamRuntimeBackend currentBackend = SteamBackendFactory.Create(); // 현재 Steam Backend
        private static uint appId; // 현재 Steam AppID
        private static bool configured; // Steam 설정 완료 상태

        public static string BackendName => currentBackend?.Name ?? "None"; // 현재 Backend 이름
        public static bool IsAvailable => currentBackend != null && currentBackend.IsAvailable; // Steam 사용 가능 상태
        public static bool IsInitialized => currentBackend != null && currentBackend.IsInitialized; // Steam 초기화 상태
        public static bool IsOverlayEnabled => currentBackend is ISteamOverlayBackend overlayBackend
            && overlayBackend.IsOverlayEnabled; // Overlay 사용 가능 상태
        public static bool IsCloudEnabled => IsInitialized
            && currentBackend is ISteamCloudBackend cloudBackend
            && cloudBackend.IsCloudEnabled; // Cloud 사용 가능 상태
        public static bool IsAchievementEnabled => IsInitialized
            && currentBackend is ISteamAchievementBackend achievementBackend
            && achievementBackend.IsAchievementEnabled; // Achievement 사용 가능 상태

        public static void Configure(uint targetAppId) // Steam AppID 설정
        {
            appId = targetAppId;
            configured = true;
        }

        public static bool TryInitialize() // Steam Runtime 초기화 시도
        {
            if (!configured || currentBackend == null)
            {
                return false;
            }

            return currentBackend.Initialize(appId);
        }

        public static void PumpCallbacks() // Steam Callback 처리
        {
            if (!IsInitialized)
            {
                return;
            }

            currentBackend.PumpCallbacks();
        }

        public static bool OpenOverlay(string dialog) // Steam Overlay 열기
        {
            if (!IsInitialized || currentBackend is not ISteamOverlayBackend overlayBackend)
            {
                return false;
            }

            return overlayBackend.OpenOverlay(dialog);
        }

        public static bool CloudFileExists(string fileName) // Steam Cloud 파일 존재 확인
        {
            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName)
                || currentBackend is not ISteamCloudBackend cloudBackend)
            {
                return false;
            }

            return cloudBackend.CloudFileExists(fileName);
        }

        public static bool TryReadCloudFile(string fileName, out byte[] data) // Steam Cloud 파일 읽기
        {
            data = null;

            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName)
                || currentBackend is not ISteamCloudBackend cloudBackend)
            {
                return false;
            }

            return cloudBackend.TryReadCloudFile(fileName, out data);
        }

        public static bool TryWriteCloudFile(string fileName, byte[] data) // Steam Cloud 파일 쓰기
        {
            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName) || data == null
                || currentBackend is not ISteamCloudBackend cloudBackend)
            {
                return false;
            }

            return cloudBackend.TryWriteCloudFile(fileName, data);
        }

        public static bool TryUnlockAchievement(string apiName) // Steam Achievement 해금 시도
        {
            if (!IsAchievementEnabled || string.IsNullOrWhiteSpace(apiName)
                || currentBackend is not ISteamAchievementBackend achievementBackend)
            {
                return false;
            }

            return achievementBackend.TryUnlockAchievement(apiName);
        }

        public static bool TryGetAchievementUnlocked(string apiName, out bool unlocked) // Steam Achievement 상태 조회
        {
            unlocked = false;

            if (!IsAchievementEnabled || string.IsNullOrWhiteSpace(apiName)
                || currentBackend is not ISteamAchievementBackend achievementBackend)
            {
                return false;
            }

            return achievementBackend.TryGetAchievementUnlocked(apiName, out unlocked);
        }

        public static void Shutdown() // Steam Runtime 종료
        {
            if (currentBackend == null)
            {
                return;
            }

            currentBackend.Shutdown();
        }

        public static void SetBackendForTests(ISteamRuntimeBackend backend) // 테스트용 Backend 지정
        {
            currentBackend = backend;
            configured = false;
            appId = 0;
        }

        public static void ResetBackend() // Steam Backend 초기 상태 복원
        {
            currentBackend?.Shutdown();
            currentBackend = SteamBackendFactory.Create();
            configured = false;
            appId = 0;
        }
    }
}
