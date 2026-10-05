using ProjectEta.Abilities;
using ProjectEta.Battle;
using ProjectEta.Board;

namespace ProjectEta.AI
{
    public static class EnemyAIThreatScoreEvaluator
    {
        private const int ThreatPenaltyPerAttacker = 120;
        private const int EscapeBonusPerReducedThreat = 40;

        public static int Evaluate(
            AIActionCandidate candidate,
            BoardState board,
            EnemyAIThreatMap threatMap)
        {
            if (candidate == null ||
                candidate.Actor == null ||
                board == null ||
                threatMap == null)
            {
                return 0;
            }

            UnityEngine.Vector2Int resultingPosition =
                ResolveResultingPosition(candidate, board);

            int currentThreat =
                threatMap.GetThreatCount(candidate.Origin);

            int resultingThreat =
                threatMap.GetThreatCount(resultingPosition);

            int score =
                -resultingThreat *
                ThreatPenaltyPerAttacker;

            if (resultingThreat < currentThreat)
            {
                score +=
                    (currentThreat - resultingThreat) *
                    EscapeBonusPerReducedThreat;
            }

            return score;
        }

        private static UnityEngine.Vector2Int ResolveResultingPosition(
            AIActionCandidate candidate,
            BoardState board)
        {
            if (candidate.ActionType == AIActionType.Move)
            {
                return candidate.Target;
            }

            if (candidate.ActionType != AIActionType.Attack)
            {
                return candidate.Origin;
            }

            var targetPiece = candidate.TargetPiece;

            if (targetPiece == null ||
                candidate.Actor.Definition == null)
            {
                return candidate.Origin;
            }

            bool lethal =
                AuraResolver.GetAttack(candidate.Actor, board) >=
                targetPiece.CurrentHp;

            if (!lethal)
            {
                return candidate.Origin;
            }

            bool occupyAfterKill =
                CombatMovementPolicy
                    .ShouldOccupyDefenderTileAfterKill(
                        candidate.Actor.Definition);

            return occupyAfterKill
                ? candidate.Target
                : candidate.Origin;
        }
    }
}
