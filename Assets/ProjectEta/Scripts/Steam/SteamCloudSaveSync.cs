using System;
using System.IO;

namespace ProjectEta.Steam
{
    public enum SteamCloudSyncResult
    {
        Unavailable,
        NoSave,
        Unchanged,
        UploadedLocal,
        RestoredRemote,
        Failed
    }

    public sealed class SteamCloudSaveSync
    {
        private readonly string localSavePath;
        private readonly string remoteFileName;
        private byte[] lastLocalSnapshot;

        public SteamCloudSaveSync(string localSavePath, string remoteFileName)
        {
            if (string.IsNullOrWhiteSpace(localSavePath))
            {
                throw new ArgumentException("Local save path is required.", nameof(localSavePath));
            }

            if (string.IsNullOrWhiteSpace(remoteFileName))
            {
                throw new ArgumentException("Remote file name is required.", nameof(remoteFileName));
            }

            this.localSavePath = localSavePath;
            this.remoteFileName = remoteFileName;
        }

        public SteamCloudSyncResult SyncAtStartup()
        {
            if (!SteamPlatformService.IsCloudEnabled)
            {
                return SteamCloudSyncResult.Unavailable;
            }

            try
            {
                if (File.Exists(localSavePath))
                {
                    byte[] localData = File.ReadAllBytes(localSavePath);
                    return UploadLocal(localData);
                }

                if (!SteamPlatformService.CloudFileExists(remoteFileName))
                {
                    return SteamCloudSyncResult.NoSave;
                }

                if (!SteamPlatformService.TryReadCloudFile(remoteFileName, out byte[] remoteData) || remoteData == null)
                {
                    return SteamCloudSyncResult.Failed;
                }

                if (!TryRestoreLocal(remoteData))
                {
                    return SteamCloudSyncResult.Failed;
                }

                lastLocalSnapshot = CopyBytes(remoteData);
                return SteamCloudSyncResult.RestoredRemote;
            }
            catch (IOException)
            {
                return SteamCloudSyncResult.Failed;
            }
            catch (UnauthorizedAccessException)
            {
                return SteamCloudSyncResult.Failed;
            }
        }

        public SteamCloudSyncResult UploadLocalIfChanged()
        {
            if (!SteamPlatformService.IsCloudEnabled)
            {
                return SteamCloudSyncResult.Unavailable;
            }

            if (!File.Exists(localSavePath))
            {
                return SteamCloudSyncResult.NoSave;
            }

            try
            {
                byte[] localData = File.ReadAllBytes(localSavePath);
                if (BytesEqual(localData, lastLocalSnapshot))
                {
                    return SteamCloudSyncResult.Unchanged;
                }

                return UploadLocal(localData);
            }
            catch (IOException)
            {
                return SteamCloudSyncResult.Failed;
            }
            catch (UnauthorizedAccessException)
            {
                return SteamCloudSyncResult.Failed;
            }
        }

        private SteamCloudSyncResult UploadLocal(byte[] localData)
        {
            if (!SteamPlatformService.TryWriteCloudFile(remoteFileName, localData))
            {
                return SteamCloudSyncResult.Failed;
            }

            lastLocalSnapshot = CopyBytes(localData);
            return SteamCloudSyncResult.UploadedLocal;
        }

        private bool TryRestoreLocal(byte[] data)
        {
            string directory = Path.GetDirectoryName(localSavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = localSavePath + ".cloud.tmp";

            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                File.WriteAllBytes(temporaryPath, data);
                File.Move(temporaryPath, localSavePath);
                return true;
            }
            catch (IOException)
            {
                DeleteTemporaryFile(temporaryPath);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                DeleteTemporaryFile(temporaryPath);
                return false;
            }
        }

        private static void DeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] CopyBytes(byte[] source)
        {
            byte[] copy = new byte[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
