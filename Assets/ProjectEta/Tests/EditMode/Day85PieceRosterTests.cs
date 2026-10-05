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
            Assert.That(PieceRosterCatalog.Entries.Count, Is.EqualTo(81));
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.OneStar), Is.EqualTo(18));
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.TwoStar), Is.EqualTo(19));
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.ThreeStar), Is.EqualTo(18));
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.FourStar), Is.EqualTo(18));
            Assert.That(PieceRosterCatalog.GetTargetCount(PieceGrade.FiveStar), Is.EqualTo(8));
        }

        [Test]
        public void TargetRoster_PieceId가모두고유하고비어있지않다()
        {
            var ids = new HashSet<string>();

            for (int index = 0; index < PieceRosterCatalog.Entries.Count; index++)
            {
                PieceRosterEntry entry = PieceRosterCatalog.Entries[index];
                Assert.That(ids.Add(entry.PieceId), Is.True, $"중복 PieceId: {entry.PieceId}");
            }
        }

        [Test]
        public void ProjectData_목표81종이모두등록됐다()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            PieceRosterValidationReport report = PieceRosterValidator.Validate(database);

            Assert.That(report.RegisteredCount, Is.EqualTo(81));
            Assert.That(report.MissingCount, Is.EqualTo(0));
            Assert.That(report.GetRegisteredCount(PieceGrade.OneStar), Is.EqualTo(18));
            Assert.That(report.GetRegisteredCount(PieceGrade.TwoStar), Is.EqualTo(19));
            Assert.That(report.GetRegisteredCount(PieceGrade.ThreeStar), Is.EqualTo(18));
            Assert.That(report.GetRegisteredCount(PieceGrade.FourStar), Is.EqualTo(18));
            Assert.That(report.GetRegisteredCount(PieceGrade.FiveStar), Is.EqualTo(8));
            Assert.That(report.UnexpectedCount, Is.EqualTo(0));
            Assert.That(report.DuplicatePieceIdCount, Is.EqualTo(0));
            Assert.That(report.MetadataMismatchCount, Is.EqualTo(0), string.Join("\n", report.MetadataMismatches));
            Assert.That(report.InvalidDefinitionCount, Is.EqualTo(0));
            Assert.That(report.IsSchemaHealthy, Is.True);
            Assert.That(report.HasCompleteRoster, Is.True);
        }
    }
}
#endif
