using System; // Action<string> Achievement 큐 연결
using ProjectEta.Pieces; // PieceGrade 사용

namespace ProjectEta.Steam
{
    public static class SteamGameEventBridge
    {
        public const int MidBossRound = 5; // 중간 보스 고정 Stage 번호

        public static void QueueBattleVictory(int completedRound, Action<string> queueAchievement) // 전투 승리 Achievement 변환
        {
            if (queueAchievement == null)
            {
                return;
            }

            queueAchievement(SteamAchievementIds.FirstVictory); // 첫 전투 승리 Achievement 등록

            if (completedRound == MidBossRound)
            {
                queueAchievement(SteamAchievementIds.MidBossClear); // 중간 보스 격파 Achievement 등록
            }
        }

        public static void QueueFusionCompleted(PieceGrade resultGrade, Action<string> queueAchievement) // 합성 성공 Achievement 변환
        {
            if (queueAchievement == null)
            {
                return;
            }

            queueAchievement(SteamAchievementIds.FirstFusion); // 첫 합성 성공 Achievement 등록

            if (resultGrade == PieceGrade.FiveStar)
            {
                queueAchievement(SteamAchievementIds.FirstFiveStar); // 5성 합성 Achievement 등록
            }
        }

        public static void QueueCardAcquired(PieceGrade acquiredGrade, Action<string> queueAchievement) // 카드 획득 Achievement 변환
        {
            if (queueAchievement == null || acquiredGrade != PieceGrade.FiveStar)
            {
                return;
            }

            queueAchievement(SteamAchievementIds.FirstFiveStar); // 첫 5성 획득 Achievement 등록
        }

        public static void QueueRunCompleted(Action<string> queueAchievement) // Run 클리어 Achievement 변환
        {
            if (queueAchievement == null)
            {
                return;
            }

            queueAchievement(SteamAchievementIds.FirstRunClear); // 첫 Run 클리어 Achievement 등록
        }

        public static bool IsFusionPoolDelta(int previousTotal, int currentTotal, bool hasAddedCard) // 보유 카드 변화 기반 합성 성공 판정
        {
            return previousTotal >= 2
                && currentTotal == previousTotal - 1
                && hasAddedCard; // 재료 2장 제거와 결과 1장 추가 형태 확인
        }
    }
}
