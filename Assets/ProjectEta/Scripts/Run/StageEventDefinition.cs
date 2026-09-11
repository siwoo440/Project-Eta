using System.Collections.Generic; // IReadOnlyList<T> 사용

namespace ProjectEta.Run
{
    public enum StageEventType
    {
        CardFind = 0, // 카드 획득 이벤트
        Rest = 1, // 킹 회복 이벤트
        RiskReward = 2, // HP 대가 보상 이벤트
        TravelingMerchant = 3, // 이벤트 전용 카드 구매
        GoldCache = 4, // Gold 획득 이벤트
        Shrine = 5, // 유료 회복 이벤트
        Training = 6 // 보유 카드 강화 이벤트
    }

    public sealed class StageEventDefinition
    {
        private readonly StageEventChoice[] _choices; // 이벤트 선택지 배열

        public string Id { get; } // 이벤트 고유 ID
        public StageEventType EventType { get; } // 이벤트 종류
        public string Title { get; } // 이벤트 제목
        public string Description { get; } // 이벤트 설명
        public int MinPhase { get; } // 최소 등장 Phase
        public int MaxPhase { get; } // 최대 등장 Phase
        public int BaseWeight { get; } // 기본 등장 가중치
        public IReadOnlyList<StageEventChoice> Choices => _choices; // 읽기 전용 선택지 목록

        public StageEventDefinition(
            string id,
            StageEventType eventType,
            string title,
            string description,
            int minPhase,
            int maxPhase,
            int baseWeight,
            params StageEventChoice[] choices)
        {
            Id = id ?? string.Empty; // 이벤트 ID 저장
            EventType = eventType; // 이벤트 종류 저장
            Title = title ?? string.Empty; // 이벤트 제목 저장
            Description = description ?? string.Empty; // 이벤트 설명 저장
            MinPhase = minPhase; // 최소 Phase 저장
            MaxPhase = maxPhase; // 최대 Phase 저장
            BaseWeight = baseWeight < 0 ? 0 : baseWeight; // 음수 가중치 차단
            _choices = choices ?? new StageEventChoice[0]; // 선택지 배열 저장
        }

        public bool IsAvailableInPhase(int phase)
        {
            return phase >= MinPhase && phase <= MaxPhase; // 현재 Phase 등장 가능 여부 반환
        }
    }

    public sealed class StageEventScenario
    {
        public StageEventDefinition Definition { get; } // 선택된 이벤트 정의
        public int Seed { get; } // 이벤트 재현 Seed
        public StageEventType EventType => Definition != null ? Definition.EventType : StageEventType.CardFind; // 기존 이벤트 타입 호환
        public string Title => Definition != null ? Definition.Title : string.Empty; // 기존 제목 호환
        public string Description => Definition != null ? Definition.Description : string.Empty; // 기존 설명 호환
        public IReadOnlyList<StageEventChoice> Choices => Definition != null ? Definition.Choices : new StageEventChoice[0]; // 이벤트 선택지 반환

        public StageEventScenario(StageEventDefinition definition, int seed)
        {
            Definition = definition; // 이벤트 정의 저장
            Seed = seed; // 이벤트 Seed 저장
        }
    }
}
