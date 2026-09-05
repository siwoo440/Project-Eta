namespace ProjectEta.Run
{
    public enum StageEventType
    {
        CardFind = 0, // 카드 획득 이벤트
        Rest = 1, // 킹 회복 이벤트
        RiskReward = 2 // HP를 대가로 큰 보상을 받는 이벤트
    }

    public sealed class StageEventScenario
    {
        public StageEventType EventType { get; } // 이벤트 종류
        public string Title { get; } // 이벤트 제목
        public string Description { get; } // 이벤트 설명

        public StageEventScenario(StageEventType eventType, string title, string description)
        {
            EventType = eventType; // 이벤트 종류 저장
            Title = title ?? string.Empty; // 이벤트 제목 저장
            Description = description ?? string.Empty; // 이벤트 설명 저장
        }
    }

    public static class StageEventGenerator
    {
        public static StageEventScenario Create(int depth)
        {
            int safeDepth = depth <= 0 ? 1 : depth; // 잘못된 깊이 보정
            int cycle = (safeDepth - 1) % 3; // 카드→휴식→위험 순환

            if (cycle == 0)
            {
                return new StageEventScenario(
                    StageEventType.CardFind,
                    "버려진 카드 꾸러미",
                    "판 위에 오래된 카드 꾸러미가 놓여 있습니다.\n쓸 만한 카드 한 장을 챙길 수 있습니다."); // 카드 이벤트 생성
            }

            if (cycle == 1)
            {
                return new StageEventScenario(
                    StageEventType.Rest,
                    "조용한 휴식처",
                    "잠시 숨을 고를 수 있는 안전한 장소입니다.\n킹의 HP를 1 회복할 수 있습니다."); // 회복 이벤트 생성
            }

            return new StageEventScenario(
                StageEventType.RiskReward,
                "위험한 계약",
                "체력 1을 대가로 런 재화와 카드 보상을 얻을 수 있습니다.\nHP가 1이면 계약할 수 없습니다."); // 위험 보상 이벤트 생성
        }
    }
}
