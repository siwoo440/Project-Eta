using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // FullScreenMode 사용
using ProjectEta.Settings; // 57일차 설정·오디오 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day57AudioSettingsTests
    {
        [Test]
        public void GameSettingsData_LegacySettingsReceiveDefaultAudioVolumes()
        {
            var legacy = new GameSettingsData
            {
                SettingsVersion = 0, // 56일차 이전 설정 버전
                ResolutionWidth = 1920, // 기존 해상도 너비
                ResolutionHeight = 1080, // 기존 해상도 높이
                ScreenMode = (int)FullScreenMode.FullScreenWindow, // 기존 화면 모드
                UiScale = 1f, // 기존 UI Scale
                MasterVolume = 0f, // 구버전 누락 필드 직렬화 결과 가정
                BgmVolume = 0f, // 구버전 누락 필드 직렬화 결과 가정
                SfxVolume = 0f // 구버전 누락 필드 직렬화 결과 가정
            };

            GameSettingsData normalized = legacy.Normalized(); // 구버전 설정 마이그레이션

            Assert.AreEqual(GameSettingsData.CurrentVersion, normalized.SettingsVersion); // 최신 설정 버전 변환 검증
            Assert.AreEqual(GameSettingsData.DefaultMasterVolume, normalized.MasterVolume, 0.001f); // Master 기본값 검증
            Assert.AreEqual(GameSettingsData.DefaultBgmVolume, normalized.BgmVolume, 0.001f); // BGM 기본값 검증
            Assert.AreEqual(GameSettingsData.DefaultSfxVolume, normalized.SfxVolume, 0.001f); // SFX 기본값 검증
        }

        [Test]
        public void GameSettingsData_CurrentAudioVolumesAreClamped()
        {
            var current = GameSettingsData.CreateDefault(); // 최신 기본 설정 생성
            current.MasterVolume = 2f; // 최대 범위 초과 Master
            current.BgmVolume = -1f; // 최소 범위 미만 BGM
            current.SfxVolume = 0.4f; // 정상 SFX

            GameSettingsData normalized = current.Normalized(); // 최신 오디오 설정 보정

            Assert.AreEqual(1f, normalized.MasterVolume, 0.001f); // Master 최대값 보정 검증
            Assert.AreEqual(0f, normalized.BgmVolume, 0.001f); // BGM 최소값 보정 검증
            Assert.AreEqual(0.4f, normalized.SfxVolume, 0.001f); // SFX 정상값 유지 검증
        }

        [Test]
        public void GameSettingsEditState_AudioPreviewValuesParticipateInDirtyAndCancel()
        {
            var state = new GameSettingsEditState(GameSettingsData.CreateDefault()); // 기본 설정 편집 상태 생성

            state.SetMasterVolume(0.65f); // Master 편집
            state.SetBgmVolume(0.45f); // BGM 편집
            state.SetSfxVolume(0.8f); // SFX 편집

            Assert.IsTrue(state.IsDirty); // 오디오 변경 Dirty 상태 검증
            Assert.AreEqual(0.65f, state.Editing.MasterVolume, 0.001f); // Master 편집값 검증
            Assert.AreEqual(0.45f, state.Editing.BgmVolume, 0.001f); // BGM 편집값 검증
            Assert.AreEqual(0.8f, state.Editing.SfxVolume, 0.001f); // SFX 편집값 검증

            state.Cancel(); // 미적용 오디오 편집 취소

            Assert.IsFalse(state.IsDirty); // 취소 후 Dirty 해제 검증
            Assert.AreEqual(GameSettingsData.DefaultMasterVolume, state.Editing.MasterVolume, 0.001f); // Master 복원 검증
        }

        [Test]
        public void GameSettingsEditState_ResetSoundCategoryDoesNotResetDisplay()
        {
            var initial = GameSettingsData.CreateDefault(); // 기본 설정 생성
            initial.ResolutionWidth = 1600; // 사용자 해상도 설정
            initial.ResolutionHeight = 900; // 사용자 해상도 설정
            initial.MasterVolume = 0.35f; // 사용자 Master 설정
            var state = new GameSettingsEditState(initial); // 사용자 설정 편집 상태 생성

            state.SetMasterVolume(0.2f); // Master 추가 편집
            state.ResetCategory(SettingsCategory.Sound); // Sound 카테고리만 초기화

            Assert.AreEqual(1600, state.Editing.ResolutionWidth); // Display 해상도 유지 검증
            Assert.AreEqual(900, state.Editing.ResolutionHeight); // Display 해상도 유지 검증
            Assert.AreEqual(GameSettingsData.DefaultMasterVolume, state.Editing.MasterVolume, 0.001f); // Sound 기본값 복원 검증
        }

        [Test]
        public void SettingsCategoryCatalog_ReservesFutureCategories()
        {
            Assert.IsTrue(SettingsCategoryCatalog.IsImplemented(SettingsCategory.Display)); // Display 활성 카테고리 검증
            Assert.IsTrue(SettingsCategoryCatalog.IsImplemented(SettingsCategory.Sound)); // Sound 활성 카테고리 검증
            Assert.IsFalse(SettingsCategoryCatalog.IsImplemented(SettingsCategory.Controls)); // Controls 향후 카테고리 검증
            Assert.IsFalse(SettingsCategoryCatalog.IsImplemented(SettingsCategory.Gameplay)); // Gameplay 향후 카테고리 검증
            Assert.IsFalse(SettingsCategoryCatalog.IsImplemented(SettingsCategory.Accessibility)); // Accessibility 향후 카테고리 검증
        }

        [Test]
        public void GameAudioService_EffectiveVolumeUsesMasterAndChannel()
        {
            float effective = GameAudioService.CalculateEffectiveVolume(0.5f, 0.4f); // Master·채널 합성 볼륨 계산

            Assert.AreEqual(0.2f, effective, 0.001f); // 실제 출력 비율 계산 검증
        }
    }
}
