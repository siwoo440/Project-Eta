using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // RunContentUnlockSnapshot 사용

namespace ProjectEta.Meta
{
    public static class MetaContentAvailabilityService
    {
        public static bool IsRequiredUnlockAvailable(MetaUnlockType type, string requiredUnlockId, RunContentUnlockSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(requiredUnlockId)) return true; // 해금 요구 ID 없는 기본 콘텐츠 상시 허용
            if (snapshot == null) return false; // 영구 해금 필요 콘텐츠는 Snapshot 누락 시 차단
            return snapshot.IsUnlocked(type, requiredUnlockId); // 런 시작 시점 영구 해금 여부 반환
        }

        public static bool IsPieceAvailable(PieceDefinition definition, RunContentUnlockSnapshot snapshot)
        {
            if (definition == null) return false; // 빈 기물 정의 차단
            return IsRequiredUnlockAvailable(MetaUnlockType.Piece, definition.RequiredMetaUnlockId, snapshot); // 기물 영구 해금 요구 판정
        }

        public static bool IsKingAvailable(string requiredUnlockId, RunContentUnlockSnapshot snapshot)
        {
            return IsRequiredUnlockAvailable(MetaUnlockType.King, requiredUnlockId, snapshot); // 킹 영구 해금 요구 판정
        }

        public static bool IsPassiveAvailable(string requiredUnlockId, RunContentUnlockSnapshot snapshot)
        {
            return IsRequiredUnlockAvailable(MetaUnlockType.Passive, requiredUnlockId, snapshot); // 향후 패시브 콘텐츠 영구 해금 요구 판정
        }
    }
}
