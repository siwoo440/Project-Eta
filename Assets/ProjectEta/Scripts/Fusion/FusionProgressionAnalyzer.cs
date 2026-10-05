using System.Collections.Generic; // IReadOnlyList 사용
using ProjectEta.Pieces; // 기물 등급·정의 사용
using ProjectEta.Run; // 콘텐츠 DB 참조 검증 사용

namespace ProjectEta.Fusion
{
    public sealed class FusionProgressionReport
    {
        private const int MaximumGrade = (int)PieceGrade.FiveStar;
        private readonly int[] _pieceCounts;
        private readonly int[] _recipeCounts;

        public static FusionProgressionReport Empty { get; } =
            new FusionProgressionReport(new int[MaximumGrade + 1], new int[MaximumGrade + 1], 0, false, PieceGrade.TwoStar);

        public int ContentIssueCount { get; }
        public bool AreDatabasesConnected { get; }
        public PieceGrade? FirstUnreachableGrade { get; }
        public PieceGrade? FirstMissingPieceGrade => FindFirstMissingGrade(_pieceCounts, PieceGrade.OneStar);
        public PieceGrade? FirstMissingRecipeGrade => FindFirstMissingGrade(_recipeCounts, PieceGrade.TwoStar);
        public bool HasCompleteGradeCoverage =>
            AreDatabasesConnected &&
            !FirstMissingPieceGrade.HasValue &&
            !FirstMissingRecipeGrade.HasValue &&
            !FirstUnreachableGrade.HasValue &&
            ContentIssueCount == 0;

        internal FusionProgressionReport(
            int[] pieceCounts,
            int[] recipeCounts,
            int contentIssueCount,
            bool areDatabasesConnected,
            PieceGrade? firstUnreachableGrade)
        {
            _pieceCounts = pieceCounts ?? new int[MaximumGrade + 1];
            _recipeCounts = recipeCounts ?? new int[MaximumGrade + 1];
            ContentIssueCount = contentIssueCount < 0 ? 0 : contentIssueCount;
            AreDatabasesConnected = areDatabasesConnected;
            FirstUnreachableGrade = firstUnreachableGrade;
        }

        public int GetPieceCount(PieceGrade grade)
        {
            int index = (int)grade;
            return IsValidGradeIndex(index) ? _pieceCounts[index] : 0;
        }

        public int GetRecipeCount(PieceGrade resultGrade)
        {
            int index = (int)resultGrade;
            return IsValidGradeIndex(index) ? _recipeCounts[index] : 0;
        }

        public string BuildGradeSummary()
        {
            return $"기물 1★{GetPieceCount(PieceGrade.OneStar)} / 2★{GetPieceCount(PieceGrade.TwoStar)} / 3★{GetPieceCount(PieceGrade.ThreeStar)} / 4★{GetPieceCount(PieceGrade.FourStar)} / 5★{GetPieceCount(PieceGrade.FiveStar)} · 레시피 2★{GetRecipeCount(PieceGrade.TwoStar)} / 3★{GetRecipeCount(PieceGrade.ThreeStar)} / 4★{GetRecipeCount(PieceGrade.FourStar)} / 5★{GetRecipeCount(PieceGrade.FiveStar)}";
        }

        private static PieceGrade? FindFirstMissingGrade(int[] counts, PieceGrade startGrade)
        {
            if (counts == null) return startGrade;

            for (int grade = (int)startGrade; grade <= MaximumGrade; grade++)
            {
                if (grade >= counts.Length || counts[grade] <= 0) return (PieceGrade)grade;
            }

            return null;
        }

        private static bool IsValidGradeIndex(int index)
        {
            return index >= (int)PieceGrade.OneStar && index <= MaximumGrade;
        }
    }

    public static class FusionProgressionAnalyzer
    {
        private const int MaximumGrade = (int)PieceGrade.FiveStar;

        public static FusionProgressionReport Analyze(PieceDatabase pieceDatabase, FusionRecipeDatabase recipeDatabase)
        {
            IReadOnlyList<PieceDefinition> definitions = pieceDatabase != null ? pieceDatabase.Definitions : null;
            IReadOnlyList<FusionRecipe> recipes = recipeDatabase != null ? recipeDatabase.Recipes : null;
            return AnalyzeInternal(definitions, recipes, pieceDatabase != null, recipeDatabase != null);
        }

        public static FusionProgressionReport Analyze(IReadOnlyList<PieceDefinition> definitions, IReadOnlyList<FusionRecipe> recipes)
        {
            return AnalyzeInternal(definitions, recipes, definitions != null, recipes != null);
        }

        private static FusionProgressionReport AnalyzeInternal(
            IReadOnlyList<PieceDefinition> definitions,
            IReadOnlyList<FusionRecipe> recipes,
            bool hasPieceDatabase,
            bool hasRecipeDatabase)
        {
            int[] pieceCounts = new int[MaximumGrade + 1];
            int[] recipeCounts = new int[MaximumGrade + 1];
            int contentIssueCount = (hasPieceDatabase ? 0 : 1) + (hasRecipeDatabase ? 0 : 1);
            var validPieceIds = new HashSet<string>();
            var reachablePieceIds = new HashSet<string>();
            var acceptedRecipes = new List<FusionRecipe>();

            if (definitions != null)
            {
                for (int index = 0; index < definitions.Count; index++)
                {
                    PieceDefinition definition = definitions[index];
                    if (!IsPlayerPiece(definition)) continue;

                    int grade = (int)definition.Grade;
                    if (grade >= (int)PieceGrade.OneStar && grade <= MaximumGrade) pieceCounts[grade]++;
                    if (!string.IsNullOrWhiteSpace(definition.PieceId)) validPieceIds.Add(definition.PieceId);

                    if (definition.Grade == PieceGrade.OneStar && !string.IsNullOrWhiteSpace(definition.PieceId))
                    {
                        reachablePieceIds.Add(definition.PieceId);
                    }
                }
            }

            if (hasPieceDatabase && hasRecipeDatabase)
            {
                contentIssueCount += RunContentPoolValidator.Validate(definitions, recipes, null).Count;
            }

            if (recipes != null && hasPieceDatabase)
            {
                var recipeIds = new HashSet<string>();
                var materialPairs = new HashSet<string>();

                for (int index = 0; index < recipes.Count; index++)
                {
                    FusionRecipe recipe = recipes[index];
                    if (FusionRuleValidator.ValidateRecipe(recipe) != FusionBlockReason.None) continue;
                    if (!ReferencesRegisteredPieces(recipe, validPieceIds)) continue;
                    if (!IsPlayerPiece(recipe.Result)) continue;
                    if (!recipeIds.Add(recipe.RecipeId)) continue;
                    if (!materialPairs.Add(CreateMaterialPairKey(recipe))) continue;

                    int resultGrade = (int)recipe.Result.Grade;
                    if (resultGrade >= (int)PieceGrade.TwoStar && resultGrade <= MaximumGrade)
                    {
                        recipeCounts[resultGrade]++;
                    }

                    acceptedRecipes.Add(recipe);
                }
            }

            ExpandReachablePieces(acceptedRecipes, reachablePieceIds);
            PieceGrade? firstUnreachableGrade = FindFirstUnreachableGrade(definitions, reachablePieceIds);
            return new FusionProgressionReport(
                pieceCounts,
                recipeCounts,
                contentIssueCount,
                hasPieceDatabase && hasRecipeDatabase,
                firstUnreachableGrade);
        }

        private static bool ReferencesRegisteredPieces(FusionRecipe recipe, HashSet<string> validPieceIds)
        {
            if (recipe == null || validPieceIds == null) return false;
            if (recipe.MaterialA == null || recipe.MaterialB == null || recipe.Result == null) return false;

            return validPieceIds.Contains(recipe.MaterialA.PieceId) &&
                   validPieceIds.Contains(recipe.MaterialB.PieceId) &&
                   validPieceIds.Contains(recipe.Result.PieceId);
        }

        private static string CreateMaterialPairKey(FusionRecipe recipe)
        {
            string first = recipe.MaterialA.PieceId;
            string second = recipe.MaterialB.PieceId;

            if (recipe.UsesOrderedMaterials) return $"ordered|{first}>{second}";

            return string.CompareOrdinal(first, second) <= 0
                ? $"unordered|{first}|{second}"
                : $"unordered|{second}|{first}";
        }

        private static void ExpandReachablePieces(IReadOnlyList<FusionRecipe> recipes, HashSet<string> reachablePieceIds)
        {
            if (recipes == null || reachablePieceIds == null) return;

            bool changed;
            do
            {
                changed = false;

                for (int index = 0; index < recipes.Count; index++)
                {
                    FusionRecipe recipe = recipes[index];
                    if (!reachablePieceIds.Contains(recipe.MaterialA.PieceId) ||
                        !reachablePieceIds.Contains(recipe.MaterialB.PieceId)) continue;

                    if (reachablePieceIds.Add(recipe.Result.PieceId)) changed = true;
                }
            }
            while (changed);
        }

        private static PieceGrade? FindFirstUnreachableGrade(
            IReadOnlyList<PieceDefinition> definitions,
            HashSet<string> reachablePieceIds)
        {
            for (int grade = (int)PieceGrade.TwoStar; grade <= MaximumGrade; grade++)
            {
                bool hasReachablePiece = false;

                if (definitions != null)
                {
                    for (int index = 0; index < definitions.Count; index++)
                    {
                        PieceDefinition definition = definitions[index];
                        if (!IsPlayerPiece(definition) || (int)definition.Grade != grade) continue;
                        if (!reachablePieceIds.Contains(definition.PieceId)) continue;

                        hasReachablePiece = true;
                        break;
                    }
                }

                if (!hasReachablePiece) return (PieceGrade)grade;
            }

            return null;
        }

        private static bool IsPlayerPiece(PieceDefinition definition)
        {
            if (definition == null) return false;
            return definition.Category != PieceCategory.Monster && definition.Category != PieceCategory.Boss;
        }
    }
}
