using System.Collections.Generic; // List<T>·HashSet<T>·IReadOnlyList<T> 사용
using ProjectEta.Fusion; // FusionRecipe·기존 Recipe 검증기 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public enum RunContentPoolIssueType
    {
        NullPiece = 0, // null PieceDefinition
        EmptyPieceId = 1, // 비어 있는 PieceId
        DuplicatePieceId = 2, // 중복 PieceId
        InvalidBossPiece = 3, // 필수 Boss 데이터 분류 오류
        FusionRecipeIssue = 4, // 기존 Fusion Recipe 규칙 오류
        FusionMaterialNotInPool = 5, // Fusion 재료가 전체 Piece Pool에 없음
        FusionResultNotInPool = 6 // Fusion 결과가 전체 Piece Pool에 없음
    }

    public sealed class RunContentPoolIssue
    {
        public RunContentPoolIssueType IssueType { get; } // 문제 종류
        public string ContentId { get; } // 관련 Piece·Recipe ID
        public string Details { get; } // 추가 진단 정보

        public RunContentPoolIssue(RunContentPoolIssueType issueType, string contentId, string details)
        {
            IssueType = issueType; // 문제 종류 저장
            ContentId = contentId ?? string.Empty; // 관련 콘텐츠 ID 저장
            Details = details ?? string.Empty; // 진단 정보 저장
        }

        public override string ToString()
        {
            return $"{IssueType} / {ContentId} / {Details}"; // 테스트·로그용 문제 설명 반환
        }
    }

    public static class RunContentPoolValidator
    {
        public static IReadOnlyList<RunContentPoolIssue> ValidatePieces(IReadOnlyList<PieceDefinition> pieces)
        {
            var issues = new List<RunContentPoolIssue>(); // Piece 검증 결과 생성
            ValidatePiecesInternal(pieces, issues, out _); // Piece ID 무결성 검사
            return issues; // Piece 검증 결과 반환
        }

        public static IReadOnlyList<RunContentPoolIssue> Validate(
            IReadOnlyList<PieceDefinition> pieces,
            IReadOnlyList<FusionRecipe> recipes,
            IReadOnlyList<PieceDefinition> requiredBosses)
        {
            var issues = new List<RunContentPoolIssue>(); // 전체 콘텐츠 검증 결과 생성
            ValidatePiecesInternal(pieces, issues, out HashSet<string> pieceIds); // Piece Pool 무결성 및 ID 집합 생성
            ValidateBosses(requiredBosses, issues); // 필수 Boss 리소스 검증
            ValidateFusion(recipes, pieceIds, issues); // Fusion Recipe 규칙·참조 검증
            return issues; // 전체 콘텐츠 문제 목록 반환
        }

        private static void ValidatePiecesInternal(
            IReadOnlyList<PieceDefinition> pieces,
            List<RunContentPoolIssue> issues,
            out HashSet<string> pieceIds)
        {
            pieceIds = new HashSet<string>(); // 유효 PieceId 집합 생성
            if (pieces == null) return; // Piece 목록 미제공 시 개별 검사 생략

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceDefinition piece = pieces[i]; // 현재 Piece 조회

                if (piece == null)
                {
                    issues.Add(new RunContentPoolIssue(RunContentPoolIssueType.NullPiece, $"index_{i}", "PieceDefinition 참조가 비어 있습니다.")); // null Piece 기록
                    continue; // 다음 Piece 검사
                }

                if (string.IsNullOrWhiteSpace(piece.PieceId))
                {
                    issues.Add(new RunContentPoolIssue(RunContentPoolIssueType.EmptyPieceId, piece.name, "PieceId가 비어 있습니다.")); // 빈 ID 기록
                    continue; // 중복 집합 등록 생략
                }

                if (!pieceIds.Add(piece.PieceId))
                {
                    issues.Add(new RunContentPoolIssue(RunContentPoolIssueType.DuplicatePieceId, piece.PieceId, "같은 PieceId를 사용하는 PieceDefinition이 둘 이상입니다.")); // 중복 ID 기록
                }
            }
        }

        private static void ValidateBosses(
            IReadOnlyList<PieceDefinition> requiredBosses,
            List<RunContentPoolIssue> issues)
        {
            if (requiredBosses == null) return; // 필수 Boss 목록 미제공 시 검사 생략

            for (int i = 0; i < requiredBosses.Count; i++)
            {
                PieceDefinition boss = requiredBosses[i]; // 현재 Boss 조회
                if (RunContentPoolRules.CanUseAsBoss(boss)) continue; // 정상 Boss 통과

                string contentId = boss != null && !string.IsNullOrWhiteSpace(boss.PieceId)
                    ? boss.PieceId
                    : $"boss_index_{i}"; // 진단용 Boss ID 계산
                issues.Add(new RunContentPoolIssue(RunContentPoolIssueType.InvalidBossPiece, contentId, "필수 Boss가 없거나 PieceCategory.Boss가 아닙니다.")); // Boss 오류 기록
            }
        }

        private static void ValidateFusion(
            IReadOnlyList<FusionRecipe> recipes,
            HashSet<string> pieceIds,
            List<RunContentPoolIssue> issues)
        {
            if (recipes == null) return; // Fusion Recipe 목록 미제공 시 검사 생략

            IReadOnlyList<FusionRecipeContentIssue> recipeIssues = FusionRecipeContentValidator.Validate(recipes); // 기존 Recipe 규칙 검증 재사용

            for (int i = 0; i < recipeIssues.Count; i++)
            {
                FusionRecipeContentIssue recipeIssue = recipeIssues[i]; // 기존 Fusion 문제 조회
                if (recipeIssue == null) continue; // 빈 문제 항목 제외
                issues.Add(new RunContentPoolIssue(
                    RunContentPoolIssueType.FusionRecipeIssue,
                    recipeIssue.RecipeId,
                    recipeIssue.ToString())); // 기존 Recipe 문제를 통합 검증 결과에 포함
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                FusionRecipe recipe = recipes[i]; // 현재 Recipe 조회
                if (recipe == null) continue; // null Recipe는 기존 Validator가 처리

                ValidateFusionMaterial(recipe.RecipeId, "MaterialA", recipe.MaterialA, pieceIds, issues); // 재료 A Pool 참조 검증
                ValidateFusionMaterial(recipe.RecipeId, "MaterialB", recipe.MaterialB, pieceIds, issues); // 재료 B Pool 참조 검증

                if (recipe.Result == null || string.IsNullOrWhiteSpace(recipe.Result.PieceId)) continue; // 결과 누락은 기존 Validator가 처리
                if (pieceIds.Contains(recipe.Result.PieceId)) continue; // 전체 Piece Pool에 결과 존재 확인

                issues.Add(new RunContentPoolIssue(
                    RunContentPoolIssueType.FusionResultNotInPool,
                    recipe.RecipeId,
                    $"Result '{recipe.Result.PieceId}'가 전체 Piece Pool에 없습니다.")); // 결과 참조 누락 기록
            }
        }

        private static void ValidateFusionMaterial(
            string recipeId,
            string slotName,
            PieceDefinition material,
            HashSet<string> pieceIds,
            List<RunContentPoolIssue> issues)
        {
            if (material == null || string.IsNullOrWhiteSpace(material.PieceId)) return; // 재료 누락은 기존 Validator가 처리
            if (pieceIds.Contains(material.PieceId)) return; // 전체 Piece Pool에 재료 존재 확인

            issues.Add(new RunContentPoolIssue(
                RunContentPoolIssueType.FusionMaterialNotInPool,
                recipeId,
                $"{slotName} '{material.PieceId}'가 전체 Piece Pool에 없습니다.")); // 재료 참조 누락 기록
        }
    }
}
