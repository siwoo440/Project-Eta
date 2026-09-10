using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Battle; // TurnState 사용
using ProjectEta.UI; // 67일차 알림 타입 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day67AnnouncementQueueTests
    {
        [Test]
        public void Enqueue_PreservesFirstInFirstOutOrder()
        {
            var queue = new Day67AnnouncementQueue(); // 새 알림 대기열 생성
            queue.Enqueue(Day67BattleAnnouncement.CreateBattleStart(3, false)); // 첫 전투 시작 알림 등록
            queue.Enqueue(Day67BattleAnnouncement.CreateTurn(TurnState.PlayerTurn, 2, false)); // 두 번째 플레이어 턴 알림 등록

            Assert.That(queue.TryDequeue(out Day67BattleAnnouncement first), Is.True); // 첫 알림 꺼내기 확인
            Assert.That(first.Title, Is.EqualTo("BATTLE START")); // FIFO 첫 제목 확인
            Assert.That(queue.TryDequeue(out Day67BattleAnnouncement second), Is.True); // 두 번째 알림 꺼내기 확인
            Assert.That(second.Title, Is.EqualTo("PLAYER TURN")); // FIFO 두 번째 제목 확인
        }

        [Test]
        public void Enqueue_RejectsAdjacentDuplicateAnnouncement()
        {
            var queue = new Day67AnnouncementQueue(); // 새 알림 대기열 생성
            Day67BattleAnnouncement announcement = Day67BattleAnnouncement.CreatePassive("격노 +1"); // 동일 패시브 알림 생성

            Assert.That(queue.Enqueue(announcement), Is.True); // 첫 등록 허용 확인
            Assert.That(queue.Enqueue(announcement), Is.False); // 인접 중복 등록 차단 확인
            Assert.That(queue.Count, Is.EqualTo(1)); // 실제 대기 수 한 개 유지 확인
        }

        [Test]
        public void CreateBattleStart_UsesBossLabelForBossRound()
        {
            Day67BattleAnnouncement announcement = Day67BattleAnnouncement.CreateBattleStart(10, true); // 최종 보스 전투 시작 알림 생성

            Assert.That(announcement.Title, Is.EqualTo("BOSS BATTLE")); // 보스 전투 전용 제목 확인
            Assert.That(announcement.Subtitle, Is.EqualTo("STAGE 10")); // 실제 Stage 문구 확인
        }

        [Test]
        public void CreateTurn_UsesDeploymentLabelForInitialDeployment()
        {
            Day67BattleAnnouncement announcement = Day67BattleAnnouncement.CreateTurn(TurnState.DeploymentTurn, 1, true); // 시작 배치 알림 생성

            Assert.That(announcement.Title, Is.EqualTo("DEPLOYMENT")); // 배치 제목 확인
            Assert.That(announcement.Subtitle, Is.EqualTo("INITIAL DEPLOYMENT")); // 시작 배치 보조 문구 확인
        }
    }
}
