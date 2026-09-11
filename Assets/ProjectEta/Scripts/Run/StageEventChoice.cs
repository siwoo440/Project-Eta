namespace ProjectEta.Run
{
    public enum StageEventChoiceEffectType
    {
        None = 0, // 변화 없는 선택
        CardReward = 1, // 무료 카드 획득 선택
        HealKing = 2, // 무료 킹 회복 선택
        RiskRewardCard = 3, // HP를 지불하고 Gold·카드 획득 선택
        PurchaseCard = 4, // Gold를 지불하고 카드 획득 선택
        CurrencyGain = 5, // Gold 획득 선택
        PaidHeal = 6, // Gold를 지불하고 킹 회복 선택
        UpgradeCard = 7 // 보유 카드 무료 강화 선택
    }

    public enum StageEventFollowUp
    {
        Complete = 0, // 이벤트 즉시 완료
        CardChoice = 1, // 카드 선택 화면 연결
        UpgradeChoice = 2 // 강화 카드 선택 화면 연결
    }

    public sealed class StageEventChoice
    {
        public string Id { get; } // 선택지 고유 ID
        public string Title { get; } // 선택지 제목
        public string Description { get; } // 선택지 설명
        public StageEventChoiceEffectType EffectType { get; } // 선택 효과 종류

        public StageEventChoice(string id, string title, string description, StageEventChoiceEffectType effectType)
        {
            Id = id ?? string.Empty; // 선택지 ID 저장
            Title = title ?? string.Empty; // 선택지 제목 저장
            Description = description ?? string.Empty; // 선택지 설명 저장
            EffectType = effectType; // 선택 효과 저장
        }
    }

    public sealed class StageEventResolution
    {
        public StageEventFollowUp FollowUp { get; } // 다음 화면 흐름
        public StageChoiceResult Result { get; } // 즉시 적용 결과

        public StageEventResolution(StageEventFollowUp followUp, StageChoiceResult result)
        {
            FollowUp = followUp; // 후속 흐름 저장
            Result = result; // 선택 결과 저장
        }
    }
}
