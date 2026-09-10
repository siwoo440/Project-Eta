using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Settings; // 게임 설정 데이터 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day68GameSettingsDataTests
    {
        [Test]
        public void SettingsVersion2_오디오값은유지하고_튜토리얼은미완료로보정한다()
        {
            var legacy = GameSettingsData.CreateDefault(); // 기본 설정 생성
            legacy.SettingsVersion = 2; // 57일차 저장 버전 설정
            legacy.MasterVolume = 0.35f; // 기존 Master 값 적용
            legacy.BgmVolume = 0.45f; // 기존 BGM 값 적용
            legacy.SfxVolume = 0.55f; // 기존 SFX 값 적용
            legacy.FirstTutorialCompleted = true; // 구버전에서 신뢰하면 안 되는 신규 필드 값 설정

            GameSettingsData normalized = legacy.Normalized(); // 최신 설정으로 보정

            Assert.AreEqual(0.35f, normalized.MasterVolume, 0.001f); // 기존 Master 값 보존 검증
            Assert.AreEqual(0.45f, normalized.BgmVolume, 0.001f); // 기존 BGM 값 보존 검증
            Assert.AreEqual(0.55f, normalized.SfxVolume, 0.001f); // 기존 SFX 값 보존 검증
            Assert.IsFalse(normalized.FirstTutorialCompleted); // 구버전은 최초 튜토리얼 미완료 처리 검증
        }

        [Test]
        public void SettingsVersion3_튜토리얼완료상태를유지한다()
        {
            var current = GameSettingsData.CreateDefault(); // 최신 기본 설정 생성
            current.FirstTutorialCompleted = true; // 튜토리얼 완료 상태 적용

            GameSettingsData normalized = current.Normalized(); // 최신 설정 보정

            Assert.IsTrue(normalized.FirstTutorialCompleted); // 완료 상태 유지 검증
            Assert.AreEqual(GameSettingsData.CurrentVersion, normalized.SettingsVersion); // 최신 버전 유지 검증
        }
    }
}
