#if UNITY_EDITOR
using System.Collections.Generic; // Dictionary 사용
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
        public void LowerFusionSegments_1대2성21개와2대3성20개를유지한다()
        {
            FusionRecipeDatabase database = LoadRecipes();

            int twoStar = 0;
            int threeStar = 0;
            int hiddenTwoStar = 0;
            int hiddenThreeStar = 0;

            foreach (FusionRecipe recipe in database.Recipes)
            {
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
        public void LowerFusionSegments_공개37개숨김4개를유지한다()
        {
            FusionRecipeDatabase database = LoadRecipes();
            int publicCount = 0;
            int hiddenCount = 0;

            foreach (FusionRecipe recipe in database.Recipes)
            {
                if (recipe.Result.Grade != PieceGrade.TwoStar &&
                    recipe.Result.Grade != PieceGrade.ThreeStar) continue;

                if (recipe.IsHiddenRecipe) hiddenCount++;
                else publicCount++;
            }

            Assert.That(publicCount, Is.EqualTo(37));
            Assert.That(hiddenCount, Is.EqualTo(4));
            Assert.That(FusionRecipeContentValidator.Validate(database).Count, Is.EqualTo(0));
        }

        [Test]
        public void FusionRules_Special재료를허용하지만실제King은제외한다()
        {
            PieceDatabase database = LoadPieces();

            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("spearman")), Is.True);
            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("cannon")), Is.True);
            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("chameleon")), Is.True);
            Assert.That(FusionRuleValidator.IsFusableMaterial(database.FindById("king")), Is.False);
        }

        [Test]
        public void FusionDatabase_87일차방향성중복조합은AB순서로결과를구분한다()
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
        public void ThreeStarPieces_기획서임시HPATK를유지한다()
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
