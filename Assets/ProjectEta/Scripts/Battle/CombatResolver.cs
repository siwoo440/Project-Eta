using ProjectEta.Abilities; // Aura 기반 실제 공격력 계산
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Battle
{
    public static class CombatResolver
    {
        public static CombatResult ResolveAttack(
            PieceRuntimeState attacker,
            PieceRuntimeState defender,
            BattleHooks hooks = null)
        {
            int rawDamage = AuraResolver.GetAttack(attacker); // BaseAtk + 현재 위치 기준 유효 Aura 보정
            int appliedDamage = DamageResolver.ApplyDamage(defender, rawDamage, attacker, hooks);

            return new CombatResult(attacker, defender, appliedDamage, defender.IsDead);
        }
    }
}
