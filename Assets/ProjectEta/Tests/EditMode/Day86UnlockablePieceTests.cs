#if UNITY_EDITOR
using System.Collections.Generic; // List·HashSet 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // 실제 에셋 로드
using UnityEngine; // Vector2Int 사용
using ProjectEta.Board; // 이동 규칙 검증
using ProjectEta.Cards; // 카드 카탈로그 사용
using ProjectEta.Meta; // 영구 해금 사용
using ProjectEta.Pieces; // 기물 데이터 사용
using ProjectEta.Run; // Reward Pool과 Snapshot 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day86UnlockablePieceTests
    {
        private static readonly string[] NewPieceIds =
        {
            "spearman",
            "shooter",
            "shield_guard",
            "flag_bearer",
            "pursuer",
            "scout"
        };

        private static readonly string[] NewUnlockIds =
        {
            PieceUnlockIds.Spearman,
            PieceUnlockIds.Shooter,
            PieceUnlockIds.ShieldGuard,
            PieceUnlockIds.FlagBearer,
            PieceUnlockIds.Pursuer,
            PieceUnlockIds.Scout
        };

        [Test]
        public void PieceDatabase_신규1성6종등록후32종이다()
        {
            PieceDatabase database = LoadDatabase(); // 실제 DB 로드
            PieceRosterValidationReport report = PieceRosterValidator.Validate(database); // 목표 로스터 비교

            Assert.That(database.Definitions.Count, Is.EqualTo(32));
            Assert.That(report.RegisteredCount, Is.EqualTo(32));
            Assert.That(report.MissingCount, Is.EqualTo(49));
            Assert.That(report.GetRegisteredCount(PieceGrade.OneStar), Is.EqualTo(18));

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                PieceDefinition definition = database.FindById(NewPieceIds[index]);
                Assert.That(definition, Is.Not.Null, NewPieceIds[index]);
                Assert.That(definition.Grade, Is.EqualTo(PieceGrade.OneStar), NewPieceIds[index]);
                Assert.That(definition.Category, Is.EqualTo(PieceCategory.Special), NewPieceIds[index]);
                Assert.That(definition.RequiredMetaUnlockId, Is.EqualTo(NewUnlockIds[index]), NewPieceIds[index]);
                Assert.That(definition.OccupancySize, Is.EqualTo(Vector2Int.one), NewPieceIds[index]);
            }
        }

        [Test]
        public void MetaUnlockCatalog_신규6종을각30토큰으로해금한다()
        {
            var pieceUnlockIds = new HashSet<string>(); // 기물 해금 ID 집합

            for (int index = 0; index < MetaUnlockCatalog.All.Count; index++)
            {
                MetaUnlockDefinition definition = MetaUnlockCatalog.All[index];
                if (definition.UnlockType != MetaUnlockType.Piece) continue;

                Assert.That(definition.Cost, Is.EqualTo(30), definition.UnlockId);
                Assert.That(pieceUnlockIds.Add(definition.UnlockId), Is.True, definition.UnlockId);
            }

            Assert.That(pieceUnlockIds.Count, Is.EqualTo(6));

            for (int index = 0; index < NewUnlockIds.Length; index++)
            {
                Assert.That(pieceUnlockIds.Contains(NewUnlockIds[index]), Is.True, NewUnlockIds[index]);
            }
        }

        [Test]
        public void RewardPool_미해금6종은제외하고다음런Snapshot에서포함한다()
        {
            PlayerStartingDeckCatalog baseCatalog = AssetDatabase.LoadAssetAtPath<PlayerStartingDeckCatalog>(
                "Assets/ProjectEta/Resources/PlayerStartingDeck26.asset"); // 기존 소스 풀
            Assert.That(baseCatalog, Is.Not.Null);

            var progress = new MetaProgressState(); // 해금 전 영구 진행
            RunContentUnlockSnapshot lockedSnapshot = RunContentUnlockSnapshot.Capture("day86_locked", progress); // 현재 런 Snapshot

            IReadOnlyList<PieceDefinition> lockedCandidates = CardRewardGenerator.Generate(
                baseCatalog.Cards,
                new List<PieceDefinition>(),
                64,
                86,
                lockedSnapshot); // 해금 전 전체 후보

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                Assert.That(ContainsId(lockedCandidates, NewPieceIds[index]), Is.False, NewPieceIds[index]);
            }

            for (int index = 0; index < NewUnlockIds.Length; index++)
            {
                progress.Unlock(MetaUnlockType.Piece, NewUnlockIds[index]); // 런 시작 후 영구 해금
            }

            IReadOnlyList<PieceDefinition> stillLockedCandidates = CardRewardGenerator.Generate(
                baseCatalog.Cards,
                new List<PieceDefinition>(),
                64,
                86,
                lockedSnapshot); // 기존 Snapshot 재사용

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                Assert.That(ContainsId(stillLockedCandidates, NewPieceIds[index]), Is.False, NewPieceIds[index]);
            }

            RunContentUnlockSnapshot nextRunSnapshot = RunContentUnlockSnapshot.Capture("day86_next", progress); // 다음 런 Snapshot
            IReadOnlyList<PieceDefinition> unlockedCandidates = CardRewardGenerator.Generate(
                baseCatalog.Cards,
                new List<PieceDefinition>(),
                64,
                86,
                nextRunSnapshot); // 다음 런 후보

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                Assert.That(ContainsId(unlockedCandidates, NewPieceIds[index]), Is.True, NewPieceIds[index]);
            }

            for (int index = 0; index < unlockedCandidates.Count; index++)
            {
                Assert.That(unlockedCandidates[index].Grade, Is.EqualTo(PieceGrade.OneStar)); // 일반 획득 후보 1성 고정
            }
        }

        [Test]
        public void RewardPool_2성Special은일반획득후보에서제외한다()
        {
            PieceDatabase database = LoadDatabase();
            PieceDefinition cannon = database.FindById("cannon"); // 기존 2성 Special

            Assert.That(cannon, Is.Not.Null);
            Assert.That(cannon.Category, Is.EqualTo(PieceCategory.Special));
            Assert.That(cannon.Grade, Is.EqualTo(PieceGrade.TwoStar));
            Assert.That(RunContentPoolRules.CanUseAsReward(cannon), Is.False); // 86일차 1성 공급 원칙
            Assert.That(RunContentPoolRules.CanUseInShop(cannon), Is.False); // Shop도 동일
        }

        [Test]
        public void Shooter_아군과적군의전방대각선이반대로계산된다()
        {
            PieceDatabase database = LoadDatabase();
            PieceDefinition shooter = database.FindById("shooter");
            var board = new BoardState();
            var origin = new Vector2Int(4, 4);

            MovementResult player = MovementResolver.GetReachableTiles(shooter, origin, true, board);
            MovementResult enemy = MovementResolver.GetReachableTiles(shooter, origin, false, board);

            Assert.Contains(new Vector2Int(3, 5), player.MoveTiles);
            Assert.Contains(new Vector2Int(5, 5), player.MoveTiles);
            Assert.IsFalse(player.MoveTiles.Contains(new Vector2Int(3, 3)));

            Assert.Contains(new Vector2Int(3, 3), enemy.MoveTiles);
            Assert.Contains(new Vector2Int(5, 3), enemy.MoveTiles);
            Assert.IsFalse(enemy.MoveTiles.Contains(new Vector2Int(3, 5)));
        }

        [Test]
        public void Spearman_빈칸은1칸이동하고직선2칸적을공격한다()
        {
            PieceDatabase database = LoadDatabase();
            PieceDefinition spearman = database.FindById("spearman");
            PieceDefinition pawn = database.FindById("pawn");
            var board = new BoardState();
            var origin = new Vector2Int(4, 4);
            var enemy = new PieceRuntimeState(pawn, new Vector2Int(4, 6), false);

            board.GetTile(enemy.BoardPosition).OccupyingPiece = enemy; // 2칸 앞 적 배치

            MovementResult result = MovementResolver.GetReachableTiles(spearman, origin, true, board);

            Assert.Contains(new Vector2Int(4, 5), result.MoveTiles); // 직교 1칸 이동
            Assert.Contains(new Vector2Int(4, 6), result.AttackTiles); // 직선 2칸 공격
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 6))); // 2칸 이동은 금지
        }

        [Test]
        public void UnlockablePoolResource_신규6종만보유한다()
        {
            PlayerStartingDeckCatalog catalog = AssetDatabase.LoadAssetAtPath<PlayerStartingDeckCatalog>(
                "Assets/ProjectEta/Resources/PlayerUnlockablePiecePool86.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Cards.Count, Is.EqualTo(6));

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                Assert.That(ContainsId(catalog.Cards, NewPieceIds[index]), Is.True, NewPieceIds[index]);
            }
        }

        private static PieceDatabase LoadDatabase()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            Assert.That(database, Is.Not.Null);
            return database;
        }

        private static bool ContainsId(IReadOnlyList<PieceDefinition> definitions, string pieceId)
        {
            if (definitions == null) return false;

            for (int index = 0; index < definitions.Count; index++)
            {
                PieceDefinition definition = definitions[index];
                if (definition != null && definition.PieceId == pieceId) return true;
            }

            return false;
        }
    }
}
#endif
