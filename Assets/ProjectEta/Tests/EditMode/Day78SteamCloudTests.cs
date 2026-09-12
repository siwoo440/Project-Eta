using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectEta.Steam;

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day78SteamCloudTests
    {
        private string tempDirectory;
        private string localSavePath;
        private FakeSteamCloudBackend backend;

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ProjectEtaDay78", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            localSavePath = Path.Combine(tempDirectory, "run_save.json");

            backend = new FakeSteamCloudBackend();
            SteamPlatformService.SetBackendForTests(backend);
            SteamPlatformService.Configure(480);
            Assert.That(SteamPlatformService.TryInitialize(), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            SteamPlatformService.ResetBackend();

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [Test]
        public void StartupSync_LocalSaveExists_UploadsLocalBytes()
        {
            byte[] localData = { 1, 2, 3, 4 };
            File.WriteAllBytes(localSavePath, localData);
            SteamCloudSaveSync sync = new SteamCloudSaveSync(localSavePath, "run_save.json");

            SteamCloudSyncResult result = sync.SyncAtStartup();

            Assert.That(result, Is.EqualTo(SteamCloudSyncResult.UploadedLocal));
            CollectionAssert.AreEqual(localData, backend.GetFile("run_save.json"));
        }

        [Test]
        public void StartupSync_LocalMissing_RemoteExists_RestoresRemoteBytes()
        {
            byte[] remoteData = { 7, 8, 9 };
            backend.SeedFile("run_save.json", remoteData);
            SteamCloudSaveSync sync = new SteamCloudSaveSync(localSavePath, "run_save.json");

            SteamCloudSyncResult result = sync.SyncAtStartup();

            Assert.That(result, Is.EqualTo(SteamCloudSyncResult.RestoredRemote));
            Assert.That(File.Exists(localSavePath), Is.True);
            CollectionAssert.AreEqual(remoteData, File.ReadAllBytes(localSavePath));
        }

        [Test]
        public void StartupSync_NoLocalOrRemote_ReturnsNoSave()
        {
            SteamCloudSaveSync sync = new SteamCloudSaveSync(localSavePath, "run_save.json");

            SteamCloudSyncResult result = sync.SyncAtStartup();

            Assert.That(result, Is.EqualTo(SteamCloudSyncResult.NoSave));
            Assert.That(File.Exists(localSavePath), Is.False);
        }

        [Test]
        public void StartupSync_CloudUnavailable_DoesNotWriteFiles()
        {
            backend.CloudEnabled = false;
            File.WriteAllBytes(localSavePath, new byte[] { 3, 2, 1 });
            SteamCloudSaveSync sync = new SteamCloudSaveSync(localSavePath, "run_save.json");

            SteamCloudSyncResult result = sync.SyncAtStartup();

            Assert.That(result, Is.EqualTo(SteamCloudSyncResult.Unavailable));
            Assert.That(backend.WriteCount, Is.EqualTo(0));
        }

        [Test]
        public void UploadLocalIfChanged_UnchangedAfterStartup_ReturnsUnchanged()
        {
            File.WriteAllBytes(localSavePath, new byte[] { 4, 5, 6 });
            SteamCloudSaveSync sync = new SteamCloudSaveSync(localSavePath, "run_save.json");
            Assert.That(sync.SyncAtStartup(), Is.EqualTo(SteamCloudSyncResult.UploadedLocal));
            int writeCount = backend.WriteCount;

            SteamCloudSyncResult result = sync.UploadLocalIfChanged();

            Assert.That(result, Is.EqualTo(SteamCloudSyncResult.Unchanged));
            Assert.That(backend.WriteCount, Is.EqualTo(writeCount));
        }

        [Test]
        public void UploadLocalIfChanged_LocalChanged_UploadsNewBytes()
        {
            File.WriteAllBytes(localSavePath, new byte[] { 1 });
            SteamCloudSaveSync sync = new SteamCloudSaveSync(localSavePath, "run_save.json");
            Assert.That(sync.SyncAtStartup(), Is.EqualTo(SteamCloudSyncResult.UploadedLocal));
            byte[] changedData = { 9, 8, 7, 6 };
            File.WriteAllBytes(localSavePath, changedData);

            SteamCloudSyncResult result = sync.UploadLocalIfChanged();

            Assert.That(result, Is.EqualTo(SteamCloudSyncResult.UploadedLocal));
            CollectionAssert.AreEqual(changedData, backend.GetFile("run_save.json"));
        }

        [Test]
        public void PlatformService_CloudFacade_UsesCloudBackend()
        {
            byte[] data = { 5, 4, 3 };

            Assert.That(SteamPlatformService.IsCloudEnabled, Is.True);
            Assert.That(SteamPlatformService.TryWriteCloudFile("run_save.json", data), Is.True);
            Assert.That(SteamPlatformService.CloudFileExists("run_save.json"), Is.True);
            Assert.That(SteamPlatformService.TryReadCloudFile("run_save.json", out byte[] loaded), Is.True);
            CollectionAssert.AreEqual(data, loaded);
        }

        private sealed class FakeSteamCloudBackend : ISteamRuntimeBackend, ISteamCloudBackend
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            private bool initialized;

            public string Name => "FakeSteamCloud";
            public bool IsAvailable => true;
            public bool IsInitialized => initialized;
            public bool CloudEnabled { get; set; } = true;
            public bool IsCloudEnabled => initialized && CloudEnabled;
            public int WriteCount { get; private set; }

            public bool Initialize(uint appId)
            {
                initialized = true;
                return true;
            }

            public void PumpCallbacks()
            {
            }

            public void Shutdown()
            {
                initialized = false;
            }

            public bool CloudFileExists(string fileName)
            {
                return IsCloudEnabled && files.ContainsKey(fileName);
            }

            public bool TryReadCloudFile(string fileName, out byte[] data)
            {
                if (!IsCloudEnabled || !files.TryGetValue(fileName, out byte[] stored))
                {
                    data = null;
                    return false;
                }

                data = CopyBytes(stored);
                return true;
            }

            public bool TryWriteCloudFile(string fileName, byte[] data)
            {
                if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName) || data == null)
                {
                    return false;
                }

                files[fileName] = CopyBytes(data);
                WriteCount++;
                return true;
            }

            public void SeedFile(string fileName, byte[] data)
            {
                files[fileName] = CopyBytes(data);
            }

            public byte[] GetFile(string fileName)
            {
                return files.TryGetValue(fileName, out byte[] data) ? CopyBytes(data) : null;
            }

            private static byte[] CopyBytes(byte[] source)
            {
                byte[] copy = new byte[source.Length];
                System.Array.Copy(source, copy, source.Length);
                return copy;
            }
        }
    }
}
