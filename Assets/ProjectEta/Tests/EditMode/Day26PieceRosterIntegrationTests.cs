using System.Collections.Generic; // HashSet 사용
using NUnit.Framework; // 테스트 도구
using UnityEditor; // 실제 프로젝트 에셋 로드
using UnityEngine; // Vector2Int 사용
using ProjectEta.Board; // 이동 규칙 검증
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day26PieceRosterIntegrationTests
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset";

        [Test]
        public void PieceDatabase_HasExactlyTargetEightyOnePieces()
        {
            PieceDatabase database = LoadDatabase();

            Assert.AreEqual(PieceRosterCatalog.TargetPieceCount, database.Definitions.Count);
            Assert.AreEqual(81, database.Definitions.Count);

            for (int index = 0; index < PieceRosterCatalog.Entries.Count; index++)
            {
                PieceRosterEntry entry = PieceRosterCatalog.Entries[index];
                Assert.IsNotNull(database.FindById(entry.PieceId), $"PieceDatabase에서 {entry.PieceId}를 찾을 수 있어야 합니다.");
            }
        }

        [Test]
        public void PieceDatabase_AllDefinitionsHaveValidCoreDataAndUniqueIds()
        {
            PieceDatabase database = LoadDatabase();
            var uniqueIds = new HashSet<string>();

            foreach (PieceDefinition definition in database.Definitions)
            {
                Assert.IsNotNull(definition);
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.PieceId), definition != null ? definition.name : "null");
                Assert.IsTrue(uniqueIds.Add(definition.PieceId), $"중복 PieceId: {definition.PieceId}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName), definition.PieceId);
                Assert.Greater(definition.BaseHp, 0, definition.PieceId);
                Assert.GreaterOrEqual(definition.BaseAtk, 0, definition.PieceId);
                Assert.AreEqual(Vector2Int.one, definition.OccupancySize, definition.PieceId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description), definition.PieceId);

                bool hasLegacyMovement = definition.MovementType != PieceMovementType.Custom;
                bool hasDataMovement = definition.MovementRules != null && definition.MovementRules.Length > 0;
                Assert.IsTrue(hasLegacyMovement || hasDataMovement, $"{definition.PieceId}: 이동 규칙이 필요합니다.");
            }
        }

        [Test]
        public void PieceRoleTags_HighGradeRepresentativesMatchCatalog()
        {
            PieceDatabase database = LoadDatabase();

            AssertHasTag(database, "marshal", PieceRoleTag.Support);
            AssertHasTag(database, "grand_cannon", PieceRoleTag.Ranged);
            AssertHasTag(database, "imperial_knight", PieceRoleTag.Jumper);
            AssertHasTag(database, "grand_guardian", PieceRoleTag.Tanker);
            AssertHasTag(database, "grand_unicorn", PieceRoleTag.Rider);
            AssertHasTag(database, "deadeye", PieceRoleTag.Ranged);
            AssertHasTag(database, "siege_commander", PieceRoleTag.Ranged);
            AssertHasTag(database, "grand_paladin", PieceRoleTag.Tanker);
            AssertHasTag(database, "grand_rider", PieceRoleTag.Rider);
            AssertHasTag(database, "phantom_general", PieceRoleTag.Attacker);
            AssertHasTag(database, "grand_sage", PieceRoleTag.Support);
            AssertHasTag(database, "iron_regent", PieceRoleTag.Tanker);
        }

        [Test]
        public void HighGradeMovementFamilies_HaveWorkingRepresentativePieces()
        {
            PieceDatabase database = LoadDatabase();
            var board = new BoardState();
            var origin = new Vector2Int(4, 4);

            string[] ids =
            {
                "marshal", "grand_cannon", "imperial_knight", "high_priest", "war_rider",
                "siege_chariot", "grand_guardian", "grand_unicorn", "grand_gryphon", "archmage",
                "war_cleric", "executioner", "storm_knight", "bastion", "field_commander", "deadeye",
                "gatekeeper", "siege_commander", "grand_paladin", "grand_rider", "emperor",
                "sky_marshal", "grand_sage", "iron_regent"
            };

            for (int index = 0; index < ids.Length; index++)
            {
                MovementResult result = MovementResolver.GetReachableTiles(database.FindById(ids[index]), origin, true, board);
                Assert.Greater(result.MoveTiles.Count, 0, ids[index]);
            }
        }

        private static PieceDatabase LoadDatabase()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath);
            Assert.IsNotNull(database);
            return database;
        }

        private static void AssertHasTag(PieceDatabase database, string pieceId, PieceRoleTag expectedTag)
        {
            PieceDefinition definition = database.FindById(pieceId);
            Assert.IsNotNull(definition, pieceId);
            Assert.IsTrue((definition.RoleTags & expectedTag) != 0, $"{pieceId}: {expectedTag}");
        }
    }
}
