namespace ProjectEta.Steam
{
    public static class SteamPlatform
    {
        public static string BackendName => SteamPlatformService.BackendName;
        public static bool IsAvailable => SteamPlatformService.IsAvailable;
        public static bool IsInitialized => SteamPlatformService.IsInitialized;
        public static bool IsOverlayEnabled => SteamPlatformService.IsOverlayEnabled;
        public static bool IsCloudEnabled => SteamPlatformService.IsCloudEnabled;

        public static bool OpenOverlay(string dialog = "Friends")
        {
            return SteamPlatformService.OpenOverlay(dialog);
        }
    }
}
