using UnityEngine; // Mathf 사용
using ProjectEta.Battle; // DamageContext 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public sealed class RedirectDamageAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.RedirectDamage;

        public bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null || context.DamageContext == null)
            {
                failureReason = "RedirectDamage에는 DamageContext가 필요합니다.";
                return false;
            }

            PieceRuntimeState protector = context.Owner;
            PieceRuntimeState protectedTarget = context.DamageContext.Target;

            if (protector == null || protectedTarget == null)
            {
                failureReason = "보호자 또는 피해 대상이 없습니다.";
                return false;
            }

            if (object.ReferenceEquals(protector, protectedTarget))
            {
                failureReason = "자기 자신의 피해를 Redirect할 수 없습니다.";
                return false;
            }

            if (protector.IsDead)
            {
                failureReason = "사망한 기물은 피해를 대신 받을 수 없습니다.";
                return false;
            }

            if (protector.IsPlayerPiece != protectedTarget.IsPlayerPiece)
            {
                failureReason = "같은 진영의 피해만 대신 받을 수 있습니다.";
                return false;
            }

            if (context.DamageContext.RedirectCount > 0)
            {
                failureReason = "한 피해 이벤트에서는 Redirect를 한 번만 적용할 수 있습니다.";
                return false;
            }

            int radius = effect.Radius > 0 ? effect.Radius : 1;
            int distance =
                Mathf.Abs(protector.BoardPosition.x - protectedTarget.BoardPosition.x) +
                Mathf.Abs(protector.BoardPosition.y - protectedTarget.BoardPosition.y);

            if (distance > radius)
            {
                failureReason = "보호 대상이 Redirect 범위를 벗어났습니다.";
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

            return AbilityExecutionResult.Succeeded(
                context.DamageContext.Amount,
                context.Owner,
                false,
                new[] { context.Owner });
        }

        public AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            if (!context.DamageContext.TryRedirect(context.Owner))
            {
                return AbilityExecutionResult.Failed("피해 대상 Redirect에 실패했습니다.");
            }

            return AbilityExecutionResult.Succeeded(
                context.DamageContext.Amount,
                context.Owner,
                false,
                new[] { context.Owner });
        }
    }
}
