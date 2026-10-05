#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.EditorTools
{
    public static class Day90AbilityFrameworkValidationMenu
    {
        [MenuItem("Project Eta/Day 90/Validate Ability Framework")]
        public static void Validate()
        {
            PieceDatabase database =
                AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");

            int piecesWithAbilities = 0;

            if (database != null)
            {
                for (int i = 0; i < database.Definitions.Count; i++)
                {
                    PieceDefinition definition = database.Definitions[i];
                    if (definition != null && definition.Abilities.Length > 0) piecesWithAbilities++;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("[Day90] 공통 Ability 프레임워크 검증");
            builder.AppendLine($"EffectType: {Enum.GetValues(typeof(AbilityEffectType)).Length}/9");
            builder.AppendLine($"현재 구현 Executor: {AbilityEffectRegistry.RegisteredCount}/9");
            builder.AppendLine($"Heal: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.Heal)}");
            builder.AppendLine($"ModifyDamage: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyDamage)}");
            builder.AppendLine($"RedirectDamage: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.RedirectDamage)}");
            builder.AppendLine($"현재 Ability 연결 기물: {piecesWithAbilities}");

            bool valid =
                Enum.GetValues(typeof(AbilityEffectType)).Length == 9 &&
                AbilityEffectRegistry.RegisteredCount >= 3 &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.Heal) &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyDamage) &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.RedirectDamage);

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
