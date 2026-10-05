using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class TileBlockService
    {
        private static readonly List<TileBlockState> Blocks = new List<TileBlockState>();
        private static readonly Dictionary<TurnManager, Action<TurnState, int>> TurnHandlers =
            new Dictionary<TurnManager, Action<TurnState, int>>();

        public static int ActiveCount => Blocks.Count;

        public static bool CanBlock(BoardState board, Vector2Int position)
        {
            if (board == null || !board.IsInsideBoard(position)) return false;

            TileState tile = board.GetTile(position);
            return tile != null &&
                   !tile.IsOccupied &&
                   !tile.HasObstacle &&
                   !tile.IsBlockedByAbility;
        }

        public static bool TryBlock(
            BoardState board,
            Vector2Int position,
            PieceRuntimeState source,
            TurnManager turnManager,
            int durationPlayerTurns = 1)
        {
            if (!CanBlock(board, position)) return false;

            TileState tile = board.GetTile(position);
            tile.IsBlockedByAbility = true;

            int currentTurn = turnManager != null ? turnManager.TurnNumber : 0;
            int expires = currentTurn + Mathf.Max(1, durationPlayerTurns);

            Blocks.Add(new TileBlockState(board, position, source, turnManager, expires));
            EnsureTurnSubscription(turnManager);
            return true;
        }

        public static bool IsBlocked(BoardState board, Vector2Int position)
        {
            TileState tile = board?.GetTile(position);
            return tile != null && tile.IsBlockedByAbility;
        }

        public static IReadOnlyList<Vector2Int> GetBlockedPositions(BoardState board)
        {
            var result = new List<Vector2Int>();

            for (int i = 0; i < Blocks.Count; i++)
            {
                TileBlockState block = Blocks[i];
                if (block != null && object.ReferenceEquals(block.Board, board))
                {
                    result.Add(block.Position);
                }
            }

            return result;
        }

        public static int ClearForBoard(BoardState board)
        {
            int cleared = 0;

            for (int i = Blocks.Count - 1; i >= 0; i--)
            {
                TileBlockState block = Blocks[i];
                if (block == null || !object.ReferenceEquals(block.Board, board)) continue;

                ClearBlock(block);
                Blocks.RemoveAt(i);
                cleared++;
            }

            return cleared;
        }


        public static int ClearForSource(PieceRuntimeState source)
        {
            if (source == null) return 0;

            int cleared = 0;

            for (int i = Blocks.Count - 1; i >= 0; i--)
            {
                TileBlockState block = Blocks[i];

                if (block == null ||
                    !object.ReferenceEquals(block.Source, source))
                {
                    continue;
                }

                ClearBlock(block);
                Blocks.RemoveAt(i);
                cleared++;
            }

            return cleared;
        }

        public static void ClearAll()
        {
            foreach (KeyValuePair<TurnManager, Action<TurnState, int>> pair in TurnHandlers)
            {
                if (pair.Key != null) pair.Key.TurnChanged -= pair.Value;
            }

            TurnHandlers.Clear();

            for (int i = Blocks.Count - 1; i >= 0; i--)
            {
                ClearBlock(Blocks[i]);
            }

            Blocks.Clear();
        }

        private static void EnsureTurnSubscription(TurnManager turnManager)
        {
            if (turnManager == null || TurnHandlers.ContainsKey(turnManager)) return;

            Action<TurnState, int> handler = null;
            handler = (state, turnNumber) =>
            {
                if (state == TurnState.BattleEnded)
                {
                    ClearForTurnManager(turnManager, true, turnNumber);
                    turnManager.TurnChanged -= handler;
                    TurnHandlers.Remove(turnManager);
                    return;
                }

                if (state == TurnState.PlayerTurn)
                {
                    ClearForTurnManager(turnManager, false, turnNumber);
                }
            };

            TurnHandlers.Add(turnManager, handler);
            turnManager.TurnChanged += handler;
        }

        private static void ClearForTurnManager(TurnManager turnManager, bool clearAll, int turnNumber)
        {
            for (int i = Blocks.Count - 1; i >= 0; i--)
            {
                TileBlockState block = Blocks[i];

                if (block == null || !object.ReferenceEquals(block.TurnManager, turnManager)) continue;
                if (!clearAll && turnNumber < block.ExpiresOnPlayerTurn) continue;

                ClearBlock(block);
                Blocks.RemoveAt(i);
            }
        }

        private static void ClearBlock(TileBlockState block)
        {
            TileState tile = block?.Board?.GetTile(block.Position);
            if (tile != null) tile.IsBlockedByAbility = false;
        }
    }
}
