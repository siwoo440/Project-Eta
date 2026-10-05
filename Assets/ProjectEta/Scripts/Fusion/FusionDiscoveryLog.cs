using System.Collections.Generic; // HashSet<T>·IEnumerable<T> 사용
using UnityEngine; // Application.isPlaying 사용
using ProjectEta.Meta; // 영구 Fusion Atlas 진행 사용

namespace ProjectEta.Fusion
{
    public class FusionDiscoveryLog
    {
        private readonly HashSet<string> _discoveredRecipeIds = new HashSet<string>(); // 현재 런 발견 ID

        public IEnumerable<string> DiscoveredRecipeIds => _discoveredRecipeIds;
        public int DiscoveredCount => _discoveredRecipeIds.Count;

        public bool IsDiscovered(FusionRecipe recipe)
        {
            if (recipe == null) return false;
            if (!recipe.IsHiddenRecipe) return true;
            if (_discoveredRecipeIds.Contains(recipe.RecipeId)) return true;

            MetaProgressState permanent = GetRuntimeMetaProgress();
            return permanent != null && permanent.IsFusionRecipeDiscovered(recipe.RecipeId);
        }

        public bool IsDiscovered(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return false;
            if (_discoveredRecipeIds.Contains(recipeId)) return true;

            MetaProgressState permanent = GetRuntimeMetaProgress();
            return permanent != null && permanent.IsFusionRecipeDiscovered(recipeId);
        }

        public bool TryMarkDiscovered(FusionRecipe recipe)
        {
            if (recipe == null || !recipe.IsHiddenRecipe) return false;

            string recipeId = recipe.RecipeId;
            MetaProgressState permanent = GetRuntimeMetaProgress();

            if (permanent != null && permanent.IsFusionRecipeDiscovered(recipeId))
            {
                _discoveredRecipeIds.Add(recipeId); // 새 런에도 현재 발견 상태 동기화
                return false; // 영구 도감에서 이미 발견했으므로 새 발견 알림은 발생시키지 않음
            }

            bool runAdded = _discoveredRecipeIds.Add(recipeId);

            if (permanent == null)
            {
                return runAdded; // EditMode 테스트·비플레이 상태는 기존 런 단위 동작 유지
            }

            bool permanentAdded = permanent.TryDiscoverFusionRecipe(recipeId);

            if (permanentAdded)
            {
                MetaProgressService.Save(); // 실제 플레이 중 최초 발견은 즉시 영구 저장
            }

            return permanentAdded; // 플레이 중 발견 이벤트는 영구 최초 발견일 때만 true
        }

        public int Merge(IEnumerable<string> recipeIds)
        {
            if (recipeIds == null) return 0;
            int added = 0;

            foreach (string recipeId in recipeIds)
            {
                if (string.IsNullOrWhiteSpace(recipeId)) continue;
                if (_discoveredRecipeIds.Add(recipeId)) added++;
            }

            return added;
        }

        public void Restore(IEnumerable<string> recipeIds)
        {
            _discoveredRecipeIds.Clear();
            Merge(recipeIds);

            MetaProgressState permanent = GetRuntimeMetaProgress();
            if (permanent == null) return;

            int migrated = permanent.MergeDiscoveredFusionRecipes(_discoveredRecipeIds);

            if (migrated > 0)
            {
                MetaProgressService.Save(); // 구버전 RunSave의 발견 기록을 영구 Fusion Atlas로 승격
            }
        }

        private static MetaProgressState GetRuntimeMetaProgress()
        {
            if (!Application.isPlaying) return null; // EditMode 테스트가 실제 사용자 메타 저장을 오염시키지 않도록 차단
            return MetaProgressService.Current;
        }
    }
}
