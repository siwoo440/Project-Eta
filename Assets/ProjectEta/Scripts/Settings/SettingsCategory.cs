namespace ProjectEta.Settings
{
    public enum SettingsCategory
    {
        Display = 0, // 디스플레이 설정
        Sound = 1, // 사운드 설정
        Controls = 2, // 향후 조작 설정
        Gameplay = 3, // 향후 게임플레이 설정
        Accessibility = 4 // 향후 접근성 설정
    }

    public static class SettingsCategoryCatalog
    {
        public static bool IsImplemented(SettingsCategory category)
        {
            return category == SettingsCategory.Display || category == SettingsCategory.Sound; // 57일차 실제 구현 카테고리 판정
        }

        public static string GetDisplayName(SettingsCategory category)
        {
            switch (category)
            {
                case SettingsCategory.Display:
                    return "디스플레이"; // 디스플레이 카테고리 문구
                case SettingsCategory.Sound:
                    return "사운드"; // 사운드 카테고리 문구
                case SettingsCategory.Controls:
                    return "조작"; // 조작 카테고리 문구
                case SettingsCategory.Gameplay:
                    return "게임플레이"; // 게임플레이 카테고리 문구
                case SettingsCategory.Accessibility:
                    return "접근성"; // 접근성 카테고리 문구
                default:
                    return category.ToString(); // 예외 카테고리 원본 문구
            }
        }
    }
}
