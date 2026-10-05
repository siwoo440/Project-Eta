using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.AI
{
    public sealed class EnemyAIPlanner
    {
        private const int MoveBaseScore = 10;
        private const int DistanceApproachScore = 25;
        private const int ImmediateAttackScore = 1000;
        private const int DirectKingAttackScore = 5000;
        private const int LethalAttackScore = 400;
        private const int DamageScorePerPoint = 40;
        private const int CurrentHpScorePerPoint = 2;

        public List<AIActionCandidate> BuildCandidates(BoardState board)
        {
            var candidates = new List<AIActionCandidate>();
            AIAbilityCandidateRegistry.Clear(); // 93일차: 이전 평가의 Ability 메타데이터 제거
            if (board == null) return candidates;

            var visitedPieces = new HashSet<PieceRuntimeState>();
            bool hasPriorityTarget = TryFindPlayerKing(board, out PieceRuntimeState playerKing);
            Vector2Int priorityTargetPosition = hasPriorityTarget
                ? playerKing.BoardPosition
                : FindFirstPlayerPiecePosition(board);
            bool hasAnyPlayerPiece = hasPriorityTarget || HasAnyPlayerPiece(board);

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    TileState tile = board.GetTile(new Vector2Int(x, y));
                    PieceRuntimeState actor = tile?.OccupyingPiece;

                    if (actor == null || actor.IsPlayerPiece || actor.IsDead) continue;
                    if (!visitedPieces.Add(actor)) continue;

                    MovementResult movement = MovementResolver.GetReachableTiles(actor, board);

                    for (int i = 0; i < movement.MoveTiles.Count; i++)
                    {
                        Vector2Int target = movement.MoveTiles[i];
                        TileState targetTile = board.GetTile(target);

                        if (targetTile == null ||
                            targetTile.IsOccupied ||
                            targetTile.IsBlockedByObstacle)
                        {
                            continue;
                        }

                        int score = EvaluateMove(
                            actor,
                            target,
                            hasAnyPlayerPiece,
                            priorityTargetPosition,
                            board);

                        candidates.Add(new AIActionCandidate(
                            actor,
                            actor.BoardPosition,
                            target,
                            AIActionType.Move,
                            null,
                            score));
                    }

                    for (int i = 0; i < movement.AttackTiles.Count; i++)
                    {
                        Vector2Int target = movement.AttackTiles[i];
                        TileState targetTile = board.GetTile(target);
                        PieceRuntimeState targetPiece = targetTile?.OccupyingPiece;

                        if (targetPiece == null ||
                            !targetPiece.IsPlayerPiece ||
                            targetPiece.IsDead)
                        {
                            continue;
                        }

                        int score = EvaluateAttack(actor, targetPiece, board);

                        candidates.Add(new AIActionCandidate(
                            actor,
                            actor.BoardPosition,
                            target,
                            AIActionType.Attack,
                            targetPiece,
                            score));
                    }

                    // 93일차: Active Ability도 Move/Attack과 같은 후보 목록에서 평가한다.
                    candidates.AddRange(
                        EnemyAIAbilityCandidateBuilder.BuildCandidates(board, actor));
                }
            }

            return candidates;
        }

        public bool TryChooseAction(BoardState board, out AIActionCandidate selectedAction)
        {
            List<AIActionCandidate> candidates = BuildCandidates(board);

            if (candidates.Count == 0)
            {
                selectedAction = null;
                return false;
            }

            selectedAction = candidates[0];

            for (int i = 1; i < candidates.Count; i++)
            {
                if (IsBetterCandidate(candidates[i], selectedAction))
                {
                    selectedAction = candidates[i];
                }
            }

            return true;
        }

        private static int EvaluateMove(
            PieceRuntimeState actor,
            Vector2Int target,
            bool hasPriorityTarget,
            Vector2Int priorityTargetPosition,
            BoardState board)
        {
            int score = MoveBaseScore;
            score += Mathf.Max(0, actor.CurrentHp) * CurrentHpScorePerPoint;

            if (hasPriorityTarget)
            {
                int beforeDistance = ManhattanDistance(
                    actor.BoardPosition,
                    priorityTargetPosition);

                int afterDistance = ManhattanDistance(
                    target,
                    priorityTargetPosition);

                score +=
                    (beforeDistance - afterDistance) *
                    DistanceApproachScore;
            }

            score += EnemyAIThreeStarRoleScoreEvaluator.EvaluateMoveBonus(
                actor,
                target,
                board);

            return score;
        }

        private static int EvaluateAttack(
            PieceRuntimeState actor,
            PieceRuntimeState targetPiece,
            BoardState board)
        {
            int score = ImmediateAttackScore;
            score += Mathf.Max(0, actor.CurrentHp) * CurrentHpScorePerPoint;

            int attackPower = Mathf.Max(
                0,
                AuraResolver.GetAttack(actor, board) +
                FourStarCombatAbilityResolver.PreviewDamageBonus(actor, targetPiece));

            int expectedDamage = Mathf.Min(
                attackPower,
                Mathf.Max(0, targetPiece.CurrentHp));

            score += expectedDamage * DamageScorePerPoint;

            if (attackPower >= targetPiece.CurrentHp)
            {
                score += LethalAttackScore;
            }

            if (IsKing(targetPiece))
            {
                score += DirectKingAttackScore;
            }

            score += EnemyAIThreeStarRoleScoreEvaluator.EvaluateAttackBonus(
                actor,
                targetPiece);

            return score;
        }

        private static bool IsBetterCandidate(
            AIActionCandidate challenger,
            AIActionCandidate currentBest)
        {
            if (challenger.Score != currentBest.Score)
            {
                return challenger.Score > currentBest.Score;
            }

            if (challenger.ActionType != currentBest.ActionType)
            {
                if (challenger.ActionType == AIActionType.Attack) return true;
                if (currentBest.ActionType == AIActionType.Attack) return false;
                if (challenger.ActionType == AIActionType.Ability) return true;
                if (currentBest.ActionType == AIActionType.Ability) return false;
            }

            string challengerId =
                challenger.Actor?.Definition?.PieceId ?? string.Empty;

            string currentId =
                currentBest.Actor?.Definition?.PieceId ?? string.Empty;

            int idComparison = string.Compare(
                challengerId,
                currentId,
                StringComparison.Ordinal);

            if (idComparison != 0) return idComparison < 0;

            if (challenger.Origin.y != currentBest.Origin.y)
            {
                return challenger.Origin.y < currentBest.Origin.y;
            }

            if (challenger.Origin.x != currentBest.Origin.x)
            {
                return challenger.Origin.x < currentBest.Origin.x;
            }

            if (challenger.Target.y != currentBest.Target.y)
            {
                return challenger.Target.y < currentBest.Target.y;
            }

            return challenger.Target.x < currentBest.Target.x;
        }

        private static bool TryFindPlayerKing(
            BoardState board,
            out PieceRuntimeState king)
        {
            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece =
                        board.GetTile(new Vector2Int(x, y))?.OccupyingPiece;

                    if (piece == null ||
                        !piece.IsPlayerPiece ||
                        piece.IsDead)
                    {
                        continue;
                    }

                    if (IsKing(piece))
                    {
                        king = piece;
                        return true;
                    }
                }
            }

            king = null;
            return false;
        }

        private static Vector2Int FindFirstPlayerPiecePosition(
            BoardState board)
        {
            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece =
                        board.GetTile(new Vector2Int(x, y))?.OccupyingPiece;

                    if (piece != null &&
                        piece.IsPlayerPiece &&
                        !piece.IsDead)
                    {
                        return piece.BoardPosition;
                    }
                }
            }

            return Vector2Int.zero;
        }

        private static bool HasAnyPlayerPiece(BoardState board)
        {
            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece =
                        board.GetTile(new Vector2Int(x, y))?.OccupyingPiece;

                    if (piece != null &&
                        piece.IsPlayerPiece &&
                        !piece.IsDead)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsKing(PieceRuntimeState piece)
        {
            if (piece?.Definition == null) return false;

            if (string.Equals(
                    piece.Definition.PieceId,
                    "king",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return piece.Definition.MovementType ==
                   PieceMovementType.King;
        }

        private static int ManhattanDistance(
            Vector2Int a,
            Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) +
                   Mathf.Abs(a.y - b.y);
        }
    }
}
