namespace ProjectEta.King
{
    public sealed class KingSelectionCarouselState
    {
        private static readonly KingArchetype[] OrderedArchetypes =
        {
            KingArchetype.Default, // 기본 King
            KingArchetype.Attack, // 공격형 King
            KingArchetype.Defense, // 방어형 King
            KingArchetype.Strategy // 전략형 King
        };

        private int _currentIndex; // 현재 중앙 King 인덱스

        public int Count => OrderedArchetypes.Length; // 전체 King 수
        public int CurrentIndex => _currentIndex; // 현재 중앙 인덱스
        public int PageNumber => _currentIndex + 1; // 1부터 시작하는 현재 페이지 번호
        public KingArchetype CurrentArchetype => OrderedArchetypes[_currentIndex]; // 현재 중앙 King 타입

        public KingSelectionCarouselState(KingArchetype initial)
        {
            SetCurrent(initial); // 초기 중앙 King 설정
        }

        public void SetCurrent(KingArchetype archetype)
        {
            for (int i = 0; i < OrderedArchetypes.Length; i++)
            {
                if (OrderedArchetypes[i] != archetype) continue; // 다른 King 타입 건너뜀
                _currentIndex = i; // 일치 King 인덱스 저장
                return; // 초기 위치 설정 종료
            }

            _currentIndex = 0; // 알 수 없는 타입은 기본 King으로 복구
        }

        public KingArchetype Move(int direction)
        {
            if (direction == 0) return CurrentArchetype; // 이동 없음 처리
            _currentIndex = WrapIndex(_currentIndex + (direction > 0 ? 1 : -1)); // 다음·이전 King 인덱스 순환
            return CurrentArchetype; // 이동 후 중앙 King 반환
        }

        public KingArchetype Peek(int offset)
        {
            return OrderedArchetypes[WrapIndex(_currentIndex + offset)]; // 현재 위치 기준 상대 King 반환
        }

        private static int WrapIndex(int index)
        {
            int count = OrderedArchetypes.Length; // 전체 King 수 조회
            if (count <= 0) return 0; // 빈 카탈로그 방어
            int wrapped = index % count; // 순환 나머지 계산
            if (wrapped < 0) wrapped += count; // 음수 인덱스 마지막부터 순환
            return wrapped; // 안전 인덱스 반환
        }
    }
}
