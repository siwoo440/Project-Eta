using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.Board
{
    public static class MovementResolver
    {
        public static MovementResult GetReachableTiles(
            PieceMovementType movementType,
            Vector2Int origin,
            bool isPlayerPiece,
            BoardState board)
        {
            IMovementRule rule = MovementRuleFactory.CreateLegacy(movementType);
            return rule.Resolve(origin, isPlayerPiece, board);
        }

        public static MovementResult GetReachableTiles(
            PieceDefinition definition,
            Vector2Int origin,
            bool isPlayerPiece,
            BoardState board)
        {
            IMovementRule rule = MovementRuleFactory.CreateFor(definition);
            return rule.Resolve(origin, isPlayerPiece, board);
        }

        public static MovementResult GetReachableTiles(PieceRuntimeState piece, BoardState board)
        {
            if (piece == null) return new MovementResult();

            if (!piece.CanMove && !piece.CanAttack)
            {
                return new MovementResult();
            }

            MovementResult result;

            if (piece.Definition != null && piece.Definition.PieceId == "chameleon")
            {
                PieceMovementType movementType;

                switch (piece.MovementCycleIndex)
                {
                    case 1: movementType = PieceMovementType.Bishop; break;
                    case 2: movementType = PieceMovementType.Rook; break;
                    case 3: movementType = PieceMovementType.Queen; break;
                    default: movementType = PieceMovementType.Knight; break;
                }

                result = GetReachableTiles(
                    movementType,
                    piece.BoardPosition,
                    piece.IsPlayerPiece,
                    board);
            }
            else
            {
                result = GetReachableTiles(
                    piece.Definition,
                    piece.BoardPosition,
                    piece.IsPlayerPiece,
                    board);
            }

            if (!piece.CanMove)
            {
                result.MoveTiles.Clear();
            }

            if (!piece.CanAttack)
            {
                result.AttackTiles.Clear();
            }

            MovementModifierResolver.Apply(piece, board, result);
            return result;
        }
    }
}
