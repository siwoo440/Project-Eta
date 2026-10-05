using System; // Math.Max 사용
using ProjectEta.Battle; // DamageContext 사용

namespace ProjectEta.Abilities
{
    public sealed class ModifyDamageAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.ModifyDamage;

        public bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null)
            {
                failureReason = "Ability 데이터 또는 실행 Context가 없습니다.";
                return false;
            }

            if (context.DamageContext == null)
            {
                failureReason = "ModifyDamage에는 DamageContext가 필요합니다.";
                return false;
            }

            if (effect.Amount == 0)
            {
                failureReason = "피해 보정량이 0입니다.";
                return false;
            }

            return true;
        }

        public AbilityExecutionResult Preview(AbilityEffectData effect, AbilityExecutionContext context)
        {
            if (!CanExecute(effect, context, out string reason))
            {
                return AbilityExecutionResult.Failed(reason);
            }

            DamageContext damage = context.DamageContext;
            int adjusted = damage.Amount + effect.Amount;

            if (damage.Amount > 0)
            {
                adjusted = Math.Max(1, adjusted); // 일반 ModifyDamage는 피해를 1 아래로 낮추지 않음
            }
            else
            {
                adjusted = Math.Max(0, adjusted);
            }

            return AbilityExecutionResult.Succeeded(
                adjusted,
                damage.Target,
                false,
                damage.Target != null ? new[] { damage.Target } : null);
        }

        public AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            context.DamageContext.Amount = preview.Amount;
            return preview;
        }
    }
}
