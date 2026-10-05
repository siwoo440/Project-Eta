using System.Collections.Generic; // HashSet 사용
using NUnit.Framework; // 테스트 도구 사용
using UnityEditor; // 실제 프로젝트 에셋 로드
using UnityEngine; // Vector2Int 사용
using ProjectEta.Board; // 이동 규칙 검증
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day26PieceRosterIntegrationTests
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 실제 기물 DB 경로

        private static readonly string[] ExpectedPieceIds =
        {
            "king",
            "pawn",
            "knight",
            "bishop",
            "rook",
            "queen",
            "archbishop",
            "chancellor",
            "amazon",
            "wazir",
            "ferz",
            "mann",
            "dabbaba",
            "alfil",
            "camel",
            "zebra",
            "centaur",
            "waffle",
            "nightrider",
            "camelrider",
            "grasshopper",
            "cannon",
            "canvasser",
            "caliph",
            "squirrel",
            "chameleon",
            "spearman",
            "shooter",
            "shield_guard",
            "flag_bearer",
            "pursuer",
            "scout",
            "assault_trooper",
            "sentry",
            "breaker",
            "courier",
            "ambusher",
            "paladin",
            "war_chariot",
            "grenadier",
            "pikeman",
            "crossbowman",
            "guardian",
            "hunter",
            "falcon",
            "unicorn",
            "gryphon",
            "dragon_horse",
            "dragon_king",
            "artillery",
            "vanguard",
            "tactician",
            "medic",
            "summoner",
            "sniper"
        }; // 87일차 기준 현재 실제 등록 55종

        [Test]
        public void PieceDatabase_HasExactlyFiftyFiveExpectedPieces()
        {
            PieceDatabase database = LoadDatabase();

            Assert.AreEqual(55, database.Definitions.Count);
            Assert.AreEqual(55, ExpectedPieceIds.Length);

            foreach (string pieceId in ExpectedPieceIds)
            {
                Assert.IsNotNull(database.FindById(pieceId), $"PieceDatabase에서 {pieceId}를 찾을 수 있어야 합니다.");
            }
        }

        [Test]
        public void PieceDatabase_AllDefinitionsHaveValidCoreDataAndUniqueIds()
        {
            PieceDatabase database = LoadDatabase();
            var uniqueIds = new HashSet<string>();

            foreach (PieceDefinition definition in database.Definitions)
            {
                Assert.IsNotNull(definition, "PieceDatabase에는 null 정의가 들어가면 안 됩니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.PieceId), $"{definition.name}: PieceId가 필요합니다.");
                Assert.IsTrue(uniqueIds.Add(definition.PieceId), $"중복 PieceId: {definition.PieceId}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName), $"{definition.PieceId}: 표시 이름이 필요합니다.");
                Assert.Greater(definition.BaseHp, 0, $"{definition.PieceId}: HP는 1 이상이어야 합니다.");
                Assert.GreaterOrEqual(definition.BaseAtk, 0, $"{definition.PieceId}: ATK는 0 이상이어야 합니다.");
                Assert.AreEqual(Vector2Int.one, definition.OccupancySize, $"{definition.PieceId}: 플레이어 기물은 현재 1×1 점유여야 합니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description), $"{definition.PieceId}: 카드 설명이 필요합니다.");

                bool hasLegacyMovement = definition.MovementType != PieceMovementType.Custom;
                bool hasDataMovement = definition.MovementRules != null && definition.MovementRules.Length > 0;
                Assert.IsTrue(hasLegacyMovement || hasDataMovement, $"{definition.PieceId}: 이동 규칙이 필요합니다.");
            }
        }

        [Test]
        public void PieceRoleTags_MatchCoreMovementFamilies()
        {
            PieceDatabase database = LoadDatabase();

            AssertHasTag(database, "pawn", PieceRoleTag.Melee);
            AssertHasTag(database, "knight", PieceRoleTag.Jumper);
            AssertHasTag(database, "rook", PieceRoleTag.Slider);
            AssertHasTag(database, "nightrider", PieceRoleTag.Rider);
            AssertHasTag(database, "cannon", PieceRoleTag.Ranged);
            AssertHasTag(database, "spearman", PieceRoleTag.Ranged);
            AssertHasTag(database, "shield_guard", PieceRoleTag.Tanker);
            AssertHasTag(database, "flag_bearer", PieceRoleTag.Support);
            AssertHasTag(database, "assault_trooper", PieceRoleTag.Jumper);
            AssertHasTag(database, "sentry", PieceRoleTag.Tanker);
            AssertHasTag(database, "breaker", PieceRoleTag.Attacker);
            AssertHasTag(database, "paladin", PieceRoleTag.Tanker);
            AssertHasTag(database, "unicorn", PieceRoleTag.Rider);
            AssertHasTag(database, "artillery", PieceRoleTag.Ranged);
            AssertHasTag(database, "summoner", PieceRoleTag.Summoner);
            AssertHasTag(database, "sniper", PieceRoleTag.Ranged);
        }

        [Test]
        public void AllMovementRuleFamilies_HaveWorkingRepresentativePieces()
        {
            PieceDatabase database = LoadDatabase();
            var emptyBoard = new BoardState();
            var origin = new Vector2Int(4, 4);

            string[] movableIds =
            {
                "wazir", "rook", "knight", "centaur", "nightrider", "pawn", "cannon", "scout",
                "assault_trooper", "sentry", "courier", "paladin", "grenadier", "gryphon", "artillery", "sniper"
            };

            for (int index = 0; index < movableIds.Length; index++)
            {
                PieceDefinition definition = database.FindById(movableIds[index]);
                MovementResult result = MovementResolver.GetReachableTiles(definition, origin, true, emptyBoard);
                Assert.Greater(result.MoveTiles.Count, 0, movableIds[index]);
            }

            var hopperBoard = new BoardState();
            var hurdle = new PieceRuntimeState(database.FindById("pawn"), new Vector2Int(4, 6), true);
            hopperBoard.GetTile(hurdle.BoardPosition).OccupyingPiece = hurdle;
            MovementResult hopper = MovementResolver.GetReachableTiles(database.FindById("grasshopper"), origin, true, hopperBoard);
            Assert.Contains(new Vector2Int(4, 7), hopper.MoveTiles);
        }

        private static PieceDatabase LoadDatabase()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath);
            Assert.IsNotNull(database, "PieceDatabase.asset이 존재해야 합니다.");
            return database;
        }

        private static void AssertHasTag(PieceDatabase database, string pieceId, PieceRoleTag expectedTag)
        {
            PieceDefinition definition = database.FindById(pieceId);
            Assert.IsNotNull(definition, $"{pieceId} 정의가 필요합니다.");
            Assert.IsTrue((definition.RoleTags & expectedTag) != 0, $"{pieceId}에는 {expectedTag} 태그가 필요합니다.");
        }
    }
}
