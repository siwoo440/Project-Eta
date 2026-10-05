#if UNITY_EDITOR
using System.Collections.Generic; // Dictionary 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // 실제 에셋 로드
using UnityEngine; // GameObject·Vector2Int 사용
using ProjectEta.Battle; // TurnManager 사용
using ProjectEta.Board; // BoardInputController·BoardView 사용
using ProjectEta.Cards; // 보유 상한 사용
using ProjectEta.Fusion; // 합성 데이터 사용
using ProjectEta.Pieces; // 기물 데이터 사용
using ProjectEta.Run; // RunState 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day88UpperFusionCompletionTests
    {
        [Test]
        public void ProjectData_81종기물과70개레시피를등록한다()
        {
            PieceDatabase pieces = LoadPieces();
            FusionRecipeDatabase recipes = LoadRecipes();
            PieceRosterValidationReport roster = PieceRosterValidator.Validate(pieces);

            Assert.That(pieces.Definitions.Count, Is.EqualTo(81));
            Assert.That(recipes.Recipes.Count, Is.EqualTo(70));
            Assert.That(roster.RegisteredCount, Is.EqualTo(81));
            Assert.That(roster.MissingCount, Is.EqualTo(0));
            Assert.That(roster.HasCompleteRoster, Is.True);
            Assert.That(roster.MetadataMismatchCount, Is.EqualTo(0), string.Join("\n", roster.MetadataMismatches));
        }

        [Test]
        public void FusionDatabase_등급별21_20_20_9와공개63숨김7을유지한다()
        {
            FusionRecipeDatabase database = LoadRecipes();
            var gradeCounts = new Dictionary<PieceGrade, int>();
            int hidden = 0;

            foreach (FusionRecipe recipe in database.Recipes)
            {
                if (!gradeCounts.ContainsKey(recipe.Result.Grade)) gradeCounts[recipe.Result.Grade] = 0;
                gradeCounts[recipe.Result.Grade]++;

                if (recipe.IsHiddenRecipe) hidden++;
            }

            Assert.That(gradeCounts[PieceGrade.TwoStar], Is.EqualTo(21));
            Assert.That(gradeCounts[PieceGrade.ThreeStar], Is.EqualTo(20));
            Assert.That(gradeCounts[PieceGrade.FourStar], Is.EqualTo(20));
            Assert.That(gradeCounts[PieceGrade.FiveStar], Is.EqualTo(9));
            Assert.That(hidden, Is.EqualTo(7));
            Assert.That(database.Recipes.Count - hidden, Is.EqualTo(63));
            Assert.That(FusionRecipeContentValidator.Validate(database).Count, Is.EqualTo(0));
        }

        [Test]
        public void FusionProgression_1성부터5성까지전체경로가연결된다()
        {
            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(LoadPieces(), LoadRecipes());

            Assert.That(report.GetPieceCount(PieceGrade.OneStar), Is.EqualTo(18));
            Assert.That(report.GetPieceCount(PieceGrade.TwoStar), Is.EqualTo(19));
            Assert.That(report.GetPieceCount(PieceGrade.ThreeStar), Is.EqualTo(18));
            Assert.That(report.GetPieceCount(PieceGrade.FourStar), Is.EqualTo(18));
            Assert.That(report.GetPieceCount(PieceGrade.FiveStar), Is.EqualTo(8));
            Assert.That(report.FirstMissingPieceGrade, Is.Null);
            Assert.That(report.FirstMissingRecipeGrade, Is.Null);
            Assert.That(report.FirstUnreachableGrade, Is.Null);
            Assert.That(report.ContentIssueCount, Is.EqualTo(0));
            Assert.That(report.HasCompleteGradeCoverage, Is.True);
        }

        [Test]
        public void UpperFusion_유니콘매방향성조합은서로다른4성결과를만든다()
        {
            PieceDatabase pieces = LoadPieces();
            FusionRecipeDatabase recipes = LoadRecipes();

            PieceDefinition unicorn = pieces.FindById("unicorn");
            PieceDefinition falcon = pieces.FindById("falcon");

            Assert.That(recipes.TryFindRecipe(unicorn, falcon, out FusionRecipe warRider), Is.True);
            Assert.That(warRider.Result.PieceId, Is.EqualTo("war_rider"));
            Assert.That(warRider.UsesOrderedMaterials, Is.True);

            Assert.That(recipes.TryFindRecipe(falcon, unicorn, out FusionRecipe imperialKnight), Is.True);
            Assert.That(imperialKnight.Result.PieceId, Is.EqualTo("imperial_knight"));
            Assert.That(imperialKnight.IsHiddenRecipe, Is.True);
            Assert.That(imperialKnight.UsesOrderedMaterials, Is.True);
        }

        [Test]
        public void HighGradeOwnershipLimits_4성2개5성1개를사용한다()
        {
            PieceDatabase pieces = LoadPieces();
            PieceDefinition marshal = pieces.FindById("marshal");
            PieceDefinition emperor = pieces.FindById("emperor");

            Assert.That(CardOwnershipRules.GetOwnedLimit(marshal.Grade), Is.EqualTo(2));
            Assert.That(CardOwnershipRules.GetOwnedLimit(emperor.Grade), Is.EqualTo(1));

            Assert.That(FusionRuleValidator.ValidateOwnedLimit(marshal, 1), Is.EqualTo(FusionBlockReason.None));
            Assert.That(FusionRuleValidator.ValidateOwnedLimit(marshal, 2), Is.EqualTo(FusionBlockReason.OwnedLimitReached));
            Assert.That(FusionRuleValidator.ValidateOwnedLimit(emperor, 0), Is.EqualTo(FusionBlockReason.None));
            Assert.That(FusionRuleValidator.ValidateOwnedLimit(emperor, 1), Is.EqualTo(FusionBlockReason.OwnedLimitReached));
        }

        [Test]
        public void HighGradeDeploymentLimits_4성2개5성1개를실제보드에서차단한다()
        {
            PieceDatabase pieces = LoadPieces();
            PieceDefinition marshal = pieces.FindById("marshal");
            PieceDefinition emperor = pieces.FindById("emperor");
            GameObject root = new GameObject("Day88DeployLimitRoot");
            BoardView boardView = root.AddComponent<BoardView>();
            BoardInputController boardInput = root.AddComponent<BoardInputController>();
            var run = new RunState(3);
            var turn = new TurnManager();

            try
            {
                boardView.Bind(run.Board);
                boardInput.Bind(run, boardView, turn);

                run.Board.GetTile(new Vector2Int(0, 0)).OccupyingPiece =
                    new PieceRuntimeState(marshal, new Vector2Int(0, 0), true);
                Assert.That(boardInput.IsWithinDeployLimit(marshal), Is.True);

                run.Board.GetTile(new Vector2Int(1, 0)).OccupyingPiece =
                    new PieceRuntimeState(marshal, new Vector2Int(1, 0), true);
                Assert.That(boardInput.IsWithinDeployLimit(marshal), Is.False);

                Assert.That(boardInput.IsWithinDeployLimit(emperor), Is.True);
                run.Board.GetTile(new Vector2Int(2, 0)).OccupyingPiece =
                    new PieceRuntimeState(emperor, new Vector2Int(2, 0), true);
                Assert.That(boardInput.IsWithinDeployLimit(emperor), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FourStarPieces_기획서임시HPATK를적용한다()
        {
            PieceDatabase database = LoadPieces();
            var expected = new Dictionary<string, Vector2Int>
            {
                { "marshal", new Vector2Int(6, 6) },
                { "grand_cannon", new Vector2Int(6, 6) },
                { "imperial_knight", new Vector2Int(6, 6) },
                { "high_priest", new Vector2Int(6, 5) },
                { "war_rider", new Vector2Int(6, 6) },
                { "siege_chariot", new Vector2Int(7, 6) },
                { "grand_guardian", new Vector2Int(8, 4) },
                { "grand_unicorn", new Vector2Int(6, 6) },
                { "grand_gryphon", new Vector2Int(6, 6) },
                { "archmage", new Vector2Int(5, 6) },
                { "war_cleric", new Vector2Int(7, 5) },
                { "executioner", new Vector2Int(6, 7) },
                { "storm_knight", new Vector2Int(6, 6) },
                { "bastion", new Vector2Int(9, 3) },
                { "field_commander", new Vector2Int(6, 5) },
                { "illusionist", new Vector2Int(5, 5) },
                { "deadeye", new Vector2Int(5, 7) },
                { "gatekeeper", new Vector2Int(8, 4) }
            };

            foreach (KeyValuePair<string, Vector2Int> pair in expected)
            {
                PieceDefinition definition = database.FindById(pair.Key);
                Assert.That(definition.BaseHp, Is.EqualTo(pair.Value.x), pair.Key);
                Assert.That(definition.BaseAtk, Is.EqualTo(pair.Value.y), pair.Key);
            }
        }

        [Test]
        public void FiveStarPieces_명시된수치는기획서값을사용하고미확정4종은테스트값표시를유지한다()
        {
            PieceDatabase database = LoadPieces();

            Assert.That(database.FindById("emperor").BaseHp, Is.EqualTo(9));
            Assert.That(database.FindById("emperor").BaseAtk, Is.EqualTo(8));
            Assert.That(database.FindById("sky_marshal").BaseHp, Is.EqualTo(8));
            Assert.That(database.FindById("sky_marshal").BaseAtk, Is.EqualTo(9));
            Assert.That(database.FindById("grand_sage").BaseHp, Is.EqualTo(10));
            Assert.That(database.FindById("grand_sage").BaseAtk, Is.EqualTo(6));
            Assert.That(database.FindById("iron_regent").BaseHp, Is.EqualTo(10));
            Assert.That(database.FindById("iron_regent").BaseAtk, Is.EqualTo(7));

            string[] unresolved =
            {
                "siege_commander", "grand_paladin", "grand_rider", "phantom_general"
            };

            for (int index = 0; index < unresolved.Length; index++)
            {
                PieceDefinition definition = database.FindById(unresolved[index]);
                Assert.That(definition.BaseHp, Is.EqualTo(8), unresolved[index]);
                Assert.That(definition.BaseAtk, Is.EqualTo(8), unresolved[index]);
                StringAssert.Contains("기획서에 HP ATK 수치가 없어", definition.Description);
            }
        }

        [Test]
        public void CyclicHighGradePieces_환술사3단계환영장군5단계를순환한다()
        {
            PieceDatabase database = LoadPieces();

            var illusionist = new PieceRuntimeState(database.FindById("illusionist"), new Vector2Int(4, 4), true);
            Assert.That(illusionist.MovementCycleIndex, Is.EqualTo(0));
            illusionist.AdvanceMovementCycle();
            Assert.That(illusionist.MovementCycleIndex, Is.EqualTo(1));
            illusionist.AdvanceMovementCycle();
            Assert.That(illusionist.MovementCycleIndex, Is.EqualTo(2));
            illusionist.AdvanceMovementCycle();
            Assert.That(illusionist.MovementCycleIndex, Is.EqualTo(0));

            var phantom = new PieceRuntimeState(database.FindById("phantom_general"), new Vector2Int(4, 4), true);
            for (int i = 0; i < 5; i++) phantom.AdvanceMovementCycle();
            Assert.That(phantom.MovementCycleIndex, Is.EqualTo(0));
            phantom.RestoreMovementCycleIndex(4);
            Assert.That(phantom.MovementCycleIndex, Is.EqualTo(4));
        }

        private static PieceDatabase LoadPieces()
        {
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            Assert.That(database, Is.Not.Null);
            return database;
        }

        private static FusionRecipeDatabase LoadRecipes()
        {
            FusionRecipeDatabase database = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset");
            Assert.That(database, Is.Not.Null);
            return database;
        }
    }
}
#endif
