namespace ProjectEta.Steam
{
    public static class SteamPlatformService
    {
        private static ISteamRuntimeBackend currentBackend = SteamBackendFactory.Create();
        private static uint appId;
        private static bool configured;

        public static string BackendName => currentBackend?.Name ?? "None";
        public static bool IsAvailable => currentBackend != null && currentBackend.IsAvailable;
        public static bool IsInitialized => currentBackend != null && currentBackend.IsInitialized;
        public static bool IsOverlayEnabled => currentBackend is ISteamOverlayBackend overlayBackend
            && overlayBackend.IsOverlayEnabled;
        public static bool IsCloudEnabled => IsInitialized
            && currentBackend is ISteamCloudBackend cloudBackend
            && cloudBackend.IsCloudEnabled;

        public static void Configure(uint targetAppId)
        {
            appId = targetAppId;
            configured = true;
        }

        public static bool TryInitialize()
        {
            if (!configured || currentBackend == null)
            {
                return false;
            }

            return currentBackend.Initialize(appId);
        }

        public static void PumpCallbacks()
        {
            if (!IsInitialized)
            {
                return;
            }

            currentBackend.PumpCallbacks();
        }

        public static bool OpenOverlay(string dialog)
        {
            if (!IsInitialized || currentBackend is not ISteamOverlayBackend overlayBackend)
            {
                return false;
            }

            return overlayBackend.OpenOverlay(dialog);
        }

        public static bool CloudFileExists(string fileName)
        {
            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName)
                || currentBackend is not ISteamCloudBackend cloudBackend)
            {
                return false;
            }

            return cloudBackend.CloudFileExists(fileName);
        }

        public static bool TryReadCloudFile(string fileName, out byte[] data)
        {
            data = null;

            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName)
                || currentBackend is not ISteamCloudBackend cloudBackend)
            {
                return false;
            }

            return cloudBackend.TryReadCloudFile(fileName, out data);
        }

        public static bool TryWriteCloudFile(string fileName, byte[] data)
        {
            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName) || data == null
                || currentBackend is not ISteamCloudBackend cloudBackend)
            {
                return false;
            }

            return cloudBackend.TryWriteCloudFile(fileName, data);
        }

        public static void Shutdown()
        {
            if (currentBackend == null)
            {
                return;
            }

            currentBackend.Shutdown();
        }

        public static void SetBackendForTests(ISteamRuntimeBackend backend)
        {
            currentBackend = backend;
            configured = false;
            appId = 0;
        }

        public static void ResetBackend()
        {
            currentBackend?.Shutdown();
            currentBackend = SteamBackendFactory.Create();
            configured = false;
            appId = 0;
        }
    }
}
