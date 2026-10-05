using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.AI
{
    public static class EnemyAIThreeStarRoleScoreEvaluator
    {
        public static int EvaluateMoveBonus(
            PieceRuntimeState actor,
            Vector2Int target,
            BoardState board)
        {
            if (actor?.Definition == null || board == null) return 0;

            PieceRoleTag tags = actor.Definition.RoleTags;
            int score = 0;

            if ((tags & PieceRoleTag.Ranged) != 0)
            {
                int before = DistanceToNearestOpponent(actor.BoardPosition, actor.IsPlayerPiece, board);
                int after = DistanceToNearestOpponent(target, actor.IsPlayerPiece, board);

                score += ScoreRangedDistance(after) - ScoreRangedDistance(before);
            }

            if ((tags & PieceRoleTag.Support) != 0 ||
                (tags & PieceRoleTag.Summoner) != 0)
            {
                int beforeAllies = CountNearbyAllies(actor, actor.BoardPosition, board);
                int afterAllies = CountNearbyAllies(actor, target, board);
                score += (afterAllies - beforeAllies) * 45;
            }

            if ((tags & PieceRoleTag.Tanker) != 0)
            {
                int beforeAllies = CountNearbyAllies(actor, actor.BoardPosition, board);
                int afterAllies = CountNearbyAllies(actor, target, board);
                score += (afterAllies - beforeAllies) * 35;
            }

            return score;
        }

        public static int EvaluateAttackBonus(
            PieceRuntimeState actor,
            PieceRuntimeState target)
        {
            if (actor?.Definition == null || target == null) return 0;

            PieceRoleTag tags = actor.Definition.RoleTags;
            int score = 0;

            if ((tags & PieceRoleTag.Ranged) != 0)
            {
                int distance = Mathf.Max(
                    Mathf.Abs(actor.BoardPosition.x - target.BoardPosition.x),
                    Mathf.Abs(actor.BoardPosition.y - target.BoardPosition.y));

                if (distance >= 2) score += 160;
                if (distance == 1) score -= 80;
            }

            if ((tags & PieceRoleTag.Support) != 0)
            {
                score -= 60;
            }

            return score;
        }

        private static int ScoreRangedDistance(int distance)
        {
            if (distance <= 0 || distance == int.MaxValue) return 0;
            if (distance == 3) return 120;
            if (distance == 2 || distance == 4) return 80;
            if (distance == 1) return -100;
            return 20;
        }

        private static int DistanceToNearestOpponent(
            Vector2Int position,
            bool isPlayerPiece,
            BoardState board)
        {
            int best = int.MaxValue;

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece;

                    if (piece == null ||
                        piece.IsDead ||
                        piece.IsPlayerPiece == isPlayerPiece)
                    {
                        continue;
                    }

                    int distance =
                        Mathf.Abs(position.x - piece.BoardPosition.x) +
                        Mathf.Abs(position.y - piece.BoardPosition.y);

                    if (distance < best) best = distance;
                }
            }

            return best;
        }

        private static int CountNearbyAllies(
            PieceRuntimeState actor,
            Vector2Int position,
            BoardState board)
        {
            int count = 0;
            var pieces = AbilityBoardRegistry.GetUniquePieces(board);

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState piece = pieces[i];

                if (piece == null ||
                    piece.IsDead ||
                    object.ReferenceEquals(piece, actor) ||
                    piece.IsPlayerPiece != actor.IsPlayerPiece)
                {
                    continue;
                }

                int distance = Mathf.Max(
                    Mathf.Abs(position.x - piece.BoardPosition.x),
                    Mathf.Abs(position.y - piece.BoardPosition.y));

                if (distance <= 2) count++;
            }

            return count;
        }
    }
}
