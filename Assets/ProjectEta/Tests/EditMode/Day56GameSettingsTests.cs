using System.IO; // 소스 회귀 검사 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // FullScreenMode·Application 경로 사용
using ProjectEta.Settings; // 56일차 설정 상태·해상도 목록 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day56GameSettingsTests
    {
        [Test]
        public void GameSettingsEditState_ApplyCancelReset_TracksDirtyState()
        {
            var initial = new GameSettingsData
            {
                ResolutionWidth = 1920, // 초기 해상도 너비
                ResolutionHeight = 1080, // 초기 해상도 높이
                ScreenMode = (int)FullScreenMode.FullScreenWindow, // 초기 화면 모드
                UiScale = 1f // 초기 UI 배율
            };
            var state = new GameSettingsEditState(initial); // 설정 편집 상태 생성

            state.SetUiScale(1.2f); // UI Scale 편집
            Assert.IsTrue(state.IsDirty); // 변경 상태 검증

            state.Cancel(); // 저장값으로 편집값 복구
            Assert.IsFalse(state.IsDirty); // 취소 후 변경 없음 검증
            Assert.AreEqual(1f, state.Editing.UiScale, 0.001f); // UI Scale 복구 검증

            state.SetResolution(1600, 900); // 해상도 편집
            GameSettingsData applied = state.Apply(); // 편집값 저장 상태로 확정
            Assert.AreEqual(1600, applied.ResolutionWidth); // 적용 해상도 검증
            Assert.IsFalse(state.IsDirty); // 적용 후 변경 없음 검증

            state.ResetToDefault(); // 기본 설정 편집값 적용
            Assert.IsTrue(state.IsDirty); // 기존 적용값과 다른 기본값 변경 검증
            Assert.AreEqual(GameSettingsData.DefaultUiScale, state.Editing.UiScale, 0.001f); // 기본 UI Scale 검증
        }

        [Test]
        public void GameSettingsData_Normalized_ClampsInvalidValues()
        {
            var invalid = new GameSettingsData
            {
                ResolutionWidth = 100, // 잘못된 너비
                ResolutionHeight = 100, // 잘못된 높이
                ScreenMode = 999, // 잘못된 화면 모드
                UiScale = 4f // 범위 밖 UI Scale
            };

            GameSettingsData normalized = invalid.Normalized(); // 안전 범위 설정 생성

            Assert.AreEqual(GameSettingsData.MinimumWidth, normalized.ResolutionWidth); // 최소 너비 보정 검증
            Assert.AreEqual(GameSettingsData.MinimumHeight, normalized.ResolutionHeight); // 최소 높이 보정 검증
            Assert.AreEqual((int)FullScreenMode.FullScreenWindow, normalized.ScreenMode); // 화면 모드 fallback 검증
            Assert.AreEqual(GameSettingsData.MaximumUiScale, normalized.UiScale, 0.001f); // UI Scale 최대값 보정 검증
        }

        [Test]
        public void GameSettingsResolutionCatalog_DeduplicatesAndSortsSizes()
        {
            var candidates = new[]
            {
                new SettingsResolutionOption(1920, 1080), // FHD 후보
                new SettingsResolutionOption(1280, 720), // HD 후보
                new SettingsResolutionOption(1920, 1080), // 중복 FHD 후보
                new SettingsResolutionOption(2560, 1440) // QHD 후보
            };

            SettingsResolutionOption[] result = GameSettingsResolutionCatalog.DeduplicateAndSort(candidates); // 중복 제거·정렬

            Assert.AreEqual(3, result.Length); // 고유 해상도 수 검증
            Assert.AreEqual(2560, result[0].Width); // 높은 해상도 우선 정렬 검증
            Assert.AreEqual(1920, result[1].Width); // FHD 두 번째 정렬 검증
            Assert.AreEqual(1280, result[2].Width); // HD 마지막 정렬 검증
        }


        [Test]
        public void SceneRuntimeBootstrap_InjectsReusableSettingsIntoMainMenuAndBattle()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs"); // 실제 씬 부트스트랩 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 부트스트랩 구현 읽기

            StringAssert.Contains("EnsureComponent<MainMenuSettingsBridge>", source); // MainMenu 재사용 설정 패널 연결 검증
            StringAssert.Contains("EnsureComponent<BattleSettingsOverlayController>", source); // Battle 인게임 설정 오버레이 연결 검증
        }

        [Test]
        public void GameSettingsEditState_ScreenModeSupportsWindowedAndFullscreenWindow()
        {
            var state = new GameSettingsEditState(GameSettingsData.CreateDefault()); // 기본 설정 상태 생성

            state.SetScreenMode(FullScreenMode.Windowed); // 창모드 편집
            Assert.AreEqual((int)FullScreenMode.Windowed, state.Editing.ScreenMode); // 창모드 적용 검증

            state.SetScreenMode(FullScreenMode.FullScreenWindow); // 전체화면 창 편집
            Assert.AreEqual((int)FullScreenMode.FullScreenWindow, state.Editing.ScreenMode); // 전체화면 창 적용 검증
        }
    }
}
