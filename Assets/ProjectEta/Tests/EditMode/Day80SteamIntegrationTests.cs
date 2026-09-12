using System.Collections.Generic; // Achievement ID 수집 목록 사용
using NUnit.Framework; // EditMode 단위 테스트 사용
using ProjectEta.Pieces; // PieceGrade 사용

namespace ProjectEta.Steam.Tests
{
    public sealed class Day80SteamIntegrationTests
    {
        [Test]
        public void QueueBattleVictory_WhenNormalStage_QueuesFirstVictory() // 일반 전투 승리 Achievement 연결 검증
        {
            var queued = new List<string>(); // Achievement ID 수집 목록 생성

            SteamGameEventBridge.QueueBattleVictory(1, queued.Add); // 일반 Stage 승리 이벤트 전달

            Assert.That(queued, Is.EquivalentTo(new[] { SteamAchievementIds.FirstVictory })); // 첫 승리 ID만 등록 검증
        }

        [Test]
        public void QueueBattleVictory_WhenMidBossStage_QueuesMidBossClear() // 중간 보스 승리 Achievement 연결 검증
        {
            var queued = new List<string>(); // Achievement ID 수집 목록 생성

            SteamGameEventBridge.QueueBattleVictory(SteamGameEventBridge.MidBossRound, queued.Add); // Stage 5 승리 이벤트 전달

            Assert.That(queued, Does.Contain(SteamAchievementIds.FirstVictory)); // 첫 승리 ID 포함 검증
            Assert.That(queued, Does.Contain(SteamAchievementIds.MidBossClear)); // 중간 보스 ID 포함 검증
        }

        [Test]
        public void QueueFusionCompleted_WhenFiveStar_QueuesFusionAndFiveStar() // 5성 합성 Achievement 연결 검증
        {
            var queued = new List<string>(); // Achievement ID 수집 목록 생성

            SteamGameEventBridge.QueueFusionCompleted(PieceGrade.FiveStar, queued.Add); // 5성 합성 이벤트 전달

            Assert.That(queued, Does.Contain(SteamAchievementIds.FirstFusion)); // 첫 합성 ID 포함 검증
            Assert.That(queued, Does.Contain(SteamAchievementIds.FirstFiveStar)); // 첫 5성 ID 포함 검증
        }

        [Test]
        public void QueueCardAcquired_WhenFiveStar_QueuesFiveStar() // 5성 카드 획득 Achievement 연결 검증
        {
            var queued = new List<string>(); // Achievement ID 수집 목록 생성

            SteamGameEventBridge.QueueCardAcquired(PieceGrade.FiveStar, queued.Add); // 5성 카드 획득 이벤트 전달

            Assert.That(queued, Is.EquivalentTo(new[] { SteamAchievementIds.FirstFiveStar })); // 첫 5성 ID 등록 검증
        }

        [Test]
        public void QueueRunCompleted_QueuesFirstRunClear() // Run 완료 Achievement 연결 검증
        {
            var queued = new List<string>(); // Achievement ID 수집 목록 생성

            SteamGameEventBridge.QueueRunCompleted(queued.Add); // Run 완료 이벤트 전달

            Assert.That(queued, Is.EquivalentTo(new[] { SteamAchievementIds.FirstRunClear })); // 첫 Run 클리어 ID 등록 검증
        }

        [Test]
        public void IsFusionPoolDelta_WhenTwoMaterialsBecomeOneResult_ReturnsTrue() // 합성 보유 풀 변화 판정 검증
        {
            bool result = SteamGameEventBridge.IsFusionPoolDelta(6, 5, true); // 재료 2장 제거·결과 1장 추가 형태 전달

            Assert.That(result, Is.True); // 합성 변화로 판정 검증
        }

        [Test]
        public void IsFusionPoolDelta_WhenOnlyCardRemoved_ReturnsFalse() // 단순 카드 제거 합성 오판 방지 검증
        {
            bool result = SteamGameEventBridge.IsFusionPoolDelta(6, 5, false); // 카드 제거만 발생한 형태 전달

            Assert.That(result, Is.False); // 합성 변화 아님 검증
        }
    }
}
