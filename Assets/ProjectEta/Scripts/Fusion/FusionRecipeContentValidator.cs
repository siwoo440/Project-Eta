using System.Collections.Generic; // IReadOnlyList·List·HashSet 사용

namespace ProjectEta.Fusion
{
    public enum FusionRecipeContentIssueType
    {
        NullRecipe = 0,
        DuplicateRecipeId = 1,
        MissingMaterial = 2,
        MissingResult = 3,
        RuleViolation = 4,
        DuplicateMaterialPair = 5
    }

    public sealed class FusionRecipeContentIssue
    {
        public FusionRecipeContentIssueType IssueType { get; }
        public string RecipeId { get; }
        public FusionBlockReason BlockReason { get; }

        public FusionRecipeContentIssue(FusionRecipeContentIssueType issueType, string recipeId, FusionBlockReason blockReason)
        {
            IssueType = issueType;
            RecipeId = recipeId ?? string.Empty;
            BlockReason = blockReason;
        }

        public override string ToString()
        {
            return $"{IssueType} / {RecipeId} / {BlockReason}";
        }
    }

    public static class FusionRecipeContentValidator
    {
        public static IReadOnlyList<FusionRecipeContentIssue> Validate(FusionRecipeDatabase database)
        {
            return Validate(database != null ? database.Recipes : null);
        }

        public static IReadOnlyList<FusionRecipeContentIssue> Validate(IReadOnlyList<FusionRecipe> recipes)
        {
            var issues = new List<FusionRecipeContentIssue>();
            if (recipes == null) return issues;

            var recipeIds = new HashSet<string>();
            var materialPairs = new HashSet<string>();

            for (int i = 0; i < recipes.Count; i++)
            {
                FusionRecipe recipe = recipes[i];

                if (recipe == null)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.NullRecipe, $"index_{i}", FusionBlockReason.NoRecipe));
                    continue;
                }

                string recipeId = recipe.RecipeId;
                if (!recipeIds.Add(recipeId))
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.DuplicateRecipeId, recipeId, FusionBlockReason.None));
                }

                if (recipe.MaterialA == null || recipe.MaterialB == null)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.MissingMaterial, recipeId, FusionBlockReason.MaterialNotFusable));
                    continue;
                }

                if (recipe.Result == null)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.MissingResult, recipeId, FusionBlockReason.NoRecipe));
                    continue;
                }

                string materialPairKey = CreateMaterialPairKey(recipe);
                if (!materialPairs.Add(materialPairKey))
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.DuplicateMaterialPair, recipeId, FusionBlockReason.None));
                }

                FusionBlockReason blockReason = FusionRuleValidator.ValidateRecipe(recipe);
                if (blockReason != FusionBlockReason.None)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.RuleViolation, recipeId, blockReason));
                }
            }

            return issues;
        }

        private static string CreateMaterialPairKey(FusionRecipe recipe)
        {
            string first = string.IsNullOrEmpty(recipe.MaterialA.PieceId) ? recipe.MaterialA.name : recipe.MaterialA.PieceId;
            string second = string.IsNullOrEmpty(recipe.MaterialB.PieceId) ? recipe.MaterialB.name : recipe.MaterialB.PieceId;

            if (recipe.UsesOrderedMaterials)
            {
                return $"ordered|{first}>{second}"; // A/B 순서를 별도 조합으로 취급
            }

            return string.CompareOrdinal(first, second) <= 0
                ? $"unordered|{first}|{second}"
                : $"unordered|{second}|{first}";
        }
    }
}
