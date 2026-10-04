#if UNITY_EDITOR
using System.Text; // Console 문자열 생성
using UnityEditor; // MenuItem과 AssetDatabase 사용
using UnityEngine; // Debug 사용
using ProjectEta.Pieces; // 로스터 진단 사용

namespace ProjectEta.EditorTools
{
    public static class Day85PieceRosterValidationMenu
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 실제 기물 DB 경로

        [MenuItem("Project Eta/Day 85/Validate 81-Piece Roster")]
        public static void ValidateTargetRoster()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath); // 실제 DB 로드
            PieceRosterValidationReport report = PieceRosterValidator.Validate(database); // 81종 기준 검증

            var builder = new StringBuilder(); // 결과 로그 생성
            builder.AppendLine("[Day85] 81종 목표 로스터 검증"); // 제목 출력
            builder.AppendLine(report.BuildGradeSummary()); // 등급별 현황 출력
            builder.AppendLine($"미등록: {report.MissingCount}"); // 미등록 수 출력
            builder.AppendLine($"예상 밖 ID: {report.UnexpectedCount}"); // 예상 밖 ID 출력
            builder.AppendLine($"중복 ID: {report.DuplicatePieceIdCount}"); // 중복 ID 출력
            builder.AppendLine($"메타 불일치: {report.MetadataMismatchCount}"); // 메타 불일치 출력
            builder.AppendLine($"잘못된 정의: {report.InvalidDefinitionCount}"); // 잘못된 정의 출력
            builder.AppendLine($"미등록 ID: {report.BuildMissingPreview(81)}"); // 전체 미등록 ID 출력

            if (report.IsSchemaHealthy)
            {
                Debug.Log(builder.ToString()); // 정상 구조 로그 출력
            }
            else
            {
                for (int index = 0; index < report.MetadataMismatches.Count; index++)
                {
                    builder.AppendLine($"불일치 · {report.MetadataMismatches[index]}"); // 상세 불일치 추가
                }

                Debug.LogError(builder.ToString()); // 구조 문제 로그 출력
            }
        }
    }
}
#endif
