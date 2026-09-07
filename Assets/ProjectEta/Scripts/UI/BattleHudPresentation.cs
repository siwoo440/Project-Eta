using System; // Math 사용
using ProjectEta.Battle; // TurnState·TurnManager 사용
using ProjectEta.Run; // RoundState 사용

namespace ProjectEta.UI
{
    public static class BattleHudPresentation
    {
        private const int MidBossStage = 5; // 중간 보스 스테이지 번호

        public static string BuildStageText(int currentStage)
        {
            int stage = Math.Max(RoundState.FirstRound, Math.Min(RoundState.FinalRound, currentStage)); // 표시 스테이지 범위 보정

            if (stage == RoundState.FinalRound)
            {
                return $"STAGE {stage} / {RoundState.FinalRound} · FINAL BOSS"; // 최종 보스 스테이지 문구
            }

            if (stage == MidBossStage)
            {
                return $"STAGE {stage} / {RoundState.FinalRound} · MID BOSS"; // 중간 보스 스테이지 문구
            }

            return $"STAGE {stage} / {RoundState.FinalRound}"; // 일반 스테이지 문구
        }

        public static string BuildTurnStateText(TurnState state, bool isInitialDeployment)
        {
            if (state == TurnState.DeploymentTurn && isInitialDeployment)
            {
                return "INITIAL DEPLOYMENT"; // 최초 배치 상태 문구
            }

            if (state == TurnState.DeploymentTurn)
            {
                return "DEPLOYMENT"; // 주기 배치 상태 문구
            }

            if (state == TurnState.PlayerTurn)
            {
                return "PLAYER TURN"; // 플레이어 턴 문구
            }

            if (state == TurnState.EnemyTurn)
            {
                return "ENEMY TURN"; // 적 턴 문구
            }

            return "BATTLE ENDED"; // 전투 종료 문구
        }

        public static string BuildTurnText(int turnNumber, int turnLimit)
        {
            int turn = Math.Max(1, turnNumber); // 표시 턴 번호 보정
            int limit = Math.Max(0, turnLimit); // 표시 턴 제한 보정

            if (limit > 0)
            {
                return $"TURN {turn} / {limit}"; // 턴 제한 포함 문구
            }

            return $"TURN {turn}"; // 턴 제한 미확인 문구
        }

        public static string BuildGoldText(int currency)
        {
            return $"GOLD {Math.Max(0, currency)}"; // 음수 없는 Gold 문구
        }

        public static string BuildDeploymentText(TurnState state, bool isInitialDeployment, int deployedCardCount, int turnNumber)
        {
            if (state == TurnState.BattleEnded)
            {
                return string.Empty; // 종료 전투 배치 안내 숨김
            }

            if (state == TurnState.DeploymentTurn && isInitialDeployment)
            {
                return "King과 기물을 배치하세요"; // 최초 배치 안내
            }

            if (state == TurnState.DeploymentTurn)
            {
                return $"배치한 카드 {Math.Max(0, deployedCardCount)}"; // 주기 배치 카드 수 안내
            }

            int remainingTurns = CalculateTurnsUntilDeployment(turnNumber); // 다음 배치까지 남은 턴 계산

            if (remainingTurns <= 0)
            {
                return "NEXT DEPLOYMENT THIS TURN"; // 현재 턴 종료 후 배치 안내
            }

            return $"NEXT DEPLOYMENT {remainingTurns}"; // 다음 배치까지 남은 턴 안내
        }

        public static int CalculateTurnsUntilDeployment(int turnNumber)
        {
            int turn = Math.Max(1, turnNumber); // 계산용 턴 번호 보정
            int remainder = turn % TurnManager.DeploymentInterval; // 배치 주기 나머지 계산

            if (remainder == 0)
            {
                return 0; // 현재 턴 종료 후 배치 진입 표시
            }

            return TurnManager.DeploymentInterval - remainder; // 다음 배치 턴까지 남은 일반 턴 수 반환
        }
    }
}
