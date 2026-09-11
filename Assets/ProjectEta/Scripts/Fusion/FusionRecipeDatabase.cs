using System.Collections.Generic; // List<T>와 IReadOnlyList<T> 사용
using UnityEngine; // ScriptableObject·SerializeField 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Fusion
{
    [CreateAssetMenu(fileName = "FusionRecipeDatabase", menuName = "ProjectEta/Fusion Recipe Database")]
    public class FusionRecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<FusionRecipe> _recipes = new List<FusionRecipe>(); // 등록 Fusion Recipe 목록

        public IReadOnlyList<FusionRecipe> Recipes => _recipes; // 등록 Recipe 전체 읽기 전용 노출

        public bool TryFindRecipe(PieceDefinition materialA, PieceDefinition materialB, out FusionRecipe recipe)
        {
            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i]; // 현재 Recipe 후보 조회
                if (candidate == null) continue; // 빈 Recipe 제외

                bool matchesInOrder = candidate.MaterialA == materialA && candidate.MaterialB == materialB; // 등록 순서 일치 여부 계산
                bool matchesReversed = candidate.MaterialA == materialB && candidate.MaterialB == materialA; // 반대 순서 일치 여부 계산

                if (matchesInOrder || matchesReversed)
                {
                    recipe = candidate; // 일치 Recipe 반환
                    return true; // Recipe 검색 성공 반환
                }
            }

            recipe = null; // 검색 실패 결과 초기화
            return false; // Recipe 검색 실패 반환
        }

        public bool TryFindRecipeById(string recipeId, out FusionRecipe recipe)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                recipe = null; // 빈 ID 검색 결과 초기화
                return false; // 빈 ID 검색 차단
            }

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i]; // 현재 Recipe 후보 조회
                if (candidate == null) continue; // 빈 Recipe 제외

                if (candidate.RecipeId == recipeId)
                {
                    recipe = candidate; // ID 일치 Recipe 반환
                    return true; // ID 검색 성공 반환
                }
            }

            recipe = null; // ID 검색 실패 결과 초기화
            return false; // ID 검색 실패 반환
        }

        public IReadOnlyList<FusionRecipe> GetVisibleRecipes(FusionDiscoveryLog discoveryLog)
        {
            var result = new List<FusionRecipe>(); // 현재 공개 Recipe 목록 생성

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i]; // 현재 Recipe 조회
                if (candidate == null) continue; // 빈 Recipe 제외

                if (!candidate.IsHiddenRecipe)
                {
                    result.Add(candidate); // 기본 공개 Recipe 등록
                    continue; // 다음 Recipe 검사
                }

                if (discoveryLog != null && discoveryLog.IsDiscovered(candidate))
                {
                    result.Add(candidate); // 발견한 숨김 Recipe 등록
                }
            }

            return result; // 현재 공개 Recipe 목록 반환
        }

        public IReadOnlyList<FusionRecipe> GetRecipesForResult(PieceDefinition resultDefinition)
        {
            var result = new List<FusionRecipe>(); // 결과 기물 기준 Recipe 목록 생성
            if (resultDefinition == null) return result; // 결과 기물 누락 빈 목록 반환

            for (int i = 0; i < _recipes.Count; i++)
            {
                FusionRecipe candidate = _recipes[i]; // 현재 Recipe 조회
                if (candidate != null && candidate.Result == resultDefinition) result.Add(candidate); // 결과 기물 일치 Recipe 등록
            }

            return result; // 결과 기물 기준 Recipe 목록 반환
        }
    }
}
