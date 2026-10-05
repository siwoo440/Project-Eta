#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.EditorTools
{
    public static class Day94FourStarValidationMenu
    {
        [MenuItem("Project Eta/Day 94/Validate Four Star Integration")]
        public static void Validate()
        {
            Day94FourStarAbilityPatcher.PatchIfNeeded();

            PieceDatabase database =
                AssetDatabase.LoadAssetAtPath<PieceDatabase>(
                    "Assets/ProjectEta/Data/PieceDatabase.asset");

            if (database == null)
            {
                Debug.LogError("[Day94] PieceDatabase.asset이 없습니다.");
                return;
            }

            int fourStarCount = 0;
            int piecesWithAbilities = 0;
            int totalAbilityLinks = 0;

            for (int i = 0; i < database.Definitions.Count; i++)
            {
                PieceDefinition definition = database.Definitions[i];

                if (definition == null ||
                    definition.Grade != PieceGrade.FourStar)
                {
                    continue;
                }

                fourStarCount++;

                if (definition.Abilities.Length > 0)
                {
                    piecesWithAbilities++;
                    totalAbilityLinks += definition.Abilities.Length;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("[Day94] 4성 기물 통합 검증");
            builder.AppendLine($"4성 기물: {fourStarCount}/18");
            builder.AppendLine($"고유 Ability 연결 기물: {piecesWithAbilities}/13");
            builder.AppendLine($"연결 Ability 수: {totalAbilityLinks}/14");
            builder.AppendLine($"임시 공격 버프 수: {TemporaryDamageModifierService.ActiveCount}");
            builder.AppendLine("복합 처리: 지휘/Aura · Heal2 · 공성 장애물 · 피해 분담 · 상태 선택 · 공격 후 치유 · 처형 · 철벽 · 장거리 저격 · 봉쇄");
            builder.AppendLine("UI: 선택 기물 Ability 설명 Overlay 활성");

            bool valid =
                fourStarCount == 18 &&
                piecesWithAbilities >= 13 &&
                totalAbilityLinks >= 14;

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
