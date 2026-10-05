using UnityEngine; // Mathf 사용
using ProjectEta.Abilities; // 90일차 공통 피해 Ability 처리
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Battle
{
    public static class DamageResolver
    {
        public static int ApplyDamage(
            PieceRuntimeState target,
            int amount,
            PieceRuntimeState source = null,
            BattleHooks hooks = null)
        {
            if (target == null || amount <= 0) return 0;

            var context = new DamageContext(target, source, amount);

            FourStarCombatAbilityResolver.ProcessBeforeDamage(context); // 94일차: 4성 조건부 피해·분담·철벽 처리
            ThreeStarPassiveAbilityResolver.ProcessBeforeDamage(context); // 93일차: 3성 조건부 공격·인접 보호 패시브 처리
            PieceAbilityService.ProcessBeforeDamage(context); // Redirect → 공격자/피격자 ModifyDamage 순서로 공통 Ability 처리
            hooks?.RaiseBeforeDamage(context); // 기존 훅은 Ability 처리 뒤에도 최종 피해를 추가 보정할 수 있음

            PieceRuntimeState actualTarget = context.Target ?? target;
            int finalAmount = Mathf.Max(0, context.Amount);

            if (finalAmount > 0)
            {
                actualTarget.CurrentHp -= finalAmount;
            }

            hooks?.RaiseAfterDamage(actualTarget, source, finalAmount);
            return finalAmount;
        }
    }
}
