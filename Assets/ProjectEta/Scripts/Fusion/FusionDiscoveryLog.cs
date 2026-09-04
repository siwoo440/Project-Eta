using System.Collections.Generic; // HashSet<T>, IEnumerable<T>를 사용하기 위한 네임스페이스

namespace ProjectEta.Fusion // 합성 관련 타입을 모아두는 네임스페이스
{
    public class FusionDiscoveryLog // 22일차: 숨김 레시피를 실제로 성공시킨 기록을 런 단위로 보관하는 클래스
    {
        private readonly HashSet<string> _discoveredRecipeIds = new HashSet<string>(); // 이미 발견한 레시피 식별자 집합

        public IEnumerable<string> DiscoveredRecipeIds => _discoveredRecipeIds; // 저장 직렬화 등에서 읽는 발견 목록
        public int DiscoveredCount => _discoveredRecipeIds.Count; // 지금까지 발견한 숨김 레시피 수

        public bool IsDiscovered(FusionRecipe recipe) // 해당 레시피를 이미 발견했는지 확인하는 메서드
        {
            if (recipe == null) return false; // 레시피가 없으면 발견하지 않은 것으로 처리
            if (!recipe.IsHiddenRecipe) return true; // 숨김 레시피가 아니면 항상 공개 상태로 취급
            return _discoveredRecipeIds.Contains(recipe.RecipeId); // 숨김 레시피는 발견 기록이 있어야 공개
        }

        public bool TryMarkDiscovered(FusionRecipe recipe) // 합성 성공 시 숨김 레시피를 새로 발견 처리하는 메서드
        {
            if (recipe == null || !recipe.IsHiddenRecipe) return false; // 숨김 레시피가 아니면 기록하지 않음
            return _discoveredRecipeIds.Add(recipe.RecipeId); // 처음 추가된 경우에만 true(= 이번에 새로 발견)
        }

        public void Restore(IEnumerable<string> recipeIds) // 저장 파일에서 발견 목록을 복원하는 메서드
        {
            _discoveredRecipeIds.Clear(); // 이전 기록을 모두 비움
            if (recipeIds == null) return; // 복원할 목록이 없으면 빈 상태로 종료

            foreach (var recipeId in recipeIds) // 저장된 식별자를 순회하며
            {
                if (!string.IsNullOrEmpty(recipeId)) _discoveredRecipeIds.Add(recipeId); // 유효한 식별자만 복원
            }
        }
    }
}
