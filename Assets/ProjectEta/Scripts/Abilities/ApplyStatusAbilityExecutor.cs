using UnityEngine; // Mathf 사용
using ProjectEta.Pieces; // 상태 효과 런타임 사용

namespace ProjectEta.Abilities
{
    public sealed class ApplyStatusAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.ApplyStatus;

        public bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null)
            {
                failureReason = "Ability 데이터 또는 실행 Context가 없습니다.";
                return false;
            }

            if (effect.StatusEffect == null)
            {
                failureReason = "적용할 StatusEffectDefinition이 없습니다.";
                return false;
            }

            PieceRuntimeState target = context.TargetPiece;
            if (target == null || target.Definition == null)
            {
                failureReason = "상태 이상 대상이 없습니다.";
                return false;
            }

            if (target.IsDead)
            {
                failureReason = "사망한 기물에는 상태 이상을 적용할 수 없습니다.";
                return false;
            }

            if ((target.Definition.ImmuneStatusTags & effect.StatusEffect.StatusType) != 0)
            {
                failureReason = $"{target.Definition.DisplayName}은(는) {effect.StatusEffect.DisplayName}에 면역입니다.";
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

            RuntimeStatusEffect existing = context.TargetPiece.FindStatus(effect.StatusEffect.StatusType);
            int expectedStacks = 1;

            if (existing != null)
            {
                expectedStacks = effect.StatusEffect.StackMode == StatusStackMode.StacksAdd
                    ? Mathf.Min(effect.StatusEffect.MaxStacks, existing.StackCount + 1)
                    : existing.StackCount;
            }

            return AbilityExecutionResult.Succeeded(
                expectedStacks,
                context.TargetPiece,
                false,
                new[] { context.TargetPiece });
        }

        public AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            if (!context.TargetPiece.ApplyStatus(effect.StatusEffect))
            {
                return AbilityExecutionResult.Failed("상태 이상 적용에 실패했습니다.");
            }

            RuntimeStatusEffect applied = context.TargetPiece.FindStatus(effect.StatusEffect.StatusType);
            int stacks = applied != null ? applied.StackCount : preview.Amount;

            return AbilityExecutionResult.Succeeded(
                stacks,
                context.TargetPiece,
                false,
                new[] { context.TargetPiece });
        }
    }
}
