#if UNITY_EDITOR
using System.Collections.Generic; // List 사용
using System.IO; // 소스 검사
using System.Reflection; // 테스트 필드 설정
using NUnit.Framework; // 테스트 도구
using UnityEditor; // 실제 에셋 로드
using UnityEngine; // ScriptableObject 사용
using ProjectEta.Fusion; // 합성 진단 사용
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day84FusionProgressionTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _createdObjects.Count; index++)
            {
                if (_createdObjects[index] != null) Object.DestroyImmediate(_createdObjects[index]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Analyze_플레이어기물과합성결과를_등급별로집계한다()
        {
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic);
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion);
            PieceDefinition threeStar = CreatePiece("three", PieceGrade.ThreeStar, PieceCategory.Fusion);
            PieceDefinition boss = CreatePiece("boss", PieceGrade.FiveStar, PieceCategory.Boss);
            FusionRecipe twoStarRecipe = CreateRecipe("two_from_one", oneStar, oneStar, twoStar);
            FusionRecipe threeStarRecipe = CreateRecipe("three_from_two", twoStar, twoStar, threeStar);

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(
                new[] { oneStar, twoStar, threeStar, boss },
                new[] { twoStarRecipe, threeStarRecipe });

            Assert.That(report.GetPieceCount(PieceGrade.OneStar), Is.EqualTo(1));
            Assert.That(report.GetPieceCount(PieceGrade.TwoStar), Is.EqualTo(1));
            Assert.That(report.GetPieceCount(PieceGrade.ThreeStar), Is.EqualTo(1));
            Assert.That(report.GetPieceCount(PieceGrade.FiveStar), Is.EqualTo(0));
            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(1));
            Assert.That(report.GetRecipeCount(PieceGrade.ThreeStar), Is.EqualTo(1));
        }

        [Test]
        public void Analyze_상위등급결과가없으면_첫누락등급을반환한다()
        {
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic);
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion);
            FusionRecipe twoStarRecipe = CreateRecipe("two_from_one", oneStar, oneStar, twoStar);

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(
                new[] { oneStar, twoStar },
                new[] { twoStarRecipe });

            Assert.That(report.FirstMissingPieceGrade, Is.EqualTo(PieceGrade.ThreeStar));
            Assert.That(report.FirstMissingRecipeGrade, Is.EqualTo(PieceGrade.ThreeStar));
        }

        [Test]
        public void Analyze_같은재료조합이중복되면_데이터문제로집계한다()
        {
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic);
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion);
            FusionRecipe first = CreateRecipe("first", oneStar, oneStar, twoStar);
            FusionRecipe duplicate = CreateRecipe("duplicate", oneStar, oneStar, twoStar);

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(
                new[] { oneStar, twoStar },
                new[] { first, duplicate });

            Assert.That(report.ContentIssueCount, Is.EqualTo(1));
            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(1));
        }

        [Test]
        public void Analyze_상위등급레시피가분리되어있으면_완료로판정하지않는다()
        {
            PieceDefinition oneStar = CreatePiece("one", PieceGrade.OneStar, PieceCategory.Basic);
            PieceDefinition reachableTwoStar = CreatePiece("two_reachable", PieceGrade.TwoStar, PieceCategory.Fusion);
            PieceDefinition isolatedTwoStar = CreatePiece("two_isolated", PieceGrade.TwoStar, PieceCategory.Fusion);
            PieceDefinition threeStar = CreatePiece("three", PieceGrade.ThreeStar, PieceCategory.Fusion);
            FusionRecipe twoStarRecipe = CreateRecipe("two", oneStar, oneStar, reachableTwoStar);
            FusionRecipe threeStarRecipe = CreateRecipe("three", isolatedTwoStar, isolatedTwoStar, threeStar);

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(
                new[] { oneStar, reachableTwoStar, isolatedTwoStar, threeStar },
                new[] { twoStarRecipe, threeStarRecipe });

            Assert.That(report.FirstUnreachableGrade, Is.EqualTo(PieceGrade.ThreeStar));
        }

        [Test]
        public void Analyze_결과기물이기물목록밖에있으면_문제로집계한다()
        {
            PieceDefinition twoStar = CreatePiece("two", PieceGrade.TwoStar, PieceCategory.Fusion);
            PieceDefinition externalThreeStar = CreatePiece("external_three", PieceGrade.ThreeStar, PieceCategory.Fusion);
            FusionRecipe externalRecipe = CreateRecipe("external", twoStar, twoStar, externalThreeStar);

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(
                new[] { twoStar },
                new[] { externalRecipe });

            Assert.That(report.ContentIssueCount, Is.EqualTo(1));
        }

        [Test]
        public void Analyze_기물목록이없으면_연결오류를구분한다()
        {
            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(null, new FusionRecipe[0]);

            Assert.That(report.AreDatabasesConnected, Is.False);
            Assert.That(report.ContentIssueCount, Is.EqualTo(1));
        }

        [Test]
        public void ProjectData_1대5성기물과전체70개레시피가연결된다()
        {
            PieceDatabase pieceDatabase = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            FusionRecipeDatabase recipeDatabase = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset");

            FusionProgressionReport report = FusionProgressionAnalyzer.Analyze(pieceDatabase, recipeDatabase);

            Assert.That(report.GetPieceCount(PieceGrade.OneStar), Is.EqualTo(18));
            Assert.That(report.GetPieceCount(PieceGrade.TwoStar), Is.EqualTo(19));
            Assert.That(report.GetPieceCount(PieceGrade.ThreeStar), Is.EqualTo(18));
            Assert.That(report.GetPieceCount(PieceGrade.FourStar), Is.EqualTo(18));
            Assert.That(report.GetPieceCount(PieceGrade.FiveStar), Is.EqualTo(8));
            Assert.That(report.GetRecipeCount(PieceGrade.TwoStar), Is.EqualTo(21));
            Assert.That(report.GetRecipeCount(PieceGrade.ThreeStar), Is.EqualTo(20));
            Assert.That(report.GetRecipeCount(PieceGrade.FourStar), Is.EqualTo(20));
            Assert.That(report.GetRecipeCount(PieceGrade.FiveStar), Is.EqualTo(9));
            Assert.That(report.FirstMissingPieceGrade, Is.Null);
            Assert.That(report.FirstMissingRecipeGrade, Is.Null);
            Assert.That(report.FirstUnreachableGrade, Is.Null);
            Assert.That(report.ContentIssueCount, Is.EqualTo(0));
            Assert.That(report.HasCompleteGradeCoverage, Is.True);
        }

        [Test]
        public void BattleScene_합성진단용기물데이터베이스를_연결한다()
        {
            string scenePath = Path.Combine(Application.dataPath, "ProjectEta/Scenes/Battle.unity");
            string source = File.ReadAllText(scenePath);
            StringAssert.Contains("_pieceDatabase: {fileID: 11400000, guid: 04bfa900f3732e84be74665555943792, type: 2}", source);
        }

        [Test]
        public void DebugPanel_합성성장집계를_상태페이지에표시한다()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/Debug/ProjectEtaDebugWindow.cs");
            string source = File.ReadAllText(sourcePath);
            StringAssert.Contains("FusionProgressionAnalyzer.Analyze", source);
            StringAssert.Contains("합성 성장", source);
        }

        private PieceDefinition CreatePiece(string pieceId, PieceGrade grade, PieceCategory category)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>();
            SetField(definition, "_pieceId", pieceId);
            SetField(definition, "_displayName", pieceId);
            SetField(definition, "_grade", grade);
            SetField(definition, "_category", category);
            _createdObjects.Add(definition);
            return definition;
        }

        private FusionRecipe CreateRecipe(string recipeId, PieceDefinition materialA, PieceDefinition materialB, PieceDefinition result)
        {
            FusionRecipe recipe = ScriptableObject.CreateInstance<FusionRecipe>();
            SetField(recipe, "_recipeId", recipeId);
            SetField(recipe, "_materialA", materialA);
            SetField(recipe, "_materialB", materialB);
            SetField(recipe, "_result", result);
            _createdObjects.Add(recipe);
            return recipe;
        }

        private static void SetField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
#endif
