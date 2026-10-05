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
            "spearman", "shooter", "shield_guard", "flag_bearer", "pursuer", "scout"
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
        public void PieceDatabase_신규1성6종이81종DB안에유지된다()
        {
            PieceDatabase database = LoadDatabase();
            PieceRosterValidationReport report = PieceRosterValidator.Validate(database);

            Assert.That(database.Definitions.Count, Is.EqualTo(81));
            Assert.That(report.RegisteredCount, Is.EqualTo(81));
            Assert.That(report.MissingCount, Is.EqualTo(0));
            Assert.That(report.GetRegisteredCount(PieceGrade.OneStar), Is.EqualTo(18));

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                PieceDefinition definition = database.FindById(NewPieceIds[index]);
                Assert.That(definition, Is.Not.Null, NewPieceIds[index]);
                Assert.That(definition.Grade, Is.EqualTo(PieceGrade.OneStar));
                Assert.That(definition.Category, Is.EqualTo(PieceCategory.Special));
                Assert.That(definition.RequiredMetaUnlockId, Is.EqualTo(NewUnlockIds[index]));
                Assert.That(definition.OccupancySize, Is.EqualTo(Vector2Int.one));
            }
        }

        [Test]
        public void MetaUnlockCatalog_신규6종을각30토큰으로해금한다()
        {
            var pieceUnlockIds = new HashSet<string>();

            for (int index = 0; index < MetaUnlockCatalog.All.Count; index++)
            {
                MetaUnlockDefinition definition = MetaUnlockCatalog.All[index];
                if (definition.UnlockType != MetaUnlockType.Piece) continue;

                Assert.That(definition.Cost, Is.EqualTo(30));
                Assert.That(pieceUnlockIds.Add(definition.UnlockId), Is.True);
            }

            Assert.That(pieceUnlockIds.Count, Is.EqualTo(6));
        }

        [Test]
        public void RewardPool_미해금6종은제외하고다음런Snapshot에서포함한다()
        {
            PlayerStartingDeckCatalog baseCatalog = AssetDatabase.LoadAssetAtPath<PlayerStartingDeckCatalog>(
                "Assets/ProjectEta/Resources/PlayerStartingDeck26.asset");
            var progress = new MetaProgressState();
            RunContentUnlockSnapshot lockedSnapshot = RunContentUnlockSnapshot.Capture("day86_locked", progress);

            IReadOnlyList<PieceDefinition> lockedCandidates = CardRewardGenerator.Generate(
                baseCatalog.Cards, new List<PieceDefinition>(), 64, 86, lockedSnapshot);

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                Assert.That(ContainsId(lockedCandidates, NewPieceIds[index]), Is.False);
                progress.Unlock(MetaUnlockType.Piece, NewUnlockIds[index]);
            }

            RunContentUnlockSnapshot nextRunSnapshot = RunContentUnlockSnapshot.Capture("day86_next", progress);
            IReadOnlyList<PieceDefinition> unlockedCandidates = CardRewardGenerator.Generate(
                baseCatalog.Cards, new List<PieceDefinition>(), 64, 86, nextRunSnapshot);

            for (int index = 0; index < NewPieceIds.Length; index++)
            {
                Assert.That(ContainsId(unlockedCandidates, NewPieceIds[index]), Is.True);
            }
        }

        [Test]
        public void Shooter_아군과적군의전방대각선이반대로계산된다()
        {
            PieceDefinition shooter = LoadDatabase().FindById("shooter");
            var board = new BoardState();
            var origin = new Vector2Int(4, 4);

            MovementResult player = MovementResolver.GetReachableTiles(shooter, origin, true, board);
            MovementResult enemy = MovementResolver.GetReachableTiles(shooter, origin, false, board);

            Assert.Contains(new Vector2Int(3, 5), player.MoveTiles);
            Assert.Contains(new Vector2Int(3, 3), enemy.MoveTiles);
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
