using System.IO; // 소스 회귀 검사 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // Application 경로 사용
using ProjectEta.Meta; // 영구 성장 상태·카테고리·정의 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day58MetaProgressPanelTests
    {
        [Test]
        public void MetaUnlockCatalog_DefinitionsProvideDescriptions()
        {
            for (int i = 0; i < MetaUnlockCatalog.All.Count; i++)
            {
                MetaUnlockDefinition definition = MetaUnlockCatalog.All[i]; // 현재 영구 해금 정의 조회
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description)); // 상세 설명 존재 검증
            }
        }

        [Test]
        public void MetaProgressPanelState_FiltersDefinitionsByCategory()
        {
            var state = new MetaProgressPanelState(); // 영구 성장 패널 상태 생성

            state.ShowCategory(MetaProgressCategory.King); // 킹 카테고리 선택
            var kings = state.GetFilteredDefinitions(); // 킹 해금 목록 조회

            Assert.AreEqual(3, kings.Count); // 공격·방어·전략형 킹 수 검증
            for (int i = 0; i < kings.Count; i++)
            {
                Assert.AreEqual(MetaUnlockType.King, kings[i].UnlockType); // 킹 타입 필터 검증
            }
        }

        [Test]
        public void MetaProgressPanelState_EvaluatesUnlockStatus()
        {
            MetaUnlockDefinition definition = MetaUnlockCatalog.All[0]; // 첫 영구 해금 정의 조회
            var progress = new MetaProgressState(); // 빈 영구 진행 상태 생성

            Assert.AreEqual(MetaUnlockDisplayState.Insufficient, MetaProgressPanelState.Evaluate(progress, definition)); // 토큰 부족 상태 검증

            progress.AddTokens(definition.Cost); // 정확한 해금 비용 지급
            Assert.AreEqual(MetaUnlockDisplayState.Available, MetaProgressPanelState.Evaluate(progress, definition)); // 해금 가능 상태 검증

            Assert.IsTrue(MetaUnlockService.TryUnlock(progress, definition)); // 실제 영구 해금 실행
            Assert.AreEqual(MetaUnlockDisplayState.Unlocked, MetaProgressPanelState.Evaluate(progress, definition)); // 해금 완료 상태 검증
        }

        [Test]
        public void MetaProgressPanelState_AllCategoryReturnsWholeCatalog()
        {
            var state = new MetaProgressPanelState(); // 영구 성장 패널 상태 생성

            state.ShowCategory(MetaProgressCategory.All); // 전체 카테고리 선택
            var definitions = state.GetFilteredDefinitions(); // 전체 목록 조회

            Assert.AreEqual(MetaUnlockCatalog.All.Count, definitions.Count); // 전체 카탈로그 수 일치 검증
        }


        [Test]
        public void MetaProgressUI_UsesReusableMetaProgressPanel()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/Meta/MetaProgressUI.cs"); // 런 결과 메타 UI 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 런 결과 메타 UI 소스 읽기

            StringAssert.Contains("MetaProgressPanelController", source); // 런 결과에서 재사용 영구 성장 패널 연결 검증
            StringAssert.Contains("영구 성장 보기", source); // 런 결과 영구 성장 진입 버튼 검증
        }

        [Test]
        public void SceneRuntimeBootstrap_InjectsMainMenuMetaProgressBridge()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs"); // 씬 부트스트랩 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 씬 부트스트랩 소스 읽기

            StringAssert.Contains("EnsureComponent<MainMenuMetaProgressBridge>", source); // MainMenu 영구 성장 브리지 주입 검증
        }
    }
}
