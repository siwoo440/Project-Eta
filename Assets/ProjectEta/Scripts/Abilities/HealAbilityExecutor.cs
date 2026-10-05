using System; // Math.Min 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public sealed class HealAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.Heal;

        public bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null)
            {
                failureReason = "Ability 데이터 또는 실행 Context가 없습니다.";
                return false;
            }

            PieceRuntimeState target = context.TargetPiece;
            if (target == null || target.Definition == null)
            {
                failureReason = "회복 대상이 없습니다.";
                return false;
            }

            if (target.IsDead)
            {
                failureReason = "사망한 기물은 회복할 수 없습니다.";
                return false;
            }

            if (context.Owner != null && context.Owner.IsPlayerPiece != target.IsPlayerPiece)
            {
                failureReason = "적 기물은 회복할 수 없습니다.";
                return false;
            }

            if (effect.Amount <= 0)
            {
                failureReason = "회복량은 1 이상이어야 합니다.";
                return false;
            }

            if (target.CurrentHp >= target.Definition.BaseHp)
            {
                failureReason = "이미 최대 HP입니다.";
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

            PieceRuntimeState target = context.TargetPiece;
            int actualHeal = Math.Min(effect.Amount, target.Definition.BaseHp - target.CurrentHp);

            return AbilityExecutionResult.Succeeded(
                actualHeal,
                target,
                false,
                new[] { target });
        }

        public AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            PieceRuntimeState target = context.TargetPiece;
            target.CurrentHp += preview.Amount;

            return AbilityExecutionResult.Succeeded(
                preview.Amount,
                target,
                false,
                new[] { target });
        }
    }
}
