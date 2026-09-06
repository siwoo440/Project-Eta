using ProjectEta.Battle; // CombatResult·DamageContext 사용
using ProjectEta.Pieces; // PieceRuntimeState·PieceMovementType 사용

namespace ProjectEta.King
{
    public static class AttackKingAbility
    {
        public static int ApplyBeforeDamage(KingRunState kingState, DamageContext context)
        {
            if (kingState == null || context == null) return 0; // 상태·피해 컨텍스트 누락 차단
            if (kingState.Archetype != KingArchetype.Attack) return 0; // 공격형 킹 외 보너스 차단
            if (!IsPlayerKing(context.Source)) return 0; // 플레이어 킹 직접 공격 외 보너스 차단
            if (kingState.RageStacks <= 0) return 0; // 격노 없는 공격 보너스 차단

            int bonusDamage = kingState.ConsumeRage(); // 현재 격노 전부 소비
            context.Amount += bonusDamage; // 격노 스택만큼 다음 공격 피해 증가
            return bonusDamage; // 적용 피해 보너스 반환
        }

        public static bool HandleAfterAttack(KingRunState kingState, CombatResult result)
        {
            if (kingState == null || result == null) return false; // 상태·공격 결과 누락 차단
            if (kingState.Archetype != KingArchetype.Attack) return false; // 공격형 킹 외 격노 획득 차단
            if (!result.DefenderDied) return false; // 직접 처치 실패 시 격노 획득 차단
            if (!IsPlayerKing(result.Attacker)) return false; // 플레이어 킹 직접 처치 외 격노 획득 차단
            return kingState.TryAddRage(); // 처치 성공 격노 1스택 획득
        }

        public static bool IsPlayerKing(PieceRuntimeState piece)
        {
            if (piece == null || piece.Definition == null) return false; // 기물·정의 누락 차단
            return piece.IsPlayerPiece && piece.Definition.MovementType == PieceMovementType.King; // 플레이어 킹 여부 반환
        }
    }
}
