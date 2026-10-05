#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectEta.Abilities;

namespace ProjectEta.EditorTools
{
    public static class Day91StatusAuraSummonValidationMenu
    {
        [MenuItem("Project Eta/Day 91/Validate Status Aura Summon")]
        public static void Validate()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Day91] ApplyStatus·Aura·Summon 검증");
            builder.AppendLine($"구현 Executor: {AbilityEffectRegistry.RegisteredCount}/9");
            builder.AppendLine($"ApplyStatus: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.ApplyStatus)}");
            builder.AppendLine($"Aura: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.Aura)}");
            builder.AppendLine($"Summon: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.Summon)}");
            builder.AppendLine("Day91 구현 대상: ApplyStatus / Aura / Summon");
            builder.AppendLine($"현재 임시 소환물 추적 수: {TemporarySummonService.ActiveCount}");

            bool valid =
                AbilityEffectRegistry.RegisteredCount >= 6 &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.ApplyStatus) &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.Aura) &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.Summon);

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
