using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.Board
{
    public class BoardState
    {
        public const int Width = 10;
        public const int Height = 10;

        private readonly TileState[,] _tiles = new TileState[Width, Height];

        public BoardState()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var position = new Vector2Int(x, y);
                    _tiles[x, y] = new TileState(position)
                    {
                        IsPlayerPlacementArea = y < Height / 2,
                        IsEnemyPlacementArea = y >= Height / 2
                    };
                }
            }

            AbilityBoardRegistry.Register(this);
        }

        public bool IsInsideBoard(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width &&
                   position.y >= 0 && position.y < Height;
        }

        public TileState GetTile(Vector2Int position)
        {
            return IsInsideBoard(position) ? _tiles[position.x, position.y] : null;
        }

        public bool CanOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState ignorePiece = null)
        {
            if (size.x <= 0 || size.y <= 0) return false;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int position = anchor + new Vector2Int(x, y);
                    if (!IsInsideBoard(position)) return false;

                    TileState tile = GetTile(position);
                    if (tile == null || tile.IsBlocked) return false;

                    if (tile.OccupyingPiece != null && tile.OccupyingPiece != ignorePiece)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState piece)
        {
            if (piece == null) return false;
            if (!CanOccupyArea(anchor, size, piece)) return false;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    GetTile(anchor + new Vector2Int(x, y)).OccupyingPiece = piece;
                }
            }

            return true;
        }

        public void ClearArea(Vector2Int anchor, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) return;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    TileState tile = GetTile(anchor + new Vector2Int(x, y));
                    if (tile != null) tile.OccupyingPiece = null;
                }
            }
        }

        public int ClearPiece(PieceRuntimeState piece)
        {
            if (piece == null) return 0;

            int clearedCount = 0;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_tiles[x, y].OccupyingPiece != piece) continue;
                    _tiles[x, y].OccupyingPiece = null;
                    clearedCount++;
                }
            }

            return clearedCount;
        }

        public int CountPieces(bool isPlayerPiece)
        {
            var uniquePieces = new HashSet<PieceRuntimeState>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    PieceRuntimeState piece = _tiles[x, y].OccupyingPiece;
                    if (piece == null) continue;

                    bool hasConfiguredHp = piece.Definition != null && piece.Definition.BaseHp > 0;
                    if (piece.IsDead && hasConfiguredHp) continue;
                    if (piece.IsPlayerPiece != isPlayerPiece) continue;

                    uniquePieces.Add(piece);
                }
            }

            return uniquePieces.Count;
        }
    }
}
