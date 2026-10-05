using UnityEngine;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class FourStarMovementAbilityResolver
    {
        private static readonly Vector2Int[] Orthogonal =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private static readonly Vector2Int[] Around8 =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        public static void ApplyMovementCandidates(
            PieceRuntimeState piece,
            BoardState board,
            MovementResult result)
        {
            if (piece == null || board == null || result == null) return;

            if (FourStarCombatAbilityResolver.HasAbility(
                    piece,
                    FourStarAbilityIds.SiegeChariotSiege))
            {
                AddSiegeChariotCandidates(piece, board, result);
            }
        }

        public static void ProcessBeforeAction(PieceRuntimeState piece)
        {
            if (piece == null) return;

            if (FourStarCombatAbilityResolver.HasAbility(
                    piece,
                    FourStarAbilityIds.GatekeeperBlock))
            {
                // 문지기가 자기 다음 행동을 시작하면 이전에 만든 봉쇄는 만료된다.
                TileBlockService.ClearForSource(piece);
            }
        }

        public static void ProcessBeforeAttack(
            PieceRuntimeState attacker,
            PieceRuntimeState defender)
        {
            if (attacker == null || defender == null) return;

            ProcessBeforeAction(attacker);

            if (!FourStarCombatAbilityResolver.HasAbility(
                    attacker,
                    FourStarAbilityIds.SiegeChariotSiege))
            {
                return;
            }

            BoardState board = AbilityBoardRegistry.FindBoardContaining(attacker);
            if (board == null) return;

            TryDestroyCrossedObstacle(
                attacker.BoardPosition,
                defender.BoardPosition,
                board);
        }

        public static void ProcessAfterMove(
            PieceRuntimeState piece,
            Vector2Int origin,
            Vector2Int destination)
        {
            if (piece == null) return;

            BoardState board = AbilityBoardRegistry.FindBoardContaining(piece);
            if (board == null) return;

            if (FourStarCombatAbilityResolver.HasAbility(
                    piece,
                    FourStarAbilityIds.SiegeChariotSiege))
            {
                TryDestroyCrossedObstacle(origin, destination, board);
            }

            if (FourStarCombatAbilityResolver.HasAbility(
                    piece,
                    FourStarAbilityIds.GatekeeperBlock))
            {
                TryCreateGatekeeperBlock(piece, board);
            }
        }

        private static void AddSiegeChariotCandidates(
            PieceRuntimeState piece,
            BoardState board,
            MovementResult result)
        {
            for (int d = 0; d < Orthogonal.Length; d++)
            {
                Vector2Int direction = Orthogonal[d];
                bool crossedDestructible = false;

                for (int step = 1; step < Mathf.Max(BoardState.Width, BoardState.Height); step++)
                {
                    Vector2Int target = piece.BoardPosition + direction * step;
                    if (!board.IsInsideBoard(target)) break;

                    TileState tile = board.GetTile(target);
                    if (tile == null) break;

                    if (tile.IsBlockedByAbility) break;

                    if (tile.HasObstacle)
                    {
                        if (!crossedDestructible && tile.IsObstacleDestructible)
                        {
                            crossedDestructible = true;
                            continue;
                        }

                        break;
                    }

                    if (tile.IsOccupied)
                    {
                        if (crossedDestructible &&
                            piece.CanAttack &&
                            tile.OccupyingPiece != null &&
                            tile.OccupyingPiece.IsPlayerPiece != piece.IsPlayerPiece)
                        {
                            result.AddAttack(target);
                        }

                        break;
                    }

                    if (crossedDestructible &&
                        piece.CanMove &&
                        board.CanOccupyArea(
                            target,
                            GetSafeFootprint(piece.Definition),
                            piece))
                    {
                        result.AddMove(target);
                    }
                }
            }
        }

        private static bool TryDestroyCrossedObstacle(
            Vector2Int origin,
            Vector2Int destination,
            BoardState board)
        {
            Vector2Int delta = destination - origin;

            if (delta.x != 0 && delta.y != 0) return false;
            if (delta == Vector2Int.zero) return false;

            Vector2Int direction = new Vector2Int(
                delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1),
                delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1));

            int distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

            for (int step = 1; step < distance; step++)
            {
                TileState tile = board.GetTile(origin + direction * step);
                if (tile == null) return false;

                if (!tile.HasObstacle) continue;
                if (!tile.IsObstacleDestructible) return false;

                return tile.TryDestroyObstacle();
            }

            return false;
        }

        private static void TryCreateGatekeeperBlock(
            PieceRuntimeState source,
            BoardState board)
        {
            Vector2Int best = default;
            bool found = false;
            int bestEnemyDistance = int.MaxValue;

            for (int i = 0; i < Around8.Length; i++)
            {
                Vector2Int candidate = source.BoardPosition + Around8[i];
                if (!TileBlockService.CanBlock(board, candidate)) continue;

                int enemyDistance = GetNearestEnemyDistance(
                    source,
                    candidate,
                    board);

                if (!found ||
                    enemyDistance < bestEnemyDistance ||
                    (enemyDistance == bestEnemyDistance &&
                     (candidate.y < best.y ||
                      (candidate.y == best.y && candidate.x < best.x))))
                {
                    found = true;
                    best = candidate;
                    bestEnemyDistance = enemyDistance;
                }
            }

            if (!found) return;

            // turnManager 없이 Source 기반 수명주기로 유지하며 다음 자기 행동/PlayerTurn에서 정리한다.
            TileBlockService.TryBlock(
                board,
                best,
                source,
                null,
                1);
        }

        private static int GetNearestEnemyDistance(
            PieceRuntimeState source,
            Vector2Int position,
            BoardState board)
        {
            int best = int.MaxValue;

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece =
                        board.GetTile(new Vector2Int(x, y))?.OccupyingPiece;

                    if (piece == null ||
                        piece.IsDead ||
                        piece.IsPlayerPiece == source.IsPlayerPiece)
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

        private static Vector2Int GetSafeFootprint(PieceDefinition definition)
        {
            if (definition == null) return Vector2Int.one;

            Vector2Int size = definition.OccupancySize;
            return new Vector2Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y));
        }
    }
}
