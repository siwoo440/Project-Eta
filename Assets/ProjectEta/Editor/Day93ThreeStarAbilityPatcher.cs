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
    public static class Day93ThreeStarAbilityPatcher
    {
        private const string DatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset";
        private const string PawnPath = "Assets/ProjectEta/Data/Pawn.asset";
        private const string AbilityRoot = "Assets/ProjectEta/Data/Abilities";
        private const string ThreeStarRoot = AbilityRoot + "/ThreeStar";

        private static readonly string[] ThreeStarPieceIds =
        {
            "paladin", "war_chariot", "grenadier", "pikeman", "crossbowman", "guardian",
            "hunter", "falcon", "unicorn", "gryphon", "dragon_horse", "dragon_king",
            "artillery", "vanguard", "tactician", "medic", "summoner", "sniper"
        };

        static Day93ThreeStarAbilityPatcher()
        {
            EditorApplication.delayCall += PatchIfNeeded;
        }

        [MenuItem("Project Eta/Day 93/Patch Three Star Abilities")]
        public static void PatchIfNeeded()
        {
            EnsureFolders();

            PieceDefinition pawn = AssetDatabase.LoadAssetAtPath<PieceDefinition>(PawnPath);

            PieceAbilityDefinition paladin = EnsureAbility(
                "Paladin_Guard",
                ThreeStarPassiveAbilityResolver.PaladinGuardId,
                "수호",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "인접 아군이 피해를 받을 때 전투당 1회 그 피해를 1 줄인다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, -1, radius: 1));

            PieceAbilityDefinition warChariot = EnsureAbility(
                "WarChariot_Charge",
                ThreeStarPassiveAbilityResolver.WarChariotChargeId,
                "가속 타격",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "3칸 이상 이동한 뒤 다음 공격 피해가 1 증가한다. 현재 행동 구조에서는 마지막 장거리 이동 기록을 사용한다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, 1));

            PieceAbilityDefinition guardian = EnsureAbility(
                "Guardian_Escort",
                ThreeStarPassiveAbilityResolver.GuardianEscortId,
                "호위",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "인접한 플레이어 왕이 피해를 받을 때 전투당 1회 피해를 1 줄인다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, -1, radius: 1));

            PieceAbilityDefinition hunter = EnsureAbility(
                "Hunter_Tracking",
                ThreeStarPassiveAbilityResolver.HunterTrackingId,
                "추적",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "공격 대상의 현재 HP가 최대 HP의 절반 이하라면 공격 피해가 1 증가한다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, 1));

            PieceAbilityDefinition vanguard = EnsureAbility(
                "Vanguard_Frontline",
                ThreeStarPassiveAbilityResolver.VanguardFrontlineId,
                "선봉",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                "전방 방향으로 공격할 때 피해가 1 증가한다.",
                new EffectSpec(AbilityEffectType.ModifyDamage, 1));

            PieceAbilityDefinition tactician = EnsureAbility(
                "Tactician_Order",
                "three_tactician_order",
                "전술 지시",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                "인접 아군 1개를 지정해 다음 행동의 이동 후보에 상하좌우 1칸을 추가한다.",
                new EffectSpec(AbilityEffectType.ModifyMoveRange, 1, durationTurns: 1, radius: 1, vector: Vector2Int.up),
                new EffectSpec(AbilityEffectType.ModifyMoveRange, 1, durationTurns: 1, radius: 1, vector: Vector2Int.down),
                new EffectSpec(AbilityEffectType.ModifyMoveRange, 1, durationTurns: 1, radius: 1, vector: Vector2Int.left),
                new EffectSpec(AbilityEffectType.ModifyMoveRange, 1, durationTurns: 1, radius: 1, vector: Vector2Int.right));

            PieceAbilityDefinition medic = EnsureAbility(
                "Medic_FirstAid",
                "three_medic_first_aid",
                "응급치료",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                "인접 아군 일반 기물 1개의 HP를 1 회복한다.",
                new EffectSpec(AbilityEffectType.Heal, 1, radius: 1));

            PieceAbilityDefinition summoner = EnsureAbility(
                "Summoner_TemporaryPawn",
                "three_summoner_temporary_pawn",
                "임시 소환",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                "인접 빈 칸에 임시 폰 1개를 소환한다. 동시에 1개만 유지한다.",
                new EffectSpec(
                    AbilityEffectType.Summon,
                    0,
                    durationTurns: 1,
                    radius: 1,
                    summonPiece: pawn,
                    maxActiveSummons: 1));

            var abilityByPiece = new Dictionary<string, PieceAbilityDefinition>(StringComparer.Ordinal)
            {
                ["paladin"] = paladin,
                ["war_chariot"] = warChariot,
                ["guardian"] = guardian,
                ["hunter"] = hunter,
                ["vanguard"] = vanguard,
                ["tactician"] = tactician,
                ["medic"] = medic,
                ["summoner"] = summoner
            };

            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[Day93] PieceDatabase.asset을 찾지 못했습니다.");
                return;
            }

            int patched = 0;

            for (int i = 0; i < ThreeStarPieceIds.Length; i++)
            {
                string pieceId = ThreeStarPieceIds[i];
                PieceDefinition piece = database.FindById(pieceId);
                if (piece == null) continue;

                if (!abilityByPiece.TryGetValue(pieceId, out PieceAbilityDefinition ability) || ability == null)
                {
                    continue; // 기획서에 별도 '능력:'이 없는 3성은 기존 이동·공격 규칙만 유지
                }

                if (piece.Abilities.Length > 0) continue; // 사용자/후속 일차의 기존 Ability를 덮어쓰지 않음

                SerializedObject serializedPiece = new SerializedObject(piece);
                SerializedProperty abilities = serializedPiece.FindProperty("_abilities");
                abilities.arraySize = 1;
                abilities.GetArrayElementAtIndex(0).objectReferenceValue = ability;
                serializedPiece.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(piece);
                patched++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (patched > 0)
            {
                Debug.Log($"[Day93] 3성 PieceDefinition Ability 연결 완료: {patched}개");
            }
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
            string path = $"{ThreeStarRoot}/{fileName}.asset";
            PieceAbilityDefinition ability = AssetDatabase.LoadAssetAtPath<PieceAbilityDefinition>(path);

            if (ability != null)
            {
                return ability; // 한 번 생성된 Day93 Ability는 이후 사용자 밸런스 조정을 덮어쓰지 않음
            }

            ability = ScriptableObject.CreateInstance<PieceAbilityDefinition>();
            AssetDatabase.CreateAsset(ability, path);

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
                element.FindPropertyRelative("_summonPiece").objectReferenceValue = spec.SummonPiece;
                element.FindPropertyRelative("_vector").vector2IntValue = spec.Vector;
                element.FindPropertyRelative("_auraGroupId").stringValue = spec.AuraGroupId ?? string.Empty;
                element.FindPropertyRelative("_includeSelf").boolValue = spec.IncludeSelf;

                SerializedProperty summonLimit = element.FindPropertyRelative("_maxActiveSummons");
                if (summonLimit != null) summonLimit.intValue = spec.MaxActiveSummons;
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

            if (!AssetDatabase.IsValidFolder(ThreeStarRoot))
            {
                AssetDatabase.CreateFolder(AbilityRoot, "ThreeStar");
            }
        }

        private readonly struct EffectSpec
        {
            public AbilityEffectType EffectType { get; }
            public int Amount { get; }
            public int DurationTurns { get; }
            public int Radius { get; }
            public StatusEffectDefinition StatusEffect { get; }
            public PieceDefinition SummonPiece { get; }
            public Vector2Int Vector { get; }
            public string AuraGroupId { get; }
            public bool IncludeSelf { get; }
            public int MaxActiveSummons { get; }

            public EffectSpec(
                AbilityEffectType effectType,
                int amount = 0,
                int durationTurns = 1,
                int radius = 1,
                StatusEffectDefinition statusEffect = null,
                PieceDefinition summonPiece = null,
                Vector2Int? vector = null,
                string auraGroupId = "",
                bool includeSelf = false,
                int maxActiveSummons = 0)
            {
                EffectType = effectType;
                Amount = amount;
                DurationTurns = durationTurns;
                Radius = radius;
                StatusEffect = statusEffect;
                SummonPiece = summonPiece;
                Vector = vector ?? Vector2Int.zero;
                AuraGroupId = auraGroupId;
                IncludeSelf = includeSelf;
                MaxActiveSummons = maxActiveSummons;
            }
        }
    }
}
#endif
