#if UNITY_EDITOR
using System.Text; // 로그 문자열 생성
using UnityEditor; // 메뉴와 에셋 로드
using UnityEngine; // Debug 사용
using ProjectEta.Meta; // 해금 카탈로그 사용
using ProjectEta.Pieces; // 기물 DB 사용

namespace ProjectEta.EditorTools
{
    public static class Day86UnlockablePieceValidationMenu
    {
        private static readonly string[] PieceIds =
        {
            "spearman",
            "shooter",
            "shield_guard",
            "flag_bearer",
            "pursuer",
            "scout"
        };

        [MenuItem("Project Eta/Day 86/Validate Unlockable Pieces")]
        public static void Validate()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 DB 로드
            PieceRosterValidationReport roster = PieceRosterValidator.Validate(database); // 81종 목표 비교
            var builder = new StringBuilder();

            builder.AppendLine("[Day86] 신규 1성 영구 해금 검증");
            builder.AppendLine(roster.BuildGradeSummary());
            builder.AppendLine($"미등록 목표: {roster.MissingCount}");
            builder.AppendLine($"구조 문제: {roster.SchemaIssueCount}");

            for (int index = 0; index < PieceIds.Length; index++)
            {
                PieceDefinition definition = database != null ? database.FindById(PieceIds[index]) : null;
                builder.AppendLine(definition != null
                    ? $"{definition.DisplayName} · {definition.PieceId} · Unlock={definition.RequiredMetaUnlockId}"
                    : $"누락 · {PieceIds[index]}");
            }

            int pieceUnlockCount = 0;
            for (int index = 0; index < MetaUnlockCatalog.All.Count; index++)
            {
                if (MetaUnlockCatalog.All[index].UnlockType == MetaUnlockType.Piece) pieceUnlockCount++;
            }

            builder.AppendLine($"기물 해금 항목: {pieceUnlockCount}/6");

            if (database != null && roster.SchemaIssueCount == 0 && pieceUnlockCount == 6)
            {
                Debug.Log(builder.ToString());
            }
            else
            {
                Debug.LogError(builder.ToString());
            }
        }
    }
}
#endif
