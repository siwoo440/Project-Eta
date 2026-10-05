namespace ProjectEta.Abilities
{
    public interface IAbilityEffectExecutor
    {
        AbilityEffectType EffectType { get; }

        bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason);

        AbilityExecutionResult Preview(AbilityEffectData effect, AbilityExecutionContext context);

        AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context);
    }
}
