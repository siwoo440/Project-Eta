using System;
using ProjectEta.Battle;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public sealed class ModifyDamageAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.ModifyDamage;

        public bool CanExecute(
            AbilityEffectData effect,
            AbilityExecutionContext context,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null)
            {
                failureReason = "Ability 데이터 또는 실행 Context가 없습니다.";
                return false;
            }

            if (effect.Amount == 0)
            {
                failureReason = "피해 보정량이 0입니다.";
                return false;
            }

            if (context.DamageContext != null)
            {
                return true;
            }

            PieceRuntimeState target = context.TargetPiece ?? context.Owner;

            if (target == null || target.IsDead)
            {
                failureReason = "피해 보정을 적용할 기물이 없습니다.";
                return false;
            }

            return true;
        }

        public AbilityExecutionResult Preview(
            AbilityEffectData effect,
            AbilityExecutionContext context)
        {
            if (!CanExecute(effect, context, out string reason))
            {
                return AbilityExecutionResult.Failed(reason);
            }

            if (context.DamageContext == null)
            {
                PieceRuntimeState target = context.TargetPiece ?? context.Owner;

                return AbilityExecutionResult.Succeeded(
                    effect.Amount,
                    target,
                    false,
                    target != null ? new[] { target } : null);
            }

            DamageContext damage = context.DamageContext;
            int adjusted = damage.Amount + effect.Amount;

            if (damage.Amount > 0)
            {
                adjusted = Math.Max(1, adjusted);
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

        public AbilityExecutionResult Execute(
            AbilityEffectData effect,
            AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            if (context.DamageContext == null)
            {
                PieceRuntimeState target = context.TargetPiece ?? context.Owner;
                TemporaryDamageModifierService.Register(target, effect.Amount);

                return AbilityExecutionResult.Succeeded(
                    effect.Amount,
                    target,
                    false,
                    target != null ? new[] { target } : null);
            }

            context.DamageContext.Amount = preview.Amount;
            return preview;
        }
    }
}
