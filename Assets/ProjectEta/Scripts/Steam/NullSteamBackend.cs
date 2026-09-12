namespace ProjectEta.Steam
{
    public sealed class NullSteamBackend : ISteamRuntimeBackend, ISteamOverlayBackend, ISteamCloudBackend
    {
        public string Name => "NullSteamBackend";
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
}
