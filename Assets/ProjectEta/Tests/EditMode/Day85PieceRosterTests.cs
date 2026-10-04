#if UNITY_EDITOR
using System.Collections.Generic; // HashSet 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // 실제 프로젝트 에셋 로드
using ProjectEta.Pieces; // 로스터 데이터 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day85PieceRosterTests
    {
        [Test]
        public void TargetRoster_총81종과등급분포를유지한다()
        {
            Assert.That(PieceRosterCatalog.Entries.Count, Is.EqualTo(81)); // 전체 목표 수 검증
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.OneStar), Is.EqualTo(18)); // 1성 목표 수 검증
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.TwoStar), Is.EqualTo(19)); // 2성 목표 수 검증
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.ThreeStar), Is.EqualTo(18)); // 3성 목표 수 검증
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.FourStar), Is.EqualTo(18)); // 4성 목표 수 검증
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.FiveStar), Is.EqualTo(8)); // 5성 목표 수 검증
        }

        [Test]
        public void TargetRoster_PieceId가모두고유하고비어있지않다()
        {
            var ids = new HashSet<string>(); // 고유 ID 집합 생성

            for (int index = 0; index < PieceRosterCatalog.Entries.Count; index++)
            {
                PieceRosterEntry entry = PieceRosterCatalog.Entries[index]; // 현재 목표 항목 조회
                Assert.That(entry, Is.Not.Null); // 목표 항목 존재 검증
                Assert.That(entry.PieceId, Is.Not.Null.And.Not.Empty); // ID 존재 검증
                Assert.That(ids.Add(entry.PieceId), Is.True, $"중복 PieceId: {entry.PieceId}"); // ID 중복 차단
            }
        }

        [Test]
        public void ProjectData_현재26종등록55종미등록상태다()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 DB 로드
            Assert.That(database, Is.Not.Null); // DB 존재 검증

            PieceRosterValidationReport report = PieceRosterValidator.Validate(database); // 현재 로스터 진단

            Assert.That(report.IsDatabaseConnected, Is.True); // DB 연결 검증
            Assert.That(report.RegisteredCount, Is.EqualTo(26)); // 현재 등록 26종 검증
            Assert.That(report.MissingCount, Is.EqualTo(55)); // 현재 미등록 55종 검증
            Assert.That(report.GetRegisteredCount(PieceGrade.OneStar), Is.EqualTo(12)); // 현재 1성 12종 검증
            Assert.That(report.GetRegisteredCount(PieceGrade.TwoStar), Is.EqualTo(14)); // 현재 2성 14종 검증
            Assert.That(report.GetRegisteredCount(PieceGrade.ThreeStar), Is.EqualTo(0)); // 현재 3성 미등록 검증
            Assert.That(report.GetRegisteredCount(PieceGrade.FourStar), Is.EqualTo(0)); // 현재 4성 미등록 검증
            Assert.That(report.GetRegisteredCount(PieceGrade.FiveStar), Is.EqualTo(0)); // 현재 5성 미등록 검증
        }

        [Test]
        public void ProjectData_현재26종은목표ID와메타데이터에충돌이없다()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 DB 로드
            PieceRosterValidationReport report = PieceRosterValidator.Validate(database); // 현재 로스터 진단

            Assert.That(report.UnexpectedCount, Is.EqualTo(0)); // 목표 밖 ID 없음 검증
            Assert.That(report.DuplicatePieceIdCount, Is.EqualTo(0)); // 중복 ID 없음 검증
            Assert.That(report.MetadataMismatchCount, Is.EqualTo(0), string.Join("\n", report.MetadataMismatches)); // 메타 불일치 없음 검증
            Assert.That(report.InvalidDefinitionCount, Is.EqualTo(0)); // 잘못된 정의 없음 검증
            Assert.That(report.IsSchemaHealthy, Is.True); // 구조 정상 검증
            Assert.That(report.HasCompleteRoster, Is.False); // 미등록 55종 때문에 완료가 아님 검증
        }

        [Test]
        public void TargetRoster_85일차신규ID11종을포함한다()
        {
            string[] requiredIds =
            {
                "spearman", "shooter", "shield_guard", "flag_bearer", "pursuer", "scout",
                "assault_trooper", "sentry", "breaker", "courier", "ambusher"
            }; // 1~2성 미등록 11종 ID 기준

            for (int index = 0; index < requiredIds.Length; index++)
            {
                Assert.That(PieceRosterCatalog.FindById(requiredIds[index]), Is.Not.Null, requiredIds[index]); // 신규 ID 존재 검증
            }
        }
    }
}
#endif
