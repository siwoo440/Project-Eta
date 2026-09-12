using NUnit.Framework;

namespace ProjectEta.Steam.Tests
{
    public sealed class Day79SteamAchievementTests
    {
        private FakeAchievementBackend backend; // 테스트용 Achievement Backend

        [SetUp]
        public void SetUp() // 테스트용 Steam Backend 준비
        {
            backend = new FakeAchievementBackend();
            SteamPlatformService.SetBackendForTests(backend);
            SteamPlatformService.Configure(480);
        }

        [TearDown]
        public void TearDown() // 테스트용 Steam Backend 정리
        {
            SteamPlatformService.ResetBackend();
        }

        [Test]
        public void UnlockAchievement_WhenRuntimeIsNotInitialized_ReturnsFalse() // 미초기화 Achievement 차단 검증
        {
            bool result = SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstVictory);

            Assert.That(result, Is.False);
            Assert.That(backend.LastUnlockedApiName, Is.Null);
        }

        [Test]
        public void UnlockAchievement_WhenStatsAreReady_ForwardsApiName() // 준비된 Achievement 해금 전달 검증
        {
            Assert.That(SteamPlatformService.TryInitialize(), Is.True);
            backend.AchievementStatsReady = true;

            bool result = SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstVictory);

            Assert.That(result, Is.True);
            Assert.That(backend.LastUnlockedApiName, Is.EqualTo(SteamAchievementIds.FirstVictory));
        }

        [Test]
        public void UnlockAchievement_WhenApiNameIsBlank_ReturnsFalse() // 빈 Achievement ID 차단 검증
        {
            Assert.That(SteamPlatformService.TryInitialize(), Is.True);
            backend.AchievementStatsReady = true;

            bool result = SteamPlatform.UnlockAchievement(" ");

            Assert.That(result, Is.False);
            Assert.That(backend.LastUnlockedApiName, Is.Null);
        }

        [Test]
        public void TryGetAchievementUnlocked_WhenStatsAreReady_ReturnsBackendState() // Achievement 상태 조회 검증
        {
            Assert.That(SteamPlatformService.TryInitialize(), Is.True);
            backend.AchievementStatsReady = true;
            backend.AchievementUnlocked = true;

            bool result = SteamPlatform.TryGetAchievementUnlocked(
                SteamAchievementIds.FirstFusion,
                out bool unlocked);

            Assert.That(result, Is.True);
            Assert.That(unlocked, Is.True);
            Assert.That(backend.LastQueriedApiName, Is.EqualTo(SteamAchievementIds.FirstFusion));
        }

        [Test]
        public void IsAchievementEnabled_WhenStatsAreNotReady_ReturnsFalse() // Stats 미준비 상태 검증
        {
            Assert.That(SteamPlatformService.TryInitialize(), Is.True);
            backend.AchievementStatsReady = false;

            Assert.That(SteamPlatform.IsAchievementEnabled, Is.False);
        }

        private sealed class FakeAchievementBackend : ISteamRuntimeBackend, ISteamAchievementBackend
        {
            public string Name => "FakeAchievementBackend"; // 테스트 Backend 이름
            public bool IsAvailable => true; // 테스트 Backend 사용 가능 상태
            public bool IsInitialized { get; private set; } // 테스트 Runtime 초기화 상태
            public bool IsAchievementEnabled => IsInitialized && AchievementStatsReady; // 테스트 Achievement 사용 가능 상태
            public bool AchievementStatsReady { get; set; } // 테스트 Stats 준비 상태
            public bool AchievementUnlocked { get; set; } // 테스트 Achievement 해금 상태
            public string LastUnlockedApiName { get; private set; } // 마지막 해금 요청 ID
            public string LastQueriedApiName { get; private set; } // 마지막 조회 요청 ID

            public bool Initialize(uint appId) // 테스트 Runtime 초기화
            {
                IsInitialized = true;
                return true;
            }

            public void PumpCallbacks() // 테스트 Callback 처리
            {
            }

            public bool TryUnlockAchievement(string apiName) // 테스트 Achievement 해금
            {
                if (!IsAchievementEnabled || string.IsNullOrWhiteSpace(apiName))
                {
                    return false;
                }

                LastUnlockedApiName = apiName;
                AchievementUnlocked = true;
                return true;
            }

            public bool TryGetAchievementUnlocked(string apiName, out bool unlocked) // 테스트 Achievement 상태 조회
            {
                unlocked = false;

                if (!IsAchievementEnabled || string.IsNullOrWhiteSpace(apiName))
                {
                    return false;
                }

                LastQueriedApiName = apiName;
                unlocked = AchievementUnlocked;
                return true;
            }

            public void Shutdown() // 테스트 Runtime 종료
            {
                IsInitialized = false;
                AchievementStatsReady = false;
            }
        }
    }
}
