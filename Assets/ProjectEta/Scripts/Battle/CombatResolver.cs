using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public static class CombatResolver // HP·ATK 기반 공격 판정을 계산하는 정적 클래스
    {
        public static CombatResult ResolveAttack(PieceRuntimeState attacker, PieceRuntimeState defender) // 공격자가 대상을 공격했을 때의 결과를 계산하는 메서드
        {
            int damage = attacker.Definition.BaseAtk; // 공격자의 기본 공격력만큼 피해를 입힘(확정 기본 규칙, 방어력 등 추가 계산 없음)
            defender.CurrentHp -= damage; // 대상의 체력을 감소시킴(0 미만으로는 내려가지 않도록 PieceRuntimeState.CurrentHp가 보정)

            return new CombatResult(attacker, defender, damage, defender.IsDead); // 이번 공격의 결과를 구성해 반환
        }
    }
}
