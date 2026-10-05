namespace ProjectEta.Abilities
{
    public sealed class AuraAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.Aura;

        public bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null || context.Owner == null)
            {
                failureReason = "Aura 소유자 또는 실행 데이터가 없습니다.";
                return false;
            }

            if (context.Owner.IsDead)
            {
                failureReason = "사망한 기물의 Aura는 적용되지 않습니다.";
                return false;
            }

            if (effect.Amount == 0)
            {
                failureReason = "Aura 보정량이 0입니다.";
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

            return AbilityExecutionResult.Succeeded(effect.Amount, context.TargetPiece);
        }

        public AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context)
        {
            // Aura는 대상의 ScriptableObject·런타임 스탯을 직접 수정하지 않고 AuraResolver가 위치 기준으로 실시간 계산한다.
            return Preview(effect, context);
        }
    }
}
