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
            "king", "pawn", "knight", "bishop", "rook", "queen",
            "archbishop", "chancellor", "amazon",
            "wazir", "ferz", "mann", "dabbaba", "alfil", "camel", "zebra",
            "centaur", "waffle", "nightrider", "camelrider",
            "grasshopper", "cannon", "canvasser", "caliph", "squirrel", "chameleon",
            "spearman", "shooter", "shield_guard", "flag_bearer", "pursuer", "scout"
        }; // 86일차 기준 현재 실제 등록 32종

        [Test]
        public void PieceDatabase_HasExactlyThirtyTwoExpectedPieces()
        {
            PieceDatabase database = LoadDatabase(); // 실제 DB 로드

            Assert.AreEqual(32, database.Definitions.Count); // 현재 등록 수 검증
            Assert.AreEqual(32, ExpectedPieceIds.Length); // 기준 목록 수 검증

            foreach (string pieceId in ExpectedPieceIds)
            {
                Assert.IsNotNull(database.FindById(pieceId), $"PieceDatabase에서 {pieceId}를 찾을 수 있어야 합니다."); // 모든 ID 조회 검증
            }
        }

        [Test]
        public void PieceDatabase_AllDefinitionsHaveValidCoreDataAndUniqueIds()
        {
            PieceDatabase database = LoadDatabase(); // 실제 DB 로드
            var uniqueIds = new HashSet<string>(); // 중복 검사 집합

            foreach (PieceDefinition definition in database.Definitions)
            {
                Assert.IsNotNull(definition, "PieceDatabase에는 null 정의가 들어가면 안 됩니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.PieceId), $"{definition.name}: PieceId가 필요합니다.");
                Assert.IsTrue(uniqueIds.Add(definition.PieceId), $"중복 PieceId: {definition.PieceId}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName), $"{definition.PieceId}: 표시 이름이 필요합니다.");
                Assert.Greater(definition.BaseHp, 0, $"{definition.PieceId}: HP는 1 이상이어야 합니다.");
                Assert.GreaterOrEqual(definition.BaseAtk, 0, $"{definition.PieceId}: ATK는 0 이상이어야 합니다.");
                Assert.Greater(definition.OccupancySize.x, 0, $"{definition.PieceId}: 점유 폭은 1 이상이어야 합니다.");
                Assert.Greater(definition.OccupancySize.y, 0, $"{definition.PieceId}: 점유 높이는 1 이상이어야 합니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description), $"{definition.PieceId}: 카드 설명이 필요합니다.");

                bool hasLegacyMovement = definition.MovementType != PieceMovementType.Custom; // 기존 이동 타입 존재 여부
                bool hasDataMovement = definition.MovementRules != null && definition.MovementRules.Length > 0; // 데이터 이동 규칙 존재 여부
                Assert.IsTrue(hasLegacyMovement || hasDataMovement, $"{definition.PieceId}: 이동 규칙이 필요합니다.");
            }
        }

        [Test]
        public void PieceRoleTags_MatchCoreMovementFamilies()
        {
            PieceDatabase database = LoadDatabase(); // 실제 DB 로드

            AssertHasTag(database, "pawn", PieceRoleTag.Melee);
            AssertHasTag(database, "wazir", PieceRoleTag.Melee);
            AssertHasTag(database, "ferz", PieceRoleTag.Melee);
            AssertHasTag(database, "knight", PieceRoleTag.Jumper);
            AssertHasTag(database, "dabbaba", PieceRoleTag.Jumper);
            AssertHasTag(database, "alfil", PieceRoleTag.Jumper);
            AssertHasTag(database, "camel", PieceRoleTag.Jumper);
            AssertHasTag(database, "zebra", PieceRoleTag.Jumper);
            AssertHasTag(database, "squirrel", PieceRoleTag.Jumper);
            AssertHasTag(database, "bishop", PieceRoleTag.Slider);
            AssertHasTag(database, "rook", PieceRoleTag.Slider);
            AssertHasTag(database, "queen", PieceRoleTag.Slider);
            AssertHasTag(database, "archbishop", PieceRoleTag.Slider);
            AssertHasTag(database, "chancellor", PieceRoleTag.Slider);
            AssertHasTag(database, "amazon", PieceRoleTag.Slider);
            AssertHasTag(database, "canvasser", PieceRoleTag.Slider);
            AssertHasTag(database, "caliph", PieceRoleTag.Slider);
            AssertHasTag(database, "nightrider", PieceRoleTag.Rider);
            AssertHasTag(database, "camelrider", PieceRoleTag.Rider);
            AssertHasTag(database, "cannon", PieceRoleTag.Ranged);

            AssertHasTag(database, "spearman", PieceRoleTag.Ranged); // 신규 창병 원거리 공격 성격
            AssertHasTag(database, "shooter", PieceRoleTag.Slider); // 신규 사수 슬라이드 성격
            AssertHasTag(database, "shield_guard", PieceRoleTag.Tanker); // 신규 방패병 탱커 성격
            AssertHasTag(database, "flag_bearer", PieceRoleTag.Support); // 신규 깃발병 지원 성격
            AssertHasTag(database, "pursuer", PieceRoleTag.Attacker); // 신규 추격병 공격 성격
            AssertHasTag(database, "scout", PieceRoleTag.Attacker); // 신규 척후병 공격 성격
        }

        [Test]
        public void AllMovementRuleFamilies_HaveWorkingRepresentativePieces()
        {
            PieceDatabase database = LoadDatabase(); // 실제 DB 로드
            var emptyBoard = new BoardState(); // 빈 보드 생성
            var origin = new Vector2Int(4, 4); // 중앙 좌표

            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("wazir"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("rook"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("knight"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("centaur"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("nightrider"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("pawn"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("cannon"), origin, true, emptyBoard).MoveTiles.Count, 0);
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("scout"), origin, true, emptyBoard).MoveTiles.Count, 0); // 86일차 방향 조건부 이동

            var hopperBoard = new BoardState();
            var hurdle = new PieceRuntimeState(database.FindById("pawn"), new Vector2Int(4, 6), true);
            hopperBoard.GetTile(hurdle.BoardPosition).OccupyingPiece = hurdle;
            MovementResult hopper = MovementResolver.GetReachableTiles(database.FindById("grasshopper"), origin, true, hopperBoard);
            Assert.Contains(new Vector2Int(4, 7), hopper.MoveTiles);

            var chameleon = new PieceRuntimeState(database.FindById("chameleon"), origin, true);
            MovementResult chameleonResult = MovementResolver.GetReachableTiles(chameleon, emptyBoard);
            Assert.Contains(new Vector2Int(5, 6), chameleonResult.MoveTiles);
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
