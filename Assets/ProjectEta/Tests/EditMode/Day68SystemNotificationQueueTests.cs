using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.UI; // 시스템 알림 Queue 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day68SystemNotificationQueueTests
    {
        [Test]
        public void NotificationQueue_등록순서대로알림을반환한다()
        {
            var queue = new SystemNotificationQueue(); // 빈 시스템 알림 Queue 생성
            queue.Enqueue(new SystemNotificationMessage("저장 완료", "STAGE 2", 1.5f)); // 첫 알림 등록
            queue.Enqueue(new SystemNotificationMessage("불러오기 완료", "STAGE 5", 2f)); // 두 번째 알림 등록

            Assert.IsTrue(queue.TryDequeue(out SystemNotificationMessage first)); // 첫 알림 꺼내기 검증
            Assert.IsTrue(queue.TryDequeue(out SystemNotificationMessage second)); // 두 번째 알림 꺼내기 검증
            Assert.AreEqual("저장 완료", first.Title); // 첫 제목 순서 검증
            Assert.AreEqual("불러오기 완료", second.Title); // 두 번째 제목 순서 검증
            Assert.AreEqual(0, queue.Count); // Queue 소진 검증
        }

        [Test]
        public void NotificationMessage_지속시간은_최소값으로보정한다()
        {
            var message = new SystemNotificationMessage("테스트", string.Empty, -5f); // 잘못된 지속시간 알림 생성

            Assert.GreaterOrEqual(message.Duration, 0.5f); // 최소 표시시간 보정 검증
        }
    }
}
