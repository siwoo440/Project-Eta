using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine.InputSystem; // Key 열거형 사용
using ProjectEta.Settings; // 설정·조작키 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day69InputBindingTests
    {
        [Test]
        public void LegacyV3Settings_조작키는기본값으로마이그레이션된다()
        {
            GameSettingsData legacy = GameSettingsData.CreateDefault(); // 기본 설정 생성
            legacy.SettingsVersion = 3; // 조작키 저장 이전 버전으로 설정
            legacy.CompleteActionKey = "R"; // 구버전에서 존재할 수 없는 임의 값 주입
            legacy.PauseKey = "P"; // 구버전에서 존재할 수 없는 임의 값 주입

            GameSettingsData normalized = legacy.Normalized(); // 최신 설정으로 마이그레이션

            Assert.AreEqual("Space", normalized.CompleteActionKey); // 행동 완료 기본 Space 적용 검증
            Assert.AreEqual("Escape", normalized.PauseKey); // Pause 기본 ESC 적용 검증
        }

        [Test]
        public void Version4Settings_유효한변경조작키를유지한다()
        {
            GameSettingsData data = GameSettingsData.CreateDefault(); // 최신 기본 설정 생성
            data.CompleteActionKey = "R"; // 행동 완료 키 변경
            data.PauseKey = "P"; // Pause 키 변경

            GameSettingsData normalized = data.Normalized(); // 설정 정규화

            Assert.AreEqual("R", normalized.CompleteActionKey); // 변경 행동 키 유지 검증
            Assert.AreEqual("P", normalized.PauseKey); // 변경 Pause 키 유지 검증
        }

        [Test]
        public void InvalidBinding_지원하지않는문자열은기본키로복구된다()
        {
            GameSettingsData data = GameSettingsData.CreateDefault(); // 최신 기본 설정 생성
            data.CompleteActionKey = "NotAKey"; // 잘못된 행동 완료 키 설정
            data.PauseKey = string.Empty; // 빈 Pause 키 설정

            GameSettingsData normalized = data.Normalized(); // 잘못된 키 보정

            Assert.AreEqual("Space", normalized.CompleteActionKey); // 행동 완료 기본값 복구 검증
            Assert.AreEqual("Escape", normalized.PauseKey); // Pause 기본값 복구 검증
        }

        [Test]
        public void InputBindingRules_표시문구는조작법에맞게변환된다()
        {
            Assert.AreEqual("Space", GameInputBindingRules.GetDisplayName(Key.Space)); // Space 표시 검증
            Assert.AreEqual("ESC", GameInputBindingRules.GetDisplayName(Key.Escape)); // Escape 축약 표시 검증
            Assert.AreEqual("R", GameInputBindingRules.GetDisplayName(Key.R)); // 문자 키 표시 검증
        }

        [Test]
        public void EditState_행동완료키변경은설정변경으로기록된다()
        {
            GameSettingsEditState state = new GameSettingsEditState(GameSettingsData.CreateDefault()); // 설정 편집 상태 생성

            state.SetControlKey(GameInputAction.CompleteAction, Key.R); // 행동 완료 키 R로 변경

            Assert.IsTrue(state.IsDirty); // 설정 변경 상태 검증
            Assert.AreEqual("R", state.Editing.CompleteActionKey); // 편집값 반영 검증
        }

        [Test]
        public void InputBindingService_적용한키를런타임과표시에같이사용한다()
        {
            GameSettingsData data = GameSettingsData.CreateDefault(); // 최신 기본 설정 생성
            data.CompleteActionKey = "R"; // 행동 완료 R 설정
            data.PauseKey = "P"; // Pause P 설정

            GameInputBindingService.Apply(data); // 런타임 조작키 적용

            Assert.AreEqual(Key.R, GameInputBindingService.GetKey(GameInputAction.CompleteAction)); // 실제 행동 키 R 적용 검증
            Assert.AreEqual("R", GameInputBindingService.GetDisplayName(GameInputAction.CompleteAction)); // 조작법 표시 R 동기화 검증
            Assert.AreEqual(Key.P, GameInputBindingService.GetKey(GameInputAction.Pause)); // 실제 Pause 키 P 적용 검증
            Assert.AreEqual("P", GameInputBindingService.GetDisplayName(GameInputAction.Pause)); // 조작법 표시 P 동기화 검증
            GameInputBindingService.ResetRuntimeState(); // 다음 테스트를 위한 정적 상태 초기화
        }
    }
}
