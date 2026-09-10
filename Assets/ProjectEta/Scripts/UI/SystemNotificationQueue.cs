using System.Collections.Generic; // Queue<T> 사용

namespace ProjectEta.UI
{
    public sealed class SystemNotificationMessage
    {
        public string Title { get; } // 시스템 알림 제목
        public string Body { get; } // 시스템 알림 본문
        public float Duration { get; } // 시스템 알림 표시 시간

        public SystemNotificationMessage(string title, string body, float duration)
        {
            Title = title ?? string.Empty; // null 제목 빈 문자열 보정
            Body = body ?? string.Empty; // null 본문 빈 문자열 보정
            Duration = duration < 0.5f ? 0.5f : duration; // 최소 표시 시간 보정
        }
    }

    public sealed class SystemNotificationQueue
    {
        private readonly Queue<SystemNotificationMessage> _queue = new Queue<SystemNotificationMessage>(); // 대기 알림 Queue

        public int Count => _queue.Count; // 대기 알림 수

        public void Enqueue(SystemNotificationMessage message)
        {
            if (message == null) return; // 빈 알림 등록 차단
            _queue.Enqueue(message); // 알림 순서 등록
        }

        public bool TryDequeue(out SystemNotificationMessage message)
        {
            if (_queue.Count == 0)
            {
                message = null; // 빈 Queue 결과 초기화
                return false; // 꺼낼 알림 없음
            }

            message = _queue.Dequeue(); // 가장 오래된 알림 반환
            return true; // 알림 반환 성공
        }

        public void Clear()
        {
            _queue.Clear(); // 대기 알림 전체 초기화
        }
    }
}
