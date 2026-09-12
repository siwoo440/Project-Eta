namespace ProjectEta.Steam
{
    public interface ISteamAchievementBackend
    {
        bool IsAchievementEnabled { get; } // Achievement 사용 가능 상태

        bool TryUnlockAchievement(string apiName); // Achievement 해금 시도
        bool TryGetAchievementUnlocked(string apiName, out bool unlocked); // Achievement 해금 상태 조회
    }
}
