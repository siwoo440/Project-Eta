namespace ProjectEta.Steam
{
    public static class SteamPlatform
    {
        public static string BackendName => SteamPlatformService.BackendName; // 현재 Steam Backend 이름
        public static bool IsAvailable => SteamPlatformService.IsAvailable; // Steam 사용 가능 상태
        public static bool IsInitialized => SteamPlatformService.IsInitialized; // Steam 초기화 상태
        public static bool IsOverlayEnabled => SteamPlatformService.IsOverlayEnabled; // Overlay 사용 가능 상태
        public static bool IsCloudEnabled => SteamPlatformService.IsCloudEnabled; // Cloud 사용 가능 상태
        public static bool IsAchievementEnabled => SteamPlatformService.IsAchievementEnabled; // Achievement 사용 가능 상태

        public static bool OpenOverlay(string dialog = "Friends") // Steam Overlay 열기
        {
            return SteamPlatformService.OpenOverlay(dialog);
        }

        public static bool UnlockAchievement(string apiName) // Steam Achievement 해금
        {
            return SteamPlatformService.TryUnlockAchievement(apiName);
        }

        public static bool TryGetAchievementUnlocked(string apiName, out bool unlocked) // Steam Achievement 상태 조회
        {
            return SteamPlatformService.TryGetAchievementUnlocked(apiName, out unlocked);
        }
    }
}
