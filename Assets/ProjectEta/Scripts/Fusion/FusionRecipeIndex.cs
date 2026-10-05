using System; // Array·Predicate 사용
using System.Collections.Generic; // Dictionary·List·HashSet 사용
using ProjectEta.Pieces; // PieceDefinition·PieceGrade 사용

namespace ProjectEta.Fusion
{
    public sealed class FusionAtlasPath
    {
        private readonly List<FusionRecipe> _steps;

        public string StartPieceId { get; }
        public IReadOnlyList<FusionRecipe> Steps => _steps;
        public PieceDefinition FinalResult => _steps.Count > 0 ? _steps[_steps.Count - 1].Result : null;

        internal FusionAtlasPath(string startPieceId, IReadOnlyList<FusionRecipe> steps)
        {
            StartPieceId = startPieceId ?? string.Empty;
            _steps = steps != null ? new List<FusionRecipe>(steps) : new List<FusionRecipe>();
        }
    }

    public sealed class FusionRecipeIndex
    {
        private readonly Dictionary<string, FusionRecipe> _byRecipeId =
            new Dictionary<string, FusionRecipe>(StringComparer.Ordinal);

        private readonly Dictionary<string, List<FusionRecipe>> _byResultPieceId =
            new Dictionary<string, List<FusionRecipe>>(StringComparer.Ordinal);

        private readonly Dictionary<string, List<FusionRecipe>> _byMaterialPieceId =
            new Dictionary<string, List<FusionRecipe>>(StringComparer.Ordinal);

        public int RecipeCount { get; }

        public FusionRecipeIndex(IReadOnlyList<FusionRecipe> recipes)
        {
            if (recipes == null)
            {
                RecipeCount = 0;
                return;
            }

            int count = 0;

            for (int i = 0; i < recipes.Count; i++)
            {
                FusionRecipe recipe = recipes[i];
                if (recipe == null) continue;

                count++;

                if (!string.IsNullOrWhiteSpace(recipe.RecipeId) && !_byRecipeId.ContainsKey(recipe.RecipeId))
                {
                    _byRecipeId.Add(recipe.RecipeId, recipe);
                }

                if (recipe.Result != null && !string.IsNullOrWhiteSpace(recipe.Result.PieceId))
                {
                    AddToIndex(_byResultPieceId, recipe.Result.PieceId, recipe);
                }

                if (recipe.MaterialA != null && !string.IsNullOrWhiteSpace(recipe.MaterialA.PieceId))
                {
                    AddToIndex(_byMaterialPieceId, recipe.MaterialA.PieceId, recipe);
                }

                if (recipe.MaterialB != null &&
                    !string.IsNullOrWhiteSpace(recipe.MaterialB.PieceId) &&
                    (recipe.MaterialA == null || !string.Equals(recipe.MaterialA.PieceId, recipe.MaterialB.PieceId, StringComparison.Ordinal)))
                {
                    AddToIndex(_byMaterialPieceId, recipe.MaterialB.PieceId, recipe);
                }
            }

            RecipeCount = count;
        }

        public FusionRecipe FindById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return null;
            _byRecipeId.TryGetValue(recipeId, out FusionRecipe recipe);
            return recipe;
        }

        public IReadOnlyList<FusionRecipe> GetRecipesForResult(string pieceId)
        {
            return GetIndexedList(_byResultPieceId, pieceId);
        }

        public IReadOnlyList<FusionRecipe> GetRecipesForResult(PieceDefinition definition)
        {
            return definition != null ? GetRecipesForResult(definition.PieceId) : Array.Empty<FusionRecipe>();
        }

        public IReadOnlyList<FusionRecipe> GetRecipesUsingMaterial(string pieceId)
        {
            return GetIndexedList(_byMaterialPieceId, pieceId);
        }

        public IReadOnlyList<FusionRecipe> GetRecipesUsingMaterial(PieceDefinition definition)
        {
            return definition != null ? GetRecipesUsingMaterial(definition.PieceId) : Array.Empty<FusionRecipe>();
        }

        public IReadOnlyList<FusionAtlasPath> GetReachableFiveStarPaths(
            string startPieceId,
            Predicate<FusionRecipe> includeRecipe = null)
        {
            var results = new List<FusionAtlasPath>();
            if (string.IsNullOrWhiteSpace(startPieceId)) return results;

            var path = new List<FusionRecipe>();
            var branchPieceIds = new HashSet<string>(StringComparer.Ordinal) { startPieceId };

            ExploreFiveStarPaths(startPieceId, startPieceId, path, branchPieceIds, includeRecipe, results);
            return results;
        }

        public IReadOnlyList<PieceDefinition> GetReachableFiveStarResults(
            string startPieceId,
            Predicate<FusionRecipe> includeRecipe = null)
        {
            IReadOnlyList<FusionAtlasPath> paths = GetReachableFiveStarPaths(startPieceId, includeRecipe);
            var results = new List<PieceDefinition>();
            var resultIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < paths.Count; i++)
            {
                PieceDefinition result = paths[i].FinalResult;
                if (result == null || string.IsNullOrWhiteSpace(result.PieceId)) continue;
                if (!resultIds.Add(result.PieceId)) continue;
                results.Add(result);
            }

            return results;
        }

        private void ExploreFiveStarPaths(
            string startPieceId,
            string currentPieceId,
            List<FusionRecipe> currentPath,
            HashSet<string> branchPieceIds,
            Predicate<FusionRecipe> includeRecipe,
            List<FusionAtlasPath> results)
        {
            IReadOnlyList<FusionRecipe> nextRecipes = GetRecipesUsingMaterial(currentPieceId);

            for (int i = 0; i < nextRecipes.Count; i++)
            {
                FusionRecipe recipe = nextRecipes[i];
                if (recipe == null || recipe.Result == null) continue;
                if (includeRecipe != null && !includeRecipe(recipe)) continue;

                string resultId = recipe.Result.PieceId;
                if (string.IsNullOrWhiteSpace(resultId) || branchPieceIds.Contains(resultId)) continue;

                currentPath.Add(recipe);

                if (recipe.Result.Grade == PieceGrade.FiveStar)
                {
                    results.Add(new FusionAtlasPath(startPieceId, currentPath));
                }
                else if ((int)recipe.Result.Grade < (int)PieceGrade.FiveStar)
                {
                    branchPieceIds.Add(resultId);
                    ExploreFiveStarPaths(startPieceId, resultId, currentPath, branchPieceIds, includeRecipe, results);
                    branchPieceIds.Remove(resultId);
                }

                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }

        private static IReadOnlyList<FusionRecipe> GetIndexedList(
            Dictionary<string, List<FusionRecipe>> index,
            string pieceId)
        {
            if (string.IsNullOrWhiteSpace(pieceId)) return Array.Empty<FusionRecipe>();
            return index.TryGetValue(pieceId, out List<FusionRecipe> recipes)
                ? recipes
                : (IReadOnlyList<FusionRecipe>)Array.Empty<FusionRecipe>();
        }

        private static void AddToIndex(
            Dictionary<string, List<FusionRecipe>> index,
            string pieceId,
            FusionRecipe recipe)
        {
            if (!index.TryGetValue(pieceId, out List<FusionRecipe> list))
            {
                list = new List<FusionRecipe>();
                index.Add(pieceId, list);
            }

            list.Add(recipe);
        }
    }
}
