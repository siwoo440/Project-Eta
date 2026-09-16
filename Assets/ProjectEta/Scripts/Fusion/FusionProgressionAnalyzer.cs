using System.Collections.Generic; // IReadOnlyList<T> 사용
using ProjectEta.Pieces; // 기물 등급·정의 사용
using ProjectEta.Run; // 전체 콘텐츠 DB 참조 검증 사용

namespace ProjectEta.Fusion // 합성 관련 타입 네임스페이스
{ // 네임스페이스 범위
    public sealed class FusionProgressionReport // 합성 성장 콘텐츠 집계 결과
    { // 클래스 범위
        private const int MaximumGrade = (int)PieceGrade.FiveStar; // 최상위 등급 숫자
        private readonly int[] _pieceCounts; // 등급별 플레이어 기물 수
        private readonly int[] _recipeCounts; // 결과 등급별 유효 레시피 수

        public static FusionProgressionReport Empty { get; } = new FusionProgressionReport(new int[MaximumGrade + 1], new int[MaximumGrade + 1], 0, false, PieceGrade.TwoStar); // 빈 진단 결과
        public int ContentIssueCount { get; } // 잘못된 레시피 수
        public bool AreDatabasesConnected { get; } // 기물·레시피 DB 연결 여부
        public PieceGrade? FirstUnreachableGrade { get; } // 1성 시작 기준 첫 도달 불가 등급
        public PieceGrade? FirstMissingPieceGrade => FindFirstMissingGrade(_pieceCounts, PieceGrade.OneStar); // 첫 누락 기물 등급
        public PieceGrade? FirstMissingRecipeGrade => FindFirstMissingGrade(_recipeCounts, PieceGrade.TwoStar); // 첫 누락 결과 레시피 등급
        public bool HasCompleteGradeCoverage => AreDatabasesConnected && !FirstMissingPieceGrade.HasValue && !FirstMissingRecipeGrade.HasValue && !FirstUnreachableGrade.HasValue && ContentIssueCount == 0; // 1~5성 전체 성장 완성 여부

        internal FusionProgressionReport(int[] pieceCounts, int[] recipeCounts, int contentIssueCount, bool areDatabasesConnected, PieceGrade? firstUnreachableGrade) // 집계 결과 생성
        { // 생성자 범위
            _pieceCounts = pieceCounts ?? new int[MaximumGrade + 1]; // 기물 수 배열 저장
            _recipeCounts = recipeCounts ?? new int[MaximumGrade + 1]; // 레시피 수 배열 저장
            ContentIssueCount = contentIssueCount < 0 ? 0 : contentIssueCount; // 문제 수 음수 차단
            AreDatabasesConnected = areDatabasesConnected; // DB 연결 상태 저장
            FirstUnreachableGrade = firstUnreachableGrade; // 첫 경로 단절 등급 저장
        } // 생성자 종료

        public int GetPieceCount(PieceGrade grade) // 지정 등급 기물 수 반환
        { // 메서드 범위
            int index = (int)grade; // 등급 숫자 변환
            return IsValidGradeIndex(index) ? _pieceCounts[index] : 0; // 범위 안 집계 반환
        } // 메서드 종료

        public int GetRecipeCount(PieceGrade resultGrade) // 지정 결과 등급 레시피 수 반환
        { // 메서드 범위
            int index = (int)resultGrade; // 등급 숫자 변환
            return IsValidGradeIndex(index) ? _recipeCounts[index] : 0; // 범위 안 집계 반환
        } // 메서드 종료

        public string BuildGradeSummary() // F1 패널 등급 요약 생성
        { // 메서드 범위
            return $"기물 1★{GetPieceCount(PieceGrade.OneStar)} / 2★{GetPieceCount(PieceGrade.TwoStar)} / 3★{GetPieceCount(PieceGrade.ThreeStar)} / 4★{GetPieceCount(PieceGrade.FourStar)} / 5★{GetPieceCount(PieceGrade.FiveStar)} · 레시피 2★{GetRecipeCount(PieceGrade.TwoStar)} / 3★{GetRecipeCount(PieceGrade.ThreeStar)} / 4★{GetRecipeCount(PieceGrade.FourStar)} / 5★{GetRecipeCount(PieceGrade.FiveStar)}"; // 전체 등급 한 줄 반환
        } // 메서드 종료

        private static PieceGrade? FindFirstMissingGrade(int[] counts, PieceGrade startGrade) // 첫 누락 등급 검색
        { // 메서드 범위
            if (counts == null) return startGrade; // 집계 없음 시작 등급 반환

            for (int grade = (int)startGrade; grade <= MaximumGrade; grade++) // 시작 등급부터 5성 순회
            { // 반복 범위
                if (grade >= counts.Length || counts[grade] <= 0) return (PieceGrade)grade; // 첫 미등록 등급 반환
            } // 반복 종료

            return null; // 모든 등급 등록 완료 반환
        } // 메서드 종료

        private static bool IsValidGradeIndex(int index) // 유효 등급 숫자 판정
        { // 메서드 범위
            return index >= (int)PieceGrade.OneStar && index <= MaximumGrade; // 1~5 범위 반환
        } // 메서드 종료
    } // 클래스 종료

    public static class FusionProgressionAnalyzer // 합성 성장 콘텐츠 분석기
    { // 클래스 범위
        private const int MaximumGrade = (int)PieceGrade.FiveStar; // 최상위 등급 숫자

        public static FusionProgressionReport Analyze(PieceDatabase pieceDatabase, FusionRecipeDatabase recipeDatabase) // 데이터베이스 기반 분석
        { // 메서드 범위
            IReadOnlyList<PieceDefinition> definitions = pieceDatabase != null ? pieceDatabase.Definitions : null; // 기물 목록 조회
            IReadOnlyList<FusionRecipe> recipes = recipeDatabase != null ? recipeDatabase.Recipes : null; // 레시피 목록 조회
            return AnalyzeInternal(definitions, recipes, pieceDatabase != null, recipeDatabase != null); // DB 연결 상태 포함 분석 반환
        } // 메서드 종료

        public static FusionProgressionReport Analyze(IReadOnlyList<PieceDefinition> definitions, IReadOnlyList<FusionRecipe> recipes) // 목록 기반 분석
        { // 메서드 범위
            return AnalyzeInternal(definitions, recipes, definitions != null, recipes != null); // 목록 연결 상태 포함 분석 반환
        } // 메서드 종료

        private static FusionProgressionReport AnalyzeInternal(IReadOnlyList<PieceDefinition> definitions, IReadOnlyList<FusionRecipe> recipes, bool hasPieceDatabase, bool hasRecipeDatabase) // 공통 성장 분석
        { // 메서드 범위
            int[] pieceCounts = new int[MaximumGrade + 1]; // 기물 등급 집계 배열 생성
            int[] recipeCounts = new int[MaximumGrade + 1]; // 레시피 결과 등급 집계 배열 생성
            int connectionIssueCount = (hasPieceDatabase ? 0 : 1) + (hasRecipeDatabase ? 0 : 1); // DB 연결 누락 수 집계
            int contentIssueCount = connectionIssueCount; // 연결 문제를 전체 문제 수에 반영
            var validPieceIds = new HashSet<string>(); // DB 등록 기물 ID 집합
            var reachablePieceIds = new HashSet<string>(); // 1성에서 도달 가능한 기물 ID 집합
            var acceptedRecipes = new List<FusionRecipe>(); // 실제 집계·연결성 계산용 고유 레시피 목록

            if (definitions != null) // 기물 목록 존재 확인
            { // 조건 범위
                for (int index = 0; index < definitions.Count; index++) // 기물 전체 순회
                { // 반복 범위
                    PieceDefinition definition = definitions[index]; // 현재 기물 조회
                    if (!IsPlayerPiece(definition)) continue; // 적·보스·빈 데이터 제외
                    int grade = (int)definition.Grade; // 기물 등급 숫자 변환
                    if (grade >= (int)PieceGrade.OneStar && grade <= MaximumGrade) pieceCounts[grade]++; // 유효 등급 기물 집계
                    if (!string.IsNullOrWhiteSpace(definition.PieceId)) validPieceIds.Add(definition.PieceId); // 유효 기물 ID 등록
                    if (definition.Grade == PieceGrade.OneStar && !string.IsNullOrWhiteSpace(definition.PieceId)) reachablePieceIds.Add(definition.PieceId); // 1성 시작 기물 등록
                } // 반복 종료
            } // 조건 종료

            if (hasPieceDatabase && hasRecipeDatabase) // 두 DB 연결 확인
            { // 조건 범위
                contentIssueCount += RunContentPoolValidator.Validate(definitions, recipes, null).Count; // 기물 ID·레시피 규칙·DB 외부 참조 문제 집계
            } // 조건 종료

            if (recipes != null && hasPieceDatabase) // 레시피와 기물 DB 존재 확인
            { // 조건 범위
                var recipeIds = new HashSet<string>(); // 고유 레시피 ID 집합
                var materialPairs = new HashSet<string>(); // 고유 재료 조합 집합

                for (int index = 0; index < recipes.Count; index++) // 레시피 전체 순회
                { // 반복 범위
                    FusionRecipe recipe = recipes[index]; // 현재 레시피 조회
                    FusionBlockReason reason = FusionRuleValidator.ValidateRecipe(recipe); // 레시피 규칙 검증

                    if (reason != FusionBlockReason.None) // 잘못된 레시피 확인
                    { // 조건 범위
                        continue; // 유효 레시피 집계 제외
                    } // 조건 종료

                    if (!ReferencesRegisteredPieces(recipe, validPieceIds)) continue; // DB 밖 재료·결과 레시피 제외
                    if (!IsPlayerPiece(recipe.Result)) continue; // 적·보스 결과 레시피 제외
                    if (!recipeIds.Add(recipe.RecipeId)) continue; // 중복 레시피 ID 제외
                    if (!materialPairs.Add(CreateMaterialPairKey(recipe))) continue; // 중복 재료 조합 제외

                    int resultGrade = (int)recipe.Result.Grade; // 결과 등급 숫자 변환
                    if (resultGrade >= (int)PieceGrade.TwoStar && resultGrade <= MaximumGrade) recipeCounts[resultGrade]++; // 유효 결과 레시피 집계
                    acceptedRecipes.Add(recipe); // 연결성 계산용 레시피 등록
                } // 반복 종료
            } // 조건 종료

            ExpandReachablePieces(acceptedRecipes, reachablePieceIds); // 1성 시작 합성 가능 결과 확장
            PieceGrade? firstUnreachableGrade = FindFirstUnreachableGrade(definitions, reachablePieceIds); // 첫 경로 단절 등급 계산
            bool areDatabasesConnected = hasPieceDatabase && hasRecipeDatabase; // 두 DB 연결 완료 여부 계산
            return new FusionProgressionReport(pieceCounts, recipeCounts, contentIssueCount, areDatabasesConnected, firstUnreachableGrade); // 완성 진단 결과 반환
        } // 메서드 종료

        private static bool ReferencesRegisteredPieces(FusionRecipe recipe, HashSet<string> validPieceIds) // 레시피 DB 참조 유효성 판정
        { // 메서드 범위
            if (recipe == null || validPieceIds == null) return false; // 레시피·ID 집합 누락 차단
            if (recipe.MaterialA == null || recipe.MaterialB == null || recipe.Result == null) return false; // 재료·결과 누락 차단
            return validPieceIds.Contains(recipe.MaterialA.PieceId) && validPieceIds.Contains(recipe.MaterialB.PieceId) && validPieceIds.Contains(recipe.Result.PieceId); // 모든 참조 등록 여부 반환
        } // 메서드 종료

        private static string CreateMaterialPairKey(FusionRecipe recipe) // 순서 무관 재료 조합 키 생성
        { // 메서드 범위
            string first = recipe.MaterialA.PieceId; // 재료 A ID 조회
            string second = recipe.MaterialB.PieceId; // 재료 B ID 조회
            return string.CompareOrdinal(first, second) <= 0 ? $"{first}|{second}" : $"{second}|{first}"; // 정렬된 조합 키 반환
        } // 메서드 종료

        private static void ExpandReachablePieces(IReadOnlyList<FusionRecipe> recipes, HashSet<string> reachablePieceIds) // 1성 시작 도달 기물 확장
        { // 메서드 범위
            if (recipes == null || reachablePieceIds == null) return; // 입력 누락 처리
            bool changed; // 현재 반복 신규 도달 여부

            do // 더 이상 결과가 추가되지 않을 때까지 반복
            { // 반복 범위
                changed = false; // 신규 도달 상태 초기화

                for (int index = 0; index < recipes.Count; index++) // 유효 레시피 순회
                { // 반복 범위
                    FusionRecipe recipe = recipes[index]; // 현재 레시피 조회
                    if (!reachablePieceIds.Contains(recipe.MaterialA.PieceId) || !reachablePieceIds.Contains(recipe.MaterialB.PieceId)) continue; // 재료 경로 단절 제외
                    if (reachablePieceIds.Add(recipe.Result.PieceId)) changed = true; // 새 결과 도달 등록
                } // 반복 종료
            } // 반복 종료
            while (changed); // 신규 결과가 있으면 다음 단계 반복
        } // 메서드 종료

        private static PieceGrade? FindFirstUnreachableGrade(IReadOnlyList<PieceDefinition> definitions, HashSet<string> reachablePieceIds) // 첫 도달 불가 등급 검색
        { // 메서드 범위
            for (int grade = (int)PieceGrade.TwoStar; grade <= MaximumGrade; grade++) // 2~5성 순회
            { // 반복 범위
                bool hasReachablePiece = false; // 현재 등급 도달 기물 여부

                if (definitions != null) // 기물 목록 존재 확인
                { // 조건 범위
                    for (int index = 0; index < definitions.Count; index++) // 전체 기물 순회
                    { // 반복 범위
                        PieceDefinition definition = definitions[index]; // 현재 기물 조회
                        if (!IsPlayerPiece(definition) || (int)definition.Grade != grade) continue; // 다른 분류·등급 제외
                        if (!reachablePieceIds.Contains(definition.PieceId)) continue; // 도달하지 못한 기물 제외
                        hasReachablePiece = true; // 현재 등급 도달 확인
                        break; // 등급 검색 종료
                    } // 반복 종료
                } // 조건 종료

                if (!hasReachablePiece) return (PieceGrade)grade; // 첫 경로 단절 등급 반환
            } // 반복 종료

            return null; // 2~5성 경로 연결 완료 반환
        } // 메서드 종료

        private static bool IsPlayerPiece(PieceDefinition definition) // 플레이어 기물 분류 판정
        { // 메서드 범위
            if (definition == null) return false; // 빈 정의 제외
            return definition.Category != PieceCategory.Monster && definition.Category != PieceCategory.Boss; // 적·보스 분류 제외
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
