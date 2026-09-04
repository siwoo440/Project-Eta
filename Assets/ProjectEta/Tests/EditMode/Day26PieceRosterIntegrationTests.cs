using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 프로젝트 데이터 에셋을 로드하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDatabase와 PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day26PieceRosterIntegrationTests // 26종 기물 데이터와 역할 태그를 통합 검증하는 테스트 모음
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 실제 기물 DB 경로

        private static readonly string[] ExpectedPieceIds = // 26일차 기준 실제 플레이 대상 26종의 고정 id 목록
        {
            "king", "pawn", "knight", "bishop", "rook", "queen", // 기본 6종
            "archbishop", "chancellor", "amazon", // 기존 합성 3종
            "wazir", "ferz", "mann", "dabbaba", "alfil", "camel", "zebra", // 단거리·도약 계열
            "centaur", "waffle", "nightrider", "camelrider", // 복합·Rider 계열
            "grasshopper", "cannon", "canvasser", "caliph", "squirrel", "chameleon" // 특수 이동 계열
        };

        [Test] // PieceDatabase가 정확히 26종을 보유하고 예상 id가 모두 존재하는지 검증
        public void PieceDatabase_HasExactlyTwentySixExpectedPieces()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드

            Assert.AreEqual(26, database.Definitions.Count); // 등록 수가 26종인지 확인
            Assert.AreEqual(26, ExpectedPieceIds.Length); // 테스트 기준 목록 자체도 26종인지 확인

            foreach (var pieceId in ExpectedPieceIds) // 예상 id를 순회
            {
                Assert.IsNotNull(database.FindById(pieceId), $"PieceDatabase에서 {pieceId}를 찾을 수 있어야 합니다."); // 모든 id 조회 성공 검증
            }
        }

        [Test] // null과 중복 id를 막고 기본 데이터가 실제 플레이 가능한 범위인지 검증
        public void PieceDatabase_AllDefinitionsHaveValidCoreDataAndUniqueIds()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드
            var uniqueIds = new HashSet<string>(); // id 중복 검사용 집합 생성

            foreach (var definition in database.Definitions) // 등록된 26종을 순회
            {
                Assert.IsNotNull(definition, "PieceDatabase에는 null 정의가 들어가면 안 됩니다."); // null 항목 금지
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.PieceId), $"{definition.name}: PieceId가 필요합니다."); // id 필수
                Assert.IsTrue(uniqueIds.Add(definition.PieceId), $"중복 PieceId: {definition.PieceId}"); // id 중복 금지
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName), $"{definition.PieceId}: 표시 이름이 필요합니다."); // 표시 이름 필수
                Assert.Greater(definition.BaseHp, 0, $"{definition.PieceId}: HP는 1 이상이어야 합니다."); // HP 유효 범위
                Assert.GreaterOrEqual(definition.BaseAtk, 0, $"{definition.PieceId}: ATK는 0 이상이어야 합니다."); // ATK 유효 범위
                Assert.Greater(definition.OccupancySize.x, 0, $"{definition.PieceId}: 점유 폭은 1 이상이어야 합니다."); // 점유 폭 검증
                Assert.Greater(definition.OccupancySize.y, 0, $"{definition.PieceId}: 점유 높이는 1 이상이어야 합니다."); // 점유 높이 검증
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description), $"{definition.PieceId}: 카드 설명이 필요합니다."); // 설명 필수

                bool hasLegacyMovement = definition.MovementType != PieceMovementType.Custom; // 기존 enum 이동을 사용하는 기물인지 확인
                bool hasDataMovement = definition.MovementRules != null && definition.MovementRules.Length > 0; // 데이터 이동 규칙 존재 여부
                Assert.IsTrue(hasLegacyMovement || hasDataMovement, $"{definition.PieceId}: Legacy 또는 MovementRules 중 하나의 이동 규칙이 필요합니다."); // 이동 규칙 누락 방지
            }
        }

        [Test] // 역할 태그가 실제 이동 계열과 일치하는 핵심 계약을 검증
        public void PieceRoleTags_MatchCoreMovementFamilies()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드

            AssertHasTag(database, "pawn", PieceRoleTag.Melee); // Pawn은 근접 기본 공격 계열
            AssertHasTag(database, "wazir", PieceRoleTag.Melee); // Wazir은 단거리 근접 계열
            AssertHasTag(database, "ferz", PieceRoleTag.Melee); // Ferz는 단거리 근접 계열

            AssertHasTag(database, "knight", PieceRoleTag.Jumper); // Knight 도약 계열
            AssertHasTag(database, "dabbaba", PieceRoleTag.Jumper); // Dabbaba 도약 계열
            AssertHasTag(database, "alfil", PieceRoleTag.Jumper); // Alfil 도약 계열
            AssertHasTag(database, "camel", PieceRoleTag.Jumper); // Camel 도약 계열
            AssertHasTag(database, "zebra", PieceRoleTag.Jumper); // Zebra 도약 계열
            AssertHasTag(database, "squirrel", PieceRoleTag.Jumper); // Squirrel 복합 도약 계열

            AssertHasTag(database, "bishop", PieceRoleTag.Slider); // Bishop 슬라이드 계열
            AssertHasTag(database, "rook", PieceRoleTag.Slider); // Rook 슬라이드 계열
            AssertHasTag(database, "queen", PieceRoleTag.Slider); // Queen 슬라이드 계열
            AssertHasTag(database, "archbishop", PieceRoleTag.Slider); // Archbishop 복합 슬라이드 계열
            AssertHasTag(database, "chancellor", PieceRoleTag.Slider); // Chancellor 복합 슬라이드 계열
            AssertHasTag(database, "amazon", PieceRoleTag.Slider); // Amazon 복합 슬라이드 계열
            AssertHasTag(database, "canvasser", PieceRoleTag.Slider); // Canvasser Rook 성분
            AssertHasTag(database, "caliph", PieceRoleTag.Slider); // Caliph Bishop 성분

            AssertHasTag(database, "nightrider", PieceRoleTag.Rider); // Nightrider Rider 계열
            AssertHasTag(database, "camelrider", PieceRoleTag.Rider); // Camelrider Rider 계열

            AssertHasTag(database, "cannon", PieceRoleTag.Ranged); // Cannon 원거리 공격 정책 연결
        }

        [Test] // 대표 기물을 통해 모든 이동 규칙 계열이 실제 MovementResolver에서 살아 있는지 검증
        public void AllMovementRuleFamilies_HaveWorkingRepresentativePieces()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드
            var emptyBoard = new BoardState(); // 일반 규칙 대표용 빈 보드
            var origin = new Vector2Int(4, 4); // 10x10 중앙 기준 좌표

            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("wazir"), origin, true, emptyBoard).MoveTiles.Count, 0); // Step
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("rook"), origin, true, emptyBoard).MoveTiles.Count, 0); // Slide
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("knight"), origin, true, emptyBoard).MoveTiles.Count, 0); // Leap
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("centaur"), origin, true, emptyBoard).MoveTiles.Count, 0); // Compound
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("nightrider"), origin, true, emptyBoard).MoveTiles.Count, 0); // Rider
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("pawn"), origin, true, emptyBoard).MoveTiles.Count, 0); // Conditional Pawn
            Assert.Greater(MovementResolver.GetReachableTiles(database.FindById("cannon"), origin, true, emptyBoard).MoveTiles.Count, 0); // Cannon

            var hopperBoard = new BoardState(); // Grasshopper는 발판이 필요한 별도 보드 생성
            var hurdle = new PieceRuntimeState(database.FindById("pawn"), new Vector2Int(4, 6), true); // 첫 발판 기물 생성
            hopperBoard.GetTile(hurdle.BoardPosition).OccupyingPiece = hurdle; // 발판을 실제 보드에 배치
            var hopper = MovementResolver.GetReachableTiles(database.FindById("grasshopper"), origin, true, hopperBoard); // Hopper 결과 계산
            Assert.Contains(new Vector2Int(4, 7), hopper.MoveTiles); // 발판 바로 뒤 한 칸 이동 확인

            var chameleon = new PieceRuntimeState(database.FindById("chameleon"), origin, true); // 상태형 Chameleon 생성
            var chameleonResult = MovementResolver.GetReachableTiles(chameleon, emptyBoard); // 초기 Knight 단계 계산
            Assert.Contains(new Vector2Int(5, 6), chameleonResult.MoveTiles); // Conditional Chameleon 초기 단계 확인
        }

        private static PieceDatabase LoadDatabase() // 실제 DB 로드 공통 도우미
        {
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath); // 지정 경로에서 DB 로드
            Assert.IsNotNull(database, "PieceDatabase.asset이 존재해야 합니다."); // DB 누락 시 즉시 실패
            return database; // 정상 DB 반환
        }

        private static void AssertHasTag(PieceDatabase database, string pieceId, PieceRoleTag expectedTag) // 역할 태그 공통 검증 도우미
        {
            var definition = database.FindById(pieceId); // id로 기물 조회
            Assert.IsNotNull(definition, $"{pieceId} 정의가 필요합니다."); // 누락 방지
            Assert.IsTrue((definition.RoleTags & expectedTag) != 0, $"{pieceId}에는 {expectedTag} 태그가 필요합니다."); // 기대 태그 포함 여부 검증
        }
    }
}
