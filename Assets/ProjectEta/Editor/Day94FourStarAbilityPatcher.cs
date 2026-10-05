#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.EditorTools
{
    [InitializeOnLoad]
    public static class Day94FourStarAbilityPatcher
    {
        private const string DatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset";
        private const string AbilityRoot = "Assets/ProjectEta/Data/Abilities";
        private const string FourStarRoot = AbilityRoot + "/FourStar";

        private static readonly string[] FourStarPieceIds =
        {
            "marshal", "grand_cannon", "imperial_knight", "high_priest", "war_rider",
            "siege_chariot", "grand_guardian", "grand_unicorn", "grand_gryphon", "archmage",
            "war_cleric", "executioner", "storm_knight", "bastion", "field_commander",
            "illusionist", "deadeye", "gatekeeper"
        };

        static Day94FourStarAbilityPatcher()
        {
            EditorApplication.delayCall += PatchIfNeeded;
        }

        [MenuItem("Project Eta/Day 94/Patch Four Star Abilities")]
        public static void PatchIfNeeded()
        {
            EnsureFolders();

            StatusEffectDefinition poison = EnsureStatus(StatusEffectType.Poison);
            StatusEffectDefinition burn = EnsureStatus(StatusEffectType.Burn);
            StatusEffectDefinition root = EnsureStatus(StatusEffectType.Root);

            PieceAbilityDefinition marshal = EnsureAbility(
                "Marshal_Command",
                FourStarAbilityIds.MarshalCommand,
                "지휘",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "인접 아군 일반 기물의 ATK를 1 증가시킨다. 동일 지휘 효과는 중첩되지 않는다.",
                new EffectSpec(
                    AbilityEffectType.Aura,
                    amount: 1,
                    radius: 1,
                    auraGroupId: "marshal_command_attack"));

            PieceAbilityDefinition grandCannon = EnsureAbility(
                "GrandCannon_Barrage",
                FourStarAbilityIds.GrandCannonBarrage,
                "연쇄 포격",
                AbilityTrigger.AfterAttack,
                AbilityActionCost.None,
                "공격 후 대상의 십자 인접 적 최대 2개에 1의 보조 피해를 준다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, amount: 1, radius: 1));

            PieceAbilityDefinition highPriest = EnsureAbility(
                "HighPriest_Blessing",
                FourStarAbilityIds.HighPriestBlessing,
                "축복",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                "자신의 행동 대신 거리 2 이내 아군 1개의 HP를 2 회복한다.",
                new EffectSpec(AbilityEffectType.Heal, amount: 2, radius: 2));

            PieceAbilityDefinition siegeChariot = EnsureAbility(
                "SiegeChariot_Siege",
                FourStarAbilityIds.SiegeChariotSiege,
                "공성",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "직선 이동 또는 공격 경로의 파괴 가능한 장애물 1개를 제거하고 행동을 계속한다.",
                new EffectSpec(AbilityEffectType.DestroyObstacle, radius: 8));

            PieceAbilityDefinition grandGuardian = EnsureAbility(
                "GrandGuardian_DamageShare",
                FourStarAbilityIds.GrandGuardianShare,
                "피해 분담",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "인접 아군이 받는 피해 중 1을 자신이 대신 받는다. 자신의 HP가 1이면 발동하지 않는다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, amount: -1, radius: 1));

            PieceAbilityDefinition archmage = EnsureAbility(
                "Archmage_SpellChoice",
                FourStarAbilityIds.ArchmageSpellChoice,
                "주문 선택",
                AbilityTrigger.AfterAttack,
                AbilityActionCost.None,
                "공격 적중 시 독·화상·속박 중 하나를 부여한다. 기본 자동 선택은 속박→화상→독 순서다.",
                new EffectSpec(AbilityEffectType.ApplyStatus, statusEffect: poison),
                new EffectSpec(AbilityEffectType.ApplyStatus, statusEffect: burn),
                new EffectSpec(AbilityEffectType.ApplyStatus, statusEffect: root));

            PieceAbilityDefinition warCleric = EnsureAbility(
                "WarCleric_BattleHeal",
                FourStarAbilityIds.WarClericBattleHeal,
                "전투 치유",
                AbilityTrigger.AfterAttack,
                AbilityActionCost.None,
                "직접 공격으로 피해를 주면 거리 1의 가장 HP가 낮은 아군 1개를 1 회복한다.",
                new EffectSpec(AbilityEffectType.Heal, amount: 1, radius: 1));

            PieceAbilityDefinition executioner = EnsureAbility(
                "Executioner_Execute",
                FourStarAbilityIds.ExecutionerExecute,
                "처형",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "현재 HP가 최대 HP의 1/3 이하인 적에게 공격할 때 피해가 2 증가한다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, amount: 2));

            PieceAbilityDefinition stormKnight = EnsureAbility(
                "StormKnight_Reposition",
                FourStarAbilityIds.StormKnightReposition,
                "폭풍 이탈",
                AbilityTrigger.AfterAttack,
                AbilityActionCost.None,
                "공격 후 대상이 생존하면 인접한 안전한 빈 칸 1개로 이동한다.",
                new EffectSpec(AbilityEffectType.ModifyMoveRange, amount: 1, radius: 1));

            PieceAbilityDefinition bastion = EnsureAbility(
                "Bastion_Fortify",
                FourStarAbilityIds.BastionFortify,
                "철벽",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "자기 턴에 이동하지 않았다면 다음 상대 턴 첫 피해를 2 줄인다. 최소 피해는 1이다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, amount: -2));

            PieceAbilityDefinition commanderAttack = EnsureAbility(
                "FieldCommander_CommandAttack",
                FourStarAbilityIds.FieldCommanderAttack,
                "명령: 공격",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                "인접 아군 1개의 다음 공격 피해를 1 증가시킨다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, amount: 1, radius: 1));

            PieceAbilityDefinition commanderMove = EnsureAbility(
                "FieldCommander_CommandMove",
                FourStarAbilityIds.FieldCommanderMove,
                "명령: 이동",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                "인접 아군 1개의 다음 행동 이동 후보를 1칸 확장한다.",
                new EffectSpec(AbilityEffectType.ModifyMoveRange, amount: 1, durationTurns: 1, radius: 1));

            PieceAbilityDefinition deadeye = EnsureAbility(
                "Deadeye_Longshot",
                FourStarAbilityIds.DeadeyeLongshot,
                "초장거리 저격",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "5칸 이상 거리의 대상에게 공격하면 피해가 1 증가한다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, amount: 1));

            PieceAbilityDefinition gatekeeper = EnsureAbility(
                "Gatekeeper_Block",
                FourStarAbilityIds.GatekeeperBlock,
                "봉쇄",
                AbilityTrigger.AfterMove,
                AbilityActionCost.None,
                "이동 후 인접 빈 칸 1개를 다음 자기 행동까지 봉쇄한다.",
                new EffectSpec(AbilityEffectType.BlockTile, durationTurns: 1, radius: 1));

            var map = new Dictionary<string, PieceAbilityDefinition[]>(StringComparer.Ordinal)
            {
                ["marshal"] = new[] { marshal },
                ["grand_cannon"] = new[] { grandCannon },
                ["high_priest"] = new[] { highPriest },
                ["siege_chariot"] = new[] { siegeChariot },
                ["grand_guardian"] = new[] { grandGuardian },
                ["archmage"] = new[] { archmage },
                ["war_cleric"] = new[] { warCleric },
                ["executioner"] = new[] { executioner },
                ["storm_knight"] = new[] { stormKnight },
                ["bastion"] = new[] { bastion },
                ["field_commander"] = new[] { commanderAttack, commanderMove },
                ["deadeye"] = new[] { deadeye },
                ["gatekeeper"] = new[] { gatekeeper }
            };

            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[Day94] PieceDatabase.asset을 찾지 못했습니다.");
                return;
            }

            int patched = 0;

            for (int i = 0; i < FourStarPieceIds.Length; i++)
            {
                string pieceId = FourStarPieceIds[i];
                PieceDefinition piece = database.FindById(pieceId);
                if (piece == null) continue;

                if (!map.TryGetValue(pieceId, out PieceAbilityDefinition[] abilities))
                {
                    continue; // 별도 능력이 없는 4성은 이동 규칙만 유지
                }

                if (piece.Abilities.Length > 0) continue;

                SerializedObject serializedPiece = new SerializedObject(piece);
                SerializedProperty property = serializedPiece.FindProperty("_abilities");
                property.arraySize = abilities.Length;

                for (int abilityIndex = 0; abilityIndex < abilities.Length; abilityIndex++)
                {
                    property.GetArrayElementAtIndex(abilityIndex).objectReferenceValue =
                        abilities[abilityIndex];
                }

                serializedPiece.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(piece);
                patched++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (patched > 0)
            {
                Debug.Log($"[Day94] 4성 PieceDefinition Ability 연결 완료: {patched}개");
            }
        }

        private static StatusEffectDefinition EnsureStatus(StatusEffectType type)
        {
            string[] guids = AssetDatabase.FindAssets("t:StatusEffectDefinition");

            for (int i = 0; i < guids.Length; i++)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                StatusEffectDefinition existing =
                    AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(existingPath);

                if (existing != null && existing.StatusType == type)
                {
                    return existing;
                }
            }

            string fileName;
            string displayName;
            StatusStackMode stackMode;
            int maxStacks;
            int durationTurns;
            int tickDamage;

            switch (type)
            {
                case StatusEffectType.Poison:
                    fileName = "Status_Poison";
                    displayName = "독";
                    stackMode = StatusStackMode.StacksAdd;
                    maxStacks = 3;
                    durationTurns = 3;
                    tickDamage = 1;
                    break;

                case StatusEffectType.Burn:
                    fileName = "Status_Burn";
                    displayName = "화상";
                    stackMode = StatusStackMode.RefreshDuration;
                    maxStacks = 1;
                    durationTurns = 1;
                    tickDamage = 1;
                    break;

                case StatusEffectType.Root:
                    fileName = "Status_Root";
                    displayName = "속박";
                    stackMode = StatusStackMode.RefreshDuration;
                    maxStacks = 1;
                    durationTurns = 1;
                    tickDamage = 0;
                    break;

                default:
                    return null;
            }

            string path = $"{FourStarRoot}/{fileName}.asset";
            StatusEffectDefinition definition =
                AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);

            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<StatusEffectDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("_statusType").intValue = (int)type;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_stackMode").enumValueIndex = (int)stackMode;
            serialized.FindProperty("_maxStacks").intValue = maxStacks;
            serialized.FindProperty("_defaultDurationTurns").intValue = durationTurns;
            serialized.FindProperty("_tickDamagePerStack").intValue = tickDamage;
            serialized.FindProperty("_description").stringValue =
                "94일차 대마도사 주문 선택 및 공통 상태 이상 적용용 정의.";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PieceAbilityDefinition EnsureAbility(
            string fileName,
            string abilityId,
            string displayName,
            AbilityTrigger trigger,
            AbilityActionCost actionCost,
            string description,
            params EffectSpec[] effects)
        {
            string path = $"{FourStarRoot}/{fileName}.asset";
            PieceAbilityDefinition ability =
                AssetDatabase.LoadAssetAtPath<PieceAbilityDefinition>(path);

            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<PieceAbilityDefinition>();
                AssetDatabase.CreateAsset(ability, path);
            }

            SerializedObject serialized = new SerializedObject(ability);
            serialized.FindProperty("_abilityId").stringValue = abilityId;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_trigger").enumValueIndex = (int)trigger;
            serialized.FindProperty("_actionCost").enumValueIndex = (int)actionCost;
            serialized.FindProperty("_description").stringValue = description;

            SerializedProperty effectsProperty = serialized.FindProperty("_effects");
            effectsProperty.arraySize = effects?.Length ?? 0;

            for (int i = 0; i < effectsProperty.arraySize; i++)
            {
                EffectSpec spec = effects[i];
                SerializedProperty element = effectsProperty.GetArrayElementAtIndex(i);

                element.FindPropertyRelative("_effectType").enumValueIndex = (int)spec.EffectType;
                element.FindPropertyRelative("_amount").intValue = spec.Amount;
                element.FindPropertyRelative("_durationTurns").intValue = spec.DurationTurns;
                element.FindPropertyRelative("_radius").intValue = spec.Radius;
                element.FindPropertyRelative("_statusEffect").objectReferenceValue = spec.StatusEffect;
                element.FindPropertyRelative("_summonPiece").objectReferenceValue = null;
                element.FindPropertyRelative("_vector").vector2IntValue = spec.Vector;
                element.FindPropertyRelative("_auraGroupId").stringValue = spec.AuraGroupId ?? string.Empty;
                element.FindPropertyRelative("_includeSelf").boolValue = spec.IncludeSelf;

                SerializedProperty summonLimit = element.FindPropertyRelative("_maxActiveSummons");
                if (summonLimit != null) summonLimit.intValue = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(AbilityRoot))
            {
                AssetDatabase.CreateFolder("Assets/ProjectEta/Data", "Abilities");
            }

            if (!AssetDatabase.IsValidFolder(FourStarRoot))
            {
                AssetDatabase.CreateFolder(AbilityRoot, "FourStar");
            }
        }

        private readonly struct EffectSpec
        {
            public AbilityEffectType EffectType { get; }
            public int Amount { get; }
            public int DurationTurns { get; }
            public int Radius { get; }
            public StatusEffectDefinition StatusEffect { get; }
            public Vector2Int Vector { get; }
            public string AuraGroupId { get; }
            public bool IncludeSelf { get; }

            public EffectSpec(
                AbilityEffectType effectType,
                int amount = 0,
                int durationTurns = 1,
                int radius = 1,
                StatusEffectDefinition statusEffect = null,
                Vector2Int? vector = null,
                string auraGroupId = "",
                bool includeSelf = false)
            {
                EffectType = effectType;
                Amount = amount;
                DurationTurns = durationTurns;
                Radius = radius;
                StatusEffect = statusEffect;
                Vector = vector ?? Vector2Int.zero;
                AuraGroupId = auraGroupId;
                IncludeSelf = includeSelf;
            }
        }
    }
}
#endif
