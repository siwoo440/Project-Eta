namespace ProjectEta.Steam
{
    public interface ISteamCloudBackend
    {
        bool IsCloudEnabled { get; }

        bool CloudFileExists(string fileName);
        bool TryReadCloudFile(string fileName, out byte[] data);
        bool TryWriteCloudFile(string fileName, byte[] data);
    }
}
