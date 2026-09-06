namespace ProjectEta.Meta
{
    public static class MetaUnlockService
    {
        public static bool CanUnlock(MetaProgressState progress, MetaUnlockDefinition definition)
        {
            if (progress == null || definition == null) return false; // 잘못된 상태·정의 차단
            if (string.IsNullOrWhiteSpace(definition.UnlockId)) return false; // 빈 해금 ID 차단
            if (progress.IsUnlocked(definition.UnlockType, definition.UnlockId)) return false; // 중복 해금 차단
            return progress.MetaTokens >= definition.Cost; // 현재 토큰 비용 충족 여부 반환
        }

        public static bool TryUnlock(MetaProgressState progress, MetaUnlockDefinition definition)
        {
            if (!CanUnlock(progress, definition)) return false; // 해금 조건 미충족 차단
            if (!progress.TrySpendTokens(definition.Cost)) return false; // 메타 토큰 비용 차감
            if (progress.Unlock(definition.UnlockType, definition.UnlockId)) return true; // 영구 해금 등록 성공 반환
            progress.AddTokens(definition.Cost); // 예외적 등록 실패 시 비용 환불
            return false; // 해금 실패 반환
        }
    }
}
