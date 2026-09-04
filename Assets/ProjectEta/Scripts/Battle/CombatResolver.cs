using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public static class CombatResolver // HP·ATK 기반 공격 판정을 계산하는 정적 클래스
    {
        public static CombatResult ResolveAttack(PieceRuntimeState attacker, PieceRuntimeState defender, BattleHooks hooks = null) // 공격자가 대상을 공격했을 때의 결과를 계산하는 메서드(29일차: 훅을 통한 피해 적용으로 전환)
        {
            int rawDamage = attacker.Definition.BaseAtk; // 공격자의 기본 공격력(확정 기본 규칙, 방어력 등 추가 계산 없음)
            int appliedDamage = DamageResolver.ApplyDamage(defender, rawDamage, attacker, hooks); // BeforeDamage/AfterDamage 훅과 함께 실제 HP 차감(훅이 없으면 기존과 동일하게 rawDamage 그대로 적용)

            return new CombatResult(attacker, defender, appliedDamage, defender.IsDead); // 이번 공격의 결과를 구성해 반환
        }
    }
}
