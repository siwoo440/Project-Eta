#if STEAMWORKS_NET
using Steamworks;
#endif

namespace ProjectEta.Steam
{
#if STEAMWORKS_NET
    public sealed class SteamworksNetBackend : ISteamRuntimeBackend, ISteamOverlayBackend, ISteamCloudBackend, ISteamAchievementBackend
    {
        private bool initialized; // Steam Runtime 초기화 상태

        public string Name => "Steamworks.NET"; // Steamworks.NET Backend 이름
        public bool IsAvailable => true; // Steamworks.NET 사용 가능 상태
        public bool IsInitialized => initialized; // Steam Runtime 초기화 상태
        public bool IsOverlayEnabled => initialized && SteamUtils.IsOverlayEnabled(); // Overlay 사용 가능 상태
        public bool IsCloudEnabled => initialized
            && SteamRemoteStorage.IsCloudEnabledForAccount()
            && SteamRemoteStorage.IsCloudEnabledForApp(); // Cloud 사용 가능 상태
        public bool IsAchievementEnabled => initialized; // Achievement 사용 가능 상태

        public bool Initialize(uint appId) // Steam Runtime 초기화
        {
            if (initialized)
            {
                return true;
            }

            if (SteamAPI.RestartAppIfNecessary(new AppId_t(appId)))
            {
                return false;
            }

            initialized = SteamAPI.Init(); // Steam API 초기화
            return initialized; // 초기화 결과 반환
        }

        public void PumpCallbacks() // Steam Callback 처리
        {
            if (!initialized)
            {
                return;
            }

            SteamAPI.RunCallbacks(); // Steam Callback 실행
        }

        public bool OpenOverlay(string dialog) // Steam Overlay 열기
        {
            if (!initialized)
            {
                return false;
            }

            SteamFriends.ActivateGameOverlay(string.IsNullOrWhiteSpace(dialog) ? "Friends" : dialog); // Steam Overlay 호출
            return true;
        }

        public bool CloudFileExists(string fileName) // Steam Cloud 파일 존재 확인
        {
            return IsCloudEnabled
                && !string.IsNullOrWhiteSpace(fileName)
                && SteamRemoteStorage.FileExists(fileName);
        }

        public bool TryReadCloudFile(string fileName, out byte[] data) // Steam Cloud 파일 읽기
        {
            data = null;

            if (!CloudFileExists(fileName))
            {
                return false;
            }

            int fileSize = SteamRemoteStorage.GetFileSize(fileName); // Cloud 파일 크기 조회
            if (fileSize < 0)
            {
                return false;
            }

            byte[] buffer = new byte[fileSize]; // Cloud 파일 버퍼 생성
            if (fileSize == 0)
            {
                data = buffer;
                return true;
            }

            int readSize = SteamRemoteStorage.FileRead(fileName, buffer, fileSize); // Cloud 파일 읽기
            if (readSize != fileSize)
            {
                return false;
            }

            data = buffer;
            return true;
        }

        public bool TryWriteCloudFile(string fileName, byte[] data) // Steam Cloud 파일 쓰기
        {
            if (!IsCloudEnabled || string.IsNullOrWhiteSpace(fileName) || data == null)
            {
                return false;
            }

            return SteamRemoteStorage.FileWrite(fileName, data, data.Length); // Cloud 파일 저장
        }

        public bool TryUnlockAchievement(string apiName) // Steam Achievement 해금
        {
            if (!IsAchievementEnabled || string.IsNullOrWhiteSpace(apiName))
            {
                return false;
            }

            if (!SteamUserStats.GetAchievement(apiName, out bool unlocked))
            {
                return false;
            }

            if (unlocked)
            {
                return true;
            }

            if (!SteamUserStats.SetAchievement(apiName))
            {
                return false;
            }

            return SteamUserStats.StoreStats(); // Achievement 상태 Steam 저장
        }

        public bool TryGetAchievementUnlocked(string apiName, out bool unlocked) // Steam Achievement 상태 조회
        {
            unlocked = false;

            if (!IsAchievementEnabled || string.IsNullOrWhiteSpace(apiName))
            {
                return false;
            }

            return SteamUserStats.GetAchievement(apiName, out unlocked); // Achievement 상태 조회
        }

        public void Shutdown() // Steam Runtime 종료
        {
            if (!initialized)
            {
                return;
            }

            SteamAPI.Shutdown(); // Steam API 종료
            initialized = false;
        }
    }
#else
    public sealed class SteamworksNetBackend : ISteamRuntimeBackend, ISteamOverlayBackend, ISteamCloudBackend, ISteamAchievementBackend
    {
        public string Name => "Steamworks.NET (Unavailable)"; // 미지원 Backend 이름
        public bool IsAvailable => false; // Steamworks.NET 사용 가능 상태
        public bool IsInitialized => false; // Steam Runtime 초기화 상태
        public bool IsOverlayEnabled => false; // Overlay 사용 가능 상태
        public bool IsCloudEnabled => false; // Cloud 사용 가능 상태
        public bool IsAchievementEnabled => false; // Achievement 사용 가능 상태

        public bool Initialize(uint appId) // 미지원 Runtime 초기화
        {
            return false;
        }

        public void PumpCallbacks() // 미지원 Callback 처리
        {
        }

        public bool OpenOverlay(string dialog) // 미지원 Overlay 열기
        {
            return false;
        }

        public bool CloudFileExists(string fileName) // 미지원 Cloud 파일 존재 확인
        {
            return false;
        }

        public bool TryReadCloudFile(string fileName, out byte[] data) // 미지원 Cloud 파일 읽기
        {
            data = null;
            return false;
        }

        public bool TryWriteCloudFile(string fileName, byte[] data) // 미지원 Cloud 파일 쓰기
        {
            return false;
        }

        public bool TryUnlockAchievement(string apiName) // 미지원 Achievement 해금
        {
            return false;
        }

        public bool TryGetAchievementUnlocked(string apiName, out bool unlocked) // 미지원 Achievement 상태 조회
        {
            unlocked = false;
            return false;
        }

        public void Shutdown() // 미지원 Runtime 종료
        {
        }
    }
#endif
}
