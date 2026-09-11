#if UNITY_EDITOR
using System.Collections.Generic; // IReadOnlyList<T> 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEditor; // 프로젝트 Fusion Recipe 에셋 로드
using ProjectEta.Fusion; // Fusion Recipe 콘텐츠 검증 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day72FusionContentTests
    {
        private const string DatabasePath = "Assets/ProjectEta/Data/FusionRecipeDatabase.asset"; // Fusion Recipe Database 경로

        [Test]
        public void FusionDatabase_등록레시피가유효하다()
        {
            FusionRecipeDatabase database = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(DatabasePath); // 프로젝트 Fusion Database 로드

            Assert.IsNotNull(database); // Fusion Database 존재 검증
            Assert.GreaterOrEqual(database.Recipes.Count, 4); // 현재 핵심 Recipe 등록 수 검증

            IReadOnlyList<FusionRecipeContentIssue> issues = FusionRecipeContentValidator.Validate(database); // 전체 Recipe 무결성 검증
            string firstIssue = issues.Count > 0 ? issues[0].ToString() : string.Empty; // 첫 문제 설명 생성
            Assert.AreEqual(0, issues.Count, firstIssue); // Recipe 누락·중복·규칙 위반 없음 검증
        }

        [Test]
        public void FusionDatabase_재료순서가바뀌어도같은레시피를찾는다()
        {
            FusionRecipeDatabase database = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(DatabasePath); // 프로젝트 Fusion Database 로드

            Assert.IsNotNull(database); // Fusion Database 존재 검증

            for (int i = 0; i < database.Recipes.Count; i++)
            {
                FusionRecipe recipe = database.Recipes[i]; // 현재 Recipe 조회
                Assert.IsNotNull(recipe); // 빈 Recipe 차단 검증
                bool found = database.TryFindRecipe(recipe.MaterialB, recipe.MaterialA, out FusionRecipe reversed); // 반대 재료 순서 조회
                Assert.IsTrue(found, recipe.RecipeId); // 반대 순서 Recipe 검색 성공 검증
                Assert.AreSame(recipe, reversed, recipe.RecipeId); // 동일 Recipe 반환 검증
            }
        }

        [Test]
        public void FusionDatabase_기획핵심레시피와숨김레시피를포함한다()
        {
            FusionRecipeDatabase database = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(DatabasePath); // 프로젝트 Fusion Database 로드

            Assert.IsNotNull(database); // Fusion Database 존재 검증
            Assert.IsTrue(database.TryFindRecipeById("archbishop_from_bishop_knight", out FusionRecipe archbishop)); // Bishop+Knight 핵심 Recipe 검색 검증
            Assert.IsNotNull(archbishop); // Archbishop Recipe 반환 검증
            Assert.IsTrue(database.TryFindRecipeById("chancellor_from_rook_knight", out FusionRecipe chancellor)); // Rook+Knight 핵심 Recipe 검색 검증
            Assert.IsNotNull(chancellor); // Chancellor Recipe 반환 검증
            Assert.IsTrue(database.TryFindRecipeById("amazon_from_queen_knight", out FusionRecipe amazon)); // Queen+Knight 핵심 Recipe 검색 검증
            Assert.IsNotNull(amazon); // Amazon Recipe 반환 검증
            Assert.IsTrue(ContainsHiddenRecipe(database)); // 숨김 Recipe 콘텐츠 존재 검증
        }

        [Test]
        public void HiddenRecipe_발견전에는숨고발견후에는공개된다()
        {
            FusionRecipeDatabase database = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(DatabasePath); // 프로젝트 Fusion Database 로드
            FusionRecipe hidden = FindHiddenRecipe(database); // 숨김 Recipe 조회
            var discovery = new FusionDiscoveryLog(); // 발견 기록 생성

            Assert.IsNotNull(database); // Fusion Database 존재 검증
            Assert.IsNotNull(hidden); // 숨김 Recipe 존재 검증

            IReadOnlyList<FusionRecipe> before = database.GetVisibleRecipes(discovery); // 발견 전 공개 Recipe 조회
            Assert.IsFalse(ContainsRecipe(before, hidden)); // 발견 전 숨김 Recipe 비공개 검증
            Assert.IsTrue(discovery.TryMarkDiscovered(hidden)); // 첫 발견 기록 성공 검증

            IReadOnlyList<FusionRecipe> after = database.GetVisibleRecipes(discovery); // 발견 후 공개 Recipe 조회
            Assert.IsTrue(ContainsRecipe(after, hidden)); // 발견 후 숨김 Recipe 공개 검증
        }

        [Test]
        public void FusionDatabase_결과기물기준으로복수레시피를찾을수있다()
        {
            FusionRecipeDatabase database = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>(DatabasePath); // 프로젝트 Fusion Database 로드

            Assert.IsNotNull(database); // Fusion Database 존재 검증
            Assert.IsTrue(database.TryFindRecipeById("amazon_from_queen_knight", out FusionRecipe amazonRecipe)); // Amazon 기본 Recipe 조회
            IReadOnlyList<FusionRecipe> recipes = database.GetRecipesForResult(amazonRecipe.Result); // Amazon 결과 Recipe 목록 조회

            Assert.GreaterOrEqual(recipes.Count, 2); // Amazon 복수 획득 Recipe 등록 검증
        }

        private static bool ContainsHiddenRecipe(FusionRecipeDatabase database)
        {
            return FindHiddenRecipe(database) != null; // 숨김 Recipe 존재 여부 반환
        }

        private static FusionRecipe FindHiddenRecipe(FusionRecipeDatabase database)
        {
            if (database == null) return null; // Database 누락 차단

            for (int i = 0; i < database.Recipes.Count; i++)
            {
                FusionRecipe recipe = database.Recipes[i]; // 현재 Recipe 조회
                if (recipe != null && recipe.IsHiddenRecipe) return recipe; // 첫 숨김 Recipe 반환
            }

            return null; // 숨김 Recipe 없음 반환
        }

        private static bool ContainsRecipe(IReadOnlyList<FusionRecipe> recipes, FusionRecipe target)
        {
            if (recipes == null || target == null) return false; // 비교 대상 누락 차단

            for (int i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] == target) return true; // 대상 Recipe 발견 반환
            }

            return false; // 대상 Recipe 미발견 반환
        }
    }
}
#endif
