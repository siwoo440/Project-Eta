using UnityEngine; // Mathf 사용

namespace ProjectEta.Meta
{
    public static class MetaRewardCalculator
    {
        public const int ParticipationBase = 2; // 런 종료 기본 참여 보상
        public const int PerReachedStage = 2; // 도달 단계당 보상
        public const int MidBossBonus = 10; // 5단계 중간 보스 처치 보너스
        public const int FinalBossBonus = 25; // 10단계 최종 보스 처치 보너스
        public const int ClearBonus = 15; // 전체 런 클리어 추가 보너스

        public static int Calculate(int reachedStage, bool midBossDefeated, bool finalBossDefeated, bool runCompleted)
        {
            int safeStage = Mathf.Clamp(reachedStage, 1, 10); // 1~10 단계 범위 보정
            int reward = ParticipationBase + safeStage * PerReachedStage; // 기본·진행도 보상 계산
            if (midBossDefeated) reward += MidBossBonus; // 중간 보스 보너스 적용
            if (finalBossDefeated) reward += FinalBossBonus; // 최종 보스 보너스 적용
            if (runCompleted) reward += ClearBonus; // 전체 클리어 보너스 적용
            return reward; // 최종 메타 토큰 보상 반환
        }
    }
}
