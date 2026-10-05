#if UNITY_EDITOR
using System.Collections.Generic; // HashSet·Dictionary 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // 실제 에셋 로드
using UnityEngine; // Vector2Int 사용
using ProjectEta.Board; // 이동 검증
using ProjectEta.Fusion; // 합성 데이터 사용
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day87FusionExpansionTests
    {
        [Test]
        public void ProjectData_55종기물과41개레시피를등록한다()
        {
            PieceDatabase pieces = LoadPieces();
            FusionRecipeDatabase recipes = LoadRecipes();

            Assert.That(pieces.Definitions.Count, Is.EqualTo(55));
            Assert.That(recipes.Recipes.Count, Is.EqualTo(41));

            PieceRosterValidationReport roster = PieceRosterValidator.Validate(pieces);
            Assert.That(roster.GetRegisteredCount(PieceGrade.OneStar), Is.EqualTo(18));
            Assert.That(roster.GetRegisteredCount(PieceGrade.TwoStar), Is.EqualTo(19));
            Assert.That(roster.GetRegisteredCount(PieceGrade.ThreeStar), Is.EqualTo(18));
            Assert.That(roster.MissingCount, Is.EqualTo(26));
        }

        [Test]
        public void FusionDatabase_1대2성21개와2대3성20개를유지한다()
        {
            FusionRecipeDatabase database = LoadRecipes();

            int twoStar = 0;
            int threeStar = 0;
            int hiddenTwoStar = 0;
            int hiddenThreeStar = 0;

            foreach (FusionRecipe recipe in database.Recipes)
            {
                Assert.That(recipe, Is.Not.Null);

                if (recipe.Result.Grade == PieceGrade.TwoStar)
                {
                    twoStar++;
                    if (recipe.IsHiddenRecipe) hiddenTwoStar++;
                }
                else if (recipe.Result.Grade == PieceGrade.ThreeStar)
                {
                    threeStar++;
                    if (recipe.IsHiddenRecipe) hiddenThreeStar++;
                }
            }

            Assert.That(twoStar, Is.EqualTo(21));
            Assert.That(threeStar, Is.EqualTo(20));
            Assert.That(hiddenTwoStar, Is.EqualTo(2));
            Assert.That(hiddenThreeStar, Is.EqualTo(2));
        }

        [Test]
        public void FusionDatabase_공개37개숨김4개이며데이터문제가없다()
        {
            FusionRecipeDatabase database = LoadRecipes();
            int hidden = 0;

            foreach (FusionRecipe recipe in database.Recipes)
            {
                if (recipe.IsHiddenRecipe) hidden++;
            }

            Assert.That(database.Recipes.Count - hidden, Is.EqualTo(37));
            Assert.That(hidden, Is.EqualTo(4));
            Assert.That(FusionRecipeContentValidator.Validate(database).Count, Is.EqualTo(0));
        }

        [Test]
        public void FusionRules_Special재료를허용하지만King은제외한다()
        {
            PieceDatabase database = LoadPieces();

            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("spearman")), Is.True);
            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("cannon")), Is.True);
            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("chameleon")), Is.True);
            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("king")), Is.False);
        }

        [Test]
        public void FusionDatabase_방향성중복조합은A와B순서로결과를구분한다()
        {
            PieceDatabase pieces = LoadPieces();
            FusionRecipeDatabase recipes = LoadRecipes();

            PieceDefinition breaker = pieces.FindById("breaker");
            PieceDefinition assault = pieces.FindById("assault_trooper");

            Assert.That(recipes.TryFindRecipe(breaker, assault, out FusionRecipe pikemanRecipe), Is.True);
            Assert.That(pikemanRecipe.Result.PieceId, Is.EqualTo("pikeman"));
            Assert.That(pikemanRecipe.UsesOrderedMaterials, Is.True);

            Assert.That(recipes.TryFindRecipe(assault, breaker, out FusionRecipe vanguardRecipe), Is.True);
            Assert.That(vanguardRecipe.Result.PieceId, Is.EqualTo("vanguard"));
            Assert.That(vanguardRecipe.UsesOrderedMaterials, Is.True);
        }

        [Test]
        public void FusionDatabase_동일카드숨김조합을지원한다()
        {
            PieceDatabase pieces = LoadPieces();
            FusionRecipeDatabase recipes = LoadRecipes();

            Assert.That(recipes.TryFindRecipe(pieces.FindById("queen"), pieces.FindById("queen"), out FusionRecipe queenPair), Is.True);
            Assert.That(queenPair.Result.PieceId, Is.EqualTo("amazon"));
            Assert.That(queenPair.IsHiddenRecipe, Is.True);
            Assert.That(queenPair.UsesIdenticalMaterials, Is.True);

            Assert.That(recipes.TryFindRecipe(pieces.FindById("knight"), pieces.FindById("knight"), out FusionRecipe knightPair), Is.True);
            Assert.That(knightPair.Result.PieceId, Is.EqualTo("nightrider"));
            Assert.That(knightPair.IsHiddenRecipe, Is.True);
            Assert.That(knightPair.UsesIdenticalMaterials, Is.True);
        }

        [Test]
        public void FusionDatabase_모든결과는재료최고등급보다정확히1단계높다()
        {
            FusionRecipeDatabase database = LoadRecipes();

            foreach (FusionRecipe recipe in database.Recipes)
            {
                Assert.That(recipe.IgnoresGradeStepRule, Is.False, recipe.RecipeId);
                Assert.That(FusionRuleValidator.IsGradeStepValid(recipe), Is.True, recipe.RecipeId);
            }
        }

        [Test]
        public void FusionProgression_1성에서3성까지연결되고4성에서끊긴다()
        {
            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(LoadPieces(), LoadRecipes());

            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(21));
            Assert.That(report.GetRecipeCount(PieceGrade.ThreeStar), Is.EqualTo(20));
            Assert.That(report.FirstUnreachableGrade, Is.EqualTo(PieceGrade.FourStar));
            Assert.That(report.ContentIssueCount, Is.EqualTo(0));
        }

        [Test]
        public void ThreeStarPieces_기획서임시HPATK를적용한다()
        {
            PieceDatabase database = LoadPieces();
            var expected = new Dictionary<string, Vector2Int>
            {
                { "paladin", new Vector2Int(5, 4) },
                { "war_chariot", new Vector2Int(5, 4) },
                { "grenadier", new Vector2Int(4, 4) },
                { "pikeman", new Vector2Int(5, 4) },
                { "crossbowman", new Vector2Int(4, 5) },
                { "guardian", new Vector2Int(6, 3) },
                { "hunter", new Vector2Int(4, 5) },
                { "falcon", new Vector2Int(4, 5) },
                { "unicorn", new Vector2Int(4, 4) },
                { "gryphon", new Vector2Int(5, 4) },
                { "dragon_horse", new Vector2Int(5, 4) },
                { "dragon_king", new Vector2Int(6, 4) },
                { "artillery", new Vector2Int(4, 5) },
                { "vanguard", new Vector2Int(5, 5) },
                { "tactician", new Vector2Int(4, 3) },
                { "medic", new Vector2Int(5, 2) },
                { "summoner", new Vector2Int(4, 3) },
                { "sniper", new Vector2Int(3, 5) }
            };

            foreach (KeyValuePair<string, Vector2Int> pair in expected)
            {
                PieceDefinition definition = database.FindById(pair.Key);
                Assert.That(definition.BaseHp, Is.EqualTo(pair.Value.x), pair.Key);
                Assert.That(definition.BaseAtk, Is.EqualTo(pair.Value.y), pair.Key);
            }
        }

        [Test]
        public void NewTwoAndThreeStarPieces_빈보드에서이동후보를가진다()
        {
            PieceDatabase database = LoadPieces();
            var board = new BoardState();
            var origin = new Vector2Int(4, 4);
            string[] ids =
            {
                "assault_trooper", "sentry", "breaker", "courier", "ambusher",
                "paladin", "war_chariot", "grenadier", "pikeman", "crossbowman", "guardian", "hunter",
                "falcon", "unicorn", "gryphon", "dragon_horse", "dragon_king", "artillery", "vanguard",
                "tactician", "medic", "summoner", "sniper"
            };

            for (int index = 0; index < ids.Length; index++)
            {
                MovementResult result = MovementResolver.GetReachableTiles(database.FindById(ids[index]), origin, true, board);
                Assert.That(result.MoveTiles.Count, Is.GreaterThan(0), ids[index]);
            }
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
