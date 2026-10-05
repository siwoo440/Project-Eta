#if UNITY_EDITOR
using System.Collections.Generic; // IReadOnlyList 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // 실제 에셋 로드
using ProjectEta.Fusion; // Atlas·Recipe·트리 검증 사용
using ProjectEta.Meta; // 영구 메타 저장 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day89FusionAtlasTests
    {
        [Test]
        public void MetaProgress_SaveRoundTrip_PersistsFusionAtlasDiscoveries()
        {
            FusionRecipeDatabase database = LoadRecipes();
            IReadOnlyList<FusionRecipe> hidden = GetHiddenRecipes(database);
            Assert.That(hidden.Count, Is.EqualTo(7));

            var progress = new MetaProgressState();
            Assert.That(progress.TryDiscoverFusionRecipe(hidden[0].RecipeId), Is.True);
            Assert.That(progress.TryDiscoverFusionRecipe(hidden[1].RecipeId), Is.True);
            Assert.That(progress.TryDiscoverFusionRecipe(hidden[0].RecipeId), Is.False);

            string json = MetaProgressSaveService.Serialize(progress);
            MetaProgressState restored = MetaProgressSaveService.Deserialize(json);

            Assert.That(MetaProgressSaveData.CurrentVersion, Is.EqualTo(3));
            Assert.That(restored.DiscoveredFusionRecipeIds.Count, Is.EqualTo(2));
            Assert.That(restored.IsFusionRecipeDiscovered(hidden[0].RecipeId), Is.True);
            Assert.That(restored.IsFusionRecipeDiscovered(hidden[1].RecipeId), Is.True);
        }

        [Test]
        public void MetaProgress_Version2Json_LoadsWithEmptyFusionAtlas()
        {
            const string legacyJson =
                "{\"version\":2,\"metaTokens\":12,\"unlockedPieceIds\":[],\"unlockedKingIds\":[],\"unlockedPassiveIds\":[],\"claimedRunRewardIds\":[]}";

            MetaProgressState restored = MetaProgressSaveService.Deserialize(legacyJson);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.MetaTokens, Is.EqualTo(12));
            Assert.That(restored.DiscoveredFusionRecipeIds.Count, Is.EqualTo(0));
        }

        [Test]
        public void FusionAtlas_EmptyMetaShows63PublicRecipes_ThenRevealsDiscoveredHidden()
        {
            FusionRecipeDatabase database = LoadRecipes();
            FusionRecipe hidden = GetHiddenRecipes(database)[0];
            var progress = new MetaProgressState();

            IReadOnlyList<FusionRecipe> before =
                FusionAtlasService.GetVisibleRecipes(database, progress);

            Assert.That(before.Count, Is.EqualTo(63));
            Assert.That(ContainsRecipe(before, hidden), Is.False);

            Assert.That(progress.TryDiscoverFusionRecipe(hidden.RecipeId), Is.True);

            IReadOnlyList<FusionRecipe> after =
                FusionAtlasService.GetVisibleRecipes(database, progress);

            Assert.That(after.Count, Is.EqualTo(64));
            Assert.That(ContainsRecipe(after, hidden), Is.True);
        }

        [Test]
        public void FusionAtlas_AllSevenHiddenRecipesPersistWithoutDuplicates()
        {
            FusionRecipeDatabase database = LoadRecipes();
            IReadOnlyList<FusionRecipe> hidden = GetHiddenRecipes(database);
            var progress = new MetaProgressState();

            for (int i = 0; i < hidden.Count; i++)
            {
                Assert.That(progress.TryDiscoverFusionRecipe(hidden[i].RecipeId), Is.True, hidden[i].RecipeId);
                Assert.That(progress.TryDiscoverFusionRecipe(hidden[i].RecipeId), Is.False, hidden[i].RecipeId);
            }

            string json = MetaProgressSaveService.Serialize(progress);
            MetaProgressState restored = MetaProgressSaveService.Deserialize(json);

            Assert.That(restored.DiscoveredFusionRecipeIds.Count, Is.EqualTo(7));

            for (int i = 0; i < hidden.Count; i++)
            {
                Assert.That(restored.IsFusionRecipeDiscovered(hidden[i].RecipeId), Is.True, hidden[i].RecipeId);
            }
        }

        [Test]
        public void FusionAtlas_MergesLegacyRunDiscoveryIntoPermanentProgress()
        {
            FusionRecipe hidden = GetHiddenRecipes(LoadRecipes())[0];
            var runDiscovery = new FusionDiscoveryLog();
            var progress = new MetaProgressState();

            Assert.That(runDiscovery.TryMarkDiscovered(hidden), Is.True); // EditMode에서는 기존 런 단위 동작
            Assert.That(progress.IsFusionRecipeDiscovered(hidden.RecipeId), Is.False);

            int merged = FusionAtlasService.MergeRunDiscoveriesIntoPermanent(runDiscovery, progress);

            Assert.That(merged, Is.EqualTo(1));
            Assert.That(progress.IsFusionRecipeDiscovered(hidden.RecipeId), Is.True);
            Assert.That(FusionAtlasService.MergeRunDiscoveriesIntoPermanent(runDiscovery, progress), Is.EqualTo(0));
        }

        [Test]
        public void RecipeIndex_FindsCraftRecipesAndMaterialUsage()
        {
            FusionRecipeIndex index = FusionAtlasService.RebuildIndex(LoadRecipes());

            IReadOnlyList<FusionRecipe> amazonRecipes = index.GetRecipesForResult("amazon");
            IReadOnlyList<FusionRecipe> paladinUses = index.GetRecipesUsingMaterial("paladin");

            Assert.That(index.RecipeCount, Is.EqualTo(70));
            Assert.That(amazonRecipes.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(paladinUses.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void RecipeIndex_TacticianCanReachEmperorFiveStarPath()
        {
            FusionRecipeIndex index = FusionAtlasService.RebuildIndex(LoadRecipes());
            IReadOnlyList<FusionAtlasPath> paths = index.GetReachableFiveStarPaths("tactician");

            bool foundEmperor = false;
            bool foundTwoStepPath = false;

            for (int i = 0; i < paths.Count; i++)
            {
                FusionAtlasPath path = paths[i];
                if (path.FinalResult != null && path.FinalResult.PieceId == "emperor")
                {
                    foundEmperor = true;
                    if (path.Steps.Count >= 2) foundTwoStepPath = true;
                }
            }

            Assert.That(foundEmperor, Is.True);
            Assert.That(foundTwoStepPath, Is.True);
        }

        [Test]
        public void FusionTree_All70RecipesSatisfyFullGrowthCoverage()
        {
            FusionTreeValidationReport report = FusionTreeValidator.Validate(LoadPieces(), LoadRecipes());

            Assert.That(report.PlayerPieceCount, Is.EqualTo(81));
            Assert.That(report.RecipeCount, Is.EqualTo(70));
            Assert.That(report.PublicRecipeCount, Is.EqualTo(63));
            Assert.That(report.HiddenRecipeCount, Is.EqualTo(7));
            Assert.That(report.ContentIssueCount, Is.EqualTo(0));
            Assert.That(report.InvalidReferenceRecipeIds, Is.Empty);
            Assert.That(report.MissingCraftRecipePieceIds, Is.Empty);
            Assert.That(report.MissingUpgradeMaterialPieceIds, Is.Empty);
            Assert.That(report.UnreachablePieceIds, Is.Empty);
            Assert.That(report.IsComplete, Is.True, report.BuildSummary());
        }

        [Test]
        public void AtlasVisibility_HiddenCraftAndPathStayMaskedUntilPermanentDiscovery()
        {
            FusionRecipeDatabase database = LoadRecipes();
            var progress = new MetaProgressState();

            IReadOnlyList<FusionRecipe> amazonBefore =
                FusionAtlasService.GetCraftRecipes(database, "amazon", progress);
            int beforeCount = amazonBefore.Count;

            FusionRecipe hiddenAmazon = null;
            IReadOnlyList<FusionRecipe> allAmazon =
                FusionAtlasService.GetCraftRecipes(database, "amazon", progress, revealUndiscoveredHidden: true);

            for (int i = 0; i < allAmazon.Count; i++)
            {
                if (allAmazon[i].IsHiddenRecipe)
                {
                    hiddenAmazon = allAmazon[i];
                    break;
                }
            }

            Assert.That(hiddenAmazon, Is.Not.Null);
            Assert.That(ContainsRecipe(amazonBefore, hiddenAmazon), Is.False);

            progress.TryDiscoverFusionRecipe(hiddenAmazon.RecipeId);

            IReadOnlyList<FusionRecipe> amazonAfter =
                FusionAtlasService.GetCraftRecipes(database, "amazon", progress);

            Assert.That(amazonAfter.Count, Is.EqualTo(beforeCount + 1));
            Assert.That(ContainsRecipe(amazonAfter, hiddenAmazon), Is.True);
        }

        private static PieceDatabase LoadPieces()
        {
            PieceDatabase database =
                AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            Assert.That(database, Is.Not.Null);
            return database;
        }

        private static FusionRecipeDatabase LoadRecipes()
        {
            FusionRecipeDatabase database =
                AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset");
            Assert.That(database, Is.Not.Null);
            return database;
        }

        private static IReadOnlyList<FusionRecipe> GetHiddenRecipes(FusionRecipeDatabase database)
        {
            var result = new List<FusionRecipe>();

            for (int i = 0; i < database.Recipes.Count; i++)
            {
                FusionRecipe recipe = database.Recipes[i];
                if (recipe != null && recipe.IsHiddenRecipe) result.Add(recipe);
            }

            return result;
        }

        private static bool ContainsRecipe(IReadOnlyList<FusionRecipe> recipes, FusionRecipe target)
        {
            if (recipes == null || target == null) return false;

            for (int i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] == target) return true;
            }

            return false;
        }
    }
}
#endif
