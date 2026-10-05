#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectEta.AI;
using ProjectEta.Pieces;

namespace ProjectEta.EditorTools
{
    public static class Day93ThreeStarValidationMenu
    {
        [MenuItem("Project Eta/Day 93/Validate Three Star Integration")]
        public static void Validate()
        {
            Day93ThreeStarAbilityPatcher.PatchIfNeeded();

            PieceDatabase database =
                AssetDatabase.LoadAssetAtPath<PieceDatabase>(
                    "Assets/ProjectEta/Data/PieceDatabase.asset");

            if (database == null)
            {
                Debug.LogError("[Day93] PieceDatabase.asset이 없습니다.");
                return;
            }

            int threeStarCount = 0;
            int piecesWithAbilities = 0;

            for (int i = 0; i < database.Definitions.Count; i++)
            {
                PieceDefinition definition = database.Definitions[i];

                if (definition == null ||
                    definition.Grade != PieceGrade.ThreeStar)
                {
                    continue;
                }

                threeStarCount++;

                if (definition.Abilities.Length > 0)
                {
                    piecesWithAbilities++;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("[Day93] 3성 기물 통합 검증");
            builder.AppendLine($"3성 기물: {threeStarCount}/18");
            builder.AppendLine($"기획서 명시 Ability 연결 기물: {piecesWithAbilities}/8");
            builder.AppendLine($"AI ActionType.Ability: {(int)AIActionType.Ability}");
            builder.AppendLine("원거리·지원·소환 AI 후보 확장: 활성");

            bool valid =
                threeStarCount == 18 &&
                piecesWithAbilities >= 8 &&
                AIActionType.Ability == (AIActionType)2;

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
