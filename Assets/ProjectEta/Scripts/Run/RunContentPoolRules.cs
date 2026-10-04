using System; // string 공백 검사
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Run
{
    public static class RunContentPoolRules
    {
        public static bool IsValidPiece(PieceDefinition definition)
        {
            return definition != null && !string.IsNullOrWhiteSpace(definition.PieceId); // 기본 데이터 유효성 판정
        }

        public static bool CanUseAsReward(PieceDefinition definition)
        {
            if (!IsValidPiece(definition)) return false; // 잘못된 Piece 제외
            if (definition.MovementType == PieceMovementType.King) return false; // King 일반 보상 제외
            if (definition.Grade != PieceGrade.OneStar) return false; // 86일차: 일반 획득은 1성만 허용
            if (definition.Category == PieceCategory.Fusion) return false; // 합성 결과 직접 보상 제외
            if (definition.Category == PieceCategory.Monster) return false; // 적 전용 제외
            if (definition.Category == PieceCategory.Boss) return false; // 보스 전용 제외
            return true; // 해금 가능한 1성 기본·특수 기물 허용
        }

        public static bool CanUseInShop(PieceDefinition definition)
        {
            return CanUseAsReward(definition); // Reward와 Shop의 카드 획득 정책 공유
        }

        public static bool CanUseAsEnemy(PieceDefinition definition)
        {
            if (!IsValidPiece(definition)) return false; // 잘못된 Piece 제외
            if (definition.MovementType == PieceMovementType.King) return false; // 플레이어 King 적 Pool 제외
            if (definition.Category == PieceCategory.Fusion) return false; // Fusion 결과 적 Pool 제외
            if (definition.Category == PieceCategory.Boss) return false; // Boss 전용 Piece 일반 Enemy 제외
            return true; // 해금 여부와 무관하게 Basic·Special·Monster 적 후보 허용
        }

        public static bool CanUseAsBoss(PieceDefinition definition)
        {
            return IsValidPiece(definition) && definition.Category == PieceCategory.Boss; // Boss Category만 허용
        }
    }
}
