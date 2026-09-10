using System.Collections.Generic; // 알림 대기열 사용

namespace ProjectEta.UI
{
    public sealed class Day67AnnouncementQueue
    {
        private readonly Queue<Day67BattleAnnouncement> _items = new Queue<Day67BattleAnnouncement>(); // 순차 알림 저장소
        private string _lastQueuedKey = string.Empty; // 인접 중복 억제 키

        public int Count => _items.Count; // 현재 대기 알림 수

        public bool Enqueue(Day67BattleAnnouncement announcement)
        {
            if (string.IsNullOrWhiteSpace(announcement.Title)) return false; // 빈 알림 등록 차단
            if (announcement.Key == _lastQueuedKey) return false; // 동일 알림 연속 등록 차단

            _items.Enqueue(announcement); // 새 알림 순서 보존 등록
            _lastQueuedKey = announcement.Key; // 최근 등록 키 갱신
            return true; // 등록 성공 반환
        }

        public bool TryDequeue(out Day67BattleAnnouncement announcement)
        {
            if (_items.Count == 0)
            {
                announcement = default; // 빈 대기열 기본 결과 반환
                return false; // 꺼낼 알림 없음 반환
            }

            announcement = _items.Dequeue(); // 가장 먼저 등록된 알림 반환
            return true; // 꺼내기 성공 반환
        }

        public void Clear()
        {
            _items.Clear(); // 모든 대기 알림 제거
            _lastQueuedKey = string.Empty; // 중복 기준 초기화
        }
    }
}
