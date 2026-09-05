using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // Resources와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // BossHealthUI를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day39BossHealthBalanceTests // 보스 HP UI와 요청된 보스 능력치 절반 조정을 검증하는 회귀 테스트
    {
        [Test] // PrototypeBoss37의 HP와 ATK가 기존 30/4에서 정확히 절반인 15/2로 조정됐는지 검증
        public void PrototypeBoss37_UsesHalfHealthAndAttack()
        {
            PieceDefinition boss = Resources.Load<PieceDefinition>("PrototypeBoss37"); // Resources에서 실제 보스 정의 로드

            Assert.IsNotNull(boss); // 보스 데이터가 실제로 존재해야 함
            Assert.AreEqual(15, boss.BaseHp); // 기존 HP 30의 절반
            Assert.AreEqual(2, boss.BaseAtk); // 기존 ATK 4의 절반
        }

        [Test] // 보스 HP UI가 현재 체력과 최대 체력을 한 줄로 읽기 쉽게 표시하는지 검증
        public void BossHealthUI_BuildDisplayTextShowsCurrentAndMaxHp()
        {
            string text = BossHealthUI.BuildDisplayText("2x2 프로토타입 보스", 9, 15); // HP 9/15 상태 문구 생성

            StringAssert.Contains("BOSS", text); // 보스 UI임을 즉시 알 수 있어야 함
            StringAssert.Contains("2x2 프로토타입 보스", text); // 실제 보스 이름 포함
            StringAssert.Contains("HP 9 / 15", text); // 현재/최대 체력 숫자 포함
        }

        [Test] // 체력바 비율이 0~1 범위로 안전하게 보정되는지 검증
        public void BossHealthUI_CalculateHealth01ClampsRange()
        {
            Assert.AreEqual(1f, BossHealthUI.CalculateHealth01(15, 15), 0.0001f); // 만피는 1
            Assert.AreEqual(0.5f, BossHealthUI.CalculateHealth01(5, 10), 0.0001f); // 절반은 0.5
            Assert.AreEqual(0f, BossHealthUI.CalculateHealth01(0, 15), 0.0001f); // 사망은 0
            Assert.AreEqual(0f, BossHealthUI.CalculateHealth01(10, 0), 0.0001f); // 잘못된 최대 HP는 0으로 안전 처리
        }
    }
}
