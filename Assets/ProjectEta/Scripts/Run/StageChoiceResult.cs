using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public enum StageChoiceEffectType
    {
        None = 0, // 변화 없음
        CurrencyChanged = 1, // 런 재화 변화
        KingHpChanged = 2, // 킹 HP 변화
        CardAdded = 3, // 카드 획득
        CardRemoved = 4, // 카드 제거
        CardUpgraded = 5, // 카드 강화
        Mixed = 6 // 복합 결과
    }

    public sealed class StageChoiceResult
    {
        public StageChoiceEffectType EffectType { get; } // 결과 종류
        public string Summary { get; } // 결과 로그 설명
        public int CurrencyDelta { get; } // 런 재화 증감
        public int KingHpDelta { get; } // 킹 HP 증감
        public PieceDefinition Card { get; } // 관련 카드

        public StageChoiceResult(StageChoiceEffectType effectType, string summary, int currencyDelta, int kingHpDelta, PieceDefinition card)
        {
            EffectType = effectType; // 결과 종류 저장
            Summary = summary ?? string.Empty; // 결과 설명 저장
            CurrencyDelta = currencyDelta; // 재화 변화 저장
            KingHpDelta = kingHpDelta; // HP 변화 저장
            Card = card; // 관련 카드 저장
        }
    }
}
