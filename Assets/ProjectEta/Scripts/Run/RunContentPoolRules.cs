using System; // string 공백 검사
using ProjectEta.Pieces; // PieceDefinition·Category·Grade·MovementType 사용

namespace ProjectEta.Run
{
    public static class RunContentPoolRules
    {
        public static bool IsValidPiece(PieceDefinition definition)
        {
            return definition != null && !string.IsNullOrWhiteSpace(definition.PieceId); // 기본 Piece 데이터 유효성 판정
        }

        public static bool CanUseAsReward(PieceDefinition definition)
        {
            if (!IsValidPiece(definition)) return false; // 잘못된 Piece 제외
            if (definition.MovementType == PieceMovementType.King) return false; // 플레이어 King 일반 보상 제외
            if (definition.Category == PieceCategory.Fusion) return false; // Fusion 결과 직접 보상 제외
            if (definition.Category == PieceCategory.Monster) return false; // 적 전용 Monster 보상 제외
            if (definition.Category == PieceCategory.Boss) return false; // Boss 전용 기물 보상 제외
            if (definition.Grade == PieceGrade.FourStar || definition.Grade == PieceGrade.FiveStar) return false; // 4·5성 일반 보상 제외
            return true; // 일반 획득 가능한 카드 허용
        }

        public static bool CanUseInShop(PieceDefinition definition)
        {
            return CanUseAsReward(definition); // 현재 Shop은 일반 카드 획득 Pool 정책 재사용
        }

        public static bool CanUseAsEnemy(PieceDefinition definition)
        {
            if (!IsValidPiece(definition)) return false; // 잘못된 Piece 제외
            if (definition.MovementType == PieceMovementType.King) return false; // 플레이어 King 적 Pool 제외
            if (definition.Category == PieceCategory.Fusion) return false; // Fusion 결과 적 Pool 제외
            if (definition.Category == PieceCategory.Boss) return false; // Boss 전용 Piece 일반 Enemy 제외
            return true; // Normal·Special·Monster 적 후보 허용
        }

        public static bool CanUseAsBoss(PieceDefinition definition)
        {
            return IsValidPiece(definition) && definition.Category == PieceCategory.Boss; // Boss Category와 유효 ID 동시 검증
        }
    }
}
