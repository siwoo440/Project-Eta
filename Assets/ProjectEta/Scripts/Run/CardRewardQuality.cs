using ProjectEta.Pieces; // PieceGrade 사용

namespace ProjectEta.Run
{
    public enum CardRewardQuality
    {
        Basic = 0, // 초반 기본 보상
        Improved = 1, // 중반·우대 보상
        Advanced = 2 // 후반 고급 보상
    }

    public sealed class CardRewardProfile
    {
        public CardRewardQuality Quality { get; } // 보상 품질 단계
        public CardRewardSource Source { get; } // 보상 발생 경로
        public int Stage { get; } // 현재 Stage
        public int OneStarWeight { get; } // 1성 선택 가중치
        public int TwoStarWeight { get; } // 2성 선택 가중치
        public int ThreeStarWeight { get; } // 3성 선택 가중치

        public string DisplayName
        {
            get
            {
                switch (Quality)
                {
                    case CardRewardQuality.Advanced:
                        return "고급 보상"; // 고급 품질 표시
                    case CardRewardQuality.Improved:
                        return "향상 보상"; // 향상 품질 표시
                    default:
                        return "기본 보상"; // 기본 품질 표시
                }
            }
        }

        public CardRewardProfile(CardRewardQuality quality, CardRewardSource source, int stage, int oneStarWeight, int twoStarWeight, int threeStarWeight)
        {
            Quality = quality; // 품질 단계 저장
            Source = source; // 보상 경로 저장
            Stage = stage < 1 ? 1 : stage; // 최소 Stage 1 보정
            OneStarWeight = oneStarWeight < 0 ? 0 : oneStarWeight; // 1성 음수 가중치 차단
            TwoStarWeight = twoStarWeight < 0 ? 0 : twoStarWeight; // 2성 음수 가중치 차단
            ThreeStarWeight = threeStarWeight < 0 ? 0 : threeStarWeight; // 3성 음수 가중치 차단
        }

        public int GetGradeWeight(PieceGrade grade)
        {
            switch (grade)
            {
                case PieceGrade.OneStar:
                    return OneStarWeight; // 1성 가중치 반환
                case PieceGrade.TwoStar:
                    return TwoStarWeight; // 2성 가중치 반환
                case PieceGrade.ThreeStar:
                    return ThreeStarWeight; // 3성 가중치 반환
                default:
                    return 0; // 일반 Reward 제외 등급 가중치 없음
            }
        }
    }
}
