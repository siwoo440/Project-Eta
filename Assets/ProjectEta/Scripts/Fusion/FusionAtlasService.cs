using System.Collections.Generic; // IReadOnlyList·List 사용
using ProjectEta.Meta; // 영구 도감 상태 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Fusion
{
    public static class FusionAtlasService
    {
        private static FusionRecipeDatabase _cachedDatabase;
        private static FusionRecipeIndex _cachedIndex;

        public static FusionRecipeIndex GetIndex(FusionRecipeDatabase database)
        {
            if (database == null) return new FusionRecipeIndex(null);

            if (_cachedIndex == null || _cachedDatabase != database)
            {
                _cachedDatabase = database;
                _cachedIndex = new FusionRecipeIndex(database.Recipes);
            }

            return _cachedIndex;
        }

        public static FusionRecipeIndex RebuildIndex(FusionRecipeDatabase database)
        {
            _cachedDatabase = database;
            _cachedIndex = new FusionRecipeIndex(database != null ? database.Recipes : null);
            return _cachedIndex;
        }

        public static bool IsPermanentlyDiscovered(FusionRecipe recipe, MetaProgressState metaProgress)
        {
            if (recipe == null) return false;
            if (!recipe.IsHiddenRecipe) return true;
            return metaProgress != null && metaProgress.IsFusionRecipeDiscovered(recipe.RecipeId);
        }

        public static bool IsDiscovered(
            FusionRecipe recipe,
            FusionDiscoveryLog runDiscovery,
            MetaProgressState metaProgress)
        {
            if (recipe == null) return false;
            if (!recipe.IsHiddenRecipe) return true;
            if (runDiscovery != null && runDiscovery.IsDiscovered(recipe.RecipeId)) return true;
            return metaProgress != null && metaProgress.IsFusionRecipeDiscovered(recipe.RecipeId);
        }

        public static int MergeRunDiscoveriesIntoPermanent(
            FusionDiscoveryLog runDiscovery,
            MetaProgressState metaProgress)
        {
            if (runDiscovery == null || metaProgress == null) return 0;
            return metaProgress.MergeDiscoveredFusionRecipes(runDiscovery.DiscoveredRecipeIds);
        }

        public static IReadOnlyList<FusionRecipe> GetVisibleRecipes(
            FusionRecipeDatabase database,
            MetaProgressState metaProgress,
            bool revealUndiscoveredHidden = false)
        {
            var result = new List<FusionRecipe>();
            if (database == null) return result;

            for (int i = 0; i < database.Recipes.Count; i++)
            {
                FusionRecipe recipe = database.Recipes[i];
                if (recipe == null) continue;

                if (revealUndiscoveredHidden ||
                    !recipe.IsHiddenRecipe ||
                    IsPermanentlyDiscovered(recipe, metaProgress))
                {
                    result.Add(recipe);
                }
            }

            return result;
        }

        public static IReadOnlyList<FusionRecipe> GetCraftRecipes(
            FusionRecipeDatabase database,
            string resultPieceId,
            MetaProgressState metaProgress,
            bool revealUndiscoveredHidden = false)
        {
            FusionRecipeIndex index = GetIndex(database);
            return FilterVisible(index.GetRecipesForResult(resultPieceId), metaProgress, revealUndiscoveredHidden);
        }

        public static IReadOnlyList<FusionRecipe> GetRecipesUsingMaterial(
            FusionRecipeDatabase database,
            string materialPieceId,
            MetaProgressState metaProgress,
            bool revealUndiscoveredHidden = false)
        {
            FusionRecipeIndex index = GetIndex(database);
            return FilterVisible(index.GetRecipesUsingMaterial(materialPieceId), metaProgress, revealUndiscoveredHidden);
        }

        public static IReadOnlyList<FusionAtlasPath> GetReachableFiveStarPaths(
            FusionRecipeDatabase database,
            string startPieceId,
            MetaProgressState metaProgress,
            bool revealUndiscoveredHidden = false)
        {
            FusionRecipeIndex index = GetIndex(database);

            return index.GetReachableFiveStarPaths(
                startPieceId,
                recipe =>
                    revealUndiscoveredHidden ||
                    recipe == null ||
                    !recipe.IsHiddenRecipe ||
                    IsPermanentlyDiscovered(recipe, metaProgress));
        }

        public static IReadOnlyList<PieceDefinition> GetReachableFiveStarResults(
            FusionRecipeDatabase database,
            string startPieceId,
            MetaProgressState metaProgress,
            bool revealUndiscoveredHidden = false)
        {
            FusionRecipeIndex index = GetIndex(database);

            return index.GetReachableFiveStarResults(
                startPieceId,
                recipe =>
                    revealUndiscoveredHidden ||
                    recipe == null ||
                    !recipe.IsHiddenRecipe ||
                    IsPermanentlyDiscovered(recipe, metaProgress));
        }

        private static IReadOnlyList<FusionRecipe> FilterVisible(
            IReadOnlyList<FusionRecipe> source,
            MetaProgressState metaProgress,
            bool revealUndiscoveredHidden)
        {
            var result = new List<FusionRecipe>();
            if (source == null) return result;

            for (int i = 0; i < source.Count; i++)
            {
                FusionRecipe recipe = source[i];
                if (recipe == null) continue;

                if (revealUndiscoveredHidden ||
                    !recipe.IsHiddenRecipe ||
                    IsPermanentlyDiscovered(recipe, metaProgress))
                {
                    result.Add(recipe);
                }
            }

            return result;
        }
    }
}
