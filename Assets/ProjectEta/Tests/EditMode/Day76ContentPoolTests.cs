using System.Collections.Generic; // List<T> 사용
using System.Reflection; // 테스트용 직렬화 필드 주입
using NUnit.Framework; // NUnit 테스트 사용
using UnityEngine; // ScriptableObject·Resources 사용
using ProjectEta.Fusion; // FusionRecipe 사용
using ProjectEta.Pieces; // PieceDefinition·분류 사용
using ProjectEta.Run; // 콘텐츠 Pool 규칙·검증기 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day76ContentPoolTests
    {
        private readonly List<Object> _createdObjects = new List<Object>(); // 테스트 객체 정리 목록

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null) Object.DestroyImmediate(_createdObjects[i]); // 테스트 객체 제거
            }

            _createdObjects.Clear(); // 정리 목록 초기화
        }

        [Test]
        public void RewardPool_RejectsRestrictedPieces()
        {
            PieceDefinition normal = CreatePiece("normal_1", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn); // 일반 카드
            PieceDefinition king = CreatePiece("king_1", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.King); // King 카드
            PieceDefinition fusion = CreatePiece("fusion_1", PieceCategory.Fusion, PieceGrade.TwoStar, PieceMovementType.Queen); // Fusion 카드
            PieceDefinition monster = CreatePiece("monster_1", PieceCategory.Monster, PieceGrade.OneStar, PieceMovementType.Pawn); // Monster 카드
            PieceDefinition boss = CreatePiece("boss_1", PieceCategory.Boss, PieceGrade.FiveStar, PieceMovementType.Queen); // Boss 카드
            PieceDefinition fourStar = CreatePiece("four_1", PieceCategory.Basic, PieceGrade.FourStar, PieceMovementType.Rook); // 4성 카드
            PieceDefinition fiveStar = CreatePiece("five_1", PieceCategory.Special, PieceGrade.FiveStar, PieceMovementType.Bishop); // 5성 카드

            Assert.IsTrue(RunContentPoolRules.CanUseAsReward(normal)); // 일반 카드 허용
            Assert.IsFalse(RunContentPoolRules.CanUseAsReward(king)); // King 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsReward(fusion)); // Fusion 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsReward(monster)); // Monster 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsReward(boss)); // Boss 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsReward(fourStar)); // 4성 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsReward(fiveStar)); // 5성 제외
        }

        [Test]
        public void ShopPool_MatchesRewardEligibility()
        {
            PieceDefinition normal = CreatePiece("normal_shop", PieceCategory.Basic, PieceGrade.ThreeStar, PieceMovementType.Knight); // 상점 허용 카드
            PieceDefinition fusion = CreatePiece("fusion_shop", PieceCategory.Fusion, PieceGrade.TwoStar, PieceMovementType.Queen); // 상점 제외 카드

            Assert.AreEqual(RunContentPoolRules.CanUseAsReward(normal), RunContentPoolRules.CanUseInShop(normal)); // Reward·Shop 일치
            Assert.AreEqual(RunContentPoolRules.CanUseAsReward(fusion), RunContentPoolRules.CanUseInShop(fusion)); // 제한 카드 일치
        }

        [Test]
        public void EnemyPool_RejectsKingFusionBossAndAllowsMonsterSpecial()
        {
            PieceDefinition king = CreatePiece("enemy_king", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.King); // King 후보
            PieceDefinition fusion = CreatePiece("enemy_fusion", PieceCategory.Fusion, PieceGrade.TwoStar, PieceMovementType.Queen); // Fusion 후보
            PieceDefinition boss = CreatePiece("enemy_boss", PieceCategory.Boss, PieceGrade.FiveStar, PieceMovementType.Queen); // Boss 후보
            PieceDefinition monster = CreatePiece("enemy_monster", PieceCategory.Monster, PieceGrade.TwoStar, PieceMovementType.Pawn); // Monster 후보
            PieceDefinition special = CreatePiece("enemy_special", PieceCategory.Special, PieceGrade.ThreeStar, PieceMovementType.Knight); // Special 후보

            Assert.IsFalse(RunContentPoolRules.CanUseAsEnemy(king)); // King 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsEnemy(fusion)); // Fusion 제외
            Assert.IsFalse(RunContentPoolRules.CanUseAsEnemy(boss)); // Boss 제외
            Assert.IsTrue(RunContentPoolRules.CanUseAsEnemy(monster)); // Monster 허용
            Assert.IsTrue(RunContentPoolRules.CanUseAsEnemy(special)); // Special 허용
        }

        [Test]
        public void BossPool_AllowsBossCategoryOnly()
        {
            PieceDefinition boss = CreatePiece("boss_valid", PieceCategory.Boss, PieceGrade.FiveStar, PieceMovementType.Queen); // 정상 Boss
            PieceDefinition normal = CreatePiece("boss_invalid", PieceCategory.Basic, PieceGrade.ThreeStar, PieceMovementType.Queen); // 일반 기물

            Assert.IsTrue(RunContentPoolRules.CanUseAsBoss(boss)); // Boss 허용
            Assert.IsFalse(RunContentPoolRules.CanUseAsBoss(normal)); // 일반 기물 차단
        }

        [Test]
        public void Validator_DetectsEmptyAndDuplicatePieceIds()
        {
            PieceDefinition empty = CreatePiece(string.Empty, PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn); // 빈 ID
            PieceDefinition first = CreatePiece("duplicate_piece", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn); // 첫 중복
            PieceDefinition second = CreatePiece("duplicate_piece", PieceCategory.Special, PieceGrade.TwoStar, PieceMovementType.Knight); // 두 번째 중복
            var pieces = new List<PieceDefinition> { empty, first, second }; // 검증 대상

            IReadOnlyList<RunContentPoolIssue> issues = RunContentPoolValidator.ValidatePieces(pieces); // Piece Pool 검증

            Assert.IsTrue(ContainsIssue(issues, RunContentPoolIssueType.EmptyPieceId)); // 빈 ID 탐지
            Assert.IsTrue(ContainsIssue(issues, RunContentPoolIssueType.DuplicatePieceId)); // 중복 ID 탐지
        }

        [Test]
        public void Validator_DetectsFusionReferenceOutsidePiecePool()
        {
            PieceDefinition materialA = CreatePiece("fusion_material_a", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Bishop); // 등록 재료
            PieceDefinition missingMaterial = CreatePiece("fusion_material_missing", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Knight); // Pool 외 재료
            PieceDefinition result = CreatePiece("fusion_result", PieceCategory.Fusion, PieceGrade.TwoStar, PieceMovementType.Queen); // 등록 결과
            FusionRecipe recipe = CreateRecipe("day76_reference", materialA, missingMaterial, result); // 누락 참조 Recipe
            var pieces = new List<PieceDefinition> { materialA, result }; // 누락 재료 제외
            var recipes = new List<FusionRecipe> { recipe }; // Recipe 목록

            IReadOnlyList<RunContentPoolIssue> issues = RunContentPoolValidator.Validate(pieces, recipes, null); // 전체 참조 검증

            Assert.IsTrue(ContainsIssue(issues, RunContentPoolIssueType.FusionMaterialNotInPool)); // 누락 재료 탐지
        }

        [Test]
        public void ProjectResources_HaveUniqueNonEmptyPieceIdsAndValidBossResources()
        {
            PieceDefinition[] pieces = Resources.LoadAll<PieceDefinition>(string.Empty); // Resources Piece 전체 로드
            PieceDefinition midBoss = Resources.Load<PieceDefinition>("MidBoss74"); // 중간 보스 로드
            PieceDefinition finalBoss = Resources.Load<PieceDefinition>("FinalBoss74"); // 최종 보스 로드
            var bosses = new List<PieceDefinition> { midBoss, finalBoss }; // 필수 Boss 목록

            IReadOnlyList<RunContentPoolIssue> issues = RunContentPoolValidator.Validate(pieces, null, bosses); // 실제 리소스 검증

            Assert.IsFalse(ContainsIssue(issues, RunContentPoolIssueType.NullPiece)); // null Piece 없음
            Assert.IsFalse(ContainsIssue(issues, RunContentPoolIssueType.EmptyPieceId)); // 빈 ID 없음
            Assert.IsFalse(ContainsIssue(issues, RunContentPoolIssueType.DuplicatePieceId)); // 중복 ID 없음
            Assert.IsFalse(ContainsIssue(issues, RunContentPoolIssueType.InvalidBossPiece)); // Boss 분류 오류 없음
        }

        private PieceDefinition CreatePiece(string pieceId, PieceCategory category, PieceGrade grade, PieceMovementType movementType)
        {
            PieceDefinition piece = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 Piece 생성
            _createdObjects.Add(piece); // 정리 등록
            SetField(piece, "_pieceId", pieceId); // PieceId 주입
            SetField(piece, "_category", category); // Category 주입
            SetField(piece, "_grade", grade); // Grade 주입
            SetField(piece, "_movementType", movementType); // MovementType 주입
            return piece; // 테스트 Piece 반환
        }

        private FusionRecipe CreateRecipe(string recipeId, PieceDefinition materialA, PieceDefinition materialB, PieceDefinition result)
        {
            FusionRecipe recipe = ScriptableObject.CreateInstance<FusionRecipe>(); // 테스트 Recipe 생성
            _createdObjects.Add(recipe); // 정리 등록
            SetField(recipe, "_recipeId", recipeId); // RecipeId 주입
            SetField(recipe, "_materialA", materialA); // 재료 A 주입
            SetField(recipe, "_materialB", materialB); // 재료 B 주입
            SetField(recipe, "_result", result); // 결과 주입
            return recipe; // 테스트 Recipe 반환
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 직렬화 필드 조회
            Assert.NotNull(field, $"테스트 필드 '{fieldName}'를 찾지 못했습니다."); // 필드 구조 변경 감지
            field.SetValue(target, value); // 테스트 값 주입
        }

        private static bool ContainsIssue(IReadOnlyList<RunContentPoolIssue> issues, RunContentPoolIssueType issueType)
        {
            if (issues == null) return false; // 빈 결과 차단

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i] != null && issues[i].IssueType == issueType) return true; // Issue 발견
            }

            return false; // Issue 없음
        }
    }
}
