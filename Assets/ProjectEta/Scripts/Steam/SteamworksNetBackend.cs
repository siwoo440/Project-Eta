#if STEAMWORKS_NET
using Steamworks;
#endif

namespace ProjectEta.Steam
{
#if STEAMWORKS_NET
    public sealed class SteamworksNetBackend : ISteamRuntimeBackend, ISteamOverlayBackend, ISteamCloudBackend
    {
        private bool initialized;

        public string Name => "Steamworks.NET";
        public bool IsAvailable => true;
        public bool IsInitialized => initialized;
        public bool IsOverlayEnabled => initialized && SteamUtils.IsOverlayEnabled();
        public bool IsCloudEnabled => initialized
            && SteamRemoteStorage.IsCloudEnabledForAccount()
            && SteamRemoteStorage.IsCloudEnabledForApp();

        public bool Initialize(uint appId)
        {
            if (initialized)
            {
                return true;
            }

            if (SteamAPI.RestartAppIfNecessary(new AppId_t(appId)))
            {
                return false;
            }

            initialized = SteamAPI.Init();
            return initialized;
        }

        public void PumpCallbacks()
        {
            if (!initialized)
            {
                return;
            }

            SteamAPI.RunCallbacks();
        }

        public bool OpenOverlay(string dialog)
        {
            if (!initialized)
            {
                return false;
            }

            SteamFriends.ActivateGameOverlay(string.IsNullOrWhiteSpace(dialog) ? "Friends" : dialog);
            return true;
        }

        public bool CloudFileExists(string fileName)
        {
            return IsCloudEnabled
                && !string.IsNullOrWhiteSpace(fileName)
                && SteamRemoteStorage.FileExists(fileName);
        }

        public bool TryReadCloudFile(string fileName, out byte[] data)
        {
            data = null;

            if (!CloudFileExists(fileName))
            {
                return false;
            }

            int fileSize = SteamRemoteStorage.GetFileSize(fileName);
            if (fileSize < 0)
            {
                return false;
            }

            byte[] buffer = new byte[fileSize];
            if (fileSize == 0)
            {
                data = buffer;
                return true;
            }

            int readSize = SteamRemoteStorage.FileRead(fileName, buffer, fileSize);
            if (readSize != fileSize)
            {
                return false;
            }

            data = buffer;
            return true;
        }

        public bool TryWriteCloudFile(string fileName, byte[] data)
        {
            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName) || data == null)
            {
                return false;
            }

            return SteamRemoteStorage.FileWrite(fileName, data, data.Length);
        }

        public void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            SteamAPI.Shutdown();
            initialized = false;
        }
    }
#else
    public sealed class SteamworksNetBackend : ISteamRuntimeBackend, ISteamOverlayBackend, ISteamCloudBackend
    {
        public string Name => "Steamworks.NET (Unavailable)";
        public bool IsAvailable => false;
        public bool IsInitialized => false;
        public bool IsOverlayEnabled => false;
        public bool IsCloudEnabled => false;

        public bool Initialize(uint appId)
        {
            return false;
        }

        public void PumpCallbacks()
        {
        }

        public bool OpenOverlay(string dialog)
        {
            return false;
        }

        public bool CloudFileExists(string fileName)
        {
            return false;
        }

        public bool TryReadCloudFile(string fileName, out byte[] data)
        {
            data = null;
            return false;
        }

        public bool TryWriteCloudFile(string fileName, byte[] data)
        {
            return false;
        }

        public void Shutdown()
        {
        }
    }
#endif
}
