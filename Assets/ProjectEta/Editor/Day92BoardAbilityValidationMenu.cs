#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectEta.Abilities;

namespace ProjectEta.EditorTools
{
    public static class Day92BoardAbilityValidationMenu
    {
        [MenuItem("Project Eta/Day 92/Validate Board Ability Framework")]
        public static void Validate()
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Day92] 보드 조작 Ability 검증");
            builder.AppendLine($"EffectType: {Enum.GetValues(typeof(AbilityEffectType)).Length}/9");
            builder.AppendLine($"구현 Executor: {AbilityEffectRegistry.RegisteredCount}/9");
            builder.AppendLine($"ModifyMoveRange: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyMoveRange)}");
            builder.AppendLine($"BlockTile: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.BlockTile)}");
            builder.AppendLine($"DestroyObstacle: {AbilityEffectRegistry.IsImplemented(AbilityEffectType.DestroyObstacle)}");
            builder.AppendLine($"현재 TileBlock: {TileBlockService.ActiveCount}");
            builder.AppendLine($"현재 MovementModifier: {MovementRangeModifierService.ActiveCount}");

            bool valid =
                Enum.GetValues(typeof(AbilityEffectType)).Length == 9 &&
                AbilityEffectRegistry.RegisteredCount == 9 &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyMoveRange) &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.BlockTile) &&
                AbilityEffectRegistry.IsImplemented(AbilityEffectType.DestroyObstacle);

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
