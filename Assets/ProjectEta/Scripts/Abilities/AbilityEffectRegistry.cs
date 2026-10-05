using System.Collections.Generic; // Dictionary 사용

namespace ProjectEta.Abilities
{
    public static class AbilityEffectRegistry
    {
        private static readonly Dictionary<AbilityEffectType, IAbilityEffectExecutor> Executors =
            new Dictionary<AbilityEffectType, IAbilityEffectExecutor>();

        static AbilityEffectRegistry()
        {
            ResetToDefaults();
        }

        public static int RegisteredCount => Executors.Count;

        public static bool Register(IAbilityEffectExecutor executor)
        {
            if (executor == null || Executors.ContainsKey(executor.EffectType)) return false;
            Executors.Add(executor.EffectType, executor);
            return true;
        }

        public static bool TryGet(AbilityEffectType effectType, out IAbilityEffectExecutor executor)
        {
            return Executors.TryGetValue(effectType, out executor);
        }

        public static bool IsImplemented(AbilityEffectType effectType)
        {
            return Executors.ContainsKey(effectType);
        }

        public static void ResetToDefaults()
        {
            Executors.Clear();
            Register(new HealAbilityExecutor());
            Register(new ModifyDamageAbilityExecutor());
            Register(new RedirectDamageAbilityExecutor());
        }
    }
}
