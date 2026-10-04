using System; // StringComparer 사용
using System.Collections.Generic; // 목록과 집합 사용
using System.Text; // 요약 문자열 생성

namespace ProjectEta.Pieces
{
    public sealed class PieceRosterValidationReport
    {
        private const int MaximumGrade = (int)PieceGrade.FiveStar; // 최고 등급 숫자
        private readonly int[] _registeredCounts; // 등급별 현재 등록 수
        private readonly List<string> _missingPieceIds; // 목표 로스터 중 미등록 ID
        private readonly List<string> _unexpectedPieceIds; // 목표 로스터 밖 ID
        private readonly List<string> _metadataMismatches; // 등급·분류·역할 불일치

        public static PieceRosterValidationReport Empty { get; } = new PieceRosterValidationReport(
            false,
            new int[MaximumGrade + 1],
            new List<string>(),
            new List<string>(),
            new List<string>(),
            0,
            0); // 빈 진단 결과

        public bool IsDatabaseConnected { get; } // PieceDatabase 연결 여부
        public int TargetCount => PieceRosterCatalog.TargetPieceCount; // 목표 로스터 수
        public int RegisteredCount { get; } // 목표 ID와 일치하는 현재 등록 수
        public int MissingCount => _missingPieceIds.Count; // 미등록 목표 기물 수
        public int UnexpectedCount => _unexpectedPieceIds.Count; // 예상 밖 ID 수
        public int MetadataMismatchCount => _metadataMismatches.Count; // 메타데이터 불일치 수
        public int DuplicatePieceIdCount { get; } // 중복 ID 수
        public int InvalidDefinitionCount { get; } // null·빈 ID 정의 수
        public int SchemaIssueCount => UnexpectedCount + MetadataMismatchCount + DuplicatePieceIdCount + InvalidDefinitionCount; // 구조 문제 총합
        public bool IsSchemaHealthy => IsDatabaseConnected && SchemaIssueCount == 0; // 현재 등록 데이터 구조 정상 여부
        public bool HasCompleteRoster => IsSchemaHealthy && MissingCount == 0 && RegisteredCount == TargetCount; // 81종 등록 완료 여부
        public IReadOnlyList<string> MissingPieceIds => _missingPieceIds; // 미등록 ID 목록
        public IReadOnlyList<string> UnexpectedPieceIds => _unexpectedPieceIds; // 예상 밖 ID 목록
        public IReadOnlyList<string> MetadataMismatches => _metadataMismatches; // 불일치 목록

        internal PieceRosterValidationReport(
            bool isDatabaseConnected,
            int[] registeredCounts,
            List<string> missingPieceIds,
            List<string> unexpectedPieceIds,
            List<string> metadataMismatches,
            int duplicatePieceIdCount,
            int invalidDefinitionCount)
        {
            IsDatabaseConnected = isDatabaseConnected; // DB 연결 상태 저장
            _registeredCounts = registeredCounts ?? new int[MaximumGrade + 1]; // 등급 집계 저장
            _missingPieceIds = missingPieceIds ?? new List<string>(); // 미등록 목록 저장
            _unexpectedPieceIds = unexpectedPieceIds ?? new List<string>(); // 예상 밖 목록 저장
            _metadataMismatches = metadataMismatches ?? new List<string>(); // 불일치 목록 저장
            DuplicatePieceIdCount = Math.Max(0, duplicatePieceIdCount); // 중복 수 저장
            InvalidDefinitionCount = Math.Max(0, invalidDefinitionCount); // 잘못된 정의 수 저장

            int registeredCount = 0; // 전체 등록 수 계산

            for (int grade = (int)PieceGrade.OneStar; grade <= MaximumGrade; grade++)
            {
                registeredCount += GetRegisteredCount((PieceGrade)grade); // 등급별 등록 수 합산
            }

            RegisteredCount = registeredCount; // 전체 등록 수 저장
        }

        public int GetRegisteredCount(PieceGrade grade)
        {
            int index = (int)grade; // 등급 숫자 변환
            if (index < (int)PieceGrade.OneStar || index > MaximumGrade) return 0; // 등급 범위 차단
            return _registeredCounts[index]; // 현재 등록 수 반환
        }

        public int GetMissingCount(PieceGrade grade)
        {
            return Math.Max(0, PieceRosterCatalog.GetTargetCount(grade) - GetRegisteredCount(grade)); // 등급별 미등록 수 반환
        }

        public string BuildGradeSummary()
        {
            return $"등록 {RegisteredCount}/{TargetCount} · " +
                   $"1★ {GetRegisteredCount(PieceGrade.OneStar)}/{PieceRosterCatalog.GetTargetCount(PieceGrade.OneStar)} · " +
                   $"2★ {GetRegisteredCount(PieceGrade.TwoStar)}/{PieceRosterCatalog.GetTargetCount(PieceGrade.TwoStar)} · " +
                   $"3★ {GetRegisteredCount(PieceGrade.ThreeStar)}/{PieceRosterCatalog.GetTargetCount(PieceGrade.ThreeStar)} · " +
                   $"4★ {GetRegisteredCount(PieceGrade.FourStar)}/{PieceRosterCatalog.GetTargetCount(PieceGrade.FourStar)} · " +
                   $"5★ {GetRegisteredCount(PieceGrade.FiveStar)}/{PieceRosterCatalog.GetTargetCount(PieceGrade.FiveStar)}"; // F1·Editor 요약 반환
        }

        public string BuildMissingPreview(int maxCount)
        {
            if (_missingPieceIds.Count == 0) return "없음"; // 누락 없음 표시
            int count = Math.Max(1, Math.Min(maxCount, _missingPieceIds.Count)); // 표시 수 제한
            var builder = new StringBuilder(); // 누락 ID 문자열 생성

            for (int index = 0; index < count; index++)
            {
                if (index > 0) builder.Append(", "); // ID 구분자 추가
                builder.Append(_missingPieceIds[index]); // 누락 ID 추가
            }

            if (_missingPieceIds.Count > count) builder.Append($" 외 {_missingPieceIds.Count - count}종"); // 남은 수 표시
            return builder.ToString(); // 누락 미리보기 반환
        }
    }

    public static class PieceRosterValidator
    {
        private const int MaximumGrade = (int)PieceGrade.FiveStar; // 최고 등급 숫자

        public static PieceRosterValidationReport Validate(PieceDatabase database)
        {
            return ValidateInternal(database != null ? database.Definitions : null, database != null); // DB 기준 검증 실행
        }

        public static PieceRosterValidationReport Validate(IReadOnlyList<PieceDefinition> definitions)
        {
            return ValidateInternal(definitions, definitions != null); // 목록 기준 검증 실행
        }

        private static PieceRosterValidationReport ValidateInternal(IReadOnlyList<PieceDefinition> definitions, bool isDatabaseConnected)
        {
            int[] registeredCounts = new int[MaximumGrade + 1]; // 등급별 현재 등록 수 준비
            var registeredIds = new HashSet<string>(StringComparer.Ordinal); // 정상 등록 ID 집합
            var seenPlayerIds = new HashSet<string>(StringComparer.Ordinal); // 중복 검사용 ID 집합
            var unexpectedIds = new List<string>(); // 예상 밖 ID 목록
            var metadataMismatches = new List<string>(); // 메타데이터 불일치 목록
            int duplicateCount = 0; // 중복 ID 수
            int invalidDefinitionCount = 0; // null·빈 ID 수

            if (definitions != null)
            {
                for (int index = 0; index < definitions.Count; index++)
                {
                    PieceDefinition definition = definitions[index]; // 현재 정의 조회

                    if (definition == null)
                    {
                        invalidDefinitionCount++; // null 정의 집계
                        continue;
                    }

                    if (!IsPlayerPiece(definition)) continue; // 몬스터·보스는 81종 플레이어 로스터에서 제외

                    string pieceId = definition.PieceId; // 현재 ID 읽기

                    if (string.IsNullOrWhiteSpace(pieceId))
                    {
                        invalidDefinitionCount++; // 빈 ID 집계
                        continue;
                    }

                    if (!seenPlayerIds.Add(pieceId))
                    {
                        duplicateCount++; // 중복 ID 집계
                        continue;
                    }

                    PieceRosterEntry expected = PieceRosterCatalog.FindById(pieceId); // 목표 카탈로그 조회

                    if (expected == null)
                    {
                        unexpectedIds.Add(pieceId); // 목표 밖 ID 기록
                        continue;
                    }

                    registeredIds.Add(pieceId); // 목표 로스터 등록 확인
                    int gradeIndex = (int)definition.Grade; // 실제 등급 숫자 읽기

                    if (gradeIndex >= (int)PieceGrade.OneStar && gradeIndex <= MaximumGrade)
                    {
                        registeredCounts[gradeIndex]++; // 등급별 등록 수 집계
                    }

                    ValidateMetadata(definition, expected, metadataMismatches); // 등급·분류·역할 검증
                }
            }

            var missingIds = new List<string>(); // 미등록 목표 ID 목록 준비

            for (int index = 0; index < PieceRosterCatalog.Entries.Count; index++)
            {
                PieceRosterEntry expected = PieceRosterCatalog.Entries[index]; // 목표 항목 순회

                if (!registeredIds.Contains(expected.PieceId))
                {
                    missingIds.Add(expected.PieceId); // 아직 등록되지 않은 ID 기록
                }
            }

            return new PieceRosterValidationReport(
                isDatabaseConnected,
                registeredCounts,
                missingIds,
                unexpectedIds,
                metadataMismatches,
                duplicateCount,
                invalidDefinitionCount); // 최종 진단 결과 반환
        }

        private static bool IsPlayerPiece(PieceDefinition definition)
        {
            return definition.Category != PieceCategory.Monster && definition.Category != PieceCategory.Boss; // 플레이어 로스터 분류 판정
        }

        private static void ValidateMetadata(PieceDefinition definition, PieceRosterEntry expected, List<string> mismatches)
        {
            if (definition.Grade != expected.Grade)
            {
                mismatches.Add($"{definition.PieceId}: Grade {definition.Grade} != {expected.Grade}"); // 등급 불일치 기록
            }

            if (definition.Category != expected.Category)
            {
                mismatches.Add($"{definition.PieceId}: Category {definition.Category} != {expected.Category}"); // 분류 불일치 기록
            }

            if (definition.RoleTags != expected.RoleTags)
            {
                mismatches.Add($"{definition.PieceId}: RoleTags {definition.RoleTags} != {expected.RoleTags}"); // 역할 불일치 기록
            }
        }
    }
}
