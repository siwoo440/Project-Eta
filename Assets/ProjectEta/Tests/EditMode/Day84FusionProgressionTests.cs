#if UNITY_EDITOR // Unity Editor 전용 테스트 범위
using System.Collections.Generic; // List<T> 사용
using System.IO; // 씬·디버그 소스 검사
using System.Reflection; // 테스트 데이터 비공개 필드 설정
using NUnit.Framework; // EditMode 테스트 도구
using UnityEditor; // 실제 프로젝트 에셋 로드
using UnityEngine; // ScriptableObject 생성
using ProjectEta.Fusion; // 합성 진행 진단 사용
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 범위
    public sealed class Day84FusionProgressionTests // 84일차 합성 성장 진단 테스트
    { // 클래스 범위
        private readonly List<Object> _createdObjects = new List<Object>(); // 테스트 생성 객체 목록

        [TearDown] // 각 테스트 종료 정리
        public void TearDown() // 생성 객체 제거
        { // 메서드 범위
            for (int index = 0; index < _createdObjects.Count; index++) // 생성 객체 순회
            { // 반복 범위
                if (_createdObjects[index] != null) Object.DestroyImmediate(_createdObjects[index]); // 남은 객체 제거
            } // 반복 종료

            _createdObjects.Clear(); // 정리 목록 초기화
        } // 메서드 종료

        [Test] // 등급별 기물·레시피 집계 검증
        public void Analyze_플레이어기물과합성결과를_등급별로집계한다() // 성장 콘텐츠 집계 확인
        { // 테스트 범위
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic); // 1성 기본 기물 생성
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion); // 2성 합성 기물 생성
            PieceDefinition threeStar = CreatePiece("three", PieceGrade.ThreeStar, PieceCategory.Fusion); // 3성 합성 기물 생성
            PieceDefinition boss = CreatePiece("boss", PieceGrade.FiveStar, PieceCategory.Boss); // 집계 제외 보스 생성
            FusionRecipe twoStarRecipe = CreateRecipe("two_from_one", oneStar, oneStar, twoStar); // 2성 결과 레시피 생성
            FusionRecipe threeStarRecipe = CreateRecipe("three_from_two", twoStar, twoStar, threeStar); // 3성 결과 레시피 생성

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze( // 합성 성장 현황 분석
                new[] { oneStar, twoStar, threeStar, boss }, // 기물 목록 전달
                new[] { twoStarRecipe, threeStarRecipe }); // 레시피 목록 전달

            Assert.That(report.GetPieceCount(PieceGrade.OneStar), Is.EqualTo(1)); // 1성 플레이어 기물 수 검증
            Assert.That(report.GetPieceCount(PieceGrade.TwoStar), Is.EqualTo(1)); // 2성 플레이어 기물 수 검증
            Assert.That(report.GetPieceCount(PieceGrade.ThreeStar), Is.EqualTo(1)); // 3성 플레이어 기물 수 검증
            Assert.That(report.GetPieceCount(PieceGrade.FiveStar), Is.EqualTo(0)); // 보스 기물 제외 검증
            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(1)); // 2성 결과 레시피 수 검증
            Assert.That(report.GetRecipeCount(PieceGrade.ThreeStar), Is.EqualTo(1)); // 3성 결과 레시피 수 검증
        } // 테스트 범위 종료

        [Test] // 끊긴 성장 단계 검증
        public void Analyze_상위등급결과가없으면_첫누락등급을반환한다() // 성장 단절 탐지 확인
        { // 테스트 범위
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic); // 1성 기본 기물 생성
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion); // 2성 합성 기물 생성
            FusionRecipe twoStarRecipe = CreateRecipe("two_from_one", oneStar, oneStar, twoStar); // 2성 결과 레시피 생성

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze( // 합성 성장 현황 분석
                new[] { oneStar, twoStar }, // 1·2성 기물 전달
                new[] { twoStarRecipe }); // 2성 레시피 전달

            Assert.That(report.HasCompleteGradeCoverage, Is.False); // 전체 등급 미완성 검증
            Assert.That(report.FirstMissingPieceGrade, Is.EqualTo(PieceGrade.ThreeStar)); // 첫 누락 기물 등급 검증
            Assert.That(report.FirstMissingRecipeGrade, Is.EqualTo(PieceGrade.ThreeStar)); // 첫 누락 레시피 등급 검증
            Assert.That(report.BuildGradeSummary(), Is.EqualTo("기물 1★1 / 2★1 / 3★0 / 4★0 / 5★0 · 레시피 2★1 / 3★0 / 4★0 / 5★0")); // 패널 요약 문구 검증
        } // 테스트 범위 종료

        [Test] // 중복 레시피 문제 집계 검증
        public void Analyze_같은재료조합이중복되면_데이터문제로집계한다() // 중복 콘텐츠 탐지 확인
        { // 테스트 범위
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic); // 1성 재료 생성
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion); // 2성 결과 생성
            FusionRecipe first = CreateRecipe("first", oneStar, oneStar, twoStar); // 첫 레시피 생성
            FusionRecipe duplicate = CreateRecipe("duplicate", oneStar, oneStar, twoStar); // 중복 재료 조합 생성

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze( // 중복 레시피 포함 분석
                new[] { oneStar, twoStar }, // 기물 목록 전달
                new[] { first, duplicate }); // 중복 레시피 목록 전달

            Assert.That(report.ContentIssueCount, Is.EqualTo(1)); // 중복 재료 조합 문제 집계 검증
            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(1)); // 실제 조회 가능한 고유 조합만 집계 검증
        } // 테스트 범위 종료

        [Test] // 합성 경로 연결성 검증
        public void Analyze_상위등급레시피가분리되어있으면_완료로판정하지않는다() // 1성 시작 경로 단절 확인
        { // 테스트 범위
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic); // 시작 1성 생성
            PieceDefinition reachableTwoStar = CreatePiece("two_reachable", PieceGrade.TwoStar, PieceCategory.Fusion); // 도달 가능한 2성 생성
            PieceDefinition isolatedTwoStar = CreatePiece("two_isolated", PieceGrade.TwoStar, PieceCategory.Fusion); // 고립된 2성 생성
            PieceDefinition threeStar = CreatePiece("three", PieceGrade.ThreeStar, PieceCategory.Fusion); // 3성 생성
            PieceDefinition fourStar = CreatePiece("four", PieceGrade.FourStar, PieceCategory.Fusion); // 4성 생성
            PieceDefinition fiveStar = CreatePiece("five", PieceGrade.FiveStar, PieceCategory.Fusion); // 5성 생성
            FusionRecipe twoStarRecipe = CreateRecipe("two", oneStar, oneStar, reachableTwoStar); // 1성 연결 레시피 생성
            FusionRecipe threeStarRecipe = CreateRecipe("three", isolatedTwoStar, isolatedTwoStar, threeStar); // 고립 3성 레시피 생성
            FusionRecipe fourStarRecipe = CreateRecipe("four", threeStar, threeStar, fourStar); // 고립 4성 레시피 생성
            FusionRecipe fiveStarRecipe = CreateRecipe("five", fourStar, fourStar, fiveStar); // 고립 5성 레시피 생성

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze( // 전체 등급 데이터 분석
                new[] { oneStar, reachableTwoStar, isolatedTwoStar, threeStar, fourStar, fiveStar }, // 모든 등급 기물 전달
                new[] { twoStarRecipe, threeStarRecipe, fourStarRecipe, fiveStarRecipe }); // 모든 결과 등급 레시피 전달

            Assert.That(report.FirstUnreachableGrade, Is.EqualTo(PieceGrade.ThreeStar)); // 3성 경로 단절 검증
            Assert.That(report.HasCompleteGradeCoverage, Is.False); // 단순 수량 충족 완료 오판 방지
        } // 테스트 범위 종료

        [Test] // 데이터베이스 외부 참조 검증
        public void Analyze_결과기물이기물목록밖에있으면_문제로집계하고레시피에서제외한다() // 외부 결과 참조 차단 확인
        { // 테스트 범위
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion); // 등록 2성 생성
            PieceDefinition externalThreeStar = CreatePiece("external_three", PieceGrade.ThreeStar, PieceCategory.Fusion); // 미등록 3성 결과 생성
            FusionRecipe externalRecipe = CreateRecipe("external", twoStar, twoStar, externalThreeStar); // 외부 결과 레시피 생성

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze( // 외부 참조 포함 분석
                new[] { twoStar }, // 결과가 빠진 기물 목록 전달
                new[] { externalRecipe }); // 외부 결과 레시피 전달

            Assert.That(report.ContentIssueCount, Is.EqualTo(1)); // DB 외부 결과 문제 집계 검증
            Assert.That(report.GetRecipeCount(PieceGrade.ThreeStar), Is.EqualTo(0)); // 잘못된 레시피 수 제외 검증
        } // 테스트 범위 종료

        [Test] // 데이터베이스 연결 누락 검증
        public void Analyze_기물목록이없으면_연결오류를구분한다() // null 데이터 연결 진단 확인
        { // 테스트 범위
            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(null, new FusionRecipe[0]); // 기물 DB 누락 상태 분석

            Assert.That(report.AreDatabasesConnected, Is.False); // DB 연결 실패 검증
            Assert.That(report.ContentIssueCount, Is.EqualTo(1)); // 연결 누락 문제 수 검증
            Assert.That(report.HasCompleteGradeCoverage, Is.False); // 연결 누락 완료 차단 검증
        } // 테스트 범위 종료

        [Test] // 실제 프로젝트 성장 현황 검증
        public void ProjectData_현재3성이첫누락성장단계다() // 등록 콘텐츠 기준 확인
        { // 테스트 범위
            PieceDatabase pieceDatabase = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 기물 DB 로드
            FusionRecipeDatabase recipeDatabase = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset"); // 실제 레시피 DB 로드

            Assert.That(pieceDatabase, Is.Not.Null); // 기물 DB 존재 검증
            Assert.That(recipeDatabase, Is.Not.Null); // 레시피 DB 존재 검증

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(pieceDatabase, recipeDatabase); // 실제 콘텐츠 성장 분석

            Assert.That(report.GetPieceCount(PieceGrade.OneStar), Is.EqualTo(12)); // 현재 1성 12종 검증
            Assert.That(report.GetPieceCount(PieceGrade.TwoStar), Is.EqualTo(14)); // 현재 2성 14종 검증
            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(4)); // 현재 2성 결과 레시피 4개 검증
            Assert.That(report.FirstMissingPieceGrade, Is.EqualTo(PieceGrade.ThreeStar)); // 현재 첫 누락 기물 3성 검증
            Assert.That(report.FirstMissingRecipeGrade, Is.EqualTo(PieceGrade.ThreeStar)); // 현재 첫 누락 레시피 3성 검증
            Assert.That(report.ContentIssueCount, Is.EqualTo(0)); // 현재 등록 레시피 무결성 검증
        } // 테스트 범위 종료

        [Test] // Battle 씬 기물 DB 연결 검증
        public void BattleScene_합성진단용기물데이터베이스를_연결한다() // 실제 씬 데이터 연결 확인
        { // 테스트 범위
            string scenePath = Path.Combine(Application.dataPath, "ProjectEta/Scenes/Battle.unity"); // Battle 씬 경로 계산
            string sceneSource = File.ReadAllText(scenePath); // Battle 씬 YAML 읽기

            StringAssert.Contains("_pieceDatabase: {fileID: 11400000, guid: 04bfa900f3732e84be74665555943792, type: 2}", sceneSource); // 실제 PieceDatabase 연결 검증
        } // 테스트 범위 종료

        [Test] // F1 합성 성장 표시 검증
        public void DebugPanel_합성성장집계를_상태페이지에표시한다() // 개발 진단 연결 확인
        { // 테스트 범위
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/Debug/ProjectEtaDebugWindow.cs"); // 디버그 패널 소스 경로 계산
            string source = File.ReadAllText(sourcePath); // 디버그 패널 구현 읽기

            StringAssert.Contains("FusionProgressionAnalyzer.Analyze", source); // 실제 분석기 호출 검증
            StringAssert.Contains("합성 성장", source); // 상태 페이지 구역 제목 검증
            StringAssert.Contains("BuildGradeSummary", source); // 등급 요약 출력 검증
        } // 테스트 범위 종료

        private PieceDefinition CreatePiece(string pieceId, PieceGrade grade, PieceCategory category) // 테스트 기물 생성
        { // 메서드 범위
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 기물 인스턴스 생성
            SetField(definition, "_pieceId", pieceId); // 고유 ID 설정
            SetField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetField(definition, "_grade", grade); // 등급 설정
            SetField(definition, "_category", category); // 분류 설정
            _createdObjects.Add(definition); // 정리 목록 등록
            return definition; // 생성 기물 반환
        } // 메서드 종료

        private FusionRecipe CreateRecipe(string recipeId, PieceDefinition materialA, PieceDefinition materialB, PieceDefinition result) // 테스트 레시피 생성
        { // 메서드 범위
            FusionRecipe recipe = ScriptableObject.CreateInstance<FusionRecipe>(); // 레시피 인스턴스 생성
            SetField(recipe, "_recipeId", recipeId); // 레시피 ID 설정
            SetField(recipe, "_materialA", materialA); // 재료 A 설정
            SetField(recipe, "_materialB", materialB); // 재료 B 설정
            SetField(recipe, "_result", result); // 결과 기물 설정
            _createdObjects.Add(recipe); // 정리 목록 등록
            return recipe; // 생성 레시피 반환
        } // 메서드 종료

        private static void SetField<TValue>(object target, string fieldName, TValue value) // 비공개 필드 설정
        { // 메서드 범위
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 대상 필드 조회
            Assert.That(field, Is.Not.Null, fieldName); // 필드 존재 검증
            field.SetValue(target, value); // 테스트 값 적용
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
#endif // Unity Editor 전용 테스트 종료
