using System.Collections.Generic; // List·IReadOnlyList 사용
using UnityEngine; // ScriptableObject·SerializeField 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Fusion
{
    [CreateAssetMenu(fileName = "FusionRecipeDatabase", menuName = "ProjectEta/Fusion Recipe Database")]
    public class FusionRecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<FusionRecipe> _recipes = new List<FusionRecipe>(); // 등록 레시피 목록

        public IReadOnlyList<FusionRecipe> Recipes => _recipes;

        public bool TryFindRecipe(PieceDefinition materialA, PieceDefinition materialB, out FusionRecipe recipe)
        {
            recipe = null;
            if (materialA == null || materialB == null) return false;

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i];
                if (candidate == null || !candidate.UsesOrderedMaterials) continue;

                if (candidate.MaterialA == materialA && candidate.MaterialB == materialB)
                {
                    recipe = candidate; // 방향성 레시피는 A/B 순서까지 일치해야 함
                    return true;
                }
            }

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i];
                if (candidate == null || candidate.UsesOrderedMaterials) continue;

                bool matchesInOrder = candidate.MaterialA == materialA && candidate.MaterialB == materialB;
                bool matchesReversed = candidate.MaterialA == materialB && candidate.MaterialB == materialA;

                if (matchesInOrder || matchesReversed)
                {
                    recipe = candidate; // 일반 레시피는 기존처럼 순서 무관
                    return true;
                }
            }

            return false;
        }

        public bool TryFindRecipeById(string recipeId, out FusionRecipe recipe)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                recipe = null;
                return false;
            }

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i];
                if (candidate == null) continue;

                if (candidate.RecipeId == recipeId)
                {
                    recipe = candidate;
                    return true;
                }
            }

            recipe = null;
            return false;
        }

        public IReadOnlyList<FusionRecipe> GetVisibleRecipes(FusionDiscoveryLog discoveryLog)
        {
            var result = new List<FusionRecipe>();

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i];
                if (candidate == null) continue;

                if (!candidate.IsHiddenRecipe)
                {
                    result.Add(candidate);
                    continue;
                }

                if (discoveryLog != null && discoveryLog.IsDiscovered(candidate))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        public IReadOnlyList<FusionRecipe> GetRecipesForResult(PieceDefinition resultDefinition)
        {
            var result = new List<FusionRecipe>();
            if (resultDefinition == null) return result;

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i];
                if (candidate != null && candidate.Result == resultDefinition) result.Add(candidate);
            }

            return result;
        }
    }
}
