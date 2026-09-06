using ProjectEta.Meta; // MetaProgressState·MetaUnlockType·가용성 서비스 사용
using ProjectEta.Run; // RunContentUnlockSnapshot·SnapshotService 사용

namespace ProjectEta.King
{
    public static class KingUnlockRules
    {
        public static bool CanSelect(KingArchetype archetype, MetaProgressState progress)
        {
            if (archetype == KingArchetype.Default) return true; // 기본 킹 상시 선택 허용
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshotService.GetOrCreateForActiveRun(progress); // 현재 런 시작 시점 영구 해금 Snapshot 조회
            return CanSelect(archetype, snapshot); // 런 고정 Snapshot 기준 킹 선택 판정
        }

        public static bool CanSelect(KingArchetype archetype, RunContentUnlockSnapshot snapshot)
        {
            if (archetype == KingArchetype.Default) return true; // 기본 킹 상시 선택 허용
            if (snapshot == null) return false; // 런 Snapshot 누락 시 특수 킹 차단
            if (archetype == KingArchetype.Attack) return MetaContentAvailabilityService.IsKingAvailable(KingUnlockIds.Attack, snapshot); // 공격형 영구 해금 Snapshot 확인
            if (archetype == KingArchetype.Defense) return MetaContentAvailabilityService.IsKingAvailable(KingUnlockIds.Defense, snapshot); // 방어형 영구 해금 Snapshot 확인
            if (archetype == KingArchetype.Strategy) return MetaContentAvailabilityService.IsKingAvailable(KingUnlockIds.Strategy, snapshot); // 전략형 영구 해금 Snapshot 확인
            return false; // 정의되지 않은 킹 차단
        }
    }
}
