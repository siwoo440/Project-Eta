using NUnit.Framework; // EditMode 테스트·Assert 사용
using UnityEngine; // Time.timeScale 사용
using ProjectEta.Battle; // CombatSpeedSettings 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day44CombatSpeedTests // 44일차 전투 배속 회귀 테스트
    {
        [SetUp] // 각 테스트 전 기본 배속 복구
        public void SetUp() // 정적 배속 상태 격리
        {
            CombatSpeedSettings.ResetToDefault(); // 현재 기존 속도인 3배속 기준으로 복구
        }

        [TearDown] // 각 테스트 후 기본 배속 복구
        public void TearDown() // 다른 테스트에 Time.timeScale 영향 차단
        {
            CombatSpeedSettings.ResetToDefault(); // 3배속·timeScale 1 복구
        }

        [Test] // 현재 기존 속도를 3배속 기준으로 취급하는지 검증
        public void DefaultSpeed_IsThreeAndKeepsUnityTimeScaleOne() // 3배속이 기존 실제 속도인지 확인
        {
            Assert.AreEqual(3, CombatSpeedSettings.CurrentSpeed); // 기본 선택 3배속 검증
            Assert.AreEqual(1f, Time.timeScale, 0.0001f); // 기존 게임 속도 유지 검증
        }

        [Test] // 단일 버튼 순환 순서 검증
        public void CycleToNextSpeed_ThreeOneTwoThree_Repeats() // 3→1→2→3 순환으로 1→2→3→1 고리 확인
        {
            Assert.AreEqual(1, CombatSpeedSettings.CycleToNextSpeed()); // 기본 3배속 다음은 1배속 검증
            Assert.AreEqual(1f / 3f, Time.timeScale, 0.0001f); // 1배속 실제 시간 배율 검증

            Assert.AreEqual(2, CombatSpeedSettings.CycleToNextSpeed()); // 1배속 다음은 2배속 검증
            Assert.AreEqual(2f / 3f, Time.timeScale, 0.0001f); // 2배속 실제 시간 배율 검증

            Assert.AreEqual(3, CombatSpeedSettings.CycleToNextSpeed()); // 2배속 다음은 3배속 검증
            Assert.AreEqual(1f, Time.timeScale, 0.0001f); // 3배속 기존 시간 배율 검증

            Assert.AreEqual(1, CombatSpeedSettings.CycleToNextSpeed()); // 3배속 다음 다시 1배속 검증
        }

        [Test] // 직접 1배속 선택 호환 검증
        public void SetSpeed_One_UsesOneThirdTimeScale() // 가장 느린 관찰용 속도 확인
        {
            Assert.IsTrue(CombatSpeedSettings.TrySetSpeed(1)); // 1배속 선택 성공 검증
            Assert.AreEqual(1, CombatSpeedSettings.CurrentSpeed); // 현재 배속 값 검증
            Assert.AreEqual(1f / 3f, Time.timeScale, 0.0001f); // 기존 3배속 대비 1/3 시간 배율 검증
        }

        [Test] // 잘못된 배속 입력 차단 검증
        public void SetSpeed_InvalidValue_KeepsCurrentSpeed() // 1~3 외 입력 방어 확인
        {
            Assert.IsFalse(CombatSpeedSettings.TrySetSpeed(4)); // 허용 범위 밖 선택 실패 검증
            Assert.AreEqual(3, CombatSpeedSettings.CurrentSpeed); // 기존 3배속 유지 검증
            Assert.AreEqual(1f, Time.timeScale, 0.0001f); // 실제 시간 배율도 유지 검증
        }
    }
}
