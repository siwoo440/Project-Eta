namespace ProjectEta.King
{
    public enum KingArchetype
    {
        Default = 0, // 기본 킹
        Attack = 1, // 공격형 킹
        Defense = 2, // 방어형 킹
        Strategy = 3 // 전략형 킹
    }

    public static class KingUnlockIds
    {
        public const string Attack = "king_attack"; // 48일차 공격형 킹 영구 해금 ID
        public const string Defense = "king_defense"; // 48일차 방어형 킹 영구 해금 ID
        public const string Strategy = "king_strategy"; // 48일차 전략형 킹 영구 해금 ID
    }

    public static class KingArchetypeNames
    {
        public static string GetDisplayName(KingArchetype archetype)
        {
            if (archetype == KingArchetype.Attack) return "공격형 킹"; // 공격형 표시 이름 반환
            if (archetype == KingArchetype.Defense) return "방어형 킹"; // 방어형 표시 이름 반환
            if (archetype == KingArchetype.Strategy) return "전략형 킹"; // 전략형 표시 이름 반환
            return "기본 킹"; // 기본 표시 이름 반환
        }
    }
}
