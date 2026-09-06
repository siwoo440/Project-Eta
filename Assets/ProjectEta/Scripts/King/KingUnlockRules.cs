using ProjectEta.Meta; // MetaProgressState·MetaUnlockType 사용

namespace ProjectEta.King
{
    public static class KingUnlockRules
    {
        public static bool CanSelect(KingArchetype archetype, MetaProgressState progress)
        {
            if (archetype == KingArchetype.Default) return true; // 기본 킹 상시 선택 허용
            if (progress == null) return false; // 영구 진행 상태 누락 시 특수 킹 차단
            if (archetype == KingArchetype.Attack) return progress.IsUnlocked(MetaUnlockType.King, KingUnlockIds.Attack); // 공격형 영구 해금 확인
            if (archetype == KingArchetype.Defense) return progress.IsUnlocked(MetaUnlockType.King, KingUnlockIds.Defense); // 방어형 영구 해금 확인
            if (archetype == KingArchetype.Strategy) return progress.IsUnlocked(MetaUnlockType.King, KingUnlockIds.Strategy); // 전략형 영구 해금 확인
            return false; // 정의되지 않은 킹 차단
        }
    }
}
